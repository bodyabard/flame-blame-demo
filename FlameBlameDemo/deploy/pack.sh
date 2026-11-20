#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd -- "$script_dir/.." && pwd)"
build_path=
while IFS='=' read -r key value; do
  case "$key" in build_path) build_path="${value%$'\r'}";; esac
done < "$script_dir/config.txt"
: "${build_path:?Missing build_path}"
build="$root/$build_path"
[[ -f "$build/index.html" ]] || { echo "Build WebGL first: index.html missing in $build" >&2; exit 1; }
command -v zip >/dev/null || { echo 'zip is required; on Windows use deploy/pack.ps1' >&2; exit 1; }
archive="$root/Builds/flameblame-demo-html5.zip"
temporary="$root/Builds/flameblame-demo-html5.tmp.zip"
[[ ! -e "$temporary" ]] || { echo "Remove stale temporary archive: $temporary" >&2; exit 1; }
(cd -- "$build" && zip -q -r "$temporary" .)
mv -f -- "$temporary" "$archive"
printf '%s\n' "$archive"
