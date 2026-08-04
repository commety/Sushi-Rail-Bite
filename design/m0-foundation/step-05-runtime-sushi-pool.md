# Step 05: 오브젝트 풀 (순수 로직) — `SushiPool<T>` · `ISushiInstanceFactory`

- **영역:** `runtime` — 어셈블리 `Runtime` (+ `Tests.EditMode`)
- **선행 단계:** step-01 완료 필요. step-04 와 **병렬 가능**
- **후행 단계:** step-06 이 `ISushiInstanceFactory` 를 `MonoBehaviour` 로 구현한다. M1 의 벨트가 이 풀로만 초밥을 얻는다

---

## 목적

`Instantiate`/`Destroy` 직접 호출을 프로덕션 코드에서 없앤다 (`CLAUDE.md` §3.4). WebGL 타깃이라 GC 스파이크가 그대로 프레임 히칭이 되고, 벨트는 초밥을 계속 만들고 버리는 구조라 풀이 없으면 반드시 문제가 된다.

**생성 자체는 이 클래스가 하지 않는다.** 생성은 주입된 팩토리에 위임한다 — 그래야 이 로직을 `MonoBehaviour` 없이 EditMode 로 검증할 수 있다 (작업서 README D3).

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD — README D1)

1. `ISushiInstanceFactory` + `SushiPool<T>` 시그니처를 만든다
2. 손으로 쓴 페이크 팩토리(생성 횟수를 세는)를 두고 테스트를 쓴 뒤 **Red 확인**
3. 최소 구현으로 Green

### 생성 파일

```
Assets/Code/Scripts/Runtime/Belt/ISushiInstanceFactory.cs   — 생성
Assets/Code/Scripts/Runtime/Belt/SushiPool.cs               — 생성
Assets/Tests/EditMode/Belt/SushiPoolTests.cs                — 생성
Assets/Tests/EditMode/Belt/FakeSushiInstanceFactory.cs      — 생성 (테스트 더블, 손으로 작성)
```

### 핵심 심볼

```csharp
namespace SushiDefense.Belt
{
    /// <summary>
    /// 풀이 채울 인스턴스를 만드는 쪽. 구현은 Presentation 에 있고(step-06),
    /// Runtime 은 Unity 오브젝트 생성을 모른 채로 남는다.
    /// </summary>
    public interface ISushiInstanceFactory<T>
    {
        T Create();
        void Dispose(T instance);   // 풀 정리 시에만 호출. 대여/반납 경로에서는 부르지 않는다
    }

    /// <summary>
    /// 초밥 인스턴스 풀. 반납된 인스턴스를 먼저 재사용하고, 없을 때만 팩토리로 새로 만든다.
    /// </summary>
    public sealed class SushiPool<T>
    {
        public int CountInactive { get; }   // 대여 대기 중
        public int CountActive { get; }     // 대여 중
        public int CountAll { get; }

        public SushiPool(ISushiInstanceFactory<T> factory, int prewarmCount = 0);

        public T Rent();
        public void Return(T instance);
        public void Clear();                // 전부 Dispose
    }
}
```

- **`Return` 을 두 번 부르면 예외**로 알린다 (조용히 넘기면 같은 인스턴스가 두 번 대여되어 화면에 초밥이 겹친다). 이건 테스트로 고정한다
- `prewarmCount` 로 스테이지 시작 시 미리 채운다 — **값은 호출자가 준다.** 풀 안에 기본 개수 상수를 두지 않는다 (`CLAUDE.md` §3.1)
- `Rent`/`Return` 경로에서 **할당을 만들지 않는다.** 내부 자료구조는 `Stack<T>`/`List<T>` 를 생성자에서 한 번만 잡는다 (§4.3, WebGL)

### 선행 산출물 의존성

없음. `SushiItem`(step-04)에 의존하지 않도록 **제네릭**으로 둔다 — 그래야 step-06 이 `GameObject`/`SushiItemView` 를 담을 수 있다.

### 밸런스 수치

없음. 프리웜 개수·최대 개수는 호출자(M1 의 벨트)가 `StageConfig` 에서 읽어 넘긴다.

### 테스트 항목 (`Tests.EditMode`)

```
SushiPoolTests
  Rent_EmptyPool_CreatesNewInstance
  Rent_AfterReturn_ReusesReturnedInstance          ← M0 완료 판정의 핵심
  Rent_AfterReturn_DoesNotCallFactoryAgain         ← 팩토리 호출 횟수로 검증
  Rent_Twice_ReturnsDistinctInstances
  Return_UnknownInstance_Throws
  Return_SameInstanceTwice_Throws
  Prewarm_WithCount_CreatesThatManyUpFront
  Clear_AfterPrewarm_DisposesAll
  CountActive_AfterRentAndReturn_TracksCorrectly
```

`FakeSushiInstanceFactory` 는 손으로 쓴다 — 생성 횟수·Dispose 횟수·발급한 인스턴스 목록을 노출한다. **NSubstitute 등 새 패키지를 추가하지 않는다** (`CLAUDE.md` §7).

### 제약

- **`Instantiate`/`Destroy`/`UnityEngine.Object` 를 이 파일들에서 쓰지 않는다.** 전부 팩토리 뒤에 있다
- `MonoBehaviour` 를 만들지 않는다
- `Runtime` 은 `Presentation` 을 참조하지 않는다
- `Unity.Pool.ObjectPool<T>` 를 쓰지 않는다 — Unity API 의존이 붙어 EditMode 테스트가 무거워지고, `Return` 중복 검출 같은 이 프로젝트의 요구를 직접 표현하기 어렵다. **직접 구현한다**
- public API 에 `///` XML 문서 주석

### 완료 판정

- [ ] `grep -rn "class SushiPool" Assets/Code/Scripts/Runtime/Belt/` → 1건
- [ ] `grep -rn "Instantiate\|Destroy\|MonoBehaviour" Assets/Code/Scripts/Runtime/Belt/SushiPool.cs Assets/Code/Scripts/Runtime/Belt/ISushiInstanceFactory.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건**
      > 주석 제외 필터의 근거는 [README D7](README.md). 이 두 파일의 `///` 주석은 "생성은 팩토리에 위임한다 — 여기서 `Instantiate` 를 부르지 않는다" 를 설명하게 되어 있어 필터 없이는 반드시 걸린다.
- [ ] `./tests/run-tests.sh` — 위 테스트 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(belt): add sushi object pool with injected instance factory
```

---

## 금지 사항

- 벨트 이동·스폰 타이밍을 구현하지 않는다. **언제 빌리고 언제 반납하는지는 M1 이 정한다** (플랜 Q2·Q3 미결).
- `Presentation` 파일을 만들지 않는다. step-06 의 몫이다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다 — step-06 이 그대로 구현한다. 바꿔야 하면 멈추고 이 작업서를 갱신한다.
