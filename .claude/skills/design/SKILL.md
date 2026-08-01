---
name: design
description: 기획 내용(자연어 또는 Notion 링크)을 받아 영역별 단계로 분해한 작업 계획 아티팩트를 design/{slug}/ 폴더에 생성합니다. 각 단계는 다음 에이전트가 그대로 실행할 수 있는 독립 프롬프트입니다.
---

# Skill: /design (Design Decomposition Instruction)

당신은 프로젝트의 '설계자'입니다. 기획을 **실행 가능한 단계**로 분해하고, 각 단계를 독립적으로 실행 가능한 에이전트용 프롬프트 파일로 저장합니다.

**중요:** 이 스킬은 계획만 만듭니다. 코드를 직접 작성하지 않습니다. 구현은 각 단계의 에이전트가 `/task-start`로 수행합니다.

---

## 1. 원칙

- **한 단계 = 한 영역(어셈블리).** 여러 어셈블리를 동시에 건드리는 단계는 만들지 않는다.
- **영역 교차가 필요하면 쪼갠다.** UI가 Core 데이터를 쓰면, Core에서 인터페이스를 먼저 정의하는 단계를 앞에 둔다.
- **인터페이스 우선.** 다음 단계가 참조할 심볼의 시그니처는 이전 단계에서 확정한다.
- **범위는 좁게, 경계는 명확히.** 각 단계는 "어느 파일, 어느 심볼, 어느 시그니처"까지 명시한다.
- **추측 금지.** 파일 경로는 `grep`으로 확인한다. 확인 불가능하면 `{TODO: verify}` 마크한다.

---

## 2. 입력 처리

- **자연어 기획:** 그대로 요약.
- **Notion 링크:** `WebFetch`로 본문을 가져온다. 접근 실패 시(사설 페이지 등) 아키텍트에게 본문 붙여넣기를 요청한다.
- **혼합:** 링크 + 보충 설명이 같이 오면 둘을 병합해 원문으로 간주한다.

---

## 3. 분해 절차

**STEP A — 목표 파악**  
기획이 무엇을 만드는지 **한 문장**으로 요약한다.

**STEP B — 영역 매핑**  
`CLAUDE.md` §3.7(어셈블리 분리) · §10(디렉터리 구조)을 먼저 확인한다. 목표 어셈블리 구성:

| 영역 | 어셈블리 | 폴더 | 담는 것 |
|---|---|---|---|
| `data` | `Runtime.Data` | `Assets/Code/Scripts/Runtime.Data/` | `SushiData`, `CustomerData`, `StageConfig`, `~EventChannelSO` |
| `runtime` | `Runtime` | `Assets/Code/Scripts/Runtime/` | 벨트·손님·점수·상태 머신의 **순수 C# 로직** |
| `presentation` | `Presentation` | `Assets/Code/Scripts/Presentation/` | `MonoBehaviour`, View, Presenter, UI 바인딩 |
| `editor` | `Editor` | `Assets/Code/Scripts/Editor/` | 게임용 에디터 툴링 |
| `tests` | `Tests.EditMode` / `Tests.PlayMode` | `Assets/Tests/` | 단위·통합 테스트 |

네임스페이스는 `SushiDefense.<Domain>` (`SushiDefense.Belt`, `.Customers`, `.Scoring`, `.Data`, `.UI`).

> 현재 저장소에는 `.asmdef` 가 아직 없다. 계획이 새 어셈블리 생성을 포함하면 **아키텍트 승인 항목**으로 README.md 에 명시하고 별도 단계로 분리한다 (`CLAUDE.md` §7). RULES.md RULE-01(Domain Reload)을 유발하지 않도록 주의.

**STEP C — 의존성 그래프**  
영역 간 호출 방향을 그린다. 기본은 `Runtime.Data` 가 루트인 단방향:

```
Runtime.Data ← Runtime ← Presentation
       ↑          ↑
    Tests.EditMode / Tests.PlayMode
```

역방향 의존(로직 → UI)은 C# `event` 또는 SO 이벤트 채널(`SushiEatenEventChannelSO`)을 `Runtime.Data` 에 두는 방식으로 풀어낸다.

**STEP D — 단계화 (topological sort)**  
의존성 그래프를 위상 정렬한다. 이 프로젝트의 기본 순서는:

```
1) SO 스키마 정의 (Runtime.Data)
2) EditMode 실패 테스트 작성 (Tests.EditMode)
3) 순수 로직 구현 (Runtime)
4) View/Presenter 연결 (Presentation)
5) 씬·프리팹 조립 (필요 시 /make-asset, /run bridge)
```

**TDD 는 기본값이다** (`CLAUDE.md` §5). 로직 단계마다 그 앞에 테스트 단계를 둔다. 테스트 단계를 생략한 계획은 미완성이다.

**단계 수의 기준:**
- 단일 영역·소규모 변경: **1~2 단계**
- 영역 2개 교차: **3~5 단계**
- 영역 3개 이상 또는 신규 어셈블리 포함: **6~10 단계**

의심스러우면 **쪼갠다**. 큰 단계 하나보다 작은 단계 여러 개가 낫다.

**STEP E — 각 단계의 계약 확정**  
단계마다 다음을 채운다:
- 생성/수정 파일 경로
- 핵심 심볼 이름과 시그니처
- 선행 단계 산출물 의존성
- 완료 판정 기준 (grep·컴파일로 확인 가능한 것 위주)

---

## 4. 아티팩트 생성

**경로:** `design/{slug}/`  
`slug`는 기획 제목에서 파생한 kebab-case 이름. 필요 시 날짜 프리픽스 (예: `inventory-system`, `2026-05-matchmaking`).

