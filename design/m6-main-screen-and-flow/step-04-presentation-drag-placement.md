# Step 04: 손님 카드 드래그앤드롭 배치

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-03 (`CardView`)
- **후행 단계:** step-12 가 씬에 손패를 배치한다

---

## 목적

지금 배치는 **빈 자리를 클릭하면 명부의 0번 손님이 앉는다**. 명부가 보상으로 자라도 고를 방법이 없고(`CustomerPlacementController.SelectPending` 은 아무도 부르지 않는다), 무엇이 앉을지 화면에 HUD 한 줄로만 적혀 있다.

손패에서 카드를 **끌어다 테이블에 떨어뜨리는** 방식으로 바꾼다. 고르는 행위와 놓는 행위가 한 동작이 되어 선택 UI 가 따로 필요 없어진다.

**기존 클릭 배치는 제거한다** (아키텍트 결정). 두 경로를 남기면 "어느 손님이 배치되나" 가 두 곳에서 정해진다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Customers/CustomerHandView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Customers/CustomerCardDrag.cs` — 생성
- `Assets/Code/Scripts/Presentation/Customers/CustomerPlacementInput.cs` — **삭제**
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (배선 교체)
- `Assets/Tests/PlayMode/Customers/SlotPickerTests.cs` — 수정 (그 컴포넌트를 쓰는 부분만)
- `Assets/Tests/PlayMode/Stage01SceneTests.cs` — 수정 (씬의 그 오브젝트를 보는 단언)
- `Assets/Tests/PlayMode/Customers/CustomerHandViewTests.cs` — 생성

> **삭제 전에 참조를 실측으로 확인했다.** `CustomerPlacementInput` 을 언급하는 곳은 위 두 테스트와 `Assets/Level/Scenes/Stage01.unity` **세 곳뿐**이다. 씬에서 오브젝트를 빼는 것은 step-12 다 — 컴포넌트만 지우고 씬을 그대로 두면 **Missing Script** 로 조용히 남는다.

`SlotPicker` 는 **그대로 재사용한다.** 드롭 지점이 어느 자리인지는 여전히 거리 비교로 끝난다.

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>
    /// 명부를 카드로 늘어놓는 손패. 카드를 <b>미리 놓아 두고</b> 켜고 끈다 (§3.4).
    /// </summary>
    public sealed class CustomerHandView : MonoBehaviour
    {
        public void Initialize(CustomerPlacementController controller, Camera worldCamera,
                               TableSlotView[] slots);
        public void Bind(IReadOnlyList<CustomerData> roster);
        public void Refresh();                       // 잔액·한도가 바뀌면 카드의 가용 표시를 갱신

        public int ShownCardCount { get; }           // 검증용
        public int PlacedCount { get; }              // 검증용

        /// <summary>드롭 지점을 자리 배치로 옮긴다. 포인터를 거치지 않는 진입점이다.</summary>
        public bool DropAt(CustomerData customer, Vector2 worldPoint);
    }

    /// <summary>카드 하나의 끌기. 판정하지 않고 좌표만 넘긴다.</summary>
    public sealed class CustomerCardDrag : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public void Initialize(CustomerHandView hand, CustomerData customer, Camera worldCamera);
    }
}
```

### 세 갈래를 섞지 않는다

이 게임의 집기 규칙과 같은 구조로 나눈다.

| | 누가 답하나 |
|---|---|
| **어느 지점에 떨어졌나** | `SlotPicker.Pick` (순수 계산, 기존) |
| **거기 놓을 수 있나** | `CustomerPlacementService.CanPlace` (순수 로직, 기존) |
| **화면에 어떻게 보이나** | `CustomerCardDrag` · `CustomerHandView` |

**드래그 핸들러가 잔액이나 한도를 되묻지 않는다.** 서비스가 이미 답하고, 되물으면 두 판정이 어긋난다 (`CustomerPlacementController` 의 클래스 주석이 이미 세운 규칙이다).

### 실패한 드롭은 카드를 제자리로 되돌린다

`DropAt` 이 `false` 를 돌려주면 카드는 원래 위치로 돌아간다. **왜 실패했는지는 이 단계가 설명하지 않는다** — 잔액 부족·자리 점유·한도 초과가 각각 다른 문구를 요구하면 그것은 별도 기획이다. 지금은 "안 놓였다" 만 보인다.

> 다만 **끌 수 없는 카드는 끌기 전에 알 수 있어야** 한다. `Refresh()` 가 `CanPlace` 를 물어 카드를 흐리게 만든다 — 놓아 보고 실패하는 것보다 낫고, `CanPlace` 는 자리 번호를 인자로 받으므로 **"어느 자리에도 못 놓는가"** 로 판단한다 (한도·잔액이 그 경우다).

### `Initialize` 의 카메라 — M5 에서 클릭이 통째로 죽은 자리

```csharp
// 넘어온 카메라가 없으면 이미 찾아 둔 것을 지우지 않는다.
if (worldCamera != null) { _worldCamera = worldCamera; }
```

씬 진입점은 카메라를 들고 있지 않아 `null` 을 넘긴다. 그대로 대입하면 `Awake` 가 잡아 둔 참조가 날아가 **입력이 통째로 죽는데, 테스트가 안쪽 진입점(`DropAt`)만 불러 지나친다** ([`presentation-and-audio.md`](../../.claude/domain/presentation-and-audio.md) §7). 같은 사고를 두 번 내지 않는다.

