# Step 02: `TargetingPriority` — 대역 거리와 대역 폭

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-01 (`CustomerData.TargetingMin`/`TargetingMax`)
- **후행 단계:** step-03 이 두 함수를 정렬 키로, step-05 가 `BandDistance == 0` 을 "대역 안" 판정으로 쓴다

---

## 목적

`Distance(price, targeting)` 를 **`BandDistance(price, min, max)`** 로 교체하고, 정렬 키 4가 될 **`BandWidth(min, max)`** 를 같은 파일에 추가한다.

```
BandDistance = max(0, min − 가격, 가격 − max)
```

**대역 안이면 0**이다. 이 성질이 M2.5 전체의 축이다 — step-05 의 "즉시 집는다" 판정이 `BandDistance(...) == 0` 한 줄로 끝나는 것도, 정렬 키 1이 대역 안 후보를 전부 동점으로 묶고 키 2(고가 우선)에 넘기는 것도 여기서 나온다.

**대칭성이 깨지는 것이 이번 변경의 핵심이다.** 대역 `100~300` 인 손님에게 90 과 310 은 거리 10 으로 같지만, 150 과 250 은 **둘 다 0** 이다 — 단일 값 시절이라면 서로 다른 거리였다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/TargetingPriority.cs` — 수정
- `Assets/Tests/EditMode/Customers/TargetingPriorityTests.cs` — 수정 (전면 재작성)
- `Assets/Code/Scripts/Runtime/Customers/ClaimPairComparer.cs` — **최소 수정** (호출부만 살린다. 정렬 키 교체는 step-03)

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    public static class TargetingPriority
    {
        /// <summary>
        /// 가격이 대역에서 얼마나 벗어났는가. <b>대역 안이면 0</b>, 작을수록 우선.
        /// </summary>
        public static int BandDistance(int sushiPrice, int targetingMin, int targetingMax);

        /// <summary>
        /// 대역의 폭. <b>좁을수록 우선</b> — 전문가가 범용가를 이긴다.
        /// </summary>
        public static int BandWidth(int targetingMin, int targetingMax);
    }
}
```

`Distance(int, int)` 는 **삭제한다.** 남기면 다음 사람이 "단일 값 손님" 을 만들려 들고 대역이 두 개의 진실을 갖는다.

### 왜 `Runtime.Data` 가 아니라 여기인가

`BandWidth` 는 `max − min` 한 줄이라 `CustomerData` 프로퍼티로 두고 싶어진다. 두지 않는 이유:

- **`Runtime.Data` 는 계약이지 계산이 아니다.** 산술이 SO 에 들어가면 밸런스 애셋을 보는 사람과 정렬 키를 보는 사람이 서로 다른 파일을 읽게 된다
- **대역 산술이 한 파일에 모여 있어야 grep 이 성립한다** (D2·D6). step-07 의 preflight 가드가 `BandDistance` 를 허용 목록으로 검사하는데, 폭 계산이 다른 파일에 있으면 그 검사가 절반만 지킨다

### 선행 산출물 의존성

- `SushiDefense.Data.CustomerData.TargetingMin` · `TargetingMax` — step-01

이 파일은 **`CustomerData` 를 참조하지 않는다.** `using SushiDefense.Data` 가 없어야 하고, `using System;` 하나로 끝난다 — 호출자가 `int` 세 개를 풀어서 넘긴다. M2 에서 세운 성질을 유지한다.

### 밸런스 수치

없음. 이 파일에 상수를 두지 않는다.

### 제약

- **자격 판정에서 호출되지 않는다.** 호출자는 `ClaimPairComparer`(step-03)와 `ClaimDeadline`(step-05) 둘뿐이다
- 오버플로를 만들지 않는다. `int` 뺄셈 세 개이고 가격 하한이 100 이라 실무상 문제없지만, `Math.Max` 를 쓰고 곱셈·나눗셈을 넣지 않는다
- **폭으로 거리를 정규화하지 않는다.** `sushi-claim-flow.md` §6 이 대역을 반대한 근거가 정규화였고, M2.5 는 **폭을 나누는 대신 이산 타이브레이커(키 4)로** 쓰는 방식으로 그 문제를 피한다. `BandDistance` 안에 `/ width` 가 나타나면 설계 위반이다
- `Random` 을 쓰지 않는다
- `///` XML 문서 주석 — **"대역 안이 전부 0 이면 정보를 버리는 것 아닌가" 에 대한 답을 남긴다**: 키 2(고가 우선)가 대역 안 동점을 깬다

### 완료 판정

- [ ] `grep -n "BandDistance\|BandWidth" Assets/Code/Scripts/Runtime/Customers/TargetingPriority.cs` — 두 함수 확인
- [ ] `grep -rn --include='*.cs' "TargetingPriority.Distance\|Distance(" Assets/Code/Scripts/ Assets/Tests/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건** (옛 API 잔존 확인)
- [ ] `grep -n "using" Assets/Code/Scripts/Runtime/Customers/TargetingPriority.cs` — **`using System;` 하나뿐**
- [ ] `grep -nE "/ *width|/ *\(.*Max.*Min" Assets/Code/Scripts/Runtime/Customers/TargetingPriority.cs` — **0건** (정규화 금지)
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` — 5번 항목이 FAIL 하면 걸린 줄을 보고한 뒤 진행 (step-07 이 가드를 재조준한다)

### 테스트 이름

```
TargetingPriorityTests
  BandDistance_PriceInsideBand_ReturnsZero
  BandDistance_PriceAtBandEdges_ReturnsZero          ← 폐구간이다
  BandDistance_PriceBelowBand_ReturnsGapToMin
  BandDistance_PriceAboveBand_ReturnsGapToMax
  BandDistance_ZeroWidthBand_BehavesLikeSinglePrice  ← min==max 는 옛 동작과 같다
  BandDistance_TwoPricesInsideBand_AreEqual          ← 150 과 250 이 동점. 키 2 가 깬다는 근거
  BandWidth_WideBand_IsGreaterThanNarrow
  BandWidth_ZeroWidthBand_ReturnsZero
```

> `BandDistance_TwoPricesInsideBand_AreEqual` 은 **공허하게 통과할 수 있다.** `0 == 0` 을 확인하는 것만으로는 구현이 상수 0 을 돌려줘도 통과한다. 같은 테스트 안에서 **대역 밖 값이 0 이 아님**을 함께 확인해 붙잡는다.

### 예상 커밋 메시지

```
feat(customer): compute targeting distance against a band instead of a point
```

---

## 금지 사항

- 정렬 키를 건드리지 않는다. `ClaimPairComparer` 는 **컴파일을 살리는 최소 수정만** — 키 4(대역 폭) 삽입은 step-03 이다
- 마감시한·대기 판정을 여기에 넣지 않는다. 이 파일은 **상태가 없는 순수 산술**로 남는다
- `Distance` 를 `[Obsolete]` 로 남기지 않는다. 지운다
- `CustomerData` 를 이 파일에서 참조하지 않는다
