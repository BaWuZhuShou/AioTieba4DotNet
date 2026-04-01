#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 1 ]; then
  echo "usage: ./scripts/test-lane.sh <deterministic|integration|live|sequence-dry-run> [stage ...]"
  exit 1
fi

lane="$1"
shift || true
repo_root="$(cd -- "$(dirname -- "$0")/.." && pwd)"
manifest_path="$repo_root/AioTieba4DotNet.Testing/test-sequencing.manifest.json"
deterministic_project="$repo_root/AioTieba4DotNet.Tests.Deterministic/AioTieba4DotNet.Tests.Deterministic.csproj"
integration_project="$repo_root/AioTieba4DotNet.Tests.Integration/AioTieba4DotNet.Tests.Integration.csproj"
live_project="$repo_root/AioTieba4DotNet.Tests.Live/AioTieba4DotNet.Tests.Live.csproj"

run_staged_lane() {
  local lane_name="$1"
  local project_path="$2"
  local lane_category="$3"
  shift 3

  python - "$manifest_path" "$lane_name" "$lane_category" "$@" <<'PY' | while IFS='|' read -r stage_name stage_filter is_cleanup; do
import json
import sys

with open(sys.argv[1], encoding="utf-8") as handle:
    manifest = json.load(handle)

lane_name = sys.argv[2]
lane_category = sys.argv[3]
requested = []
seen = set()
for raw_stage in sys.argv[4:]:
    for stage in raw_stage.split(","):
        stage = stage.strip()
        if not stage or stage in seen:
            continue
        seen.add(stage)
        requested.append(stage)

known = [stage["name"] for stage in manifest["stages"]]
unknown = [stage for stage in requested if stage not in known]
if unknown:
    raise SystemExit(f"Unknown stage filter(s): {', '.join(unknown)}")

lane_stages = [stage for stage in manifest["stages"] if lane_name in stage["lanes"]]
lane_stage_names = {stage["name"] for stage in lane_stages}
out_of_lane = [stage for stage in requested if stage not in lane_stage_names]
if out_of_lane:
    raise SystemExit(f"Stage filter(s) are not available for lane '{lane_name}': {', '.join(out_of_lane)}")

for stage in manifest["stages"]:
    if lane_name not in stage["lanes"]:
        continue
    if requested and stage["name"] not in requested:
        continue

    is_cleanup = stage["name"] == "Cleanup"
    stage_filter = "" if is_cleanup else f"TestCategory={lane_category}&TestCategory={stage['name']}"
    print(f"{stage['name']}|{stage_filter}|{'true' if is_cleanup else 'false'}")
PY
    if [ "$is_cleanup" = "true" ]; then
      printf '[%s] %s -> cleanup compensations / recorded object ledger\n' "$lane_name" "$stage_name"
      continue
    fi

    printf '[%s] %s -> %s\n' "$lane_name" "$stage_name" "$stage_filter"
    dotnet test "$project_path" --configuration Release --nologo --filter "$stage_filter" -p:CollectCoverage=false
  done
}

case "$lane" in
  deterministic)
    dotnet test "$deterministic_project" --configuration Release --nologo "/p:CollectCoverage=true"
    ;;
  integration)
    run_staged_lane integration "$integration_project" Integration "$@"
    ;;
  live)
    run_staged_lane live "$live_project" Live "$@"
    ;;
  sequence-dry-run)
    python - "$manifest_path" "$@" <<'PY'
import json
import sys

with open(sys.argv[1], encoding="utf-8") as handle:
    manifest = json.load(handle)

requested = []
seen = set()
for raw_stage in sys.argv[2:]:
    for stage in raw_stage.split(","):
        stage = stage.strip()
        if not stage or stage in seen:
            continue
        seen.add(stage)
        requested.append(stage)

known = [stage["name"] for stage in manifest["stages"]]
unknown = [stage for stage in requested if stage not in known]
if unknown:
    raise SystemExit(f"Unknown stage filter(s): {', '.join(unknown)}")

stages = [stage for stage in manifest["stages"] if not requested or stage["name"] in requested]

for index, stage in enumerate(stages, start=1):
    lanes = ", ".join(stage["lanes"])
    print(f"{index}. {stage['name']} [{lanes}] - {stage['description']}")
    if "integration" in stage["lanes"]:
        print(f"    integration => TestCategory=Integration&TestCategory={stage['name']}")
    if "live" in stage["lanes"]:
        if stage["name"] == "Cleanup":
            print("    live => cleanup compensations / recorded object ledger")
        else:
            print(f"    live => TestCategory=Live&TestCategory={stage['name']}")
PY
    ;;
  *)
    echo "unknown lane: $lane"
    exit 1
    ;;
esac
