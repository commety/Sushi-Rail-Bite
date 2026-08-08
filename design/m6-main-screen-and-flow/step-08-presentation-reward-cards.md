# Step 08: 보상 선택을 카드로 · 건너뛰기 버튼

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-03 (`CardView`)
- **후행 단계:** step-11(문구 서브셋) · step-12(씬 배치)

---

## 목적

보상 화면이 지금 **텍스트 목록 + 숫자 키**다. 무엇을 고르는지 그림이 없고, 마우스로는 아무것도 할 수 없으며, 건너뛰기가 `Esc` 키에만 있어 존재를 알 수 없다.

카드로 바꾸고 버튼을 붙인다. **프레젠터는 거의 그대로다** — 로직이 이미 옳게 나뉘어 있어서 바뀌는 것은 뷰 계약 한 줄이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Presenters/IRewardSelectionView.cs` — 수정 (**계약 변경**)
- `Assets/Code/Scripts/Presentation/Presenters/RewardSelectionPresenter.cs` — 수정 (넘기는 값만)
- `Assets/Code/Scripts/Presentation/Views/RewardSelectionView.cs` — 수정 (카드 + 버튼)
- `Assets/Tests/EditMode/UI/RewardSelectionPresenterTests.cs` — 수정
- `Assets/Tests/PlayMode/UI/RewardSelectionViewTests.cs` — 수정

### 계약 변경 — 이것 하나뿐이다 (README D2)

```diff
- void ShowOffers(IReadOnlyList<string> offerNames);
+ void ShowOffers(IReadOnlyList<RewardOffer> offers);
```

**문자열로는 아이콘을 그릴 수 없다.** 카드에는 그림이 필요하고, 그림은 `SushiData.Icon`·`CustomerData.Icon` 에 있다.

M5 의 D2 는 *"뷰의 public 계약을 바꾸지 않는다"* 를 회귀 그물로 삼았다. 여기서 그 그물의 **한 칸만** 푼다:

| 계약 | 처분 |
|---|---|
| `ShowOffers` 의 인자 타입 | **바꾼다** |
| `Hide` · `Choose` · `Skip` · `OfferCount` · `IsOpen` · `RewardChosen` · `Closed` | **그대로** |
| 후보 0개도 화면을 연다는 규칙 | **그대로** |
| 이미 가진 카드는 제시되지 않는다 (`RewardGenerator`) | **손대지 않는다** |

프레젠터 안의 `_names` 버퍼가 `_offers` 를 그대로 넘기는 것으로 바뀌므로 **버퍼 하나가 사라진다.** `RewardSelectionPresenterTests` 는 `ShowOffers` 를 받는 스텁의 시그니처만 고치면 나머지가 그대로 통과해야 한다 — 통과하지 않으면 계약을 하나 이상 바꾼 것이다.

### `RewardOffer.DisplayName` 의 처분 (README D7)

카드가 이름과 수치를 `CardCaption` 으로 만들면 `RewardOffer.DisplayName` 이 쓰이지 않게 될 수 있다.

1. `grep -rn "DisplayName" Assets/` 로 **실제 사용처를 확인한다**
2. 프로덕션 사용처가 0 이면 **지운다** — 죽은 코드를 남기지 않는다 (R8). 테스트만 쓰고 있으면 그 테스트도 함께 정리한다
3. 남아 있으면 그대로 둔다. **손님 보상에 영입 비용을 붙이는 규칙**은 `CardCaption.DetailOf(CustomerData)` 가 이어받아야 하며, 그러지 않으면 "얻고 나서 예산이 모자라 못 앉히는" 상황을 고르는 시점에 예측할 수 없다 — 그 주석이 이유를 이미 적어 두었다

**둘 중 어느 쪽으로 갔는지 이 작업서에 실측으로 남긴다.**

### 화면 구성

```
RewardPanel
├── TitleLabel        ("보상 선택")
├── CardRow
│   ├── Card 0 …  N   ← CardView, RewardCatalog.OfferCount 만큼 미리 놓는다 (지금 3)
└── SkipButton        ("건너뛰기") ← 최하위
```

- 카드 클릭 → `CardView.Clicked` → 뷰가 인덱스를 붙여 `presenter.Choose(i)`
- 건너뛰기 → `presenter.Skip()`
- **후보가 0개면 카드를 전부 끄고 "받을 보상 없음" 을 띄운다.** 건너뛰기 버튼은 그때 **유일한 출구**이므로 반드시 남는다
- 숫자 키·`Esc` 는 **남긴다.** 키보드가 있으면 더 빠르고, 이미 동작하며, 제거할 이유가 없다. 다만 안내 문구는 버튼이 대신하므로 화면에서 뺀다

### 뷰가 인덱스를 붙인다 — 판정이 아니다

`CardView` 는 자기가 몇 번째인지 모른다. 뷰가 자기 카드 배열에서 위치를 찾아 `Choose(i)` 를 부른다. **유효한 인덱스인지는 프레젠터가 답한다** — 범위 밖이면 `false` 를 돌려주고 화면을 그대로 두는 계약이 이미 있다.

### 선행 산출물 의존성

- step-03 의 `CardView` · `Card.prefab` · `CardCaption`
- 기존 `RewardSelectionPresenter` · `RewardOffer` · `RewardGenerator`

### 밸런스 수치

**없다.** 제시 개수는 이미 `RewardCatalog.OfferCount` 다.

### 제약

- 프레젠터는 `UnityEngine` 을 참조하지 않는다 (§3.6)
- 카드를 `Instantiate` 하지 않는다 (§3.4) — `OfferCount` 만큼 미리 놓는다
- `Update` 경로 할당 금지 (§4.3)
- `UnityEngine.Input` 금지 — 키 입력은 `Keyboard.current` 를 그대로 쓴다
- 구독·`onClick` 해제는 `OnDestroy` 에서
- **한글 문구는 폰트 서브셋에 들어간다** (step-11)

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
EditMode  (기존 프레젠터 테스트가 스텁 시그니처만 고치고 전부 통과해야 한다)
          ShowOffers_ReceivesOffersNotStrings          ← 계약 변경 자체를 고정
PlayMode  ShowOffers_Three_DrawsThreeCards
          ShowOffers_Empty_ShowsNoticeAndKeepsSkip     ← 유일한 출구가 남는가
          ClickCard_Second_ChoosesIndexOne             ← 인덱스가 어긋나지 않는가
          ClickSkip_Closes
          ShowOffers_Twice_DoesNotLeaveStaleCards
          ClickCard_AfterHide_DoesNothing
          KeyboardStillWorks_DigitOne_ChoosesFirst     ← 키 경로를 안 죽였는가
```

