---
description: Assets/Code/Scripts/ 하위 C# 파일 수정 시 적용되는 규칙
globs: ["Assets/Code/Scripts/**/*.cs", "Assets/Tests/**/*.cs", "Assets/Editor/**/*.cs"]
---

# 스크립트 작성 규칙

근거: `CLAUDE.md` §3(아키텍처 원칙) · §4(코딩 컨벤션). 여기엔 파일 단위로 즉시 확인 가능한 항목만 둔다.

## 1. 배치 · 네임스페이스

- 네임스페이스는 `SushiDefense.<Domain>` 형태다 (`SushiDefense.Belt`, `SushiDefense.Customers`, `SushiDefense.Scoring`, `SushiDefense.Data`, `SushiDefense.UI` …).
  - 예외: 에이전트 툴링용 `Assets/Editor/`(ClaudeBridge, RunBuildCommand)는 기존대로 `Editor` / `Editor.ClaudeBridge` 네임스페이스를 쓴다. 게임 코드와 섞지 않는다.
- 파일이 놓이는 폴더와 어셈블리:

| 폴더 | 어셈블리 | 들어가는 것 |
|---|---|---|
| `Assets/Code/Scripts/Runtime/` | `Runtime` | 순수 C# 게임 로직 (`CustomerLogic`, 점수 계산, 상태 머신). Unity API 의존 최소 |
| `Assets/Code/Scripts/Runtime.Data/` | `Runtime.Data` | `ScriptableObject` 정의 (`SushiData`, `CustomerData`, `StageConfig`, `~EventChannelSO`) |
| `Assets/Code/Scripts/Presentation/` | `Presentation` | `MonoBehaviour`, View, Presenter 바인딩 |
| `Assets/Code/Scripts/Editor/` | `Editor` | 게임용 에디터 툴링 |
| `Assets/Tests/EditMode/` | `Tests.EditMode` | 순수 로직 단위 테스트 |
| `Assets/Tests/PlayMode/` | `Tests.PlayMode` | 씬·코루틴 통합 테스트 |

- 한 파일 = 한 public 타입. 파일명 = 타입명.
- `using` 순서: System → Unity → 서드파티 → `SushiDefense.*`.
- 이름 규칙: public `PascalCase` / private 필드 `_camelCase` / 지역·인자 `camelCase` / 인터페이스 `I` 접두 / SO 는 `~Data`·`~Config` 접미 / 이벤트 채널 SO 는 `~EventChannelSO`.

## 2. 로직은 MonoBehaviour 밖에

- `MonoBehaviour` 는 라이프사이클·씬 배치용 얇은 껍데기다. 집기 판정·경합 해소·포화도·소화 타이머·점수 계산은 **plain C# 클래스**에 둔다 (`Customer : MonoBehaviour` → `CustomerLogic`).
- 이유: EditMode 테스트가 `MonoBehaviour` 없이 로직을 검증할 수 있어야 한다 (`CLAUDE.md` §5.2).
- 상속 계층을 깊게 만들지 않는다. 인터페이스(`ISushiConsumer`, `IScorable`) + 컴포지션 우선.
- 손님 상태(`Idle → Eating → Full/Digesting → Idle`)는 명시적 상태 머신으로. `enum` + `switch` 도 좋지만 전이 로직 자체는 테스트 가능한 순수 클래스에 있어야 한다.

### 초밥 집기 규칙 — 자주 틀리는 지점

`CLAUDE.md` §1.1-3a 가 이 프로젝트의 핵심 규칙이고, 직관과 어긋나서 반복적으로 잘못 구현된다.

