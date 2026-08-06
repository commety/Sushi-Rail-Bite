# Step 03: `StageController` — 틱 계약과 정지

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-02 완료 필요 (`StageClock` · `StageEvaluator` · `StageOutcome`)
- **후행 단계:** step-07 의 `StageBootstrap.Update` 가 이 클래스만 틱한다

---

## 목적

지금 `StageBootstrap.Update()` 가 `Coordinator.Tick(Time.deltaTime)` 을 직접 부른다. 그러면 **"언제 멈추나" 를 `MonoBehaviour` 가 판단**하게 되어 EditMode 로 검증할 수 없다.

이 단계는 시계·판정·조율자를 한 순수 클래스로 묶고, **틱 순서와 정지 조건을 계약으로 못박는다.** `ClaimCoordinator.Tick` 의 6단계 주석이 그랬듯, 순서 자체가 검증 대상이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Stages/StageController.cs` — **생성**
- `Assets/Tests/EditMode/Stages/StageControllerTests.cs` — **생성**

**`ClaimCoordinator.cs` 를 수정하지 않는다.** 조율자는 컨트롤러의 존재를 모른다.

### 핵심 심볼

```csharp
namespace SushiDefense.Stages
{
    /// <summary>
    /// 스테이지 한 판의 진행을 굴린다 — 시간을 흘리고, 결과가 나면 <b>멈춘다</b>.
    ///
    /// <para>
    /// <b>배정 규칙을 모른다.</b> 조율자에게 시간을 넘겨줄 뿐이며, 누가 무엇을 먹는지는
    /// <c>ClaimCoordinator</c> 의 몫이다.
    /// </para>
    /// </summary>
    public sealed class StageController
    {
        public StageController(ClaimCoordinator coordinator, RevenueLedger revenue, StageConfig config);

        /// <summary>지금까지의 판정. 아직 안 끝났으면 <c>InProgress</c>.</summary>
        public StageOutcome Outcome { get; }

        /// <summary>남은 시간(초). 0 아래로 내려가지 않는다.</summary>
        public float RemainingSeconds { get; }

        /// <summary>아직 진행 중인가. 표시·조립용 편의 프로퍼티다.</summary>
        public bool IsRunning { get; }

        /// <summary>결과가 확정됐다. <b>판당 정확히 한 번</b> 발생한다.</summary>
        public event Action<StageOutcome> OutcomeDecided;

