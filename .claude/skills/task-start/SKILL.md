---
name: task-start
description: 작업 시작 전 범위와 제약을 확정하는 브리핑 루틴. 먼저 `.claude/INDEX.md`를 스캔해 필요한 지침만 선별 로드하여 토큰과 시간을 아낍니다.
---

# Skill: /task-start (Task Briefing)

작업을 시작하기 전에 반드시 이 브리핑을 수행한다. **파일을 열기 전에 범위를 먼저 확정한다.**

---

### STEP 0 — 인덱스 기반 선별 로드 (토큰 절약)

[`.claude/INDEX.md`](../../INDEX.md)를 먼저 읽는다. 이 파일만 로드하면 몇 백 토큰이다.

1. 작업 프롬프트에서 핵심 명사·동사·심볼을 뽑는다. (예: "손님이 눈앞 초밥을 안 집고 구경한다" → `customer`, `targeting`, `claim`, `FIFO`, `reach`, `CustomerData`, `belt`)
2. 인덱스 각 항목의 `keywords`와 매칭한다.
3. **매칭된 파일만** `Read`/`Grep`으로 연다. 매칭이 없으면 읽지 않는다.
4. `RULES.md`는 항상 스캔. `CLAUDE.md`는 이미 로드돼 있다.

선별 로드 결과를 한 줄로 선언:

```
[STEP 0] 매칭된 지침: RULES.md, rules/scripts.md, rules/scriptable-object.md, rules/tests.md
         건너뜀: knowledge/csharp-dotnet.md, rules/asmdef.md, knowledge/unity-webgl-performance.md, 기타 skills
```

이 선언은 이후 STEP에서 실제로 읽은 범위를 검증할 근거가 된다.

---

### STEP 1 — RULES.md 재확인

- 방금 스캔한 `RULES.md`에서 **이번 작업과 관련된 규칙**을 골라 명시한다.
- 위반 위험이 있는 규칙은 대응책과 함께 선언한다.

---

### STEP 2 — 대상 파악

- `grep`으로 실제 파일 위치를 확인한다. **추측하지 않는다.**
- 대화 내역에서 경로를 꺼내지 않는다. 반드시 직접 조회한다.

```bash
grep -rn "class TargetClass" Assets/ --include="*.cs" -l
grep -rn "TargetMethod"      Assets/ --include="*.cs" -l
```

관련 파일·심볼을 모두 나열한다.

---

### STEP 3 — 작업 범위 선언

- 수정할 파일과 심볼을 명시한다.
- 수정하지 않는 파일도 명시한다 (호출부 확인만 등).
- **어느 어셈블리를 건드리는지** 명시한다 (`Runtime` / `Runtime.Data` / `Presentation` / `Tests.*`). 여러 어셈블리를 한 작업에서 건드려야 하면 `/design` 으로 쪼갤 후보다.
- **테스트 계획**을 한 줄로 선언한다: 어느 EditMode 테스트를 먼저 실패시킬 것인가 (TDD, `CLAUDE.md` §5). 순수 로직인데 테스트 계획이 없으면 범위 정의가 덜 된 것이다.
- 범위 밖 작업이 발견되면 **즉시 멈추고 아키텍트에게 보고**한다.
- `CLAUDE.md` §7 (인간 판단 영역)에 걸리는 항목 — 새 어셈블리·새 패키지·ProjectSettings·밸런스 SO 일괄 수정·머지/브랜치 삭제 — 이 범위에 있으면 여기서 **승인 요청**부터 한다.

---

### STEP 3.5 — 브랜치 확인

- 현재 브랜치가 `feature/*` 인지 확인한다 (`git branch --show-current`).
- `main` / `dev` / `release/*` 위에 있으면 **작업 전에** `feature/*` 브랜치를 제안한다. `CLAUDE.md` §6 의 3-way handshake 1단계(propose)다.
- 브랜치 생성은 제안 후 사용자 확인을 받고 진행한다.

---

### STEP 4 — Lock 획득

- 하네스 세션이 활성이면 SymbolLock을 획득한다.
- 하네스가 없으면 이 단계를 건너뛴다.

---

### STEP 5 — 작업 시작 선언

- 모든 사전 조건이 확인됐음을 선언하고 작업 내용을 한 줄로 요약한다.

---

## 출력 형식

```
[STEP 0] 인덱스 선별 로드
매칭된 지침: {파일 목록}
건너뜀: {파일 목록}

[STEP 1] 관련 규칙
{RULE-NN: 이번 작업과의 연관, 위반 위험 여부}

[STEP 2] 대상 파악
{grep 결과와 파일 위치}

[STEP 3] 작업 범위
어셈블리: {Runtime | Runtime.Data | Presentation | Tests.EditMode | ...}
수정할 파일:
- {파일} → {심볼}
수정하지 않는 파일:
- {파일} (사유)
테스트 계획: {먼저 실패시킬 EditMode 테스트} 또는 {해당 없음 + 사유}
§7 승인 필요 항목: {없음 | 목록}

[STEP 3.5] 브랜치: {feature/xxx 확인됨 | main 위 — feature 브랜치 제안}

[STEP 4] Lock: {획득 또는 하네스 비활성}

[STEP 5] 작업 시작: {한 줄 요약}
```

## 금지 사항

- **지침 전체를 매번 통째로 로드하지 않는다.** 인덱스가 존재하는 이유다.
- **매칭 안 된 파일을 "혹시 몰라서" 읽지 않는다.** 작업 진행 중 실제로 필요해지면 그때 연다.
- **파일 경로를 추측하지 않는다.** grep으로 확인하거나 `{TODO: verify}` 마크한다.
