#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd -- "$script_dir/.." && pwd)"
slug= channel= build_path=
while IFS='=' read -r key value; do
  value="${value%$'\r'}"
  case "$key" in slug) slug="$value";; channel) channel="$value";; build_path) build_path="$value";; esac
done < "$script_dir/config.txt"
: "${slug:?Missing slug}" "${channel:?Missing channel}" "${build_path:?Missing build_path}"
build="$root/$build_path"
[[ -f "$build/index.html" ]] || { echo "Build WebGL first: index.html missing in $build" >&2; exit 1; }
if [[ "${1:-}" == '--dry-run' ]]; then printf 'butler push %q %q\n' "$build" "$slug:$channel"; exit 0; fi
command -v butler >/dev/null || { echo 'butler is not in PATH' >&2; exit 1; }
butler push "$build" "$slug:$channel"
printf 'https://%s.itch.io/%s\n' "${slug%%/*}" "${slug#*/}"