> **`ClickCard_Second_ChoosesIndexOne` 이 이 단계의 핵심이다.** *"클릭하면 골라진다"* 만 보면 **어느 카드를 눌러도 0번을 고르는** 구현이 통과한다. 두 번째 카드를 눌러 인덱스 1이 나오는지 확인한다.

> **`ShowOffers_Empty_ShowsNoticeAndKeepsSkip` 이 필요한 이유:** 후보 0개는 실제로 발생한다 (카탈로그의 카드를 전부 가진 뒤). 그때 건너뛰기가 사라지면 **화면에서 나갈 수 없다** — 진행이 통째로 막힌다.

### 주입 실측

| 주입 | 예측 | 실제 |
|---|---|---|
| 카드 클릭이 항상 `Choose(0)` 를 부른다 | 1건 | 예측대로 **1건** |
| 후보 0개일 때 건너뛰기를 숨긴다 | 1건 | 예측대로 **1건** |
| 숫자 키·`Esc` 경로를 통째로 지운다 | 1건 | **0건** — 아래 |

### 키보드 경로에는 테스트가 없다 — 만들지 않았고, 그 이유를 남긴다

`Update` 를 통째로 비워도 EditMode 650건이 전부 초록이었다. 작업서가 계획한
`KeyboardStillWorks_DigitOne_ChoosesFirst` 는 **이 구성으로는 쓸 수 없다**:

- 화면이 열려 있어야 도는 `Update` 이고 (§1 «가드가 걸린 `Update`»)
- 배치 모드에는 **키보드 장치가 없다** — `Keyboard.current` 가 `null` 이라 그 아래로 못 간다
- 키를 흉내 내려면 `Unity.InputSystem.TestFramework` 참조가 필요한데, 그것은 어셈블리
  구성 변경(§7)이고 이 단계의 범위가 아니다

**그래서 덮지 못한다는 사실을 코드 주석과 여기에 적었다.** 이 자리가 M5 에서 Enter·Esc 가
런타임에 죽어 있던 그 모양이며, 지금의 방어는 `InputBackendTests` 가 **레거시 입력이 꺼져
있다는 전제를 컴파일 시점에 고정**하는 것 하나뿐이다. 커버리지가 있는 척하지 않는다.

