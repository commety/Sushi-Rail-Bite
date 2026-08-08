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

### 주입 실측

| 주입 | 예측 | 실제 |
|---|---|---|
| `Update` 의 일시정지 가드 제거 | 1건 | **2건** — 시계와 벨트가 각각 |
| `Restart` 가 메뉴를 닫지 않는다 | 1건 | **2건** — 순서 테스트와 «재시작이 시간을 다시 흘린다» 가 함께 |
| `QuitToMain` 이 재시작까지 부른다 | 1건 | 예측대로 **1건** |

### PlayMode 에서 프레임으로 기다리면 안 된다

`Paused_SushiOnBeltDoesNotMove` 가 처음 두 번 **«벨트에 초밥이 하나도 없다»** 로 실패했다.
240프레임을 기다렸는데도 스폰 간격 `0.2`초를 못 넘긴 것이다 — **배치 모드는 프레임이 매우
짧아** 수백 프레임이 게임 시간 몇 분의 일 초밖에 안 된다.

`WaitForSeconds` 로 바꿔 해결했다. 그리고 이 실패는 **테스트가 자기가 아무것도 못 봤다는
것을 알렸기 때문에** 드러났다 — 초밥이 없으면 조용히 통과하는 대신 `Assert.IsNotNull` 로
실패시켜 둔 덕이다. 없었으면 «멈추면 안 움직인다» 가 영원히 초록인 채로 아무것도 검증하지
않았을 것이다.

> `Time.timeScale` 을 쓰지 않으므로 `WaitForSeconds` 가 일시정지 중에도 정상적으로 흐른다 —
> 전역 배율을 썼다면 이 테스트 자체가 얼어붙는다.

### 완료 판정

- [x] `grep -rn "timeScale" Assets/Code/` = **0건**
- [x] `StageMenuPresenter` 에 `UnityEngine` **0건**
- [x] `ISceneRouter` 정의가 저장소에 **한 곳**뿐이다 (`Navigation/ISceneRouter.cs`)
- [x] EditMode **656/656** · PlayMode **204/204**
- [x] **기존** `StageIntegrationTests` 가 수정 없이 통과한다
- [x] `./tests/preflight.sh` 전 항목 PASS

### step-09 의 내비게이션 계층을 앞당겼다

`SceneNames` · `ISceneRouter` · `SceneRouter` 세 파일을 여기서 만들었다. **나가기는 목적지가
없으면 성립하지 않는다** — 인터페이스만 정의하면 씬 진입점이 물릴 구현이 없어 배선 자체가
불가능하다. step-09 는 이제 메인 화면 프레젠터·뷰만 만든다.

`SceneNamesTests` 도 함께 왔다 (§5.4 — 새 파일에는 테스트가 따라온다). 다만 그 테스트가
확인할 수 있는 것은 **상수끼리의 정합성뿐**이다. `"Stage1"` 같은 오타는 컴파일도 프레젠터
테스트도 통과하며, **Build Settings 의 실제 경로와 대조해야만** 드러난다 — step-12 의 몫이다.

> `Main.unity` 는 아직 없다. 지금 나가기를 누르면 Unity 가 *"씬이 빌드 설정에 없다"* 를
> 로그로 남기고 아무 일도 하지 않는다. 예외는 아니며 step-12 가 등록하면서 해소된다.

### 재시작은 `Retry()` 다 — 이름을 둘로 만들지 않았다

`StageBootstrap` 이 `IStageRestarter` 를 **명시적 구현**으로 받는다
(`void IStageRestarter.Restart() => Retry();`). 공개 이름을 하나로 유지해야 «덱·명부는
유지되고 시도 횟수가 오른다» 는 규칙도 한 곳에만 남는다.

### 게이트는 런 수명, 새 판은 흐르는 상태

`PauseState` 는 `EnsureRunScope` 에서 한 번 만들어지고 `Build()` 가 `Resume()` 한다.
판마다 새로 만들면 메뉴가 죽은 객체를 가리키고, 재개하지 않으면 재시작한 판이 멈춘 채로
열려 고장으로 보인다. **두 계약 모두 테스트로 고정했다** — `Retry_KeepsTheSamePauseGate` 와
`Retry_WhilePaused_StartsRunning`.

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
