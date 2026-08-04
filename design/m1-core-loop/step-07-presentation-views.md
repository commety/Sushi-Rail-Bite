# Step 07: 뷰 연결 — `SushiBeltView` · `SushiItemView` · `CustomerView` · `TableSlotView`

- **영역:** `presentation` — 어셈블리 `Presentation` (+ `Tests.PlayMode`)
- **선행 단계:** step-06 완료 필요 (`ClaimCoordinator` · `SushiBelt` 이벤트)
- **후행 단계:** step-08 배치 조작이 `TableSlotView` 를 쓰고, step-09 가 씬에 조립한다

---

## 목적

여기까지 만든 로직에 **화면을 붙인다.** 뷰는 얇다 — 판단이 하나도 없다. 위치를 읽어 `transform` 에 옮기고, 이벤트를 받아 풀에서 오브젝트를 빌리고 돌려줄 뿐이다.

**`Update` 에서 판정이 돌면 이 단계는 실패다.** `MonoBehaviour` 가 하는 일은 `Time.deltaTime` 을 조율자에게 넘기는 것과 좌표를 반영하는 것 두 가지뿐이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD)

PlayMode 테스트는 느리다. **뷰의 동작 대부분은 이미 EditMode 에서 검증됐다** — 여기서는 "이벤트가 오면 풀에서 빌리고 돌려주는가"만 본다.

1. 시그니처를 만든다
2. PlayMode 테스트를 쓰고 **Red 확인** (`./tests/run-tests.sh all`)
3. 구현

### 생성 파일

```
Assets/Code/Scripts/Presentation/Belt/SushiBeltView.cs         — 생성
Assets/Code/Scripts/Presentation/Belt/SushiItemView.cs         — 생성
Assets/Code/Scripts/Presentation/Customers/CustomerView.cs     — 생성
Assets/Code/Scripts/Presentation/Customers/TableSlotView.cs    — 생성
Assets/Tests/PlayMode/Belt/SushiBeltViewTests.cs               — 생성
```

프리팹이 필요하면 **`Assets/Code/Scripts/Presentation/` 하위 기능 폴더**에 둔다. `Assets/Art/` 는 심링크 폴더라 워크트리 git 이 새 파일을 못 본다 (RULE-02).

### 핵심 심볼

```csharp
namespace SushiDefense.Belt
{
    /// <summary>
    /// 벨트 로직의 화면 대응물. SushiBelt 이벤트를 구독해 SushiPoolBehaviour 에서
    /// 뷰를 빌리고 돌려주며, 매 프레임 위치만 반영한다. <b>판정이 없다.</b>
    /// </summary>
    public sealed class SushiBeltView : MonoBehaviour
    {
        [SerializeField] private SushiPoolBehaviour _viewPool;
        [SerializeField] private StageConfig _stageConfig;
        [SerializeField] private Transform _beltStart;
        [SerializeField] private Transform _beltEnd;

        /// <summary>씬 부트스트랩이 로직을 물려 준다. 뷰가 로직을 만들지 않는다.</summary>
        public void Bind(ClaimCoordinator coordinator, SushiBelt belt);

        private void Update();     // coordinator.Tick(Time.deltaTime) + 위치 반영. 그 외 금지
        private void OnDestroy();  // 구독 해제
    }

    /// <summary>초밥 하나의 화면 표현. 대응하는 SushiItem 을 들고 좌표만 따라간다.</summary>
    public sealed class SushiItemView : MonoBehaviour
    {
        public SushiItem Model { get; }
        public void Bind(SushiItem model);
        public void Release();
    }
}

namespace SushiDefense.Customers
{
    /// <summary>손님 하나의 화면 표현. CustomerLogic 에 위임만 한다 (CLAUDE.md §3.2).</summary>
    public sealed class CustomerView : MonoBehaviour
    {
        public CustomerLogic Logic { get; }
        public void Bind(CustomerLogic logic);
    }

    /// <summary>손님을 놓는 고정 자리. 점유 여부와 벨트 좌표를 들고 있다.</summary>
    public sealed class TableSlotView : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;

        public int SlotIndex { get; }
        public float BeltPosition { get; }     // TableSlotDefinition.BeltPosition 에서 주입
        public bool IsOccupied { get; }
        public CustomerView Occupant { get; }

        public void Bind(TableSlotDefinition definition);
        public void Occupy(CustomerView customer);
        public void Vacate();
    }
}
```

