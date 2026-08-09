# Step 06: 정보 창 뷰와 손님 클릭 라우팅

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-05 (`ICustomerInspectorView` · `CustomerInspectorPresenter`)
- **후행 단계:** step-09 가 씬에 패널을 놓는다

---

## 목적

프레젠터에 **화면과 손가락**을 붙인다. 손님은 월드 스페이스 `SpriteRenderer` 라 uGUI 레이캐스트가 닿지 않으므로, 클릭을 자리로 바꾸는 조각이 하나 필요하다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Views/CustomerInspectorView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Customers/CustomerTapRouter.cs` — 생성
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (조립 · 구독 해제)
- `Assets/Tests/PlayMode/UI/CustomerInspectorViewTests.cs` — 생성
- `Assets/Tests/PlayMode/Customers/CustomerTapRouterTests.cs` — 생성
- `Assets/Tests/EditMode/UI/KoreanFontCoverageTests.cs` — 수정 (아래 문구)

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    /// <summary>정보 창의 화면. 판정하지 않는다 — 프레젠터가 만든 글자를 옮기기만 한다.</summary>
    public sealed class CustomerInspectorView : MonoBehaviour, ICustomerInspectorView
    {
        public bool IsShowing { get; private set; }
        public string NameText { get; private set; }
        public string KindText { get; private set; }
        public string StatsText { get; private set; }
        public string StateText { get; private set; }
        public string SaturationText { get; private set; }
        public string RemainingText { get; private set; }
    }
}
```

```csharp
namespace SushiDefense.Customers
{
    /// <summary>
    /// 화면의 클릭을 «어느 자리의 손님» 으로 바꿔 넘긴다. <b>판정하지 않는다</b> —
    /// 어느 자리인지는 <see cref="SlotPicker"/> 가, 거기 누가 앉아 있는지는
    /// <c>CustomerPlacementService.OccupantOf</c> 가 답한다.
    /// </summary>
    public sealed class CustomerTapRouter : MonoBehaviour
    {
        public void Bind(CustomerPlacementService placement, TableSlotView[] slots,
                         Camera worldCamera, Action<CustomerLogic> onTapped);

        /// <summary>
        /// 포인터를 거치지 않는 진입점. <b>테스트가 직접 부른다</b> —
        /// 안쪽만 부르는 테스트가 사각지대를 만들지 않도록 바깥 경로도 함께 검증한다
        /// (<c>.claude/rules/tests.md</c> §1).
        /// </summary>
        public bool TapAt(Vector2 worldPoint);

        /// <summary>지금까지 열린 횟수. 검증용이다.</summary>
        public int TapCount { get; }
    }
}
```

### 「드롭」과 「클릭」을 가르는 두 조건 (README D7)

`Update` 에서 마우스/터치의 **눌렀다 뗀 프레임**을 잡되, 아래 둘 중 하나라도 걸리면 무시한다:

1. **직전 프레임까지 드래그가 진행 중이었다** — 카드를 자리에 떨어뜨린 손짓이다. `CustomerHandView` 나 카드 쪽에 «지금 끌고 있는 카드가 있나» 를 묻는 읽기 전용 창구를 하나 열고, 뗀 프레임에 그것이 참이었으면 건너뛴다
2. **포인터가 uGUI 위에 있다** — `EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()`. 손패 카드·HUD·패널 위의 클릭이 자리로 새지 않게 한다

> **②만으로는 부족하다.** 드래그의 **끝점은 자리 위**(uGUI 밖)라 ② 를 그대로 통과한다. ①이 없으면 카드를 앉히는 순간 정보 창이 같이 뜬다.

### 입력 백엔드

`ENABLE_LEGACY_INPUT_MANAGER` 가 **정의돼 있지 않다.** `UnityEngine.Input` 은 런타임 예외를 던진다 — `RewardSelectionView`·`StageTransitionView` 와 같이 `UnityEngine.InputSystem` 을 쓴다.

```csharp
using UnityEngine.InputSystem;
...
var pointer = Pointer.current;              // 마우스·터치 공통
if (pointer == null || !pointer.press.wasReleasedThisFrame) return;
var world = _worldCamera.ScreenToWorldPoint(pointer.position.ReadValue());
```

`_worldCamera` 가 비면 `Camera.main` 으로 대신한다 — 단 **`Bind` 에 `null` 이 넘어와도 이미 찾아 둔 것을 지우지 않는다.** M5 에서 `Initialize(..., null, ...)` 가 `Awake` 가 잡아 둔 카메라를 지워 클릭이 통째로 죽은 적이 있다 (`CustomerHandView.Initialize` 의 주석 참고).

### 조립 (`StageBootstrap`)

- `EnsureRunScope` 에서 `Inspector = new CustomerInspectorPresenter(_inspectorView, Windows);`
- `_tapRouter.Bind(Placement, _slots, null, Inspector.Open);`
- `Update` 에서 `Inspector.Tick();` — **`Pause.IsPaused` 로 조기 반환하는 기존 게이트보다 앞에 둔다.** 정보 창은 멈춤 중에도 열려 있을 수 있고, 그때 값이 안 변하는 것은 판이 안 도니 당연하다
- `OnWindowCloseRequested` 의 `switch` 에 `case StageWindow.CustomerInfo: Inspector.Close(); break;` 를 더한다
- `ReleaseRunScope` 에서 정리. **`Reward` 는 여전히 그 `switch` 에 넣지 않는다** ([`in-stage-windows.md`](../../.claude/domain/in-stage-windows.md) §2)
- `ResolveMissingReferences` 에 `_inspectorView`·`_tapRouter` 를 더한다 — 인스펙터가 비면 자기 하위에서 찾는다

