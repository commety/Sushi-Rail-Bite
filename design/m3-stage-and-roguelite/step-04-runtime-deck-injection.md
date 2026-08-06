# Step 04: `SushiDeck` — 벨트가 덱을 주입받는다

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** 없음. step-01 · step-02 와 **병렬 가능**
- **후행 단계:** step-05 의 `RunState` 가 이 `SushiDeck` 을 들고, step-07 이 그것을 벨트에 넘긴다

---

## 목적

지금 `SushiBelt` 는 생성자에서 `config.SpawnTable` 을 직접 읽는다:

```csharp
_spawnSequence = new SpawnSequence(config.SpawnTable, config.SparsityExponent);
```

이대로 두면 **보상으로 얻은 초밥이 벨트에 영영 나오지 않는다.** 덱의 진실이 `StageConfig`(읽기 전용 SO)에 있는 한, 런 중에 자라는 덱을 표현할 방법이 없다.

`StageConfig.SpawnTable` 을 **런의 시작 덱**으로 격하하고, 벨트는 주입받은 덱을 읽게 한다.

**이 단계는 동작을 바꾸지 않는다.** 부트스트랩이 여전히 `config.SpawnTable` 에서 만든 덱을 넘기므로, 스폰 결과가 한 개도 달라지지 않아야 한다. 그래야 다음 단계에서 무언가 달라졌을 때 **원인이 이 리팩터가 아님이 확실해진다.**

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Run/SushiDeck.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Belt/SpawnShareTable.cs` — 수정 (생성자 인자 타입)
- `Assets/Code/Scripts/Runtime/Belt/SpawnSequence.cs` — 수정 (생성자 인자 타입)
- `Assets/Code/Scripts/Runtime/Belt/SushiBelt.cs` — 수정 (생성자에 덱 추가)
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (덱을 만들어 넘긴다)
- `Assets/Tests/EditMode/Run/SushiDeckTests.cs` — **생성**
- `Assets/Tests/EditMode/Belt/SpawnShareTableTests.cs` · `SpawnSequenceTests.cs` · `SushiBeltTests.cs` — 수정 (헬퍼)

### 핵심 심볼

```csharp
namespace SushiDefense.Run
{
    /// <summary>
    /// 이 런이 들고 다니는 초밥 덱. <b>런 중에 자란다</b> — 클리어 보상이 카드를 더한다.
    ///
    /// <para>
    /// <c>StageConfig.SpawnTable</c> 은 이 덱의 <b>시작 상태</b>일 뿐이다. 벨트가 SO 를 직접
    /// 읽으면 보상이 반영될 자리가 없다 (작업서 D4).
    /// </para>
    /// </summary>
    public sealed class SushiDeck
    {
        public SushiDeck();
        public SushiDeck(IEnumerable<SushiData> cards);

        /// <summary>덱에 든 카드. <b>추가 순서를 유지한다</b> — 스폰 동률이 덱 순서로 갈린다.</summary>
        public IReadOnlyList<SushiData> Cards { get; }

        public int Count { get; }
        public bool Contains(SushiData card);

        /// <summary>이미 있거나 <c>null</c> 이면 <c>false</c> 를 돌려주고 덱을 그대로 둔다.</summary>
        public bool TryAdd(SushiData card);

        /// <summary><c>StageConfig</c> 의 시작 덱에서 만든다. 빈 슬롯(<c>null</c>)은 빠진다.</summary>
        public static SushiDeck FromSpawnTable(StageConfig config);
    }
}
```

**순서 유지가 계약이다.** `SpawnSequence` 는 credit 동률을 **덱 순서**로 끊으므로(`SpawnSequence` 클래스 주석), 순서가 흔들리면 스폰 결과가 흔들린다. `HashSet` 으로 갈아타지 않는다 — 중복 검사는 `List.Contains` 로 충분하다 (덱은 수십 장 규모다).

### 시그니처 변경

```csharp
// SpawnShareTable
- public SpawnShareTable(IReadOnlyList<SushiSpawnEntry> deck, float sparsityExponent)
+ public SpawnShareTable(IReadOnlyList<SushiData> deck, float sparsityExponent)

// SpawnSequence
- public SpawnSequence(IReadOnlyList<SushiSpawnEntry> entries, float sparsityExponent)
+ public SpawnSequence(IReadOnlyList<SushiData> deck, float sparsityExponent)

