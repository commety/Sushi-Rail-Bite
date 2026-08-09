# Step 01: 손님이 차지하는 인구수를 데이터에 둔다

- **영역:** `data` — 어셈블리 `Runtime.Data`
- **선행 단계:** 없음
- **후행 단계:** step-02 가 `CustomerData.Population` 으로 한도를 계산한다

---

## 목적

배치 한도가 **머릿수에서 인구수로** 바뀌려면, 먼저 손님마다 «몇을 차지하는가» 가 데이터에 있어야 한다. 이 단계는 필드 하나와 그 불변식만 만든다 — 세는 쪽은 건드리지 않는다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime.Data/CustomerData/CustomerData.cs` — 수정
- `Assets/Tests/EditMode/Data/CustomerDataTests.cs` — 수정
- `Assets/Level/Balance/Customer.*.asset` 4장 — **§7 승인 후** 수정
- `CLAUDE.md` §2 용어 사전 · `AGENTS.md` — **§7 승인 후** 수정 (두 파일은 항상 같은 내용이다)

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    public sealed class CustomerData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _population = 1;

        /// <summary>
        /// 이 손님이 배치 한도에서 차지하는 몫. <b>자리 수가 아니다</b> — 인구수가 2여도
        /// 앉는 자리는 하나다.
        /// </summary>
        public int Population => _population;
    }
}
```

`OnValidate` 에 `_population = Mathf.Max(1, _population);` 를 더한다. **0 은 허용하지 않는다** — 0 이면 한도가 아무것도 막지 못하는 손님이 생기고, 그것은 밸런스가 아니라 구조 붕괴다.

### 선행 산출물 의존성

없음.

### 밸런스 수치

**§7 승인 대상이다.** 에이전트는 아래 diff 를 제시하고 **멈춘다.**

| 애셋 | `_population` | 근거 |
|---|---|---|
| `Customer.Standard.asset` | `1` | 기준값 |
| `Customer.SmallEater.asset` | `1` | 정수 유지 (README D1) |
| `Customer.BigEater.asset` | **`2`** | 요구 원문 |
| `Customer.Placeholder.asset` | `1` | 테스트용 — 판정에 끼지 않게 |

> **스테이지의 `_maxPlacedCustomers` 는 이 단계에서 건드리지 않는다.** 얼마가 맞는지는 인구수가 실제로 도는 것을 보고 정해야 한다 → step-03 §7-2.

### 용어 사전 등재 (§7 승인 대상)

`CLAUDE.md` §2 와 `AGENTS.md` 의 같은 표에 한 줄을 더한다:

```
| 인구수 | 손님 한 명이 배치 한도에서 차지하는 몫. **자리 수가 아니다** — 인구수 2여도 앉는 자리는 하나다 | `Population` |
```

### 제약

- `Runtime.Data` 는 계약이지 계산이 아니다. **`Population` 을 쓰는 산술을 여기 두지 않는다** — 합계는 step-02 의 `Runtime` 에 산다 ([`scriptable-object.md`](../../.claude/rules/scriptable-object.md) §7 이 대역 산술에 대해 정한 것과 같은 선)
- `[SerializeField] private` + 읽기 전용 프로퍼티. `public` 필드 금지
- public API 에 `///` XML 문서 주석
- **`_maxPlacedCustomers` 를 포함해 기존 직렬화 필드 이름을 하나도 바꾸지 않는다** (README D3)
- `OnValidate` 는 **구조 불변식만** 본다. «먹보의 인구수가 2인 것이 밸런스로 맞나» 는 검증 대상이 아니다

### 완료 판정

- [ ] `grep -n "Population" Assets/Code/Scripts/Runtime.Data/CustomerData/CustomerData.cs` 로 필드·프로퍼티·클램프 3곳 확인
- [ ] `CustomerDataTests` 에 다음이 있다:
  - `OnValidate_PopulationBelowOne_ClampsToOne`
  - `Population_Default_IsOne` — 값을 안 넣은 기존 애셋이 **한 명으로 읽히는지**. 이게 없으면 직렬화 기본값이 0 이 되어 한도가 무력화되는 회귀를 못 잡는다
- [ ] `./tests/run-tests.sh` EditMode 전량 Green
- [ ] `.asset` 4장과 `CLAUDE.md`/`AGENTS.md` 변경은 **승인 전까지 커밋하지 않는다**

### 예상 커밋 메시지

```
feat(customer): give each customer a population weight
```

애셋·문서를 함께 넣는다면 승인 뒤 별도 커밋:

```
chore(balance): set the big eater to two population
```

---

## 금지 사항

- `CustomerPlacementService` 를 이 단계에서 고치지 않는다 (step-02).
- 인구수를 `float` 로 두지 않는다. 분수는 README 가 뺐다.
- 승인 없이 `.asset` 을 커밋하지 않는다 (`CLAUDE.md` §7).
