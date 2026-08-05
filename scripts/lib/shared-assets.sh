#!/usr/bin/env bash
# shared-assets.sh — RULE-02 공유(심링크) 폴더의 오염을 감지·원복한다.
#
# 왜 필요한가:
#   Unity 는 빌드 중에 셰이더 스트리핑 상태(m_Prefilter*)와 플랫폼 배칭 설정을
#   URP 애셋에 **되쓴다.** 정상 동작이라 막을 수 없고, `-buildTarget` 으로 타깃을
#   전환할 때마다 Assets/Settings/ 아래 파일들이 더러워진다.
#
#   그 폴더들은 RULE-02 심링크 대상이라 워크트리에서 수정하면 안 되고, 메인
#   프로젝트에서도 빌드 부산물이 커밋에 섞이면 안 된다. M2 에서 실제로 한 번
#   섞여 들어가 amend 했다.
#
# 사용:
#   source "$(dirname "${BASH_SOURCE[0]}")/lib/shared-assets.sh"
#   shared_assets_restore        # 더러워진 것을 되돌리고 무엇을 되돌렸는지 출력
#   shared_assets_dirty          # 되돌리지 않고 목록만 출력 (preflight 용)
#
# 다루는 범위:
#   **추적 중인 파일의 미스테이지 변경**만 본다. 빌드가 만드는 것이 정확히 그것이고,
#   스테이지된 변경까지 건드리면 사람이 의도한 작업을 지울 수 있다.

# ── 보호 대상 ───────────────────────────────────────────────────────────
# 원본은 ensure-worktree-setup.sh 의 SYMLINK_ASSET_DIRS / SYMLINK_ROOT_DIRS 다.
# 목록을 복제하지 않고 **그 스크립트에서 직접 읽는다** — RULE-02 가 "세 곳이
# 어긋나면 규칙이 오탐·미탐을 낸다" 고 못박은 지점이라, 복제하는 순간 네 곳이 된다.
shared_assets_paths() {
    local setup="${BASH_SOURCE[0]%/*}/../ensure-worktree-setup.sh"
    if [ ! -f "$setup" ]; then
        return 1
    fi

    sed -n '/^SYMLINK_ASSET_DIRS=(/,/^)/p;/^SYMLINK_ROOT_DIRS=(/,/^)/p' "$setup" \
        | sed -n 's/^[[:space:]]*"\([^"]*\)".*/\1/p'
}

# ── 내부: rel 경로를 소유한 저장소와 그 안에서의 경로 ───────────────────
# 워크트리에서는 Assets/Settings 가 심링크라 그 워크트리의 git 이 변경을 보지
# 못한다 — 오염은 **메인 프로젝트**의 git status 에 뜬다. 그래서 심링크를 따라가
# 실제 파일을 소유한 저장소를 찾아 거기서 작업한다.
_shared_assets_owner() {
    local rel="$1" phys top
    [ -e "$rel" ] || return 1
    phys=$(cd -P "$rel" 2>/dev/null && pwd) || return 1
    top=$(git -C "$phys" rev-parse --show-toplevel 2>/dev/null) || return 1
    printf '%s|%s\n' "$top" "${phys#"$top"/}"
}

# ── 더러워진 파일 목록 (원복하지 않음) ──────────────────────────────────
# 출력: "<저장소 루트>|<저장소 기준 경로>" 한 줄에 하나.
shared_assets_dirty() {
    local rel owner top sub

    while IFS= read -r rel; do
        [ -n "$rel" ] || continue
        owner=$(_shared_assets_owner "$rel") || continue
        top=${owner%%|*}
        sub=${owner#*|}

        # -z 로 받아야 공백 있는 경로("Build Profiles/Web - Desktop ...")가
        # 따옴표로 감싸지지 않는다.
        git -C "$top" diff -z --name-only -- "$sub" 2>/dev/null \
            | tr '\0' '\n' \
            | while IFS= read -r f; do
                [ -n "$f" ] && printf '%s|%s\n' "$top" "$f"
            done
    done < <(shared_assets_paths)
}

# ── 커밋 게이트용 상태 (스테이지·미추적까지 포함) ──────────────────────
# shared_assets_dirty 보다 넓게 본다. 원복은 빌드가 만든 것만 건드려야 하지만,
# **커밋을 막을지 판단할 때는** 스테이지된 변경도 미추적 파일도 다 봐야 한다.
# 출력: "<저장소 기준 경로>" 한 줄에 하나.
shared_assets_status() {
    local rel owner top sub

    while IFS= read -r rel; do
        [ -n "$rel" ] || continue
        owner=$(_shared_assets_owner "$rel") || continue
        top=${owner%%|*}
        sub=${owner#*|}

        git -C "$top" status --porcelain -z -- "$sub" 2>/dev/null \
            | tr '\0' '\n' \
            | sed -n 's/^...//p'
    done < <(shared_assets_paths)
}

# ── 원복 ────────────────────────────────────────────────────────────────
# 되돌린 파일 수를 SHARED_ASSETS_RESTORED 에 담는다.
shared_assets_restore() {
    local line top sub count=0
    local -a tops=()

    while IFS= read -r line; do
        [ -n "$line" ] || continue
        top=${line%%|*}
        sub=${line#*|}

        if [ "$count" -eq 0 ]; then
            echo "" >&2
            echo "공유 애셋이 빌드로 더러워져 원복합니다 (RULE-02):" >&2
        fi
        echo "    $sub" >&2

        git -C "$top" checkout -- "$sub" 2>/dev/null || true
        count=$((count + 1))

        case " ${tops[*]-} " in
            *" $top "*) ;;
            *) tops+=("$top") ;;
        esac
    done < <(shared_assets_dirty)

    SHARED_ASSETS_RESTORED=$count

    if [ "$count" -gt 0 ]; then
        echo "" >&2
        echo "  ↑ Unity 가 빌드 중에 되쓴 렌더 파이프라인·빌드 프로파일 상태입니다." >&2
        echo "    렌더 설정을 **의도적으로** 바꿨다면 에디터에서 다시 바꾼 뒤 직접 커밋하세요." >&2
        if [ "${#tops[@]}" -gt 0 ] && [ "${tops[0]}" != "$(git rev-parse --show-toplevel 2>/dev/null)" ]; then
            echo "    (심링크를 따라 ${tops[0]} 에서 원복했습니다)" >&2
        fi
        echo "" >&2
    fi
}
