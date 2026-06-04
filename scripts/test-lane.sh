#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 1 ]; then
  echo "usage: ./scripts/test-lane.sh <safe|restricted|sequence-dry-run>"
  exit 1
fi

lane="$1"

if [ "$#" -gt 1 ]; then
  echo "stage filters are no longer accepted by the governance ordered-suite wrapper. use the governance project filters directly when you need narrower execution."
  exit 1
fi

repo_root="$(cd -- "$(dirname -- "$0")/.." && pwd)"
governance_project="$repo_root/AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj"

case "$lane" in
  safe)
    printf '[safe] Governance ordered suite host -> TestCategory=Suite:SafeOrdered\n'
    dotnet test "$governance_project" --configuration Release --nologo --filter "TestCategory=Suite:SafeOrdered" -p:CollectCoverage=false
    ;;
  restricted)
    printf '[restricted] Governance ordered suite host -> TestCategory=Suite:RestrictedOrdered\n'
    dotnet test "$governance_project" --configuration Release --nologo --filter "TestCategory=Suite:RestrictedOrdered" -p:CollectCoverage=false
    ;;
  sequence-dry-run)
    printf '1. SafeOrdered [default]\n'
    printf '    dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter TestCategory=Suite:SafeOrdered -p:CollectCoverage=false\n'
    printf '2. RestrictedOrdered [explicit opt-in]\n'
    printf '    dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter TestCategory=Suite:RestrictedOrdered -p:CollectCoverage=false\n'
    ;;
  *)
    echo "unknown lane: $lane"
    exit 1
    ;;
esac
