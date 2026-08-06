# Step 05: `RunState` · `IRandomSource` — 런의 기억과 정당한 난수

- **영역:** `runtime` — 어셈블리 `Runtime` · **+ 하네스** (`tests/preflight.sh`)
- **선행 단계:** step-04 완료 필요 (`SushiDeck`)
- **후행 단계:** step-06 이 `RunState` 를 읽어 보상을 뽑고 되돌려 쓴다

---

## 목적

스테이지 하나가 끝나도 살아남는 것을 담는다 — **덱 · 손님 명부 · 몇 번째 스테이지인가**. 지금은 이 셋이 아무 데도 없다: 덱은 SO 에, 손님은 `StageBootstrap._defaultCustomer` 라는 인스펙터 필드 하나에 박혀 있다.

동시에 이 프로젝트에서 **처음으로 정당한 난수**를 들인다. 배정에는 여전히 난수가 없으므로, 그 경계를 코드가 아니라 **가드로** 지킨다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Run/CustomerDeck.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Run/RunState.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Run/IRandomSource.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Run/XorShiftRandomSource.cs` — **생성**
- `Assets/Tests/EditMode/Run/CustomerDeckTests.cs` — **생성**
- `Assets/Tests/EditMode/Run/RunStateTests.cs` — **생성**
- `Assets/Tests/EditMode/Run/XorShiftRandomSourceTests.cs` — **생성**
- `tests/preflight.sh` — 수정 (4번 가드 재조준)

### 핵심 심볼

```csharp
namespace SushiDefense.Run
{
    /// <summary>이 런이 배치할 수 있는 손님 명부. <c>SushiDeck</c> 과 같은 규칙으로 자란다.</summary>
    public sealed class CustomerDeck
    {
        public CustomerDeck();
        public CustomerDeck(IEnumerable<CustomerData> members);

        public IReadOnlyList<CustomerData> Members { get; }
        public int Count { get; }
        public bool Contains(CustomerData member);
        public bool TryAdd(CustomerData member);
    }

    /// <summary>
    /// 런 하나의 기억 — 스테이지가 끝나도 남는 것들.
    ///
    /// <para>
    /// <b>Unity API 를 모른다.</b> SO 참조를 담을 뿐이라, 나중에 저장이 필요해지면
    /// <c>SushiData.Id</c>·<c>CustomerData.Id</c> 로 매핑하는 계층만 얹으면 된다 (작업서 D10).
    /// 지금 Id 기반으로 짜지 않는다 — 쓰지 않을 간접층이다.
    /// </para>
    /// <para>
    /// <b><c>StageBootstrap.Build()</c> 바깥에 산다.</b> 재시도는 <c>Build()</c> 재호출이고,
    /// 원장·지갑·벨트·순차번호 발급기는 그때 새로 열리지만 이 객체는 살아남는다 (작업서 D6).
    /// </para>
    /// </summary>
    public sealed class RunState
    {
        public RunState(SushiDeck sushi, CustomerDeck customers, int seed);

        public SushiDeck Sushi { get; }
        public CustomerDeck Customers { get; }

        /// <summary>지금 도전 중인 스테이지 번호. 1 부터 시작한다.</summary>
        public int StageNumber { get; }

        /// <summary>지금 스테이지를 몇 번째 시도하고 있나. 처음이 1 이다.</summary>
        public int AttemptNumber { get; }

        /// <summary>보상 추첨에 쓰는 난수원. 런 내내 같은 인스턴스다.</summary>
        public IRandomSource Random { get; }

        /// <summary>클리어했다. 다음 스테이지로 넘어가고 시도 횟수를 1 로 되돌린다.</summary>
        public void AdvanceStage();

        /// <summary>실패했다. <b>같은 스테이지에 머물고</b> 시도 횟수만 올린다 (착수 시 확정).</summary>
        public void RecordFailedAttempt();
    }

    /// <summary>
    /// 결정적 난수원. <b>주입해서 쓴다</b> — <c>UnityEngine.Random</c>·<c>System.Random</c> 을
    /// 직접 부르면 시드를 잡을 수 없어 같은 런이 재현되지 않는다 (작업서 D5).
    /// </summary>
    public interface IRandomSource
    {
        /// <summary><c>[0, exclusiveMax)</c> 범위의 정수. <paramref name="exclusiveMax"/> 가 1 미만이면 예외.</summary>
        int Next(int exclusiveMax);
    }

