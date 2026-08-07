# Step 07: 인스테이지 Menu — Pause · Play · Restart · Quit

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-02 (`PauseState`)
- **후행 단계:** step-09 가 `ISceneRouter` 구현을 제공한다 (Quit 의 목적지)

---

## 목적

스테이지에 **출구가 없다.** 시작하면 실패하거나 클리어할 때까지 나갈 수 없고, 멈출 수도 없다. 웹 데모에서 이건 새로고침 말고는 방법이 없다는 뜻이다.

버튼 하나로 네 가지가 열린다 — 멈추기 · 재개 · 다시 시작 · 메인으로.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Presenters/IStageMenuView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/StageMenuPresenter.cs` — 생성
- `Assets/Code/Scripts/Presentation/Views/StageMenuView.cs` — 생성
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (`PauseState` 소유 + `Update` 게이트 + 배선)
- `Assets/Tests/EditMode/UI/StageMenuPresenterTests.cs` — 생성
- `Assets/Tests/PlayMode/Stages/StagePauseTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    public interface IStageMenuView
    {
        /// <summary>메뉴를 연다. 지금 멈춰 있는지도 함께 넘겨 버튼 표시를 맞춘다.</summary>
        void ShowMenu(bool paused);

        void Hide();
    }

    /// <summary>인스테이지 메뉴의 로직. Unity API 를 모른다.</summary>
    public sealed class StageMenuPresenter
    {
        public bool IsOpen { get; }

        public StageMenuPresenter(IStageMenuView view, PauseState pause,
                                  IStageRestarter restarter, ISceneRouter router);

        public void Open();
        public void Close();          // 닫으면 재개한다
        public void Pause();
        public void Resume();
        public void Restart();        // 같은 판을 다시 — 런은 살아 있다
        public void QuitToMain();     // 런을 버리고 메인으로
    }

    /// <summary>같은 스테이지를 다시 연다. 구현은 <c>StageBootstrap</c> 이다.</summary>
    public interface IStageRestarter
    {
        void Restart();
    }
}
```

`ISceneRouter` 는 step-09 가 정의한다. **이 단계는 인터페이스만 쓰고 구현을 만들지 않는다** — 두 단계가 같은 파일을 만들면 충돌한다. step-09 보다 먼저 실행한다면 인터페이스를 여기서 만들고 step-09 가 구현만 더한다. **어느 쪽이든 정의는 한 곳이다.**

### 멈춤은 `Tick` 을 건너뛰는 것이다 (README D4)

```csharp
// StageBootstrap.Update
private void Update()
{
    if (Pause.IsPaused) { return; }
    Stage?.Tick(Time.deltaTime);
}
```

이 한 줄이 벨트·손님·시계·판정을 전부 멈춘다. `Time.timeScale` 을 쓰지 않는 이유:

- 전역이라 **누가 멈췄는지 추적할 수 없다**. 나중에 연출용 슬로모션이 생기면 두 주체가 같은 값을 다툰다
- UI 애니메이션과 `WaitForSeconds` 까지 얼린다 ([`unity-scripting-gotchas.md`](../../.claude/knowledge/unity-scripting-gotchas.md) §2)
- **테스트가 프레임을 세야 한다.** `PauseState` 는 EditMode 로 끝난다

`PauseState` 는 **스테이지 수명이 아니라 런 수명**이다. `Build()` 마다 새로 만들면 스테이지가 넘어갈 때 멈춘 상태가 조용히 풀린다 — `Rewards`·`Transition` 을 `EnsureRunScope` 에 둔 것과 같은 판단이다. 다만 **`Build()` 는 재개 상태로 시작한다** (새 판이 멈춘 채 열리면 고장으로 보인다).

### Restart 는 `Retry()` 다 — 시도 횟수가 오른다

`StageBootstrap.Retry()` 가 `Run.RecordFailedAttempt()` 를 부른다. **플레이어가 스스로 다시 시작하는 것도 그 판을 포기한 것**이므로 시도 횟수가 오르는 것이 맞다. 경로를 둘로 나누면 "실패로 다시" 와 "눌러서 다시" 가 갈리고, 덱·명부 유지 규칙이 두 곳에 살게 된다.

**메뉴를 닫는 것이 먼저다.** `Retry()` → `Build()` 가 뷰를 다시 물리는데 그때 메뉴가 떠 있으면 새 판 위에 겹쳐 남는다 — `StageTransitionPresenter.Proceed` 가 *"화면을 먼저 내리고 이벤트를 발행한다"* 로 같은 사고를 이미 막아 둔 자리다.

### Quit 은 런을 버린다 (README D1)

`ISceneRouter.LoadMain()` 을 부른다. `RunState` 는 스테이지 씬과 함께 사라진다 — 저장·이어하기는 M6 의 범위가 아니다.

**확인 절차를 넣지 않는다.** "정말 나가시겠습니까" 는 화면이 하나 더 느는 일이고, 요구에 없다. 진행이 아깝다는 판단이 서면 그때 넣는다.

### 선행 산출물 의존성

- step-02 의 `PauseState`
- step-09 의 `ISceneRouter` (정의는 한 곳 — 위 참조)
- 기존 `StageBootstrap.Retry()`

### 밸런스 수치

**없다.**

### 제약

- 프레젠터는 `UnityEngine` 을 참조하지 않는다 (§3.6)
- **`Time.timeScale` 을 쓰지 않는다** — `grep -rn "timeScale" Assets/Code/` 가 0건이어야 한다
- 뷰는 판정하지 않는다. 지금 멈춰 있는지도 프레젠터가 넘긴다
- 구독·`onClick` 해제는 `OnDestroy` 에서
- `StageBootstrap` 의 기존 public 계약(`Build`·`Retry`·`Initialize`·노출 프로퍼티)을 바꾸지 않는다 — `StageIntegrationTests` 가 회귀 그물이다
- **한글 문구는 폰트 서브셋에 들어간다** (step-11)

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
EditMode  Open_WhenRunning_PausesAndShows          ← 여는 것이 곧 멈추는 것인가 (아래 주 참조)
          Close_WhenPaused_Resumes
          Pause_Twice_StaysPaused
          Restart_CallsRestarterOnce
          Restart_ClosesMenuBeforeRestarting       ← 순서를 고정한다
          QuitToMain_CallsRouterOnce
          QuitToMain_DoesNotRestart
PlayMode  Paused_BeltDoesNotAdvance                ← 멈춤이 실제로 시간을 멈추는가
          Paused_RemainingSecondsUnchanged
          Resumed_TickResumes
          Restart_KeepsDeckAndRoster               ← 런이 살아 있다
```

