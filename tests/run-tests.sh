#!/usr/bin/env bash
# run-tests.sh — Unity Test Framework 스위트를 헤드리스로 실행하고 요약한다.
#
# Usage:
#   ./tests/run-tests.sh [editmode|playmode|all]     기본: editmode
#   ./tests/run-tests.sh editmode --filter Claim*    NUnit 필터 전달
#
# Output:
#   tests/.results/{platform}-results.xml   NUnit3 원본 (gitignore 대상)
#   tests/.results/{platform}.log           Unity 배치모드 로그
#   stdout                                   parse-results.py 요약
#
# Exit:
#   0 통과 / 1 테스트 실패 / 2 컴파일 실패 / 3 결과 불명
#   4 Unity 에디터가 이미 열려 있음 / 그 외는 unity-path.sh 참조

set -uo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
source scripts/lib/unity-path.sh

RESULTS_DIR="tests/.results"
mkdir -p "$RESULTS_DIR"

# ── 인자 ────────────────────────────────────────────────────────────────
MODE="${1:-editmode}"
[ $# -gt 0 ] && shift
EXTRA_ARGS=("$@")

case "$MODE" in
    editmode|edit)   PLATFORMS=("EditMode") ;;
    playmode|play)   PLATFORMS=("PlayMode") ;;
    all)             PLATFORMS=("EditMode" "PlayMode") ;;
    *)
        echo "ERROR: 알 수 없는 모드: $MODE" >&2
        echo "사용 가능: editmode | playmode | all" >&2
        exit 2 ;;
esac

# ── 에디터 점유 확인 ─────────────────────────────────────────────────────
# 배치모드는 프로젝트 락을 잡는다. GUI 에디터(ClaudeBridge 용)가 떠 있으면
# 실패하는데, Unity 의 에러 메시지가 불친절해서 원인을 찾기 어렵다.
if [ -f "Temp/UnityLockfile" ]; then
    cat >&2 <<EOF
ERROR: Unity 에디터가 이 프로젝트를 열고 있습니다 (Temp/UnityLockfile).

배치모드 테스트는 프로젝트 락을 단독으로 잡아야 합니다. 둘 중 하나:
  1. 에디터를 닫고 다시 실행
  2. 에디터를 띄운 채로 하려면 Test Runner 창(Window > General > Test Runner) 사용
EOF
    exit 4
fi

WORST_EXIT=0

for PLATFORM in "${PLATFORMS[@]}"; do
    LOWER=$(echo "$PLATFORM" | tr '[:upper:]' '[:lower:]')
    XML="$RESULTS_DIR/$LOWER-results.xml"
    LOG="$RESULTS_DIR/$LOWER.log"
    rm -f "$XML"

    echo "── $PLATFORM ─────────────────────────────────────────"

    # -quit 를 쓰지 않는다: -runTests 는 완료 후 스스로 종료하며,
    # -quit 를 같이 주면 테스트가 끝나기 전에 에디터가 내려간다.
    UNITY_ARGS=(
        -batchmode
        -runTests
        -testPlatform "$PLATFORM"
        -projectPath "$PROJECT_ROOT"
        -testResults "$PROJECT_ROOT/$XML"
        -logFile "$PROJECT_ROOT/$LOG"
    )
    # PlayMode 는 렌더링 경로를 타므로 -nographics 를 주지 않는다.
    [ "$PLATFORM" = "EditMode" ] && UNITY_ARGS+=(-nographics)

    "$UNITY_BIN" "${UNITY_ARGS[@]}" "${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"}"

    python3 tests/parse-results.py "$XML" --log "$LOG"
    EXIT=$?
    [ $EXIT -gt $WORST_EXIT ] && WORST_EXIT=$EXIT
    echo ""
done

if [ $WORST_EXIT -ne 0 ]; then
    echo "원본 결과: $RESULTS_DIR/" >&2
fi

exit $WORST_EXIT
