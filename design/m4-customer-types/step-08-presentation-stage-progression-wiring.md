# Step 08: `StageBootstrap` 스테이지 진행 배선

- **영역:** `presentation` — 어셈블리 `Presentation` (+ `Tests.PlayMode`)
- **선행 단계:** step-01 (`RunConfig`) · step-03 (`RunProgression`) · step-05 (프레젠터) · **step-07 완료 필수** (같은 파일)
- **후행 단계:** step-10 이 씬에 `RunConfig` 를 물린다

---

## 목적

세 조각(스테이지 목록 · 진행 판정 · 전환 화면)을 씬 진입점에서 잇는다. 이 단계 뒤에 **클리어 → 보상 → 전환 → 다음 스테이지 → … → 런 종료** 가 실제로 돈다.

M4 에서 가장 무거운 단계이고, 잘못 짜면 두 가지 사고가 난다.

1. **`Build()` 가 자기를 부른 프레젠터를 파괴한다** (README D8)
2. **스테이지 2 를 시작했는데 스테이지 1 설정으로 돈다** — `_stageConfig` 가 두 개의 진실을 갖게 될 때

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 수정 파일

- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정
- `Assets/Tests/PlayMode/StageIntegrationTests.cs` — 수정 (테스트 추가)

### 새 직렬화 필드

```csharp
[SerializeField] private RunConfig _runConfig;
[SerializeField] private StageTransitionView _transitionView;
```

`_stageConfig` 는 **남긴다.** 지우면 목록을 물리지 않은 기존 씬·테스트가 전부 죽는다.

### 새 공개 표면

```csharp
/// 이 런의 진행. Run 과 같은 수명이다.
public RunProgression Progression { get; private set; }

/// 전환 화면의 로직. 화면이 씬에 없으면 null 이다.
public StageTransitionPresenter Transition { get; private set; }

/// 지금 돌고 있는 스테이지. StageConfig 프로퍼티는 이 값을 돌려준다.
public StageConfig ActiveStage { get; private set; }
```

### 계약 1 — 스테이지 목록의 진실은 하나다 (README D5)

```
스테이지 목록 = _runConfig 가 있고 비어 있지 않으면 _runConfig.Stages
              아니면 [_stageConfig] 한 장짜리
```

**"진행이 없는 모드" 라는 두 번째 경로를 만들지 않는다.** 스테이지가 1개인 런이 곧 M3 의 현행 동작이고, 그 경우 첫 클리어가 곧 런 종료다. 분기가 하나 늘 때마다 테스트가 두 배가 된다.

`Build()` 안의 모든 스테이지 참조를 `_stageConfig` 대신 **`ActiveStage`** 로 바꾼다 — 벨트·조율자·배치 서비스·`StageController`·자리 바인딩(step-07) 전부. **`_stageConfig` 를 런타임에 대입하지 않는다**: 직렬화 필드를 런타임에 쓰면 인스펙터에 물린 값이 진실인지 아닌지 읽는 사람이 알 수 없게 된다.

기존 `public StageConfig StageConfig => _stageConfig;` 는 `=> ActiveStage;` 로 바꾼다. 진단·테스트가 이 프로퍼티로 "지금 도는 스테이지" 를 묻고 있으므로 의미가 정확해진다.

### 계약 2 — 런 수명과 스테이지 수명을 나눈다 (README D8)

지금 `Rewards` 는 `Build()` 에서 만들어지고 `Teardown()` 에서 버려진다. 전환 화면의 `Proceed()` 가 `Build()` 를 부르므로, 그대로 두면 **이벤트 발행 도중에 자기 발밑이 무너진다.**

```
런 수명 (한 번만 · Teardown 이 건드리지 않는다)
    Run · Progression · Rewards · Transition

스테이지 수명 (Build 마다 새로)
    Belt · Revenue · Wallet · Coordinator · Placement · Stage
```

`Build()` 구조:

