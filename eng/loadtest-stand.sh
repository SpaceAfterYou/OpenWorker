#!/usr/bin/env bash
# Runs the load test on a stack of its own that shares nothing with the compose stack of development: plain `timeout 120 docker run`
# containers named owlt<n>-*, a network of their own, and every port moved up by n * 1000. It is for a machine where the
# compose stack (OpenWorker-*, ports 6379, 5432, 10000 to 10101 and others) is in use, or where `docker compose` cannot
# build. eng/loadtest.sh is the one command when the machine is free.
#
#   LOADTEST_STAND=21 SOULWORKER_DATAS=/mnt/games/soulworker/_datas eng/loadtest-stand.sh --bots 1000 --ramp-up 50 \
#     --duration 60s --warmup 10s --move-rate 5 --maze-share 0.2
#
# Everything after the script name goes to `OpenWorker.LoadTest run`; the script adds --auth-port, --docker-stats,
# --docker-names, --prometheus and --output in front, so the same options given by hand win.
#
# What runs is what loadtest.sh runs, without the images: the Release build of each server (bin/Release/net10.0) in the
# ASP.NET image, with the client tables, maps, scripts, action files and navigation meshes of SOULWORKER_DATAS mounted
# the way compose.yaml mounts them, and the settings of eng/loadtest/compose.loadtest.yaml: Warning logs, no packet
# dumps, twenty channels per district. One relay, one auth, one gate, four districts (Rocco Town takes the bots), one
# maze server, Redis, Postgres with 200 connections and Prometheus, which the servers push their metrics to.
#
# LOADTEST_STAND      Required, from 1 to 22: the number n. Ports: auth 10000 + n * 1000, gate +10, districts +11 to +14, maze
#                     +101, Postgres 15432 + n * 1000, Redis 16399 + n * 1000, Prometheus 19090 + n * 1000.
# SOULWORKER_DATAS    Required, as for loadtest.sh: the `datas` folder of a client, or its unpacked copy.
# LOADTEST_ACCOUNTS   Accounts to create. Default: the number of --bots, counted from --account-offset.
# LOADTEST_CHANNELS   Channels of a district. Default 20.
# LOADTEST_KEEP       Set to 1 to leave the containers running at the end. By default they are removed.
# LOADTEST_GATE_POOL  Maximum Pool Size of the gate's database connections; unset keeps the 16 of its settings.
# LOADTEST_LOG_LEVEL  Log level of the servers' own messages (Debug to see what they answer a bot). Default Warning.
# LOADTEST_SETTLE     Seconds to wait after the accounts are made, for the districts to register their channels. Default 15.

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

fail() {
  echo "loadtest-stand: $*" >&2
  exit 1
}

[[ "${LOADTEST_STAND:-}" =~ ^[1-9][0-9]*$ ]] || fail "LOADTEST_STAND must be a number from 1: it moves every port by n * 1000, so that nothing of another stack is touched."
# The client keeps a port in a signed 16-bit number, so the servers refuse a public port above 32767: the maze, at 10101 + n * 1000, sets the limit.
((LOADTEST_STAND <= 22)) || fail "LOADTEST_STAND must be at most 22: the ports of the servers have to stay at or below 32767."
[[ -n "${SOULWORKER_DATAS:-}" ]] || fail "SOULWORKER_DATAS is not set. Point it at the 'datas' folder of a client."

for folder in bin/Table World/Table World/Navmesh Character/ActionData Scripts/Server; do
  [[ -d "$SOULWORKER_DATAS/$folder" ]] || fail "SOULWORKER_DATAS=$SOULWORKER_DATAS has no folder '$folder'."
done

command -v docker > /dev/null || fail "docker is not installed or not on the PATH."
command -v dotnet > /dev/null || fail "dotnet is not installed or not on the PATH."

N=$LOADTEST_STAND
OFF=$((N * 1000))
P="owlt$N-"
NET="owlt$N"
IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0
CHANNELS="${LOADTEST_CHANNELS:-20}"
SETTLE="${LOADTEST_SETTLE:-15}"
ROOT=$PWD

AUTH_PORT=$((10000 + OFF))
GATE_PORT=$((10010 + OFF))
MAZE_PORT=$((10101 + OFF))
PG_PORT=$((15432 + OFF))
REDIS_PORT=$((16399 + OFF))
PROM_PORT=$((19090 + OFF))

# Only the containers named owlt<n>-* are removed: the prefix belongs to this script and to nothing else.
stop_stack() {
  timeout 15 docker ps -a --filter "name=^${P}" --format '{{.Names}}' | xargs -r timeout 30 docker rm -f > /dev/null
  timeout 30 docker network rm "$NET" > /dev/null 2>&1 || true
}

