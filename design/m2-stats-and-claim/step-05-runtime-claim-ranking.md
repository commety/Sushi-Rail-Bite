# Step 05: `ClaimPairComparer` — 정렬 키 4단 완성

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-04 (`TargetingPriority.Distance`)
- **후행 단계:** step-09 가 완성된 배정을 조율자에 물린다

---

## 목적

M1 은 정렬 키가 2단이었다 (초밥 SeqNo → 손님 SeqNo). M2 는 그 **앞에** 2단을 붙여 기획의 4단 완전순서를 완성한다.

| # | 키 | 방향 | M1/M2 |
|---|---|---|---|
| 1 | `\|가격 − 타겟팅\|` | 작을수록 먼저 | **M2 신규** |
| 2 | 가격 | **클수록 먼저** | **M2 신규** |
| 3 | 초밥 순차번호 | 작을수록 먼저 | M1 |
| 4 | 손님 순차번호 | 작을수록 먼저 | M1 |

**이 파일 하나만 바뀐다.** `SushiClaimResolver` 의 그리디 루프도, `TryClaim` 중복 방지도, `CandidateSet` 도 한 줄도 손대지 않는다 — M1 이 이 지점을 "M2 가 정렬 키를 추가하는 유일한 지점" 으로 남겨 둔 이유다.

**초밥 키가 손님 키보다 위다.** TD 로 보면 적(초밥)이 최선, 타워(손님)가 차선이다. 순차번호가 유일하므로 이 비교는 **완전순서**이고, 서로 다른 쌍에서 0 이 나오지 않는다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/ClaimPairComparer.cs` — 수정 (키 2개 추가)
- `Assets/Tests/EditMode/Customers/ClaimPairComparerTests.cs` — 수정 (1·2순위 케이스 추가)
- `Assets/Tests/EditMode/Customers/SushiClaimResolverTests.cs` — 수정 (플랜의 배정 시나리오 전량)

### 핵심 심볼

시그니처는 **바뀌지 않는다.** `Compare` 본문만 확장한다.

```csharp
public int Compare(ClaimCandidatePair a, ClaimCandidatePair b)
{
    // 1순위 — 타겟팅 거리. 가까울수록 먼저.
    var byDistance = TargetingPriority.Distance(a.Sushi.Data.Price, a.Customer.State.Data.TargetingPrice)
        .CompareTo(TargetingPriority.Distance(b.Sushi.Data.Price, b.Customer.State.Data.TargetingPrice));
    if (byDistance != 0) return byDistance;

    // 2순위 — 가격. 높을수록 먼저 (부호 반전).
    var byPrice = b.Sushi.Data.Price.CompareTo(a.Sushi.Data.Price);
    if (byPrice != 0) return byPrice;

    // 3순위 — 초밥 순차번호. (M1 그대로)
    // 4순위 — 손님 순차번호. (M1 그대로)
}
```

### 선행 산출물 의존성

- `SushiDefense.Customers.TargetingPriority.Distance` — step-04
- `SushiDefense.Data.SushiData.Price` · `CustomerData.TargetingPrice` — M0 부터 존재

### 밸런스 수치

없음. 거리 임계값·가중치를 만들지 않는다.

### 제약

- **케이스별 분기를 만들지 않는다.** 1:N · N:1 · N:M 을 나눠 짜면 경계에서 반드시 어긋난다. 정렬 키 하나가 셋을 전부 덮는다 (`.claude/domain/sushi-claim-flow.md` §2)
- **`Random` 을 쓰지 않는다.** 순차번호가 모든 동률을 끝낸다 — 난수원 주입도, 시드 고정도 필요 없다
- 2순위의 **부호 반전을 빠뜨리지 않는다.** 가격만 오름차순으로 두면 "동거리면 싼 쪽" 이 되어 기획과 정반대가 된다
- `Compare` 안에서 할당하지 않는다. 배정은 `Update` 경로다
- 로직은 `MonoBehaviour` 밖 순수 C#

### 완료 판정

- [ ] `grep -n "TargetingPriority" Assets/Code/Scripts/Runtime/Customers/ClaimPairComparer.cs` — 1순위 키 확인
- [ ] `grep -rn "TargetingPriority" Assets/Code/Scripts/Runtime/ | grep -v "TargetingPriority.cs" | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **`ClaimPairComparer.cs` 에서만** 호출된다
- [ ] `grep -rn "Targeting\|Price" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] `git diff --stat Assets/Code/Scripts/Runtime/Customers/SushiClaimResolver.cs` — **변경 0줄**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름 (플랜 §테스트 항목 그대로)

```
자격 (건드리지 않았음을 확인)
  CanTake_SushiFarFromTargeting_ReturnsTrue        ← 멀어도 자격은 있다
  CanTake_SushiLatchedPastReach_ReturnsTrue

배정 — 1:N
  Resolve_OneCustomerManySushi_TakesNearestToTargeting
  Resolve_EqualDistance_TakesHigherPrice           ← 타겟팅 200 → 190 vs 210 이면 210
  Resolve_SamePrice_TakesLowerSushiSequence

배정 — N:1
  Resolve_ManyCustomersOneSushi_NearestTargetingWins
  Resolve_EqualDistanceSameSushi_LowerCustomerSequenceWins

배정 — N:M
  Resolve_ManyToMany_AssignsInSequenceOrder

배정 — 불변식
  Resolve_SameBoardTwice_ProducesIdenticalResult   ← 결정성 회귀선
  Resolve_SushiAlreadyClaimed_NotAssignedAgain
  Resolve_CandidatesExist_AlwaysAssignsSomeone     ← "아무도 못 집는" 결과는 나올 수 없다
```

**`Resolve_EqualDistance_TakesHigherPrice` 가 이 단계의 핵심**이다. 2순위 부호를 뒤집어 쓰면 이 테스트만 깨진다.

`Resolve_CandidatesExist_AlwaysAssignsSomeone` 은 기획 규칙을 직접 고정한다 — 후보가 있으면 반드시 하나가 정해진다. 손님이 눈앞의 초밥을 두고 구경하는 상태는 **플레이 경험 버그**다.

**자격과 배정을 한 테스트에서 같이 검증하지 않는다.** 섞으면 먼 초밥을 안 먹었을 때 자격이 막은 건지 배정이 다른 걸 고른 건지 구분되지 않는다.

### 예상 커밋 메시지

```
feat(customer): rank claim pairs by targeting distance then price
```

---

## 금지 사항

- **`SushiClaimResolver.cs` 를 수정하지 않는다.** 그리디 루프와 중복 방지는 M1 그대로 맞다. 여기를 고쳐야 할 것 같으면 멈추고 보고한다
- `CustomerLogic.cs` · `CandidateSet.cs` 를 수정하지 않는다
- 자격 판정에 거리 임계값을 넣지 않는다. **이것이 이 마일스톤 최대 위험이다**
- 정렬 키 순서를 바꾸지 않는다 (SeqNo 를 타겟팅 위로 올리는 어그로 방식은 **검토 후 미채택**이다 — `.claude/domain/sushi-claim-flow.md` §2)
