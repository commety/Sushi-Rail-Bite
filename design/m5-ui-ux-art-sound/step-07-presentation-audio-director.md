# Step 07: `AudioDirector` 배선 + 자동재생 게이트

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-02 (`SoundBudget`·`AudioUnlockGate`), step-06 (`AudioBank.asset`)
- **후행 단계:** step-11 이 씬에서 확인한다

---

## 목적

로직이 이미 내고 있는 사건을 소리로 옮긴다. **판정은 전부 앞 단계에 있다** — 이 컴포넌트는 구독하고, 예산에 물어보고, `AudioSource` 를 부른다.

여기가 M5 에서 **수명 관리 실수가 가장 나기 쉬운 지점**이다. `ClaimCoordinator` 는 `MonoBehaviour` 가 아니라 씬이 내려가도 살아 있을 수 있고, 스테이지가 넘어갈 때 `StageBootstrap.Build()` 가 다시 불린다. 구독을 떼지 않으면 **파괴된 뷰를 계속 부르거나, 한 번의 먹힘이 두 번 재생된다** (`StageHudView.Unbind` 가 같은 이유로 존재한다).

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Audio/AudioDirector.cs` — 생성
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (배선)
- `Assets/Tests/PlayMode/Audio/AudioDirectorTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.Audio
{
    /// <summary>
    /// 로직의 사건을 소리로 옮긴다. <b>판정하지 않는다</b> — 겹침 여부는
    /// <see cref="SoundBudget"/>, 재생 가능 여부는 <see cref="AudioUnlockGate"/> 가 정한다.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] private AudioBankSO _bank;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _bgmSource;

        /// <summary>지금까지 실제로 재생한 횟수. 검증용이다.</summary>
        public int PlayedCount { get; }

        /// <summary>예산·게이트에 막혀 버려진 요청 수. 검증용이다.</summary>
        public int SuppressedCount { get; }

        /// <summary>BGM 이 지금 울리고 있는가. 검증용이다.</summary>
        public bool IsBgmPlaying { get; }

        /// <summary>씬 진입점이 물려 준다. 이미 물려 있으면 먼저 끊는다.</summary>
        public void Bind(ClaimCoordinator coordinator, CustomerPlacementService placement,
                         StageController stage, RewardSelectionPresenter rewards,
                         StageTransitionPresenter transition);

        public void Unbind();

        /// <summary>첫 사용자 입력에서 호출한다. 자동재생 정책 해제 지점이다.</summary>
        public void NotifyUserInput();
    }
}
```

`PlayedCount` / `SuppressedCount` 를 노출하는 이유: **PlayMode 테스트가 실제 소리를 들을 수 없다.** 헤드리스 배치 모드에는 오디오 장치가 없다. 재생 여부를 검증하려면 카운터가 필요하다 — `StageHudView` 가 `RevenueText` 를 노출하는 것과 같은 이유다.

### 구독 대상

| 사건 | 출처 | 큐 |
|---|---|---|
| 초밥이 먹혔다 | `ClaimCoordinator.SushiEaten` | `SushiEaten` |
| 손님을 앉혔다 | `CustomerPlacementService` | `CustomerPlaced` |
| 보상을 골랐다 | `RewardSelectionPresenter` | `RewardPicked` |
| 다음 스테이지로 | `StageTransitionPresenter.StageAdvanced` | `StageAdvanced` |
| 클리어 / 실패 | `StageController.Outcome` 변화 | `StageCleared` / `StageFailed` |

**`SushiEatenEventChannelSO` 를 쓰지 않는다** (README D5). 채널은 정의만 있고 프로덕션 사용처가 0건이며, 지금 필요한 정보는 `StageBootstrap` 이 이미 붙잡고 있는 `ClaimCoordinator` 에서 그대로 나온다. 지금 채널을 끼우면 같은 사실을 알리는 경로가 두 벌이 된다.

`CustomerPlacementService` 와 `StageController` 에 변경 이벤트가 없으면 **`StageHudView` 와 같은 방식으로** 값 변화를 감지한다 (매 프레임 확인하되 **바뀐 프레임에만** 반응). `Runtime` 에 이벤트를 새로 뚫는 것은 이 단계의 범위 밖이다 — 필요하다고 판단되면 멈추고 보고한다.

### 자동재생 정책 (README D10)

WebGL 은 사용자 제스처 전까지 오디오 컨텍스트가 잠긴다. `Start()` 에서 BGM 을 재생하면 **무음으로 시작해 영영 들리지 않는다** — 재생이 밀리는 게 아니라 이미 지나가 버린다.

```
Awake/Start        → 아무것도 재생하지 않는다
첫 입력 감지       → NotifyUserInput() → AudioUnlockGate.Unlock()
그 직후 한 번      → TryConsumeUnlockMoment() 가 true → BGM 시작
이후               → SFX 는 SoundBudget 판정을 거쳐 재생
```

첫 입력을 무엇으로 볼 것인가: 이 씬에는 이미 손님 배치 클릭과 보상/전환의 Enter 입력이 있다. **입력을 새로 만들지 않고** 기존 입력 지점에서 `NotifyUserInput()` 을 부른다.

### 선행 산출물 의존성

- `SushiDefense.Audio.SoundBudget` · `AudioUnlockGate` (step-02)
- `SushiDefense.Data.AudioBankSO` (step-01), `AudioBank.asset` (step-06)

### 밸런스 수치

전부 `AudioBankSO` 에서 읽는다. **코드 상수 금지** (`CLAUDE.md` §3.1). 쿨다운·볼륨·상한 중 어느 하나라도 이 파일에 숫자로 나타나면 위반이다.

### 제약

- **구독은 `Unbind()`/`OnDestroy()` 에서 반드시 해제한다** (`scripts.md` §6). `Bind` 는 먼저 `Unbind` 를 부른다 — `Build()` 가 두 번 불리면 구독이 겹쳐 한 번의 먹힘이 두 번 재생된다
- **`Update` 경로에 할당을 만들지 않는다** (§4). 문자열 결합·LINQ·`new` 금지
- `Time.time` 은 여기서 읽고 `SoundBudget` 에 **인자로 넘긴다.** 예산이 시계를 갖게 하지 않는다
- 뱅크·소스가 `null` 이어도 죽지 않는다. **소리는 로직의 전제 조건이 아니다** — `PlaceholderLabel.Write` 가 라벨이 없어도 조용히 넘어가는 것과 같은 원칙이다
- `FindObjectOfType` 금지 (§4.3). 참조는 인스펙터 또는 `StageBootstrap` 주입
- `AudioSource` 를 두 개로 나눈다 — BGM 은 루프·독립 볼륨이고 SFX 는 `PlayOneShot` 이다. 하나로 합치면 BGM 이 SFX 마다 끊긴다

### `StageBootstrap` 수정

- `[SerializeField] private AudioDirector _audioDirector;`
- `ResolveMissingReferences` 에 추가 — 씬 조립에서 참조를 일일이 물리지 않아도 되게 (기존 패턴 그대로)
- **수명은 런 스코프다.** `Rewards`·`Transition` 과 같은 자리에 둔다 — `Build()` 마다 새로 만들면 스테이지 전환 도중 BGM 이 끊기고 게이트가 다시 잠긴다
- `Teardown()` 에서 `Unbind()` 를 부르되 객체를 파괴하지 않는다

### 테스트 계획

**PlayMode 다** — `MonoBehaviour` 와 `AudioSource` 가 필요하다.

```
재생      SushiEaten_Once_IncrementsPlayedCount
          SushiEaten_FourInSameFrame_SuppressesSome    ← 겹침 제어가 실제로 걸린다
          StageCleared_Once_PlaysClearCue
