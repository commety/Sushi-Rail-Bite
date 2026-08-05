# Step 09: 조율자 배선 — 먹는 시간 · 소비 · 경제

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-03 · step-05 · step-06 · step-08 **전부 완료 필요**
- **후행 단계:** step-10 이 이 조율자의 이벤트를 화면에 띄운다

---

## 목적

네 갈래로 나눠 만든 것을 `ClaimCoordinator` 하나에 모은다. **M1 의 소비 시점이 바뀌는 것**이 이 단계의 본질이다.

### M1 → M2 의 변화

M1 은 배정 확정이 곧 소비였다:

```csharp
SushiClaimed?.Invoke(claim.Customer, claim.Sushi);
_belt.Remove(claim.Sushi);          // ← 즉시
```

M2 는 그 사이에 먹는 시간이 들어간다:

```
배정 확정  →  Eating 진입. 초밥은 Claimed 로 벨트에 남는다
   ↓ eatSeconds 경과
소비 확정  →  포화도 증가 · 매출/재화 누적 · 벨트에서 제거 · 포화면 Digesting
```

**초밥은 먹는 동안 벨트 위에 남는다.** `Claimed` 상태라 다른 손님이 가져갈 수 없고(`TryClaim` 이 막는다), 끝점에 닿으면 M1 그대로 반납된다 — 즉 **먹다 만 초밥이 끝점을 지나면 놓친다.** 의도된 동작이며 테스트로 고정한다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — 수정 (핵심)
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (원장·지갑 조립, 스테이지 리셋)
- `Assets/Tests/EditMode/Customers/ClaimCoordinatorTests.cs` — 수정
- `Assets/Tests/PlayMode/StageIntegrationTests.cs` — 수정 (스테이지 리셋 회귀)

### 핵심 심볼

```csharp
public sealed class ClaimCoordinator
{
    public ClaimCoordinator(SushiBelt belt, StageConfig config,
                            RevenueLedger revenue, RecruitWallet wallet);

    /// <summary>배정이 확정됐다 — 먹기 시작 시점. 뷰가 먹는 연출을 켠다.</summary>
    public event Action<CustomerLogic, SushiItem> SushiClaimed;

    /// <summary>소비가 끝났다 — 매출·재화가 이미 반영된 뒤다. 뷰가 연출을 끈다.</summary>
    public event Action<CustomerLogic, SushiItem> SushiEaten;
}
```

`PlaceCustomer` 는 손님마다 `CustomerAppetiteMachine` 을 만들어 들고, `RemoveCustomer` 는 그 구독을 끊는다.

### 틱 순서 계약 (M1 5단계 → M2 6단계)

M1 의 순서를 유지하고 **식욕 진행을 앞에 끼워 넣는다.**

```
1) belt.Tick(delta)          벨트 진행 · 스폰 · 끝점 반납
2) TickAppetites(delta)      ← M2 신규. 먹기·소화 진행. 완료 시 소비 확정
3) RecognizeDueEntries()     기한이 된 진입 예약 → 인식
4) ExpireCandidates()        래치 만료
5) SyncEligibility()         자격 상실 → 후보 비움 / 자격 회복 → 재예약
6) ResolveClaims()           배정 확정 → 먹기 시작
```

**2가 5보다 앞이어야 한다.** 먹기가 끝나 `Idle` 로 돌아온 손님이 **같은 틱에** 자격을 회복하고 배정에 참여한다. 뒤에 두면 손님이 매번 한 틱씩 굶는다.

**2가 6보다 앞이어야 한다.** 같은 틱에 먹기 완료와 새 배정이 겹치면 완료가 먼저다 — 아니면 `Eating` 상태라 배정을 못 받는다.

이 두 문장을 **코드 주석으로 남긴다.** M1 이 그렇게 했고, 순서가 계약이라는 것이 파일만 봐서는 드러나지 않는다.

### 소비 확정 시 하는 일 (순서 고정)

```
1) sushi.TryConsume()            상태 Claimed → Consumed
2) revenue.Add(price)            매출
3) wallet.AccrueFrom(price)      영입 재화 — 원장을 구독하지 않는다 (D5)
4) belt.Remove(sushi)            벨트에서 내림 → 모든 손님 후보에서 제거
5) SushiEaten 발행
```

포화도 증가는 `CustomerAppetiteMachine` 안에서 이미 끝나 있다 — 여기서 다시 만지지 않는다.

### 선행 산출물 의존성

- `SushiDefense.Customers.CustomerAppetiteMachine` — step-06
- `SushiDefense.Scoring.RevenueLedger` · `RecruitWallet` — step-07
- `SushiDefense.Customers.CustomerPlacementService` (지갑 인자) — step-08
- `SushiDefense.Belt.SpawnSequence` (credit 배출) — step-03
- `SushiDefense.Customers.ClaimPairComparer` (4단 정렬) — step-05

### 밸런스 수치

