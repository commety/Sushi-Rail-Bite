# Step 02: `RunProgression` 실패 테스트 (Red)

- **영역:** `tests` — 어셈블리 `Tests.EditMode`
- **선행 단계:** 없음 (step-01 과 병렬 가능 — 이 테스트는 `RunConfig` 를 쓰지 않는다)
- **후행 단계:** step-03 이 이 테스트를 통과시킨다

---

## 목적

스테이지 진행의 **계약을 코드로 먼저 고정**한다 (`CLAUDE.md` §5, TDD 기본값). 구현 전에 쓰므로 이 단계의 산출물은 **컴파일 에러**다 — `RunProgression` 이 아직 없기 때문이며, 그것이 정상적인 Red 다.

이 단계에서 확정하는 계약은 셋이다.

1. **현재 스테이지**는 `RunState.StageNumber` 로 목록을 인덱싱한 결과다 (1-based)
2. **런 종료**는 별도 플래그가 아니라 `StageNumber > StageCount` 에서 **유도**된다 (README D3)
3. **마지막 스테이지를 깨면 더 진행하지 않는다** — 두 번 불러도 스테이지 번호가 계속 오르지 않는다

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

- `Assets/Tests/EditMode/Run/RunProgressionTests.cs` — 생성

### 검증 대상 시그니처 (step-03 이 구현할 것)

```csharp
namespace SushiDefense.Run
{
    public sealed class RunProgression
    {
        public RunProgression(IReadOnlyList<StageConfig> stages, RunState run);

        public int StageCount { get; }
        public StageConfig CurrentStage { get; }   // 런이 끝났으면 null
        public bool IsRunComplete { get; }
        public bool HasNextStage { get; }
        public bool AdvanceAfterClear();           // 다음 스테이지가 있으면 true
    }
}
```

> **생성자가 `RunConfig` 가 아니라 `IReadOnlyList<StageConfig>` 를 받는 이유**: SO 를 통해서만 세울 수 있으면 테스트가 매번 `SerializedObject` 로 목록을 주입해야 한다. 목록을 그대로 받으면 `Runtime` 이 `RunConfig` 를 몰라도 되고, 스테이지가 1개인 경우(README D5)도 배열 하나로 표현된다.

### 테스트 목록

```
구성    StageCount_ThreeStages_IsThree
        StageCount_ListWithNullEntries_CountsOnlySurvivors   ← 걸러내는 쪽은 여기다 (step-01 이 아니다)
        Ctor_NullStages_TreatsAsEmpty
        Ctor_NullRun_Throws

현재    CurrentStage_FreshRun_IsFirstStage
        CurrentStage_AfterOneAdvance_IsSecondStage
        CurrentStage_RunComplete_IsNull

진행    AdvanceAfterClear_MidRun_ReturnsTrueAndMovesForward
        AdvanceAfterClear_LastStage_ReturnsFalse
        AdvanceAfterClear_AfterRunComplete_ReturnsFalseAndDoesNotAdvance   ← 멱등
        AdvanceAfterClear_MidRun_ResetsAttemptNumber

종료    IsRunComplete_FreshRun_IsFalse
        IsRunComplete_AfterClearingLastStage_IsTrue
        HasNextStage_LastStage_IsFalse

경계    CurrentStage_EmptyStageList_IsNullAndRunComplete
        CurrentStage_SingleStageRun_IsThatStage             ← README D5 의 현행 동작
```

### 공허하게 통과하지 않게

- **`CurrentStage_AfterOneAdvance_IsSecondStage` 는 세 스테이지를 서로 다른 인스턴스로 만들고 `AreSame` 으로 확인한다.** `IsNotNull` 만 보면 첫 스테이지를 계속 돌려주는 구현도 통과한다.
- **`AdvanceAfterClear_AfterRunComplete_...` 는 `false` 만 보지 않는다.** 호출 전후의 `run.StageNumber` 가 **같은 값**임을 함께 박는다. 안 그러면 번호는 계속 오르는데 `false` 만 돌려주는 구현이 통과하고, 그 상태는 나중에 인덱스가 한참 벗어난 채로 발견된다.
- **`StageCount_ListWithNullEntries_...` 는 `null` 을 목록 중간에 둔다.** 끝에 두면 잘라내는 구현과 걸러내는 구현이 구분되지 않는다.
- **`AdvanceAfterClear_MidRun_ResetsAttemptNumber` 는 먼저 `run.RecordFailedAttempt()` 를 불러 시도 횟수를 2 로 만든 뒤** 진행시키고 1 로 돌아왔는지 본다. 처음부터 1 이면 아무것도 검증하지 않는다.

### 준비물

- `StageConfig` 인스턴스는 `ScriptableObject.CreateInstance<StageConfig>()` 로 만든다. 값은 채울 필요가 없다 — 이 단계는 **동일성**만 본다. 필요하면 기존 `Assets/Tests/EditMode/Data/StageConfigBuilder.cs` 를 재사용한다.
- `RunState` 는 `new RunState(null, null, seed: 0)` 로 충분하다. 덱은 이 단계와 무관하다.

### 제약

- `Tests.EditMode` 는 `Runtime` · `Runtime.Data` 를 참조한다. **역방향 금지** (asmdef 규칙 §3)
- 테스트 더블은 손으로 쓴 스텁/페이크. 새 패키지 추가 금지 (§7)
- `Assert` 없는 테스트를 만들지 않는다 ([`rules/tests.md`](../../.claude/rules/tests.md) §7)

### 완료 판정

- [ ] `./tests/run-tests.sh` 가 **종료 코드 2(컴파일 실패)** 를 낸다 — `RunProgression` 이 없어서다. 이것이 이 단계의 Red 다
- [ ] 실패 원인이 **오직** `RunProgression` 미정의임을 로그로 확인한다 (오타·다른 심볼 때문이 아니어야 한다)
- [ ] 테스트 메서드 수가 위 목록과 일치

### 예상 커밋 메시지

```
test(run): cover stage progression and run completion
```

---

## 금지 사항

- **`RunProgression` 을 구현하지 않는다.** 이 단계는 Red 로 끝난다.
- 통과시키려고 테스트를 `[Ignore]` 하거나 지우지 않는다.
- `RunState` 를 수정하지 않는다 (README D4).
