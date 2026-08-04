# Step 05: 배정 — `SushiClaimResolver`

- **영역:** `runtime` — 어셈블리 `Runtime` (+ `Tests.EditMode`)
- **선행 단계:** step-04 완료 필요 (`CandidateSet`), step-03 (`CustomerLogic`)
- **후행 단계:** step-06 `ClaimCoordinator` 가 이 리졸버를 호출한다. **M2 가 정렬 키를 추가하는 유일한 파일**

---

## 목적

**누가 어떤 초밥을 가져가는지** 정한다. M1 의 정렬 키는 `초밥 SeqNo → 손님 SeqNo` 둘뿐이지만, 구조는 [`sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §2 의 **쌍 랭킹 → 그리디 확정** 을 지금 완성한다.

M2 에서 `−|가격 − 타겟팅|` 과 `가격 내림차순` 이 정렬 키 1·2번으로 앞에 끼워진다. **그때 바뀌는 것이 비교자 하나뿐이어야 한다** — 케이스별 분기(1:N / N:1 / N:M)를 짜 두면 M2 에서 전부 다시 쓰게 된다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD)

1. 타입·시그니처를 만든다 (`Resolve` 는 빈 결과 반환)
2. 테스트를 쓰고 **Red 확인**
3. 구현 → 리팩터

### 생성 파일

```
Assets/Code/Scripts/Runtime/Customers/ClaimCandidatePair.cs   — 생성 (손님×초밥 쌍)
Assets/Code/Scripts/Runtime/Customers/ClaimPairComparer.cs    — 생성 (정렬 키. M2 확장 지점)
Assets/Code/Scripts/Runtime/Customers/SushiClaimResolver.cs   — 생성
Assets/Tests/EditMode/Customers/SushiClaimResolverTests.cs    — 생성
Assets/Tests/EditMode/Customers/ClaimPairComparerTests.cs     — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>배정 후보 한 쌍. 정렬의 단위다.</summary>
    public readonly struct ClaimCandidatePair
    {
        public CustomerLogic Customer { get; }
        public SushiItem Sushi { get; }

        public ClaimCandidatePair(CustomerLogic customer, SushiItem sushi);
    }

    /// <summary>
    /// 쌍의 우선순위. <b>M2 가 정렬 키를 추가하는 유일한 지점이다.</b>
    ///
    /// M1 의 키: 초밥 SeqNo(낮은 순) → 손님 SeqNo(낮은 순).
    /// M2 에서 앞에 붙을 키: −|가격 − 타겟팅|(가까운 순) → 가격(높은 순).
    /// 순차번호가 유일하므로 이 비교는 <b>완전순서</b>이고 0 을 돌려주는 경우가 없다.
    /// </summary>
    public sealed class ClaimPairComparer : IComparer<ClaimCandidatePair>
    {
        public int Compare(ClaimCandidatePair a, ClaimCandidatePair b);
    }

    /// <summary>
    /// 인식된 쌍을 랭킹해 그리디로 배정한다. 케이스별 분기를 두지 않는다 —
    /// 하나의 정렬 규칙이 1:N · N:1 · N:M 을 모두 덮는다 (sushi-claim-flow §2).
    /// <b>난수를 쓰지 않는다.</b>
    /// </summary>
    public sealed class SushiClaimResolver
    {
        /// <summary>
        /// 자격을 통과한 손님들의 후보에서 배정을 확정한다.
        /// 확정된 쌍의 손님과 초밥은 이후 쌍에서 제외된다.
        /// </summary>
        public void Resolve(IReadOnlyList<CustomerLogic> customers,
                            IReadOnlyDictionary<CustomerLogic, CandidateSet> candidates,
                            List<ClaimCandidatePair> results);
    }
}
```

- **결과를 `List` 인자로 받아 채운다.** 반환하면 호출마다 할당이 생기고, 이 경로는 배정이 필요할 때마다 돈다 (§4.3). 호출자가 버퍼를 재사용한다
- 확정은 `SushiItem.TryClaim(customerSequenceNumber)`(M0)로 한다. `false` 가 오면 이미 다른 쪽이 가져간 것이므로 그 쌍은 버린다 — **이 방어가 중복 배정을 막는 마지막 관문이다**
- 자격 판정을 여기서 다시 하지 않는다. **자격을 통과한 손님만 들어온다**는 것이 이 메서드의 전제다 (호출자 = step-06)
- 정렬은 `List.Sort(comparer)` 로 충분하다. 안정 정렬이 아니어도 비교가 완전순서라 결과가 유일하다

### 선행 산출물 의존성

- step-03 — `CustomerLogic`
- step-04 — `CandidateSet`
- M0 — `SushiItem.TryClaim` · `SushiItem.SequenceNumber` · `CustomerRuntimeState.SequenceNumber`

### 밸런스 수치

**없다.** M1 의 배정은 순차번호만 본다. `SushiData`·`CustomerData` 의 어떤 수치도 읽지 않는다.

### 테스트 항목 (`Tests.EditMode`)

```
ClaimPairComparerTests
  Compare_LowerSushiSequence_ComesFirst
  Compare_SameSushi_LowerCustomerSequenceComesFirst
  Compare_IdenticalPair_ReturnsZeroOnlyForSamePair    ← 완전순서 확인
  Compare_SushiKeyOutranksCustomerKey                 ← 초밥 키가 손님 키보다 위 (§2)

