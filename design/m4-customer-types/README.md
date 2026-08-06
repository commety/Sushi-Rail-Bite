# M4 — 손님 유형과 스테이지 진행

## 한 줄 요약

손님 세 유형을 **코드 분기 없이 대역 값만으로** 구분하고, 클리어한 스테이지가 다음 스테이지로 이어져 3판을 마치면 런이 끝나게 한다.

## 원문

[`docs/plan/M4-customer-types.md`](../../docs/plan/M4-customer-types.md)

> *"기본 손님: 모든 스탯이 평이하다 / 소식 손님: 포화도가 적지만 비싼 걸 우선적으로 고른다, 영입 비용 중간 / 먹보 손님: 먹는 속도가 빠르고 포화도가 높지만 저가를 우선적으로 고른다, 영입 비용이 비싸다 / 스테이지가 끝나면 다음 스테이지로 넘어간다"*

"우선적으로 고른다" 는 **배정**에서 작동한다 (`CLAUDE.md` §1.1-3a). 자격에는 관여하지 않는다 — 소식 손님도 눈앞에 싼 것뿐이면 **먹는다**. 이 마일스톤에서 가장 깨지기 쉬운 규칙이고, step-04 가 그 방어선이다.

---

## 착수 전에 확정한 것 (사람 결정)

| 질문 | 결정 |
|---|---|
| 세 유형의 대역 배치 | **3분할, 겹침 없음** — 먹보 100~150 / 기본 150~250 / 소식 250~320. 기본의 대역을 100~300 에서 **좁힌다** |
| 먹보의 나머지 스탯 | 먹는 속도 1.0초 · 포화도 8 · 소화 3.0초 · 영입 비용 60 |
| 시작 명부 · 기본 비용 | **현행 유지** — 명부 `[기본]`, 비용 20, 스테이지 1 예산 60 |
| 스테이지 2·3 난이도 | **표준** — S2 목표 6200 / S3 목표 7500 |

## 아키텍처 결정

### D1. 스테이지 전환은 **씬 전환이 아니라 `StageConfig` 교체**다

`RunState` 는 `StageBootstrap`(MonoBehaviour) 위에 산다. 스테이지마다 씬을 새로 로드하면 **런이 통째로 죽는다** — 덱도 명부도 사라진다. `DontDestroyOnLoad` 로 살리는 방법이 있지만, M3 에서 "런은 메모리만, 새로고침 시 초기화" 로 확정했으므로 씬 경계를 넘길 이유 자체가 없다.

한 씬에서 `Build()` 를 다시 부르며 활성 `StageConfig` 만 바꾼다. `Build()` 는 이미 호출마다 벨트·원장·지갑·조율자를 전부 새로 여는 구조라(M3 D6) **재시도와 같은 경로**를 탄다.

### D2. 스테이지 목록은 `RunConfig` SO 에 둔다

3스테이지 순서를 코드나 씬 배열에 박으면 §3.1 위반이다. `Runtime.Data` 에 `RunConfig` 를 새로 만든다. **새 어셈블리가 아니라 기존 `Runtime.Data` 안의 새 타입**이므로 §7 승인 대상이 아니다.

### D3. `IsRunComplete` 는 플래그가 아니라 **유도값**이다

```csharp
public bool IsRunComplete => _run.StageNumber > StageCount;
```

`bool _finished` 를 따로 들면 `StageNumber` 와 어긋날 수 있고, 어긋난 순간 "3스테이지를 깼는데 런이 안 끝난다" 나 그 반대가 된다. M3 에서 `OutcomeDecided` 발행 플래그를 두지 않은 것과 같은 이유다 — **두 장치가 같은 것을 지키면 나중에 한쪽만 고쳐진다.**

### D4. `RunState` 는 손대지 않는다

`AdvanceStage()` 는 이미 있다. **`RunState` 가 스테이지 총 개수를 알면 안 된다** — 그건 `RunConfig` 의 지식이고, 알게 되는 순간 런 상태가 스테이지 목록에 결합된다. 총수를 아는 것은 `RunProgression` 하나다.

### D5. 스테이지가 1개인 런이 곧 **현행 동작**이다

`_runConfig` 가 비면 `[_stageConfig]` 한 장짜리 목록으로 취급한다. 덕분에 **경로가 하나뿐**이고, 스테이지 목록을 물리지 않은 기존 PlayMode 테스트·씬이 그대로 산다. "런 진행이 없는 모드" 라는 두 번째 경로를 만들지 않는다.

