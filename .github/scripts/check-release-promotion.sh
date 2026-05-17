#!/bin/bash
set -euo pipefail

OUTPUT_FILE="${GITHUB_OUTPUT:?GITHUB_OUTPUT is not set}"

# This script requires full git history. Detect shallow clones early.
if git rev-parse --is-shallow-repository 2>/dev/null | grep -q true; then
    echo "Error: shallow clone detected. fetch-depth: 0 is required." >&2
    exit 1
fi

if ! git fetch origin +refs/tags/candidate:refs/tags/candidate 2>/dev/null; then
    echo "Candidate tag does not exist. Will build fresh."
    echo "safe=false" >> "$OUTPUT_FILE"
    exit 0
fi

CANDIDATE_SHA=$(git rev-parse candidate)
HEAD_SHA=$(git rev-parse HEAD)

# Case 1: HEAD is exactly the candidate commit (fast-forward or direct tag)
if [ "$HEAD_SHA" = "$CANDIDATE_SHA" ]; then
    echo "Release commit matches candidate (${CANDIDATE_SHA})"
    echo "safe=true" >> "$OUTPUT_FILE"
    echo "promote_sha=${CANDIDATE_SHA}" >> "$OUTPUT_FILE"
    exit 0
fi

# Case 2: HEAD is a merge commit that merged candidate (dev → main merge).
# HEAD^2 resolves to the second parent (the dev branch tip).
# NOTE: This only works for true merge commits. If main is updated with
# squash merges or additional commits after the merge, this branch will NOT
# fire and the release will fall back to building fresh.
HEAD_CANDIDATE_PARENT=$(git rev-parse HEAD^2 2>/dev/null || echo "")

if [ "$HEAD_CANDIDATE_PARENT" = "$CANDIDATE_SHA" ]; then
    echo "Release is a merge of candidate (${CANDIDATE_SHA})"
    echo "safe=true" >> "$OUTPUT_FILE"
    echo "promote_sha=${CANDIDATE_SHA}" >> "$OUTPUT_FILE"
    exit 0
fi

# Case 3: Squash merge — candidate is an ancestor with exactly 1 commit ahead.
# This handles dev → main squash merges where all candidate changes are
# flattened into a single commit on main, with no extra commits on top.
if git merge-base --is-ancestor candidate HEAD 2>/dev/null; then
    AHEAD_COUNT=$(git rev-list --count candidate..HEAD)
    if [ "$AHEAD_COUNT" -eq 1 ]; then
        echo "Release is a squash merge of candidate (${CANDIDATE_SHA})"
        echo "safe=true" >> "$OUTPUT_FILE"
        echo "promote_sha=${CANDIDATE_SHA}" >> "$OUTPUT_FILE"
        exit 0
    else
        echo "Candidate is ancestor but ${AHEAD_COUNT} commits ahead (expected 1). Will build fresh."
    fi
fi

# Case 4: Anything else — build fresh
echo "Release commit does not match candidate. Will build fresh."
echo "safe=false" >> "$OUTPUT_FILE"
