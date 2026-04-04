#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 1 ]; then
  echo "usage: ./scripts/test-lane.sh <safe|restricted|sequence-dry-run>"
  exit 1
fi

lane="$1"
repo_root="$(cd -- "$(dirname -- "$0")/.." && pwd)"
suite_project="$repo_root/AioTieba4DotNet.Tests.Online.Suite/AioTieba4DotNet.Tests.Online.Suite.csproj"

case "$lane" in
  safe)
    printf '[safe] ordered suite host -> TestCategory=Suite:SafeOrdered\n'
    dotnet test "$suite_project" --configuration Release --nologo --filter "TestCategory=Suite:SafeOrdered" -p:CollectCoverage=false
    ;;
  restricted)
    printf '[restricted] ordered suite host -> TestCategory=Suite:RestrictedOrdered\n'
    dotnet test "$suite_project" --configuration Release --nologo --filter "TestCategory=Suite:RestrictedOrdered" -p:CollectCoverage=false
    ;;
  sequence-dry-run)
    printf '1. SafeOrdered [default]\n'
    printf '    dotnet test AioTieba4DotNet.Tests.Online.Suite/AioTieba4DotNet.Tests.Online.Suite.csproj --configuration Release --nologo --filter TestCategory=Suite:SafeOrdered -p:CollectCoverage=false\n'
    printf '2. RestrictedOrdered [explicit opt-in]\n'
    printf '    dotnet test AioTieba4DotNet.Tests.Online.Suite/AioTieba4DotNet.Tests.Online.Suite.csproj --configuration Release --nologo --filter TestCategory=Suite:RestrictedOrdered -p:CollectCoverage=false\n'
    ;;
  *)
    echo "unknown lane: $lane"
    exit 1
    ;;
esac