- **벨트 좌표 → 화면 좌표 변환은 `SushiBeltView` 한곳에서만** 한다. `_beltStart`/`_beltEnd` 사이를 `position / StageConfig.BeltLength` 비율로 보간한다. 여러 곳에서 변환하면 씬을 옮길 때 어긋난다
- `Update` 안에서 **할당 금지** — 위치 반영 루프에서 LINQ·`new`·문자열 결합을 만들지 않는다 (WebGL, §4.3)
- `GetComponent` 결과는 `Awake` 에서 캐싱한다 (§4.3)
- `Find`/`FindObjectOfType` 금지 — 참조는 인스펙터 주입 또는 `Bind` 인자로
- 구독(`+=`)을 만들면 `OnDestroy`/`OnDisable` 에서 해제한다 (`.claude/rules/scripts.md` §6)

### 선행 산출물 의존성

- step-06 — `ClaimCoordinator`
- step-02 — `SushiBelt` 와 그 이벤트
- step-03 — `CustomerLogic`
- step-01 — `TableSlotDefinition.BeltPosition`
- M0 — `SushiPoolBehaviour`

### 밸런스 수치

`StageConfig` 에서 읽는다. 뷰에 숫자 상수를 두지 않는다. 화면 좌표(`_beltStart`/`_beltEnd`)는 밸런스가 아니라 **씬 배치**라 인스펙터 참조로 둔다.

### 테스트 항목 (`Tests.PlayMode`)

```
SushiBeltViewTests
  Bind_SushiSpawned_RentsViewFromPool
  Bind_SushiRemoved_ReturnsViewToPool
  Bind_SushiRemoved_DoesNotDestroyGameObject       ← Destroy 호출 0 (플랜 완료 판정)
  Update_SushiAdvances_MovesViewTowardBeltEnd
  Bind_ManySpawns_ReusesViewsAfterRemoval
  OnDestroy_Unsubscribes_NoCallbackAfterTeardown
```

테스트 프리팹은 디스크 애셋을 로드하지 말고 `new GameObject` 로 만들어 `SushiPoolBehaviour.Initialize` 에 넘긴다 (M0 에서 쓴 방식).

### 제약

- **뷰에 판정을 넣지 않는다.** 집기·배정·자격 판단이 `Presentation` 에 나타나면 이 단계는 실패다
- **물리 컴포넌트 금지** (`Rigidbody2D`·`Collider2D`) — 진입 판정은 계산이다 (D5)
- `Instantiate`/`Destroy` 를 새로 부르지 않는다. 초밥 오브젝트는 `SushiPoolBehaviour` 경유 (§3.4)
- 씬 파일(`Assets/Level/Scenes/*.unity`)을 수정하지 않는다 — 조립은 step-09 다
- `Assets/Art/`·`Assets/Audio/`·`Assets/Settings/` 에 파일을 만들지 않는다 (RULE-02)

### 완료 판정

- [ ] `grep -rln "class SushiBeltView\|class SushiItemView\|class CustomerView\|class TableSlotView" Assets/Code/Scripts/Presentation/` → 4파일
- [ ] `grep -rn "Instantiate\|Destroy(" Assets/Code/Scripts/ --include="*.cs" | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **`SushiPoolBehaviour.cs` 외 0건**
- [ ] `grep -rn "FindObjectOfType\|GameObject.Find\|Rigidbody2D\|Collider2D" Assets/Code/Scripts/Presentation/` | 주석 제외 → 0건
- [ ] `grep -rn "CanTake\|Resolve(" Assets/Code/Scripts/Presentation/` | 주석 제외 → **0건** (판정이 뷰로 새지 않았는지)
- [ ] `./tests/run-tests.sh all` — EditMode + PlayMode 전량 Green
- [ ] `git status --short Assets/Level/` → 변경 없음

### 예상 커밋 메시지

```
feat(belt): add belt and customer views delegating to runtime logic
```

---

## 금지 사항

- `Update` 에서 손님×초밥 순회를 만들지 않는다. 그건 조율자가 이벤트 기반으로 이미 한다.
- 뷰가 `CustomerLogic`·`SushiBelt` 를 **생성하지 않는다.** 씬 부트스트랩이 만들어 `Bind` 로 넘긴다.
- 씬·프리팹 조립을 여기서 하지 않는다 (step-09).
