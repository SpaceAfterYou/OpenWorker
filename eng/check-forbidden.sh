#!/usr/bin/env bash
# Fails when product code uses a pattern the project forbids. CI runs it on every push and pull request;
# run it before you push: eng/check-forbidden.sh
#
# Only product projects are checked: the test projects (*.Test), obj/, bin/ and the worktrees of agents (.claude/) are skipped.
# To add a rule, add a line to RULES: "regex<TAB>why". The regex is an extended regular expression.

set -u

cd "$(dirname "${BASH_SOURCE[0]}")/.." || exit 2

RULES=(
  $'Task\\.Factory\\.StartNew\tAn async lambda in StartNew returns Task<Task> and its failure goes unobserved. Write an async Task ExecuteAsync with a PeriodicTimer (REFACTORING.md, principle 6).'
  $'\\bConsole\\.(Write|WriteLine|Error|Out)\\b\tProduct code logs through ILogger (REFACTORING.md, principle 14).'
  $'using +static +System\\.Console\\b\tProduct code logs through ILogger (REFACTORING.md, principle 14).'
)

status=0

for rule in "${RULES[@]}"; do
  pattern="${rule%%$'\t'*}"
  reason="${rule#*$'\t'}"

  matches=$(grep -rnE "$pattern" . \
    --include='*.cs' \
    --exclude-dir=obj --exclude-dir=bin --exclude-dir=.git --exclude-dir=.claude --exclude-dir='*.Test' \
    | sed 's|^\./||')

  if [[ -n "$matches" ]]; then
    status=1
    echo "Forbidden: $pattern"
    echo "  $reason"
    echo "$matches" | sed 's/^/  /'
    echo
  fi
done

if [[ $status -eq 0 ]]; then
  echo "No forbidden patterns."
fi

exit $status
