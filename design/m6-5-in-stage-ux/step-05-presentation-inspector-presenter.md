# Step 05: 손님 정보 창의 프레젠터

- **영역:** `presentation` — 어셈블리 `Presentation` (단, Unity API 를 모르는 순수 C#)
- **선행 단계:** 없음. step-01 이 끝나 있으면 인구수 한 줄이 더 붙는다
- **후행 단계:** step-06 이 이 프레젠터에 뷰와 클릭을 물린다

---

## 목적

앉아 있는 손님의 **정적 스탯과 살아 있는 상태**를 한 창에 모은다. 판정·표시를 나누는 이 프로젝트의 규칙대로, 이 단계는 **문자열을 만드는 쪽까지**만 만든다. 화면도 입력도 다음 단계다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Presenters/ICustomerInspectorView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/CustomerInspectorPresenter.cs` — 생성
- `Assets/Tests/EditMode/UI/FakeCustomerInspectorView.cs` — 생성
- `Assets/Tests/EditMode/UI/CustomerInspectorPresenterTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    /// <summary>정보 창의 화면 쪽 계약. 구현은 MonoBehaviour 지만 여기서는 모른다.</summary>
    public interface ICustomerInspectorView
    {
        /// <summary>창을 띄우고 정적 스탯을 그린다.</summary>
        void ShowCustomer(string name, string kind, string stats);

        /// <summary>매 프레임 바뀔 수 있는 줄만 다시 쓴다.</summary>
        void RefreshLive(string state, string saturation, string remaining);

        void Hide();
    }
}
```

```csharp
namespace SushiDefense.UI
{
    public sealed class CustomerInspectorPresenter
    {
        public CustomerInspectorPresenter(ICustomerInspectorView view, StageWindowArbiter windows);

        public bool IsOpen { get; }

        /// <summary>지금 보고 있는 손님. 닫혀 있으면 <c>null</c>. 검증용이다.</summary>
        public CustomerLogic Target { get; }

        /// <summary>이 손님을 연다. 더 높은 창이 떠 있으면 열리지 않는다.</summary>
        public void Open(CustomerLogic customer);

        public void Close();

        /// <summary>살아 있는 줄만 다시 그린다. 값이 안 바뀌었으면 뷰를 부르지 않는다.</summary>
        public void Tick();
    }
}
```

### 표시 항목

| 줄 | 출처 | 갱신 |
|---|---|---|
| 이름 | `CardCaption.NameOf(data)` | 정적 |
| 유형 | `CustomerKind` → 한글 (`기본`·`소식`·`먹보`) | 정적 |
| 범위 · 대역 · 먹는 시간 · 소화 시간 · 영입 비용 · **인구수** | `CustomerData` | 정적 |
| 상태 | `CustomerState` → 한글 | **실시간** |
| `현재/최대` 포화도 | `CustomerRuntimeState` | **실시간** |
| 소화 남은 시간 | `RemainingDigestSeconds` — 소화 중이 아니면 빈 문자열 | **실시간** |

**정적과 실시간을 두 메서드로 가른다.** 합치면 매 프레임 정적 문자열까지 다시 만들게 되고, 그것이 곧 `Update` 경로 할당이다 (`CLAUDE.md` §4.3).

`CustomerKind`·`CustomerState` → 한글 변환은 **이 프레젠터 안의 `switch` 하나**에 둔다. `CustomerKind` 는 «표시·정렬용이지 로직 분기용이 아니다» 라고 그 enum 자신이 적어 두었으므로, 표시로 쓰는 것은 규칙 위반이 아니다.

### 선행 산출물 의존성

- `SushiDefense.UI.StageWindowArbiter` · `StageWindow.CustomerInfo` — **이미 있다** (M6)
- `SushiDefense.Customers.CustomerLogic` · `CustomerRuntimeState` — 이미 있다
- `SushiDefense.Data.CustomerData.Population` — step-01. **없으면 그 줄만 빼고 진행하고, step-01 머지 뒤 한 줄을 더한다**

### 밸런스 수치

없다.

### 제약

- **`using UnityEngine` 을 쓰지 않는다.** `RewardSelectionPresenter`·`DeckPanelPresenter` 와 같은 자리다 (M6 D5). `Mathf.CeilToInt` 가 필요하면 `(int)System.Math.Ceiling(...)` 을 쓴다
- **판을 멈추지 않는다.** `PauseState` 를 생성자에서 받지도 않는다 — 받으면 언젠가 쓰게 된다 (README D5)
- **조정자를 반드시 거친다.** `Open` 은 `_windows.TryOpen(StageWindow.CustomerInfo)` 가 `false` 면 아무 일도 하지 않는다. `Close` 는 `_windows.Close(...)` 를 부른다
- `Tick` 은 **값이 바뀐 경우에만** 뷰를 부른다. 직전 값 셋을 필드로 들고 비교한다
- 닫힌 상태에서 `Tick` 이 불려도 아무 일이 없어야 한다

### 테스트 계획 (TDD)

```
자격/조정   Open_WhileMenuOpen_DoesNotShow          ← 조정자가 거절한다
            Open_WhileRewardOpen_StillShows         ← 보상은 밑에 깔리는 창이라 막지 않는다
            Open_WhileDeckOpen_DoesNotShow
            Close_WhileOpen_ReleasesTheWindow       ← 닫은 뒤 메뉴가 열리는지로 확인
