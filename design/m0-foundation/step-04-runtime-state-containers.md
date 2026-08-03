# Step 04: 런타임 상태 컨테이너 — `SushiItem` · `CustomerRuntimeState` · `SequenceNumberIssuer`

- **영역:** `runtime` — 어셈블리 `Runtime` (+ `Tests.EditMode`)
- **선행 단계:** step-01 완료 필요. step-02 의 `SushiData`·`CustomerData` **시그니처**에 의존한다 — 두 단계를 병렬로 돌린다면 step-02 의 타입명·프로퍼티명을 작업서 그대로 가정하고 쓴 뒤 머지 시 맞춘다
- **후행 단계:** M1 의 벨트·손님, M2 의 배정 로직이 이 타입 위에서 돈다

---

## 목적

**SO(정적 템플릿)와 런타임 가변 상태를 물리적으로 분리한다** (`CLAUDE.md` §3.1). 에디터에서 SO 필드를 런타임에 쓰면 플레이 종료 후에도 디스크에 남아 밸런스 애셋이 조용히 오염된다.

동시에 **순차번호 발급기**를 세운다. 이 프로젝트의 배정에는 난수가 없고, 모든 동률을 순차번호가 끝낸다 (`CLAUDE.md` §1.1-3b). 발급 지점이 여기 하나뿐이어야 결정성이 보장된다.

M0 에서는 **상태를 담는 그릇과 전이 뼈대만** 만든다. 자격 판정·배정·타이머 진행은 M1·M2 다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD — README D1)

1. 타입 · 프로퍼티 · 메서드 시그니처를 만든다 (본문은 최소)
2. 테스트를 쓰고 **Red 확인**
3. 최소 구현으로 Green

### 생성 파일

```
Assets/Code/Scripts/Runtime/Belt/SushiItem.cs                   — 생성
Assets/Code/Scripts/Runtime/Belt/SushiState.cs                  — 생성 (enum)
Assets/Code/Scripts/Runtime/Customers/CustomerRuntimeState.cs   — 생성
Assets/Code/Scripts/Runtime/Customers/CustomerState.cs          — 생성 (enum)
Assets/Code/Scripts/Runtime/SequenceNumberIssuer.cs             — 생성
Assets/Tests/EditMode/Belt/SushiItemTests.cs                    — 생성
Assets/Tests/EditMode/Customers/CustomerRuntimeStateTests.cs    — 생성
Assets/Tests/EditMode/SequenceNumberIssuerTests.cs              — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Belt
{
    /// <summary>벨트 위 초밥 1개의 런타임 상태. SushiData 는 읽기 전용 참조로만 갖는다.</summary>
    public sealed class SushiItem
    {
        public SushiData Data { get; }
        public int SequenceNumber { get; }      // 스폰 순서. 배정 타이브레이커
        public SushiState State { get; }
        public float BeltPosition { get; set; } // 벨트 위 진행도

        public SushiItem(SushiData data, int sequenceNumber);

        /// <summary>중복 배정 방지 — 이미 Claimed/Consumed 면 false 를 돌려주고 상태를 바꾸지 않는다.</summary>
        public bool TryClaim(int customerSequenceNumber);
        public bool TryConsume();
        public void ResetForReuse(SushiData data, int sequenceNumber);  // 풀 대여 시 (step-05)
    }

    public enum SushiState { OnBelt = 0, Claimed = 1, Consumed = 2 }
}

namespace SushiDefense.Customers
{
    /// <summary>손님 1명의 런타임 상태. CustomerData 는 읽기 전용 참조.</summary>
    public sealed class CustomerRuntimeState
    {
        public CustomerData Data { get; }
        public int SequenceNumber { get; }      // 배치 순서. 배정 타이브레이커
        public CustomerState State { get; }
        public int CurrentSaturation { get; }
        public float RemainingEatSeconds { get; }
        public float RemainingDigestSeconds { get; }

        public CustomerRuntimeState(CustomerData data, int sequenceNumber);

        public bool HasSaturationHeadroom { get; }   // 자격 판정의 한 축 (§1.1-3a)
    }

    public enum CustomerState { Idle = 0, Eating = 1, Digesting = 2 }
}

namespace SushiDefense
{
    /// <summary>
    /// 순차번호 발급기. 배정에 난수를 쓰지 않기 위한 유일한 순서 출처 (CLAUDE.md §1.1-3b).
    /// 인스턴스 단위라 스코프(스테이지 리셋 / 런 연속)는 소유자가 정한다 — 플랜 Q8 미결.
    /// </summary>
    public sealed class SequenceNumberIssuer
    {
        public int Next();      // 0 부터 1씩 증가
        public void Reset();
    }
}
```

