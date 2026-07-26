#!/usr/bin/env sh
set -eu

REPO_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$REPO_ROOT"

if ! command -v node >/dev/null 2>&1; then
  echo "ERROR: Node.js 18 or newer is required." >&2
  exit 1
fi

node scripts/harness/verify.mjs "$@"

echo
echo "Harness baseline is healthy."
echo "Read feature_list.json and select at most one in-progress feature."
