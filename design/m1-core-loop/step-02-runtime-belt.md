# Step 02: 벨트 진행 로직 — `SushiBelt`

- **영역:** `runtime` — 어셈블리 `Runtime` (+ `Tests.EditMode`)
- **선행 단계:** step-01 완료 필요 (`SpawnIntervalSeconds` · `BeltLength` · `BeltSpeed` · `SpawnTable`)
- **후행 단계:** step-06 `ClaimCoordinator` 가 이 벨트를 구동하고, step-07 `SushiBeltView` 가 이벤트를 구독한다
- **step-03 과 병렬 가능** — 파일이 겹치지 않는다

---

## 목적

초밥이 **시작점에서 일정 간격으로 나와**(Q2), 등속으로 흐르다, **끝점에 닿으면 풀로 돌아간다**(Q3). 이 단계가 M1 완료 판정의 앞 두 줄을 만든다.

전부 순수 C# 이다. `MonoBehaviour` 도, `Time.deltaTime` 도 여기 없다 — 시간은 인자로 받는다. 그래야 EditMode 에서 "3.7초 흘렸을 때"를 프레임 대기 없이 검증할 수 있다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD)

1. 타입·시그니처를 만든다 (`Tick` 본문은 비운다)
2. 테스트를 쓰고 **Red 확인**
3. 최소 구현으로 Green → 리팩터

### 생성 파일

```
Assets/Code/Scripts/Runtime/Belt/SushiBelt.cs               — 생성
Assets/Code/Scripts/Runtime/Belt/SushiItemFactory.cs        — 생성 (순수 팩토리, ISushiInstanceFactory<SushiItem>)
Assets/Code/Scripts/Runtime/Belt/SpawnSequence.cs           — 생성 (결정적 스폰 선택)
Assets/Tests/EditMode/Belt/SushiBeltTests.cs                — 생성
Assets/Tests/EditMode/Belt/SpawnSequenceTests.cs            — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Belt
{
    /// <summary>StageConfig.SpawnTable 을 결정적으로 소화한다. 난수를 쓰지 않는다 (작업서 D4).</summary>
    public sealed class SpawnSequence
    {
        public SpawnSequence(IReadOnlyList<SushiSpawnEntry> entries);

        public bool IsEmpty { get; }
        public SushiData Next();   // 가중치만큼 반복해 순환. 같은 입력 → 같은 순서
        public void Reset();
    }

    /// <summary>SushiPool 을 채울 순수 상태 객체 팩토리. Unity 오브젝트와 무관하다 (D2).</summary>
    public sealed class SushiItemFactory : ISushiInstanceFactory<SushiItem>
    {
        public SushiItem Create();
        public void Dispose(SushiItem instance);
    }

    /// <summary>
    /// 1차원 등속 벨트. 스폰 타이밍·위치 진행·끝점 판정만 갖는다.
    /// 시간은 인자로 받는다 — Time.deltaTime 을 읽지 않아야 EditMode 로 검증된다.
    /// </summary>
    public sealed class SushiBelt
    {
        public IReadOnlyList<SushiItem> ActiveSushi { get; }

        /// <summary>초밥이 시작점에 올라왔다. SushiBeltView 가 구독해 뷰를 빌린다 (D3).</summary>
        public event Action<SushiItem> SushiSpawned;

        /// <summary>초밥이 벨트에서 내려갔다 — 끝점 도달 또는 소비. 뷰를 반납할 시점이다.</summary>
        public event Action<SushiItem> SushiRemoved;

        public SushiBelt(StageConfig config, SequenceNumberIssuer sequenceNumbers,
                         SushiPool<SushiItem> pool);

        /// <summary>deltaSeconds 만큼 시간을 흘린다. 스폰·이동·끝점 반납이 여기서 일어난다.</summary>
        public void Tick(float deltaSeconds);

        /// <summary>손님이 먹어서 벨트에서 내린다. 끝점 도달과 같은 반납 경로를 탄다.</summary>
        public void Remove(SushiItem item);
    }
}
```

- **`Tick` 안에서 할당을 만들지 않는다** (`CLAUDE.md` §4.3 — WebGL). `ActiveSushi` 순회는 `for` + 인덱스로, LINQ·`foreach` 위 클로저·문자열 결합 금지. 제거는 뒤에서부터 훑는 swap-remove 나 별도 버퍼 재사용으로 처리한다
- 스폰 위치는 `0f`, 끝점은 `config.BeltLength`. 이동은 `position += config.BeltSpeed * deltaSeconds`
- `deltaSeconds` 가 스폰 간격보다 클 수 있다 — **한 틱에 여러 개가 스폰될 수 있어야 한다.** 남은 시간을 버리지 말고 누적한다 (버리면 저사양 기기에서 초밥이 덜 나온다)
- 순차번호는 `SequenceNumberIssuer.Next()` 로 받는다. **벨트가 직접 카운터를 들지 않는다** (M0 에서 세운 유일한 순서 출처)
- 풀 반납 시 `SushiItem.ResetForReuse(data, seqNo)` 로 상태를 씻는다 — M0 이 이 용도로 만든 메서드다