    /// <summary>xorshift32. 외부 의존이 없고 시드가 같으면 수열이 같다.</summary>
    public sealed class XorShiftRandomSource : IRandomSource
    {
        public XorShiftRandomSource(int seed);
        public int Next(int exclusiveMax);
    }
}
```

### `RunState` 가 하지 않는 것

- **매출·영입 재화를 담지 않는다.** 둘은 스테이지마다 리셋된다 (M2 확정 — `.claude/domain/data-model.md` §4). 여기 넣으면 이월이 생긴 것처럼 읽힌다
- **`StageConfig` 를 담지 않는다.** 어느 스테이지 설정을 쓸지는 씬·부트스트랩의 문제다. `StageNumber` 만 든다
- **보상 후보를 담지 않는다.** 후보는 클리어 순간에 계산되는 일시적인 것이다 (step-06)

### xorshift 구현 주의

```csharp
// 시드 0 은 xorshift 에서 영원히 0 을 뱉는 고정점이다. 0 을 받으면 다른 값으로 바꾼다.
_state = seed != 0 ? (uint)seed : 0x9E3779B9u;
```

`Next(exclusiveMax)` 는 `(int)(_state % (uint)exclusiveMax)` 로 충분하다. 모듈로 편향이 있지만 **보상 후보는 많아야 수십 개**라 무시할 수 있는 크기이며, 편향 제거 루프를 넣으면 실행 횟수가 입력에 따라 달라져 검증이 오히려 어려워진다. 이 판단을 코드 주석에 남긴다.

### 하네스 — preflight 4번 가드 재조준

지금 가드는 `Runtime` 전역에서 `\b(UnityEngine\.)?Random\.` 을 찾는다. 두 문제가 있다:

1. **구멍**: `new System.Random()` 은 `Random.` 형태가 아니라 **통과한다.** 지금도 뚫려 있다
2. **이 단계 이후 오탐 위험**: 정당한 난수가 `Runtime/Run/` 에 생긴다

대역 산술 가드(5번)와 **같은 형태**로 바꾼다 — 정규식으로 금지형을 잡지 말고, **허용 목록** 밖에 난수가 있으면 FAIL.

```bash
# ── 4. 난수 격리 (rules/tests.md §5) ────────────────────────────────
# 배정은 순차번호가 모든 동률을 끝내므로 난수가 없다. 보상 추첨(M3)만 예외이고,
# 그것도 **주입된 IRandomSource** 를 거친다. 허용 목록은 Runtime/Run/ 한 곳이다.
RANDOM_HITS=$(grep -rnE '\bRandom\b' Assets/Code/Scripts/Runtime 2>/dev/null \
    | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)' \
    | grep -vE '^Assets/Code/Scripts/Runtime/Run/' || true)

# 전역 난수는 허용 목록 안에서도 금지다. 시드를 잡을 수 없어 런이 재현되지 않는다.
GLOBAL_RANDOM=$(grep -rnE '(UnityEngine|System)\.Random|new Random\(' \
    Assets/Code/Scripts/Runtime 2>/dev/null \
    | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)' || true)
