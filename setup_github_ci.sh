#!/bin/bash
# GitHub + CI/CD Setup for Arcanepunk
# Run this INSIDE the project folder after setup_unity_project.sh

set -e

REPO_NAME="arcanepunk-lifesim"
GITHUB_USER=""  # SET THIS: your GitHub username

echo "🚀 Setting up GitHub repo + CI/CD..."

# 1. Init git
git init
git add .
git commit -m "Initial commit: Arcanepunk MVP data + systems"

# 2. Create GitHub repo (needs gh CLI: https://cli.github.com)
if command -v gh &> /dev/null; then
    echo "📦 Creating GitHub repo..."
    gh repo create "$GITHUB_USER/$REPO_NAME" --public --source=. --push
else
    echo "⚠️  GitHub CLI (gh) not installed."
    echo "   Install: https://cli.github.com/"
    echo "   Then run manually:"
    echo "   gh repo create $GITHUB_USER/$REPO_NAME --public --source=. --push"
fi

# 3. Add UNITY_LICENSE secret (REQUIRED for CI)
echo ""
echo "🔑 CRITICAL: Add UNITY_LICENSE secret to GitHub repo:"
echo "   1. Go to: https://github.com/$GITHUB_USER/$REPO_NAME/settings/secrets/actions"
echo "   2. New repository secret → Name: UNITY_LICENSE"
echo "   3. Value: your Unity Personal license (from Unity Hub → Account → License)"
echo "   4. Save"
echo ""
echo "   Get license: Unity Hub → Gear icon → License Management → Copy license key"
echo ""

# 4. Push trigger
echo "📤 Pushing to trigger first build..."
git push -u origin main

echo ""
echo "✅ Done! Check GitHub Actions tab for build status."
echo "   APK will be in Actions → Build Android APK → Artifacts"