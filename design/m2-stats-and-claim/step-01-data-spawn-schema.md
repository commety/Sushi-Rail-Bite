# Step 01: 스폰 스키마 정리 — α 추가 · `Weight` 제거 · 가격 하한

- **영역:** `data` — 어셈블리 `Runtime.Data`
- **선행 단계:** 없음 (M1 완료 상태에서 시작)
- **후행 단계:** step-02~10 전부. 스키마가 바뀌면 나머지가 전부 다시 컴파일된다

---

## 목적

M2 는 **share 를 가격에서 유도한다.** 그러려면 손으로 적는 가중치가 사라져야 하고, 희소성 지수 α 가 스테이지 필드로 들어와야 한다. 여기에 착수 시 확정한 "가격은 100 엔 이상" 불변조건을 더한다.

`SushiSpawnEntry.Weight` 를 남기면 안 되는 이유가 핵심이다 — share 가 가격에서 나오는데 손으로 적은 가중치가 함께 있으면 **가격과 어긋나는 두 번째 진실**이 된다. 둘이 갈라지는 순간 어느 쪽이 맞는지 판정할 방법이 없다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime.Data/StageConfig/StageConfig.cs` — 수정 (α 필드 추가)
- `Assets/Code/Scripts/Runtime.Data/StageConfig/SushiSpawnEntry.cs` — 수정 (`_weight` 제거)
- `Assets/Code/Scripts/Runtime.Data/SushiData/SushiData.cs` — 수정 (가격 하한 100)
- `Assets/Tests/EditMode/Data/StageConfigTests.cs` — 수정 (α 검증 추가)
- `Assets/Tests/EditMode/Data/SushiDataTests.cs` — 수정 (가격 하한 검증 추가)
- `Assets/Tests/EditMode/Data/SushiSpawnEntryTests.cs` — **생성** (덱 슬롯이 초밥 참조만 갖는다는 검증)
- `Assets/Tests/EditMode/Data/StageConfigBuilder.cs` — 수정 (`WithSpawnEntry` 시그니처)
- `Assets/Tests/PlayMode/StageConfigTestFactory.cs` — 수정 (`_weight` 쓰기 제거)
- `Assets/Code/Scripts/Runtime/Belt/SpawnSequence.cs` — **최소 수정** (`entry.Weight` 참조 제거 — 항목당 1회 배출. 진짜 교체는 step-03)
- `Assets/Tests/EditMode/Belt/SpawnSequenceTests.cs` — **최소 수정** (가중치 전제 테스트 2건. 전면 재작성은 step-03)

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    public sealed class StageConfig : ScriptableObject
    {
        /// 희소성 지수. share = (덱 내 최저가 / 가격) ^ α
        [SerializeField, Min(0f)] private float _sparsityExponent = 1f;
        public float SparsityExponent => _sparsityExponent;
    }

    [Serializable]
    public sealed class SushiSpawnEntry
    {
        [SerializeField] private SushiData _sushi;
        public SushiData Sushi => _sushi;
        // Weight 는 없다. share 는 가격에서 유도된다.
    }

    public sealed class SushiData : ScriptableObject
    {
        private const int MinimumPrice = 100;   // 구조 불변조건 — 밸런스 값이 아니다
    }
}
```

`StageConfigBuilder` 의 시그니처는 이렇게 좁아진다:

```csharp
public StageConfigBuilder WithSpawnEntry(SushiData sushi)   // int weight 인자 제거
```

### 선행 산출물 의존성

없음.

### 밸런스 수치

- `SparsityExponent` 의 **기본값 1.0** 은 플랜에서 확정된 값이다 (`docs/plan/M2-stats-and-claim.md` "share 계산 — 확정됨"). 애셋에서 사람이 조절한다
- `MinimumPrice = 100` 은 **밸런스 수치가 아니라 구조 불변조건**이다. `StageConfig.MinimumTimeLimitSeconds = 1f` 와 같은 성격 — "이 아래는 성립하지 않는다"는 경계다. 사람 판단 근거: *"초밥은 실제 엔 단위로 100 이상이다. 100 미만은 오류다"*
- **그 외 어떤 값도 채우지 않는다** (`CLAUDE.md` §7)

