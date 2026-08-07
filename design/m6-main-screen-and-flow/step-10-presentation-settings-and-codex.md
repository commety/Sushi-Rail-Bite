# Step 10: 설정 화면 · 백과사전

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-01 (`CardCatalog`) · step-02 (`GameSettings`·`ISettingsStore`) · step-03 (`CardView`) · step-09 (메인 화면)
- **후행 단계:** step-11(문구 서브셋) · step-12(씬 조립)

---

## 목적

메인 화면의 나머지 두 버튼이 열 화면을 만든다.

- **설정** — 볼륨(마스터 하나)과 전체화면. 다시 켜도 유지된다
- **백과사전** — 이 게임에 있는 초밥 8종·손님 3종을 카드로 본다

둘 다 메인 씬 안의 패널이며, 로직은 프레젠터에 있고 Unity 호출은 인터페이스 뒤에 있다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Presenters/ISettingsView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/SettingsPresenter.cs` — 생성
- `Assets/Code/Scripts/Presentation/Views/SettingsView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Settings/PlayerPrefsSettingsStore.cs` — 생성
- `Assets/Code/Scripts/Presentation/Settings/SettingsApplier.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/ICodexView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/CodexPresenter.cs` — 생성
- `Assets/Code/Scripts/Presentation/Views/CodexView.cs` — 생성
- `Assets/Tests/EditMode/UI/SettingsPresenterTests.cs` — 생성
- `Assets/Tests/EditMode/UI/CodexPresenterTests.cs` — 생성
- `Assets/Tests/PlayMode/UI/CodexViewTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    public interface ISettingsView
    {
        /// <summary>지금 값을 화면에 반영한다. 슬라이더·토글이 여기서 맞춰진다.</summary>
        void ShowSettings(float masterVolume, bool fullscreen);

        void Hide();
    }

    public sealed class SettingsPresenter
    {
        public SettingsPresenter(ISettingsView view, GameSettings settings,
                                 ISettingsStore store, ISettingsApplier applier);

        public bool IsOpen { get; }
        public void Open();
        public void Close();                        // 닫을 때 저장한다
        public void SetMasterVolume(float value);
        public void SetFullscreen(bool value);
    }

    /// <summary>고른 설정을 실제 엔진에 먹인다. 구현이 Unity 를 안다.</summary>
    public interface ISettingsApplier
    {
        void Apply(GameSettings settings);
    }

    public interface ICodexView
    {
        void ShowEntries(IReadOnlyList<SushiData> sushi, IReadOnlyList<CustomerData> customers);
        void Hide();
    }

    public sealed class CodexPresenter
    {
        public CodexPresenter(ICodexView view, CardCatalog catalog);
        public bool IsOpen { get; }
        public int EntryCount { get; }
        public void Open();
        public void Close();
    }
}

namespace SushiDefense.Settings
{
    /// <summary><see cref="PlayerPrefs"/> 에 남긴다. WebGL 에서는 IndexedDB 로 간다.</summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore { }

