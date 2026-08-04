# Step 06: 조율자 — `ClaimCoordinator`

- **영역:** `runtime` — 어셈블리 `Runtime` (+ `Tests.EditMode`)
- **선행 단계:** step-02(`SushiBelt`) 와 step-05(`SushiClaimResolver`) **둘 다** 완료 필요
- **후행 단계:** step-07 뷰가 이 조율자를 구동하고, step-08 배치가 `PlaceCustomer` 를 부른다

---

## 목적

지금까지 만든 조각(벨트·자격·후보·배정)은 서로를 모른다. 이 단계가 **재배정이 필요한 순간**을 하나의 진입점으로 묶는다 ([`sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §4).

| 트리거 | 처리 |
|---|---|
| 초밥이 손님 범위에 **진입** | 해당 손님 `CandidateSet.Recognize` |
| 초밥이 **소비됨** | 모든 손님 후보에서 `Forget` |
| 초밥이 **벨트에서 제거됨** (끝점) | 모든 손님 후보에서 `Forget` |
| 손님이 `Eating → Idle` | 자격 회복 → 재배정 대상 |
| 손님이 **자격 상실** | `CandidateSet.Clear` |
| **손님이 새로 배치됨** | 신규 손님을 대상에 추가 — **Q4 결정으로 M1 에 실재하는 경로다** |

여기까지 순수 C# 이다. `MonoBehaviour` 는 다음 단계에서 이 조율자의 `Tick` 을 부르기만 한다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD)

1. 타입·시그니처를 만든다 (`Tick` 본문 비움)
2. 통합 성격의 테스트를 쓰고 **Red 확인**
3. 구현 → 리팩터

### 생성 파일

```
Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs        — 생성
Assets/Tests/EditMode/Customers/ClaimCoordinatorTests.cs         — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>
    /// 벨트·자격·후보·배정을 잇는 순수 오케스트레이터. 재배정이 필요한 순간을
    /// 한곳으로 모아 두어, 트리거가 늘 때 흩어지지 않게 한다 (sushi-claim-flow §4).
    /// </summary>
    public sealed class ClaimCoordinator
    {
        public IReadOnlyList<CustomerLogic> Customers { get; }

        /// <summary>배정이 확정됐다. 뷰가 먹는 연출을 시작할 지점이다 (M1 은 즉시 소비).</summary>
        public event Action<CustomerLogic, SushiItem> SushiClaimed;

        public ClaimCoordinator(SushiBelt belt, StageConfig config);

        /// <summary>스테이지 진행 중에도 부를 수 있다 (Q4 결정). 즉시 재배정 대상이 된다.</summary>
        public void PlaceCustomer(CustomerLogic customer);

        /// <summary>배치를 취소한다. 후보를 비우고 대상에서 뺀다.</summary>
        public void RemoveCustomer(CustomerLogic customer);

        /// <summary>
        /// 시간을 흘린다. 벨트 Tick → 진입 인식 → 래치 만료 → 배정 순으로 돈다.
        /// 이 순서가 뒤집히면 "방금 들어온 초밥을 한 틱 놓치는" 그림이 나온다.
        /// </summary>
        public void Tick(float deltaSeconds);
    }
}
```

### 틱 순서 — 이 순서가 계약이다

```
1) belt.Tick(delta)                        스폰·이동·끝점 반납
2) 제거된 초밥을 모든 후보에서 Forget       (belt.SushiRemoved 구독)
3) 손님별 진입 인식                        ReachWindow 로 지금 범위 안인 초밥을 Recognize
4) CandidateSet.ExpireOlderThan(now)       래치 시간 상한 (Q9)
5) 자격 잃은 손님의 후보 Clear
6) resolver.Resolve(...)                   배정 확정
7) 확정된 쌍마다 SushiClaimed 발행 + belt.Remove(sushi)
```

- **4번이 6번보다 앞이어야 한다.** 만료된 후보가 배정에 들어가면 상한이 무의미해진다
- **2번이 3번보다 앞이어야 한다.** 끝점에서 내려간 초밥이 같은 틱에 인식되면 유령 배정이 된다
- M1 은 **먹는 시간이 없다** — 배정 즉시 소비로 처리하고 초밥을 벨트에서 내린다. `Eating` 상태 체류와 타이머는 M2 다. 이 단순화를 `///` 주석에 남긴다