정적        Open_Customer_ShowsNameKindAndStats     ← 대역·범위·영입 비용 구체값을 박는다
            Open_BigEater_ShowsPopulationTwo        ← step-01 뒤
실시간      Tick_SaturationChanged_RefreshesLive
            Tick_NothingChanged_DoesNotCallTheView  ← 호출 횟수를 센다
            Tick_Digesting_ShowsRemaining
            Tick_NotDigesting_LeavesRemainingBlank  ← 위와 짝. 상수를 돌려주는 구현을 배제한다
            Tick_WhileClosed_DoesNothing
전환        Open_AnotherCustomer_ReplacesTheTarget  ← 창을 닫았다 열지 않는다
```

- **`Open_WhileRewardOpen_StillShows` 를 반드시 넣는다.** `Reward` 는 조정자의 유일한 양방향 예외라 «가장 아래 창은 남의 위에 겹치지 않는다» 규칙과 정면으로 맞물린다 — 실제 동작을 여기서 확정해 둔다
  - 조정자 코드를 읽어 보면 `window == Lowest && _open.Count > 0` 이 먼저 걸려 **거절된다.** 그 동작이 맞는지 아키텍트에게 확인하고, 맞으면 테스트 이름을 `Open_WhileRewardOpen_DoesNotShow` 로 뒤집는다. **추측으로 정하지 않는다**
- `Tick_NothingChanged_DoesNotCallTheView` 는 페이크 뷰의 호출 횟수로 본다. **«둘이 같다» 만 보지 않는다**

`FakeCustomerInspectorView` 는 손으로 쓴 스텁이다 (`tests.md` §3 — NSubstitute 도입은 팀 합의 사항).

### 완료 판정

- [ ] `grep -n "using UnityEngine" Assets/Code/Scripts/Presentation/Presenters/CustomerInspectorPresenter.cs` — **0건**
- [ ] `grep -n "PauseState" Assets/Code/Scripts/Presentation/Presenters/CustomerInspectorPresenter.cs` — **0건**
- [ ] `grep -n "StageWindow.CustomerInfo" Assets/Code/Scripts/Presentation/Presenters/CustomerInspectorPresenter.cs` — `TryOpen`·`Close` 2건
- [ ] `./tests/run-tests.sh` EditMode 전량 Green
- [ ] 깨뜨려 보기: `TryOpen` 의 반환을 무시하도록 고치면 `Open_WhileMenuOpen_DoesNotShow` 가 죽는지 확인하고 되돌린다

### 예상 커밋 메시지

```
feat(ui): add the customer inspector presenter
```

---

## 금지 사항

- `MonoBehaviour` 를 만들지 않는다 (step-06).
- `StageWindow` 열거형에 값을 더하지 않는다. `CustomerInfo` 가 이미 있다.
- 정보 창에서 손님을 물리거나(배치 취소) 조작하는 경로를 넣지 않는다. **읽기 전용이다** — 요구에 없다.