    /// <summary><c>AudioListener.volume</c> · <c>Screen.fullScreen</c> 을 쓴다.</summary>
    public sealed class SettingsApplier : ISettingsApplier { }
}
```

### 층을 셋으로 나눈다 — 오디오와 같은 구조

M5 의 오디오가 값/판정/재생 셋으로 나뉜 것과 같은 이유다.

| 층 | 사는 곳 | 하는 일 |
|---|---|---|
| **값** | `GameSettings` (`Runtime`) | 무엇을 골랐나 · 클램프 |
| **저장** | `ISettingsStore` → `PlayerPrefsSettingsStore` | 다음에도 남나 |
| **적용** | `ISettingsApplier` → `SettingsApplier` | 엔진에 먹인다 |

합치면 *"소리가 안 줄었다"* 의 원인이 값인지 저장인지 적용인지 테스트에서 구분되지 않는다 — `SoundBudget` 과 `AudioUnlockGate` 를 합치지 않은 것과 같은 판단이다.

### 볼륨은 `AudioListener.volume` 하나 (아키텍트 결정)

마스터 하나이므로 `AudioDirector` 를 고칠 필요가 없다. **큐별 상대 볼륨은 `AudioBankSO` 가 이미 들고 있고**, 마스터는 그 위에 한 번 곱해지는 값이다. 둘을 나눈 덕에 이 단계가 오디오 코드를 건드리지 않는다.

### 전체화면 — WebGL 의 제약을 미리 안다

- `Screen.fullScreen = true` 는 **사용자 제스처 안에서만** 먹는다. 토글을 누르는 것이 그 제스처이므로 정상 경로에서는 동작한다. **`Load` 직후 자동 적용은 무시될 수 있다** — 저장된 값이 전체화면이어도 페이지를 새로 열면 창 모드로 뜰 수 있고, 그것을 버그로 취급하지 않는다
- `Screen.fullScreenMode` 의 데스크톱 전용 모드는 WebGL 에서 의미가 없다. `fullScreen` 불리언만 쓴다
- **화면에 상태를 되읽어 표시한다.** 적용이 거부됐을 때 토글만 켜져 있으면 플레이어가 무엇이 참인지 알 수 없다

이 셋을 **`SettingsApplier` 의 클래스 주석에 남긴다.** 다음 사람이 "전체화면이 안 되는데요" 에서 멈춘다.

### 저장 시점은 닫을 때다

슬라이더를 끄는 동안 매 프레임 저장하면 `PlayerPrefs` 쓰기가 프레임마다 돈다. **적용은 즉시, 저장은 닫을 때**로 나눈다 — 소리는 바로 들려야 하고 디스크는 그럴 필요가 없다.

### 설정은 헤더가 고정이다 (원문 §M6)

패널 위쪽에 제목 줄을 고정하고 내용만 스크롤한다. 지금은 항목이 둘뿐이라 스크롤이 필요 없지만, **헤더를 나중에 얹으면 레이아웃을 다시 짠다.**

### 백과사전은 카탈로그를 그대로 늘어놓는다

`CardCatalog.AllSushi` + `AllCustomers` 를 카드로 그린다. **소유 여부로 가리지 않는다** — 사전은 무엇이 있는지 알려 주는 화면이고, 미획득 카드를 숨기면 사전이 아니다.

- **초밥에는 설명문이 없다.** `SushiData` 에 `_description` 필드가 없다 (`CustomerData` 에만 있다). 카드의 수치 줄로 채우고, **설명이 필요하다는 판단이 서면 SO 필드 추가는 별건**이다 (§7)
- 카드는 미리 놓아 두고 켜고 끈다 (§3.4). 자리 수는 `AllSushi.Count + AllCustomers.Count` 를 덮어야 한다 — 지금 11
- 카탈로그가 자리보다 크면 **있는 만큼 그리고 예외를 내지 않는다.** 그 상태를 테스트로 고정한다

### 선행 산출물 의존성

- step-01 의 `CardCatalog`
- step-02 의 `GameSettings` · `ISettingsStore`
- step-03 의 `CardView` · `Card.prefab`
- step-09 의 메인 화면 (두 패널을 여는 곳)

### 밸런스 수치

**없다.** 마스터 볼륨 기본값 `1.0` 은 `GameSettings` 의 코드 기본값이다 — 밸런스가 아니라 *"설정을 만진 적 없음"* 의 값이다.

### 제약

- 프레젠터는 `UnityEngine` 을 참조하지 않는다 (§3.6) — `PlayerPrefs`·`Screen`·`AudioListener` 는 구현 쪽에만
- `Instantiate`/`Destroy` 금지 (§3.4)
- `Update` 경로 할당 금지 (§4.3)
- 구독·`onClick`·슬라이더 `onValueChanged` 해제는 `OnDestroy` 에서
- 정적 가변 상태 금지 (RULE-01)
- **한글 문구는 폰트 서브셋에 들어간다** (step-11)

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
설정   Open_ShowsCurrentValues
       SetMasterVolume_AppliesImmediately          ← 적용이 즉시인가
       SetMasterVolume_DoesNotSaveYet              ← 저장은 아직 아니다
       Close_SavesOnce
       Close_Twice_SavesOnce
       Open_AfterStoreLoad_ShowsStoredValues
       SetMasterVolume_AboveOne_ShowsClampedValue  ← 클램프가 화면까지 도달하는가
백과   Open_ShowsEverySushiAndCustomer
       Open_EmptyCatalog_StillOpens
       EntryCount_MatchesCatalog
PlayMode  ShowEntries_MoreThanSlots_DrawsWhatFits
          ShowEntries_Twice_DoesNotLeaveStaleCards
          Hide_AfterShow_DisablesEveryCard
```

> **`SetMasterVolume_DoesNotSaveYet` 과 `Close_SavesOnce` 를 짝으로 둔다.** 하나만 있으면 "매번 저장" 과 "한 번도 저장 안 함" 중 하나가 통과한다.

> **`SetMasterVolume_AboveOne_ShowsClampedValue` 는 step-02 의 클램프 테스트와 다른 것을 본다.** 저기는 모델이 자르는지, 여기는 **잘린 값이 화면까지 오는지**다. 프레젠터가 원본을 그대로 뷰에 넘기면 슬라이더와 실제 볼륨이 어긋난다.

> **`ShowEntries_Twice_DoesNotLeaveStaleCards` 는 줄어드는 방향으로 쓴다** (step-06 과 같은 이유).

### 주입 검증

| 주입 | 예측 |
|---|---|
| `SetMasterVolume` 이 저장까지 한다 | `SetMasterVolume_DoesNotSaveYet` |
| `Close` 가 저장하지 않는다 | `Close_SavesOnce` |
| 프레젠터가 클램프 전 값을 뷰에 넘긴다 | `SetMasterVolume_AboveOne_ShowsClampedValue` |
| 백과사전이 `RewardCatalog` 를 읽는다 | `Open_ShowsEverySushiAndCustomer` (시작 덱 5종이 빠진다) |

### 완료 판정

- [ ] `grep -rn "PlayerPrefs\|Screen\.\|AudioListener" Assets/Code/Scripts/Presentation/Presenters/` = **0건**
- [ ] `grep -rn "RewardCatalog" Assets/Code/Scripts/Presentation/Presenters/CodexPresenter.cs` = **0건**
- [ ] `SettingsApplier` 의 클래스 주석에 WebGL 전체화면 제약 3줄이 있다
- [ ] EditMode · PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(ui): add settings and codex screens
```

---

## 금지 사항

- 프레젠터에서 `PlayerPrefs`·`Screen`·`AudioListener` 를 직접 부르지 않는다
- 볼륨을 둘 이상으로 나누지 않는다 (아키텍트 결정)
- 백과사전에서 미획득 카드를 숨기지 않는다
- `SushiData` 에 설명 필드를 추가하지 않는다 — 별건이다 (§7)
- 카드를 런타임에 생성하지 않는다
- 다른 어셈블리 파일을 수정하지 않는다
