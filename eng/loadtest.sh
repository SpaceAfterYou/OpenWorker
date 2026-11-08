#!/usr/bin/env bash
# Runs the load test with one command: starts the servers of compose.yaml, waits until they accept connections,
# creates the accounts of the bots, runs the bots, and writes the report to TestResults/loadtest/<timestamp>/.
#
#   SOULWORKER_DATAS=/mnt/games/soulworker/datas eng/loadtest.sh --bots 500 --duration 2m
#
# Everything after the script name goes to `OpenWorker.LoadTest run` (--help of the script prints its options). The script
# adds --docker-stats, --output and --auth-port in front of them, so the same options given by hand win.
#
# SOULWORKER_DATAS   Required. The `datas` folder of an installed client: the district, gate and maze servers read the
#                    tables, the batch files and the scripts of it, and cannot start without.
# LOADTEST_ACCOUNTS  Accounts to create. Default: the number of --bots, counted from --account-offset.
# LOADTEST_WAIT      Seconds to wait for the servers. Default 300.
# LOADTEST_SETTLE    Seconds to wait after the ports are open, for the districts to register their channels. Default 15.
# LOADTEST_CHANNELS  Channels of a district (a channel holds 65 players). Default 20.
# LOADTEST_NO_BUILD  Set to 1 to start the containers without building the images again.
# LOADTEST_DOWN      Set to 1 to stop the containers when the run is over. By default they are left running, so
#                    the next run starts at once.
# LOADTEST_DISCORD_TOKEN  A real token for the relay; the placeholder in the override is enough for the load test.
#
# The servers answer the bots with 127.0.0.1, which is right for a bot on this machine. For a bot in a container or
# on another host pass --redirect-host <address>.

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

COMPOSE=(docker compose -f compose.yaml -f eng/loadtest/compose.loadtest.yaml)
PROJECT=OpenWorker.LoadTest/OpenWorker.LoadTest.csproj
DLL=OpenWorker.LoadTest/bin/Release/net10.0/OpenWorker.LoadTest.dll

fail() {
  echo "loadtest: $*" >&2
  exit 1
}

# The tables and the batch files are what the servers cannot start without, so say so here rather than let five
# containers fail one after another.
if [[ -z "${SOULWORKER_DATAS:-}" ]]; then
  fail "SOULWORKER_DATAS is not set. Point it at the 'datas' folder of an installed SoulWorker client, e.g.
  SOULWORKER_DATAS=/mnt/games/soulworker/datas eng/loadtest.sh --bots 100
The district, gate and maze servers read the client tables from it and do not start without them."
fi

for folder in bin/Table World/Table Character/ActionData Scripts/Server; do
  [[ -d "$SOULWORKER_DATAS/$folder" ]] || fail "SOULWORKER_DATAS=$SOULWORKER_DATAS has no folder '$folder'. It must be the 'datas' folder of a client."
done

command -v docker > /dev/null || fail "docker is not installed or not on the PATH."
command -v dotnet > /dev/null || fail "dotnet is not installed or not on the PATH."

export SOULWORKER_DATAS

# What the seed command needs to know about the accounts is in the options of the bots.
bots=100
prefix=lt
offset=0
password=loadtest
auth_port=10000
output=""

args=("$@")

for ((i = 0; i < ${#args[@]}; i++)); do
  case "${args[i]}" in
    --bots) bots="${args[i + 1]:-}" ;;
    --bots=*) bots="${args[i]#*=}" ;;
    --account-prefix) prefix="${args[i + 1]:-}" ;;
    --account-prefix=*) prefix="${args[i]#*=}" ;;
    --account-offset) offset="${args[i + 1]:-}" ;;
    --account-offset=*) offset="${args[i]#*=}" ;;
    --password) password="${args[i + 1]:-}" ;;
    --password=*) password="${args[i]#*=}" ;;
    --auth-port) auth_port="${args[i + 1]:-}" ;;
    --auth-port=*) auth_port="${args[i]#*=}" ;;
    --output) output="${args[i + 1]:-}" ;;
    --output=*) output="${args[i]#*=}" ;;
    -h | --help)
      dotnet run -c Release --project "$PROJECT" -- run --help
      exit 0
      ;;
  esac
done

[[ "$bots" =~ ^[0-9]+$ ]] || fail "--bots must be a number."

accounts="${LOADTEST_ACCOUNTS:-$bots}"
wait_seconds="${LOADTEST_WAIT:-300}"
settle_seconds="${LOADTEST_SETTLE:-15}"

if [[ -z "$output" ]]; then
  output="TestResults/loadtest/$(date +%Y%m%d-%H%M%S)"
fi

up_args=(up -d)

if [[ "${LOADTEST_NO_BUILD:-0}" != 1 ]]; then
  up_args+=(--build)
fi

echo "loadtest: building the bot"
dotnet build "$PROJECT" -c Release --nologo -v quiet

echo "loadtest: starting the servers"
"${COMPOSE[@]}" "${up_args[@]}"

if [[ "${LOADTEST_DOWN:-0}" == 1 ]]; then
  trap '"${COMPOSE[@]}" down' EXIT
fi

# Until the health checks of the servers exist, a server is ready when its port accepts a connection.
wait_for_port() {
  local name=$1 port=$2 deadline=$((SECONDS + wait_seconds))

  until (exec 3<> "/dev/tcp/127.0.0.1/$port") 2> /dev/null; do
    if ((SECONDS >= deadline)); then
      "${COMPOSE[@]}" ps --all >&2 || true
      fail "$name did not accept connections on port $port within ${wait_seconds} s. See: docker compose logs"
    fi

    sleep 2
  done

  echo "loadtest: $name is up on port $port"
}

wait_for_port Postgres 5432
wait_for_port Redis 6379
wait_for_port "auth server" "$auth_port"
wait_for_port "gate server" 10010
wait_for_port "district Rocco Town" 10011
wait_for_port "district Candus City" 10012
wait_for_port "district Grace City" 10013
wait_for_port "district Ruin Fortress" 10014
wait_for_port "maze server" 10101

# A container that started and died has its port closed again, so look at the state too.
exited=$("${COMPOSE[@]}" ps --all --status exited --format '{{.Name}}' | grep -v -e UpdateContent -e RelayServer || true)

if [[ -n "$exited" ]]; then
  fail "these containers have stopped: $(echo "$exited" | tr '\n' ' '). See: docker compose logs"
fi

echo "loadtest: waiting ${settle_seconds} s for the districts to register their channels"
sleep "$settle_seconds"

echo "loadtest: creating $accounts accounts ($prefix, from $offset)"
dotnet "$DLL" seed --account-prefix "$prefix" --account-offset "$offset" --count "$accounts" --password "$password"

echo "loadtest: running the bots, the report goes to $output"
mkdir -p "$output"

status=0

dotnet "$DLL" run \
  --auth-port "$auth_port" \
  --docker-stats \
  --output "$output" \
  "$@" || status=$?

echo "loadtest: report: $output/report.md and $output/report.json"

exit "$status"