- 없음. 전부 SO 에서 온 값을 앞 단계가 이미 읽었다
- `StageBootstrap.Build()` 가 `new RecruitWallet(_stageConfig.InitialRecruitBudget)` 으로 지갑을 연다

### 스테이지 리셋 (착수 시 확정 — 열린 질문 2·5)

`StageBootstrap.Build()` 는 이미 **호출마다 새 `SequenceNumberIssuer` 2개**를 만든다. 즉 순차번호는 이미 스테이지 리셋이다. M2 는 여기에 원장·지갑·스폰 수열을 더한다.

- 순차번호(초밥·손님) — 스테이지 시작 시 0 부터
- 스폰 수열 credit — 스테이지 시작 시 0
- 매출 — 스테이지 시작 시 0
- 영입 재화 — 스테이지 시작 시 `InitialRecruitBudget` (**이월 없음**)

`Build()` 를 두 번 부르면 네 가지가 전부 초기 상태로 돌아가야 한다. **테스트로 고정한다.**

### 제약

- **`Dispose()` 에서 식욕 머신 구독을 전부 해제한다.** 손님마다 `EatingFinished` 를 구독하므로 M1 의 벨트 구독 해제만으로는 부족하다 (`.claude/rules/scripts.md` §6)
- **`Tick` 안에서 할당하지 않는다.** 손님별 머신은 `PlaceCustomer` 에서 만들어 딕셔너리에 둔다. 소비 처리용 임시 리스트도 필드로 잡는다
- **매 프레임 손님 × 초밥 전수 순회를 만들지 않는다.** M1 의 예약(`PendingEntry`) 구조를 그대로 유지한다
- **`SushiClaimResolver` · `CandidateSet` · `CustomerLogic` 을 수정하지 않는다**
- `Random` 을 쓰지 않는다

### 완료 판정

- [ ] `grep -n "TickAppetites\|CustomerAppetiteMachine" Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — 배선 확인
- [ ] `grep -rn "Targeting\|Price" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] `git diff --stat Assets/Code/Scripts/Runtime/Customers/SushiClaimResolver.cs Assets/Code/Scripts/Runtime/Customers/CandidateSet.cs Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs` — **변경 0줄**
- [ ] `grep -rn "Random" Assets/Code/Scripts/Runtime/` — **0건**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS
- [ ] `./scripts/run.sh webgl` 빌드 성공 유지 (M1 완료 판정 계승)

### 테스트 이름

```
먹는 시간
  Tick_ClaimAssigned_CustomerEntersEating
  Tick_BeforeEatSeconds_SushiStaysOnBelt           ← 먹는 동안 벨트 위에 남는다
  Tick_EatSecondsElapsed_SushiRemovedFromBelt
  Tick_EatingCustomer_NotAssignedAnotherSushi      ← 한 번에 하나
  Tick_ClaimedSushiReachesBeltEnd_IsLostNotEaten   ← 놓치는 것이 의도된 동작

경제 반영
  Tick_SushiEaten_AddsPriceToRevenue
  Tick_SushiEaten_AccruesRecruitCurrency
  Tick_SushiClaimedNotYetEaten_RevenueUnchanged

틱 순서 계약
  Tick_EatingFinishesThisTick_CustomerClaimsAgainSameTick   ← 2가 5·6보다 앞
  Tick_SaturationFull_StopsClaimingUntilDigested
  Tick_DigestionComplete_ResumesClaimingSameTick

스테이지 리셋
  Build_CalledTwice_ResetsSequenceNumbers
  Build_CalledTwice_ResetsRevenueAndWallet
  Build_CalledTwice_ResetsSpawnSequence

수명
  Dispose_AfterPlacements_UnsubscribesAllAppetiteMachines
```

`Tick_EatingFinishesThisTick_CustomerClaimsAgainSameTick` 이 **틱 순서 계약의 회귀선**이다. 2를 5·6 뒤로 옮기면 이 테스트만 깨진다.

> **M1 의 교훈:** 풀 재사용 때문에 `SushiItem` 인스턴스 참조로는 초밥을 식별할 수 없다. 테스트에서는 **`SequenceNumber` 로 식별**한다 — `ClaimCoordinatorTests.cs` 헤더에 그 이유가 이미 적혀 있다. 또 긴 시간을 흘리면 초밥이 여러 개 스폰되므로, 시나리오마다 `SpawnIntervalSeconds` · `BeltLength` 를 좁게 잡은 전용 `StageConfig` 를 쓴다.

### 예상 커밋 메시지

```
feat(customer): wire eating duration, consumption and economy into claim flow
```

---

## 금지 사항

- `SushiClaimResolver` · `CandidateSet` · `CustomerLogic` · `CustomerAppetiteMachine` 을 수정하지 않는다. 이 단계는 **배선만** 한다
- 매 프레임 전수 순회로 되돌리지 않는다. 예약 구조가 불편하면 멈추고 보고한다
- View 를 건드리지 않는다. 화면은 step-10 이다
- 밸런스 애셋(`Assets/Level/Balance/*.asset`)을 수정하지 않는다 (§7)