SushiClaimResolverTests
  Resolve_OneCustomerOneSushi_AssignsIt
  Resolve_MultipleSushiInReach_TakesLowestSequence    ← 플랜 완료 판정
  Resolve_ManyCustomersOneSushi_LowerCustomerSequenceWins
  Resolve_ManyCustomersManySushi_AssignsWithoutDuplicates
  Resolve_AssignedSushi_NotAssignedAgain
  Resolve_AssignedCustomer_DoesNotTakeSecondSushi     ← 한 번에 하나 (claim-flow §7 기본안)
  Resolve_CandidatesExist_AlwaysProducesAtLeastOne    ← "구경하지 않는다" 의 EditMode 대응물
  Resolve_EmptyCandidates_ProducesNothing
  Resolve_SameBoardTwice_ProducesIdenticalResult      ← 결정성 회귀 방지 (플랜)
  Resolve_ReusedResultBuffer_DoesNotAccumulate        ← 버퍼를 지우고 채우는지
```

`Resolve_CandidatesExist_AlwaysProducesAtLeastOne` 이 플랜의 *"범위 안에 초밥이 있는데 집지 않고 지나보내는 프레임이 0"* 을 EditMode 로 잡아 두는 장치다. 런타임 확인은 `/qa` 에 맡긴다.

### 제약

- **`Random` 금지.** 프로덕션 배정 경로에 난수가 등장하면 규칙 위반이다 (`CLAUDE.md` §1.1-3b)
- **`MonoBehaviour` 금지**
- **케이스별 분기 금지.** `if (customers.Count == 1)` 같은 특수 경로를 만들지 않는다 — 하나의 정렬이 전부를 덮는다
- **M1 에서 `TargetingPrice`·`Price` 를 읽지 않는다.** M2 의 확장 지점을 `ClaimPairComparer` **한 파일**로 좁혀 둔다
- `Resolve` 안에서 할당 금지 — 정렬 버퍼는 필드로 재사용한다
- 자격 판정을 다시 하지 않는다 (step-03 의 책임)

### 완료 판정

- [ ] `grep -rn "class SushiClaimResolver\|class ClaimPairComparer\|struct ClaimCandidatePair" Assets/Code/Scripts/Runtime/Customers/` → 3건
- [ ] `grep -rn "Random" Assets/Code/Scripts/Runtime/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건**
- [ ] `grep -n "TargetingPrice\|\.Price" Assets/Code/Scripts/Runtime/Customers/*.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건** (M1 범위 확인)
- [ ] `./tests/run-tests.sh` — 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(customer): add pair-ranked greedy claim resolver
```

---

## 금지 사항

- 정렬 키를 M2 것까지 미리 넣지 않는다. M1 에는 가격이 없다.
- 난수·시드를 도입하지 않는다. 필요해 보이면 순서 키 설계가 빠진 신호다.
- 자격 판정을 리졸버 안으로 끌어오지 않는다 (D6).