### 제약

- `[Min]` 은 인스펙터 입력만 막는다. 직렬화된 이상값·코드 대입을 잡으려면 `OnValidate()` 에서 한 번 더 조인다 — 기존 세 SO 가 이미 쓰는 패턴을 그대로 따른다
- α 는 **0 이 유효한 값**이다 (`α=0` → 모든 유형 share 균등). 하한만 막고 0 을 걸러내지 않는다
- `Runtime.Data` 는 다른 게임 어셈블리를 참조하지 않는다 (`.claude/rules/asmdef.md` §3)
- public 프로퍼티에 `///` XML 문서 주석. α 는 **왜 가격 비율만 쓰는지**(절대 스케일이 아니라서 100엔이든 1000엔이든 동일하게 동작한다)를 남긴다

### 주의 — 기존 테스트가 깨진다

가격 하한 100 을 넣으면 **낮은 가격으로 SO 를 만드는 기존 테스트가 깨진다.** `SerializedFieldSetter` → `OnValidate()` 경로를 타는 테스트를 전수 확인하고 값을 올린다.

```bash
grep -rn "_price" Assets/Tests/
```

**테스트를 `[Ignore]` 로 덮거나 지우지 않는다** (`.claude/rules/tests.md` §7). 값만 올린다.

`Assets/Level/Balance/Stage01.Placeholder.asset` 에 남는 `_weight: 1` 은 **건드리지 않는다** — 필드가 사라지면 Unity 가 다음 직렬화에서 조용히 버린다. 밸런스 애셋 편집은 §7 이라 지금 손대지 않는다.

### 완료 판정

- [ ] `grep -n "SparsityExponent" Assets/Code/Scripts/Runtime.Data/StageConfig/StageConfig.cs` — 필드·프로퍼티 확인
- [ ] `grep -rn --include='*.cs' "Weight" Assets/Code/Scripts/ Assets/Tests/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
      (`--include` 의 따옴표를 빼면 zsh 가 먼저 글롭을 시도해 `no matches found` 로 죽는다. `--include` 자체를 빼면 `.prefab` YAML 의 `m_BlendShapeWeights` 가 걸려 영원히 0 이 되지 않는다)

  현재(step-01 착수 전) 걸리는 4곳 — 이 단계가 전부 없애야 할 대상이다:

  | 파일 | 줄 |
  |---|---|
  | `Runtime.Data/StageConfig/SushiSpawnEntry.cs` | 21 |
  | `Runtime/Belt/SpawnSequence.cs` | 43 |
  | `Tests/EditMode/Belt/SpawnSequenceTests.cs` | 41, 61 |
- [ ] `grep -n "MinimumPrice" Assets/Code/Scripts/Runtime.Data/SushiData/SushiData.cs` — 상수·클램프 확인
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름

```
StageConfigTests
  OnValidate_NegativeSparsityExponent_ClampsToZero
  OnValidate_ZeroSparsityExponent_Kept          ← 0 은 유효하다
  SparsityExponent_Default_IsOne

SushiDataTests
  OnValidate_PriceBelowMinimum_ClampsToHundred
  OnValidate_PriceAtMinimum_Kept

SushiSpawnEntryTests
  Sushi_AssignedEntry_ExposesReference
```

### 예상 커밋 메시지

```
feat(stage): derive spawn share from price and drop hand-written weight
```

---

## 금지 사항

- `Runtime` · `Presentation` 어셈블리 파일을 수정하지 않는다. `SpawnSequence` 가 `Weight` 를 읽어 컴파일이 깨지면 **최소한으로만** 고친다 (예: 항목당 1회 배출) — 진짜 교체는 step-03 이다
- `Assets/Level/Balance/*.asset` 을 수정하지 않는다 (§7)
- α 의 기본값 1.0 을 다른 값으로 바꾸지 않는다. 플랜 확정 사항이다
