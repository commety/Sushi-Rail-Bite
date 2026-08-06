# M3 — 스테이지 클리어와 로그라이트

## 한 줄 요약

한 판이 **끝나는** 게임으로 만든다 — 제한 시간과 목표 매출로 클리어/실패를 판정하고, 클리어하면 보상을 골라 런의 덱이 자란다.

## 원문

[`docs/plan/M3-stage-and-roguelite.md`](../../docs/plan/M3-stage-and-roguelite.md) (원본 3차 목표).

> *"스테이지 목표를 달성하면 클리어한다 / 스테이지 종료 시 보상(초밥 카드 추가 또는 손님 영입)을 선택할 수 있다 / 초밥은 시너지를 가질 수 있다(선택사항)"*

---

## 착수 전 확정한 것 (사람 결정)

계획서 §열린 질문 중 "정해지지 않으면 설계를 시작할 수 없다" 고 표시된 것들이다.

| # | 질문 | 결정 |
|---|---|---|
| Q1 | 런 상태를 브라우저 종료 후에도 유지하나 | **유지하지 않는다.** 메모리만, 새로고침 시 초기화 |
| — | 실패 시 어떻게 되나 | **같은 스테이지 재시도.** 런은 유지된다 (덱·명부 보존) |
| — | 보상 후보를 어떻게 뽑나 | **주입된 난수원**(`IRandomSource`). preflight 난수 가드를 배정 경로로 재조준한다 |
| Q6 | 시너지 | **M3 밖.** `SushiTrait` 은 `None` 그대로 둔다 |
| — | 시간 초과 순간에 목표 달성 시 | **클리어** (D2). 판정이 매출을 먼저 본다 |
| — | 보상 후보 개수 | 기본안 **3 중 1**. 값은 step-09 에서 사람이 확정 |
| Q5 | 스테이지 2·3 밸런스 | **M3 밖.** 다음 스테이지는 M4 다 |

---

## 착수 시점의 현황 — 계획서와 다른 두 가지

**작업서를 쓰면서 코드를 조사해 확인한 것이다. 계획서에는 없다.**

### 1. "매출 집계" 는 이미 끝나 있다

계획서 작업 순서 1번(`SushiEatenEventChannelSO` 구독 → 가격 누적)은 M2 에서 완료됐다. `RevenueLedger`·`RecruitWallet` 이 있고, `ClaimCoordinator.ConsumeFinished` 가 소비 시점에 양쪽에 알리며, `StageHudView` 가 `매출 N/목표` 를 그리고, `RevenueLedgerTests`·`RecruitWalletTests` 가 Green 이다.

> **이벤트 채널을 경유하지 않는다.** 조율자가 원장·지갑을 직접 들고 있다 (M2 결정 — 구독 구조면 초기 예산과 매출 유래분이 한 스트림에 섞인다). `SushiEatenEventChannelSO` 는 `Runtime.Data` 에 존재하지만 아직 프로덕션 경로에서 쓰이지 않는다. **M3 에서 억지로 끼워 넣지 않는다.**

남은 것은 **시계·판정·런 상태·보상**이다.

### 2. 보상 풀이 비어 있다 — step-09 의 핵심 질문

현재 `Assets/Level/Balance/` 의 초밥 애셋은 5종(한치 120 · 연어 150 · 광어 150 · 방어 190 · 장어 300)이고, **다섯 개 전부 stage01 시작 덱(`SpawnTable`)에 들어 있다.** 손님도 2종(기본·소식) 뿐이며 둘 다 배치 가능하다.

즉 **지금 상태로는 보상으로 줄 새 카드가 하나도 없다.** 보상 화면이 빈 목록을 띄운다. 밸런스 애셋 결정(§7 사람 판단)이 필요하며 step-09 에서 다룬다.

---

## 아키텍처 결정

### D1 — 시계 · 판정 · 조율을 셋으로 나눈다

