#!/usr/bin/env bash
# lint.sh — .editorconfig 기준 포맷/스타일 검사 (CLAUDE.md §8-1).
#
# Usage:
#   ./tests/lint.sh          검사만 (변경 없음). 위반이 있으면 실패
#   ./tests/lint.sh --fix    실제로 고친다
#
# Exit:
#   0 통과 / 1 위반 있음 / 6 프로젝트 파일 없음 / 7 dotnet SDK 없음

set -uo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

if ! command -v dotnet >/dev/null 2>&1; then
    cat >&2 <<'EOF'
ERROR: dotnet SDK 가 없습니다.

  macOS  : brew install --cask dotnet-sdk
  기타   : https://dotnet.microsoft.com/download
EOF
    exit 7
fi

# .sln / .csproj 는 Unity 가 생성하며 .gitignore 대상이다.
# 클론 직후에는 존재하지 않으므로, 없을 때 원인을 명확히 알려준다.
SLN=$(find . -maxdepth 1 -name "*.sln" | head -1)
if [ -z "$SLN" ]; then
    cat >&2 <<'EOF'
ERROR: .sln 이 없습니다 — dotnet format 이 검사할 대상을 찾지 못합니다.

.sln/.csproj 는 Unity 가 생성하는 파일이고 .gitignore 대상입니다.
Unity 에디터를 한 번 열면(./scripts/run-editor.sh) 자동 생성됩니다.

에디터 안에서 강제 재생성:
  Edit > Preferences > External Tools > "Regenerate project files"
EOF
    exit 6
fi

if [ "${1:-}" = "--fix" ]; then
    echo "포맷 적용: $SLN"
    dotnet format "$SLN" --verbosity minimal
    exit $?
fi

echo "포맷 검사: $SLN"
dotnet format "$SLN" --verify-no-changes --verbosity minimal
EXIT=$?

if [ $EXIT -ne 0 ]; then
    cat >&2 <<'EOF'

포맷 위반이 있습니다. 자동으로 고치려면:
  ./tests/lint.sh --fix
EOF
    exit 1
fi

echo "통과."
