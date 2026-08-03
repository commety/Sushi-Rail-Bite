# Step 02: SO 스키마 3종 — `SushiData` · `CustomerData` · `StageConfig`

- **영역:** `data` — 어셈블리 `Runtime.Data` (+ `Tests.EditMode`)
- **선행 단계:** step-01 완료 필요 (`Runtime.Data.asmdef`, `Tests.EditMode.asmdef`)
- **후행 단계:** step-04 가 `SushiData`·`CustomerData` 를 런타임 상태에서 참조한다. M1 이후 전부가 이 계약 위에 선다

---

## 목적

밸런스 데이터의 **계약**을 세운다. 필드 이름과 제약(음수 금지 등)만 정하고 **값은 하나도 정하지 않는다** — 값은 M2·M3 에서 사람이 정한다 (`CLAUDE.md` §3.1 · §7). 이 단계 뒤로는 "가격이 코드에 박혀 있다" 같은 상황이 나올 수 없어야 한다.

필드 목록의 출처는 [`.claude/domain/data-model.md`](../../.claude/domain/data-model.md) §1~§3 이다. 🟢(원본 명시) 는 전부 넣고, 🔵(구현상 제안) 도 이번에 함께 넣는다 — 필드 추가는 싸고, 나중에 넣으면 애셋을 다시 만져야 한다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD — README D1)

1. 세 SO 타입을 **필드 + 읽기 전용 프로퍼티만** 만든다. 검증(`OnValidate`/클램프)은 아직 넣지 않는다
2. 검증 테스트를 쓰고 **Red 확인** (`./tests/run-tests.sh`)
3. `OnValidate` 클램프 + `[Min]`/`[Range]` 어트리뷰트로 Green

### 생성 파일

```
Assets/Code/Scripts/Runtime.Data/SushiData/SushiData.cs            — 생성
Assets/Code/Scripts/Runtime.Data/CustomerData/CustomerData.cs      — 생성
Assets/Code/Scripts/Runtime.Data/StageConfig/StageConfig.cs        — 생성
Assets/Code/Scripts/Runtime.Data/StageConfig/TableSlotDefinition.cs — 생성 (직렬화 가능한 struct/class)
Assets/Tests/EditMode/Data/SushiDataTests.cs                       — 생성
Assets/Tests/EditMode/Data/CustomerDataTests.cs                    — 생성
Assets/Tests/EditMode/Data/StageConfigTests.cs                     — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    [CreateAssetMenu(menuName = "SushiRailBite/Sushi Data")]
    public sealed class SushiData : ScriptableObject
    {
        public string Id { get; }               // 덱·저장 참조용 안정 키
        public string DisplayName { get; }
        public int Price { get; }               // [Min(0)] 매출 기여량이자 타겟팅 거리 기준
        public int SaturationAmount { get; }    // [Min(0)] 손님 포화도를 채우는 양
        public SushiTrait Trait { get; }        // 시너지 키 (M3). None 허용
        public Sprite Icon { get; }
    }

    /// <summary>초밥 특성 — 시너지 버프 발동 키 (M3 선택사항).</summary>
    public enum SushiTrait { None = 0 }         // 실제 항목은 M3 에서 추가

    [CreateAssetMenu(menuName = "SushiRailBite/Customer Data")]
    public sealed class CustomerData : ScriptableObject
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public CustomerKind Kind { get; }       // 기본/소식/먹보 — 표시·정렬용. 로직 분기 금지
        public float Reach { get; }             // [Min(0)] 벨트 위 집기 범위
        public int TargetingPrice { get; }      // [Min(0)] 선호 가격 (단일 값). 자격 게이트 아님
        public float EatSeconds { get; }        // [Min(0)] 초밥 1개 소비 시간
        public int MaxSaturation { get; }       // [Min(1)]
        public float DigestSeconds { get; }     // [Min(0)] 포화 후 휴식 길이
        public int RecruitCost { get; }         // [Min(0)] 배치 비용
        public Sprite Icon { get; }
    }

    public enum CustomerKind { Normal = 0, SmallEater = 1, BigEater = 2 }

    [CreateAssetMenu(menuName = "SushiRailBite/Stage Config")]
    public sealed class StageConfig : ScriptableObject
    {
        public int StageNumber { get; }
        public string DisplayName { get; }
        public int TargetRevenue { get; }         // [Min(1)] 목표 매출
        public float TimeLimitSeconds { get; }    // [Min(1)] 제한 시간
        public int MaxPlacedCustomers { get; }    // [Min(1)] 최대 배치 손님 수
        public int InitialRecruitBudget { get; }  // [Min(0)] 초기 영입 예산
        public float BeltSpeed { get; }           // [Min(0)]
        public IReadOnlyList<TableSlotDefinition> TableSlots { get; }
        public IReadOnlyList<SushiSpawnEntry> SpawnTable { get; }   // 어떤 초밥이 어떤 빈도로
        public IReadOnlyList<BonusObjective> BonusObjectives { get; }
    }
}
```

