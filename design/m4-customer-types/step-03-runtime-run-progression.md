# Step 03: `RunProgression` 구현 (Green)

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-02 완료 필요 (실패 테스트가 있어야 한다)
- **후행 단계:** step-05(프레젠터) · step-08(배선) 이 이 타입을 쓴다

---

## 목적

step-02 가 고정한 계약을 **최소 구현**으로 통과시킨다. 이 클래스가 M4 에서 유일하게 새로 생기는 `Runtime` 로직이다.

핵심은 **런 종료를 플래그로 들지 않는 것**이다 (README D3). `bool _finished` 를 두면 `RunState.StageNumber` 와 어긋날 수 있고, 어긋난 상태는 "3판 깼는데 안 끝난다" 또는 그 반대로 나타난다. 유도값이면 어긋날 수가 없다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

- `Assets/Code/Scripts/Runtime/Run/RunProgression.cs` — 생성

### 핵심 심볼

```csharp
using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Run
{
    public sealed class RunProgression
    {
        private readonly List<StageConfig> _stages = new();
        private readonly RunState _run;

        public RunProgression(IReadOnlyList<StageConfig> stages, RunState run);

        public int StageCount => _stages.Count;
        public bool IsRunComplete => _run.StageNumber > StageCount;
        public bool HasNextStage => _run.StageNumber < StageCount;
        public StageConfig CurrentStage => IsRunComplete ? null : _stages[_run.StageNumber - 1];
        public bool AdvanceAfterClear();
    }
}
```

### 구현 지침

**생성자** — `stages` 가 `null` 이면 빈 목록. `null` 항목은 **여기서 걸러낸다** (step-01 이 아니다 — 걸러내는 지점이 둘이면 어느 쪽이 지켰는지 알 수 없다). `run` 이 `null` 이면 `ArgumentNullException`: 런 없이 진행이라는 개념이 성립하지 않으므로 빈 것으로 감싸면 원인이 한참 뒤에 드러난다.

**`AdvanceAfterClear()`**

```
이미 끝났으면 → false (번호를 올리지 않는다)
_run.AdvanceStage()
return !IsRunComplete
```

멱등성이 첫 줄 하나에서 나온다. **별도 가드 플래그를 두지 않는다** — M3 에서 `StageController.Tick` 의 조기 반환 하나가 "정지" 와 "판정 1회" 를 동시에 지킨 것과 같은 구조다.

**`CurrentStage` 의 빈 목록 처리** — `StageCount` 가 0 이면 `StageNumber(1) > 0` 이 참이라 `IsRunComplete` 가 즉시 `true` 가 되고 `null` 이 나온다. 별도 분기가 필요 없다. 이 우연이 아니라 **의도**임을 주석으로 남긴다.

### 주석에 남길 "왜"

- 왜 플래그가 아니라 유도값인가 (README D3)
- 왜 `RunConfig` 가 아니라 목록을 받는가 (테스트 가능성 + `Runtime` → `Runtime.Data` 결합 최소화)
- 왜 `StageConfig.StageNumber` 를 보지 않는가 (순서의 진실은 목록이다)

### 선행 산출물 의존성

- `SushiDefense.Run.RunState` — 이미 있다. **수정하지 않는다** (README D4)
- `SushiDefense.Data.StageConfig` — 이미 있다

### 밸런스 수치

없음. 스테이지 개수는 애셋에서 온다.

### 제약

- **`MonoBehaviour` 가 아니다.** 순수 C# (§3.2)
- `Runtime` 은 `Presentation` 을 모른다 (asmdef 규칙 §3)
- **난수 금지.** `Runtime/Run/` 은 preflight 의 난수 허용 목록 안이지만, 그건 보상 추첨을 위한 예외다. 이 파일에는 `Random` 이 들어갈 이유가 없다
- public API 에 `///` XML 문서 주석 (§4.4)
- `Update()` 에서 도는 경로가 아니므로 할당 최적화는 불필요. 대신 **생성자에서 목록을 복사**해 외부 목록 변경에 흔들리지 않게 한다

### 완료 판정

- [ ] `./tests/run-tests.sh` EditMode 전량 Green — step-02 의 테스트 전부 포함
- [ ] `grep -rn "bool _finished\|_isComplete\|_completed" Assets/Code/Scripts/Runtime/Run/RunProgression.cs` 가 **0건** (플래그를 두지 않았다)
- [ ] `grep -rn "Random" Assets/Code/Scripts/Runtime/Run/RunProgression.cs` 가 0건
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 주입 검증 (구현 후 반드시)

계약이 실제로 테스트에 걸리는지 확인한다. **하나씩** 깨뜨리고, 지목한 테스트가 실제로 실패하는지 본 뒤 되돌린다.

| 주입 | 실패해야 하는 테스트 (실측) |
|---|---|
| `AdvanceAfterClear` 의 조기 반환 제거 | `AdvanceAfterClear_AfterRunComplete_ReturnsFalseAndDoesNotAdvance` |
| `IsRunComplete` 를 `>=` 로 | `AdvanceAfterClear_LastStage_ReturnsFalse` · `CurrentStage_SingleStageRun_IsThatStage` · `IsRunComplete_AfterClearingLastStage_IsTrue` |
| 생성자의 `null` 필터 제거 | `StageCount_ListWithNullEntries_CountsOnlySurvivors` |
| `CurrentStage` 인덱스를 `StageNumber` 로 (off-by-one) | `CurrentStage_FreshRun_IsFirstStage` 외 4개 |

> **두 번째 줄은 원래 `IsRunComplete_FreshRun_IsFalse` 라고 적혀 있었다. 틀렸다.** 그 테스트는
> 3스테이지 런이라 `1 >= 3` 이 여전히 거짓이어서 **주입에도 통과한다**. `>=` 를 실제로 잡으려면
> **번호와 총수가 같아지는 지점**을 보는 테스트가 필요하고, 위 셋이 그 자리에 있다.
> 주입 실험으로 드러나 정정한 서술이다 — 지목이 틀린 표는 "잡혔다" 는 오답을 준다.

**전량 통과하는 주입이 나오면 그 계약은 아직 테스트가 없는 것이다.** 되돌린 뒤 테스트를 먼저 추가하고, 결과를 보고한다.

### 예상 커밋 메시지

```
feat(run): add RunProgression for stage advance and run end
```

---

## 금지 사항

- `RunState` 에 필드를 더하지 않는다 (README D4).
- 전환 화면·UI 를 만들지 않는다 (step-05·06).
- `StageBootstrap` 을 건드리지 않는다 (step-08).
- 테스트를 고쳐서 통과시키지 않는다. 테스트가 틀렸다고 판단되면 **먼저 보고**한다.
