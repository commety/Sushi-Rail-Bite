# Step 07: 남은 시간·결과 표시 + 부트스트랩 조립

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-03 (`StageController`) · step-05 (`RunState`). **둘 다 필요하다**
- **후행 단계:** step-08 이 결과 표시 옆에 보상 화면을 붙인다

---

## 목적

여기서 M3 가 **화면에 나타난다.** 지금까지의 순수 로직을 씬에 물리고, 남은 시간과 클리어/실패를 보여 주고, 실패했을 때 다시 시작할 수 있게 한다.

동시에 `StageBootstrap.Update()` 가 **더 이상 조율자를 직접 틱하지 않게** 된다 — 그게 step-03 이 컨트롤러를 만든 이유다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (조립·틱·재시도)
- `Assets/Code/Scripts/Presentation/UI/StageHudView.cs` — 수정 (남은 시간·결과)
- `Assets/Tests/PlayMode/UI/StageHudViewTests.cs` — 수정
- `Assets/Tests/PlayMode/StageIntegrationTests.cs` — 수정 (또는 새 케이스 추가)

### `StageBootstrap` 의 변화

```csharp
/// <summary>이 런의 기억. <b><see cref="Build"/> 바깥에 산다</b> — 재시도해도 덱이 남는다.</summary>
public RunState Run { get; private set; }

/// <summary>이 스테이지의 진행. 시간을 흘리고 결과를 판정한다.</summary>
public StageController Stage { get; private set; }

/// <summary>같은 스테이지를 다시 시작한다. 덱·명부는 유지되고 나머지는 새로 열린다.</summary>
public void Retry();
```

조립 순서:

```csharp
// 런은 한 번만 만든다. Build() 를 다시 불러도 살아남아야 재시도에 덱이 유지된다.
Run ??= new RunState(SushiDeck.FromSpawnTable(_stageConfig), new CustomerDeck(_startingCustomers), NewSeed());

Belt = new SushiBelt(_stageConfig, Run.Sushi.Cards, new SequenceNumberIssuer(), …);
Revenue = new RevenueLedger();
Wallet = new RecruitWallet(_stageConfig.InitialRecruitBudget);
Coordinator = new ClaimCoordinator(Belt, _stageConfig, Revenue, Wallet);
Placement = new CustomerPlacementService(Coordinator, _stageConfig, new SequenceNumberIssuer(), Wallet);
Stage = new StageController(Coordinator, Revenue, _stageConfig);
Stage.OutcomeDecided += OnOutcomeDecided;
```

```csharp
private void Update()
{
    Stage?.Tick(Time.deltaTime);   // Coordinator.Tick 을 직접 부르지 않는다
}
```

`Retry()` 는 `Run.RecordFailedAttempt()` 를 부른 뒤 `Build()` 를 다시 부른다. `Build()` 가 이미 `Teardown()` 으로 시작하므로 **정리 코드를 새로 쓰지 않는다.**

`Teardown()` 에 `Stage.OutcomeDecided -= OnOutcomeDecided;` 를 **반드시** 넣는다 (`.claude/rules/scripts.md` §6). 빠뜨리면 `Build()` 를 두 번 부를 때 결과가 두 번 처리된다.

### 시드는 어디서 오나

`RunState` 는 시드를 **받는다.** `StageBootstrap` 이 만들며, 여기는 `Presentation` 이라 `UnityEngine.Random` 을 써도 가드에 걸리지 않는다. 그럼에도 **한 줄짜리 private 메서드로 격리**하고, "런마다 달라지는 유일한 지점" 임을 주석에 적는다 — 테스트에서 시드를 고정하고 싶어질 때 갈아 끼울 자리가 된다.

### `_defaultCustomer` → 시작 명부

`_defaultCustomer`(단일 `CustomerData`)를 `_startingCustomers`(`CustomerData[]`)로 바꾼다. 보상으로 손님을 영입해도 배치할 수 없으면 그 보상이 화면에서 아무 일도 하지 않기 때문이다.

`CustomerPlacementController` 는:

```csharp
/// <summary>배치할 손님 목록. 런이 자라면 여기도 자란다.</summary>
public void BindRoster(IReadOnlyList<CustomerData> roster);

/// <summary>다음에 앉힐 손님을 고른다. 범위를 벗어나면 아무 일도 하지 않는다.</summary>
public void SelectPending(int index);

/// <summary>지금 고른 손님. 명부가 비었으면 <c>null</c>.</summary>
public CustomerData PendingCustomer { get; }
```

**고르는 UI 는 만들지 않는다.** `SelectPending` 을 공개 진입점으로 두고, HUD 에 현재 선택을 한 줄 표시하는 데서 멈춘다 — 손님 선택 UI 는 M6(메인화면·덱빌딩)이다. 여기서 만들면 M6 에서 버린다.

### `StageHudView` 의 추가분

```csharp
/// <summary>지금 표시 중인 남은 시간 문구.</summary>
public string TimeText { get; private set; }

/// <summary>지금 표시 중인 결과 문구. 진행 중이면 빈 문자열이다.</summary>
public string OutcomeText { get; private set; }

/// <summary>지금 표시 중인 배치 예정 손님 문구.</summary>
public string PendingCustomerText { get; private set; }
```

`Bind` 시그니처에 `StageController` 가 추가된다. **인자가 여섯 개를 넘어가므로** 여기서 한 번 정리할지 판단하고, 정리한다면 그 사실을 보고한다 (묶는 것 자체는 이 단계 범위 안이다).