### 새 문구 — 폰트 커버리지

`KoreanFontCoverageTests` 에 한 줄을 더한다:

```csharp
[Test]
public void Font_CoversInspectorLabels()
{
    AssertCovers("손님 정보 유형 범위 대역 먹는 시간 소화 시간 영입 비용 인구수 상태 포화도 남은 기본 소식 먹보 대기 중 닫기");
}
```

> **실측으로 누락 0자를 확인했다** — 폰트가 M6 에서 전체 커버리지로 굽혔기 때문이다 (3,248자). **재굽기가 필요 없다.** 이 테스트는 그 사실을 고정하는 그물이지 새 작업을 부르는 신호가 아니다.

### 밸런스 수치

없다. 클릭 인정 반경은 `CustomerHandView._dropRadius` 와 같은 성격의 **연출 수치**이므로 `CustomerTapRouter` 의 `[SerializeField] private float _tapRadius = 1.2f;` 에 둔다 (M5 D7 구분선).

### 제약

- **콜라이더를 도입하지 않는다** (README D6, RULE-04). `SlotPicker.Pick` 재사용
- 뷰는 **판정하지 않는다.** 글자는 프레젠터가 만든 것을 그대로 쓴다
- 이벤트 구독은 `OnDestroy`/`OnDisable` 에서 반드시 해제한다 ([`scripts.md`](../../.claude/rules/scripts.md) §6)
- **`Awake` 에만 참조 수집을 두지 않는다.** 패널은 꺼진 채 시작하므로 꺼진 오브젝트의 `Awake` 는 켜질 때까지 돌지 않는다 — `CardView`·`CustomerView` 와 같은 **멱등 `Resolve()`** 를 쓴다 ([`unity-scripting-gotchas.md`](../../.claude/knowledge/unity-scripting-gotchas.md) §5)
- `Find`/`FindObjectOfType` 금지

### 테스트 계획

```
CustomerInspectorViewTests
  ShowCustomer_WritesEveryLabel               ← 라벨 6개 각각
  RefreshLive_WritesOnlyLiveLabels            ← 정적 줄이 안 흔들린다
  Hide_TurnsThePanelOff
  ShowCustomer_WhileInactive_StillWrites      ← 꺼진 채로 Bind → 멱등 Resolve 검증.
                                                 이게 없으면 «두 번째부터 정상» 이 새어 나간다

CustomerTapRouterTests
  TapAt_OccupiedSlot_OpensTheInspector
  TapAt_EmptySlot_DoesNothing
  TapAt_FarFromEverySlot_DoesNothing
  Update_PointerOverUi_DoesNotTap             ← 바깥 껍데기를 지나는 경로
  Update_ReleasedRightAfterDrag_DoesNotTap    ← D7 ①. 이게 없으면 «카드를 앉히면
                                                 정보 창이 같이 뜬다» 를 아무도 못 잡는다
```

- **`Update_...` 두 개는 `TapAt` 을 직접 부르지 않는다.** 안쪽 진입점만 부르는 테스트는 바깥 껍데기(입력·좌표 변환)를 지나쳐, M5 에서 Enter·Esc 가 전멸한 것을 아무도 못 잡은 형태다 (`tests.md` §1)
- `Pointer.current` 는 `InputTestFixture` 없이 다루기 번거롭다. **입력 읽기를 얇은 `protected virtual` 또는 주입 가능한 델리게이트 뒤로 미루지 말 것** — 그러면 또 하나의 «테스트가 편하려고 만든 우회로» 가 된다. 대신 `InputSystem.AddDevice<Mouse>()` 로 가상 장치를 만들어 실제 경로를 태운다

### 완료 판정

- [ ] `grep -rn "UnityEngine.Input;" Assets/Code/Scripts/Presentation/Customers/CustomerTapRouter.cs` — **0건** (레거시 아님)
- [ ] `grep -n "IsPointerOverGameObject" Assets/Code/Scripts/Presentation/Customers/CustomerTapRouter.cs` — 1건
- [ ] `grep -n "case StageWindow.CustomerInfo" Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 1건
- [ ] `grep -n "case StageWindow.Reward" Assets/Code/Scripts/Presentation/StageBootstrap.cs` — **0건** (일부러 없다)
- [ ] `Physics2D`·`Collider2D` 신규 사용 0건
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] 깨뜨려 보기: 드래그 가드(①)를 지우면 `Update_ReleasedRightAfterDrag_DoesNotTap` 이 죽는지 확인하고 되돌린다

### 예상 커밋 메시지

```
feat(ui): open a customer inspector by tapping a seated customer
```

---

## 금지 사항

- **씬(`Stage01.unity`)을 이 단계에서 편집하지 않는다** (step-09). 씬 편집은 한 번에 한 워크트리다 ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §3).
- 정보 창에서 판을 멈추지 않는다.
- `StageWindowArbiter` 를 고치지 않는다. `CustomerInfo` 규칙은 이미 들어 있다.
