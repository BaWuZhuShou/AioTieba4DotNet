#!/usr/bin/env bash

set -euo pipefail

validation_failed_message="Artifact validation failed."

artifact_dir="${1:-}"
expected_stem="${2:-}"

if [[ -z "$artifact_dir" || -z "$expected_stem" ]]; then
    echo "$validation_failed_message"
    echo "Expected arguments: <artifact-dir> <expected-package-stem>."
    echo "Example: bash ./.github/scripts/validate-package-artifacts.sh ./nupkg AioTieba4DotNet.2.0.0"
    exit 1
fi

if [[ ! -d "$artifact_dir" ]]; then
    echo "$validation_failed_message"
    echo "Expected artifact directory: '$artifact_dir'"
    echo "Found: directory does not exist."
    exit 1
fi

shopt -s nullglob

main_packages=("$artifact_dir"/*.nupkg)
symbol_packages=("$artifact_dir"/*.snupkg)

expected_main="${expected_stem}.nupkg"
expected_symbol="${expected_stem}.snupkg"

print_found_list() {
    local label="$1"
    shift

    if (( $# == 0 )); then
        printf '  %s: (none)\n' "$label"
        return
    fi

    printf '  %s:\n' "$label"
    local item
    for item in "$@"; do
        printf '    - %s\n' "$(basename "$item")"
    done
}

if (( ${#main_packages[@]} != 1 || ${#symbol_packages[@]} != 1 )); then
    echo "$validation_failed_message"
    echo "Expected exactly one main package '$expected_main' and one symbol package '$expected_symbol' in '$artifact_dir'."
    echo "Found counts: main=${#main_packages[@]}, symbols=${#symbol_packages[@]}"
    print_found_list "main packages" "${main_packages[@]}"
    print_found_list "symbol packages" "${symbol_packages[@]}"
    exit 1
fi

actual_main="$(basename "${main_packages[0]}")"
actual_symbol="$(basename "${symbol_packages[0]}")"

if [[ "$actual_main" != "$expected_main" || "$actual_symbol" != "$expected_symbol" ]]; then
    echo "$validation_failed_message"
    echo "Expected main package:   $expected_main"
    echo "Expected symbol package: $expected_symbol"
    echo "Found main package:      $actual_main"
    echo "Found symbol package:    $actual_symbol"
    exit 1
fi

echo "Artifact validation passed."
echo "Validated main package:   $actual_main"
echo "Validated symbol package: $actual_symbol"
