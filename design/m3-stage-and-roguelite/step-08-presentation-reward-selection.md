# Step 08: 보상 선택 화면 (MVP)

- **영역:** `presentation` — 어셈블리 `Presentation` (Presenter 는 `Tests.EditMode` 대상)
- **선행 단계:** step-06 (`RewardGenerator`) · step-07 (부트스트랩 조립). **둘 다 필요하다**
- **후행 단계:** step-09 가 실제 보상 애셋을 채워 이 화면이 빈 목록이 아니게 만든다

---

## 목적

M3 의 마지막 기능 조각이다 — **클리어하면 보상 후보가 뜨고, 고른 것이 런의 덱에 들어간다.**

이 프로젝트에서 **MVP 패턴을 처음 쓰는 지점**이다 (`CLAUDE.md` §3.6). 화면 로직을 `MonoBehaviour` 안에 두면 EditMode 로 검증할 수 없고, M6 에서 UI 를 갈아 끼울 때 로직까지 다시 짜게 된다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Presenters/IRewardSelectionView.cs` — **생성**
- `Assets/Code/Scripts/Presentation/Presenters/RewardSelectionPresenter.cs` — **생성**
- `Assets/Code/Scripts/Presentation/Views/RewardSelectionView.cs` — **생성**
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (클리어 시 화면을 띄운다)
- `Assets/Tests/EditMode/UI/RewardSelectionPresenterTests.cs` — **생성**
- `Assets/Tests/EditMode/UI/FakeRewardSelectionView.cs` — **생성** (손으로 쓴 스텁)
- `Assets/Tests/PlayMode/UI/RewardSelectionViewTests.cs` — **생성**

> `Presenters/` · `Views/` 폴더는 `CLAUDE.md` §10 의 디렉터리 구조에 이미 예고돼 있다. 지금 저장소에는 없으므로 여기서 처음 생긴다.

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    /// <summary>
    /// 보상 화면이 프레젠터에게 제공하는 것. <b>Unity 타입이 하나도 없다</b> —
    /// 프레젠터를 EditMode 로 검증하기 위해서다 (<c>CLAUDE.md</c> §3.6).
    /// </summary>
    public interface IRewardSelectionView
    {
        /// <summary>후보를 화면에 올린다. 빈 목록이면 "받을 보상 없음" 을 표시한다.</summary>
        void ShowOffers(IReadOnlyList<string> offerNames);

        /// <summary>화면을 내린다.</summary>
        void Hide();
    }

    /// <summary>
    /// 보상 선택의 로직. <b>Unity API 를 모른다.</b>
    /// </summary>
    public sealed class RewardSelectionPresenter
    {
        public RewardSelectionPresenter(IRewardSelectionView view, RewardGenerator generator);

        /// <summary>지금 제시 중인 후보 수. 0 이면 고를 것이 없다.</summary>
        public int OfferCount { get; }

        /// <summary>화면이 떠 있나.</summary>
        public bool IsOpen { get; }

        /// <summary>보상을 골랐다. 인자는 반영된 보상이며, 건너뛴 경우 발생하지 않는다.</summary>
        public event Action<RewardOffer> RewardChosen;

        /// <summary>화면이 닫혔다 — 선택했든 건너뛰었든 발생한다.</summary>
        public event Action Closed;

        /// <summary>이 런의 보상 후보를 뽑아 화면을 연다.</summary>
        public void Open(RunState run);

        /// <summary>후보 하나를 고른다. 범위 밖 인덱스면 <c>false</c> 를 돌려주고 화면을 유지한다.</summary>
        public bool Choose(int index);

        /// <summary>아무것도 고르지 않고 닫는다. 후보가 0개일 때의 유일한 출구이기도 하다.</summary>
        public void Skip();
    }
}
```

### 후보가 0개일 때

**정상 동작이다.** 착수 시점의 실제 상태이기도 하다 (README §현황 2 — 초밥 5종이 전부 시작 덱에 있다).

- `Open` 은 여전히 화면을 연다. `ShowOffers(빈 목록)` 를 부르고, 뷰가 "받을 보상 없음" 을 표시한다
- `Skip()` 만이 출구다
- **`Open` 을 조용히 건너뛰지 않는다.** 건너뛰면 클리어했는데 아무 화면도 안 뜨는 상태가 되어, 보상 시스템이 고장 난 것과 구분되지 않는다

### `StageBootstrap` 의 연결

```csharp
private void OnOutcomeDecided(StageOutcome outcome)
{
    if (outcome == StageOutcome.Cleared) _rewards?.Open(Run);
    // 실패는 재시도 경로다 — 보상 화면을 띄우지 않는다
}
```

`RewardChosen` · `Closed` 구독도 `Teardown` 에서 해제한다.

**다음 스테이지로 넘어가는 것(`Run.AdvanceStage()`)은 이 단계에서 부르지 않는다.** 다음 스테이지 씬·설정이 M4 이기 때문이다. `Closed` 이벤트만 발행해 두고, 지금은 아무도 구독하지 않아도 된다 — **M4 가 붙일 자리를 비워 두는 것이 목적**이며, 그 사실을 주석에 적는다.

### `RewardSelectionView` — placeholder

`StageHudView` 의 선례를 그대로 따른다:

- `TextMesh` 로 그린다. 씬에 Canvas 가 없고 전부 월드 스페이스다
- `PlaceholderLabel.Resolve` / `Write` 를 재사용한다 — **라벨 조회·쓰기 코드를 복제하지 않는다**
- 클래스 주석에 **"M6(메인화면·덱빌딩)에서 제대로 된 UI 로 교체될 placeholder 다"** 를 명시한다
- 입력은 숫자 키(1·2·3)로 고르고 `Esc` 로 건너뛰는 수준까지. 클릭 히트박스를 만들지 않는다

### 선행 산출물 의존성

- `RewardGenerator` · `RewardOffer` — step-06
- `RunState` — step-05
- `StageBootstrap.Run` · `OutcomeDecided` 배선 — step-07
- `PlaceholderLabel` — 이미 있다 (`Presentation/UI/`)

### 밸런스 수치

**없다.** 제시 개수는 `RewardCatalog.OfferCount` 에서 생성기를 거쳐 온다.

### 제약

- **`RewardSelectionPresenter` 가 `UnityEngine` 을 참조하지 않는다.** 이 단계의 핵심 계약이다
- 테스트 더블은 **손으로 쓴 스텁**(`FakeRewardSelectionView`). NSubstitute 같은 패키지를 추가하지 않는다 (`CLAUDE.md` §7)
- 뷰가 규칙을 복제하지 않는다 — "고를 수 있나" 는 프레젠터가 답한다 (`CustomerPlacementController` 의 선례)
- 이벤트 구독은 전부 해제 경로를 갖는다
- `Runtime` · `Runtime.Data` 를 수정하지 않는다
- 새 프리팹·머티리얼은 `Assets/Code/Scripts/Presentation/` 옆에 만든다. `Assets/Art/` 는 심링크라 워크트리 git 이 새 파일을 못 본다 (`.claude/rules/parallel-work.md` §2)
- 씬 편집은 **한 번에 한 워크트리만**

### 완료 판정

- [ ] `grep -n "UnityEngine" Assets/Code/Scripts/Presentation/Presenters/RewardSelectionPresenter.cs Assets/Code/Scripts/Presentation/Presenters/IRewardSelectionView.cs` — **0건**
- [ ] `grep -rn "TextMesh\|MonoBehaviour" Assets/Code/Scripts/Presentation/Presenters/` — **0건**
- [ ] `grep -n "PlaceholderLabel" Assets/Code/Scripts/Presentation/Views/RewardSelectionView.cs` — 라벨 처리를 **재사용**한다
- [ ] `grep -n "RewardChosen\|Closed" Assets/Code/Scripts/Presentation/StageBootstrap.cs` — `+=` 와 `-=` 가 짝을 이룬다
- [ ] `git diff --stat Assets/Code/Scripts/Runtime/ Assets/Code/Scripts/Runtime.Data/` — **0줄**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름

```
RewardSelectionPresenterTests (EditMode — 대부분이 여기 있다)
  Open_WithOffers_ShowsThemOnTheView
  Open_NoOffers_StillOpensWithEmptyList              ← ★ 조용히 건너뛰지 않는다
  Choose_ValidIndex_AppliesToRunAndCloses
  Choose_ValidIndex_RaisesRewardChosenOnce
  Choose_OutOfRange_ReturnsFalseAndStaysOpen
  Choose_AfterClosed_ReturnsFalse
  Skip_Open_ClosesWithoutChangingRun                 ← ★ 런이 그대로임을 함께 확인
  Skip_RaisesClosedButNotRewardChosen
  Choose_SushiOffer_CardAppearsInDeck                ← ★ 보상이 실제로 반영된다
  Choose_CustomerOffer_MemberAppearsInRoster

RewardSelectionViewTests (PlayMode — 최소한만)
  ShowOffers_ThreeNames_WritesAllThree
  ShowOffers_Empty_WritesNoRewardNotice
  Hide_AfterShow_ClearsLabels
```

> **★ `Skip_Open_ClosesWithoutChangingRun`** 은 **덱 크기와 명부 크기를 둘 다** 확인한다. 하나만 보면 반대쪽에 몰래 추가하는 구현이 통과한다.
>
> **★ `Choose_SushiOffer_CardAppearsInDeck`** 이 완료 판정 *"고른 보상이 덱/손님 목록에 반영된다"* 를 코드로 고정한다. `RewardChosen` 이 발생했는지만 보지 말고 **`run.Sushi.Contains(그 카드)` 를 직접 확인**한다 — 이벤트만 쏘고 반영은 안 하는 구현이 있을 수 있다.
>
> `Choose_ValidIndex_RaisesRewardChosenOnce` 는 **횟수를 센다.** "발생했다" 만 보면 두 번 쏘는 구현이 통과한다.

### 예상 커밋 메시지

```
feat(reward): add the reward selection screen
```

---

## 금지 사항

- 프레젠터에 Unity API 를 넣지 않는다. `Debug.Log` 도 안 된다
- 후보가 0개라고 `Open` 을 건너뛰지 않는다
- 실패 시에 보상 화면을 띄우지 않는다
- `Run.AdvanceStage()` 를 여기서 부르지 않는다. 다음 스테이지는 M4 다
- 보상 확률·희귀도·재추첨을 만들지 않는다
- 새 패키지(NSubstitute 등)를 추가하지 않는다 (§7)
- `Runtime` · `Runtime.Data` 를 수정하지 않는다
- `Assets/Art/` 등 심링크 폴더에 파일을 만들지 않는다 (RULE-02)