`StageClock`(시간만) / `StageEvaluator`(규칙만, 순수 정적) / `StageController`(둘을 합치고 조율자를 굴린다).

**이유**: 경계 규칙(D2)을 시간 없이 테스트하기 위해서다. 판정이 컨트롤러 안에 있으면 "정각에 목표 달성" 을 검증하려고 매번 시계를 정확히 감아야 한다.

### D2 — 경계는 "매출을 먼저 본다"

```
revenue >= target        → Cleared   (만료 여부와 무관)
아니고 expired           → Failed
그 외                    → InProgress
```

제한 시간 정각에 목표를 채우면 **클리어**다. 계획서의 테스트 이름 `Evaluate_TargetReachedExactlyAtTimeout_Clears` 가 이미 이 답을 담고 있었고, 그대로 채택한다.

### D3 — 틱 순서는 **조율자 → 시계 → 판정**

마지막 프레임에 입에 들어간 초밥의 매출을 인정한다. 시계를 먼저 흘려 만료시키면, 같은 프레임에 소비가 끝난 초밥이 판정에 반영되지 않아 "먹었는데 실패" 가 나온다.

판정이 끝난 뒤에는 **조율자를 틱하지 않는다.** 결과가 난 뒤에도 벨트가 흐르면 매출이 계속 오르고, 재시도 화면 뒤에서 시뮬레이션이 도는 상태가 된다.

### D4 — 덱의 진실을 `StageConfig` 에서 `RunState` 로 옮긴다

지금 `SushiBelt` 는 생성자에서 `config.SpawnTable` 을 직접 읽는다. 그대로 두면 **보상으로 얻은 초밥이 벨트에 영영 나오지 않는다** — 완료 판정 "고른 보상이 덱에 반영된다" 를 만족할 수 없다.

`StageConfig.SpawnTable` 은 **런의 시작 덱**으로 격하되고, 벨트는 주입받은 `SushiDeck` 을 읽는다. 이 전환은 동작 변화가 0인 별도 단계(step-04)로 분리한다.

### D5 — 난수는 주입한다. 전역 난수는 금지

보상 추첨은 이 프로젝트에서 **처음으로 정당한 난수**다. 다만:

- `IRandomSource` 를 주입한다. `UnityEngine.Random` · `System.Random` 은 `Runtime` 어디에서도 쓰지 않는다 — 시드를 못 잡으면 같은 런이 재현되지 않고 테스트가 통계 검증이 된다
- 구현은 자체 xorshift 한 개. 시드는 `RunState` 가 들고 있다
- **배정 경로에는 여전히 난수가 없다.** 순차번호가 모든 동률을 끝낸다 (`CLAUDE.md` §1.1-3b)

`tests/preflight.sh` 4번 가드가 지금 `Runtime` 전역에서 `Random\.` 을 잡는데, 이 가드에는 이미 **구멍**이 있다 — `new System.Random()` 은 `Random.` 형태가 아니라 통과한다. step-05 에서 **허용 목록 방식**(대역 산술 가드와 같은 형태)으로 재조준하며 그 구멍도 함께 막는다.

### D6 — `RunState` 는 `Build()` 밖에서 산다

`StageBootstrap.Build()` 는 원장·지갑·벨트·조율자·순차번호 발급기를 **매번 새로** 만든다 (스테이지마다 리셋 — M2 확정). 재시도는 `Build()` 를 다시 부르는 것이고, `RunState` 는 그 바깥에 있어 덱·명부가 살아남는다.

이 구조 덕분에 **재시도에 새 코드가 거의 없다.**

### D7 — 보상 화면은 MVP