### D6. 스테이지 2·3 의 `_spawnTable` 은 **비워 둔다**

M3 에서 **덱의 진실이 `StageConfig` 에서 `RunState` 로 옮겨 갔다** ([`stage-and-run.md`](../../.claude/domain/stage-and-run.md) §5). 벨트는 `Run.Sushi.Cards` 를 읽고, `SushiDeck.FromSpawnTable` 은 **런을 처음 열 때 한 번만** 불린다.

따라서 스테이지 2·3 의 `_spawnTable` 은 **읽히지 않는다.** 채워 두면 "여기서 스폰 구성을 바꿀 수 있다" 는 거짓 신호가 되어 다음 사람이 반드시 속는다. 빈 목록이 곧 "덱은 런의 것" 이라는 표시다.

> 스테이지가 새 초밥을 **소개**하게 만들고 싶어지면, 그건 `_spawnTable` 을 되살리는 게 아니라 스테이지 시작 시 덱에 합치는 별도 규칙이다. M4 범위 밖.

### D7. 자리 정의를 드디어 `StageConfig` 에서 읽는다

**지금 `StageConfig.TableSlots` 는 프로덕션 코드에서 아무도 읽지 않는다.** 자리 좌표는 씬의 `TableSlotView` 에 손으로 박혀 있고, `TableSlotView.Bind(TableSlotDefinition)` 은 **정의돼 있지만 호출되지 않는다.**

스테이지 1 에서는 우연히 값이 같아 문제가 없었다. 스테이지 2·3 이 자리 4개를 쓰는 순간 **설정을 고쳐도 화면이 안 바뀌는** 상태가 된다 — M3 에서 씬이 조용히 죽었던 것과 같은 계열의 사고다. step-07 이 이 구멍을 닫는다.

빈 목록은 **no-op** 으로 둔다. 자리 정의 없이 세운 기존 테스트 하네스가 그대로 돌아야 한다.

### D8. 보상·전환 화면은 **런 수명**이다

지금 `Rewards` 는 `Build()` 안에서 만들어지고 `Teardown()` 에서 버려진다. 전환 화면의 "다음 스테이지" 입력이 `Build()` 를 부르는데, 그 `Build()` 가 **자기를 부른 프레젠터를 파괴**하면 이벤트 발행 도중에 발밑이 무너진다.

두 프레젠터를 `RunState`·`RunProgression` 과 같은 수명으로 올린다. 둘 다 스테이지가 아니라 런에 딸린 것이므로 원래 그 자리가 맞다.

### D9. 유형 추가에 **프로덕션 코드 변경은 0** 이 기대값이다

step-04 는 테스트만 추가한다. 세 유형을 검증하는 테스트가 **하나도 안 고치고 통과**하면 M2·M2.5 설계가 옳았다는 증거다. 통과시키려고 `Runtime` 을 건드려야 한다면 그건 이 마일스톤의 성과가 아니라 **설계 결함 발견**이므로, 고치지 말고 먼저 보고한다.

### D10. 범위 밖으로 명시하는 것

| 항목 | 왜 |
|---|---|
| 영입 재화 스테이지 간 이월 | M2 열린 질문 7. 현행(이월 없음) 유지. M9 밸런싱 |
| 실패 → 재시도 UI 배선 | `Retry()` 는 이미 있고 호출 UI 는 M6(메인화면) |
| 런 종료 후 재시작 | 메인화면이 없다. 종료 표시까지가 M4 |
| 손님 아트 | M5 |
| 시너지(`SushiTrait`) | 마일스톤 미정 |

---

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | `Runtime.Data` | `RunConfig` SO 신규. 밸런스 애셋 (먹보 · 스테이지 2·3 · 런 구성) |
| runtime | `Runtime` | `RunProgression` 신규 — 현재 스테이지 · 다음 스테이지 · 런 종료 판정 |
| presentation | `Presentation` | 전환 화면 MVP, 자리 정의 바인딩, `StageBootstrap` 진행 배선, 손님 유형 표시 |
| tests | `Tests.EditMode` / `Tests.PlayMode` | 유형 동일 코드 경로 방어선, 진행 로직, 프레젠터·뷰 |

**새 어셈블리 없음. 새 패키지 없음.** §7 승인이 필요한 항목은 **밸런스 애셋 편집** 하나이고, 값은 위 표에서 이미 확정됐다.

## 의존성 그래프

