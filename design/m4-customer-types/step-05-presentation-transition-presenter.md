# Step 05: 전환 화면 프레젠터

- **영역:** `presentation` — 어셈블리 `Presentation` (+ `Tests.EditMode`)
- **선행 단계:** step-03 완료 필요 (`RunProgression`)
- **후행 단계:** step-06 이 `IStageTransitionView` 를 구현한다. step-08 이 프레젠터를 배선한다

---

## 목적

스테이지를 깬 뒤 **다음 판으로 넘어가기 전의 한 박자**를 만든다. 보상 화면이 닫히자마자 다음 스테이지가 시작되면 플레이어는 자기가 몇 판째인지도, 런이 끝났는지도 알 수 없다.

보상 화면과 **같은 MVP 구조**로 만든다 (`CLAUDE.md` §3.6). 로직을 `MonoBehaviour` 안에 두면 EditMode 로 검증할 수 없고, M6 에서 UI 를 갈아 끼울 때 로직까지 다시 짠다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

- `Assets/Code/Scripts/Presentation/Presenters/IStageTransitionView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/StageTransitionPresenter.cs` — 생성
- `Assets/Tests/EditMode/UI/FakeStageTransitionView.cs` — 생성
- `Assets/Tests/EditMode/UI/StageTransitionPresenterTests.cs` — 생성

기존 `IRewardSelectionView` / `RewardSelectionPresenter` / `FakeRewardSelectionView` 를 **형태의 본보기로 삼는다.**

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    /// Unity 타입이 하나도 없다 — 프레젠터를 EditMode 로 검증하기 위해서다.
    public interface IStageTransitionView
    {
        void ShowStageCleared(int clearedStageNumber, int nextStageNumber);
        void ShowRunComplete(int clearedStageNumber);
        void Hide();
    }

    public sealed class StageTransitionPresenter
    {
        public StageTransitionPresenter(IStageTransitionView view, RunProgression progression);

        public bool IsOpen { get; }

        /// 지금 열려 있는 화면이 "런 종료" 인가. 표시 상태이지 판정이 아니다.
        public bool IsRunFinale { get; }

        /// 다음 스테이지로 넘어간다. 인자는 넘어갈 스테이지다.
        public event Action<StageConfig> StageAdvanced;

        /// 마지막 스테이지였다. 넘어갈 곳이 없다.
        public event Action RunCompleted;

        public void Open();
        public bool Proceed();
    }
}
```

### 동작 계약

**`Open()`** — 화면만 연다. **런 상태를 바꾸지 않는다.**

```
IsRunFinale = !_progression.HasNextStage
IsOpen = true
IsRunFinale ? view.ShowRunComplete(현재 스테이지 번호)
            : view.ShowStageCleared(현재 번호, 현재 번호 + 1)
```

> **왜 `Open()` 에서 진행시키지 않나**: 화면을 띄우는 것과 상태를 바꾸는 것은 다른 일이다. `Open()` 이 스테이지 번호를 올리면 플레이어가 확인 입력을 하기도 전에 HUD 의 스테이지 번호가 바뀐다. **상태는 확인 입력에서 바뀐다.**

**`Proceed()`** — 확인 입력. 여기서 진행한다.

```
열려 있지 않으면 → false (아무 일도 하지 않는다)
var advanced = _progression.AdvanceAfterClear();
Close();                       // 화면을 먼저 내린다
advanced ? StageAdvanced?.Invoke(_progression.CurrentStage)
         : RunCompleted?.Invoke();
return advanced;
```

> **닫기를 이벤트 발행보다 먼저** 한다. `StageAdvanced` 구독자가 `Build()` 를 부르는데(step-08), 그 시점에 화면이 아직 떠 있으면 새 스테이지 위에 전환 화면이 겹쳐 남는다.

**`Close()` 는 private 하나뿐이다.** 보상 프레젠터와 같은 이유 — 닫는 경로가 둘이면 나중에 한쪽만 고쳐진다.

**닫기 전용 출구를 만들지 않는다.** 보상 화면의 `Skip()` 은 "후보가 0개일 때의 유일한 출구" 라 필요했지만, 전환 화면은 `Proceed()` 하나로 충분하다. 건너뛸 것이 없다.

### 테스트 목록 (`StageTransitionPresenterTests`)

```
열기  Open_MidRun_ShowsClearedWithNextNumber      ← 뷰가 (1, 2) 를 받았는지 인자까지 확인
      Open_LastStage_ShowsRunComplete
      Open_MidRun_DoesNotAdvanceRunState         ← Open 은 상태를 안 바꾼다
      Open_Twice_DoesNotDoubleAdvance

