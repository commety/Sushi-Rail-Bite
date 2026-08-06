# Step 02: 시계와 판정 규칙 — `StageClock` · `StageEvaluator`

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** 없음. step-01 · step-04 와 **병렬 가능**
- **후행 단계:** step-03 이 둘을 합쳐 `StageController` 를 만든다

---

## 목적

"몇 초 남았나" 와 "지금 판정하면 뭐가 나오나" 를 **서로 모르는 두 조각**으로 만든다.

판정을 컨트롤러 안에 두면 경계 규칙(제한 시간 정각에 목표 달성)을 검증할 때마다 시계를 정확히 감아야 하고, 그 테스트는 시간 계산이 조금만 어긋나도 함께 깨진다. 규칙을 순수 함수로 떼어 내면 **규칙만 단독으로 고정**된다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Stages/StageOutcome.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Stages/StageClock.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Stages/StageEvaluator.cs` — **생성**
- `Assets/Tests/EditMode/Stages/StageClockTests.cs` — **생성**
- `Assets/Tests/EditMode/Stages/StageEvaluatorTests.cs` — **생성**

폴더 `Runtime/Stages/` 와 네임스페이스 `SushiDefense.Stages` 가 새로 생긴다. **어셈블리는 늘지 않는다** — `Runtime.asmdef` 안이다.

### 핵심 심볼

```csharp
namespace SushiDefense.Stages
{
    /// <summary>스테이지 한 판의 결과. <c>InProgress</c> 는 "아직 안 끝났다" 이지 실패가 아니다.</summary>
    public enum StageOutcome
    {
        InProgress = 0,
        Cleared = 1,
        Failed = 2
    }

    /// <summary>
    /// 제한 시간 대비 경과. <b>스스로 시간을 읽지 않는다</b> — <see cref="Advance"/> 인자로
    /// 받으므로 "59.9초가 흘렀을 때" 를 프레임 대기 없이 EditMode 로 검증할 수 있다
    /// (<c>SushiBelt</c> 와 같은 방식).
    /// </summary>
    public sealed class StageClock
    {
        public StageClock(float limitSeconds);

        public float LimitSeconds { get; }
        public float ElapsedSeconds { get; }

        /// <summary>남은 시간. <b>0 아래로 내려가지 않는다</b> — 표시에 음수가 새면 안 된다.</summary>
        public float RemainingSeconds { get; }

        /// <summary>제한 시간에 <b>도달했거나</b> 넘었다.</summary>
        public bool IsExpired { get; }

        public void Advance(float deltaSeconds);
        public void Reset();
    }

    /// <summary>
    /// 클리어/실패 규칙. <b>시간을 모른다</b> — 만료 여부를 <c>bool</c> 로 받는다.
    /// </summary>
    public static class StageEvaluator
    {
        public static StageOutcome Evaluate(int revenue, int targetRevenue, bool expired);
    }
}
```

### 판정 규칙 — 매출을 먼저 본다 (작업서 D2)

```csharp
if (revenue >= targetRevenue) return StageOutcome.Cleared;
return expired ? StageOutcome.Failed : StageOutcome.InProgress;
```

**제한 시간 정각에 목표를 채우면 클리어다.** 순서를 뒤집으면(만료를 먼저 보면) 같은 프레임에 목표를 채운 플레이어가 실패한다. 계획서의 테스트 이름이 이미 `Evaluate_TargetReachedExactlyAtTimeout_Clears` 였고 그대로 채택한다.

이 두 줄에 **`if` 가 세 개 이상 생기면 규칙이 늘어난 것**이다. 보너스 목표(`StageConfig.BonusObjectives`)는 클리어 판정이 아니라 추가 보상 조건이므로 **여기 들어오지 않는다.**

### `StageClock` 의 경계

- `IsExpired` 는 `ElapsedSeconds >= LimitSeconds` 다. `>` 가 아니다 — 정각이 만료다. 그래야 D2 의 "정각에 클리어" 가 실제로 발생 가능한 상태가 된다
- `Advance(음수)` 는 **예외**다. 조용히 통과시키면 시간이 되감기는 경로가 생긴다 (`RevenueLedger.Add` 가 음수 가격을 예외로 두는 것과 같은 판단)
- 생성자에서 `limitSeconds <= 0` 도 예외다. `StageConfig.OnValidate` 가 이미 1초 하한을 걸지만, 이 클래스는 SO 없이도 만들어질 수 있다
- `Reset()` 은 경과만 0 으로 되돌린다. 제한 시간은 그대로다

### 선행 산출물 의존성

없음. `StageConfig` 를 참조하지 않는다 — 제한 시간을 `float` 로 받는다. 컨트롤러(step-03)가 SO 에서 꺼내 넘긴다.

### 밸런스 수치

**없다.** 제한 시간·목표 매출은 전부 인자로 들어온다. 이 두 파일에 숫자 상수가 없어야 한다.

### 제약

- 로직은 `MonoBehaviour` 밖 순수 C# (`CLAUDE.md` §3.2)
- `Runtime` 은 `Presentation` 을 모른다
- `Time.deltaTime` · `Time.time` 을 읽지 않는다. 시간은 인자로만 들어온다
- `Random` 을 쓰지 않는다
- public API 에 `///` XML 문서 주석
- **`StageConfig` 를 참조하지 않는다.** 참조하는 순간 EditMode 테스트마다 SO 를 만들어야 한다

