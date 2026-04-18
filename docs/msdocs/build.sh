#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Ensure .NET global tools are on PATH
export PATH="$PATH:$HOME/.dotnet/tools"

# Check if docfx is available
if ! command -v docfx &>/dev/null; then
    echo "docfx not found. Installing as a .NET global tool..."
    dotnet tool install -g docfx
fi

# Clean previous output to avoid stale file warnings
rm -rf "$SCRIPT_DIR/_site"

echo "Building docs..."
docfx build "$SCRIPT_DIR/docfx.json" --output "$SCRIPT_DIR/_site"

echo ""
echo "Build complete. Output is in: $SCRIPT_DIR/_site"
echo "To preview locally, run: docfx serve $SCRIPT_DIR/_site"
