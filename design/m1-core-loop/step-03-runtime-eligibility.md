# Step 03: 자격 판정 — `CustomerLogic`

- **영역:** `runtime` — 어셈블리 `Runtime` (+ `Tests.EditMode`)
- **선행 단계:** step-01 완료 필요 (`TableSlotDefinition.BeltPosition`)
- **후행 단계:** step-04 가 자격 상실 시 후보를 비우고, step-05 가 자격을 통과한 손님으로만 쌍을 만든다
- **step-02 와 병렬 가능**

---

## 목적

**"이 손님이 뭐라도 집을 수 있나"만 판정한다.** 세 조건뿐이다:

```
범위 안에 있는가  ∧  포화도에 여유가 있는가  ∧  집을 수 있는 상태(Idle)인가
```

이 단계가 이 프로젝트에서 **가장 자주 잘못 구현되는 지점**이다 (`CLAUDE.md` §1.1-3a). M1 에는 가격이 없어 저절로 지켜지지만, 그래서 더 중요하다 — M2 에서 가격이 생길 때 끼어들 자리를 지금 없애 둔다. 자격 판정 파일이 가격 타입을 아예 참조하지 않으면 M2 의 실수가 컴파일 단계에서 드러난다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD)

1. 타입·시그니처를 만든다 (판정 본문은 `false` 반환)
2. 테스트를 쓰고 **Red 확인**
3. 최소 구현으로 Green

### 생성 파일

```
Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs          — 생성
Assets/Tests/EditMode/Customers/CustomerLogicTests.cs           — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님의 자격 판정. <b>배정은 하지 않는다</b> — 누가 무엇을 가져가는지는
    /// SushiClaimResolver 의 몫이고, 둘을 한곳에 두면 M2 에서 가격이 자격에 끼어든다
    /// (CLAUDE.md §1.1-3a, 작업서 D6).
    /// </summary>
    public sealed class CustomerLogic
    {
        public CustomerRuntimeState State { get; }
        public float BeltPosition { get; }        // 앉은 자리의 벨트 좌표 (TableSlotDefinition.BeltPosition)

        public CustomerLogic(CustomerRuntimeState state, float beltPosition);

        /// <summary>지금 새 초밥을 받을 수 있는 상태인가. 초밥과 무관한 손님 쪽 조건만 본다.</summary>
        public bool CanAcceptSushi { get; }

        /// <summary>초밥이 이 손님의 집기 범위 안에 있는가. 순수 위치 비교다.</summary>
        public bool IsInReach(float sushiBeltPosition);

        /// <summary>
        /// 이 초밥을 집을 자격이 있는가 = CanAcceptSushi ∧ IsInReach ∧ 초밥이 아직 OnBelt.
        /// <b>가격을 보지 않는다.</b>
        /// </summary>
        public bool CanTake(SushiItem sushi);
    }
}
```

- 범위는 `[BeltPosition − Reach, BeltPosition + Reach]` **폐구간**이다. 경계값을 포함할지 여부를 테스트로 고정한다 — 안 정하면 다음 사람이 반대로 구현한다
- `CanAcceptSushi` = `State.State == CustomerState.Idle && State.HasSaturationHeadroom`. M0 이 만든 프로퍼티를 그대로 읽는다
- **`CustomerData.TargetingPrice` 와 `SushiData.Price` 를 이 파일에서 참조하지 않는다.** 이것이 D6 의 실질이다

### 선행 산출물 의존성

- step-01 — `TableSlotDefinition.BeltPosition` (생성자에 넘길 값의 출처)
- M0 — `CustomerRuntimeState` · `CustomerState` · `SushiItem` · `SushiState` · `CustomerData.Reach`

### 밸런스 수치

`Reach` 는 `CustomerData` 에서 읽는다. **숫자 상수 금지.**

### 테스트 항목 (`Tests.EditMode`)

```
CustomerLogicTests
  IsInReach_SushiAtCenter_ReturnsTrue
  IsInReach_SushiAtReachEdge_ReturnsTrue             ← 폐구간을 고정한다
  IsInReach_SushiBeyondReach_ReturnsFalse
  IsInReach_SushiBehindCustomer_ReturnsTrue          ← 범위는 앞뒤 대칭이다

  CanAcceptSushi_Idle_ReturnsTrue
  CanAcceptSushi_Eating_ReturnsFalse
  CanAcceptSushi_Digesting_ReturnsFalse
  CanAcceptSushi_Full_ReturnsFalse

  CanTake_SushiInReach_ReturnsTrue
  CanTake_SushiOutOfReach_ReturnsFalse
  CanTake_Full_ReturnsFalse
  CanTake_AlreadyClaimedSushi_ReturnsFalse
  CanTake_ExpensiveSushiFarFromTargeting_ReturnsTrue  ← 가격이 자격을 막지 않는다 (핵심)
  CanTake_CheapSushi_ReturnsTrue                      ← 위와 짝. 어떤 가격도 자격에 영향 없다
```

마지막 두 개는 M1 에서는 당연히 통과하지만 **M2 를 위한 회귀 방지 장치**다. 누군가 자격에 가격을 끼우면 이 둘이 먼저 깨진다. 지우지 않는다.

### 제약

- **`MonoBehaviour` 금지**
- **자격에 가격을 넣지 않는다.** 아래는 기획 위반이다:
  ```csharp
  if (Mathf.Abs(sushi.Data.Price - _data.TargetingPrice) > threshold) return false;   // ❌
  ```
- 배정(`Resolve`)을 여기 구현하지 않는다 (step-05)
- 후보 집합·래치를 여기 두지 않는다 (step-04)
- 상태 전이(`Idle → Eating`)를 여기서 하지 않는다 — M1 은 자격을 **읽기만** 한다. 전이는 step-06 조율자가, 타이머는 M2 가 맡는다
- `Runtime` 은 `Presentation` 을 참조하지 않는다

### 완료 판정

- [ ] `grep -rn "class CustomerLogic" Assets/Code/Scripts/Runtime/Customers/` → 1건
- [ ] `grep -n "TargetingPrice\|\.Price" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs | grep -vE '^[0-9]+:[[:space:]]*(///|//|\*)'` → **0건** (D6 의 핵심 검사)
- [ ] `grep -n "MonoBehaviour" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs` | 주석 제외 → 0건
- [ ] `./tests/run-tests.sh` — 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(customer): add eligibility check separate from assignment
```

---

## 금지 사항

- 배정 로직을 넣지 않는다. 이 클래스는 "누가 무엇을" 을 모른다.
- `CanTake` 가 `SushiData.Price` 나 `CustomerData.TargetingPrice` 를 읽게 만들지 않는다.
- 테스트를 통과시키려고 `CustomerRuntimeState` 의 접근 제한자를 넓히지 않는다.
