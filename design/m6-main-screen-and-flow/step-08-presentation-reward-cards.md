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

### 주입 검증

| 주입 | 예측 |
|---|---|
| 카드 클릭이 항상 `Choose(0)` 를 부른다 | `ClickCard_Second_ChoosesIndexOne` |
| 후보 0개일 때 건너뛰기 버튼을 끈다 | `ShowOffers_Empty_ShowsNoticeAndKeepsSkip` |
| `Hide` 에서 카드 끄기 제거 | `ShowOffers_Twice_DoesNotLeaveStaleCards` · `ClickCard_AfterHide_DoesNothing` |
| 숫자 키 경로 제거 | `KeyboardStillWorks_DigitOne_ChoosesFirst` |

### 완료 판정

- [ ] `grep -n "IReadOnlyList<string>" Assets/Code/Scripts/Presentation/Presenters/IRewardSelectionView.cs` = **0건**
- [ ] `RewardSelectionPresenter` 의 `Choose`·`Skip`·이벤트 시그니처가 그대로다
- [ ] `RewardOffer.DisplayName` 의 처분을 **실측으로 이 파일에 적었다**
- [ ] EditMode · PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS

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