**폴더가 이미 존재하면** 덮어쓰지 말고 `{slug}-v2` 등으로 새로 만든다. 기존 계획을 보존하기 위함.

### 파일 구조

```
design/{slug}/
├── README.md                  ← 전체 요약, 아키텍처 결정, 단계 목록, 의존성 그래프
├── step-01-{area}-{topic}.md  ← 첫 단계 에이전트 프롬프트
├── step-02-{area}-{topic}.md
└── step-NN-{area}-{topic}.md
```

`{area}`는 소문자 영역 이름(`data`, `runtime`, `presentation`, `tests`, `editor`), `{topic}`는 kebab-case 짧은 주제.

예: `step-01-data-sushi-schema.md`, `step-02-tests-customer-price-range.md`, `step-03-runtime-customer-logic.md`

---

### `README.md` 템플릿

```markdown
# {기획 제목}

## 한 줄 요약
{한 문장}

## 원문
{자연어 기획 원문 또는 Notion 링크 + 요약}

## 아키텍처 결정
- {결정 1}: {왜}
- {결정 2}: {왜}

## 터치 영역
| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | Runtime.Data | {역할} |
| runtime | Runtime | {역할} |
| presentation | Presentation | {역할} |

## 의존성 그래프
\`\`\`
SushiDefense.Data.CustomerData → SushiDefense.Customers.CustomerLogic → SushiDefense.UI.CustomerView
\`\`\`

## 새 밸런스 수치
| 수치 | 들어갈 SO | 초기값 제안 | 근거 |
|---|---|---|---|
| {필드} | {SushiData/CustomerData/StageConfig} | {값} | {기획 근거} |

## 단계
1. [step-01-data-schema.md](step-01-data-schema.md) — Data: SO 스키마 정의
2. [step-02-tests-logic.md](step-02-tests-logic.md) — Tests: 실패 테스트 작성
3. [step-03-runtime-logic.md](step-03-runtime-logic.md) — Runtime: 로직 구현
4. ...

## 병렬 실행 가능성
- step-01 완료 후 step-02와 step-03 병렬 가능 (서로 독립)
- step-04는 step-02, step-03 모두 완료 후 실행
```

---

### `step-NN-{area}-{topic}.md` 템플릿 (다음 에이전트가 그대로 실행할 프롬프트)

```markdown
# Step {NN}: {제목}

- **영역:** `{area}` — 어셈블리 `{Runtime.Data|Runtime|Presentation|Tests.EditMode|Tests.PlayMode|Editor}`
- **선행 단계:** step-{NN-1} 완료 필요 ({산출물})
- **후행 단계:** step-{NN+1}에서 이 단계의 {심볼}을 사용

---

## 목적
{이 단계가 해결하는 것 — 한 문단}

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.

### 생성/수정 파일
- `Assets/Code/Scripts/{Assembly}/{Domain}/{File}.cs` — {생성|수정}
- `Assets/Tests/EditMode/{Domain}/{File}Tests.cs` — {생성|수정}

### 핵심 심볼
\`\`\`csharp
namespace SushiDefense.{Domain}
{
    public interface I{Name}
    {
        {signature1}
        {signature2}
    }
}
\`\`\`

### 선행 산출물 의존성
- `SushiDefense.Data.{Foo}Data` — step-01에서 정의됨
- 없으면 "없음"으로 명시

### 밸런스 수치
- 이 단계에서 쓰는 수치는 전부 `{SO 이름}` 의 필드에서 읽는다. 코드 상수 금지 (`CLAUDE.md` §3.1)
- 없으면 "없음"으로 명시

### 제약
- RULES.md RULE-{N} 준수 ({간단 요약})
- 어셈블리 간 역방향 의존 금지 (`Runtime` → `Presentation` 참조 금지)
- 로직은 `MonoBehaviour` 밖 순수 C# 클래스에 (`CLAUDE.md` §3.2)
- {기타 제약}

### 완료 판정
- [ ] `grep -rn "I{Name}" Assets/Code/Scripts/{Assembly}/` 로 정의 확인
- [ ] EditMode 테스트 Green (`Unity -batchmode -runTests -testPlatform EditMode ...`)
- [ ] Unity 컴파일 통과 (`./scripts/bridge-run.sh`)
- [ ] {추가 체크}

### 예상 커밋 메시지
Conventional Commits, 스코프는 도메인 용어 (`CLAUDE.md` §6.5):
\`\`\`
feat(customer): add digestion cooldown state
test(belt): cover sushi spawn distribution
\`\`\`

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 어셈블리 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
```

---

## 5. 마지막 보고

아티팩트 생성을 마친 뒤 아키텍트에게 다음을 보고한다:

1. 생성된 폴더 경로: `design/{slug}/`
2. 총 단계 수와 영역별 분포 (예: "총 5단계 — data 1개, tests 2개, runtime 1개, presentation 1개")
3. 병렬 실행 가능한 단계 쌍
4. 첫 단계에 투입할 명령 예시:
   ```
   cat design/{slug}/step-01-*.md | claude
   ```

---

## 6. 금지 사항

- **코드를 직접 작성하지 않는다.** 이 스킬은 계획만 만든다.
- **여러 영역을 한 단계에 몰아넣지 않는다.** 의심스러우면 쪼갠다.
- **추측한 파일 경로를 그대로 쓰지 않는다.** grep으로 확인하지 못한 경로는 `{TODO: verify}` 마크한다.
- **아키텍트 승인 없이 새 어셈블리를 생성하지 않는다.** 제안만 README.md에 남기고 별도 단계로 분리한다.
