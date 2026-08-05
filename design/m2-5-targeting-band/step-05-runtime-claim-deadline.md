# Step 05: `ClaimDeadline` — 지금 확정할까, 기다릴까

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-03 (5단 정렬 키) · step-04 (이탈 시각). **둘 다 필요하다**
- **후행 단계:** step-06 이 `IsWaiting` 을 화면에 그린다

---

## 목적

**M2.5 의 본론이다.** 즉시 확정(eager commit)을 없앤다.

| # | 조건 | 행동 |
|---|---|---|
| 1 | 후보 중 **대역 안**이 있다 | **즉시 집는다** |
| 2 | 대역 밖만 있다 | 그중 **가장 좋은 것이 범위를 벗어나기 직전**까지 기다린다 |
| 3 | 그 시각이 됐다 | **집는다** |

*"소식좌가 싼 걸 먼저 집는다"* 의 진짜 원인은 정렬 키가 아니라 **후보가 함께 있지 않다는 것**이었다. 싼 초밥이 먼저 범위에 들어오면 후보가 그것 하나뿐이라 정렬 키가 작동할 기회 자체가 없다. 대기가 그 기회를 만든다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/ClaimDeadline.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — 수정 (필터 + `IsWaiting`)
- `Assets/Tests/EditMode/Customers/ClaimDeadlineTests.cs` — **생성**
- `Assets/Tests/EditMode/Customers/ClaimCoordinatorTests.cs` — 수정

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>
    /// "지금 배정을 확정할까, 더 좋은 것을 기다릴까" 만 답한다. <b>누가 무엇을 가져가는지는
    /// 모른다</b> — 그건 <see cref="SushiClaimResolver"/> 의 몫이다 (작업서 D3).
    /// </summary>
    public sealed class ClaimDeadline
    {
        public ClaimDeadline(ClaimPairComparer comparer);

        /// <summary>
        /// 이 손님의 배정을 지금 확정해도 되는가.
        /// 후보가 없으면 <c>false</c>(확정할 것이 없다).
        /// </summary>
        public bool IsDue(CustomerLogic customer, CandidateSet candidates, float nowSeconds);
    }
}
```

판정 순서:

```
1. 후보 중 살아 있는 것(OnBelt)들 가운데 ClaimPairComparer 로 최선을 고른다
2. 없으면 false
3. 최선이 대역 안이면 (BandDistance == 0) → true
4. 아니면 nowSeconds >= 그 최선 후보의 ExitAtOf → true, 아니면 false
```

**3번이 "최선만 보면 되는" 이유**: 대역 거리가 정렬 키 1이므로, **최선이 대역 밖이면 나머지도 전부 대역 밖이다.** 후보를 두 번 훑지 않는다.

**4번이 곧 "갱신"이다.** 더 좋은 후보가 인식되면 최선이 바뀌고 마감시한도 함께 따라 움직인다 — 별도의 갱신 코드가 없다. 못한 후보가 먼저 사라지는 것은 손해가 아니다.

`ClaimCoordinator` 쪽:

```csharp
/// <summary>지금 더 좋은 후보를 기다리는 중인가. 진단·표시용이다.</summary>
public bool IsWaiting(CustomerLogic customer);
```

`ResolveClaims()` 앞에 필터가 붙는다:

```csharp
private void ResolveClaims()
{
    _due.Clear();
    for (var i = 0; i < _customers.Count; i++)
    {
        var customer = _customers[i];
        var set = _candidates[customer];
        var due = _deadline.IsDue(customer, set, _elapsedSeconds);

        _waiting[customer] = !due && set.Count > 0;
        if (due) _due.Add(customer);
    }

    _resolver.Resolve(_due, _candidates, _claims);   // ← 리졸버는 변경 0줄
    …
}
```

### 왜 비교자를 재사용하나

`ClaimDeadline` 이 "최선 후보" 를 스스로 정하면 **정렬 키가 두 파일에 살게 된다.** 그러면 키가 바뀔 때 한쪽만 고치는 사고가 난다.

같은 손님끼리 비교하면 키 4(대역 폭)·5(손님 SeqNo)가 동률이라 **키 1~3 만 작동한다** — 즉 `ClaimPairComparer` 를 그대로 넣으면 정확히 "이 손님에게 가장 좋은 초밥" 이 나온다. 별도 로직이 필요 없다.

### 선행 산출물 의존성

- `TargetingPriority.BandDistance` — step-02
- `ClaimPairComparer` (5단) — step-03
- `CandidateSet.ExitAtOf` — step-04

### 밸런스 수치

**없다.** 유예 시간·인내심 계수 같은 필드를 만들지 않는다 — 마감시한은 전부 기하에서 나온다. 계획서의 `PatienceRatio` 는 **도입하지 않는다** (열린 질문 #3 기본안).

### 제약

- **`SushiClaimResolver.cs` 를 수정하지 않는다** (D3). `git diff` 0줄로 확인한다
- **자격 판정을 여기서 다시 하지 않는다.** 자격 없는 손님은 `SyncEligibility`(틱 5단계)가 이미 후보를 비웠으므로 `Count == 0` 으로 자연히 걸러진다. `CanAcceptSushi` 를 `ClaimDeadline` 안에서 부르면 자격과 타이밍이 한 함수가 된다
- **`nowSeconds >= exitAt` 로 판정한다.** `> ` 가 아니다. 그리고 이탈 직후 한 틱 늦게 확정되는 것은 **정상이다** — 래치가 정확히 그 지연을 받쳐 주기 위해 존재한다 (§1.1-3c). 이탈 시각을 앞당겨 보정하는 코드(`exitAt − deltaSeconds` 등)를 넣지 않는다
- **`_waiting` 을 `RemoveCustomer` 에서 지운다.** `_candidates` · `_wasEligible` · `_appetites` 와 같은 수명이다. 빠뜨리면 배치 취소된 손님이 딕셔너리에 남는다
- `_due` 리스트를 **필드로 잡아 재사용**한다. 틱마다 `new List<>` 를 만들면 WebGL 에서 GC 스파이크가 그대로 히칭이 된다 (§4.3)
- 틱 6단계의 순서를 바꾸지 않는다. 필터는 **6단계 안**에 들어간다 (새 7단계가 아니다)
- `Random` 을 쓰지 않는다
- 로직은 `MonoBehaviour` 밖 순수 C#

### 대기의 비용 — 의도된 성질

기다리는 동안 **다른 손님이 그 초밥을 가져갈 수 있다.** 확정된 손님(`_due`)만 리졸버에 들어가므로, 대기 중인 손님은 그 초밥에 대한 지분이 없다.

**버그가 아니라 게임이다.** 대기에 아무 위험이 없으면 대역이 좁은 손님이 일방적으로 유리해진다. 이 성질을 테스트로 고정하지는 않되(밸런스에 따라 흔들린다) **도메인 문서에 남긴다** (step-07).

### 완료 판정

- [ ] `grep -n "IsDue" Assets/Code/Scripts/Runtime/Customers/ClaimDeadline.cs` — 정의 확인
- [ ] `git diff --stat Assets/Code/Scripts/Runtime/Customers/SushiClaimResolver.cs` — **0줄**
- [ ] `grep -rn "CanAcceptSushi\|CanTake\|IsInReach" Assets/Code/Scripts/Runtime/Customers/ClaimDeadline.cs` — **0건** (자격을 다시 보지 않는다)
- [ ] `grep -rn "Targeting\|Price" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs Assets/Code/Scripts/Runtime/Customers/CustomerAppetiteMachine.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] `grep -rn "new List<" Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — 전부 **필드 초기화**, 메서드 안에 없다
- [ ] `grep -n "_waiting" Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — `PlaceCustomer` · `RemoveCustomer` · `ResolveClaims` 세 곳에서 다뤄진다
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` — 5번 항목이 `ClaimDeadline` 을 오탐할 수 있다. **걸린 줄을 보고하고 진행한다** (step-07 이 가드를 재조준)

### 테스트 이름

```
ClaimDeadlineTests
  IsDue_NoCandidates_ReturnsFalse
  IsDue_InBandCandidate_ReturnsTrueImmediately
  IsDue_OnlyOutOfBandBeforeExit_ReturnsFalse
  IsDue_OnlyOutOfBandAtExit_ReturnsTrue
  IsDue_BetterCandidateRecognized_PushesDeadlineLater      ← 갱신
  IsDue_WorseCandidateExitsFirst_StillWaits                ← 못한 후보의 이탈은 무관
  IsDue_InBandArrivesWhileWaiting_ReturnsTrueImmediately   ← 규칙 2 → 규칙 1 전환

