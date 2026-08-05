# Step 06: `CustomerAppetiteMachine` — 먹는 시간 · 포화 · 소화

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** 없음 (M1 의 `CustomerRuntimeState` · `CustomerState` 만 있으면 된다)
- **후행 단계:** step-09 가 조율자에서 이 상태 머신을 돌린다

---

## 목적

M1 의 손님은 상태 필드만 있고 **전이가 없었다.** 배정이 곧 소비였다. M2 는 그 사이에 시간을 넣는다.

```
Idle ──BeginEating()──→ Eating ──eatSeconds 경과──→ (포화 미달) Idle
                                                  └→ (포화 도달) Digesting ──digestSeconds 경과──→ Idle
```

`CLAUDE.md` §3.5 는 전이 로직이 **테스트 가능한 순수 클래스**에 있기를 요구한다. 그 자리를 `CustomerLogic` 이 아니라 **별도 파일**로 잡는 것이 이 단계의 핵심 설계다 (작업서 D2).

### 왜 `CustomerLogic` 에 넣지 않는가

`CustomerLogic` 도 순수 클래스라 §3.5 는 만족한다. 하지만 여기에 먹는 로직을 넣으면 그 파일이 **초밥의 정적 데이터를 만지기 시작하고**, M1 이 세운 방어선이 무너진다:

> `CustomerLogic.cs` 에 `Price` · `Targeting` 이 등장하지 않는다 → 타겟팅이 자격 게이트로 샐 수 없다

별도 파일로 빼고 **`SushiItem` 을 받지 않는 시그니처**로 잠그면, 가격을 볼 수 있는 경로가 타입 수준에서 사라진다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/CustomerAppetiteMachine.cs` — **생성**
- `Assets/Tests/EditMode/Customers/CustomerAppetiteMachineTests.cs` — **생성**

`CustomerRuntimeState.cs` 는 **가급적 수정하지 않는다.** `State` · `CurrentSaturation` · `RemainingEatSeconds` · `RemainingDigestSeconds` 가 이미 `internal set` 이라 같은 어셈블리에서 쓸 수 있다.

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 1명의 상태 전이와 타이머. <b>초밥을 받지 않는다</b> — 포화도 기여량만 원시 값으로
    /// 받으므로 가격을 볼 수 있는 경로가 없다 (작업서 D2).
    /// </summary>
    public sealed class CustomerAppetiteMachine
    {
        public CustomerRuntimeState State { get; }

        /// <summary>먹기 시작한다. Idle 이 아니면 false 를 돌려주고 아무것도 바꾸지 않는다.</summary>
        public bool BeginEating(int saturationAmount);

        /// <summary>
        /// 시간을 흘린다. 먹기가 끝나면 <see cref="EatingFinished"/> 가 발생하고,
        /// 그 시점에 포화도가 오른다.
        /// </summary>
        public void Tick(float deltaSeconds);

        /// <summary>먹기가 끝났다 — 소비 확정 시점이다. 조율자가 매출·벨트 제거를 처리한다.</summary>
        public event Action EatingFinished;
    }
}
```

`BeginEating` 이 `saturationAmount` 를 받아 두고 **완료 시점에 반영**하는 것이 핵심이다. 시작할 때 포화도를 올리면 "먹는 도중 포화되어 상태가 꼬이는" 경계가 생긴다.

`eatSeconds` · `maxSaturation` · `digestSeconds` 는 `State.Data` 에서 읽는다 — 인자로 받지 않는다.

### 선행 산출물 의존성

- `SushiDefense.Customers.CustomerRuntimeState` — M1
- `SushiDefense.Customers.CustomerState` — M1 (`Idle` / `Eating` / `Digesting`)

### 밸런스 수치

- 먹는 시간·최대 포화도·소화 시간은 전부 `CustomerData` 에서 읽는다. **코드 상수 0건**
- `eatSeconds == 0` · `digestSeconds == 0` 은 유효한 값이다 (즉시 완료). 현재 placeholder 애셋이 그렇다 — 예외를 던지지 않는다

### 제약

- **한 번에 하나만 먹는다** (착수 시 확정, 열린 질문 4). `Eating` 중에 `BeginEating` 을 다시 부르면 `false`. 예약 큐를 만들지 않는다
- **`SushiItem` · `SushiData` 를 참조하지 않는다.** `using SushiDefense.Belt` 가 이 파일에 들어가면 D2 가 깨진 것이다
- **전이 순서를 지킨다** — 먹기 완료 → 포화도 증가 → 포화 판정. 순서를 뒤집으면 마지막 한 입이 반영되지 않는다
- `Tick` 안에서 할당하지 않는다 (`new` / LINQ 금지). 손님 수만큼 매 프레임 돈다
- 이벤트 구독은 조율자가 하고 **`Dispose` 에서 해제**한다 (`.claude/rules/scripts.md` §6) — 이 클래스는 이벤트를 **발행만** 한다
- `Random` 을 쓰지 않는다
- 로직은 `MonoBehaviour` 밖 순수 C#

### 완료 판정

- [ ] `grep -n "using" Assets/Code/Scripts/Runtime/Customers/CustomerAppetiteMachine.cs` — `SushiDefense.Belt` · `SushiDefense.Data` **둘 다 없음**
- [ ] `grep -rn "Price\|Targeting\|SushiItem" Assets/Code/Scripts/Runtime/Customers/CustomerAppetiteMachine.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] `git diff --stat Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs` — **변경 0줄**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름 (플랜 §테스트 항목 + 전이 조건)

```
먹는 시간
  Eat_BeforeDurationElapsed_StillEating
  Eat_DurationElapsed_RaisesEatingFinished
  Eat_DurationElapsed_ReturnsToIdleWhenHeadroomRemains
  BeginEating_WhileEating_ReturnsFalse             ← 한 번에 하나
  BeginEating_WhileDigesting_ReturnsFalse
  BeginEating_ZeroEatSeconds_FinishesOnNextTick

포화 · 소화
  Eat_SaturationReachesMax_EntersDigesting
  Digest_BeforeCooldown_StillDigesting
  Digest_AfterCooldown_ResetsSaturation            ← 플랜 명시
  Digest_AfterCooldown_ReturnsToIdle

경계
  Tick_Idle_DoesNothing
  Eat_SaturationExceedsMax_ClampedAtMax
```

`Eat_SaturationReachesMax_EntersDigesting` 과 `Digest_AfterCooldown_ResetsSaturation` 이 이 단계의 **자격 회복 경로**를 고정한다. 이 둘이 맞아야 `CustomerLogic.CanAcceptSushi` 가 다시 참이 된다.

### 예상 커밋 메시지

```
feat(customer): add appetite state machine for eating and digestion
```

---

## 금지 사항

- **`CustomerLogic.cs` 를 수정하지 않는다.** 이 단계에서 그 파일이 바뀌면 D2 의 의미가 사라진다
- `ClaimCoordinator` 를 건드리지 않는다. 배선은 step-09 다
- 예약 큐·동시 다중 섭취를 만들지 않는다 (착수 시 확정 사항)
- 먹는 속도·소화 속도 버프를 미리 넣지 않는다. 시너지는 M3 다
