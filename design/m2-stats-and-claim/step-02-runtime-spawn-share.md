# Step 02: `SpawnShareTable` — 가격에서 등장 비율을 유도한다

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-01 완료 필요 (`SushiSpawnEntry` 에 `Weight` 가 없어야 함, `StageConfig.SparsityExponent`)
- **후행 단계:** step-03 이 이 클래스의 `ShareOf(int index)` 를 credit 누적에 쓴다

---

## 목적

**비쌀수록 적게, 쌀수록 많이** 나오게 하되 비싼 카드도 제 주기로는 반드시 나오게 한다. 이 단계는 그중 **"얼마나 자주"** 만 답한다 — 순서는 step-03 이다.

```
share_i  ∝  (minPrice / price_i) ^ α
```

`minPrice` 는 **덱 안의 최저가**다. 스테이지 스폰 테이블 전체나 전역 최저가가 아니다. 덱이 바뀌면 기존 카드의 등장률도 바뀌는데, 이건 버그가 아니라 **설계 목표**다 — 비싼 카드를 넣으면 나머지가 상대적으로 흔해지는 것이 덱빌딩의 의미다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Belt/SpawnShareTable.cs` — **생성**
- `Assets/Tests/EditMode/Belt/SpawnShareTableTests.cs` — **생성**

### 핵심 심볼

```csharp
namespace SushiDefense.Belt
{
    /// <summary>덱 + α → 유형별 등장 비율. 가격을 읽는 곳이다.</summary>
    public sealed class SpawnShareTable
    {
        /// <summary>덱에 실제로 오른 유형 수. 비거나 초밥이 null 인 항목은 빠진다.</summary>
        public int Count { get; }

        /// <summary>덱 안 최저가. 모든 share 의 기준점이다.</summary>
        public int MinimumPrice { get; }

        /// <summary>이 인덱스의 초밥 종류. 덱 순서를 유지한다.</summary>
        public SushiData SushiAt(int index);

        /// <summary>이 인덱스의 등장 비율. 전체 합이 1 이 되도록 정규화돼 있다.</summary>
        public float ShareOf(int index);

        public SpawnShareTable(IReadOnlyList<SushiSpawnEntry> deck, float sparsityExponent);
    }
}
```

### 선행 산출물 의존성

- `SushiDefense.Data.SushiSpawnEntry` — step-01 에서 `Sushi` 참조만 남도록 좁혀짐
- `SushiDefense.Data.StageConfig.SparsityExponent` — step-01 에서 추가됨. **이 클래스는 `StageConfig` 를 받지 않고 `float` 로 받는다** (덱과 α 만 있으면 계산되므로 스테이지 전체를 끌고 올 이유가 없다)

### 밸런스 수치

- α 는 인자로 받는다. **코드에 1.0 을 적지 않는다** — 기본값은 step-01 의 SO 필드에 있다
- `MinimumPrice` 하한 100 은 step-01 의 SO 검증이 보장한다. 여기서 다시 검사하지 않는다

### 제약

- **생성자에서 전부 계산해 배열에 넣는다.** `ShareOf` 가 호출마다 `Mathf.Pow` 를 돌면 스폰 경로에 연산이 쌓인다. 배열은 생성자에서 한 번만 잡는다 (`CLAUDE.md` §4.3 — 배포 타깃이 WebGL)
- **정규화는 필수다.** `(minPrice/price)^α` 의 합은 1 이 아니다. 합으로 나눠 share 총합을 1 로 만든다 — step-03 의 credit 누적이 "매 스폰마다 share 합만큼 credit 이 는다" 는 전제 위에 서 있다
- **비율만 쓴다.** `price / 10` 같은 절대 스케일 연산이 들어가면 안 된다. 모든 가격을 10배로 키워도 share 가 변하지 않아야 한다
- 덱이 비었으면 `Count == 0`. 예외를 던지지 않는다 — 벨트가 "스폰할 것이 없다" 로 조용히 지나가야 한다 (M1 `SpawnSequence.IsEmpty` 와 같은 계약)
- `Random` 을 쓰지 않는다
- 로직은 `MonoBehaviour` 밖 순수 C# (`CLAUDE.md` §3.2)

### 완료 판정

- [ ] `grep -rn "class SpawnShareTable" Assets/Code/Scripts/Runtime/Belt/` — 정의 확인
- [ ] `grep -rn "Random" Assets/Code/Scripts/Runtime/Belt/SpawnShareTable.cs` — **0건**
- [ ] `grep -n "Mathf.Pow" Assets/Code/Scripts/Runtime/Belt/SpawnShareTable.cs` — 생성자 안에만 등장 (`ShareOf` 본문에 없음)
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름 (플랜 §테스트 항목 그대로)

```
Share_ExpensiveSushi_LowerThanCheap
Share_AllPricesScaledTenfold_ProducesIdenticalShares    ← 비율만 쓴다는 것의 검증
Share_AlphaOne_EqualRevenuePerType                       ← price × share 가 상수
Share_DeckWithoutCheapestCard_RebasesOnDeckMinimum       ← minPrice 는 덱 안에서
Share_AllShares_SumToOne
Share_AlphaZero_AllTypesEqual
Share_EmptyDeck_CountIsZero
Share_EntryWithNullSushi_Excluded
```

`Share_AlphaOne_EqualRevenuePerType` 이 이 단계의 **핵심 회귀선**이다. 플랜의 예시(장어 300 · 방어 150 · 연어 120 · 한치 100 · 광어 100, α=1.0)를 그대로 써서 `price × share` 가 다섯 유형 모두 같은지 본다.

부동소수 비교는 `Assert.That(actual, Is.EqualTo(expected).Within(tolerance))` 로 한다. tolerance 는 테스트 안의 지역 상수로 두고, 밸런스 값이 아니므로 SO 로 빼지 않는다.

### 예상 커밋 메시지

```
feat(belt): derive spawn share from price and sparsity exponent
```

---

## 금지 사항

- `SpawnSequence.cs` 를 건드리지 않는다. 교체는 step-03 이다
- `SushiPool` 크기를 이 계산에 끌어들이지 않는다. **풀 크기는 구성 규칙이 아니라 프리웜 힌트다** (`docs/plan/M2-stats-and-claim.md`)
- 인스턴스 재고(가방) 방식으로 되돌리지 않는다. 이 클래스가 답하는 것은 **시간당 등장 빈도**이지 재고량이 아니다
