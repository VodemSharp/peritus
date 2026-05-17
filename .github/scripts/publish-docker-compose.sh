#!/bin/bash
set -euo pipefail

APP_VERSION="${APP_VERSION:?APP_VERSION is not set}"
# OUTPUT_DIR can be overridden per-step (e.g. verify uses artifacts/compose-verify).
# Falls back to artifacts/compose when run standalone.
OUTPUT_DIR="${OUTPUT_DIR:-artifacts/compose}"
PROJECT_PATH="${APPHOST_PROJECT_PATH:?APPHOST_PROJECT_PATH is not set}"

if [ ! -f "$PROJECT_PATH" ]; then
    echo "AppHost project not found: $PROJECT_PATH" >&2
    exit 1
fi

echo "=== Generating Docker Compose artifacts ==="
echo "APP_VERSION: $APP_VERSION"
echo "Output directory: $OUTPUT_DIR"

TMP_DIR=$(mktemp -d)
trap 'rm -rf "$TMP_DIR"' EXIT

APP_VERSION="$APP_VERSION" aspire publish \
    --project "$PROJECT_PATH" \
    --output-path "$TMP_DIR" \
    --non-interactive

mv "$TMP_DIR" "${OUTPUT_DIR}.new"
trap 'rm -rf "${OUTPUT_DIR}.new"' EXIT
rm -rf "$OUTPUT_DIR"
mv "${OUTPUT_DIR}.new" "$OUTPUT_DIR"
trap - EXIT

echo "=== Generated files ==="
for file in "$OUTPUT_DIR"/*; do
    [ -e "$file" ] && echo "  $(basename "$file")"
done
