#!/bin/bash
set -euo pipefail

REGISTRY="${REGISTRY_ENDPOINT:-ghcr.io}"
REPOSITORY="${REGISTRY_REPOSITORY:?REGISTRY_REPOSITORY is not set}"
SOURCE_TAG="${SOURCE_TAG:?SOURCE_TAG is not set}"
TARGET_TAGS="${TARGET_TAGS:?TARGET_TAGS is not set}"
SKIP_PULL="${SKIP_PULL:-false}"

# Comma-separated list of image names. Defaults to api,migrator,web.
IMAGES="${IMAGES:-api,migrator,web}"

PREFIX="${REGISTRY}/${REPOSITORY}"
IFS=',' read -ra IMAGE_LIST <<< "$IMAGES"
IFS=',' read -ra TAGS <<< "$TARGET_TAGS"

for image in "${IMAGE_LIST[@]}"; do
    # Trim leading/trailing whitespace using bash parameter expansion
    image="${image#"${image%%[![:space:]]*}"}"
    image="${image%"${image##*[![:space:]]}"}"
    source="${PREFIX}/${image}:${SOURCE_TAG}"

    if [ "$SKIP_PULL" != "true" ]; then
        echo "Pulling ${source}..."
        docker pull "$source"
    else
        docker inspect "$source" > /dev/null 2>&1 || { echo "Source image not found locally: $source" >&2; exit 1; }
    fi

    for tag in "${TAGS[@]}"; do
        # Trim leading/trailing whitespace
        tag="${tag#"${tag%%[![:space:]]*}"}"
        tag="${tag%"${tag##*[![:space:]]}"}"
        target="${PREFIX}/${image}:${tag}"
        echo "Tagging ${source} -> ${target}..."
        docker tag "$source" "$target"
        echo "Pushing ${target}..."
        docker push "$target"
    done
done
