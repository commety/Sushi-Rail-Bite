#!/usr/bin/env bash
# preflight.sh — 커밋 전 체크리스트(CLAUDE.md §8)를 한 번에 돌린다.
#
# Usage:
#   ./tests/preflight.sh              전체
#   ./tests/preflight.sh --fast       유니티를 띄우지 않는 검사만 (수 초)
#
# 한 항목이 실패해도 멈추지 않고 끝까지 돌린 뒤 표로 보고한다.
# 하나 고치고 다시 돌렸더니 다음 게 터지는 왕복을 줄이기 위해서다.
#
# Exit: 0 전부 통과 / 1 실패 항목 있음

set -uo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

FAST=0
[ "${1:-}" = "--fast" ] && FAST=1

RESULTS=()
FAILED=0

record() {
    RESULTS+=("$1|$2|$3")
    [ "$2" = "FAIL" ] && FAILED=1
    return 0
}

# ── 1. 에셋 누출 (§9) ────────────────────────────────────────────────────
# 원본 스프라이트·음원이 의도치 않게 커밋에 섞이는 것을 막는다.
ASSET_RE='\.(png|psd|psb|wav|mp3|ogg|aif|aiff|aseprite|ase|tif|tiff|blend|fbx)$'
LEAKED=$(git status --porcelain 2>/dev/null | awk '{print $NF}' | grep -iE "$ASSET_RE" || true)
if [ -n "$LEAKED" ]; then
    record "에셋 누출 (§9)" "WARN" "$(echo "$LEAKED" | wc -l | tr -d ' ')건 — 아래 목록 확인"
else
    record "에셋 누출 (§9)" "PASS" "원본 에셋 변경 없음"
fi

# ── 2. .meta 직접 편집 (RULE-03) ────────────────────────────────────────
META_ONLY=$(git diff --name-only 2>/dev/null | grep '\.meta$' || true)
META_SRC=$(git diff --name-only 2>/dev/null | grep -v '\.meta$' || true)
if [ -n "$META_ONLY" ] && [ -z "$META_SRC" ]; then
    record ".meta 편집 (RULE-03)" "WARN" "소스 변경 없이 .meta 만 바뀜"
else
    record ".meta 편집 (RULE-03)" "PASS" "-"
fi

# ── 3. ProjectSettings 수정 (RULE-06 · §7) ──────────────────────────────
PS_CHANGED=$(git status --porcelain ProjectSettings/ Packages/ 2>/dev/null || true)
if [ -n "$PS_CHANGED" ]; then
    record "ProjectSettings/Packages (§7)" "WARN" "사람 승인 필요한 변경 있음"
else
    record "ProjectSettings/Packages (§7)" "PASS" "-"
fi

# ── 4. 배정 경로 난수 (rules/tests.md §5) ───────────────────────────────
# 순차번호가 모든 동률을 끝내므로 프로덕션 배정 경로에 Random 이 있으면 규칙 위반.
if [ -d "Assets/Code/Scripts/Runtime" ]; then
    RANDOM_HITS=$(grep -rnE '\b(UnityEngine\.)?Random\.' Assets/Code/Scripts/Runtime 2>/dev/null || true)
    if [ -n "$RANDOM_HITS" ]; then
        record "배정 난수 금지" "FAIL" "$(echo "$RANDOM_HITS" | wc -l | tr -d ' ')건 발견"
    else
        record "배정 난수 금지" "PASS" "Runtime 에 Random 0건"
    fi
else
    record "배정 난수 금지" "SKIP" "Runtime 어셈블리 아직 없음"
fi

# ── 5. 타겟팅 자격 오용 (CLAUDE.md §1.1-3a) ─────────────────────────────
# 타겟팅은 배정에만 쓰인다. bool 을 반환하는 자격 판정 경로에 나타나면 기획 위반.
if [ -d "Assets/Code/Scripts/Runtime" ]; then
    TARGETING_GATE=$(grep -rnE 'Targeting.*(>|<|>=|<=).*(threshold|Threshold)|Abs\(.*Targeting.*\)\s*(>|>=)' \
        Assets/Code/Scripts/Runtime 2>/dev/null || true)
    if [ -n "$TARGETING_GATE" ]; then
        record "타겟팅 자격 오용" "FAIL" "게이트 의심 패턴 발견"
    else
        record "타겟팅 자격 오용" "PASS" "의심 패턴 없음"
    fi
