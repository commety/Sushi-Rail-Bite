# Step 01: `RunConfig` — 런의 스테이지 순서

- **영역:** `data` — 어셈블리 `Runtime.Data`
- **선행 단계:** 없음
- **후행 단계:** step-08 이 이 SO 를 인스펙터로 물린다. step-10 이 애셋을 만든다

---

## 목적

3스테이지 데모의 **순서**가 지금 어디에도 없다. 코드나 씬 배열에 박으면 `CLAUDE.md` §3.1(데이터 주도) 위반이고, 스테이지를 늘리거나 순서를 바꾸는 데 코드 수정이 필요해진다.

`RunConfig` 는 **순서 있는 `StageConfig` 목록** 하나만 담는다. 진행 판정(지금 몇 번째인가 · 다음이 있는가 · 런이 끝났는가)은 여기 두지 않는다 — 그건 `Runtime` 의 `RunProgression`(step-03) 이고, `Runtime.Data` 는 **계약이지 계산이 아니다**.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

- `Assets/Code/Scripts/Runtime.Data/RunConfig/RunConfig.cs` — 생성
- `Assets/Tests/EditMode/Data/RunConfigTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    [CreateAssetMenu(menuName = "SushiRailBite/Run Config", fileName = "RunConfig")]
    public sealed class RunConfig : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private List<StageConfig> _stages = new();

        public string DisplayName { get; }
        public IReadOnlyList<StageConfig> Stages { get; }
        public int StageCount { get; }   // = _stages.Count
    }
}
```

### 설계 지침

- **`OnValidate` 를 두지 않는다.** 구조 불변식이 없다 — 빈 목록도, `null` 항목도, 스테이지 번호가 뒤죽박죽인 목록도 **여기서는 오류가 아니다**. 씬을 조금씩 조립하는 동안 반쯤 채워진 애셋이 정상 상태이기 때문이다.
  - `null` 항목을 걸러내는 것은 `RunProgression`(step-03) 의 일이다. 걸러내는 지점을 둘로 나누면 어느 쪽이 지켰는지 알 수 없게 된다.
  - 다른 SO 들이 `OnValidate` 를 갖는 것은 **음수 가격처럼 그 값이면 게임이 성립하지 않는** 경우다. 여기엔 그런 값이 없다.
- **`StageConfig.StageNumber` 와 목록 인덱스를 대조하지 않는다.** 같아야 한다는 규칙을 만들면 순서를 바꿀 때마다 애셋 두 곳을 고쳐야 한다. **순서의 진실은 목록**이고 `StageNumber` 는 표시용이다.
- `StageCount` 를 프로퍼티로 두는 이유: 호출부가 `Stages.Count` 를 쓰면 `Stages` 를 매번 만지게 되는데, 진행 판정이 자주 묻는 값이다.

### 선행 산출물 의존성

- `SushiDefense.Data.StageConfig` — 이미 있다 (`Assets/Code/Scripts/Runtime.Data/StageConfig/StageConfig.cs`)

### 밸런스 수치

없음. 이 단계는 **스키마만** 만든다. 애셋(`Run.Demo.asset`)은 step-10.

### 제약

- `[SerializeField] private` + 읽기 전용 프로퍼티. `public` 필드 금지 (§4.2)
- public API 에 `///` XML 문서 주석 (§4.4 — `Runtime.Data` 는 계약이다)
- `Runtime.Data` 는 `Runtime` · `Presentation` 을 참조하지 않는다 (asmdef 규칙 §3·§4)
- `.meta` 파일을 손으로 만들지 않는다 (RULE-03) — 유니티가 생성하게 둔다
- 파일을 `Assets/Art/` 등 심링크 폴더에 만들지 않는다 (RULE-02)

### 테스트 (`RunConfigTests`)

`ScriptableObject.CreateInstance<RunConfig>()` 로 만들고, 목록 주입은 기존 `Assets/Tests/EditMode/Data/SerializedFieldSetter.cs` 를 재사용한다 (디스크 애셋을 로드하지 않는다 — [`rules/tests.md`](../../.claude/rules/tests.md) §4).

```
Stages_FreshConfig_IsEmptyNotNull
StageCount_FreshConfig_IsZero
StageCount_ThreeStages_IsThree
Stages_ThreeStages_PreservesAuthoredOrder     ← 인덱스 0·1·2 를 각각 확인. "개수만 같다" 로 끝내지 않는다
Stages_ListWithNullEntry_KeepsItAsAuthored    ← 걸러내지 않는 것이 계약임을 고정
```

> `Stages_ThreeStages_PreservesAuthoredOrder` 는 **스테이지 번호를 인덱스와 엇갈리게** 주고 짠다 (예: 목록 순서 `[번호3, 번호1, 번호2]`). 나란히 두면 번호로 정렬하는 잘못된 구현도 통과한다.

### 완료 판정

- [ ] `grep -rn "class RunConfig" Assets/Code/Scripts/Runtime.Data/` 로 정의 확인
- [ ] `grep -rn "OnValidate" Assets/Code/Scripts/Runtime.Data/RunConfig/` 가 **0건** (의도적으로 없다)
- [ ] `./tests/run-tests.sh` EditMode 전량 Green
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(run): add RunConfig for demo stage order
```

---

## 금지 사항

- 진행 판정(`CurrentStage`·`IsRunComplete`·`Advance`)을 여기 넣지 않는다. `Runtime.Data` 는 계산하지 않는다.
- `StageConfig` 를 수정하지 않는다.
- 스테이지 애셋을 만들지 않는다 (step-10).