- 손님은 집기 범위 안의 초밥을 **가격과 무관하게 FIFO(먼저 들어온 것 먼저)** 로 집는다.
- **타겟팅(선호 가격대)은 먹을 수 있는지를 가르는 조건이 아니다.** 한 초밥을 두 명 이상이 집을 수 있을 때 **누가 가져갈지 정하는 우선순위**일 뿐이다. 우선순위가 같으면 임의(랜덤 등)로 정한다.
- **금지 패턴** — 이런 코드는 기획 위반이다:
  ```csharp
  // ❌ 타겟팅을 게이트로 사용 — 손님이 눈앞의 초밥을 두고 구경하게 된다
  if (sushi.Price < _data.TargetingMin || sushi.Price > _data.TargetingMax)
      return false;
  ```
- 올바른 구조: 집기 가능 여부는 **범위·포화도·상태**만 본다. 타겟팅은 경합 해소기(`SushiClaimResolver`)에서만 읽는다.
- 손님이 집을 수 있는데 집지 않는 상태가 보이면 **플레이 경험 버그**로 취급하고 원인을 찾는다.

## 3. 데이터 · 매직 넘버

- 밸런스 수치(초밥 가격, 집기 범위, 먹는 속도, 포화도, 소화 시간, 목표 매출, 제한 시간)를 코드에 하드코딩하지 않는다. 전부 SO 필드. → [`scriptable-object.md`](scriptable-object.md)
- SO 는 읽기 전용 템플릿이다. 런타임 상태(현재 포화도, 남은 소화 시간)는 별도 클래스/구조체에 둔다.

## 4. Unity API 사용

- `GetComponent<T>()` 결과는 `Awake`/`Start` 에서 필드에 캐싱한다. `Update()` 에서 매 프레임 호출 금지.
- `Find` / `FindObjectOfType` 는 프로덕션 코드에서 금지 (부트스트랩·에디터 툴링만 예외). 인스펙터 주입으로 해결한다.
- `TryGetComponent` 우선. null 체크는 `!= null` 을 명시적으로 (`UnityEngine.Object` 의 `==` 오버로드 주의).
- `Update()` 안에서 할당(new / LINQ / 문자열 결합)을 만들지 않는다. **배포 타깃이 WebGL** 이라 GC 스파이크가 그대로 프레임 히칭이 된다.
- 벨트 위 초밥은 항상 `SushiPool` 을 경유한다. 프로덕션 코드에서 `Instantiate`/`Destroy` 직접 호출 금지 (에디터 툴링·테스트는 예외).

## 5. 결합도

- 벨트 ↔ 손님 ↔ 점수 ↔ UI 사이의 직접 참조를 최소화한다. C# `event`/`Action` 또는 SO 이벤트 채널(`SushiEatenEventChannelSO`)로 통신한다.
- UI 는 MVP: `View`(입력·렌더링만) / `Presenter`(순수 C#, 테스트 대상) / `Model`(덱·런 상태). `Presenter` 는 Unity API 에 직접 의존하지 않고 `IDeckBuildingView` 같은 인터페이스만 본다.

## 6. 수명 · 비동기

- 이벤트 구독(`+=`) 시 반드시 `OnDestroy()`/`OnDisable()` 에서 해제(`-=`)한다.
- `static` 변수 추가 시 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` 초기화 메서드를 함께 작성한다. (RULE-01)
- `async` 메서드는 `CancellationToken` 을 반드시 인자로 받는다. (RULE-05)
- 물리 API 호출은 `FixedUpdate()` 에서만. (RULE-04)

## 7. 직렬화 · 문서화

- 인스펙터 노출은 `[SerializeField] private` 이 기본. `public` 필드 금지.
- `Runtime` / `Runtime.Data` 어셈블리의 public API 에는 `///` XML 문서 주석을 단다.
- 주석은 "왜"만 남긴다. 코드가 이미 말하는 "무엇"은 쓰지 않는다.

## 8. 테스트 동반 (TDD 기본값)

새 로직 파일을 만들면 대응하는 EditMode 테스트가 같은 커밋에 있어야 한다 (`CLAUDE.md` §5.4 · §8). 규칙은 [`tests.md`](tests.md).