```

두 개를 각각 `record` 한다 — `"배정 난수 금지"` / `"전역 난수 금지"`. 하나로 합치면 어느 쪽이 터졌는지 표에서 알 수 없다.

**가드가 죽어 있지 않음을 증명한다** (M2.5 에서 확립한 절차):

1. `ClaimPairComparer.cs` 에 `var x = new System.Random();` 을 임시로 넣고 → 두 가드 모두 FAIL 확인
2. `Runtime/Run/XorShiftRandomSource.cs` 에 같은 줄을 넣고 → `"전역 난수 금지"` 만 FAIL 확인 (허용 목록이 전역 난수까지 봐주지는 않는다)
3. 되돌리고 → 둘 다 PASS 확인
4. **무엇을 넣어 어떻게 터뜨렸는지 보고한다**

`XorShiftRandomSource` · `IRandomSource` 는 `\bRandom\b` 에 걸리지 않는다(`RandomSource` 는 `Random` 뒤에 `S` 가 붙어 단어 경계가 아니다). 그럼에도 허용 목록에 `Runtime/Run/` 을 넣는 이유는, 변수명 `Random` 이나 XML 주석 밖의 언급까지 매번 따지지 않기 위해서다.

### 선행 산출물 의존성

- `SushiDeck` — step-04

### 밸런스 수치

**없다.** 시드는 밸런스가 아니라 런마다 달라지는 입력이며 `StageBootstrap` 이 넘긴다 (step-07).

### 제약

- 로직은 `MonoBehaviour` 밖 순수 C#
- **`UnityEngine.Random` · `System.Random` 을 쓰지 않는다.** `Runtime/Run/` 안에서도 금지다
- `Runtime` → `Presentation` 참조 금지
- `RunState` 에 Unity API 를 넣지 않는다 (`Debug.Log` 포함)
- `SushiDeck` 을 수정하지 않는다 — step-04 에서 확정된 계약이다
- `StageBootstrap` 을 아직 건드리지 않는다. 조립은 step-07 이다
- `tests/preflight.sh` 외의 하네스 파일을 수정하지 않는다

### 완료 판정

- [ ] `grep -rn "UnityEngine\|Debug\." Assets/Code/Scripts/Runtime/Run/` — **0건**
- [ ] `grep -rn "System.Random\|UnityEngine.Random" Assets/Code/Scripts/Runtime/` — **0건**
- [ ] `git diff --stat Assets/Code/Scripts/Presentation/` — **0줄**
- [ ] 가드 주입 검증 3회를 실제로 돌리고 결과를 보고했다
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS — 표에 `배정 난수 금지` · `전역 난수 금지` 두 줄이 보인다

### 테스트 이름

```
CustomerDeckTests
  TryAdd_NewMember_AddsAndReturnsTrue
  TryAdd_DuplicateMember_ReturnsFalseAndKeepsCount
  TryAdd_Null_ReturnsFalse
  Members_MultipleAdds_PreservesInsertionOrder

RunStateTests
  StageNumber_NewRun_StartsAtOne
  AttemptNumber_NewRun_StartsAtOne
  AdvanceStage_AfterClear_IncrementsStageAndResetsAttempt
  RecordFailedAttempt_AfterFailure_KeepsStageAndIncrementsAttempt   ← ★ 착수 시 확정한 규칙
  Sushi_RewardAdded_IsVisibleInDeck
  Random_SameSeed_ProducesIdenticalRun                              ← 결정성

XorShiftRandomSourceTests
  Next_SameSeed_ProducesIdenticalSequence
  Next_DifferentSeeds_ProduceDifferentSequences
  Next_ZeroSeed_StillProducesVaryingValues        ← 0 고정점 방어
  Next_ExclusiveMaxOne_AlwaysReturnsZero
  Next_ExclusiveMaxZeroOrNegative_Throws
  Next_ManyDraws_StaysWithinRange
```

> **★ `RecordFailedAttempt_AfterFailure_KeepsStageAndIncrementsAttempt`** 가 "실패해도 런은 유지된다" 는 결정을 고정한다. **스테이지 번호가 그대로임을 함께 확인**한다 — 시도 횟수만 보면 런을 리셋하는 구현도 통과한다.
>
> `Next_DifferentSeeds_ProduceDifferentSequences` 는 **구체적인 두 시드**로 짜고, 첫 값만이 아니라 **앞 몇 개를 비교**한다. 한 값만 보면 우연히 같아 깜빡이는 테스트가 된다.
>
> `Random_SameSeed_ProducesIdenticalRun` 은 같은 시드로 만든 두 `RunState` 에서 `Random.Next` 를 여러 번 뽑아 수열이 같은지 본다 — 난수를 들이면서도 **결정성이 유지된다**는 것을 이 테스트가 지킨다.

### 예상 커밋 메시지

```
feat(run): add run state, customer deck, and an injectable random source
```

---

## 금지 사항

- **`UnityEngine.Random` · `System.Random` 을 쓰지 않는다.** 이 단계의 존재 이유 절반이 그 경계다
- 시드를 코드에 상수로 박지 않는다. 그러면 매 런이 같아져 주입한 의미가 사라진다
- `RunState` 에 매출·영입 재화를 넣지 않는다 (스테이지마다 리셋된다)
- 보상 생성·후보 추첨을 여기 넣지 않는다. step-06 이다
- 가드가 오탐한다고 허용 목록을 넓히지 않는다. 넓혀야 할 것 같으면 멈추고 보고한다
- `Presentation` 을 수정하지 않는다