진행  Proceed_MidRun_RaisesStageAdvancedWithNextStage   ← AreSame 으로 인스턴스 확인
      Proceed_MidRun_ReturnsTrue
      Proceed_LastStage_RaisesRunCompleted
      Proceed_LastStage_DoesNotRaiseStageAdvanced
      Proceed_NotOpen_ReturnsFalseAndRaisesNothing
      Proceed_Twice_RaisesOnce

닫기  Proceed_Any_HidesViewBeforeRaisingEvent    ← 페이크가 Hide 순서를 기록한다
      IsOpen_AfterProceed_IsFalse

방어  Ctor_NullView_Throws
      Ctor_NullProgression_Throws
```

### 공허하게 통과하지 않게

- **`Open_MidRun_ShowsClearedWithNextNumber` 는 스테이지 1 이 아닌 지점에서 연다.** 스테이지 1 에서 열면 `(1, 2)` 라서 "항상 1 을 넘기는" 구현도 통과한다. 먼저 한 번 진행시켜 **`(2, 3)`** 을 확인한다.
- **`Proceed_Any_HidesViewBeforeRaisingEvent` 는 순서를 본다.** 페이크 뷰가 `Hide()` 호출 시점에 플래그를 세우고, 이벤트 핸들러 안에서 그 플래그를 읽는다. "둘 다 일어났다" 만 보면 순서가 뒤집힌 구현도 통과한다.
- **`Proceed_MidRun_RaisesStageAdvancedWithNextStage` 는 `AreSame`** 으로 확인한다. `IsNotNull` 이면 현재 스테이지를 그대로 넘기는 구현이 통과한다.

### 선행 산출물 의존성

- `SushiDefense.Run.RunProgression` — step-03
- `SushiDefense.Data.StageConfig` — 이미 있다

### 밸런스 수치

없음.

### 제약

- **프레젠터에 `UnityEngine` 참조가 하나도 없어야 한다** (§3.6). `RewardSelectionPresenter` 와 같은 기준이다
- `Presentation` 은 `Runtime` · `Runtime.Data` 를 참조한다. 역방향 금지
- public API 에 `///` XML 문서 주석
- 이벤트 구독을 프레젠터가 하지 않는다 — 발행만 한다. 해제 책임을 만들지 않기 위해서다

### 완료 판정

- [ ] `grep -rn "UnityEngine" Assets/Code/Scripts/Presentation/Presenters/StageTransitionPresenter.cs` 가 **0건**
- [ ] `grep -rn "UnityEngine" Assets/Code/Scripts/Presentation/Presenters/IStageTransitionView.cs` 가 **0건**
- [ ] `./tests/run-tests.sh` EditMode 전량 Green
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 주입 검증 (구현 후 반드시)

| 주입 | 실패해야 하는 테스트 |
|---|---|
| `Open()` 에서 `AdvanceAfterClear()` 를 부르게 | `Open_MidRun_DoesNotAdvanceRunState` |
| `Close()` 를 이벤트 발행 뒤로 옮김 | `Proceed_Any_HidesViewBeforeRaisingEvent` |
| `Proceed()` 의 `IsOpen` 가드 제거 | `Proceed_NotOpen_ReturnsFalseAndRaisesNothing` · `Proceed_Twice_RaisesOnce` |
| `ShowStageCleared` 의 두 번째 인자를 `+1` 없이 | `Open_MidRun_ShowsClearedWithNextNumber` |

전량 통과하는 주입이 나오면 **그 계약은 아직 테스트가 없는 것이다.** 되돌리고 테스트를 먼저 추가한 뒤 보고한다.

### 예상 커밋 메시지

```
feat(stage): add stage transition presenter
```

---

## 금지 사항

- `MonoBehaviour` 뷰를 만들지 않는다 (step-06).
- `StageBootstrap` 을 건드리지 않는다 (step-08).
- `RunProgression` · `RunState` 를 수정하지 않는다.