- **`static` 카운터를 쓰지 않는다.** 인스턴스로 두면 Q8(스테이지 리셋 vs 런 연속)이 어느 쪽으로 결정돼도 코드를 안 고친다. `static` 을 쓰면 RULE-01 관련 초기화 메서드도 따라붙는다
- `CustomerRuntimeState` 의 **상태 전이 로직 자체는 이 단계에 넣지 않는다.** M2 의 `CustomerLogic` 이 가져간다. 여기서는 값 보관 + `HasSaturationHeadroom` 같은 파생 프로퍼티까지

### 선행 산출물 의존성

- step-02 — `SushiDefense.Data.SushiData`, `SushiDefense.Data.CustomerData`

### 밸런스 수치

없음. 최대 포화도·소화 시간은 전부 `CustomerData` 에서 읽는다. **이 파일들에 숫자 상수를 쓰지 않는다** (`CLAUDE.md` §3.1).

### 테스트 항목 (`Tests.EditMode`)

```
SequenceNumberIssuerTests
  Next_CalledRepeatedly_IncrementsByOne
  Next_TwoIssuers_AreIndependent
  Reset_AfterIssuing_RestartsFromZero

SushiItemTests
  TryClaim_OnBelt_TransitionsToClaimed
  TryClaim_AlreadyClaimed_ReturnsFalseAndKeepsState     ← 두 손님이 같은 초밥을 먹는 버그 방지
  TryConsume_Claimed_TransitionsToConsumed
  TryConsume_OnBelt_ReturnsFalse
  ResetForReuse_ConsumedItem_ReturnsToOnBelt

CustomerRuntimeStateTests
  Ctor_NewCustomer_StartsIdleWithZeroSaturation
  HasSaturationHeadroom_BelowMax_ReturnsTrue
  HasSaturationHeadroom_AtMax_ReturnsFalse
```

SO 는 `ScriptableObject.CreateInstance<T>()` 로 만든다.

### 제약

- **`MonoBehaviour` 를 만들지 않는다.** 전부 순수 C# (`CLAUDE.md` §3.2) — 이게 EditMode 테스트의 전제다
- SO 필드에 런타임 값을 쓰지 않는다. `Data` 는 읽기만
- **배정·자격 판정 로직을 여기 넣지 않는다.** 특히 `TargetingPrice` 를 이 단계에서 쓰면 안 된다 — 배정은 M2 의 `SushiClaimResolver` 몫이다
- 프로덕션 코드에 `Random` 금지 (`.claude/rules/tests.md` §5)
- `Runtime` 은 `Presentation` 을 참조하지 않는다
- public API 에 `///` XML 문서 주석

### 완료 판정

- [ ] `grep -rn "class SushiItem\|class CustomerRuntimeState\|class SequenceNumberIssuer" Assets/Code/Scripts/Runtime/` → 3건
아래 세 검사에는 **주석 제외 필터**를 붙인다 ([README D7](README.md)). 이 단계의 `///` 주석은 "`MonoBehaviour` 밖에 둔다", "난수(`Random`)를 쓰지 않는다", "`static` 카운터를 쓰지 않는다" 처럼 **금지 심볼 이름을 그대로 적게 되어 있어**, 필터가 없으면 규칙을 지킨 코드가 전부 걸린다.

- [ ] `grep -rn "MonoBehaviour" Assets/Code/Scripts/Runtime/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건**
- [ ] `grep -rn "Random" Assets/Code/Scripts/Runtime/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건**
- [ ] `grep -n "static" Assets/Code/Scripts/Runtime/SequenceNumberIssuer.cs | grep -vE '^[0-9]+:[[:space:]]*(///|//|\*)'` → 0건
      > 단일 파일 검사라 출력이 `줄번호:내용` 이다 — 경로 부분이 없으므로 필터 모양이 다르다.
- [ ] `./tests/run-tests.sh` — 위 테스트 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(runtime): add sushi and customer runtime state containers
```

---

## 금지 사항

- 자격 판정(`CanTake`)·배정(`Resolve`)을 구현하지 않는다. M2 의 범위다.
- `TargetingPrice` 를 자격 게이트로 쓰는 코드를 만들지 않는다 (`CLAUDE.md` §1.1-3a — 이 프로젝트에서 가장 자주 틀리는 지점).
- `Runtime.Data` 의 파일을 수정하지 않는다. 필드가 모자라면 멈추고 보고한다.