stop_stack

if [[ "${LOADTEST_KEEP:-0}" != 1 ]]; then
  trap stop_stack EXIT
fi

# The bot is built in Release as loadtest.sh does; the servers are built in Release because the Debug build saves every packet
# it receives to a file under a lock and logs a line for each packet that is not a move.
echo "loadtest-stand: building the servers and the bot (Release)"
for project in OpenWorker.RelayServer OpenWorker.AuthServer OpenWorker.GateServer OpenWorker.DistrictServer OpenWorker.MazeServer OpenWorker.LoadTest; do
  dotnet build "$project/$project.csproj" -c Release --nologo -v quiet
done

DLL=OpenWorker.LoadTest/bin/Release/net10.0/OpenWorker.LoadTest.dll

echo "loadtest-stand: starting the stack $P (auth $AUTH_PORT, gate $GATE_PORT, maze $MAZE_PORT)"
timeout 30 docker network create "$NET" > /dev/null

timeout 120 docker run -d --rm --name "${P}redis" --network "$NET" --network-alias redis -p "127.0.0.1:$REDIS_PORT:6379" docker.io/redis/redis-stack-server:latest > /dev/null
timeout 120 docker run -d --rm --name "${P}pg" --network "$NET" --network-alias postgres -p "127.0.0.1:$PG_PORT:5432" \
  -e POSTGRES_HOST_AUTH_METHOD=trust docker.io/library/postgres:alpine postgres -c max_connections=200 > /dev/null
timeout 120 docker run -d --rm --name "${P}prometheus" --network "$NET" --network-alias prometheus -p "127.0.0.1:$PROM_PORT:9090" \
  -v "$ROOT/eng/observability/prometheus/prometheus.yml":/etc/prometheus/prometheus.yml:ro \
  docker.io/prom/prometheus:v3.15.1 --config.file=/etc/prometheus/prometheus.yml --web.enable-otlp-receiver --storage.tsdb.path=/prometheus > /dev/null

for _ in $(seq 1 30); do
  timeout 30 docker exec "${P}pg" pg_isready -U postgres > /dev/null 2>&1 && break
  sleep 1
done

# Whether a container has logged that its server started. grep reads the whole log (-c, not -q): with pipefail, a grep that stops at
# the first match makes `docker logs` fail on the closed pipe, and the line would never count as seen.
started() {
  local count
  count=$(timeout 15 docker logs "$1" 2>&1 | grep -c 'Application started' || true)
  ((count > 0))
}

# A server: run NAME PROJECT [extra docker arguments]. The build output is the content root (its appsettings.json); the working
# directory /app holds the client data, where the servers look for it.
DATAS=$SOULWORKER_DATAS
run() {
  local name=$1 project=$2
  shift 2

  timeout 120 docker run -d --name "$name" --network "$NET" \
    -v "$ROOT/$project/bin/Release/net10.0":/srv:ro \
    -v "$DATAS/bin/Table":/app/res:ro -v "$DATAS/World/Table":/app/batch:ro -v "$DATAS/Scripts/Server":/app/scripts:ro \
    -v "$DATAS/Character/ActionData":/app/action:ro -v "$DATAS/World/Navmesh":/app/navmesh:ro \
    -v "$SERVER_RES":/app/res-server:ro \
    -w /app -e DOTNET_CONTENTROOT=/srv -e Game__Version=1.4.2.3 \
    -e Logging__LogLevel__Default=Warning -e Logging__LogLevel__OpenWorker.DependencyInjection.RuntimeSettingsLogService=Information \
    -e Logging__LogLevel__Microsoft.Hosting.Lifetime=Information -e "Logging__LogLevel__OpenWorker=${LOADTEST_LOG_LEVEL:-Warning}" \
    -e Hotspot__PacketDumpPath= \
    "$@" --entrypoint dotnet "$IMAGE" "/srv/$project.dll" > /dev/null
}

# The tables only the server has (the folder res-server of the container): as in compose.yaml, the client's own tables
# when SOULWORKER_SERVER_RES is not set.
SERVER_RES="${SOULWORKER_SERVER_RES:-$DATAS/bin/Table}"

# The relay migrates the database and makes the Redis indexes the others search by; the others start when it has started.
run "${P}relay" OpenWorker.RelayServer -e Credentials__DiscordToken=loadtest-placeholder

relay_started=0
for _ in $(seq 1 120); do
  started "${P}relay" && { relay_started=1; break; }
  [[ "$(timeout 15 docker inspect -f '{{.State.Status}}' "${P}relay")" == running ]] || fail "${P}relay has stopped: docker logs ${P}relay"
  sleep 1