// SushiBelt
- public SushiBelt(StageConfig config, SequenceNumberIssuer sequenceNumbers, SushiPool<SushiItem> pool)
+ public SushiBelt(StageConfig config, IReadOnlyList<SushiData> deck,
+                  SequenceNumberIssuer sequenceNumbers, SushiPool<SushiItem> pool)
```

`SpawnShareTable.Collect` 가 지금 `entry?.Sushi` 형태로 두 겹을 벗기고 있다면 한 겹으로 줄어든다. **`null` 항목을 거르는 동작은 유지한다** — 인스펙터에서 슬롯을 늘리면 자연히 생기는 상태다.

`StageConfig` 는 **건드리지 않는다.** `SpawnTable` 도 `SushiSpawnEntry` 도 그대로 남는다 — 인스펙터에서 덱을 짜는 형식이며, 그 래퍼가 존재하는 이유(슬롯이라는 의미)는 여전히 유효하다. 벗기는 일만 `Runtime` 으로 내려온다.

`StageBootstrap.Build()` 는 이렇게 바뀐다:

```csharp
Belt = new SushiBelt(_stageConfig, SushiDeck.FromSpawnTable(_stageConfig).Cards,
                     new SequenceNumberIssuer(), new SushiPool<SushiItem>(new SushiItemFactory()));
```

step-07 에서 이 자리가 `_runState.SushiDeck.Cards` 로 바뀐다.

### 테스트 헬퍼 정리

`SpawnSequenceTests` · `SpawnShareTableTests` 의 `BuildDeck(params int[] prices)` 는 지금 `StageConfig` 를 만들어 `SerializedFieldSetter` 로 `_spawnTable` 을 채우고 `_config.SpawnTable` 을 돌려준다. **이제 `SushiData` 리스트를 바로 만들면 되므로 `StageConfig` 경유가 사라진다.**

이 정리가 이 단계의 부수 이익이다 — 스폰 테스트가 더 이상 스테이지 SO 를 세우지 않는다.

### 선행 산출물 의존성

없음.

### 밸런스 수치

**없다.** 값의 변화가 0인 것이 이 단계의 완료 조건이다.

### 제약

- **동작 변화 0.** 기존 테스트를 **기대값 수정 없이** 전량 통과시킨다. 어떤 테스트의 `Assert` 기대값이라도 바꿔야 한다면 멈추고 보고한다 — 리팩터가 아니라 동작 변경이라는 뜻이다
- `StageConfig.cs` 와 `SushiSpawnEntry.cs` 를 **수정하지 않는다**
- `SushiDeck` 은 `Runtime.Data` 를 참조해도 되지만 그 반대는 금지 (asmdef 규칙 §3)
- `Random` 을 쓰지 않는다
- 로직은 `MonoBehaviour` 밖 순수 C#
- `SushiDeck` 에 "제거" 를 만들지 않는다. 덱에서 카드가 빠지는 기획이 없다

### 완료 판정

- [ ] `git diff --stat Assets/Code/Scripts/Runtime.Data/` — **0줄**
- [ ] `grep -rn "SpawnTable" Assets/Code/Scripts/Runtime/` — **0건** (벨트가 SO 의 덱을 직접 읽지 않는다)
- [ ] `grep -rn "SushiSpawnEntry" Assets/Code/Scripts/Runtime/` — `SushiDeck.FromSpawnTable` **한 곳뿐**
- [ ] `git diff` 에서 테스트의 **기대값(`Assert` 인자)이 바뀐 곳이 0건** — 헬퍼·생성자 호출만 바뀐다
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] PlayMode Green — `./tests/run-tests.sh all` (`Stage01SceneTests` 가 실제 씬으로 스폰을 확인한다)
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름

```
SushiDeckTests
  TryAdd_NewCard_AddsAndReturnsTrue
  TryAdd_DuplicateCard_ReturnsFalseAndKeepsCount
  TryAdd_Null_ReturnsFalse
  Cards_MultipleAdds_PreservesInsertionOrder        ← 스폰 동률이 이 순서로 갈린다
  FromSpawnTable_ConfigWithDeck_CopiesEveryCard
  FromSpawnTable_EntryWithNullSushi_SkipsIt
  FromSpawnTable_TwiceFromSameConfig_ProducesEqualDecks
  FromSpawnTable_MutatingDeck_DoesNotTouchConfig    ← 덱은 SO 의 사본이다
```

> `Cards_MultipleAdds_PreservesInsertionOrder` 는 **서로 다른 카드 3장**으로 짠다. 2장이면 우연히 맞을 여지가 남는다.
>
> `FromSpawnTable_MutatingDeck_DoesNotTouchConfig` 가 **런타임이 SO 를 오염시키지 않는다**(`.claude/rules/scriptable-object.md` §2)를 이 경로에서 고정한다.

### 예상 커밋 메시지

```
refactor(belt): read the deck from an injected list instead of the stage config
```

---

## 금지 사항

- **동작을 바꾸지 않는다.** 스폰 순서·비율이 한 개라도 달라지면 이 단계의 목적이 사라진다
- `StageConfig` · `SushiSpawnEntry` 를 수정하지 않는다
- `RunState` 를 만들지 않는다. step-05 다
- `CustomerDeck` 을 만들지 않는다. step-05 다
- 테스트를 지우거나 `[Ignore]` 로 덮지 않는다. 깨지면 원인을 보고한다 (`.claude/rules/tests.md` §7)
