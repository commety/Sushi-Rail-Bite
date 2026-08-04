# Step 04: 범위 진입 계산 + 후보 래치 — `ReachWindow` · `CandidateSet`

- **영역:** `runtime` — 어셈블리 `Runtime` (+ `Tests.EditMode`)
- **선행 단계:** step-03 완료 필요 (`CustomerLogic`), step-01 (`RecognitionLatchSeconds`)
- **후행 단계:** step-05 가 후보 집합에서 쌍을 만들고, step-06 이 진입/제거 이벤트를 여기에 흘려 넣는다

---

## 목적

**매 프레임 손님 × 초밥을 전수 순회하지 않는다.** 그 구조는 WebGL 프레임 예산을 그대로 먹고, M1 완료 판정이 명시적으로 금지한다.

벨트는 1차원·등속이라 초밥이 손님 범위에 **들어오는 시각을 계산으로** 얻을 수 있다 ([`sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §4 에서 채택된 결정). 물리 엔진도, 트리거 콜백도 쓰지 않는다.

그리고 **한 번 인식된 초밥은 범위를 벗어나도 후보에 남는다**(래치). 다만 이번 마일스톤의 결정으로 **시간 상한이 붙는다**(Q9) — 인식 후 `RecognitionLatchSeconds` 가 지나면 후보에서 빠진다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD)

1. 타입·시그니처를 만든다 (본문 최소)
2. 테스트를 쓰고 **Red 확인**
3. 구현 → 리팩터

### 생성 파일

```
Assets/Code/Scripts/Runtime/Customers/ReachWindow.cs        — 생성
Assets/Code/Scripts/Runtime/Customers/CandidateSet.cs       — 생성
Assets/Tests/EditMode/Customers/ReachWindowTests.cs         — 생성
Assets/Tests/EditMode/Customers/CandidateSetTests.cs        — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>
    /// 등속 1차원 벨트에서 초밥이 손님 범위에 들어오고 나가는 <b>시각</b>을 계산한다.
    /// 물리 엔진도 트리거 콜백도 쓰지 않는다 (작업서 D5).
    /// </summary>
    public readonly struct ReachWindow
    {
        public float EnterSeconds { get; }   // 지금부터 진입까지 남은 시간. 이미 안이면 0
        public float ExitSeconds { get; }    // 지금부터 이탈까지 남은 시간
        public bool WillEverEnter { get; }   // 이미 지나갔거나 속도가 0 이면 false

        /// <summary>현재 위치·속도·범위로 진입/이탈 시각을 푼다.</summary>
        public static ReachWindow Solve(float sushiPosition, float beltSpeed,
                                        float reachMin, float reachMax);
    }

    /// <summary>
    /// 한 손님이 인식한 초밥들. <b>인식은 래치된다</b> — 범위를 벗어나도 남는다 (§1.1-3c).
    /// 다만 인식 후 latchSeconds 가 지나면 만료된다 (Q9 결정). latchSeconds 가 0 이면 상한 없음.
    /// </summary>
    public sealed class CandidateSet
    {
        public int Count { get; }
        public IReadOnlyList<SushiItem> Items { get; }

        public CandidateSet(float latchSeconds);

        /// <summary>범위 진입 시 후보로 등록한다. 이미 있으면 인식 시각을 <b>갱신하지 않는다</b>.</summary>
        public bool Recognize(SushiItem sushi, float nowSeconds);

        /// <summary>소비·벨트 제거 시 후보에서 뺀다.</summary>
        public bool Forget(SushiItem sushi);

        /// <summary>손님이 자격을 잃었을 때 전부 비운다.</summary>
        public void Clear();

        /// <summary>래치 시간이 지난 후보를 정리한다. 상한이 0 이면 아무것도 만료되지 않는다.</summary>
        public void ExpireOlderThan(float nowSeconds);
    }
}
```

- `Recognize` 가 **인식 시각을 갱신하지 않는** 이유: 갱신하면 범위 안에 오래 머무는 초밥의 래치가 계속 연장되어 상한이 무의미해진다. 인식은 한 번뿐이다
- `Items` 순서는 **등록 순서를 유지**한다. step-05 의 정렬이 안정적으로 돌려면 입력 순서가 결정적이어야 한다
- `ExpireOlderThan` 은 뒤에서부터 훑어 swap-remove 한다. **할당을 만들지 않는다**
- 후보에서 빠지는 경우는 넷뿐이다 — 소비됨 / 벨트에서 제거됨 / 손님 자격 상실 / **래치 만료**(신규). 물리적으로 범위를 벗어난 것만으로는 빠지지 않는다

### 선행 산출물 의존성

- step-01 — `StageConfig.RecognitionLatchSeconds`
- step-03 — `CustomerLogic` (자격 상실 판정의 출처)
- M0 — `SushiItem`

### 밸런스 수치

`latchSeconds` 는 호출자가 `StageConfig.RecognitionLatchSeconds` 를 읽어 넘긴다. **`CandidateSet` 안에 기본값 상수를 두지 않는다.**

### 테스트 항목 (`Tests.EditMode`)

```
ReachWindowTests
  Solve_SushiAlreadyInside_EnterSecondsIsZero
  Solve_SushiApproaching_ReturnsPositiveEnterSeconds
  Solve_SushiAlreadyPassed_WillEverEnterIsFalse
  Solve_ZeroBeltSpeed_WillEverEnterIsFalse           ← 0 나눗셈 방어
  Solve_ExitAfterEnter_Always                        ← ExitSeconds > EnterSeconds 불변식
  Solve_SameInputTwice_ProducesIdenticalWindow

CandidateSetTests
  Recognize_NewSushi_AddsToCandidates                 ← OnEnter_SushiEntersReach_AddsToCandidates (플랜)
  Recognize_SameSushiTwice_DoesNotDuplicate
  Recognize_SameSushiTwice_KeepsOriginalRecognitionTime  ← 래치 연장 금지
  Forget_EatenByOther_RemovesFromCandidates           ← OnTaken_... (플랜)
  Forget_UnknownSushi_ReturnsFalse
  Clear_LostEligibility_EmptiesSet
  Items_PreservesRecognitionOrder

  ExpireOlderThan_WithinLatchWindow_KeepsCandidate
  ExpireOlderThan_BeyondLatchWindow_RemovesCandidate  ← Q9 시간 상한
  ExpireOlderThan_ZeroLatch_NeverExpires              ← 0 = 상한 없음 규약
  Recognize_SushiPastReachEdge_StaysCandidate         ← 래치 본질: 범위 이탈로는 안 빠진다
```

### 제약

- **`MonoBehaviour` 금지.** 트리거 콜백(`OnTriggerEnter2D` 등)을 쓰지 않는다
- **물리 컴포넌트 금지** (`Rigidbody2D`·`Collider2D`) — D5
- **`Time.*` 를 읽지 않는다.** 현재 시각은 `nowSeconds` 인자로 들어온다
- **매 프레임 전수 순회를 만들지 않는다.** 이 단계의 존재 이유다
- `Recognize`/`Forget`/`ExpireOlderThan` 경로에서 할당 금지 (§4.3)
- 배정을 여기서 하지 않는다 (step-05). `CandidateSet` 은 "누가 이길지" 를 모른다
- `Runtime` 은 `Presentation` 을 참조하지 않는다

### 완료 판정

- [ ] `grep -rn "struct ReachWindow\|class CandidateSet" Assets/Code/Scripts/Runtime/Customers/` → 2건
- [ ] `grep -rn "OnTrigger\|Rigidbody2D\|Collider2D\|MonoBehaviour\|Time\." Assets/Code/Scripts/Runtime/Customers/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건**
- [ ] `./tests/run-tests.sh` — 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(customer): add reach window calculation and latched candidate set
```

---

## 금지 사항

- 물리 트리거로 진입을 감지하지 않는다. 채택된 구현은 구간 계산이다 (D5).
- 범위를 벗어났다는 이유로 후보를 빼지 않는다. 빠지는 경우는 위 넷뿐이다.
- 배정·정렬을 넣지 않는다 (step-05).
