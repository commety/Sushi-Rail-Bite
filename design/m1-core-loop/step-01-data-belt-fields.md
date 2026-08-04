# Step 01: 벨트·래치 밸런스 필드 추가

- **영역:** `data` — 어셈블리 `Runtime.Data` (+ `Tests.EditMode`)
- **선행 단계:** 없음 (M0 완료가 전제)
- **후행 단계:** step-02 가 `SpawnIntervalSeconds`·`BeltLength`, step-04 가 `RecognitionLatchSeconds`, step-07 이 `TableSlotDefinition.BeltPosition` 을 읽는다

---

## 목적

M1 의 순수 로직이 씬 없이 돌려면 벨트 길이·스폰 간격·래치 상한을 **데이터에서** 읽어야 한다. 지금 필드를 세워 두지 않으면 다음 단계들이 코드에 숫자를 박게 되고, 그건 `CLAUDE.md` §3.1 위반이자 나중에 되돌리기 비싼 종류의 부채다.

**값은 정하지 않는다.** 필드 이름과 제약만 만든다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD)

1. 필드·프로퍼티를 추가한다. `OnValidate` 클램프는 **아직 넣지 않는다**
2. 검증 테스트를 쓰고 **Red 를 눈으로 확인**한다 (`./tests/run-tests.sh`)
3. 클램프를 추가해 Green

> 컴파일 에러는 Red 로 세지 않는다 (M0 작업서 D1 과 같은 규칙). 시그니처가 먼저 서 있어야 한다.

### 수정 파일

```
Assets/Code/Scripts/Runtime.Data/StageConfig/StageConfig.cs            — 수정 (필드 3개 추가)
Assets/Code/Scripts/Runtime.Data/StageConfig/TableSlotDefinition.cs    — 수정 (필드 1개 추가)
Assets/Tests/EditMode/Data/StageConfigTests.cs                         — 수정 (테스트 추가)
Assets/Tests/EditMode/Data/TableSlotDefinitionTests.cs                 — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    public sealed class StageConfig : ScriptableObject
    {
        // ── 추가 ──
        public float SpawnIntervalSeconds { get; }   // [Min] 양수. 0 이면 초밥이 무한히 쏟아진다
        public float BeltLength { get; }             // [Min] 양수. 이 값을 넘으면 끝점 도달
        public float RecognitionLatchSeconds { get; } // [Min(0)] 0 = 상한 없음
    }

    public sealed class TableSlotDefinition
    {
        // ── 추가 ──
        public float BeltPosition { get; }   // 이 자리의 1차원 벨트 좌표
    }
}
```

- 저장은 기존과 같이 `[SerializeField] private` + 읽기 전용 프로퍼티. `public` 필드 금지
- `TableSlotDefinition.Position`(Vector2, 기존)은 **화면 배치용으로 남긴다.** 새 `BeltPosition` 은 집기 범위 계산용 1차원 좌표라 역할이 다르다 — 둘을 합치지 않는다. 이 이유를 `///` 주석에 남긴다
- `RecognitionLatchSeconds` 의 `0 = 상한 없음` 규약을 `///` 주석에 명시한다. 이 규약을 모르면 다음 사람이 0 을 "즉시 만료"로 읽는다

### 선행 산출물 의존성

- M0 의 `StageConfig` · `TableSlotDefinition`

### 밸런스 수치

**이 단계에서 정하는 값은 없다.** `.asset` 인스턴스를 만들지 않는다 (`CLAUDE.md` §7).

### 테스트 항목 (`Tests.EditMode`)

SO 는 `ScriptableObject.CreateInstance<T>()` 로 만든다. 디스크 애셋을 로드하지 않는다.
기존 `SerializedFieldSetter`(M0, `Assets/Tests/EditMode/Data/`)를 그대로 쓴다.

```
StageConfigTests (추가)
  OnValidate_ZeroSpawnInterval_ClampsToPositive
  OnValidate_NegativeSpawnInterval_ClampsToPositive
  OnValidate_ZeroBeltLength_ClampsToPositive
  OnValidate_NegativeRecognitionLatch_ClampsToZero
  RecognitionLatchSeconds_Zero_MeansNoCap            ← 규약을 테스트로 고정한다

TableSlotDefinitionTests (신규)
  BeltPosition_Default_IsZero
  BeltPosition_NegativeValue_IsAllowed               ← 벨트 좌표계 원점보다 앞일 수 있다
```

> `BeltPosition` 은 **클램프하지 않는다.** 벨트 좌표의 원점을 어디로 잡을지는 씬 조립(step-09)에서 정해지고, 음수 자리가 정상일 수 있다. 클램프를 걸면 step-09 에서 되돌리게 된다.

### 제약

- `Runtime.Data` 는 `Runtime`·`Presentation` 을 참조하지 않는다
- 추가 필드 전부에 `///` XML 문서 주석 (`CLAUDE.md` §4.4)
- `[Min]` 은 인스펙터 입력만 막는다. 직렬화된 이상값·코드 대입까지 막으려면 `OnValidate` 클램프가 필요하다 (M0 에서 확립한 패턴)
- 기존 필드의 이름·타입을 바꾸지 않는다. 이미 M0 테스트가 물고 있다

### 완료 판정

D7 주석 제외 필터를 붙인다 ([M0 작업서 README D7](../m0-foundation/README.md)):
`| grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'`

- [ ] `grep -n "SpawnIntervalSeconds\|BeltLength\|RecognitionLatchSeconds" Assets/Code/Scripts/Runtime.Data/StageConfig/StageConfig.cs` → 프로퍼티·필드 모두 확인
- [ ] `grep -n "BeltPosition" Assets/Code/Scripts/Runtime.Data/StageConfig/TableSlotDefinition.cs` → 확인
- [ ] `grep -rn "public [A-Za-z<>]* [A-Za-z]*;" Assets/Code/Scripts/Runtime.Data/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → public 필드 0건
- [ ] `./tests/run-tests.sh` — 전량 Green (M0 의 54개 + 신규)
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(data): add belt timing and recognition latch fields
```

---

## 금지 사항

- 밸런스 값을 정하지 않는다. `.asset` 을 만들지 않는다.
- `Runtime`·`Presentation` 파일을 건드리지 않는다.
- `TableSlotDefinition.Position`(Vector2)을 지우거나 `BeltPosition` 으로 대체하지 않는다. 역할이 다르다.
