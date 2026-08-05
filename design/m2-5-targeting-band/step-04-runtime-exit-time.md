# Step 04: `CandidateSet` — 이탈 시각을 버리지 않고 보관한다

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** 없음 (step-01~03 과 **병렬 가능**. 파일이 겹치지 않는다)
- **후행 단계:** step-05 의 `ClaimDeadline` 이 이 시각을 마감시한으로 쓴다

---

## 목적

`ReachWindow.Solve` 는 이미 `ExitSeconds` 를 계산해 놓고 **버리고 있다** — `ClaimCoordinator.Schedule` 이 `EnterSeconds` 만 쓴다. 그걸 후보와 함께 보관한다.

**마감시한을 손님별 유예 시간(초) 필드로 두지 않는 이유가 여기 있다.** 마감은 그 (손님, 초밥) 쌍의 **기하** 로 결정된다 — 벨트가 1차원·등속이라 이미 답이 나와 있다. Reach 가 크면 자연히 길게, 벨트가 빠르면 짧게 스케일한다.

> 고정 길이 타이머는 쓸 수 없다. 유예 시계는 **손님 기준**이고 이탈은 **초밥 기준**이라 정렬되지 않는다. 통과 시간이 4초일 때 `t=3.5` 에 인식한 초밥은 `t=7.5` 에 나가는데 타이머는 `t=4.0` 에 만료된다.

---

## 확정된 결정 — D4 (래치를 이탈 기준으로)

**현재 밸런스에서는 마감시한이 절대 도달하지 않는다.**

| 값 | 출처 | 크기 |
|---|---|---|
| 범위 통과 시간 `2 × Reach / BeltSpeed` | `_reach: 3`, `_beltSpeed: 2` | **3.0초** |
| 인식 래치 상한 | `_recognitionLatchSeconds: 1` | **1.0초** |

`ExpireOlderThan` 이 **인식 시각 기준**(`now − recognizedAt > latch`)이라, 후보가 범위 안에 멀쩡히 있는데 1초 만에 후보에서 빠진다. 대역 밖 초밥을 기다리게 만들면 이탈 2초 전에 사라지고 손님은 굶는다.

**사람 판단으로 A 안이 확정됐다 (2026-08-05).** 아래는 그 결정과 기각된 대안의 기록이다.

| | **A — 채택** | B — 기각 |
|---|---|---|
| 만료식 | **`now − 이탈시각 > latch`** | 그대로 (`now − 인식시각`) |
| 애셋 | 안 바꿈 | `_recognitionLatchSeconds: 1 → 3.5+` (§7) |
| 불변식 | **밸런스와 무관하게 구조적으로 성립** | 값이 어긋나면 다시 굶는다 |
| 문서 | 래치 정의를 다시 쓴다 | `latch ≥ 2·maxReach/BeltSpeed` 제약을 어딘가 적어 둠 |
| 계획서 | *"래치는 손대지 않는다"* 와 **어긋난다** | 계획서 그대로 |

**A 의 근거**: §1.1-3c 가 정의한 래치는 *"물리적으로 범위를 조금 벗어나도 후보에서 빼지 않는다"* — **애초에 이탈 기준 개념이다.** 인식 기준 계산은 `latch ≥ 통과시간` 일 때만 그 의도와 일치하는 근사였다.

결정적으로, A 는 마감시한(`now ≥ 이탈`)이 만료(`now > 이탈 + latch`)보다 **항상 먼저 오게** 만든다. `latch > 0` 이면 두 시각 사이에 `latch` 만큼의 여유가 반드시 생기고, `latch == 0` 이면 만료 자체가 없다 (상한 없음 규약). 즉

> **불변식 "손님은 유한 시간 안에 반드시 집는다" 가 어떤 밸런스 값에서도 성립한다.**