```
SushiDefense.Data.RunConfig ──┐
                              ├─→ SushiDefense.Run.RunProgression ──→ SushiDefense.UI.StageTransitionPresenter
SushiDefense.Run.RunState ────┘                                              │
                                                                             ↓
                                                             SushiDefense.StageBootstrap
                                                                             ↑
SushiDefense.Data.StageConfig.TableSlots ──→ SushiDefense.Customers.TableSlotView
```

`Runtime` 은 `Presentation` 을 여전히 모른다. 전환 화면은 `IStageTransitionView` 인터페이스로만 프레젠터와 만난다 (§3.6).

---

## 새 밸런스 수치

### 손님 대역 재배치 (3분할)

| 손님 | 대역 | 폭 | 변경 |
|---|---|---|---|
| 먹보 (신규) | **100~150** | 50 | 신규 |
| 기본 | **150~250** | 100 | `100~300` 에서 **좁힘** |
| 소식 | 250~320 | 70 | 변경 없음 |

**경계값 150·250 은 두 대역 모두에서 거리 0 이다.** 그 동률은 배정 키 4(대역 폭)가 깨므로 **150 은 먹보(폭 50), 250 은 소식(폭 70)** 이 가져간다. 규칙대로의 동작이지만 밸런스를 볼 때 알고 있어야 하는 지점이다.

덱 가격 분포에 대응시키면:

| 가격 | 초밥 | 대역 안인 손님 |
|---|---|---|
| 120 | 한치 | 먹보 |
| 130 | 달걀 (보상) | 먹보 |
| 150 | 연어 · 광어 | 먹보(폭 50) > 기본 |
| 190 | 방어 | 기본 |
| 250 | 참치 (보상) | 소식(폭 70) > 기본 |
| 300 | 장어 | 소식 |
| 400 | 성게 (보상) | **아무도 없음** — 소식이 거리 80 으로 가장 가깝다 |

성게 400 이 모든 대역 밖인 것은 M3 에서 넘어온 열린 항목이다. **먹지 못하는 게 아니라** 소식이 이탈 직전에 집는다 — 상한을 올릴지는 M9 에서 본다.

### 먹보 손님 (`Customer.BigEater`)

| 필드 | 값 | 기본 대비 |
|---|---|---|
| `_id` | `customer-big-eater` | |
| `_displayName` | 먹보 | |
| `_kind` | `BigEater` (2) | |
| `_reach` | 3 | 동일 |
| `_targetingMin` / `_targetingMax` | 100 / 150 | 낮고 좁다 |
| `_eatSeconds` | 1.0 | 1.5배 빠름 |
| `_maxSaturation` | 8 | 1.6배 |
| `_digestSeconds` | 3.0 | 동일 |
| `_recruitCost` | 60 | 3배 (= 기본 3명 값) |

### 스테이지 2·3

| 필드 | Stage 01 (기존) | Stage 02 | Stage 03 |
|---|---|---|---|
| `_targetRevenue` | 5000 | **6200** | **7500** |
| `_timeLimitSeconds` | 60 | 60 | 60 |
| `_spawnIntervalSeconds` | 1.5 | **1.3** | **1.1** |
| `_maxPlacedCustomers` | 3 | **4** | **4** |
| `_initialRecruitBudget` | 60 | **80** | **100** |
| `_beltSpeed` / `_beltLength` | 2 / 20 | 2 / 20 | 2 / 20 |
| `_recognitionLatchSeconds` / `_sparsityExponent` | 1 / 1 | 1 / 1 | 1 / 1 |
| 자리 수 | 3 | **4** | **4** |
| `_spawnTable` | 5종 (= 런 시작 덱) | **빈 목록** (D6) | **빈 목록** (D6) |

**목표 매출의 근거** — α=1 에서 한 번 스폰의 기대 매출은 `종수 / Σ(1/가격)` 이다. 시작 덱 5종(120·150·150·190·300) 기준 **165.2**.

| | 스폰 수 | 벨트 총액 | 목표 | 비율 |
|---|---|---|---|---|
| Stage 01 | 60 / 1.5 = 40 | 6608 | 5000 | 75.7% |
| Stage 02 | 60 / 1.3 = 46 | 7599 | 6200 | 81.6% |
| Stage 03 | 60 / 1.1 = 54 | 8921 | 7500 | 84.1% |

M3 실측에서 **3명이 총액의 91%** 를 먹은 것이 상한이다. 84% 는 그 아래이므로 이론상 클리어 가능하지만 **자리가 4개로 늘어난 상태의 실측은 아직 없다** — step-10 에서 측정하고, 상한을 넘으면 목표를 내린다.

