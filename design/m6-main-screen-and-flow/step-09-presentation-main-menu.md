# Step 09: 메인 화면 · 씬 라우팅

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-03 (`UnityEngine.UI` 참조)
- **후행 단계:** step-10(설정·백과사전이 이 화면에서 열린다) · step-12(씬 조립)

---

## 목적

**게임에 입구가 없다.** 브라우저를 열면 스테이지 1 이 이미 돌고 있다. 시작·설정·사전이 놓일 자리를 만들고, 씬 두 장 사이를 오가는 길을 낸다.

씬 전환은 이 프로젝트가 **한 번도 해 본 적이 없는 일**이다 (`SceneManager` 프로덕션 사용처 0건). 그래서 이 단계의 절반은 그 길을 하나로 만드는 데 쓴다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Navigation/SceneNames.cs` — 생성
- `Assets/Code/Scripts/Presentation/Navigation/ISceneRouter.cs` — 생성 (step-07 이 먼저 만들었으면 **재사용**)
- `Assets/Code/Scripts/Presentation/Navigation/SceneRouter.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/IMainMenuView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/MainMenuPresenter.cs` — 생성
- `Assets/Code/Scripts/Presentation/Views/MainMenuView.cs` — 생성
- `Assets/Tests/EditMode/UI/MainMenuPresenterTests.cs` — 생성
- `Assets/Tests/EditMode/Navigation/SceneNamesTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.Navigation
{
    /// <summary>씬 이름의 유일한 출처. 문자열을 두 곳에 적지 않는다.</summary>
    public static class SceneNames
    {
        public const string Main = "Main";
        public const string Stage = "Stage01";
    }

    /// <summary>어느 화면으로 갈지. 구현이 <c>SceneManager</c> 를 안다.</summary>
    public interface ISceneRouter
    {
        void LoadMain();
        void LoadStage();
    }

    public sealed class SceneRouter : MonoBehaviour, ISceneRouter { }
}

namespace SushiDefense.UI
{
    public interface IMainMenuView
    {
        void ShowMenu();
        void Hide();
    }

    /// <summary>메인 화면의 로직. Unity API 를 모른다.</summary>
    public sealed class MainMenuPresenter
    {
        public MainMenuPresenter(IMainMenuView view, ISceneRouter router);