- `TableSlotDefinition` / `SushiSpawnEntry` / `BonusObjective` 는 `[Serializable]` 값 타입으로 **같은 폴더에** 둔다. 구성은 최소로: 자리 위치·인덱스 / `SushiData` + 가중치 / 설명 + 조건 플레이스홀더. **M3 에서 확장한다는 전제로 얇게 만든다**
- 실제 저장은 전부 `[SerializeField] private` 필드 + 위 프로퍼티. `public` 필드 금지
- `IReadOnlyList<T>` 노출 시 내부 `List<T>`/배열을 그대로 반환해도 되지만, **호출부가 수정할 수 없는 타입으로** 내보낸다

### 선행 산출물 의존성

- step-01 의 `Runtime.Data.asmdef` · `Tests.EditMode.asmdef`

### 밸런스 수치

**이 단계에서 정하는 값은 없다.** `.asset` 파일도 만들지 않는다 — 스키마(`.cs`)만이다. 밸런스 애셋 생성·수정은 `CLAUDE.md` §7 사람 판단 영역이다.

### 테스트 항목 (`Tests.EditMode`)

SO 는 전부 `ScriptableObject.CreateInstance<T>()` 로 만든다. **디스크의 `.asset` 을 로드하지 않는다** (`.claude/rules/tests.md` §4).

```
SushiDataTests
  OnValidate_NegativePrice_ClampsToZero
  OnValidate_NegativeSaturationAmount_ClampsToZero
  Trait_Default_IsNone

CustomerDataTests
  OnValidate_NegativeTargetingPrice_ClampsToZero
  OnValidate_ZeroMaxSaturation_ClampsToOne
  OnValidate_NegativeDigestSeconds_ClampsToZero
  OnValidate_NegativeReach_ClampsToZero

StageConfigTests
  OnValidate_ZeroTargetRevenue_ClampsToOne
  OnValidate_ZeroTimeLimit_ClampsToPositive
  OnValidate_ZeroMaxPlacedCustomers_ClampsToOne
  OnValidate_NegativeInitialBudget_ClampsToZero
```

> `[Min]` 어트리뷰트는 **인스펙터 입력만** 막는다. 코드로 넣은 값이나 직렬화된 이상값은 안 막히므로 `OnValidate` 에서도 클램프해야 테스트가 의미를 갖는다. 테스트는 리플렉션 대신 `OnValidate` 를 호출할 수 있는 최소 경로(예: `internal` 세터 + `InternalsVisibleTo`, 또는 `SerializedObject`)를 쓴다 — **테스트를 위해 `public` 세터를 열지 않는다** (`.claude/rules/tests.md` §7).

### 제약

- **타겟팅 필드 이름은 `TargetingPrice`.** `MaxEatablePrice`·`PreferredPriceLimit` 등 "제한" 뉘앙스 금지 (`.claude/rules/scriptable-object.md` §7)
- `Runtime.Data` 는 `Runtime`·`Presentation` 을 참조하지 않는다
- public API 전부에 `///` XML 문서 주석 (`CLAUDE.md` §4.4). `Runtime.Data` 는 나머지 전부가 읽는 계약이다
- 런타임 가변 상태(현재 포화도 등)를 **여기 넣지 않는다.** step-04 의 몫이다
- 순차번호(`SequenceNumber`)를 SO 에 넣지 않는다 — 런타임 발급 값이다

### 완료 판정

- [ ] `grep -rn "class SushiData\|class CustomerData\|class StageConfig" Assets/Code/Scripts/Runtime.Data/` → 3건
- [ ] `grep -rn "public .* Price\|public .* TargetingPrice" Assets/Code/Scripts/Runtime.Data/` 로 프로퍼티 노출 확인
- [ ] `grep -rn "public [A-Za-z<>]* [A-Za-z]*;" Assets/Code/Scripts/Runtime.Data/` → **public 필드 0건**
- [ ] `grep -rniE "MaxEatable|PriceLimit|EatablePrice" Assets/Code/Scripts/` → 0건
- [ ] `./tests/run-tests.sh` — 위 테스트 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(data): add sushi, customer and stage scriptable object schemas
```

---

## 금지 사항

- **밸런스 값을 정하지 않는다.** `.asset` 인스턴스를 만들지 않는다.
- `Runtime`/`Presentation` 폴더의 파일을 건드리지 않는다.
- 필드 이름을 임의로 줄이거나 바꾸지 않는다. 추가·변경이 필요하면 멈추고 아키텍트에게 보고한 뒤 이 작업서를 갱신한다.
