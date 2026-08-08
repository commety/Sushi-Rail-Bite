# Step 01: `CardCatalog` SO · UI 클릭 큐

- **영역:** `data` — 어셈블리 `Runtime.Data`
- **선행 단계:** 없음
- **후행 단계:** step-10(백과사전)이 `CardCatalog` 를, step-11 이 `_uiClick` 클립을 쓴다

---

## 목적

백과사전에는 **이 게임에 존재하는 모든 카드**가 나와야 한다. 지금 그 목록을 가진 애셋이 없다 — `RewardCatalog.asset` 은 초밥 3장·손님 2명뿐인 **보상 전용 풀**이라 시작 덱 5종이 통째로 빠져 있다. 그것을 백과사전의 출처로 쓰면 플레이어가 벨트에서 보고 있는 초밥이 사전에 없는 상태가 된다.

곁들여 UI 클릭음 큐를 `AudioBankSO` 에 하나 더한다. M5 가 넘긴 그대로 — *"메뉴 SE 를 큐로 추가만 하면 된다"*.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime.Data/CardCatalog/CardCatalog.cs` — 생성
- `Assets/Code/Scripts/Runtime.Data/Audio/AudioBankSO.cs` — 수정 (큐 1개 추가)
- `Assets/Tests/EditMode/Data/CardCatalogTests.cs` — 생성
- `Assets/Tests/EditMode/Data/AudioBankSOTests.cs` — 수정 (기존 파일 있는지 확인 후)
- `Assets/Level/Balance/CardCatalog.asset` — 생성 (**§7 승인 후**)

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    /// <summary>이 게임에 존재하는 카드 전체. 백과사전·덱 표시의 출처다.</summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Card Catalog", fileName = "CardCatalog")]
    public sealed class CardCatalog : ScriptableObject
    {
        public IReadOnlyList<SushiData> AllSushi { get; }
        public IReadOnlyList<CustomerData> AllCustomers { get; }
    }
}
```

`AudioBankSO` 에 추가:

```csharp
[SerializeField] private AudioCue _uiClick = new();

/// <summary>버튼·카드를 눌렀을 때. 메뉴에서 연타되므로 가장 작아야 한다.</summary>
public AudioCue UiClick => _uiClick;
```

`OnValidate()` 의 `Clamp()` 호출 목록에도 **반드시 더한다** — 중첩 `[Serializable]` 타입은 Unity 의 검증 훅을 따로 받지 못한다 (step-01/M5 에서 확인된 것).

### 이름을 `RewardCatalog` 와 나눠 둔 이유

둘은 다른 질문에 답한다.

| | 답하는 질문 |
|---|---|
| `RewardCatalog` | **보상으로 줄 수 있는** 카드는 무엇인가 |
| `CardCatalog` | 이 게임에 **존재하는** 카드는 무엇인가 |

지금은 후자가 전자를 포함하지만, 시작 덱 카드는 보상으로 나오지 않으므로 **같은 목록이 아니다.** 한 SO 에 몰면 "보상 풀에서 뺐더니 사전에서도 사라졌다" 가 난다.

> **중복을 걱정할 필요는 있다.** 두 애셋이 카드를 각각 나열하므로, 카드를 추가하고 한쪽만 채우는 사고가 가능하다. 그래서 **`CardCatalog` 가 `RewardCatalog` 를 덮는지 확인하는 데이터 검증 테스트**를 함께 둔다 (아래 테스트 계획). 코드로 합치는 대신 테스트로 지킨다 — 목록이 갈리는 것은 정상이고, **한쪽이 다른 쪽의 부분집합이 아닌 것**만 오류다.

### 선행 산출물 의존성

없음. `SushiData` · `CustomerData` 는 이미 있다.

### 밸런스 수치

| 값 | 어디에 | 제안 |
|---|---|---|
| UI 클릭 볼륨 | `AudioBank.asset` | `0.4` |
| UI 클릭 간격(초) | `AudioBank.asset` | `0.05` |

**`.asset` 에 쓰는 것은 사람의 판단이다** (§7). 값과 diff 를 제시하고 승인을 기다린다. 클립 자체는 step-11 이 만든다 — **이 단계에서는 클립이 비어 있어도 된다** (`AudioCue.HasClip` 이 이미 그것을 다룬다).

### 제약

- `Runtime.Data` 는 다른 게임 어셈블리를 참조하지 않는다 (asmdef §3)
- `[SerializeField] private` + 읽기 전용 프로퍼티. `public` 필드 금지
- **`OnValidate` 는 구조 불변식만 본다** ([`scriptable-object.md`](../../.claude/rules/scriptable-object.md) §6). 목록이 비어 있는 것·`null` 항목이 섞인 것은 **오류가 아니다** — `RewardCatalog` 가 이미 그 판단을 내렸고 같은 규칙을 따른다
- public API 에 `///` XML 문서 주석
- **밸런스 `.asset` 생성은 §7** — 코드가 먼저, 애셋은 승인 뒤

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
EditMode  Catalog_Empty_ReturnsEmptyLists
          Catalog_NullEntry_IsNotFiltered            ← RewardCatalog 와 같은 판단임을 고정
          UiClick_Default_HasNoClip
          OnValidate_UiClickVolumeAboveOne_Clamps    ← 큐 추가가 Clamp 목록에 빠지지 않았나
데이터    Catalog_CoversRewardPool                   ← 보상 풀이 사전의 부분집합인가
          Catalog_CoversEveryStageSpawnTable         ← 벨트에 도는 것이 사전에 있는가
          Catalog_HasNoDuplicate · HasNoEmptySlot
          Catalog_HasCustomerOutsideRewardPool       ← 시작 손님이 사전에 있는가