### 완료 판정

- [ ] `grep -rn "StageConfig\|Time\.\|Random" Assets/Code/Scripts/Runtime/Stages/StageClock.cs Assets/Code/Scripts/Runtime/Stages/StageEvaluator.cs` — **0건**
- [ ] `grep -c "if" Assets/Code/Scripts/Runtime/Stages/StageEvaluator.cs` — 주석 제외 **2개 이하**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh --fast` 전 항목 PASS

### 테스트 이름

```
StageEvaluatorTests
  Evaluate_TargetReachedBeforeTimeout_Clears
  Evaluate_TimeoutBeforeTarget_Fails
  Evaluate_TargetReachedExactlyAtTimeout_Clears     ← ★ 경계 확정
  Evaluate_BelowTargetNotExpired_ReturnsInProgress
  Evaluate_RevenueOverTarget_Clears
  Evaluate_ZeroRevenueNotExpired_ReturnsInProgress

StageClockTests
  RemainingSeconds_Midway_ReportsRemainder
  RemainingSeconds_BeyondLimit_ClampsToZero
  IsExpired_ExactlyAtLimit_ReturnsTrue
  IsExpired_JustBeforeLimit_ReturnsFalse
  Advance_Negative_Throws
  Advance_SplitIntoManySteps_MatchesOneBigStep      ← 틱 길이에 결과가 흔들리지 않는다
  Reset_AfterExpiry_ClearsElapsed
```

> **★ `Evaluate_TargetReachedExactlyAtTimeout_Clears` 가 이 단계의 결정을 고정한다.** 이 테스트 하나가 없으면 다음 사람이 순서를 뒤집어도 아무것도 빨개지지 않는다.
>
> **공허하게 통과하는 테스트를 조심한다** (`.claude/rules/tests.md` §3):
> - `Evaluate_TimeoutBeforeTarget_Fails` 는 **목표를 1 낮추면 클리어가 되는 반례**를 같은 파일에 함께 둔다. 안 그러면 항상 `Failed` 를 돌려주는 구현도 통과한다
> - `RemainingSeconds_Midway_ReportsRemainder` 는 **구체값**을 박는다 (한도 60, 2.5초 경과 → 57.5). "둘이 같다" 만 보면 상수를 돌려주는 구현이 통과한다
> - `IsExpired_JustBeforeLimit_ReturnsFalse` 를 반드시 함께 둔다. `IsExpired`가 늘 `true` 인 구현을 잡는 유일한 테스트다

### 예상 커밋 메시지

```
feat(stage): add stage clock and clear/fail evaluation
```

---

## 금지 사항

- 판정을 `StageClock` 안에 넣지 않는다. 시계는 시간만 안다
- `StageOutcome` 에 `Paused` · `Retrying` 같은 항목을 추가하지 않는다. 필요해지면 그때 별도로 판단한다 — 지금 넣으면 `Evaluate` 가 답할 수 없는 값이 enum 에 생긴다
- 보너스 목표(`StageConfig.BonusObjectives`)를 판정에 넣지 않는다. 클리어 조건이 아니다
- `StageController` 를 만들지 않는다. step-03 이다
- `Presentation` · `Runtime.Data` 를 수정하지 않는다
