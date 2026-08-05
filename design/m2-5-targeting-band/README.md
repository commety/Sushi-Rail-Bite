# M2.5 — 타겟팅 대역 전환

## 한 줄 요약

타겟팅을 **단일 가격 + 대칭 거리**에서 **대역(`min`~`max`) + 마감시한**으로 바꾼다 — 손님이 대역 밖 초밥밖에 없을 때 즉시 물지 않고 **범위를 벗어나기 직전까지 기다린다.**

## 원문

[`docs/plan/M2.5-targeting-band.md`](../../docs/plan/M2.5-targeting-band.md) — 원본에 없던 **기획 변경**이다.

바꾸는 이유 둘:

1. **대칭 거리가 예측되지 않는다.** 타겟팅 200 인 손님이 199 를 250 보다 먼저 집는다. 플레이어에게 *"200엔대를 좋아한다"* 고 설명한 뒤 이 동작을 보여 주면 규칙이 학습되지 않는다
2. **즉시 확정(eager commit) 때문에 정렬 키가 작동할 기회가 없다.** `ResolveClaims()` 가 매 틱 그 순간의 후보만으로 확정하므로, 싼 초밥이 먼저 들어오면 후보가 그것 하나뿐이라 "소식좌가 싼 걸 먼저 집는" 그림이 나온다. **대역만 넣고 이걸 안 고치면 증상은 그대로다**

---

## 착수 전 발견 — 계획서가 예상하지 못한 산술 충돌

**현재 밸런스에서는 마감시한이 절대 도달하지 않는다.**

| 값 | 출처 | 크기 |
|---|---|---|
| 범위 통과 시간 `2 × Reach / BeltSpeed` | `Customer.Standard._reach: 3`, `Stage01._beltSpeed: 2` | **3.0초** |
| 인식 래치 상한 | `Stage01._recognitionLatchSeconds: 1` | **1.0초** |

`CandidateSet.ExpireOlderThan` 은 **인식 시각 기준**으로 만료시킨다 (`now − recognizedAt > latch`). 즉 후보가 범위 안에 멀쩡히 있는데도 **인식 1초 뒤에 후보에서 빠진다.** 대역 밖 초밥을 기다리게 만들면 그 초밥은 이탈 2초 전에 사라지고, 손님은 아무것도 못 집는다 — 계획서가 최대 리스크로 지목한 *"손님이 굶는다"* 가 밸런스 값이 아니라 **래치 계산식 때문에** 발생한다.

→ **결정 D4** 로 다룬다. `.claude/rules/parallel-work.md` 성격의 잡음이 아니라 M1 계약 변경이라 아키텍트 확인이 필요하다.

---

## 아키텍처 결정

### D1 — 자격은 이번에도 손대지 않는다. 다만 불변식 문구가 바뀐다

M2 까지의 불변식 *"가격은 자격에 절대 들어가지 않는다"* 를 **문자 그대로는 유지하지 못한다.** 마감시한이 대역(=가격)을 보고 **언제** 집을지를 바꾸기 때문이다. 대신:

> **모든 손님은 유한 시간 안에 반드시 집는다.**
> 대역은 *무엇을 · 언제* 를 정할 뿐, **영구 배제는 금지**다.

코드 수준의 방어선은 그대로다:

- `CustomerLogic.CanAcceptSushi` · `IsInReach` · `CanTake` 는 **한 줄도 바뀌지 않는다**
- `CustomerLogic.cs` 에 `Price` · `Targeting` 이 등장하면 위반 — 매 단계 grep
- `CustomerAppetiteMachine` 은 여전히 원시 값(`int saturationAmount`)만 받는다

### D2 — 대역 산술은 `TargetingPriority` 한 파일에만 둔다

M2 가 세운 **"가격을 읽는 곳은 넷뿐이다"** 표를 그대로 유지한다. `BandDistance` 도 `BandWidth` 도 이 파일에 넣는다 — 세 줄짜리 계산에 타입을 하나 쓰는 이유는 M2 때와 같다: **다음 사람이 같은 식을 자격 판정에 복사해 넣어도 grep 으로 잡히게** 하기 위해서다.

| 파일 | 무엇을 위해 | 변화 |
|---|---|---|
| `Belt/SpawnShareTable` | `(덱 내 최저가/가격)^α` | 없음 |
| `Customers/TargetingPriority` | **대역 거리 · 대역 폭** | **교체** |
| `Customers/ClaimPairComparer` | 위 값을 정렬 키로 | 키 1 교체 + 키 4 삽입 |
| `Customers/ClaimCoordinator` | 소비 시점 매출·재화 | 없음 |
| *(신규)* `Customers/ClaimDeadline` | 대역 안 여부 판정 | **다섯 번째 지점** |