else
    record "타겟팅 자격 오용" "SKIP" "Runtime 어셈블리 아직 없음"
fi

# ── 6. 포맷/린트 (§8-1) ─────────────────────────────────────────────────
if [ $FAST -eq 1 ]; then
    record "포맷/린트 (§8-1)" "SKIP" "--fast"
else
    LINT_OUT=$(./tests/lint.sh 2>&1)
    case $? in
        0) record "포맷/린트 (§8-1)" "PASS" "-" ;;
        6|7) record "포맷/린트 (§8-1)" "SKIP" "$(echo "$LINT_OUT" | head -1)" ;;
        *) record "포맷/린트 (§8-1)" "FAIL" "./tests/lint.sh --fix 로 수정" ;;
    esac
fi

# ── 7. 테스트 (§8-3) ────────────────────────────────────────────────────
# 테스트 .cs 가 하나도 없으면 스위트가 아직 없는 것이다 (M0 이전).
# 파일은 있는데 0개가 발견되는 것과는 다르다 — 그건 asmdef 오설정이므로 FAIL.
TEST_FILES=$(find Assets/Tests -name '*.cs' 2>/dev/null | head -1)

if [ $FAST -eq 1 ]; then
    record "EditMode 테스트 (§8-3)" "SKIP" "--fast"
elif [ -z "$TEST_FILES" ]; then
    record "EditMode 테스트 (§8-3)" "SKIP" "테스트 파일 없음 — M0 에서 생성"
else
    TEST_OUT=$(./tests/run-tests.sh editmode 2>&1)
    TEST_EXIT=$?
    SUMMARY=$(echo "$TEST_OUT" | grep -E '^(통과|실패|컴파일 실패|테스트가 0개)' | head -1)
    case $TEST_EXIT in
        0) record "EditMode 테스트 (§8-3)" "PASS" "${SUMMARY:-통과}" ;;
        2) record "EditMode 테스트 (§8-3)" "FAIL" "컴파일 실패 — 테스트 미실행" ;;
        # 4 = 에디터 점유. 환경 조건이지 코드 결함이 아니다. FAIL 로 두면
        # ClaudeBridge 에디터를 띄워두는 평상시 상태에서 게이트가 늘 빨간불이 된다.
        4) record "EditMode 테스트 (§8-3)" "WARN" "에디터 점유로 실행 못 함 — 에디터를 닫고 재실행" ;;
        3) record "EditMode 테스트 (§8-3)" "FAIL" "${SUMMARY:-결과 불명}" ;;
        *) record "EditMode 테스트 (§8-3)" "FAIL" "${SUMMARY:-실패}" ;;
    esac
    echo "$TEST_OUT" > tests/results/preflight-tests.log
fi

# ── 보고 ────────────────────────────────────────────────────────────────
echo ""
echo "커밋 전 체크리스트 (CLAUDE.md §8)"
echo "──────────────────────────────────────────────────────────────"
for row in "${RESULTS[@]}"; do
    IFS='|' read -r name status detail <<< "$row"
    case "$status" in
        PASS) mark="ok  " ;;
        FAIL) mark="FAIL" ;;
        WARN) mark="warn" ;;
        *)    mark="skip" ;;
    esac
    # 한글 라벨은 바이트 수와 표시 폭이 달라 printf 로 열을 맞출 수 없다.
    # 구분자를 쓰는 편이 깨지지 않는다.
    echo "[$mark] $name — $detail"
done
echo "──────────────────────────────────────────────────────────────"

if [ -n "$LEAKED" ]; then
    echo ""
    echo "원본 에셋 변경 목록 (§9 — 공개용인지 사람이 판단):"
    echo "$LEAKED" | sed 's/^/  /'
fi

echo ""
if [ $FAILED -eq 1 ]; then
    echo "FAIL 이 있습니다. 커밋하지 말고 원인을 보고하세요 (CLAUDE.md §8)."
    exit 1
fi
echo "커밋 가능. warn 항목은 사람의 판단이 필요합니다."