        public void Tick(float deltaSeconds);
    }
}
```

### 틱 계약 (작업서 D3)

> **구현 중 정정됨.** 계약은 **"판정이 조율자 틱 뒤에 온다"** 하나다. 조율자와 시계의 상대 순서는 관측되지 않는다 — 서로의 값을 읽지 않으므로 판정이 마지막에 오는 한 결과가 같다. 아래 코드의 1·2 는 바꿔도 되지만 **3 은 1 뒤여야 한다.**

```csharp
public void Tick(float deltaSeconds)
{
    // 0) 이미 끝났으면 아무것도 하지 않는다. 결과가 난 뒤에도 벨트가 흐르면
    //    보상 화면 뒤에서 매출이 계속 오른다.
    if (Outcome != StageOutcome.InProgress) return;

    // 1) 조율자를 먼저 굴린다. 마지막 프레임에 입에 들어간 초밥의 매출을 인정한다.
    //    시계를 먼저 흘려 만료시키면 "먹었는데 실패" 가 나온다.
    _coordinator.Tick(deltaSeconds);

    // 2) 시간을 흘린다.
    _clock.Advance(deltaSeconds);

    // 3) 판정. InProgress 가 아니게 된 그 틱에만 알린다.
    var outcome = StageEvaluator.Evaluate(_revenue.Total, _config.TargetRevenue, _clock.IsExpired);
    if (outcome == StageOutcome.InProgress) return;

    Outcome = outcome;
    OutcomeDecided?.Invoke(outcome);
}
```

**`OutcomeDecided` 를 두 번 쏘지 않는다.** 0번의 조기 반환이 그것을 보장하므로, 별도의 `_raised` 플래그를 두지 않는다 — 두 장치가 같은 것을 지키면 나중에 한쪽만 고쳐진다.

### 왜 컨트롤러가 매출 원장을 들고 있나

`StageEvaluator` 가 `revenue` 를 인자로 받으므로 누군가는 현재 매출을 읽어야 한다. 원장을 구독(`TotalChanged`)해 캐시하는 방법도 있지만 **쓰지 않는다** — 구독하면 해제 경로(`Dispose`)가 늘고, 매출이 바뀌지 않은 프레임에 만료로 실패하는 경우를 따로 처리해야 한다. 틱마다 `int` 하나를 읽는 편이 싸고 정확하다.

### 선행 산출물 의존성

- `StageClock` · `StageEvaluator` · `StageOutcome` — step-02
- `ClaimCoordinator` · `RevenueLedger` · `StageConfig` — 이미 있다

### 밸런스 수치

**없다.** 제한 시간·목표 매출은 `StageConfig` 에서 읽는다. 이 파일에 숫자 상수가 없어야 한다 (`CLAUDE.md` §3.1).

### 제약

- 로직은 `MonoBehaviour` 밖 순수 C#
- **`ClaimCoordinator.cs` 를 수정하지 않는다.** `git diff --stat` 으로 확인한다
- `Time.deltaTime` 을 읽지 않는다
- 조율자를 `Dispose` 하지 않는다 — 조율자의 수명은 이 클래스가 아니라 `StageBootstrap.Teardown` 이 쥔다. 여기서 함께 버리면 소유권이 둘이 된다
- `Random` 을 쓰지 않는다
- `Runtime` → `Presentation` 참조 금지

### 완료 판정

- [ ] `git diff --stat Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — **0줄**
- [ ] `grep -rn "Time\.\|Random\|Dispose" Assets/Code/Scripts/Runtime/Stages/StageController.cs` — **0건**
- [ ] `grep -rn "[0-9]" Assets/Code/Scripts/Runtime/Stages/StageController.cs | grep -vE '(///|//|\*)'` — 숫자 리터럴이 **0건**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh --fast` 전 항목 PASS

### 테스트 이름

```
StageControllerTests
  Tick_BelowTargetWithinTime_StaysInProgress
  Tick_TargetReached_OutcomeIsCleared
  Tick_TimeoutWithoutTarget_OutcomeIsFailed
  Tick_TargetReached_RaisesOutcomeDecidedOnce         ← 두 번 쏘지 않는다
  Tick_AfterDecided_RevenueStopsGrowing               ← ★ 정지 계약
  Tick_AfterDecided_RaisesNothingMore
  Tick_TargetReachedOnTheExpiringFrame_Clears         ← ★ D2 · D3 이 함께 걸린다
  RemainingSeconds_Midway_ReportsRemainder
  Tick_Negative_Throws                                 ← 시계가 던지는 것을 삼키지 않는다
```

> **★ `Tick_AfterDecided_RevenueStopsGrowing`** — 판정이 난 뒤 충분히 오래 틱하고 `Revenue.Total` 이 그대로인지 본다. `Outcome` 만 확인하면 조율자가 계속 도는 구현도 통과한다.
>
> **★ `Tick_TargetReachedOnTheExpiringFrame_Clears`** 가 이 마일스톤에서 가장 미묘한 지점이다. **마지막 초밥의 소비가 끝나는 프레임과 시계가 만료되는 프레임을 일치**시켜야 한다. 목표 매출을 "그 초밥 하나를 먹어야 닿는 값" 으로 잡고, 제한 시간을 그 소비 완료 시각에 맞춘다. 순서를 뒤집은 구현(시계 → 조율자)이면 `Failed` 가 나온다.
>
> 이 테스트를 짜기 위해 실제 벨트·손님을 세워야 한다. `ClaimCoordinatorTests` 의 SetUp 을 참고하되 **복사하지 말고**, 필요한 최소만 세운다. 손님의 대역은 `100~300` 으로 둔다 — `0~0` 이면 모든 초밥이 대역 밖이라 전부 이탈 직전까지 유예되어 타이밍이 예측 불가능해진다 (M2.5 에서 두 테스트 파일이 이 함정에 빠졌다).

### 예상 커밋 메시지

```
feat(stage): stop the simulation once the outcome is decided
```

---

## 금지 사항

- 틱 순서를 바꾸지 않는다. 조율자 → 시계 → 판정이다
- `_raised` 같은 중복 방어 플래그를 넣지 않는다. 조기 반환이 이미 보장한다
- 재시도(`Retry`)를 여기 넣지 않는다. 재시도는 `StageBootstrap.Build()` 재호출이며 step-07 이다
- 보상 생성을 여기서 부르지 않는다. 컨트롤러는 "끝났다" 만 알린다 — 무엇을 줄지는 step-06 이 답한다
- `ClaimCoordinator` 를 수정하지 않는다
- `Presentation` 을 수정하지 않는다