`ClaimDeadline` 이 다섯 번째가 되는 것은 불가피하다 — "대역 안이냐"를 물어야 즉시 확정을 결정할 수 있다. 다만 **계산식은 갖지 않는다**: `TargetingPriority` 와 `ClaimPairComparer` 를 경유한다.

### D3 — 마감시한 판정을 `SushiClaimResolver` 에 넣지 않는다

계획서의 지시이자 M2 의 D3(정렬 키는 한 파일)과 같은 논리다. **랭킹과 타이밍은 다른 관심사다.**

```
ResolveClaims()
  ├ ClaimDeadline 이 손님을 거른다   ← 신규. "지금 확정할까"
  └ SushiClaimResolver.Resolve(거른 손님, …)   ← 변경 0줄. "누가 무엇을"
```

리졸버는 `IReadOnlyList<CustomerLogic> customers` 를 받으므로 **필터링된 리스트를 넘기는 것만으로** 끝난다. `SushiClaimResolver.cs` 는 이번 마일스톤에서 **수정 금지**다.

`ClaimDeadline` 은 "이 손님의 최선 후보"를 골라야 하는데, 그 순서는 `ClaimPairComparer` 를 그대로 재사용한다 (같은 손님끼리 비교하면 키 4·5 가 동률이라 키 1~3 만 작동한다). 정렬 키를 두 번 쓰지 않기 위해서다.

### D4 — 래치를 **이탈 시각 기준**으로 바꾼다 *(사람 판단으로 확정, 2026-08-05)*

위 산술 충돌의 해법이다. 래치의 *역할* 은 계획서가 정의한 그대로 두되, *계산 기준* 을 바꾼다.

| | 지금 (M1) | D4 이후 |
|---|---|---|
| 만료 조건 | `now − 인식시각 > latch` | `now − **이탈시각** > latch` |
| latch = 0 | 상한 없음 | 상한 없음 *(그대로)* |
| 의미 | "인식하고 N초" | **"범위를 벗어나고 N초"** |

**왜 이쪽이 맞나** — §1.1-3c 가 정의한 래치는 *"물리적으로 범위를 조금 벗어나도 후보에서 빼지 않는다"* 이다. **애초에 이탈 기준 개념이다.** 인식 기준 계산은 `latch ≥ 통과시간` 일 때만 그 의도와 일치하는 근사였고, 지금 stage01 은 그 조건을 만족하지 않는다.

**결정적인 이점**: D4 를 넣으면 마감시한(`now ≥ 이탈시각`)이 만료(`now > 이탈시각 + latch`)보다 **항상 먼저 온다.** 즉

> **불변식 "손님은 유한 시간 안에 반드시 집는다" 가 밸런스 값과 무관하게 구조적으로 성립한다.**

밸런스로 막으려면 `RecognitionLatchSeconds ≥ 2·maxReach/BeltSpeed` 라는, 문서에 적어도 다음 사람이 어기는 종류의 제약이 생긴다.

> **기각된 대안:** 래치 계산은 그대로 두고 `Stage01._recognitionLatchSeconds` 를 **1 → 3.5 이상**으로 올리는 안. 불변식을 값을 올바로 채워 넣는 사람에게 의존시키므로 채택하지 않았다. 판단 근거는 [step-04](step-04-runtime-exit-time.md) 에 남아 있다.

### D5 — "기다리는 중" 은 상태 머신에 넣지 않는다

`CustomerState` 에 `Waiting` 을 추가하지 않는다. 추가하면 `CanAcceptSushi` 가 `State == Idle` 을 보므로 **대기 중인 손님이 자격을 잃는다** — 즉 대기가 곧 굶기가 된다. `CustomerRuntimeState` 에 플래그를 넣는 것도 하지 않는다. 클레임 타이밍 개념이 손님 상태 객체로 새면, `CustomerLogic` 을 가격에서 떼어 놓은 M2 의 방어와 같은 종류의 누수가 생긴다.

→ **`ClaimCoordinator.IsWaiting(CustomerLogic)`** 진단 접근자로 노출한다. `CandidatesOf` · `EatingOf` 와 같은 패턴이다. 뷰까지 가는 배선은 step-06 이 맡는다.

### D6 — preflight 가드를 정규식에서 **허용 목록**으로 바꾼다

현재 검사는 패턴 기반이라 오탐·미탐이 둘 다 난다:

```bash
'Targeting.*(>|<|>=|<=).*(threshold|Threshold)|Abs\(.*Targeting.*\)\s*(>|>=)'
```

- **오탐**: `ClaimDeadline` 은 정당하게 대역을 보고 `false` 를 돌려준다
- **미탐**: 새 금지형 `if (BandDistance(...) > 0) return false;` 를 잡지 못한다