```

> **스테이지를 `RunConfig` 로 순회한다.** 경로를 하나씩 적으면 스테이지가 늘 때 이 테스트만
> 조용히 뒤처진다. 그리고 **모든 스폰 구성이 비면 아무것도 검사하지 않게 되므로**, 모은
> 목록이 비어 있지 않다는 단언을 같은 테스트에 함께 박는다.

뒤 두 개는 **디스크의 실제 애셋을 읽는 데이터 검증 테스트**다. [`tests.md`](../../.claude/rules/tests.md) §4 가 금지하는 것은 *밸런스 수치*를 애셋에서 읽는 것이지, **목록의 정합성**을 보는 것은 정확히 이 테스트가 있어야 하는 자리다. 수치가 바뀌어도 깨지지 않고, 카드를 한쪽에만 넣으면 깨진다.

> **`OnValidate_UiClickVolumeAboveOne_Clamps` 를 공허하지 않게 쓴다.** 다른 큐도 함께 클램프되므로, **`_uiClick` 만 이상값**으로 두고 나머지는 정상값으로 채워야 이 큐가 목록에서 빠진 것을 잡는다.

### 주입 실측

| 주입 | 예측 | 실제 |
|---|---|---|
| `OnValidate` 의 `_uiClick.Clamp()` 제거 | 1건 | **2건** — 볼륨·간격 두 테스트가 각각 잡았다 |
| 직렬화 필드 `_allSushi` 를 개명 | 순서 테스트 | 예측대로. `SerializedFieldSetter.AssertFound` 가 *"필드명이 바뀌었는지 확인하세요"* 로 원인까지 짚었다 |
| `CardCatalog.asset` 에서 **장어**(스폰 구성)를 뺀다 | `CoversEveryStageSpawnTable` | 예측대로 **1건** |
| `CardCatalog.asset` 에서 **참치**(보상 풀)를 뺀다 | `CoversRewardPool` | 예측대로 **1건** |

> **초밥 두 장을 따로 뺀 이유.** M5 step-05 에서 덱에 없는 초밥에 주입했다가 0건이 나온 적이
> 있다 — 주입이 관측 범위 밖이었던 것이다. 스폰 구성에만 있는 것(장어)과 보상 풀에만 있는
> 것(참치)을 나눠 넣어야 **두 검사가 각각 살아 있음**이 확인된다.

### 공허해서 지운 테스트 하나

`AppendSushi_DoesNotTouchCustomers`(두 목록이 서로를 모른다)를 처음 넣었다가 **지웠다.**

두 목록은 타입이 달라 C# 에서 같은 필드를 가리킬 수 없고, 그 상태를 만드는 주입 자체가
불가능하다. 실제로 필드를 개명해 보니 순서 테스트와 **같은 이유로 같이** 실패했다 —
독립적으로 잡는 것이 하나도 없다. 남겨 두면 검증은 안 늘고 코드만 는다 (R8).

**Red 는 실제로 관측했다.** 테스트를 먼저 쓰고 돌려 `CS0246: CardCatalog` 컴파일 실패(종료
코드 2)를 확인한 뒤 구현했다 — M5 step-08 에서 건너뛴 절차다.

### 완료 판정

- [x] `grep -rn "class CardCatalog" Assets/Code/Scripts/Runtime.Data/`
- [x] `AudioBankSO.OnValidate` 의 `Clamp()` 호출이 큐 여덟과 맞는다
- [x] EditMode Green — **572/572**
- [x] `CardCatalog.asset` 이 §7 승인 후 생성되었고 **초밥 8 · 손님 3** 을 담는다
- [x] `./tests/preflight.sh` 전 항목 PASS

### 실측 정정 — 카드는 11장이다

작업서와 README 가 *"초밥 9 · 손님 3"* 으로 적었는데 **실재는 초밥 8 · 손님 3** 이다.
`Sushi.Placeholder` · `Customer.Placeholder` 둘은 테스트용이라 사전에서 뺐다 (포화도 99 같은
비현실적 값을 갖고 있어 사전에 나오면 안 된다).

| | |
|---|---|
| 초밥 8 | 오징어 120 · 계란 130 · 광어 150 · 연어 150 · 방어 190 · 참치 250 · 장어 300 · 성게 400 |
| 손님 3 | 기본 20 · 소식 40 · 먹보 60 |

Stage02 · Stage03 의 스폰 구성은 **비어 있다** — 덱은 Stage01 의 5종에서 시작해 보상 3종으로
자란다. 그래서 `Catalog_CoversEveryStageSpawnTable` 이 실제로 덮는 것은 Stage01 뿐이며,
스테이지를 순회하는 형태로 짜 둔 것은 M9 에서 나머지 둘이 채워질 때를 위한 것이다.

### `AudioBankAssetTests` 가 아직 큐 여덟을 덮지 않는다

`EveryCue_HasClip` · `EveryCue_UsesItsOwnClip` 은 **일곱**만 본다. `_uiClick` 의 클립이
step-11 에서 만들어지기 때문이며, 그 사실을 테스트 파일의 주석에 남겼다. **step-11 이 클립을
물리면서 두 테스트의 개수를 함께 늘린다.**

### 예상 커밋 메시지

```
feat(data): add card catalog and ui click cue
```

---

## 금지 사항

- `RewardCatalog` 를 고쳐 두 용도를 겸하게 만들지 않는다
- `SushiData`·`CustomerData` 에 등급 필드를 추가하지 않는다 (README D3)
- `OnValidate` 에서 목록 내용을 검증하지 않는다 — 밸런스를 구조 검증에 섞는 것이다
- 승인 없이 `.asset` 을 만들지 않는다
- 다른 어셈블리 파일을 수정하지 않는다