> **`Open` 이 멈추게 할지 정하고 테스트로 박는다.** 메뉴를 열면 멈추는 것이 자연스럽지만, `Pause`/`Resume` 버튼이 따로 있으므로 **열림과 멈춤이 같은 것이 아니게** 만들 수도 있다. 이 작업서는 **여는 순간 멈춘다**로 정한다 — 메뉴를 보는 동안 벨트가 흐르면 메뉴가 곧 페널티가 된다. `Pause`/`Play` 버튼은 메뉴를 연 채로 다시 흘려 보고 싶을 때의 것이다.

> **`Paused_BeltDoesNotAdvance` 를 「값이 같다」로만 쓰지 않는다.** 멈춘 뒤 여러 프레임을 흘리고, **재개한 뒤에는 실제로 움직였는지**를 같은 테스트에 함께 박는다. 그러지 않으면 벨트가 아예 안 도는 구현에서도 통과한다 ([`tests.md`](../../.claude/rules/tests.md) §3).

### 주입 검증

| 주입 | 예측 |
|---|---|
| `StageBootstrap.Update` 의 일시정지 가드 제거 | `Paused_BeltDoesNotAdvance` |
| `Restart` 가 메뉴를 닫지 않는다 | `Restart_ClosesMenuBeforeRestarting` |
| `PauseState` 를 `Build()` 안으로 옮긴다 (스테이지 수명) | (잡히지 않으면 그 계약은 테스트가 없는 것 — 스테이지 전환 테스트를 추가한다) |
| `Close` 가 재개하지 않는다 | `Close_WhenPaused_Resumes` |

### 완료 판정

- [ ] `grep -rn "timeScale" Assets/Code/` = **0건**
- [ ] `grep -c "UnityEngine" Assets/Code/Scripts/Presentation/Presenters/StageMenuPresenter.cs` = **0**
- [ ] `ISceneRouter` 정의가 저장소에 **한 곳**뿐이다
- [ ] EditMode · PlayMode Green — `./tests/run-tests.sh all`
- [ ] **기존** `StageIntegrationTests` 가 수정 없이 통과한다
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(stage): add in-stage menu with pause, restart and quit
```

---

## 금지 사항

- `Time.timeScale` 을 쓰지 않는다
- 재시작 경로를 `Retry()` 와 따로 만들지 않는다
- Quit 에 확인 대화상자를 넣지 않는다
- 뷰에 멈춤 상태를 저장하지 않는다 — 프레젠터가 넘긴다
- 다른 어셈블리 파일을 수정하지 않는다
