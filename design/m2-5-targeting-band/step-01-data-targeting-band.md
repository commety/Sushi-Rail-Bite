# Step 01: `CustomerData` — 타겟팅을 단일 값에서 대역으로

- **영역:** `data` — 어셈블리 `Runtime.Data`
- **선행 단계:** 없음 (M2 완료 상태에서 시작)
- **후행 단계:** step-02 가 `TargetingMin`/`TargetingMax` 를 읽는다. step-08 이 애셋 값을 채운다

---

## 목적

`_targetingPrice` 하나를 `_targetingMin` · `_targetingMax` 둘로 쪼갠다. **스키마만 바꾼다** — 거리 계산도, 정렬 키도, 마감시한도 여기서 건드리지 않는다.

이 단계만 끝난 상태에서는 **게임 동작이 오히려 나빠진다** (대역 폭이 0, 거리 계산이 아직 단일 값용). 정상이다. step-02·03 이 이어서 붙는다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime.Data/CustomerData/CustomerData.cs` — 수정
- `Assets/Code/Scripts/Runtime.Data/SushiData/SushiData.cs` — **주석 1줄 수정** (`<see cref="CustomerData.TargetingPrice"/>` 가 사라진 심볼을 가리킨다 → `CS1574` 경고)
- `Assets/Tests/EditMode/Data/CustomerDataTests.cs` — 수정
- `Assets/Code/Scripts/Runtime/Customers/ClaimPairComparer.cs` — **최소 수정** (컴파일만 살린다. 진짜 교체는 step-02·03)
- `Assets/Tests/EditMode/Customers/CustomerRuntimeStateTests.cs` — 수정 (`SetTargetingPrice` 헬퍼)

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    public sealed class CustomerData : ScriptableObject
    {
        [SerializeField, Min(0)] private int _targetingMin;
        [SerializeField, Min(0)] private int _targetingMax;

        /// <summary>선호 가격대의 하한. <b>자격 조건이 아니다</b> — 대역 밖 초밥도 먹는다.</summary>
        public int TargetingMin => _targetingMin;

        /// <summary>선호 가격대의 상한. <c>TargetingMin</c> 이상임이 보장된다.</summary>
        public int TargetingMax => _targetingMax;
    }
}
```

`OnValidate` 에 한 줄이 는다:

```csharp
_targetingMin = Mathf.Max(0, _targetingMin);
_targetingMax = Mathf.Max(_targetingMin, _targetingMax);   // min ≤ max
```

**`TargetingPrice` 프로퍼티를 남기지 않는다.** `(min+max)/2` 같은 호환 프로퍼티를 두면 다음 사람이 그걸 쓰고, 대역이 두 개의 진실을 갖게 된다 — M2 의 `SushiSpawnEntry.Weight` 를 지운 것과 같은 이유다.

**`TargetingWidth` 프로퍼티도 두지 않는다.** 대역 폭은 정렬 키라서 `TargetingPriority.BandWidth` 에 들어간다 (D2·step-02). `Runtime.Data` 는 산술을 갖지 않는다.

### 호출부 — 지금 깨지는 곳

**두 종류를 모두 grep 해야 한다.** 프로퍼티 `TargetingPrice` 만 보면 절반을 놓친다:

```bash
grep -rn --include='*.cs' "TargetingPrice" Assets/      # 컴파일러가 잡아 준다
grep -rn --include='*.cs' "_targetingPrice" Assets/     # 문자열 필드명 — 컴파일러가 못 잡는다
```

두 번째가 중요하다. `SerializedFieldSetter.SetInt(data, "_targetingPrice", …)` 는 **문자열**이라 컴파일은 통과하고 `AssertFound` 가 **테스트 실행 시점에** 터진다. 컴파일만 보고 넘어가면 놓친다.

**총 8곳**이다 (작업서 초판이 프로퍼티만 grep 해 3곳을 빠뜨렸다):

| 파일 | 줄 | 이 단계에서 |
|---|---|---|
| `Runtime.Data/CustomerData/CustomerData.cs` | 17, 44, 68 | **교체** — 필드·프로퍼티·클램프 |
| `Runtime.Data/SushiData/SushiData.cs` | 39 | XML `cref` 문구 수정 |
| `Runtime/Customers/ClaimPairComparer.cs` | 64 | **최소 수정** — `Data.TargetingMin` 을 넘겨 컴파일만 살린다 |
| `Tests/EditMode/Data/CustomerDataTests.cs` | 24, 26, 30, 74, 78, 82 | 대역 검증으로 다시 쓴다 |
| `Tests/EditMode/Customers/CustomerRuntimeStateTests.cs` | 60, 63, 74, 76 | `SetTargetingBand(min, max)` |
| `Tests/EditMode/Customers/ClaimPairComparerTests.cs` | 159 | **문자열 필드명** — `NewCustomer` 헬퍼 |
| `Tests/EditMode/Customers/CustomerLogicTests.cs` | 170, 180 | **문자열 필드명** — 자격 회귀 테스트 2건 |
| `Tests/EditMode/Customers/SushiClaimResolverTests.cs` | 346 | **문자열 필드명** — `AddCustomer` 헬퍼 |