→ `BandDistance` · `TargetingMin` · `TargetingMax` 가 **허용 목록 밖 파일에 등장하면 FAIL.** 프로젝트가 이미 쓰는 "가격을 읽는 곳은 셀 수 있다" 교리와 같은 형태이고 패턴 취약성이 없다.

### D7 — 완료 판정 grep 은 주석을 제외한다 (M0~M2 그대로)

```
| grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'
```

---

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | `Runtime.Data` | `CustomerData` 단일 값 → 대역 |
| runtime | `Runtime` | 대역 거리·폭, 정렬 키 5단, 이탈 시각 보관, 마감시한 필터 |
| presentation | `Presentation` | 대역 표시, 대기 상태 표시 |
| tests | `Tests.EditMode` / `Tests.PlayMode` | 위 전부 |
| — | 하네스·문서 | preflight 가드 재조준, 문서 8종 일괄 갱신 |

**새 어셈블리는 만들지 않는다.** 새 파일은 `Runtime/Customers/ClaimDeadline.cs` 하나뿐이다.

## 의존성 그래프

```
SushiDefense.Data.CustomerData (대역)
        │
        ├─→ Customers.TargetingPriority.BandDistance / BandWidth
        │            │
        │            └─→ Customers.ClaimPairComparer (키 1·4)
        │                        │
        │            ┌───────────┴───────────┐
        │            ↓                       ↓
        │   Customers.ClaimDeadline    Customers.SushiClaimResolver  ← 변경 0줄
        │            │                       │
        │            └──────→ Customers.ClaimCoordinator ←───────────┘
        │                              ↑
        └──────────────────────────────┼─────→ SushiDefense.UI.CustomerView / StageHudView
                                       │
        Customers.ReachWindow.ExitSeconds → Customers.CandidateSet (이탈 시각)
```

`CustomerLogic` 은 이번에도 이 그래프의 **어느 노드와도 연결되지 않는다.**

## 새 밸런스 수치

**값은 사람이 정한다** (`CLAUDE.md` §7). 에이전트는 필드와 검증만 만든다.

| 수치 | 들어갈 SO | 현재 | 계획서 예시안 |
|---|---|---|---|
| `Customer.Standard` 대역 | `CustomerData` | `_targetingPrice: 120` | 기본 100~300 |
| `Customer.SmallEater` 대역 | `CustomerData` | `_targetingPrice: 300` | 소식 300~550 |
| `Customer.Placeholder` 대역 | `CustomerData` | `_targetingPrice: 100` | *(미정)* |
| `RecognitionLatchSeconds` | `StageConfig` | `1` | **D4 확정으로 손대지 않는다** |

> ⚠️ **step-01 을 하면 위 세 애셋의 타겟팅이 조용히 `0~0` 이 된다.** Unity 는 사라진 필드를 다음 직렬화에서 버리고 새 필드를 기본값으로 채운다. 대역 `0~0` 은 **모든 초밥이 대역 밖 + 폭 0(최강 전문가)** 이라 배정이 뒤집힌다. step-08 이 값을 채우기 전까지 stage01 은 의미 있는 동작을 하지 않는다 — **step-01 과 step-08 은 같은 PR 안에 있어야 한다.**
>
> ⚠️ 덱의 최고가는 **300**(`Sushi.Eel`)이다. 소식 대역 `300~550` 은 대역 안 후보가 **정확히 한 종류**뿐이라는 뜻이고, 상한 550 은 더 비싼 초밥이 생기기 전까지 아무 일도 하지 않는다. 의도된 희소성인지 step-08 에서 확인받는다.

## 단계

| # | 파일 | 영역 | 내용 |
|---|---|---|---|
| 00 | [step-00-tests-baseline.md](step-00-tests-baseline.md) | tests | **(선택)** 현재 동작 기준선 측정 |
| 01 | [step-01-data-targeting-band.md](step-01-data-targeting-band.md) | data | `CustomerData` 단일 값 → `min`/`max` |
| 02 | [step-02-runtime-band-distance.md](step-02-runtime-band-distance.md) | runtime | `TargetingPriority` — `BandDistance` · `BandWidth` |
| 03 | [step-03-runtime-claim-ranking.md](step-03-runtime-claim-ranking.md) | runtime | `ClaimPairComparer` 정렬 키 5단 |
| 04 | [step-04-runtime-exit-time.md](step-04-runtime-exit-time.md) | runtime | `CandidateSet` 이탈 시각 보관 + **D4 래치** |
| 05 | [step-05-runtime-claim-deadline.md](step-05-runtime-claim-deadline.md) | runtime | `ClaimDeadline` + 조율자 필터 + `IsWaiting` |
| 06 | [step-06-presentation-band-hud.md](step-06-presentation-band-hud.md) | presentation | 대역 표시 · 대기 표시 |
| 07 | [step-07-docs-and-harness.md](step-07-docs-and-harness.md) | 문서·하네스 | 문서 9종 일괄 갱신, preflight 가드 재조준 |
| 08 | [step-08-balance-band-values.md](step-08-balance-band-values.md) | 밸런스 | **사람 결정** — 대역 값 채우기 |

