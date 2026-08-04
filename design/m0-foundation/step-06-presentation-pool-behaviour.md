# Step 06: 풀 팩토리 구현 — `SushiPoolBehaviour`

- **영역:** `presentation` — 어셈블리 `Presentation` (+ `Tests.PlayMode`)
- **선행 단계:** step-05 완료 필요 (`ISushiInstanceFactory<T>` · `SushiPool<T>` 시그니처 확정)
- **후행 단계:** step-07 빌드 관문. M1 의 벨트가 이 컴포넌트에서 초밥을 빌린다

---

## 목적

step-05 의 순수 풀에 **실제 Unity 인스턴스 생성**을 붙인다. 이 파일이 프로젝트에서 `Object.Instantiate` 를 부르는 **유일한 프로덕션 지점**이 된다 (`CLAUDE.md` §3.4). 그리고 반납한 오브젝트가 정말 재사용되는지를 PlayMode 로 확인한다 — M0 완료 판정 항목이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD — README D1)

1. `SushiPoolBehaviour` 시그니처를 만든다
2. PlayMode 테스트를 쓰고 **Red 확인** (`./tests/run-tests.sh all`)
3. 최소 구현으로 Green

### 생성 파일

```
Assets/Code/Scripts/Presentation/Belt/SushiPoolBehaviour.cs   — 생성
Assets/Tests/PlayMode/Belt/SushiPoolBehaviourTests.cs         — 생성
```

프리팹이 필요하면 **`Assets/Code/Scripts/Presentation/Belt/` 안에** 둔다 — `Assets/Art/` 는 심링크 폴더라 워크트리 git 이 새 파일을 못 본다 (RULE-02 · `.claude/rules/parallel-work.md` §2). 다만 **테스트는 프리팹 없이도 돌아야 한다** (아래 참고).

### 핵심 심볼

```csharp
namespace SushiDefense.Belt
{
    /// <summary>
    /// SushiPool 에 실제 GameObject 생성을 공급하는 유일한 지점.
    /// 프로덕션 코드에서 Instantiate/Destroy 를 부르는 곳은 여기뿐이다 (CLAUDE.md §3.4).
    /// </summary>
    public sealed class SushiPoolBehaviour : MonoBehaviour, ISushiInstanceFactory<GameObject>
    {
        [SerializeField] private GameObject _sushiPrefab;
        [SerializeField] private Transform _inactiveParent;
        [SerializeField, Min(0)] private int _prewarmCount;

        public GameObject Create();
        public void Dispose(GameObject instance);

        public GameObject Rent();          // 내부 SushiPool<GameObject> 에 위임
        public void Return(GameObject instance);

        /// <summary>테스트·부트스트랩에서 인스펙터 없이 초기화하기 위한 진입점.</summary>
        public void Initialize(GameObject prefab, int prewarmCount);
    }
}
```

- 대여 시 `SetActive(true)` + 활성 부모로, 반납 시 `SetActive(false)` + `_inactiveParent` 로 되돌린다
- **실제 풀 로직은 `SushiPool<GameObject>` 가 갖는다.** 이 클래스는 위임만 — 로직이 `MonoBehaviour` 안으로 새면 EditMode 검증이 막힌다 (`CLAUDE.md` §3.2)
- `Awake` 에서 `SushiPool<GameObject>` 를 만들고 `_prewarmCount` 만큼 프리웜한다. `GetComponent` 결과는 `Awake` 에서 캐싱 (§4.3)

### 선행 산출물 의존성

- step-05 — `SushiDefense.Belt.ISushiInstanceFactory<T>`, `SushiDefense.Belt.SushiPool<T>`

### 밸런스 수치

`_prewarmCount` 는 인스펙터 노출 필드다. **값을 코드 상수로 박지 않는다.** M1 에서 `StageConfig` 에서 넘겨받는 경로로 바꾼다 (지금은 인스펙터 기본값 0).

### 테스트 항목 (`Tests.PlayMode`)

```
SushiPoolBehaviourTests
  Rent_EmptyPool_InstantiatesNewGameObject
  Rent_AfterReturn_ReturnsSameInstanceReference       ← 재사용 (M0 완료 판정)
  Rent_AfterReturn_DoesNotIncreaseTotalObjectCount    ← 새로 Instantiate 되지 않음
  Return_Instance_DeactivatesGameObject
  Rent_ReusedInstance_IsActiveAgain
  Initialize_WithPrewarm_CreatesInstancesUpFront
```

- 테스트 프리팹은 디스크 애셋을 로드하지 말고 **테스트 안에서 `new GameObject(...)` 로 만들어 `Initialize` 에 넘긴다.** 애셋 의존이 없어야 워크트리·CI 어디서든 돈다 (테스트 코드의 `Instantiate` 는 §3.4 예외)
- 생성한 `GameObject` 는 `[TearDown]` 에서 정리한다
- `Rent_AfterReturn_DoesNotIncreaseTotalObjectCount` 는 씬 전체를 세지 말고 **팩토리 호출 횟수**나 풀의 `CountAll` 로 판정한다 (씬 카운트는 다른 테스트의 잔여물에 오염된다)

### 제약

- **PlayMode 테스트는 여기까지만.** 프레임 정확성이 실제로 필요한 것만 PlayMode 로 간다 (`.claude/rules/tests.md` §1). 풀 로직 자체의 검증은 step-05 의 EditMode 가 이미 갖고 있다
- `Find`/`FindObjectOfType` 금지 (§4.3). 참조는 인스펙터 주입 또는 `Initialize` 인자로
- 이벤트 구독을 만들면 `OnDestroy`/`OnDisable` 에서 해제한다 (§6)
- 씬(`Assets/Level/Scenes/*.unity`)을 수정하지 않는다 — 병렬 작업 시 씬은 직렬화 대상이다 (`.claude/rules/parallel-work.md` §3)
- `Presentation` → `Runtime` 참조는 정방향이라 정상. **역방향(`Runtime` → `Presentation`)을 만들지 않는다**

### 완료 판정

- [ ] `grep -rn "Instantiate\|Destroy(" Assets/Code/Scripts/ --include="*.cs" | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **`SushiPoolBehaviour.cs` 외 0건**
- [ ] `grep -rn "FindObjectOfType\|GameObject.Find" Assets/Code/Scripts/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → 0건
      > 주석 제외 필터의 근거는 [README D7](README.md).
- [ ] `./tests/run-tests.sh all` — EditMode + PlayMode 전량 Green
- [ ] `./tests/lint.sh` 통과
- [ ] `git status --short Assets/Level/` → 변경 없음

### 예상 커밋 메시지

```
feat(belt): add sushi pool behaviour backing the pool with instantiate
```

---

## 금지 사항

- 벨트 이동·스폰을 구현하지 않는다 (M1).
- 풀 로직을 이 `MonoBehaviour` 안에 다시 쓰지 않는다. `SushiPool<T>` 에 위임한다.
- `Assets/Art/` 에 새 스프라이트·프리팹을 만들지 않는다 (RULE-02).