B 는 이 성질을 `RecognitionLatchSeconds` 를 올바로 채워 넣는 사람에게 의존시킨다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/CandidateSet.cs` — 수정
- `Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — 수정 (`Schedule` 이 이탈 시각을 함께 예약)
- `Assets/Tests/EditMode/Customers/CandidateSetTests.cs` — 수정
- `Assets/Tests/EditMode/Customers/ClaimCoordinatorTests.cs` — 수정 (호출부·회귀)

### 핵심 심볼

```csharp
public sealed class CandidateSet
{
    public CandidateSet(float latchSeconds);

    /// <param name="exitAtSeconds">이 초밥이 범위를 벗어나는 <b>절대 시각</b>(경과 초).</param>
    public bool Recognize(SushiItem sushi, float exitAtSeconds);

    /// <summary>인식 순서의 <paramref name="index"/> 번째 후보가 범위를 벗어나는 시각.</summary>
    public float ExitAtOf(int index);

    /// <summary>이탈하고 래치 시간이 지난 후보를 정리한다. 상한이 0 이면 만료가 없다.</summary>
    public void ExpirePastLatch(float nowSeconds);
}
```

**세 가지가 바뀐다.**

1. `Recognize` 의 `nowSeconds` 인자가 **`exitAtSeconds` 로 대체된다.** 인식 시각은 더 이상 쓰이지 않으므로 보관하지 않는다 — 안 쓰는 값을 남기면 다음 사람이 옛 만료식을 되살린다
2. `ExpireOlderThan` → **`ExpirePastLatch` 로 이름이 바뀐다.** 의미가 바뀌었는데 이름이 같으면 호출자가 옛 뜻으로 읽는다
3. `ExitAtOf(int)` 가 는다. `Items` 와 **인덱스가 나란하다**

`ClaimCoordinator.PendingEntry` 에 이탈 시각이 하나 는다:

```csharp
private readonly struct PendingEntry
{
    public CustomerLogic Customer { get; }
    public SushiItem Sushi { get; }
    public float DueSeconds { get; }
    public float ExitAtSeconds { get; }   // 신규
}
```

`Schedule` 이 `ReachWindow` 의 두 값을 모두 절대 시각으로 바꿔 넣는다:

```csharp
_pending.Add(new PendingEntry(customer, sushi,
                              _elapsedSeconds + window.EnterSeconds,
                              _elapsedSeconds + window.ExitSeconds));
```

### 선행 산출물 의존성

- `SushiDefense.Customers.ReachWindow.ExitSeconds` — M1 부터 존재. **계산은 이미 있다. 쓰기만 하면 된다**

### 밸런스 수치

- 래치 상한은 `StageConfig.RecognitionLatchSeconds` 에서 읽는다. **코드 상수 0건**
- **`latch <= 0` = 상한 없음** 규약은 그대로 유지한다. "즉시 만료" 가 아니다 — 이걸 뒤집으면 마감시한과 같은 틱에 후보가 사라진다

### 제약

- **`ExpirePastLatch` 와 `Recognize` 만 바뀐다.** `Forget` · `Clear` · `Items` · `Count` 는 그대로
- **인식 순서를 유지한다.** swap-remove 를 쓰지 않는다 — 배정 입력이 결정적이어야 한다. `_items` 와 `_exitAt` 을 **같은 인덱스로 함께** 지운다
- **재인식은 이탈 시각을 갱신하지 않는다.** 이미 있는 초밥에 `Recognize` 가 다시 불려도 `false` 를 돌려주고 값을 두는 현재 동작을 유지한다 — 기하로 정해진 이탈 시각은 다시 계산해도 같아야 하고, 다르다면 그건 벨트 속도가 바뀌었다는 뜻이라 별개 문제다
- 틱 순서(`ClaimCoordinator.Tick` 6단계)를 **바꾸지 않는다.** 만료(4)는 배정(6)보다 앞에 그대로 둔다. D4 는 만료 *시점* 을 뒤로 미루므로 순서를 건드릴 필요가 없다
- `Update` 상당 경로에서 할당을 만들지 않는다. `List<float>` 을 하나 더 드는 것까지가 허용 범위이고, 배열을 매 틱 새로 만들지 않는다
- `Random` 을 쓰지 않는다
- `///` 문서 주석 — **래치의 뜻이 바뀌었음을 명시**한다. "인식하고 N초" 가 아니라 "범위를 벗어나고 N초"