## 병렬 실행 가능성

```
00 (선택)
 │
 ├─→ 01 ─→ 02 ─→ 03 ─┐
 │                    ├─→ 05 ─→ 06 ─→ 07 ─→ 08
 └─→ 04 ──────────────┘
```

- **01 과 04 는 병렬 가능하다.** 파일이 겹치지 않는다:
  - 01·02·03 → `Runtime.Data/CustomerData/CustomerData.cs`, `Runtime/Customers/TargetingPriority.cs`, `ClaimPairComparer.cs`
  - 04 → `Runtime/Customers/CandidateSet.cs`, `ClaimCoordinator.cs`
- **05 는 03·04 가 모두 끝나야 시작한다.** `ClaimCoordinator.cs` 에 두 갈래가 모이므로 **04 와 05 를 병렬로 돌리지 않는다** (`.claude/rules/parallel-work.md` §3)
- 06·07·08 은 직렬

> 실제로는 **직렬 실행을 권한다.** 01 직후 컴파일이 깨진 상태(호출부 4곳)를 지나가야 하고, 병렬 워크트리 둘이 그 상태를 동시에 겪으면 어느 쪽 실패인지 구분이 안 된다.

## 검증 명령

```bash
./tests/preflight.sh
```

step-07 이 preflight 의 5번 검사(타겟팅 자격 오용)를 재조준하므로, **step-07 이전까지 이 항목이 오탐으로 FAIL 할 수 있다.** 그 경우 실패를 무시하지 말고 어떤 줄이 걸렸는지 보고한 뒤 진행한다 — 진짜 위반과 구분해야 한다.

## 리스크와 대응

| 리스크 | 대응 | 어느 단계 |
|---|---|---|
| **대역이 자격 게이트로 굳는다** — 최대 위험 | 불변식 테스트 `Claim_OnlyOutOfBandSushiInReach_TakesItBeforeExit` + D6 허용 목록 가드 | 05·07 |
| **래치가 마감시한보다 먼저 와 손님이 굶는다** | **이미 현재 밸런스에서 발생한다.** D4 가 구조적으로 막는다 | 04 |
| 마감시한 판정이 리졸버 안으로 들어간다 | D3 — `SushiClaimResolver.cs` **수정 금지**를 완료 판정에 넣었다 (`git diff` 0줄) | 05 |
| 대역 안에서 FIFO 로 되돌아간다 | 키 2(고가 우선) 유지. `Resolve_TwoInBandSushi_TakesHigherPrice` | 03 |
| 애셋 대역이 `0~0` 인 채로 남는다 | `Stage01SceneTests` 에 대역 검증 추가 + step-08 | 06·08 |
| 문서만 바뀌고 코드가 안 바뀐다 (또는 반대) | **같은 브랜치·같은 PR.** step-07 이 문서 9종을 한 번에 뒤집는다 | 07 |
| 전문가가 배치 순서에 밀린다 | 키 4(대역 폭)가 키 5(손님 SeqNo)보다 위 | 03 |
| 대기가 손해로만 작동한다 | 대기 중 다른 손님이 그 초밥을 가져갈 수 있다 — **의도된 비용**이다. 테스트로 고정하지는 않되 도메인 문서에 남긴다 | 05·07 |

## 계획서와 달라지는 지점

작업 중 발견해 **의도적으로 바꾼 것들**이다. 계획서를 그대로 따르지 않았으므로 여기 모아 둔다.

| # | 계획서 | 이 작업서 | 왜 |
|---|---|---|---|
| 1 | *"래치는 손대지 않는다"* | **래치 만료를 이탈 기준으로** (D4) | 현재 밸런스에서 마감시한이 도달 불가. 계획서의 최대 리스크가 밸런스가 아니라 계산식에서 발생 |
| 2 | `StageHudView` — 대역 표시, 대기 표시 | **대역·대기는 `CustomerView`**, HUD 는 대기 인원 수 | 대역은 손님별 값이다. 전역 HUD 에 넣으면 손님 셋의 대역을 구분해 보여 줄 수 없다 |
| 3 | preflight 가드 *"재조준"* | **정규식 → 허용 목록** (D6) | 새 금지형을 정규식으로 잡으려면 `ClaimDeadline` 이 오탐된다 |
| 4 | 산출물 표에 `TargetingPriority.BandDistance` 만 | **`BandWidth` 도 같은 파일** | 대역 폭이 정렬 키 4가 되므로 대역 산술이 두 파일로 흩어지는 것을 막는다 |