**그리고 그 우회로를 타지 않는 테스트를 하나 남긴다** — `Initialize(controller, null, slots)` 를 부른 뒤 카메라 참조가 살아 있는지 보는 테스트다 ([`tests.md`](../../.claude/rules/tests.md) §1).

### 스크린 좌표 → 월드 좌표

`PointerEventData.position` 은 스크린 좌표다. 자리는 월드에 있다. 변환은 **한 곳**에서만 한다:

```csharp
var worldPoint = _worldCamera.ScreenToWorldPoint(eventData.position);
```

Canvas 가 `Screen Space - Overlay` 이면 카드의 `RectTransform` 은 스크린 좌표계로 움직이므로 카드 이동에는 변환이 필요 없다. **변환은 드롭 판정에만 쓴다.**

### 선행 산출물 의존성

- step-03 의 `CardView` · `Card.prefab`
- 기존 `CustomerPlacementController` · `CustomerPlacementService` · `SlotPicker` · `TableSlotView`

### 밸런스 수치

**없다.** 드롭 인정 반경은 프리팹의 `[SerializeField]` 이며, 제거되는 `CustomerPlacementInput._pickRadius` 의 값 `1.2` 를 그대로 옮긴다 (연출 수치 — M5 D7).

### 제약

- **`EventSystem` 이 씬에 있어야 동작한다.** 이 단계는 코드만 만들고, 씬 배치는 step-12 다. 그 사이에는 PlayMode 테스트가 `DropAt` 을 직접 불러 검증한다 — **다만 그것이 곧 사각지대이므로** 위의 "우회로를 타지 않는 테스트" 를 반드시 남긴다
- `Instantiate`/`Destroy` 금지 (§3.4) — 손패 카드는 미리 놓아 두고 켜고 끈다
- `Update` 경로 할당 금지 (§4.3). 드래그 중 매 프레임 도는 코드에서 문자열·LINQ·`new` 를 만들지 않는다
- `UnityEngine.Input` 금지 — 레거시 백엔드가 꺼져 있어 런타임 예외다. 포인터는 `EventSystem` 이 배달한다
- 물리를 쓰지 않는다 (RULE-04 회피). 자리 판정은 거리 비교다
- 구독·`onClick` 해제는 `OnDestroy` 에서 ([`scripts.md`](../../.claude/rules/scripts.md) §6)

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
드롭     DropAt_EmptySlotInRange_PlacesCustomer
         DropAt_FarFromEverySlot_DoesNotPlace
         DropAt_OccupiedSlot_DoesNotPlace
         DropAt_InsufficientBalance_DoesNotPlace       ← 서비스가 막는다
손패     Bind_Roster_ShowsOneCardPerMember
         Bind_GrownRoster_ShowsNewCard                 ← 보상으로 영입한 손님이 손패에 뜬다
         Bind_Twice_DoesNotDuplicateCards
         Refresh_WhenLimitReached_DisablesAllCards
우회로   Initialize_NullCamera_KeepsResolvedCamera     ← 안쪽 진입점을 지나치지 않는 테스트
```

> **`Bind_GrownRoster_ShowsNewCard` 가 이 단계의 실제 이유다.** M3~M5 내내 손님 보상은 HUD 한 줄로만 존재했다.

> **`Refresh_WhenLimitReached_DisablesAllCards` 를 공허하지 않게 쓴다.** 잔액을 넉넉히 두고 **한도만** 채워야 한도 검사가 불린다. 둘을 동시에 막으면 잔액만 보는 구현으로도 통과한다.

### 주입 검증

| 주입 | 예측 |
|---|---|
| `DropAt` 이 `CanPlace` 를 되묻고 서비스를 건너뛴다 | `DropAt_InsufficientBalance_DoesNotPlace` |
| `Initialize` 에서 카메라 `null` 가드 제거 | `Initialize_NullCamera_KeepsResolvedCamera` |
| `Bind` 에서 기존 카드 정리 제거 | `Bind_Twice_DoesNotDuplicateCards` |
| `SlotPicker` 에 넘기는 반경을 무한대로 | `DropAt_FarFromEverySlot_DoesNotPlace` |

### 완료 판정

- [ ] `grep -rn "CustomerPlacementInput" Assets/` 가 **0건** (테스트·씬·문서 포함)
- [ ] `grep -rn "Instantiate\|Destroy(" Assets/Code/Scripts/Presentation/Customers/` 가 `OnDestroy` 라이프사이클 외 **0건**
- [ ] `StageBootstrap` 이 `_placementInput` 대신 손패를 물린다
- [ ] PlayMode Green — `./tests/run-tests.sh all`
- [ ] **재생해서 실제로 끌어 놓아 본다** — 씬 배치가 없으면 step-12 로 넘기고 여기 기록한다
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(customer): place customers by dragging cards onto tables
```

---

## 금지 사항

- 클릭 배치 경로를 남기지 않는다 — 대체이지 추가가 아니다
- 드래그 핸들러 안에서 배치 가능 여부를 판정하지 않는다
- `CustomerPlacementService` · `CustomerPlacementController` 의 **public 계약**을 바꾸지 않는다
- 자리 판정에 콜라이더·물리를 도입하지 않는다
- 다른 어셈블리 파일을 수정하지 않는다
