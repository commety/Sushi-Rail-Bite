# Step 07: `RevenueLedger` · `RecruitWallet` — 매출과 영입 재화

- **영역:** `runtime` — 어셈블리 `Runtime` (네임스페이스 `SushiDefense.Scoring`)
- **선행 단계:** step-01 (가격 하한 100 — `가격/10` 정수 나눗셈의 전제)
- **후행 단계:** step-08 이 지갑으로 배치를 막고, step-09 가 소비 시점에 둘 다 채운다

---

## 목적

**두 개의 다른 자원**을 만든다. 섞지 않는다 ([`data-model.md`](../../.claude/domain/data-model.md) §4).

| 자원 | 획득 | 용도 |
|---|---|---|
| **매출(점수)** | 소비된 초밥 가격의 합 | 스테이지 클리어 판정 (M3) |
| **영입 재화** | `가격 / 10` 누적 + 스테이지 초기 예산 | 손님 배치 |

### 착수 시 확정한 계산 규칙

**정수 누적이다.** 초밥 1개를 소비할 때마다 `가격 / 10` 을 **정수 나눗셈**해 더한다.

사람 판단: *"초밥은 실제 엔 단위로 100 이상이다. 100 미만은 오류다"* — 가격 하한이 100 이므로 초밥 1개당 최소 10 이 들어오고, `data-model.md` §4 가 걱정한 "가격 5짜리를 계속 먹어도 재화가 안 오르는" 상황이 구조적으로 발생하지 않는다. 그래서 실수 누적·잔여분 이월 같은 장치가 필요 없다.

> 플랜의 제안(실수 누적, 표시할 때만 버림)은 **채택하지 않았다.** step-10 에서 `data-model.md` §4 를 갱신한다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Scoring/RevenueLedger.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Scoring/RecruitWallet.cs` — **생성**
- `Assets/Tests/EditMode/Scoring/RevenueLedgerTests.cs` — **생성**
- `Assets/Tests/EditMode/Scoring/RecruitWalletTests.cs` — **생성**

`Assets/Code/Scripts/Runtime/Scoring/` 폴더는 **새로 만든다** (`CLAUDE.md` §10 의 목표 구조에 이미 있는 자리다). **새 `.asmdef` 는 만들지 않는다** — `Runtime` 어셈블리 안의 하위 폴더일 뿐이다.

### 핵심 심볼

```csharp
namespace SushiDefense.Scoring
{
    /// <summary>스테이지 매출. 클리어 판정(M3)의 입력이다.</summary>
    public sealed class RevenueLedger
    {
        public int Total { get; }

        /// <summary>초밥 1개가 소비됐다. 가격을 그대로 더한다.</summary>
        public void Add(int price);

        /// <summary>스테이지 시작 시 0 으로 되돌린다.</summary>
        public void Reset();

        public event Action<int> TotalChanged;
    }

    /// <summary>배치에 쓰는 영입 재화. 매출과 별개의 자원이다.</summary>
    public sealed class RecruitWallet
    {
        /// <summary>이 재화 1 이 매출 얼마에 해당하는가. 기획 확정값(점수/10)이다.</summary>
        private const int RevenuePerCurrency = 10;

        public int Balance { get; }

        /// <summary>초기 예산으로 스테이지를 연다. 잔액은 이월되지 않는다.</summary>
        public RecruitWallet(int initialBudget);

        /// <summary>소비된 초밥 가격에서 재화를 적립한다. 정수 나눗셈이다.</summary>
        public void AccrueFrom(int price);

        public bool CanAfford(int cost);

        /// <summary>모자라면 false 를 돌려주고 잔액을 바꾸지 않는다.</summary>
        public bool TrySpend(int cost);

        /// <summary>스테이지 시작 시 초기 예산으로 되돌린다.</summary>
        public void Reset();