```
Teardown()                  // 스테이지 수명만 정리
ResolveMissingReferences()
EnsureRunScope()            // 런 수명 — 이미 있으면 아무 일도 하지 않는다
ActiveStage = Progression.CurrentStage ?? _stageConfig
... 기존 조립 (ActiveStage 기준) ...
```

`EnsureRunScope()` 안에서:

```
Run 이 이미 있으면 즉시 반환
Run = new RunState(SushiDeck.FromSpawnTable(첫 스테이지), new CustomerDeck(_startingCustomers), NewSeed())
Progression = new RunProgression(스테이지 목록, Run)
BuildRewards()      // Rewards.Closed += OnRewardsClosed
BuildTransition()   // Transition.StageAdvanced += ... / RunCompleted += ...
```

> **덱은 첫 스테이지의 `SpawnTable` 에서만 온다** (README D6). 스테이지 2·3 의 `_spawnTable` 은 읽히지 않으며 step-10 에서 **비워 둔다**.

`Teardown()` 에서 **`Rewards = null` 을 제거한다.** 대신 `OnDestroy` 경로에서 런 수명 객체의 구독을 해제할 지점을 따로 둔다 (`ReleaseRunScope()`), 그렇지 않으면 §6(수명·비동기) 위반이다.

### 계약 3 — 흐름

```
StageController.OutcomeDecided(Cleared)
    → Rewards?.Open(Run)                 (기존)
    → 보상 화면이 닫히면 Rewards.Closed
        → Transition?.Open()
        → 확인 입력 → Transition.Proceed()
            → StageAdvanced(next)  → ActiveStage 갱신은 Build() 가 한다 → Build()
            → RunCompleted         → 아무것도 하지 않는다 (화면이 이미 종료를 표시했다)
```

**`Rewards` 나 `Transition` 이 씬에 없으면 흐름이 거기서 멈춘다.** 지금도 보상은 그렇게 동작한다 — 없다고 판이 못 서면 씬을 조금씩 조립하는 동안 아무것도 못 돌린다.

**`RunCompleted` 에서 `Build()` 를 부르지 않는다.** 넘어갈 스테이지가 없다. 재시작은 메인화면(M6)이다.

### 계약 4 — 실패 경로는 건드리지 않는다

`Retry()` 는 그대로다. 실패는 전환 화면을 열지 않는다 — 같은 스테이지를 다시 하는 것이지 넘어가는 것이 아니다 (M3 착수 시 확정).

### 테스트 목록 (`StageIntegrationTests` 에 추가)

```
목록  Build_NoRunConfig_TreatsSingleStageAsRun       ← D5. ActiveStage 가 _stageConfig
      Build_RunConfigWithThreeStages_StartsAtFirst

진행  Clear_MidRunStage_OpensTransitionAfterRewardsClosed
      Proceed_MidRun_RebuildsWithNextStageConfig     ← ActiveStage 가 **2번째 인스턴스**인지 AreSame
      Proceed_MidRun_KeepsRunDeckAndRoster           ← 덱·명부가 살아남는다
      Proceed_MidRun_ResetsRevenueAndWallet          ← 매출·지갑은 새로 열린다
      Proceed_LastStage_DoesNotRebuild               ← ActiveStage 가 그대로

수명  Proceed_MidRun_KeepsSameRunStateInstance       ← AreSame
      Proceed_MidRun_KeepsSameRewardsPresenter       ← D8. Build 가 프레젠터를 죽이지 않는다
      Proceed_Twice_DoesNotThrow                     ← 재진입 사고 회귀 방지
```

### 공허하게 통과하지 않게

