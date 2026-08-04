# Step 08: 손님 배치 조작 — `CustomerPlacementController`

- **영역:** `presentation` — 어셈블리 `Presentation` (+ `Tests.EditMode` / `Tests.PlayMode`)
- **선행 단계:** step-07 완료 필요 (`TableSlotView` · `CustomerView`)
- **후행 단계:** step-09 가 씬에 조립한다

---

## 목적

플레이어가 **빈 자리를 골라 손님을 앉힌다.** M1 완료 판정의 *"손님을 `TableSlot` 에 배치할 수 있다"* 가 이 단계다.

**Q4 결정으로 배치가 스테이지 진행 중에도 가능하다** — 이건 기본안(시작 전에만)과 다르고, 배치가 곧 재배정 트리거가 된다는 뜻이다. 새로 앉은 손님은 **다음 틱에 바로** 자기 범위의 초밥을 인식해야 하며, 기존 손님의 확정된 배정을 흔들어서는 안 된다. 두 성질 다 step-06 의 `ClaimCoordinator.PlaceCustomer` 가 이미 갖고 있고, 이 단계는 그것을 조작에 연결한다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD)

배치 **규칙**(빈 자리인가, 한도를 넘지 않는가)은 순수 클래스로 빼서 EditMode 로 검증한다. 입력 처리(클릭·터치)만 `MonoBehaviour` 에 남긴다 — 그래야 규칙이 테스트된다.

1. `CustomerPlacementService`(순수) 시그니처 → 테스트 → **Red** → 구현
2. `CustomerPlacementController`(`MonoBehaviour`)로 입력을 연결

### 생성 파일

```
Assets/Code/Scripts/Runtime/Customers/CustomerPlacementService.cs         — 생성 (순수 규칙)
Assets/Code/Scripts/Presentation/Customers/CustomerPlacementController.cs — 생성 (입력 껍데기)
Assets/Tests/EditMode/Customers/CustomerPlacementServiceTests.cs          — 생성
```

