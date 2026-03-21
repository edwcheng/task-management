#!/bin/bash
# Git validation script - run before git operations
# Location: /home/z/my-project/task-management/validate-git.sh

CORRECT_ROOT="/home/z/my-project/task-management"
CORRECT_BRANCH="main"

current_dir=$(pwd)
current_branch=$(git branch --show-current 2>/dev/null)

echo "=== Git Validation ==="
echo "Current directory: $current_dir"
echo "Current branch: $current_branch"
echo ""

if [ "$current_dir" != "$CORRECT_ROOT" ]; then
    echo "❌ ERROR: Wrong directory!"
    echo "   Expected: $CORRECT_ROOT"
    echo "   Run: cd $CORRECT_ROOT"
    exit 1
fi

if [ "$current_branch" != "$CORRECT_BRANCH" ]; then
    echo "⚠️  WARNING: Not on $CORRECT_BRANCH branch!"
    echo "   Run: git checkout $CORRECT_BRANCH"
    exit 1
fi

echo "✅ Git validation passed"
echo ""