        public event Action<int> BalanceChanged;
    }
}
```

### 선행 산출물 의존성

- 없음. **`SushiItem` 도 `SushiData` 도 받지 않는다** — `int price` 만 받는다. 그래야 이 두 클래스가 `Runtime.Data` 조차 참조하지 않고, `Scoring` 이 벨트·손님 어느 쪽에도 묶이지 않는다

### 밸런스 수치

- `initialBudget` 은 `StageConfig.InitialRecruitBudget` 에서 온다. **생성자 인자로 받고 SO 를 직접 읽지 않는다** (호출자가 step-09 의 조립부다)
- `RevenuePerCurrency = 10` 은 **기획 확정 공식**이다 (원본 2차 "영입 재화는 점수/10"). 튜닝 손잡이가 아니므로 SO 필드로 빼지 않는다. 나중에 스테이지별로 바꾸고 싶어지면 그때 SO 로 승격한다

### 제약

- **지갑이 원장을 구독하지 않는다** (작업서 D5). 조율자가 소비 시점에 양쪽 모두에 알린다. 구독하면 "초기 예산"과 "매출 유래분"이 한 스트림에 섞여 잔액 추적이 어려워진다
- **`AccrueFrom` 은 정수 나눗셈이다.** `price / 10` 을 `int` 로 계산한다. `Mathf.FloorToInt(price / 10f)` 처럼 실수를 경유하지 않는다 — 결과는 같지만 실수가 끼면 다음 사람이 잔여분 이월을 넣고 싶어진다
- **`TrySpend` 는 실패 시 잔액을 바꾸지 않는다.** 부분 차감이 없다
- 음수 방어: `Add(음수)` · `AccrueFrom(음수)` · `TrySpend(음수)` 는 **`ArgumentOutOfRangeException`** 을 던진다. 조용히 통과시키면 매출이 줄어드는 경로가 생긴다
- 이벤트는 **값이 실제로 바뀔 때만** 발생시킨다. `AccrueFrom(100)` 이 `Balance` 를 10 올릴 때 1회
- `Random` 을 쓰지 않는다
- 로직은 `MonoBehaviour` 밖 순수 C#

### 완료 판정

- [ ] `grep -rn "class RevenueLedger\|class RecruitWallet" Assets/Code/Scripts/Runtime/Scoring/` — 정의 확인
- [ ] `grep -rn "using SushiDefense" Assets/Code/Scripts/Runtime/Scoring/` — **0건** (원시 타입만 받는다)
- [ ] `ls Assets/Code/Scripts/Runtime/Scoring/*.asmdef 2>/dev/null` — **없음** (새 어셈블리를 만들지 않았다)
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름

```
RevenueLedgerTests
  Add_SushiEaten_IncreasesByPrice                  ← 플랜 명시
  Add_MultipleSushi_AccumulatesTotal
  Add_NegativePrice_Throws
  Reset_AfterAdds_ReturnsToZero
  Add_Value_RaisesTotalChangedOnce

RecruitWalletTests
  AddRecruitCurrency_ScoreGained_AccruesOneTenth   ← 플랜 명시. 가격 100 → 재화 10
  AccrueFrom_PriceNotMultipleOfTen_TruncatesDown   ← 155 → 15
  Balance_NewWallet_EqualsInitialBudget
  TrySpend_SufficientBalance_DeductsAndReturnsTrue
  TrySpend_InsufficientBalance_ReturnsFalseAndKeepsBalance
  CanAfford_ExactBalance_ReturnsTrue
  Reset_AfterSpending_ReturnsToInitialBudget       ← 이월 안 함
```

`AccrueFrom_PriceNotMultipleOfTen_TruncatesDown` 이 **정수 규칙을 고정하는 회귀선**이다. 누군가 실수 누적으로 되돌리면 이 테스트가 먼저 깨진다.

### 예상 커밋 메시지

```
feat(scoring): add revenue ledger and recruit wallet with integer accrual
```

---

## 금지 사항

- **새 `.asmdef` 를 만들지 않는다.** `Scoring` 은 네임스페이스일 뿐이다 (`CLAUDE.md` §7 — 새 어셈블리는 사람 승인 사항)
- `CustomerPlacementService` 를 건드리지 않는다. 배치 연결은 step-08 이다
- `ClaimCoordinator` 를 건드리지 않는다. 배선은 step-09 다
- 스테이지 간 이월 로직을 만들지 않는다 (착수 시 확정 — 이월 안 함)
- 실수 누적·잔여분 이월을 넣지 않는다