### 배선 구멍을 하나 실제로 잡았다

`ClickSkip` 이 처음에 실패했다. `Awake` 가 `Initialize` 보다 먼저 도는데 **`Subscribe` 를
`Awake` 에서만 걸어** 뒀고, 건너뛰기 버튼에는 이름 폴백도 없어 그 시점에 `null` 이었다 —
버튼은 화면에 멀쩡히 보이는데 눌러도 아무 일이 없다.

둘 다 고쳤다: `Initialize` 가 **구독을 다시 걸고**, `Awake` 가 이름으로도 버튼을 찾는다.
후보가 0개일 때 건너뛰기가 유일한 출구이므로, 이 구멍은 **화면에 갇히는** 형태로 드러났을 것이다.

> `DeckPanelView`·`StageMenuView` 도 같은 모양이다(버튼에 이름 폴백이 없고 `Initialize` 가
> 재구독하지 않는다). 그쪽은 인스펙터로 물리는 경로만 쓰므로 지금은 동작하지만,
> **step-12 에서 버튼을 실제로 배선할 때 같이 확인한다.**

### 완료 판정

- [x] `IRewardSelectionView` 에 `IReadOnlyList<string>` **0건**
- [x] `RewardSelectionPresenter` 의 `Choose`·`Skip`·이벤트 시그니처가 그대로다
- [x] `RewardOffer.DisplayName` 을 **지웠다** (아래)
- [x] EditMode **650/650** · PlayMode **209/209**
- [x] `./tests/preflight.sh` 전 항목 PASS

### `RewardOffer.DisplayName` 은 지웠다 — D7 이 여기서 끝난다

프레젠터가 후보를 그대로 넘기게 되면서 프로덕션 사용처가 **0** 이 됐다. 남겨 두면 죽은
코드다 (R8). 함께 `RewardOfferTests` 의 해당 테스트 다섯도 지웠다.

그 API 가 지고 있던 규칙 — *"손님 보상에는 영입 비용을 함께"* — 은 `CardCaption.DetailOf`
가 이어받았고, `ShowOffers_CustomerCard_ShowsRecruitCost` 가 화면까지 도달하는지 지킨다.

**이로써 이름 폴백 구현이 저장소 전체에서 한 곳이 됐다** —
`grep -rn "IsNullOrWhiteSpace" Assets/Code/Scripts/` 가 **1건**이다. README D7 이 세 곳에서
시작해 카드가 네 번째가 되는 것을 막겠다고 한 것이 여기서 완결된다.

### 뷰 테스트가 진짜 프레젠터를 쓴다

`RewardSelectionPresenter` 는 구체 타입이라 가짜를 물릴 수 없다. 카탈로그·런을 세워
**진짜를 쓰고 그 이벤트로 관측**했다 — 실제 배선을 그대로 지나므로 오히려 낫다.

뽑기 순서는 추첨이 정하므로 테스트가 알 수 없다. 그래서 **화면에 그려진 이름과 대조**해
«올바른 번호를 넘겼는가» 를 본다. 다만 고르면 화면이 닫히면서 카드가 비므로
**누르기 전에** 이름을 잡아 둬야 한다 (처음에 이걸 놓쳐 두 건이 실패했다).

### `git checkout` 이 이번 단계 작업을 지웠다

주입을 되돌리려고 `git checkout <파일>` 을 썼다가 **커밋되지 않은 이 단계의 재작성이 통째로
날아갔다.** RULES.md RULE-02 의 «원복은 의도한 변경도 함께 지운다» 와 같은 형태다 —
거기서는 빌드 스크립트가, 여기서는 내가 원복 도구를 썼다.

**주입은 넣을 때와 같은 방식으로 되돌린다** (편집 도구로 그 자리만). 파일 단위 원복은
«마지막 커밋 이후의 모든 것» 을 지운다.

### 예상 커밋 메시지

```
feat(ui): pick rewards as clickable cards with a skip button
```

---

## 금지 사항

- `ShowOffers` 외의 계약을 바꾸지 않는다 (README D2)
- 뷰에서 "이 보상을 골라도 되나" 를 판정하지 않는다
- 카드를 런타임에 생성하지 않는다
- 키보드 경로를 제거하지 않는다
- `RewardGenerator` 의 추첨 규칙에 손대지 않는다 — 난수 경계는 M3 이 정했다
- 다른 어셈블리 파일을 수정하지 않는다
