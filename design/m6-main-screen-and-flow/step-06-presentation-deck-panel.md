# Step 06: 인스테이지 초밥 덱 보기

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-03 (`CardView`)
- **후행 단계:** step-12 가 씬에 패널과 버튼을 놓는다

---

## 목적

**보상으로 얻은 초밥이 화면 어디에도 없다.** 벨트에 언젠가 흘러가긴 하지만, 지금 덱에 무엇이 있는지 확인할 방법이 없어 "카드를 받았다" 가 화면에서 사라진다. 로그라이트의 축적감이 통째로 안 보인다는 뜻이다.

덱 버튼 하나와 카드 그리드 하나면 된다. **읽기 전용이다** — 편집은 M6 의 범위가 아니다 (README D9).

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Presenters/IDeckPanelView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Presenters/DeckPanelPresenter.cs` — 생성
- `Assets/Code/Scripts/Presentation/Views/DeckPanelView.cs` — 생성
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (배선)
- `Assets/Tests/EditMode/UI/DeckPanelPresenterTests.cs` — 생성
- `Assets/Tests/PlayMode/UI/DeckPanelViewTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    /// <summary>덱 화면이 프레젠터에게 제공하는 것. <b>Unity 타입이 없다.</b></summary>
    public interface IDeckPanelView
    {
        /// <summary>덱을 화면에 올린다. <b>빈 덱도 온다</b> — 그때는 "덱이 비어 있음" 을 표시한다.</summary>
        void ShowDeck(IReadOnlyList<SushiData> cards);

        void Hide();
    }

    /// <summary>덱 보기의 로직. Unity API 를 모른다 (<c>CLAUDE.md</c> §3.6).</summary>
    public sealed class DeckPanelPresenter
    {
        public bool IsOpen { get; }
        public int CardCount { get; }

        public void Open(RunState run);
        public void Close();
        public void Toggle(RunState run);
    }
}
```

`IDeckPanelView` 가 `SushiData`(= `ScriptableObject`)를 받는 것은 **"Unity 타입이 없다" 를 어긴 것이 아니다.** 카드를 그리려면 아이콘이 필요하고, 아이콘은 데이터에 있다. EditMode 테스트는 `ScriptableObject.CreateInstance<SushiData>()` 로 만든다 ([`tests.md`](../../.claude/rules/tests.md) §4). 금지되는 것은 뷰가 `MonoBehaviour`·`Transform` 을 프레젠터에게 노출하는 것이다.

> `IRewardSelectionView` 는 문자열만 받도록 만들어졌지만 step-08 에서 같은 이유로 `RewardOffer` 를 받게 된다 (README D2). **두 인터페이스가 같은 기준을 쓰게 된다.**

### 덱은 런에서 온다

`RunState.Sushi.Cards` 가 진실이다. `StageConfig.SpawnTable` 을 읽으면 **보상으로 얻은 카드가 영영 안 보인다** — `SushiDeck` 의 클래스 주석이 이미 세운 규칙이며, M3 에서 벨트가 같은 실수를 하지 않도록 옮겨 놓은 것이다.

### 열어도 멈추지 않는다 (README D8)

덱 확인은 정보 조회다. 멈추는 것은 Menu(step-07)의 일이다. **한 화면이 둘을 하면 나중에 한쪽만 고쳐진다.**

`DeckPanelPresenter` 는 `PauseState` 를 **참조하지 않는다.** 참조가 없는 것이 이 결정의 증거이며, `grep` 으로 확인할 수 있다.

### 카드를 미리 놓아 둔다

덱 크기는 자란다 — 시작 5장 + 보상. 하지만 카드를 `Instantiate` 할 수 없다 (§3.4).

**프리팹에 카드를 상한만큼 미리 놓고 켜고 끈다.** 상한은 `CardCatalog.AllSushi.Count` 를 넘을 수 없다 — 덱에서 카드를 빼는 경로가 없고 중복도 안 들어가므로(`SushiDeck.TryAdd`), **덱은 카탈로그 크기를 절대 넘지 않는다.** 지금은 9장이다.

- 덱이 카드 자리보다 커지면 **있는 만큼만 그리고 예외를 내지 않는다** (`StageBootstrap.BindSlots` 가 자리 정의에 대해 내린 것과 같은 판단)
- 그 상태를 **테스트로 고정**한다 — 카드를 추가할 때 조용히 잘리는 것을 알아채야 한다

### 선행 산출물 의존성

- step-03 의 `CardView` · `Card.prefab`
- 기존 `RunState` · `SushiDeck`

### 밸런스 수치

**없다.** 그리드 열 수·카드 자리 수는 프리팹의 직렬화 값이다.

### 제약

- 프레젠터는 `UnityEngine` 을 참조하지 않는다 (§3.6) — `grep -c "UnityEngine" DeckPanelPresenter.cs` = 0
- 뷰는 규칙을 갖지 않는다 — 열려 있는지도 프레젠터가 답한다 (`RewardSelectionView` 와 같은 형태)
- `Instantiate`/`Destroy` 금지 (§3.4)
- 구독·`onClick` 해제는 `OnDestroy` 에서 ([`scripts.md`](../../.claude/rules/scripts.md) §6)
- **한글 문자열은 폰트 서브셋에 들어간다** — step-11 이 재추출한다

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
EditMode  Open_WithDeck_ShowsAllCards
          Open_EmptyDeck_StillOpens                  ← 조용히 안 열리면 고장과 구분되지 않는다
          Toggle_WhenOpen_Closes
          Toggle_WhenClosed_Opens
          Open_AfterRewardAdded_IncludesNewCard      ← 이 단계의 실제 이유
          Presenter_DoesNotTouchPause                ← 참조가 없다는 것을 코드로 고정하기 어렵다면 grep 완료 판정으로 대체
PlayMode  ShowDeck_MoreCardsThanSlots_DrawsWhatFits
          ShowDeck_Twice_DoesNotLeaveStaleCards      ← 두 번째가 더 적을 때 앞의 것이 남는가
          Hide_AfterShow_DisablesEveryCard
```

