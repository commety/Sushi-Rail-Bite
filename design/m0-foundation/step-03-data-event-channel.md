# Step 03: 이벤트 채널 — `SushiEatenEventChannelSO`

- **영역:** `data` — 어셈블리 `Runtime.Data` (+ `Tests.EditMode`)
- **선행 단계:** step-01 완료 필요. step-02 와 **병렬 가능** (파일이 겹치지 않는다)
- **후행 단계:** M1·M2 의 점수 집계와 UI 갱신이 이 채널을 구독한다

---

## 목적

벨트/손님 쪽에서 "초밥이 먹혔다"를 발행하고, 점수·UI 쪽에서 구독한다. 이 채널 하나로 `Runtime → Presentation` 직접 참조를 없앤다 (`CLAUDE.md` §3.3). M0 에서는 **채널 자체만** 만들고, 실제 발행자·구독자는 M1·M2 에서 붙는다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 아래 순서로 수행한다.

### 실행 순서 (TDD — README D1)

1. `SushiEatenPayload` + `SushiEatenEventChannelSO` 의 시그니처만 만든다 (`Raise` 본문은 비워 둔다)
2. 발행/구독 테스트를 쓰고 **Red 확인**
3. 최소 구현으로 Green

### 생성 파일

```
Assets/Code/Scripts/Runtime.Data/Events/SushiEatenPayload.cs          — 생성
Assets/Code/Scripts/Runtime.Data/Events/SushiEatenEventChannelSO.cs   — 생성
Assets/Tests/EditMode/Data/SushiEatenEventChannelTests.cs             — 생성
```

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    /// <summary>
    /// 초밥 1개가 소비됐을 때 실리는 값. 참조 타입을 싣지 않는다 —
    /// Runtime.Data 가 Runtime 을 역참조하게 되기 때문 (작업서 README D2).
    /// </summary>
    [Serializable]
    public readonly struct SushiEatenPayload
    {
        public int Price { get; }
        public int SaturationAmount { get; }
        public int SushiSequenceNumber { get; }
        public int CustomerSequenceNumber { get; }

        public SushiEatenPayload(int price, int saturationAmount,
                                 int sushiSequenceNumber, int customerSequenceNumber);
    }

    [CreateAssetMenu(menuName = "SushiRailBite/Events/Sushi Eaten Channel")]
    public sealed class SushiEatenEventChannelSO : ScriptableObject
    {
        public event Action<SushiEatenPayload> OnRaised;

        public void Raise(in SushiEatenPayload payload);

        /// <summary>도메인 리로드·플레이 재시작 시 남은 구독을 끊는다.</summary>
        public void ClearSubscribers();
    }
}
```

### 선행 산출물 의존성

없음 (step-02 의 SO 타입을 참조하지 않는다 — 페이로드는 원시 값뿐이다).

### 밸런스 수치

없음.

### 테스트 항목 (`Tests.EditMode`)

```
SushiEatenEventChannelTests
  Raise_WithSubscriber_InvokesHandlerOnce
  Raise_AfterUnsubscribe_DoesNotInvokeHandler
  Raise_NoSubscribers_DoesNotThrow
  Raise_MultipleSubscribers_InvokesAll
  Raise_PassesPayloadUnchanged                  ← 가격·SeqNo 가 그대로 전달되는지
  ClearSubscribers_ThenRaise_DoesNotInvokeHandler
```

채널은 `ScriptableObject.CreateInstance<SushiEatenEventChannelSO>()` 로 만든다 (`.claude/rules/tests.md` §4).

### 제약

- **페이로드에 `SushiItem`·`CustomerRuntimeState` 등 `Runtime` 타입을 넣지 않는다.** 어셈블리 역방향 의존이 된다 (README D2)
- `Runtime.Data` 는 `Runtime`·`Presentation` 을 참조하지 않는다
- 구독자는 `OnDestroy`/`OnDisable` 에서 `-=` 해야 한다 — 이 규칙을 채널의 `///` 주석에 명시한다. **SO 는 플레이 종료 후에도 살아 있어** 구독이 남으면 다음 플레이에서 죽은 객체를 부른다 (`.claude/rules/scripts.md` §6)
- `static` 이벤트를 쓰지 않는다. 쓴다면 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 초기화가 필수 (RULE-01 관련) — **이 단계에서는 static 을 쓰지 않는 쪽으로 간다**
- `[InitializeOnLoad]` 신규 추가 금지 (RULE-01)
- public API 에 `///` XML 문서 주석

### 완료 판정

- [ ] `grep -rn "class SushiEatenEventChannelSO" Assets/Code/Scripts/Runtime.Data/` → 1건
- [ ] `grep -rn "SushiItem\|CustomerRuntimeState" Assets/Code/Scripts/Runtime.Data/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → **0건** (D2 위반 없음)
      > 주석 제외 필터의 근거는 [README D7](README.md). 이 검사는 실제로 `///` 주석 3건을 오탐한 전력이 있다.
- [ ] 위 검사는 교차 확인한다 — `Runtime.Data` 의 `using` 에 `SushiDefense.Belt`·`SushiDefense.Customers` 가 없고, `Runtime.Data.asmdef` 의 `"references"` 가 `[]` 인지 본다. 컴파일러가 보는 것은 이쪽이다
- [ ] `grep -rE '\[InitializeOnLoad\]' Assets/Code/Scripts/ --include="*.cs" | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → 0건 (RULE-01)
      > `Assets/` 전체로 잡으면 안 된다. `Assets/Editor/ParallelAgentSetup.cs` · `Assets/Editor/ClaudeBridge/ClaudeBridgeServer.cs` 에 **기존** 하네스용 `[InitializeOnLoad]` 2건이 있고, 이건 정상이다. RULE-01 이 막는 것은 **신규 추가**다.
- [ ] `./tests/run-tests.sh` — 위 테스트 전량 Green
- [ ] `./tests/lint.sh` 통과

### 예상 커밋 메시지

```
feat(data): add sushi eaten event channel
```

---

## 금지 사항

- 채널을 여러 개 만들지 않는다. M0 는 **1종**이다. 필요해 보이면 M1 에서 추가한다.
- 발행자·구독자(벨트·손님·점수·UI)를 이 단계에서 만들지 않는다.
- `.asset` 인스턴스를 만들지 않는다 (씬에 물릴 채널 애셋은 M1 에서 필요해질 때).
