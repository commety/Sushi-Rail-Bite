# Step 01: `RewardCatalog` — 보상 풀의 스키마

- **영역:** `data` — 어셈블리 `Runtime.Data`
- **선행 단계:** 없음. step-02 · step-04 와 **병렬 가능**
- **후행 단계:** step-06 이 이 SO 에서 후보를 뽑는다

---

## 목적

"클리어하면 무엇을 줄 수 있나" 의 목록을 데이터로 정의한다. 보상 후보를 코드에 적으면 카드를 추가할 때마다 코드를 고쳐야 하고, 그건 이 프로젝트가 명시적으로 금지한 형태다 (`CLAUDE.md` §3.1).

**애셋(값)은 만들지 않는다.** 스키마와 검증만 만든다 — 어떤 카드를 풀에 넣을지는 §7 사람 판단이며 step-09 다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime.Data/RewardCatalog/RewardCatalog.cs` — **생성**
- `Assets/Tests/EditMode/Data/RewardCatalogTests.cs` — **생성**

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    /// <summary>
    /// 클리어 보상으로 제시할 수 있는 카드의 전체 목록. <b>런의 진행 상태를 담지 않는다</b> —
    /// 무엇을 이미 가졌는지는 런타임의 <c>RunState</c> 가 안다 (<c>CLAUDE.md</c> §3.1).
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Reward Catalog", fileName = "RewardCatalog")]
    public sealed class RewardCatalog : ScriptableObject
    {
        /// <summary>보상으로 줄 수 있는 초밥 카드 전체.</summary>
        public IReadOnlyList<SushiData> SushiPool { get; }

        /// <summary>보상으로 영입할 수 있는 손님 전체.</summary>
        public IReadOnlyList<CustomerData> CustomerPool { get; }

        /// <summary>한 번에 제시할 후보 수. 남은 후보가 이보다 적으면 있는 만큼만 제시된다.</summary>
        public int OfferCount { get; }

        internal void OnValidate();
    }
}
```

필드는 `[SerializeField] private List<SushiData> _sushiPool = new();` 형태다. `SushiSpawnEntry` 같은 래퍼를 만들지 않는다 — 그 래퍼는 "덱의 슬롯" 이라는 의미가 있어서 존재하는 것이고, 여기엔 담을 부가 정보가 없다.

### `OnValidate` 가 볼 것 — 구조 불변식만

```csharp
_offerCount = Mathf.Max(1, _offerCount);
```

`OfferCount` 의 하한이 **1** 인 이유: 0 이면 클리어해도 아무것도 제시되지 않아 보상 화면이 성립하지 않는다. 이건 밸런스가 아니라 구조 조건이라 코드에 둔다 (`StageConfig` 의 `MinimumTimeLimitSeconds` 와 같은 성격).

**밸런스를 검증에 섞지 않는다** (`.claude/rules/scriptable-object.md` §6):

- 풀이 비어 있는 것은 **오류가 아니다.** 유효한 구성이며(보상 없는 런), 실제로 착수 시점의 상태다
- `OfferCount` 가 풀 크기보다 큰 것도 **오류가 아니다.** 있는 만큼 제시하는 것이 정상 동작이다
- 풀에 `null` 항목이 섞이는 것도 막지 않는다 — 인스펙터에서 슬롯을 늘리면 자연히 생기며, `SpawnShareTable.Collect` 가 이미 같은 상황을 런타임에서 걸러 낸다. **거르는 책임은 step-06 의 생성기에 있다**

### 선행 산출물 의존성

없음. `SushiData` · `CustomerData` 는 M0 부터 있다.

### 밸런스 수치

**없다.** 이 단계는 애셋을 만들지 않는다. `_offerCount` 의 초기값은 인스펙터 기본값 `3` 으로 두되, **그것이 결정이 아님을** 필드 주석에 적는다 (확정은 step-09).

### 제약

- `Runtime.Data` 는 가장 아래 어셈블리다. `Runtime` · `Presentation` 을 참조하지 않는다 (asmdef 규칙 §3)
- **`Runtime.Data` 에 계산을 넣지 않는다.** "미보유 카드만 추리기" 는 `Runtime` 의 생성기 몫이다 — 대역 산술을 `CustomerData` 에 넣지 않기로 한 것과 같은 이유다 (`.claude/rules/scriptable-object.md` §7)
- `[SerializeField] private` + public 읽기 전용 프로퍼티. `public` 필드 금지
- public API 에 `///` XML 문서 주석
- `.asset` 파일을 만들지 않는다. 새 애셋 생성은 step-09
- 새 `.asmdef` 를 만들지 않는다

### 완료 판정

- [ ] `grep -n "RewardCatalog" Assets/Code/Scripts/Runtime.Data/RewardCatalog/RewardCatalog.cs` — 정의 확인
- [ ] `grep -rn "public " Assets/Code/Scripts/Runtime.Data/RewardCatalog/RewardCatalog.cs` — public **필드**가 0건 (프로퍼티만)
- [ ] `grep -rn "RunState\|SushiDeck\|Random" Assets/Code/Scripts/Runtime.Data/` — **0건** (데이터 어셈블리가 런타임을 모른다)
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh --fast` 전 항목 PASS

### 테스트 이름

```
RewardCatalogTests
  OfferCount_Zero_ClampsToOne
  OfferCount_Negative_ClampsToOne
  SushiPool_FreshCatalog_IsEmptyNotNull        ← StageConfigTests 의 같은 계약을 따른다
  CustomerPool_FreshCatalog_IsEmptyNotNull
  SushiPool_PopulatedByInspector_PreservesOrder ← 순서가 유지돼야 결정적 추첨이 가능하다
  OfferCount_LargerThanPool_IsNotAnError        ← 밸런스를 검증에 섞지 않는다
```

> `SushiPool_PopulatedByInspector_PreservesOrder` 는 **공허하게 통과하기 쉽다.** 항목 하나로 짜면 어떤 구현으로도 통과한다. **서로 다른 초밥 3개**를 넣고 인덱스별로 확인한다.
>
> SO 는 `ScriptableObject.CreateInstance<RewardCatalog>()` 로 만들고 `SerializedFieldSetter` 로 채운다. **디스크의 밸런스 애셋을 로드하지 않는다** (`.claude/rules/tests.md` §4).

### 예상 커밋 메시지

```
feat(reward): add reward catalog schema
```

---

## 금지 사항

- 애셋(`.asset`)을 만들거나 값을 채우지 않는다 (§7 — step-09 다)
- 풀이 비었거나 `OfferCount` 가 풀보다 크다고 `OnValidate` 에서 경고하지 않는다. 그건 기획이지 오류가 아니다
- `SushiTrait` 을 건드리지 않는다 (D8 — 시너지는 M3 밖)
- 후보 추첨·중복 제거 로직을 여기 넣지 않는다. `Runtime.Data` 는 계약이지 계산이 아니다
- 다른 어셈블리 파일을 수정하지 않는다