        public void Open();
        public void StartGame();       // 스테이지 씬으로
        public void OpenSettings();    // step-10
        public void OpenCodex();       // step-10 (백과사전)
    }
}
```

### 씬 이름을 상수 하나로 묶는 이유

문자열 씬 이름은 **오타가 컴파일에 안 잡힌다.** `LoadScene("Stage1")` 은 조용히 실패하고 화면이 그대로 멈춘다. 상수로 묶으면 최소한 한 곳에서만 틀릴 수 있다.

그리고 **그 상수가 Build Settings 의 등록 이름과 맞는지 테스트가 본다** (step-12). M5 가 등록을 빠뜨려 *"빌드는 성공하고 화면은 클리어 색만"* 을 만든 자리가 정확히 여기다 ([`tests.md`](../../.claude/rules/tests.md) §1 «애셋 등록·설정»).

`SceneNames` 는 `const` 만 갖는다 — **가변 정적 상태가 아니므로 RULE-01 의 초기화 메서드가 필요 없다.** 필드를 더하고 싶어지면 그때 규칙을 다시 본다.

### 라우터를 인터페이스 뒤에 두는 이유

`MainMenuPresenter` 가 `SceneManager.LoadScene` 을 직접 부르면 **"시작을 누르면 스테이지로 간다" 를 EditMode 로 확인할 수 없다.** 씬을 실제로 로드해야 하기 때문이다. 인터페이스 뒤로 밀면 스텁이 호출 횟수만 세면 된다 — `IRewardSelectionView` 가 이미 쓰는 방식이다 (§3.6).

`SceneRouter` 자체는 **한 줄짜리 래퍼**다. 테스트하지 않는다 — 테스트할 것이 없다.

### 화면 구성

```
MainCanvas
├── TitleLabel      ("스시 레일 바이트" 또는 로고 스프라이트)
├── StartButton     ("게임 시작")
├── SettingsButton  ("설정")
└── CodexButton     ("백과사전")
```

**버튼은 셋뿐이다** (아키텍트 요구). 스테이지 선택도 덱빌딩도 넣지 않는다 — `게임 시작` 이 곧 스테이지 1 이다 (README D9).

설정·백과사전은 **메인 씬 안의 패널**이다. 씬을 더 만들지 않는다 — 화면 두 개 때문에 씬 두 개를 늘리면 로드 시간과 배선만 는다.

### `Stage01.unity` 의 자동 시작을 끈다

지금 `StageBootstrap.Awake` 가 설정이 있으면 바로 `Build()` 한다. 씬을 메인에서 로드하는 구조에서도 **그 동작이 그대로 맞다** — 스테이지 씬은 스테이지를 시작하려고 여는 것이다.

**바꾸지 않는다.** 확인만 하고 지나간다.

### M5 가 남긴 숙제 — `SushiEatenEventChannelSO` 의 처분

M5 의 D5 는 *"씬이 여럿이 되어 씬을 넘는 통신이 실제로 필요해지는 시점에 살릴지 지울지 정한다"* 고 미뤄 두었다. **그 시점이 지금이다.**

판단:

1. `grep -rn "SushiEatenEventChannelSO\|SushiEatenPayload" Assets/` 로 사용처를 확인한다
2. M6 의 씬 전환은 **메인 ↔ 스테이지**이고, 두 씬 사이에 오갈 게임 사건이 없다 — 메인 화면은 초밥이 먹히는 것을 알 필요가 없다
3. 따라서 **채널이 필요해지지 않았다.** 프로덕션 사용처가 여전히 0 이면 **지운다** (R8 — 죽은 코드를 남기지 않는다). 테스트도 함께 정리한다
4. 지우지 않기로 판단했다면 **그 이유를 이 파일에 적는다.** "언젠가 쓸지도" 는 이유가 아니다

**결론을 이 작업서에 실측으로 남긴다.** 두 마일스톤을 미룬 항목이 세 번째로 미뤄지면 그것은 결정이 아니라 회피다.

### 선행 산출물 의존성

- step-03 의 `UnityEngine.UI` 어셈블리 참조
- step-07 의 `ISceneRouter` (먼저 실행됐다면)

### 밸런스 수치

**없다.**

### 제약

- 프레젠터는 `UnityEngine` 을 참조하지 않는다 (§3.6) — `SceneRouter` 만 안다
- **씬 이름 문자열이 `SceneNames` 밖에 나타나지 않는다** — `grep` 으로 확인 가능한 계약이다
- `FindObjectOfType` 금지 (§4.3)
- `Instantiate`/`Destroy` 금지 (§3.4)
- 구독·`onClick` 해제는 `OnDestroy` 에서
- 정적 가변 상태를 만들지 않는다 (RULE-01)
- **한글 문구는 폰트 서브셋에 들어간다** (step-11)

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
EditMode  StartGame_CallsLoadStageOnce
          StartGame_DoesNotLoadMain              ← 두 메서드를 뒤바꾼 구현을 배제
          OpenSettings_DoesNotChangeScene
          OpenCodex_DoesNotChangeScene
          SceneNames_AreDistinct                 ← 복붙으로 같아지는 것을 막는다
PlayMode  MainMenuView_Start_InvokesPresenter    ← 버튼이 실제로 배달되는가
```

> **`StartGame_DoesNotLoadMain` 이 없으면 공허하다.** `LoadStage`·`LoadMain` 둘 다 호출하는 구현도 `CallsLoadStageOnce` 를 통과한다.

> **`MainMenuView_Start_InvokesPresenter` 는 `EventSystem` 이 필요하다.** 없으면 클릭이 배달되지 않는다 — PlayMode 하네스에서 `EventSystem` 을 세우거나, `Button.onClick.Invoke()` 로 직접 부른다. **후자는 우회로이므로**, 씬에 `EventSystem` 이 실재하는지는 step-12 의 씬 테스트가 따로 본다.

### 주입 검증

| 주입 | 예측 |
|---|---|
| `StartGame` 이 `LoadMain` 을 부른다 | `StartGame_DoesNotLoadMain` |
| `SceneNames.Stage` 를 `"Stage1"` 로 오타 | (여기서는 안 잡힌다 — **step-12 의 Build Settings 테스트가 잡는다.** 그 사실 자체가 step-12 를 필요하게 만드는 근거다) |
| `OpenSettings` 가 씬을 로드한다 | `OpenSettings_DoesNotChangeScene` |

### 완료 판정

- [ ] `grep -rn '"Stage01"\|"Main"' Assets/Code/Scripts/Presentation/` 이 **`SceneNames.cs` 에서만** 나온다
- [ ] `grep -c "UnityEngine" Assets/Code/Scripts/Presentation/Presenters/MainMenuPresenter.cs` = **0**
- [ ] `ISceneRouter` 정의가 저장소에 **한 곳**뿐이다
- [ ] **`SushiEatenEventChannelSO` 의 처분이 이 파일에 결론으로 적혔다**
- [ ] EditMode · PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(ui): add main menu and scene routing
```

---

## 금지 사항

- 씬을 셋 이상 만들지 않는다 — 설정·백과사전은 패널이다
- 스테이지 선택·덱 편집을 넣지 않는다 (README D9)
- 프레젠터에서 `SceneManager` 를 직접 부르지 않는다
- 씬 이름 문자열을 두 번째 장소에 쓰지 않는다
- `DontDestroyOnLoad` 객체를 만들지 않는다 (README D1)
- 다른 어셈블리 파일을 수정하지 않는다