### 완료 판정

- [ ] `grep -n "ExitAtOf\|ExpirePastLatch" Assets/Code/Scripts/Runtime/Customers/CandidateSet.cs` — 새 API 확인
- [ ] `grep -rn --include='*.cs' "ExpireOlderThan\|_recognizedAt" Assets/Code/Scripts/ Assets/Tests/` — **0건**
- [ ] `grep -n "ExitSeconds" Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — 예약에 실린다
- [ ] `grep -n "Tick(float deltaSeconds)" -A 25 Assets/Code/Scripts/Runtime/Customers/ClaimCoordinator.cs` — 6단계 주석·순서 그대로
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름

```
CandidateSetTests
  Recognize_NewSushi_StoresExitTime
  Recognize_SameSushiTwice_KeepsOriginalExitTime
  Recognize_MultipleSushi_ExitTimesStayIndexAligned      ← Items 와 ExitAtOf 의 정렬
  Forget_MiddleCandidate_KeepsRemainingExitTimesAligned  ← 제거가 두 리스트를 함께 당긴다
  ExpirePastLatch_StillInsideReach_KeepsCandidate        ← D4 의 본론. 통과시간 > latch 인 값으로 짠다
  ExpirePastLatch_JustAfterExit_KeepsCandidate           ← 이탈 직후는 래치가 붙든다
  ExpirePastLatch_BeyondExitPlusLatch_RemovesCandidate
  ExpirePastLatch_ZeroLatch_NeverExpires                 ← 0 = 상한 없음 규약

ClaimCoordinatorTests
  Schedule_SpawnedSushi_RecordsExitTimeFromReachWindow
  Tick_LatchShorterThanReachTraversal_KeepsCandidateUntilExit   ← 1초 latch / 3초 통과. 발견한 버그의 회귀선
```

> `ExpirePastLatch_StillInsideReach_KeepsCandidate` 를 **stage01 의 실제 비율(통과 3초 · 래치 1초)로** 짠다. 임의의 큰 래치로 짜면 D4 이전 구현으로도 통과해 버려 아무것도 증명하지 못한다.
>
> `Tick_LatchShorterThanReachTraversal_KeepsCandidateUntilExit` 는 **이 단계를 하게 만든 그 버그**를 조율자 층에서 잡는다. D4 를 되돌리면 이 테스트만 깨진다.

### 예상 커밋 메시지

```
fix(customer): latch recognition from reach exit instead of recognition time
```

**`fix` 인 이유**: 이탈 시각 보관은 새 기능이지만, 이 단계가 실제로 고치는 것은 **현재 밸런스에서 이미 발생하고 있는 결함**이다 — 범위 안 초밥이 인식 1초 뒤 후보에서 빠진다.

---

## 금지 사항

- **마감시한 판정을 여기에 넣지 않는다.** 이 단계는 **값을 나르기만** 한다. "지금 확정할까 기다릴까" 는 step-05 의 `ClaimDeadline` 이다
- 대역·가격을 참조하지 않는다. `CandidateSet` 은 M2.5 이후에도 **가격 타입을 모르는 클래스**로 남는다
- 손님별 유예 시간(초) SO 필드를 만들지 않는다. 계획서 완료 판정에 *"유예 시간 SO 필드가 **없다**"* 가 명시돼 있다
- `ReachWindow` 를 수정하지 않는다. `ExitSeconds` 는 M1 부터 이미 맞게 계산되고 있다
- 틱 6단계의 순서를 바꾸지 않는다. 순서를 바꿔야 할 것 같으면 멈추고 보고한다 — 그건 D4 가 틀렸다는 신호다