`IRewardSelectionView`(인터페이스) / `RewardSelectionPresenter`(순수 C#, EditMode 테스트 대상) / `RewardSelectionView : MonoBehaviour`(TextMesh placeholder). Presenter 는 Unity API 를 모른다 (`CLAUDE.md` §3.6).

### D8 — 시너지는 M3 밖이다

`SushiTrait` 은 `None` 하나로 두고 손대지 않는다. 클리어 판정 + `RunState` + 보상 + UI 만으로 이미 9단계다. 시너지를 넣으면 Q6(발동 조건)·버프 중첩 규칙까지 이번에 정해야 하고, "버프가 배정 우선순위를 바꾸지 않는다" 를 검증할 테스트가 또 붙는다.

### D9 — 이미 가진 카드는 보상으로 제시하지 않는다

스폰 비중이 **가격에서만** 유도되므로(`share ∝ (최저가/가격)^α`) 중복 카드는 아무 효과가 없다. 손으로 적는 가중치 필드가 없다는 것이 곧 "중복이 무의미하다" 는 뜻이다.

### D10 — 저장 계층은 없지만, 나중이 싸게

`RunState` 는 Unity API 를 모르는 POCO 로 두고 SO 참조를 담는다. 나중에 저장이 필요해지면 `SushiData.Id` · `CustomerData.Id`(둘 다 이미 존재한다)로 매핑하는 계층만 얹으면 된다. **지금 Id 기반으로 짜지는 않는다** — 쓰지 않을 간접층이다.

---

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | `Runtime.Data` | `RewardCatalog` SO (보상 풀 · 제시 개수) |
| runtime | `Runtime` | 시계·판정·컨트롤러 / 덱·명부·런 상태 / 난수원 / 보상 생성 |
| presentation | `Presentation` | 남은 시간·결과 HUD, 보상 선택 화면(MVP), 부트스트랩 조립 |
| tests | `Tests.EditMode` · `Tests.PlayMode` | 판정·보상·Presenter 는 EditMode / 씬 흐름은 PlayMode 최소 |
| harness | `tests/` | preflight 난수 가드 재조준 |

**새 어셈블리를 만들지 않는다.** 새로 생기는 것은 `Runtime` 안의 폴더·네임스페이스 둘(`SushiDefense.Stages`, `SushiDefense.Run`)뿐이라 §7 승인 대상이 아니다.

## 의존성 그래프

```
SushiDefense.Data.StageConfig ─┐
SushiDefense.Data.RewardCatalog┤
                               ├→ SushiDefense.Run.RunState ─┐
SushiDefense.Data.SushiData ───┘   (SushiDeck · CustomerDeck) │
                                                              ├→ SushiDefense.Belt.SushiBelt
SushiDefense.Scoring.RevenueLedger ─┐                         │
SushiDefense.Customers.ClaimCoordinator ─┴→ SushiDefense.Stages.StageController
                                                              │
                        SushiDefense.Run.RewardGenerator ─────┤
                                                              ↓
                                        SushiDefense.UI.StageHudView
                                        SushiDefense.UI.RewardSelectionPresenter → IRewardSelectionView
```

역방향(로직 → UI) 참조는 없다. 컨트롤러는 `event Action<StageOutcome> OutcomeDecided` 로만 바깥에 알린다.

## 새 밸런스 수치

**값은 전부 step-09 에서 사람이 정한다.** 여기서는 필드만 정의한다 (`CLAUDE.md` §3.1·§7).

| 수치 | 들어갈 SO | 현재값 | 비고 |
|---|---|---|---|
| 목표 매출 | `StageConfig._targetRevenue` | **2500** | M2 에서 ⚠️ 로 넘어온 미결. 60초에 손님 하나로도 넘는다 → **실패가 구조적으로 불가능** |
| 제한 시간 | `StageConfig._timeLimitSeconds` | **60** | 이미 존재. M3 에서 처음으로 실제 의미를 갖는다 |
| 보상 풀 (초밥) | `RewardCatalog._sushiPool` | *(신규)* | 지금 후보가 0개다 — §현황 2 |
| 보상 풀 (손님) | `RewardCatalog._customerPool` | *(신규)* | 〃 |
| 제시 개수 | `RewardCatalog._offerCount` | *(신규)* | 기본안 3 |

## 단계

| # | 파일 | 영역 | 내용 |
|---|---|---|---|
| 01 | [step-01-data-reward-catalog.md](step-01-data-reward-catalog.md) | data | `RewardCatalog` SO 스키마 |
| 02 | [step-02-runtime-stage-outcome.md](step-02-runtime-stage-outcome.md) | runtime | `StageOutcome` · `StageClock` · `StageEvaluator` |
| 03 | [step-03-runtime-stage-controller.md](step-03-runtime-stage-controller.md) | runtime | `StageController` — 틱 계약과 정지 |
| 04 | [step-04-runtime-deck-injection.md](step-04-runtime-deck-injection.md) | runtime | `SushiDeck` 도입, 벨트가 덱을 주입받는다 (**동작 변화 0**) |
| 05 | [step-05-runtime-run-state.md](step-05-runtime-run-state.md) | runtime + harness | `CustomerDeck` · `RunState` · `IRandomSource` + preflight 가드 재조준 |
| 06 | [step-06-runtime-reward-generation.md](step-06-runtime-reward-generation.md) | runtime | `RewardKind` · `RewardOffer` · `RewardGenerator` · 보상 반영 |
| 07 | [step-07-presentation-stage-hud-result.md](step-07-presentation-stage-hud-result.md) | presentation | 남은 시간·결과 HUD + 부트스트랩 조립 + 재시도 |
| 08 | [step-08-presentation-reward-selection.md](step-08-presentation-reward-selection.md) | presentation | 보상 선택 화면 (MVP) |
| 09 | [step-09-balance-stage-values.md](step-09-balance-stage-values.md) | 밸런스 + 문서 | **사람이 값을 정한다** + 도메인 문서 승격 |

## 병렬 실행 가능성

```
01 ─────────────────────────────┐
02 → 03 ──────────────┐         │
04 → 05 → 06 ─────────┴→ 07 → 08 → 09
```

- **step-01 · step-02 · step-04 는 서로 독립**이라 병렬 가능하다 (data / stages / belt — 파일이 겹치지 않는다)
- step-03 은 step-02 뒤, step-05 는 step-04 뒤, step-06 은 step-01·step-05 뒤
- **step-07 부터는 직렬이다.** 셋 다 `StageBootstrap.cs` 와 씬을 건드린다 (`.claude/rules/parallel-work.md` §3 — 씬 편집은 한 번에 한 워크트리)

## 리스크

| 리스크 | 대응 |
|---|---|
| **목표 매출 2500 / 60초에서 실패가 나오지 않는다** → 판정이 죽은 코드가 된다 | step-09 에서 사람이 확정. EditMode 판정 테스트는 값과 무관하게 Green 이므로 **코드가 막히지는 않는다** |
| **보상 풀에 줄 카드가 없다** (§현황 2) | step-09. 시작 덱 축소 / 신규 애셋 추가 둘 다 §7 사람 판단 |
| 덱 주입 전환이 M1·M2 테스트를 깨뜨린다 | step-04 를 **동작 변화 0** 으로 못박고, 전량 Green 을 완료 판정에 넣는다 |
| 난수 도입이 배정 결정성으로 새어 든다 | 허용 목록 가드가 `Runtime/Run/` 밖의 난수를 FAIL 로 잡는다. 주입한 시드로 보상 테스트도 결정적 |
| 판정 후에도 시뮬레이션이 돈다 | D3. `Tick_AfterDecided_RevenueStopsGrowing` 이 고정한다 |
| 보상 화면이 `StageHudView` 처럼 placeholder 로 굳는다 | M6(메인화면·덱빌딩)에서 교체될 것임을 클래스 주석에 명시 (`StageHudView` 의 선례를 따른다) |