### 선행 산출물 의존성

- step-02 — `SushiBelt` (+ `SushiSpawned`·`SushiRemoved` 이벤트)
- step-03 — `CustomerLogic`
- step-04 — `ReachWindow` · `CandidateSet`
- step-05 — `SushiClaimResolver`
- step-01 — `StageConfig.RecognitionLatchSeconds`

### 밸런스 수치

`RecognitionLatchSeconds` 만 읽어 `CandidateSet` 에 넘긴다. 그 외 상수 금지.

### 테스트 항목 (`Tests.EditMode`)

```
ClaimCoordinatorTests
  Tick_SushiEntersReach_IsRecognized
  Tick_SushiInReach_GetsClaimed
  Tick_SushiClaimed_RaisesSushiClaimed
  Tick_SushiClaimed_RemovedFromBelt
  Tick_SushiRemovedAtBeltEnd_ForgottenByAllCustomers
  Tick_SushiClaimedByOne_ForgottenByOthers            ← 플랜 OnTaken_...
  Tick_LatchExpiredBeforeResolve_NotClaimed           ← 틱 순서 4→6 계약
  Tick_RemovedBeforeRecognize_NoGhostClaim            ← 틱 순서 2→3 계약
  Tick_CustomerLosesEligibility_CandidatesCleared

  PlaceCustomer_MidStage_ParticipatesInNextResolve    ← Q4 결정
  PlaceCustomer_MidStage_DoesNotDisturbExistingClaims
  RemoveCustomer_ClearsCandidatesAndStopsClaiming

  Tick_SameScenarioTwice_ProducesIdenticalClaims      ← 결정성
  Tick_SushiInReachAndCustomerIdle_NeverSkipsAFrame   ← "구경하지 않는다"
```

### 제약

- **`MonoBehaviour` 금지.** 조율자는 순수 클래스다 — 이게 M1 통합 동작을 EditMode 로 검증할 수 있게 만드는 지점이다
- **`Time.*` 금지.** 시간은 `Tick(deltaSeconds)` 로만
- `Tick` 안에서 할당 금지 — 결과 버퍼·후보 딕셔너리를 필드로 재사용한다 (§4.3)
- 이벤트 구독(`belt.SushiRemoved += ...`)을 만들면 해제 경로도 만든다 (`.claude/rules/scripts.md` §6)
- `Runtime` 은 `Presentation` 을 참조하지 않는다
- **점수를 집계하지 않는다.** `SushiEatenEventChannelSO` 를 발행하지 않는다 — M2 다

### 완료 판정

- [ ] `grep -rn "class ClaimCoordinator" Assets/Code/Scripts/Runtime/Customers/` → 1건
- [ ] `grep -rn "MonoBehaviour\|Time\.\|Random" Assets/Code/Scripts/Runtime/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건**
- [ ] `grep -rn "SushiEatenEventChannelSO" Assets/Code/Scripts/Runtime/` | 주석 제외 → 0건 (M1 범위)
- [ ] `./tests/run-tests.sh` — 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(customer): add claim coordinator wiring belt, candidates and resolver
```

---

## 금지 사항

- 틱 순서를 임의로 바꾸지 않는다. 두 계약(2→3, 4→6)은 테스트로 고정돼 있다.
- 먹는 시간·포화도 증가·소화 타이머를 넣지 않는다 (M2).
- `MonoBehaviour` 를 만들지 않는다 (step-07).