> 테스트 헬퍼는 **폭 0 대역**(`min == max == 옛 단일 값`)으로 옮긴다. 그래야 step-01 이 기존 동작을 그대로 보존하고, step-02·03 의 Red 가 대역 때문에 생긴 것임이 분명해진다.

> `ClaimPairComparer` 를 "최소 수정" 으로 지나가는 것은 M2 step-01 이 `SpawnSequence` 를 다룬 방식과 같다. **이 단계에서 대역 거리를 구현하지 않는다** — 그러면 step-02 의 Red 를 관측할 수 없다.

### 애셋은 건드리지 않는다

`Assets/Level/Balance/Customer.*.asset` 의 `_targetingPrice: N` 은 **그대로 둔다.** Unity 가 다음 직렬화에서 조용히 버리고 새 필드를 `0` 으로 채운다. 밸런스 애셋 편집은 §7 이라 step-08 에서 사람에게 값을 받는다.

> ⚠️ **그 사이 stage01 의 대역은 `0~0` 이다.** 모든 초밥이 대역 밖이고 폭 0(최강 전문가)이라 배정이 뒤집힌다. **step-01 과 step-08 이 같은 PR 안에 있어야 하는 이유**이며, 중간 커밋에서 씬을 재생해 판단하지 않는다.

### 선행 산출물 의존성

없음.

### 밸런스 수치

**이 단계에서 어떤 값도 채우지 않는다** (`CLAUDE.md` §7). 필드와 검증만 만든다.

### 제약

- `[Min]` 은 인스펙터 입력만 막는다. `min ≤ max` 는 `OnValidate` 에서 조인다 — 기존 세 SO 가 쓰는 패턴 그대로
- **`min == max` 는 유효하다** (폭 0 = 완전 전문가). 0 을 걸러내지 않는다
- **`min == 0 && max == 0` 도 컴파일·검증을 통과한다.** 데이터 검증으로 막지 않는 이유: `CustomerData` 는 덱·튜토리얼용 더미로도 만들어지고, 여기서 하한을 강제하면 테스트가 매번 값을 채워야 한다. **애셋 쪽 검증은 `Stage01SceneTests`(step-06)가 맡는다**
- `Runtime.Data` 는 다른 게임 어셈블리를 참조하지 않는다 (`.claude/rules/asmdef.md` §3)
- public 프로퍼티에 `///` XML 문서 주석. **"자격 조건이 아니다" 를 두 프로퍼티 모두에 남긴다** — 이름이 `Min`/`Max` 라 다음 사람이 반드시 게이트로 읽으려 든다

### 완료 판정

- [ ] `grep -n "TargetingMin\|TargetingMax" Assets/Code/Scripts/Runtime.Data/CustomerData/CustomerData.cs` — 필드·프로퍼티·클램프 확인
- [ ] `grep -rn --include='*.cs' "TargetingPrice" Assets/Code/Scripts/ Assets/Tests/` — **0건**
- [ ] `grep -rn --include='*.cs' "TargetingWidth" Assets/Code/Scripts/Runtime.Data/` — **0건** (산술은 `Runtime` 에)
- [ ] `git diff --stat Assets/Level/Balance/` — **변경 0건**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름

```
CustomerDataTests
  OnValidate_NegativeTargetingMin_ClampsToZero
  OnValidate_MaxBelowMin_RaisesMaxToMin          ← min ≤ max 불변식
  OnValidate_MaxEqualsMin_Kept                   ← 폭 0 은 유효하다
  OnValidate_BandFarFromAnyPrice_IsNotRejected   ← 기존 테스트의 대역판. 자격 게이트가 아님을 데이터 층에서 못박는다
```

### 예상 커밋 메시지

```
feat(customer): replace single targeting price with a min-max band
```

---

## 금지 사항

- **대역 거리 계산을 여기서 만들지 않는다.** `TargetingPriority` 는 step-02 다
- 정렬 키를 건드리지 않는다. `ClaimPairComparer` 는 **컴파일을 살리는 최소 수정만**
- `TargetingPrice` 호환 프로퍼티를 남기지 않는다
- `Assets/Level/Balance/*.asset` 을 수정하지 않는다 (§7)
- 마이그레이션 코드(`ISerializationCallbackReceiver` 로 옛 값을 대역으로 옮기는 등)를 쓰지 않는다. 애셋 3개는 사람이 step-08 에서 직접 정한다