게이트    Bind_BeforeUserInput_DoesNotStartBgm         ← 자동재생 정책
          NotifyUserInput_First_StartsBgm
          NotifyUserInput_Twice_DoesNotRestartBgm
수명      Bind_Twice_DoesNotDoublePlay                 ← 구독 중복 방지
          Unbind_ThenEvent_DoesNotPlay
견고성    Bind_WithoutBank_DoesNotThrow                ← 소리는 전제 조건이 아니다
```

`SushiEaten_FourInSameFrame_SuppressesSome` 은 **구체값을 박는다** — 쿨다운 `0.06`, 상한 `6` 에서 같은 프레임 4건이면 몇 건이 통과해야 하는지 계산해 그 수를 단언한다. *"일부가 막혔다"* 만 보면 전부 막는 구현에서도 통과한다 (`tests.md` §3).

### 주입으로 확인한다

| 주입 | 잡혀야 할 테스트 (가설) |
|---|---|
| `Bind` 에서 선행 `Unbind` 제거 | `Bind_Twice_DoesNotDoublePlay` |
| `Start()` 에서 BGM 즉시 재생 | `Bind_BeforeUserInput_DoesNotStartBgm` |
| `SoundBudget` 판정 무시하고 항상 재생 | `SushiEaten_FourInSameFrame_SuppressesSome` |

**예측은 가설이다.** 실측으로 정정한다.

### 완료 판정

- [ ] `grep -rn "SushiEatenEventChannel" Assets/Code/Scripts/Presentation/` 가 **0건** (D5 확인)
- [ ] `grep -nE "[0-9]+\.[0-9]+f" Assets/Code/Scripts/Presentation/Audio/AudioDirector.cs` 에 **밸런스 수치가 없다** (쿨다운·볼륨은 전부 SO 에서)
- [ ] `grep -c "OnDestroy\|Unbind" Assets/Code/Scripts/Presentation/Audio/AudioDirector.cs` ≥ 2 — 해제 경로 존재
- [ ] `./tests/run-tests.sh all` Green
- [ ] 주입 3건 실측 후 표 정정
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(audio): play cues for gameplay events
```

---

## 금지 사항

- `SushiEatenEventChannelSO` 를 배선하지 않는다 (D5)
- `Runtime` 에 새 이벤트를 뚫지 않는다. 필요하다고 판단되면 **멈추고 보고**한다
- 쿨다운·볼륨을 코드에 쓰지 않는다
- `AudioListener` 를 추가하지 않는다 — Main Camera 에 이미 있다. 둘이 되면 소리가 두 번 들린다
- 다른 어셈블리 파일을 수정하지 않는다