### 선행 산출물 의존성

- step-01 — `StageConfig.SpawnIntervalSeconds` · `BeltLength`
- M0 — `SushiItem` · `SushiPool<T>` · `ISushiInstanceFactory<T>` · `SequenceNumberIssuer` · `StageConfig.BeltSpeed` · `SpawnTable`

### 밸런스 수치

전부 `StageConfig` 에서 읽는다. **이 파일들에 숫자 상수를 쓰지 않는다** (`CLAUDE.md` §3.1).

### 테스트 항목 (`Tests.EditMode`)

```
SpawnSequenceTests
  Next_SingleEntry_AlwaysReturnsThatSushi
  Next_WeightedEntries_RepeatsByWeight
  Next_PastEnd_WrapsAround
  Next_SameSequenceTwice_ProducesIdenticalOrder      ← 결정성 회귀 방지 (D4)
  IsEmpty_NoEntries_ReturnsTrue

SushiBeltTests
  Tick_BeforeInterval_SpawnsNothing
  Tick_AtInterval_SpawnsOne
  Tick_LongerThanTwoIntervals_SpawnsTwo              ← 남은 시간을 버리지 않는다
  Tick_Spawned_AssignsIncreasingSequenceNumbers
  Tick_Spawned_RaisesSushiSpawned
  Tick_Advances_MovesByBeltSpeedTimesDelta
  Tick_ReachedBeltEnd_RemovesFromActive
  Tick_ReachedBeltEnd_ReturnsToPool                  ← 풀 CountInactive 로 확인
  Tick_ReachedBeltEnd_RaisesSushiRemoved
  Remove_ConsumedItem_ReturnsToPoolAndRaisesRemoved
  Tick_ReusedFromPool_HasCleanState                  ← ResetForReuse 가 먹혔는지
  Tick_SameConfigTwice_ProducesIdenticalPositions    ← 결정성
```

`StageConfig` 는 `ScriptableObject.CreateInstance` + `SerializedFieldSetter`(M0) 로 꾸민다.

### 제약

- **`MonoBehaviour` 금지.** 이 파일들에 `UnityEngine` 의존은 SO 타입 참조 외에 없어야 한다
- **`Time.deltaTime`·`Time.time` 을 읽지 않는다.** 시간은 `Tick(deltaSeconds)` 인자로만 들어온다
- **`Instantiate`/`Destroy` 금지.** 초밥 상태 객체는 `SushiPool<SushiItem>` 경유 (`CLAUDE.md` §3.4)
- **`Random` 금지** — 스폰 선택은 결정적이다 (D4)
- `Tick` 안에서 `new`·LINQ 금지 (§4.3)
- 물리 컴포넌트(`Rigidbody2D`·`Collider2D`)를 쓰지 않는다 (D5)
- `Runtime` 은 `Presentation` 을 참조하지 않는다 — 뷰에는 C# 이벤트로만 알린다 (D3)

### 완료 판정

- [ ] `grep -rn "class SushiBelt\|class SpawnSequence\|class SushiItemFactory" Assets/Code/Scripts/Runtime/Belt/` → 3건
- [ ] `grep -rn "MonoBehaviour\|Time\.deltaTime\|Time\.time\|Random\|Instantiate\|Destroy" Assets/Code/Scripts/Runtime/Belt/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건**
- [ ] `grep -rn "Rigidbody2D\|Collider2D" Assets/Code/Scripts/Runtime/` | 주석 제외 → 0건 (D5)
- [ ] `./tests/run-tests.sh` — 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(belt): add deterministic sushi belt with spawn and end-of-line return
```

---

## 금지 사항

- 집기 판정·배정을 여기 넣지 않는다. 벨트는 **누가 먹는지 모른다** (step-03~05).
- 뷰·프리팹을 만들지 않는다 (step-07).
- `SushiEatenEventChannelSO` 를 발행하지 않는다 — M1 에는 점수가 없다.
- 순환 벨트를 만들지 않는다. Q3 결정은 "끝점에서 사라지고 풀 반납"이다.