**매 프레임 문자열을 만들지 않는다** (§4.3 — WebGL 에서 GC 스파이크가 곧 히칭이다). 이미 있는 `_shownPlacedCount` · `_shownWaitingCount` 패턴을 따라 **초 단위로 잘라** 변경 감지한다:

```csharp
var seconds = Mathf.CeilToInt(_stage.RemainingSeconds);
if (seconds == _shownSeconds) return;
```

`Mathf.CeilToInt` 인 이유: 남은 시간이 0.3초일 때 `0` 이 아니라 `1` 로 보이는 편이 낫다. 실제로 0 이 되는 순간은 만료 순간뿐이다.

### 선행 산출물 의존성

- `StageController` · `StageOutcome` — step-03
- `RunState` · `CustomerDeck` · `SushiDeck` — step-04 · step-05

### 밸런스 수치

**없다.** 제한 시간·목표 매출은 `StageConfig` 에서 컨트롤러를 거쳐 온다. 표시 문자열의 서식은 밸런스가 아니다.

### 제약

- **뷰가 계산하지 않는다** (`CLAUDE.md` §3.2). 남은 시간도 클리어 여부도 컨트롤러가 이미 정한 값을 문자열로 옮길 뿐이다
- `Find` / `FindObjectOfType` 금지. 기존 `ResolveMissingReferences` 방식(자기 하위 계층 이름 조회)을 따른다
- 이벤트 구독은 `Unbind` / `Teardown` / `OnDestroy` 에서 반드시 해제한다 (RULE-05 계열, `.claude/rules/scripts.md` §6)
- `Update` 안에서 할당(문자열 결합·LINQ·`new`)을 만들지 않는다
- `Runtime` 을 수정하지 않는다. 이 단계는 `Presentation` 전용이다
- `Assets/Art/` · `Assets/Settings/` 에 파일을 만들지 않는다 (RULE-02). 새 프리팹이 필요하면 `Assets/Code/Scripts/Presentation/` 옆에 둔다 (`.claude/rules/parallel-work.md` §2)
- 씬 편집은 **한 번에 한 워크트리만**. 이 단계부터 step-09 까지 직렬이다

### 완료 판정

- [ ] `grep -n "Coordinator.Tick\|Coordinator?.Tick" Assets/Code/Scripts/Presentation/StageBootstrap.cs` — **0건** (컨트롤러만 틱한다)
- [ ] `grep -n "OutcomeDecided" Assets/Code/Scripts/Presentation/StageBootstrap.cs` — `+=` 와 `-=` 가 **짝을 이룬다**
- [ ] `grep -n "Run ??=\|Run =" Assets/Code/Scripts/Presentation/StageBootstrap.cs` — `Run` 이 `Teardown` 에서 **null 로 지워지지 않는다** (지우면 재시도에 덱이 사라진다)
- [ ] `git diff --stat Assets/Code/Scripts/Runtime/ Assets/Code/Scripts/Runtime.Data/` — **0줄**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS
- [ ] `./scripts/run.sh webgl` 빌드 성공 후 `git status --short` 가 **깨끗하다**

### 테스트 이름

```
StageHudViewTests (PlayMode)
  TimeText_AfterTick_CountsDown
  TimeText_SameSecondTwice_DoesNotRewrite            ← 할당 억제
  OutcomeText_InProgress_IsEmpty
  OutcomeText_Cleared_ShowsClear
  OutcomeText_Failed_ShowsFail
  PendingCustomerText_RosterSelected_ShowsThatCustomer

StageIntegrationTests (PlayMode)
  Play_TargetReached_StageStopsAndRevenueFreezes     ← ★ 정지가 씬에서도 성립한다
  Play_Retry_KeepsDeckAndResetsRevenue               ← ★ D6
  Play_Retry_IncrementsAttemptAndKeepsStageNumber
```

> **★ `Play_Retry_KeepsDeckAndResetsRevenue`** 가 D6 의 전부다. **덱은 남고 매출은 0** 이라는 두 가지를 한 테스트에서 함께 확인한다 — 하나만 보면 런 전체를 새로 만드는 구현도, 아무것도 리셋하지 않는 구현도 통과한다.
>
> `TimeText_SameSecondTwice_DoesNotRewrite` 는 문자열 인스턴스를 비교(`ReferenceEquals`)하거나 라벨 쓰기 횟수를 세서 확인한다. 문구가 같은지만 보면 매 프레임 새로 만드는 구현도 통과한다.
>
> PlayMode 는 느리다. **씬·프레임이 실제로 필요한 것만** 여기 둔다 (`.claude/rules/tests.md` §1).

### 예상 커밋 메시지

```
feat(ui): show remaining time and stage outcome, and allow retry
```

---

## 금지 사항

- `StageBootstrap.Update` 에서 `Coordinator.Tick` 을 부르지 않는다. 컨트롤러를 우회하면 정지 계약이 무너진다
- 판정·시간 계산을 뷰에 복제하지 않는다
- 손님 선택 UI 를 만들지 않는다. `SelectPending` 진입점까지가 이 단계다 — 화면은 M6
- 보상 화면을 만들지 않는다. step-08 이다
- `Runtime` · `Runtime.Data` 를 수정하지 않는다. 시그니처가 모자라면 멈추고 보고한다
- `ProjectSettings/` · `Packages/` · 심링크 공유 폴더를 건드리지 않는다 (RULE-02 · §7)