ClaimCoordinatorTests
  Tick_InBandSushiRecognized_ClaimsImmediately
  Tick_OnlyOutOfBandSushi_DefersUntilExit
  Tick_AtDeadline_TakesNearestToBand                       ← SeqNo 순이 아니다
  Tick_OnlyOutOfBandSushiInReach_TakesItBeforeExit         ← ★ 불변식
  Tick_WaitingCustomer_IsWaitingReturnsTrue
  Tick_EatingCustomer_IsWaitingReturnsFalse
  Tick_SameBoardTwice_ProducesIdenticalClaims              ← 결정성 회귀
```

> **★ `Tick_OnlyOutOfBandSushiInReach_TakesItBeforeExit` 이 이 마일스톤의 방어선이다.** 대역이 자격 게이트로 굳는 사고를 이 테스트 하나가 막는다. **손님 1명 · 대역 밖 초밥 1개** 로 짜서, 못 집은 이유가 자격인지 대기인지 섞이지 않게 한다.
>
> `Tick_AtDeadline_TakesNearestToBand` 는 **공허하게 통과하기 쉽다.** 대역에 가장 가까운 초밥을 **낮은 SeqNo 로** 스폰하면 옛 FIFO 구현으로도 통과한다. **가까운 쪽에 더 큰 SeqNo 를 준다.**
>
> `IsDue_WorseCandidateExitsFirst_StillWaits` 도 마찬가지다. 두 후보의 이탈 시각이 **뒤바뀌어 있어야**(못한 쪽이 먼저 나감) "최선 기준" 이 실제로 작동했음이 증명된다.

### 예상 커밋 메시지

```
feat(customer): defer out-of-band claims until the piece leaves reach
```

---

## 금지 사항

- `SushiClaimResolver` 를 수정하지 않는다 (D3). 리졸버에 `nowSeconds` 를 넘기고 싶어지면 설계가 틀어진 것이다
- **대역을 자격 게이트로 쓰지 않는다.** `if (BandDistance(...) > 0) return false;` 형태가 **자격 경로**에 생기면 기획 위반이다. `ClaimDeadline` 안의 `false` 는 "아직 아니다" 이지 "못 먹는다" 가 아니며, 유한 시간 뒤 반드시 `true` 가 되어야 한다
- `CustomerState` 에 `Waiting` 을 추가하지 않는다 (D5). 추가하면 `CanAcceptSushi` 가 `State == Idle` 을 보므로 **대기가 곧 굶기가 된다**
- `CustomerRuntimeState` 에 대기 플래그를 넣지 않는다 (D5). 클레임 타이밍이 손님 상태 객체로 새면 안 된다
- `PatienceRatio` 같은 유예 계수를 만들지 않는다
- `Presentation` 을 수정하지 않는다. 화면은 step-06 이다
