#!/bin/bash
set -euo pipefail

OWNER="${GITHUB_REPOSITORY_OWNER:?GITHUB_REPOSITORY_OWNER is not set}"
OWNER_TYPE="${GITHUB_OWNER_TYPE:-user}"
PACKAGE="${1:?Usage: $0 <package> [keepCount]}"
KEEP_COUNT="${2:-20}"
TOKEN="${GITHUB_TOKEN:?GITHUB_TOKEN is not set}"

# URL-encode the package name for the GitHub Packages API
urlencode() {
    local string="$1"
    local encoded=""
    local pos c o
    for (( pos=0; pos<${#string}; pos++ )); do
        c="${string:$pos:1}"
        case "$c" in
            [-_.~a-zA-Z0-9])
                encoded+="$c"
                ;;
            *)
                printf -v o '%%%02x' "'$c"
                encoded+="$o"
                ;;
        esac
    done
    echo "$encoded"
}

ENCODED_PACKAGE=$(urlencode "$PACKAGE")
BASE_URL="https://api.github.com/${OWNER_TYPE}/${OWNER}/packages/container/${ENCODED_PACKAGE}/versions"

# Collect all versions across pages
ALL_VERSIONS="[]"
PAGE_URL="${BASE_URL}?per_page=100"
HEADERS_FILE=$(mktemp)
trap 'rm -f "$HEADERS_FILE"' EXIT

echo "Fetching versions for ${OWNER_TYPE}/${OWNER}/${PACKAGE}..."

while [ -n "$PAGE_URL" ]; do
    > "$HEADERS_FILE"
    # Hide token from set -x traces
    { HTTP_BODY=$(curl -sSL \
        -H "Authorization: token ${TOKEN}" \
        -H "Accept: application/vnd.github+json" \
        -D "$HEADERS_FILE" \
        "$PAGE_URL"); } 2>/dev/null

    ALL_VERSIONS=$(echo "$ALL_VERSIONS $HTTP_BODY" | jq -s 'add')

    # Extract next page URL from Link header
    NEXT_URL=$(grep -i '^link:' "$HEADERS_FILE" 2>/dev/null | sed -n 's/.*<\([^>]*\)>; *rel="next".*/\1/p' || true)
    PAGE_URL="${NEXT_URL:-}"
done

# Protect: latest, dev-latest, and any semver tag (v1.0.0, v2, etc.).
# Exclude any version that has at least one protected tag.
# Untagged versions (orphaned manifests) are treated as deletable.
NAMED_COUNT=$(echo "$ALL_VERSIONS" | jq '[.[] | select([.metadata.container.tags[]? | test("^(latest|dev-latest|v[0-9])")] | any)] | length')
SHA_ONLY=$(echo "$ALL_VERSIONS" | jq '[.[] | select(
  (.metadata.container.tags | length) == 0 or
  ([.metadata.container.tags[]? | test("^(latest|dev-latest|v[0-9])")] | any | not)
)] | sort_by(.created_at) | reverse')

TOTAL_SHA=$(echo "$SHA_ONLY" | jq 'length')
TO_DELETE=$(echo "$SHA_ONLY" | jq ".[${KEEP_COUNT}:]")
DELETE_COUNT=$(echo "$TO_DELETE" | jq 'length')

echo "Named tag versions (protected): ${NAMED_COUNT}"
echo "SHA-only versions: ${TOTAL_SHA}"
echo "Will delete: ${DELETE_COUNT}"

FAILED_IDS=""

while read -r version; do
    ID=$(echo "$version" | jq '.id')
    TAGS=$(echo "$version" | jq -r '[.metadata.container.tags[]?] | join(", ")')
    CREATED=$(echo "$version" | jq -r '.created_at')
    echo "  Deleting version ${ID} (tags: [${TAGS}], created: ${CREATED})"

    DELETE_URL="${BASE_URL}/${ID}"
    STATUS=$( { curl -sSL -o /dev/null -w "%{http_code}" \
        -X DELETE \
        -H "Authorization: token ${TOKEN}" \
        -H "Accept: application/vnd.github+json" \
        "$DELETE_URL"; } 2>/dev/null)

    if [ "$STATUS" = "204" ] || [ "$STATUS" = "404" ]; then
        echo "    -> OK"
    else
        echo "    -> FAILED (${STATUS})"
        FAILED_IDS="${FAILED_IDS} ${ID}"
    fi
done < <(echo "$TO_DELETE" | jq -c '.[]')

if [ -n "$FAILED_IDS" ]; then
    echo "Failed to delete versions:${FAILED_IDS}" >&2
    exit 1
fi