> 보상으로 덱이 자라면 기대 매출이 오른다 (8종이면 179.9). **총액이 늘어 실제로는 쉬워진다** — 보상이 힘이 되는 것이 로그라이트 설계 의도이므로 이 방향은 맞다.

### 자리 좌표 (4자리 스테이지)

| 자리 | 벨트 좌표 | 화면 위치 |
|---|---|---|
| 0 | 4 | (-6, -2) |
| 1 | 8 | (-2, -2) |
| 2 | 12 | (2, -2) |
| 3 | 16 | (6, -2) |

제약 확인: `자리 좌표 + Reach < 벨트 길이` → `16 + 3 = 19 < 20` ✓ ([`rules/scripts.md`](../../.claude/rules/scripts.md) §2). 이 조건이 깨지면 초밥이 범위를 벗어나기 전에 끝점에서 사라져 **마감시한이 영영 오지 않는다.**

---

## 단계

| # | 파일 | 영역 | 내용 |
|---|---|---|---|
| 01 | [step-01-data-run-config.md](step-01-data-run-config.md) | data | `RunConfig` SO — 스테이지 순서 |
| 02 | [step-02-tests-run-progression.md](step-02-tests-run-progression.md) | tests | `RunProgression` 실패 테스트 (Red) |
| 03 | [step-03-runtime-run-progression.md](step-03-runtime-run-progression.md) | runtime | `RunProgression` 구현 (Green) |
| 04 | [step-04-tests-customer-kind-parity.md](step-04-tests-customer-kind-parity.md) | tests | **유형 동일 코드 경로 방어선** — 프로덕션 변경 0 이 기대값 |
| 05 | [step-05-presentation-transition-presenter.md](step-05-presentation-transition-presenter.md) | presentation | `IStageTransitionView` + `StageTransitionPresenter` |
| 06 | [step-06-presentation-transition-view.md](step-06-presentation-transition-view.md) | presentation | `StageTransitionView` (PlayMode) |
| 07 | [step-07-presentation-table-slot-binding.md](step-07-presentation-table-slot-binding.md) | presentation | 자리 정의를 `StageConfig` 에서 바인딩 |
| 08 | [step-08-presentation-stage-progression-wiring.md](step-08-presentation-stage-progression-wiring.md) | presentation | `StageBootstrap` 진행 배선 (D5·D8) |
| 09 | [step-09-presentation-customer-kind-label.md](step-09-presentation-customer-kind-label.md) | presentation | 손님 유형 표시 |
| 10 | [step-10-data-balance-and-scene.md](step-10-data-balance-and-scene.md) | data + 씬 | 밸런스 애셋 4종 + 씬 조립 + 실측 |

## 병렬 실행 가능성

```
01 ─────────────────────────┐
02 → 03 → 05 → 06           │
04 (독립)                   ├→ 08 → 10
07 ─────────────────────────┘
09 (독립)
```

- **step-01 · step-02 · step-04 · step-07 · step-09 는 서로 독립**이다. 다만 07 과 08 은 같은 `StageBootstrap.cs` 를 고치므로 **반드시 직렬**이다 ([`rules/parallel-work.md`](../../.claude/rules/parallel-work.md) §3).
- step-06 은 step-05 의 인터페이스가 확정된 뒤.
- step-10 은 씬을 건드리므로 **혼자** 돈다. 씬 편집은 한 번에 한 워크트리만.

## 이 마일스톤에서 가장 조심할 것

1. **"소식은 싼 걸 안 먹어야 하지 않나?"** — 유형이 생기는 순간 이 오해가 코드로 들어간다. step-04 의 자격 테스트 2개가 유일한 방어선이다.
2. **유형별 `if` / 상속 클래스** — 생기면 설계 실패다. D9.
3. **공허하게 통과하는 유형 테스트** — 소식과 먹보를 비교할 때 두 손님의 SeqNo 를 선호 순서와 **엇갈리게** 준다. 나란히 두면 SeqNo 만 보는 구현으로도 통과한다 ([`rules/tests.md`](../../.claude/rules/tests.md) §3).
4. **스테이지 2·3 을 만들었는데 자리가 안 바뀐다** — D7. 지금 `TableSlots` 는 죽은 필드다.
5. **`Build()` 가 자기를 부른 프레젠터를 파괴** — D8.