> ⚠️ 배치 **규칙**은 `Runtime` 에 둔다. 이 단계가 두 어셈블리를 건드리는 유일한 예외이며, 이유는 규칙을 `MonoBehaviour` 밖으로 빼지 않으면 EditMode 로 검증할 수 없기 때문이다 (`CLAUDE.md` §3.2). 파일이 서로 겹치지 않으므로 병렬 위험은 없다.

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>배치가 가능한지 판정하고 확정한다. 입력·화면을 모른다.</summary>
    public sealed class CustomerPlacementService
    {
        public int PlacedCount { get; }
        public int MaxPlacedCustomers { get; }      // StageConfig 에서 주입

        public CustomerPlacementService(ClaimCoordinator coordinator, StageConfig config,
                                        SequenceNumberIssuer customerSequenceNumbers);

        /// <summary>이 자리에 이 손님을 놓을 수 있는가. 점유 여부와 배치 한도만 본다.</summary>
        public bool CanPlace(int slotIndex);

        /// <summary>
        /// 배치를 확정한다. 손님 순차번호를 발급하고 조율자에 등록해
        /// <b>다음 틱부터 배정에 참여</b>시킨다 (Q4).
        /// </summary>
        public CustomerLogic Place(CustomerData data, int slotIndex, float slotBeltPosition);

        /// <summary>배치를 취소한다. 자리를 비우고 조율자에서 뺀다.</summary>
        public bool Remove(int slotIndex);
    }
}

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배치 입력만 처리하는 껍데기. 판정은 전부 CustomerPlacementService 에 있다.
    /// </summary>
    public sealed class CustomerPlacementController : MonoBehaviour
    {
        [SerializeField] private TableSlotView[] _slots;
        [SerializeField] private CustomerData _pendingCustomer;   // M1 은 한 종류로 검증

        public void Bind(CustomerPlacementService service);
        public void TryPlaceAt(TableSlotView slot);               // 클릭 핸들러가 부른다
    }
}
```

- **M1 의 배치는 무료다.** `CustomerData.RecruitCost` 를 읽지 않는다 — 영입 재화는 M2 다. 한도는 `StageConfig.MaxPlacedCustomers` 만 본다
- 손님 순차번호는 `SequenceNumberIssuer` 로 발급한다. 벨트(초밥)와 **다른 인스턴스**를 쓴다 — 두 카운터는 독립이다 (M0 에서 확인된 성질)
- 입력 방식은 프로젝트의 Input System(`Assets/Settings/InputSystem_Actions.inputactions`, 기존)을 쓴다. **새 패키지를 추가하지 않는다** (§7)
- `Find` 금지. 자리 목록은 인스펙터 주입

### 선행 산출물 의존성

- step-06 — `ClaimCoordinator.PlaceCustomer` · `RemoveCustomer`
- step-07 — `TableSlotView` · `CustomerView`
- step-01 — `TableSlotDefinition.BeltPosition`
- M0 — `SequenceNumberIssuer` · `CustomerRuntimeState` · `StageConfig.MaxPlacedCustomers`

### 밸런스 수치

`MaxPlacedCustomers` 는 `StageConfig` 에서 읽는다. **코드 상수 금지.**

### 테스트 항목 (`Tests.EditMode`)

```
CustomerPlacementServiceTests
  CanPlace_EmptySlot_ReturnsTrue
  CanPlace_OccupiedSlot_ReturnsFalse
  CanPlace_AtMaxPlacedCustomers_ReturnsFalse
  CanPlace_AfterRemove_ReturnsTrueAgain

  Place_EmptySlot_ReturnsCustomerWithSequenceNumber
  Place_Twice_AssignsIncreasingCustomerSequenceNumbers
  Place_OccupiedSlot_Throws                          ← 조용히 실패시키지 않는다
  Place_MidStage_ParticipatesInNextResolve           ← Q4 결정의 본체
  Place_MidStage_DoesNotReassignExistingClaims       ← 기존 배정을 흔들지 않는다
  Place_DoesNotReadRecruitCost                       ← M1 범위 (M2 회귀 방지)

  Remove_PlacedSlot_FreesSlotAndStopsClaiming
  Remove_EmptySlot_ReturnsFalse
```

### 제약

- **판정을 `MonoBehaviour` 에 두지 않는다.** `CustomerPlacementController` 에는 `if (slot.IsOccupied)` 같은 규칙이 없어야 한다 — 전부 서비스에 묻는다
- **`RecruitCost`·영입 재화를 읽지 않는다** (M2)
- 새 Input 패키지를 추가하지 않는다 (§7)
- `Find`/`FindObjectOfType` 금지
- 씬 파일을 수정하지 않는다 (step-09)
- 구독을 만들면 `OnDestroy` 에서 해제한다

### 완료 판정

- [ ] `grep -rn "class CustomerPlacementService" Assets/Code/Scripts/Runtime/Customers/` → 1건
- [ ] `grep -rn "class CustomerPlacementController" Assets/Code/Scripts/Presentation/` → 1건
- [ ] `grep -n "IsOccupied\|CanPlace\|MaxPlaced" Assets/Code/Scripts/Presentation/Customers/CustomerPlacementController.cs | grep -vE '^[0-9]+:[[:space:]]*(///|//|\*)'` → **판정문 0건** (서비스 호출만 있어야 한다)
- [ ] `grep -rn "RecruitCost" Assets/Code/Scripts/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → 0건 (M1 범위)
- [ ] `./tests/run-tests.sh` — 전량 Green
- [ ] `git status --short Assets/Level/ Packages/` → 변경 없음

### 예상 커밋 메시지

```
feat(customer): add table slot placement with mid-stage support
```

---

## 금지 사항

- 배치 비용·영입 재화를 구현하지 않는다 (M2).
- 배치 규칙을 `MonoBehaviour` 안에 복제하지 않는다.
- 손님 재배치(자리 옮기기)를 만들지 않는다 — MVP 범위 밖이다 (`sushi-claim-flow.md` §7).
