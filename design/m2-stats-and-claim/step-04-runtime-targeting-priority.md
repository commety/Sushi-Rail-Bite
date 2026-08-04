# Step 04: `TargetingPriority` — 타겟팅 거리 계산

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-01 (가격 하한). 실질적으로는 M1 상태에서 바로 시작 가능
- **후행 단계:** step-05 의 `ClaimPairComparer` 가 이 계산을 1·2순위 키로 쓴다

---

## 목적

배정 정렬의 1순위 키를 **한 곳에 가둔다.**

```
−|초밥 가격 − 손님 타겟팅|    (가까울수록 먼저)
```

계산 자체는 세 줄이다. 이걸 굳이 별도 타입으로 빼는 이유는 **가격을 읽는 지점을 셀 수 있게 만들기 위해서**다. 비교자 안에 산술을 인라인하면, 다음 사람이 같은 식을 자격 판정 쪽에 복사해 넣어도 grep 이 잡지 못한다.

**거리는 대칭이다 — 의도된 것이다.** 타겟팅 200 인 손님은 199 짜리를 250 짜리보다 먼저 집는다. 점수만 보면 손해지만, 모든 손님이 무조건 비싼 걸 먹으면 타겟팅도 배치도 의미가 없어진다. 손님이 **탐욕적이지 않은 선호**를 갖기 때문에 플레이어의 배열이 전략이 된다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/TargetingPriority.cs` — **생성**
- `Assets/Tests/EditMode/Customers/TargetingPriorityTests.cs` — **생성**

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>
    /// 배정 우선순위의 1순위 키. <b>가격을 읽는 세 지점 중 하나</b>이며,
    /// 자격 판정에서는 절대 호출되지 않는다.
    /// </summary>
    public static class TargetingPriority
    {
        /// <summary>
        /// 가격과 타겟팅 사이의 거리. <b>작을수록 우선</b>이다.
        ///
        /// 도메인 문서의 표기는 <c>−|가격 − 타겟팅|</c>(클수록 우선)이지만,
        /// 부호를 뒤집어 오름차순으로 다루는 편이 비교자에서 헷갈릴 자리가 없다.
        /// 두 표현은 정렬 결과가 동일하다.
        /// </summary>
        public static int Distance(int sushiPrice, int targetingPrice);
    }
}
```

### 선행 산출물 의존성

없음. `int` 두 개만 받는다 — **`SushiItem` 도 `CustomerData` 도 받지 않는다.** 타입 의존이 없어야 이 함수가 어디서 호출되는지 grep 한 줄로 세어진다.

### 밸런스 수치

없음. 임계값·허용 오차 같은 튜닝 손잡이를 만들지 않는다.

### 제약

- **비대칭 가중치를 넣지 않는다.** MVP 는 대칭으로 간다. 플레이 후 "손님이 멍청해 보인다" 는 피드백이 나오면 그때 도입할 튜닝 손잡이로 남겨 둔다 (`.claude/domain/sushi-claim-flow.md` §2)
- **정규화하지 않는다.** 타겟팅이 단일 값이라 모든 손님의 거리가 같은 단위(엔)다 — 그게 대역 방식을 버린 이유다 (§6)
- `Mathf.Abs` 든 `Math.Abs` 든 무방하나, **`UnityEngine` 의존을 넣지 않는 편**을 선호한다. 이 파일은 Unity API 가 필요 없다
- `Random` 을 쓰지 않는다
- public API 에 `///` 문서 주석. **"자격 판정에서 부르지 않는다"** 를 명시적으로 남긴다

### 완료 판정

- [ ] `grep -rn "class TargetingPriority" Assets/Code/Scripts/Runtime/Customers/` — 정의 확인
- [ ] `grep -rn "Targeting" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건** (자격에 타겟팅이 없다)
- [ ] `grep -rn "Price" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름

```
Distance_ExactMatch_IsZero
Distance_BelowTargeting_IsSymmetricToAbove       ← 190 과 210 의 거리가 같다 (타겟팅 200)
Distance_FarPrice_IsLargerThanNear
Distance_ZeroTargeting_UsesPriceAsDistance
```

`Distance_BelowTargeting_IsSymmetricToAbove` 가 **대칭성 회귀선**이다. 누군가 "비싼 쪽을 선호하게" 비대칭 가중치를 넣으면 이 테스트가 먼저 깨진다.

### 예상 커밋 메시지

```
feat(customer): add targeting distance as the primary claim ranking key
```

---

## 금지 사항

- `CustomerLogic.cs` 를 **한 글자도 수정하지 않는다.** 이 단계에서 그 파일이 바뀌면 D1 방어선이 무너진 것이다
- `ClaimPairComparer` 를 건드리지 않는다. 정렬 키 추가는 step-05 다
- 이 함수를 `SushiClaimResolver` 나 `CandidateSet` 에서 호출하지 않는다. 호출자는 **비교자 하나뿐**이다