> **`ShowDeck_Twice_DoesNotLeaveStaleCards` 를 줄어드는 방향으로 쓴다.** 5장 → 3장 순서여야 남는 카드가 드러난다. 늘어나는 방향은 덮어써지므로 잘못된 구현도 통과한다.

### 주입 실측

| 주입 | 예측 | 실제 |
|---|---|---|
| `Close` 가 열림 여부를 보지 않는다 | 1건 | 예측대로 **1건** |
| `ShowDeck` 에서 남는 카드 비우기 제거 | 1건 | 예측대로 **1건** |
| 덱 출처를 첫 두 장으로 자른다 (스폰 구성을 읽는 것과 같은 효과) | 1건 | **2건** — 보상 테스트와 **빈 덱 테스트**가 함께 |

> **빈 덱 테스트가 출처 주입을 함께 잡았다.** 덱이 비었는데 앞 두 장을 읽으면 범위 밖
> 접근으로 터진다 — «빈 덱도 연다» 를 지키려고 넣은 테스트가 «덱을 어디서 읽는가» 까지
> 덮은 셈이다.

### 완료 판정

- [x] `DeckPanelPresenter` 에 `UnityEngine` **0건**
- [x] `DeckPanelPresenter` 에 `PauseState` **0건** (D8 — 덱은 시간을 멈추지 않는다)
- [x] 뷰·프레젠터 어디에도 `SpawnTable` **0건** (덱의 진실은 런에 있다)
- [x] EditMode **641/641** · PlayMode **199/199**
- [x] `./tests/preflight.sh` 전 항목 PASS

### 계약을 작업서와 다르게 갔다 — 런을 생성자에서 받는다

작업서는 `Open(RunState run)` 이었지만 **`DeckPanelPresenter(IDeckPanelView, RunState)` +
`Open()`/`Close()`/`Toggle()`** 로 바꿨다.

덱 버튼이 인자를 넘길 방법이 없다. 뷰에 런을 들려 주면 **화면이 런 상태를 알게 되어**
인터페이스가 Unity 밖 타입까지 끌고 오고, 그러면 «뷰는 규칙을 갖지 않는다» 가 흐려진다.
프레젠터의 수명은 런과 같으므로 `EnsureRunScope` 에서 `Rewards`·`Transition` 옆에 선다.

### 뷰 컴포넌트가 패널 바깥에 산다

덱 버튼은 **패널이 닫혀 있을 때도 눌려야** 한다. 컴포넌트가 패널 안에 있으면 닫는 순간
버튼까지 함께 꺼져 다시 열 수 없다. `DeckPanelView` 는 늘 켜져 있는 오브젝트에 붙고
`_panelRoot` 만 켜고 끈다 — `DigestingBadgeView._visual` 과 같은 형태다.

### 씬 배치는 아직이다

`StageBootstrap` 이 없으면 조용히 건너뛰므로(보상·전환과 같은 판단) 씬을 건드리지 않아도
아무것도 깨지지 않는다. 카드 자리·버튼 배치는 **step-12** 다.

### 예상 커밋 메시지

```
feat(ui): show the current sushi deck in stage
```

---

## 금지 사항

- 덱 편집 기능을 넣지 않는다 (README D9) — 읽기 전용이다
- 덱을 열 때 일시정지하지 않는다 (README D8)
- 카드를 런타임에 생성하지 않는다
- 뷰에 열림/닫힘 판정을 두지 않는다
- 다른 어셈블리 파일을 수정하지 않는다