- **`Proceed_MidRun_RebuildsWithNextStageConfig` 는 두 스테이지에 서로 다른 값**(목표 매출·자리 수)을 넣고 `AreSame` 으로 인스턴스를 확인한다. 같은 값이면 갈아 끼우지 않는 구현도 통과한다.
- **`..._ResetsRevenueAndWallet` 은 먼저 매출을 0 이 아니게 만든다.** 처음부터 0 이면 아무것도 검증하지 않는다.
- **`..._KeepsRunDeckAndRoster` 는 진행 전에 보상으로 카드를 한 장 넣는다.** 시작 덱만으로 비교하면 `SushiDeck.FromSpawnTable` 을 다시 부르는 구현도 통과한다 — **이것이 D6 이 실제로 지켜지는지 보는 유일한 테스트다.**
- **`Proceed_LastStage_DoesNotRebuild` 는 마지막 스테이지에서 벨트 인스턴스가 그대로인지도 함께 본다.** `ActiveStage` 만 보면 `Build()` 를 부르고도 같은 설정을 다시 물리는 구현이 통과한다.

### 선행 산출물 의존성

- `SushiDefense.Data.RunConfig` — step-01
- `SushiDefense.Run.RunProgression` — step-03
- `SushiDefense.UI.StageTransitionPresenter` — step-05
- `SushiDefense.UI.StageTransitionView` — step-06
- step-07 의 자리 바인딩 — **같은 파일이므로 먼저 머지돼 있어야 한다**

### 밸런스 수치

없음.

### 제약

- 이벤트 구독은 반드시 짝지어 해제한다 (`OnDestroy`) — [`rules/scripts.md`](../../.claude/rules/scripts.md) §6
- `Find` / `FindObjectOfType` 금지. `ResolveMissingReferences()` 는 **자기 하위 계층만** 본다 (기존 방식 유지)
- `NewSeed()` 는 여전히 **한 곳뿐**이어야 한다. 전역 난수가 허용되는 유일한 지점이다 ([`stage-and-run.md`](../../.claude/domain/stage-and-run.md) §6)
- `Update()` 는 `Stage?.Tick(Time.deltaTime)` 하나를 유지한다. 조율자를 직접 틱하지 않는다 (M3 정지 계약)
- `Runtime` 을 수정하지 않는다

### 완료 판정

- [ ] `grep -n "_stageConfig" Assets/Code/Scripts/Presentation/StageBootstrap.cs` 의 등장이 **직렬화 선언 · `Initialize` · 목록 폴백 · `ActiveStage` 폴백** 네 곳뿐 (조립 경로에서 사라졌다)
- [ ] `grep -n "Rewards = null" Assets/Code/Scripts/Presentation/StageBootstrap.cs` 가 `Teardown()` 안에는 **0건**
- [ ] `./tests/run-tests.sh all` 전량 Green — 기존 PlayMode 테스트가 하나도 안 죽어야 한다
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 주입 검증 (구현 후 반드시)

| 주입 | 실패해야 하는 테스트 |
|---|---|
| `Teardown()` 에 `Rewards = null` 복원 | `Proceed_MidRun_KeepsSameRewardsPresenter` · `Proceed_Twice_DoesNotThrow` |
| `EnsureRunScope()` 의 조기 반환 제거 | `Proceed_MidRun_KeepsSameRunStateInstance` · `..._KeepsRunDeckAndRoster` |
| `ActiveStage` 를 항상 첫 스테이지로 | `Proceed_MidRun_RebuildsWithNextStageConfig` |
| `RunCompleted` 에서도 `Build()` 호출 | `Proceed_LastStage_DoesNotRebuild` |

전량 통과하는 주입이 나오면 되돌리고 **테스트를 먼저 추가한 뒤 보고**한다.

### 예상 커밋 메시지

```
feat(stage): advance through the run's stages after each clear
```

---

## 금지 사항

- 씬을 편집하지 않는다 (step-10).
- `RunState` · `RunProgression` · 프레젠터의 시그니처를 바꾸지 않는다. 필요하면 **멈추고 보고**한 뒤 계획을 갱신한다.
- 실패 경로(`Retry`)의 동작을 바꾸지 않는다.
- 밸런스 애셋을 만들거나 고치지 않는다.
