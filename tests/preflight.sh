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

# ── 3. 공유 폴더 수정 (RULE-02 · RULE-06 · §7) ──────────────────────────
# ProjectSettings/ · Packages/ 만 보던 검사를 **RULE-02 심링크 폴더 전체**로 넓혔다.
# Assets/Settings/ 가 빠져 있어 WebGL 빌드가 되쓴 URP 애셋이 그대로 커밋에 섞인
# 적이 있다 (M2). 목록은 ensure-worktree-setup.sh 에서 읽으므로 복제되지 않는다.
source "$(dirname "${BASH_SOURCE[0]}")/../scripts/lib/shared-assets.sh"
SHARED_CHANGED=$(shared_assets_status 2>/dev/null || true)
if [ -n "$SHARED_CHANGED" ]; then
    record "공유 폴더 (RULE-02·§7)" "WARN" \
        "$(echo "$SHARED_CHANGED" | wc -l | tr -d ' ')건 — 사람 승인 필요"
    echo "$SHARED_CHANGED" | sed 's/^/    /' >&2
else
    record "공유 폴더 (RULE-02·§7)" "PASS" "심링크 폴더 9곳 변경 없음"
fi

# ── 4. 난수 격리 (rules/tests.md §5) ────────────────────────────────────
# 배정은 순차번호가 모든 동률을 끝내므로 난수가 없다. 보상 추첨(M3)만 예외이고,
# 그것도 주입된 IRandomSource 를 거친다.
#
# 옛 가드는 `Random\.` 이라는 **금지형**을 정규식으로 잡았는데 구멍이 있었다 —
# `new System.Random()` 은 그 형태가 아니라 그냥 통과했다. 대역 산술 가드(5)와 같은
# **허용 목록** 방식으로 바꾸고, 그 구멍도 함께 막는다.
RANDOM_ALLOW='Assets/Code/Scripts/Runtime/Run/'
if [ -d "Assets/Code/Scripts/Runtime" ]; then
    RANDOM_HITS=$(grep -rnE '\bRandom\b' Assets/Code/Scripts/Runtime 2>/dev/null \
        | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)' \
        | grep -vE "^$RANDOM_ALLOW" || true)
    if [ -n "$RANDOM_HITS" ]; then
        record "배정 난수 금지" "FAIL" "허용 목록 밖 $(echo "$RANDOM_HITS" | wc -l | tr -d ' ')건"
        echo "$RANDOM_HITS" | head -5 >&2
    else
        record "배정 난수 금지" "PASS" "허용 목록(Run/) 밖 0건"
    fi

    # 전역 난수는 허용 목록 안에서도 금지다. 시드를 우리가 들고 있지 않으면 같은 런을
    # 다시 돌려볼 수 없고, 보상 테스트가 값 비교가 아니라 통계 검증이 된다.
    GLOBAL_RANDOM=$(grep -rnE '(UnityEngine|System)\.Random|new[[:space:]]+Random\(' \
        Assets/Code/Scripts/Runtime 2>/dev/null \
        | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)' || true)
    if [ -n "$GLOBAL_RANDOM" ]; then
        record "전역 난수 금지" "FAIL" "$(echo "$GLOBAL_RANDOM" | wc -l | tr -d ' ')건 — IRandomSource 주입으로 바꾸세요"
        echo "$GLOBAL_RANDOM" | head -5 >&2
    else
        record "전역 난수 금지" "PASS" "Unity·BCL 전역 난수 0건"
    fi
else
    record "배정 난수 금지" "SKIP" "Runtime 어셈블리 아직 없음"
    record "전역 난수 금지" "SKIP" "Runtime 어셈블리 아직 없음"
fi

# ── 5. 대역 산술 격리 (CLAUDE.md §1.1-3a) ──────────────────────────────
# 타겟팅 대역은 배정·타이밍에만 쓰인다. 정규식으로 "게이트 패턴"을 잡는 방식은
# 오탐(ClaimDeadline 은 정당하게 대역을 보고 false 를 돌려준다)과 미탐(새 금지형
# `if (BandDistance(...) > 0) return false;`)이 둘 다 난다.
#
# 대신 **허용 목록**으로 본다 — 대역 산술이 정해진 세 파일 밖에 등장하면 FAIL.
# 근거: .claude/domain/sushi-claim-flow.md §7 "가격을 읽는 곳은 다섯뿐이다"
if [ -d "Assets/Code/Scripts/Runtime" ]; then
    BAND_HITS=$(grep -rnE 'BandDistance|BandWidth|TargetingMin|TargetingMax' \
        Assets/Code/Scripts/Runtime 2>/dev/null \
        | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)' \
        | grep -vE '/(TargetingPriority|ClaimPairComparer|ClaimDeadline)\.cs:' || true)
    if [ -n "$BAND_HITS" ]; then
        record "대역 산술 격리" "FAIL" "허용 목록 밖 $(echo "$BAND_HITS" | wc -l | tr -d ' ')건"
        echo "$BAND_HITS" | head -5 >&2
    else
        record "대역 산술 격리" "PASS" "허용 목록 3파일 밖 0건"
    fi
else
    record "대역 산술 격리" "SKIP" "Runtime 어셈블리 아직 없음"
fi

# ── 5b. 자격 경로 청정 (CLAUDE.md §1.1-3a) ─────────────────────────────
# 자격 판정과 식욕 상태 머신은 가격 타입을 아예 참조하지 않는다. 이 둘에
# Price/Targeting 이 등장하면 자격에 가격이 샌 것이다.
ELIGIBILITY_FILES="Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs \
Assets/Code/Scripts/Runtime/Customers/CustomerAppetiteMachine.cs"
if [ -f "Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs" ]; then
    # shellcheck disable=SC2086
    ELIGIBILITY_HITS=$(grep -nE 'Price|Targeting|Band' $ELIGIBILITY_FILES 2>/dev/null \
        | grep -vE ':[0-9]+:[[:space:]]*(///|//|\*)' || true)
    if [ -n "$ELIGIBILITY_HITS" ]; then
        record "자격 경로 청정" "FAIL" "자격 경로에 가격이 샜다"
        echo "$ELIGIBILITY_HITS" | head -5 >&2
    else
        record "자격 경로 청정" "PASS" "CustomerLogic·AppetiteMachine 에 0건"
    fi
else
    record "자격 경로 청정" "SKIP" "자격 판정 파일 아직 없음"
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