done
((relay_started)) || fail "${P}relay did not start within 120 s: the other servers refuse a database it has not migrated."

run "${P}auth" OpenWorker.AuthServer -p "127.0.0.1:$AUTH_PORT:10000"
# The bots walk a new character straight into a district: the gate does not send it through the prologue maze first.
# LOADTEST_GATE_POOL gives the gate a larger pool of database connections than its appsettings.json (16).
gate_pool=()
[[ -n "${LOADTEST_GATE_POOL:-}" ]] && gate_pool=(-e "ConnectionStrings__Postgres=Host=postgres;Port=5432;Database=openworker;Username=postgres;Maximum Pool Size=$LOADTEST_GATE_POOL")
run "${P}gate" OpenWorker.GateServer -p "127.0.0.1:$GATE_PORT:10000" -e Gate__Id=1 -e "Gate__Port=$GATE_PORT" -e Gate__Host=127.0.0.1 \
  -e Game__Prologue=false "${gate_pool[@]}"
run "${P}maze" OpenWorker.MazeServer -p "127.0.0.1:$MAZE_PORT:10000" -e Gate__Id=1 -e "Maze__Port=$MAZE_PORT"

# Rocco Town takes the bots; the other districts are as compose.yaml starts them. The test commands of the chat are open to every
# account: a bot that goes to a maze raises its character and records the maze before it as cleared (/exp, /maze-clear) first.
district=0
for entry in roccotown:10003 canduscity:10021 gracecity:10031 ruinfortress:10041; do
  district=$((district + 1))
  run "${P}district-${entry%%:*}" OpenWorker.DistrictServer -p "127.0.0.1:$((10010 + OFF + district)):10000" \
    -e Gate__Id=1 -e Channel__From=1 -e "Channel__To=$CHANNELS" -e "Instance__Location=${entry##*:}" \
    -e "Instance__PublicPort=$((10010 + OFF + district))" -e "Maze__Port=$MAZE_PORT" -e Commands__OpenToEveryone=true
done

servers=(auth gate maze district-roccotown district-canduscity district-gracecity district-ruinfortress)

echo "loadtest-stand: waiting for the servers to start"
deadline=$((SECONDS + ${LOADTEST_WAIT:-300}))

for server in "${servers[@]}"; do
  until started "${P}${server}"; do
    [[ "$(timeout 15 docker inspect -f '{{.State.Status}}' "${P}${server}")" == running ]] || fail "${P}${server} has stopped: timeout 15 docker logs ${P}${server}"
    ((SECONDS < deadline)) || fail "${P}${server} did not start within the wait."
    sleep 1
  done
done

bots=100
prefix=lt
offset=0
password=loadtest
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
    --output) output="${args[i + 1]:-}" ;;
    --output=*) output="${args[i]#*=}" ;;
  esac
done

[[ "$bots" =~ ^[0-9]+$ ]] || fail "--bots must be a number."

accounts="${LOADTEST_ACCOUNTS:-$bots}"

if [[ -z "$output" ]]; then
  output="TestResults/loadtest/$(date +%Y%m%d-%H%M%S)"
fi

echo "loadtest-stand: creating $accounts accounts ($prefix, from $offset)"
dotnet "$DLL" seed --postgres "Host=localhost;Port=$PG_PORT;Database=openworker;Username=postgres" \
  --account-prefix "$prefix" --account-offset "$offset" --count "$accounts" --password "$password"

echo "loadtest-stand: waiting ${SETTLE} s for the districts to register their channels and the auth server to list the gate"
sleep "$SETTLE"

names="${P}auth,${P}gate,${P}maze,${P}district-roccotown,${P}district-canduscity,${P}district-gracecity,${P}district-ruinfortress,${P}relay,${P}pg,${P}redis,${P}prometheus"

mkdir -p "$output"

echo "loadtest-stand: running the bots, the report goes to $output"
status=0

dotnet "$DLL" run \
  --auth-port "$AUTH_PORT" \
  --docker-stats --docker-names "$names" \
  --prometheus "http://127.0.0.1:$PROM_PORT" \
  --output "$output" \
  "$@" || status=$?

echo "loadtest-stand: report: $output/report.md and $output/report.json"

# The servers push their metrics every 10 s: wait for the last push, then see which handlers took the time.
if command -v python3 > /dev/null; then
  sleep 12
  echo "loadtest-stand: the heaviest handlers of each server (also in $output/handlers.txt)"
  python3 -I eng/loadtest/handlers.py "http://127.0.0.1:$PROM_PORT" 1800 12 | tee "$output/handlers.txt" || true
fi

exit "$status"
