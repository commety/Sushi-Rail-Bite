# Step 03: `ClaimPairComparer` — 정렬 키를 4단에서 5단으로

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-02 (`BandDistance` · `BandWidth`)
- **후행 단계:** step-05 의 `ClaimDeadline` 이 이 비교자를 재사용해 "최선 후보" 를 고른다

---

## 목적

키 1을 대역 거리로 갈고, **키 4에 대역 폭을 끼워 넣는다.**

| # | 키 | 방향 | M2 → M2.5 |
|---|---|---|---|
| 1 | `BandDistance(가격, min, max)` | 가까울수록 먼저 | **교체** — 대역 안은 전부 0 |
| 2 | 초밥 가격 | 높을수록 먼저 | 그대로 — **이제 대역 안 동점을 깨는 것이 주 역할** |
| 3 | 초밥 순차번호 | 낮을수록 먼저 | 그대로 |
| 4 | **대역 폭** | **좁을수록 먼저** | **신규** |
| 5 | 손님 순차번호 | 낮을수록 먼저 | 그대로 (4번이 위로 끼어들었다) |

**키 4가 키 5보다 위인 것이 이 단계의 전부다.** 없으면 기본(100~300)과 소식(300~550)이 300 에서 겹칠 때 **배치 순서**가 승자를 정한다 — 소식좌가 자기 전문 분야를 먼저 앉은 범용 손님에게 빼앗기는 그림은, 이 마일스톤이 없애려는 "의아함"과 같은 종류다.

**초밥 키(1~3)가 손님 키(4~5)보다 위**라는 원칙은 유지된다. TD 로 보면 적이 최선, 타워가 차선이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/ClaimPairComparer.cs` — 수정 **(이 단계에서 바뀌는 유일한 프로덕션 파일)**
- `Assets/Tests/EditMode/Customers/ClaimPairComparerTests.cs` — 수정
- `Assets/Tests/EditMode/Customers/SushiClaimResolverTests.cs` — 수정 (대역 전제로 케이스 재작성)

### 핵심 심볼

시그니처는 **바뀌지 않는다.** 몸통만 바뀐다.

```csharp
public sealed class ClaimPairComparer : IComparer<ClaimCandidatePair>
{
    public int Compare(ClaimCandidatePair a, ClaimCandidatePair b);
}
```

```csharp
// 4순위 — 대역 폭. 좁은 쪽이 먼저다. 전문가가 범용가를 이긴다.
var byWidth = WidthOf(a).CompareTo(WidthOf(b));
if (byWidth != 0) return byWidth;

// 5순위 — 손님 순차번호. 같은 초밥을 같은 폭의 손님 둘이 겨룰 때만 작동한다.
return a.Customer.State.SequenceNumber.CompareTo(b.Customer.State.SequenceNumber);
```

### 선행 산출물 의존성

- `SushiDefense.Customers.TargetingPriority.BandDistance` · `BandWidth` — step-02

### 밸런스 수치

없음. 임계값·가중치를 두지 않는다 — **5개 키의 사전식 순서가 전부다.**

### 제약

- **`SushiClaimResolver.cs` 를 수정하지 않는다** (D3). 그리디 루프도 `TryClaim` 중복 방지도 그대로다. 케이스별 분기(1:N / N:1 / N:M)를 만들지 않는다 — 이 한 규칙이 셋을 전부 덮는다
- **완전순서를 유지한다.** 손님 순차번호가 유일하므로 서로 다른 쌍에서 `Compare` 가 0 을 돌려주지 않는다. 이게 깨지면 `List.Sort` 가 불안정 정렬이라 **결정성이 무너진다**
- 키 계산에 할당을 만들지 않는다. `ClaimCandidatePair` 는 `readonly struct` 이므로 그대로 두고, LINQ·람다를 쓰지 않는다 — 이 비교자는 배정마다 `O(n log n)` 번 불린다
- `Random` 을 쓰지 않는다
- `///` 문서 주석의 4단 목록을 **5단으로 갱신**한다. 여기가 "기획의 정렬 키가 사는 유일한 지점" 이라 주석이 곧 사양이다

### 완료 판정

- [ ] `grep -c "순위" Assets/Code/Scripts/Runtime/Customers/ClaimPairComparer.cs` — 주석에 **5단**이 다 있다
- [ ] `grep -n "BandWidth" Assets/Code/Scripts/Runtime/Customers/ClaimPairComparer.cs` — 키 4 확인
- [ ] `git diff --stat Assets/Code/Scripts/Runtime/Customers/SushiClaimResolver.cs` — **0줄**
- [ ] `grep -rn "Targeting\|Price" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` — 5번 항목 FAIL 시 걸린 줄 보고 후 진행

### 테스트 이름

```
ClaimPairComparerTests
  Compare_NearerToBand_ComesFirst
  Compare_BothInsideBand_HigherPriceComesFirst        ← 키 2 가 대역 안 동점을 깬다
  Compare_SameDistanceSamePrice_LowerSushiSequenceComesFirst
  Compare_SameSushiNarrowerBand_ComesFirst            ← 키 4 신규
  Compare_SameSushiSameWidth_LowerCustomerSequenceComesFirst
  Compare_BandWidthOutranksCustomerSequence           ← 키 4 > 키 5 를 못박는다
  Compare_SushiKeyOutranksCustomerKey                 ← 키 3 > 키 4 (초밥 우선 원칙)
  Compare_DifferentPairs_NeverReturnsZero             ← 완전순서 회귀

SushiClaimResolverTests
  Resolve_TwoInBandSushi_TakesHigherPrice
  Resolve_NarrowBandVsWideBand_NarrowWins
  Resolve_NarrowBandPlacedLater_StillWins             ← 전문가가 배치순을 이긴다
  Resolve_SameBoardTwice_ProducesIdenticalResult      ← 결정성 회귀
```

> **공허한 통과를 조심한다.** `Compare_SameSushiNarrowerBand_ComesFirst` 는 좁은 대역 손님을 **낮은 순차번호로** 배치하면 키 5 만으로도 통과한다. **좁은 대역 손님에게 더 큰 순차번호를 준다** — 그래야 키 4 가 실제로 작동했음이 증명된다. `Resolve_NarrowBandPlacedLater_StillWins` 가 같은 것을 리졸버 층에서 다시 잡는다.
>
> 마찬가지로 `Compare_BandWidthOutranksCustomerSequence` 는 두 손님의 **초밥이 같아야** 의미가 있다. 초밥이 다르면 키 1~3 에서 이미 갈려 키 4 가 불리지 않는다.

### 예상 커밋 메시지

```
feat(customer): rank claims by band distance and prefer narrower bands
```

---

## 금지 사항

- `SushiClaimResolver` 를 수정하지 않는다. 리졸버가 깨져 보이면 비교자를 잘못 고친 것이다
- 마감시한·대기 판정을 여기에 넣지 않는다 (D3). **비교자는 시간을 모른다** — `nowSeconds` 를 인자로 받고 싶어지면 step-05 로 갈 내용이다
- 대역 폭으로 거리를 나누지 않는다. **폭은 이산 타이브레이커다** (step-02 참조)
- 키의 순서를 바꾸지 않는다. 초밥 키가 손님 키보다 위인 원칙은 협상 대상이 아니다
