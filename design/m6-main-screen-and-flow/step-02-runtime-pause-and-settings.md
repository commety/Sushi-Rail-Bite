# Step 02: 일시정지 게이트 · 설정 모델 · 포화도 계산

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** 없음
- **후행 단계:** step-05(포화도 바) · step-07(Menu) · step-10(설정)

---

## 목적

M6 이 화면에 붙이려는 것 셋의 **판정 부분**을 먼저 순수 C# 으로 뽑는다. 셋 다 `MonoBehaviour` 안에 넣으면 EditMode 로 검증할 수 없고, 특히 일시정지는 **"멈췄다"를 프레임을 세어 확인**하게 되어 느리고 불안정한 PlayMode 테스트가 된다.

세 객체는 서로를 모른다. 한 단계에 묶은 이유는 어셈블리가 같고 셋 다 작기 때문이지, 관련이 있어서가 아니다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Stages/PauseState.cs` — 생성
- `Assets/Code/Scripts/Runtime/Settings/GameSettings.cs` — 생성
- `Assets/Code/Scripts/Runtime/Settings/ISettingsStore.cs` — 생성
- `Assets/Code/Scripts/Runtime/Customers/SaturationGauge.cs` — 생성
- `Assets/Tests/EditMode/Stages/PauseStateTests.cs` — 생성
- `Assets/Tests/EditMode/Settings/GameSettingsTests.cs` — 생성
- `Assets/Tests/EditMode/Customers/SaturationGaugeTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.Stages
{
    /// <summary>이 판이 멈춰 있나. 시간을 흘릴지 말지의 유일한 진실이다.</summary>
    public sealed class PauseState
    {
        public bool IsPaused { get; }
        public event Action<bool> Changed;   // 값이 실제로 바뀐 때만 발생한다
        public void Pause();
        public void Resume();
    }
}

namespace SushiDefense.Settings
{
    /// <summary>플레이어가 고른 설정. Unity 를 모른다 — 적용도 저장도 다른 곳이 한다.</summary>
    public sealed class GameSettings
    {
        public float MasterVolume { get; }   // 0~1 로 잘린다
        public bool Fullscreen { get; }
        public event Action Changed;
        public void SetMasterVolume(float value);
        public void SetFullscreen(bool value);
    }

    /// <summary>설정을 어딘가에 남기고 다시 읽어 온다. 어디인지는 Presentation 이 정한다.</summary>
    public interface ISettingsStore
    {
        void Load(GameSettings into);
        void Save(GameSettings from);
    }
}

namespace SushiDefense.Customers
{
    /// <summary>포화도를 몇 칸으로 그릴지. 계산만 하고 그리지 않는다.</summary>
    public static class SaturationGauge
    {
        public static int VisibleCells(int maxSaturation, int cellCapacity);
        public static int FilledCells(int currentSaturation, int maxSaturation, int cellCapacity);
    }
}
```

### 세 객체의 계약 — 틀리기 쉬운 지점

#### `PauseState` — 값이 바뀐 때만 알린다

`Pause()` 를 두 번 불러도 `Changed` 는 한 번이다. 이 가드가 없으면 구독자(오디오 덕킹·버튼 아이콘)가 같은 전이를 여러 번 처리한다. `StageController.OutcomeDecided` 가 조기 반환으로 같은 것을 지키는 방식과 같다 — **별도의 발행 플래그를 두지 않는다.**

**결과가 난 판을 일시정지할 수 있는가**는 이 클래스가 답하지 않는다. `StageBootstrap` 이 `Tick` 을 건너뛸 뿐이고, 이미 끝난 판은 `StageController` 가 스스로 아무것도 하지 않는다 — **두 겹이 필요 없다.**

#### `GameSettings` — 클램프는 여기 한 곳

`SetMasterVolume(1.5f)` 는 예외가 아니라 `1.0` 이다. 설정 슬라이더는 사람이 만지는 값이라 예외를 던지면 UI 가 죽는다. 반대로 `ISettingsStore.Load` 가 손상된 값을 넣어도 같은 경로를 지나야 하므로, **`Load` 는 프로퍼티가 아니라 `Set*` 를 거친다.**

`Changed` 는 볼륨·전체화면을 구분하지 않는다. 구독자가 둘 다 다시 적용하는 편이 싸고, 이벤트를 나누면 한쪽만 구독하는 버그가 생긴다.

#### `SaturationGauge` — 하나의 식, 분기 없음 (README D6)

```
VisibleCells = clamp(min(maxSaturation, cellCapacity), 0, cellCapacity)
FilledCells  = ceil(VisibleCells × currentSaturation / maxSaturation)
```

`maxSaturation ≤ cellCapacity` 이면 두 번째 식은 `currentSaturation` 과 **정확히 같다** — 정상 구간(3·5·8)에 근사가 끼지 않는다. Placeholder 애셋의 `99` 는 8칸에 비례로 접힌다.

- `maxSaturation ≤ 0` 이면 0 을 돌려준다. `CustomerData.OnValidate` 가 1 을 하한으로 잡지만 이 클래스는 SO 없이도 불린다 (`StageClock` 이 제한 시간 0 을 예외로 두는 것과 같은 판단이며, **여기서는 예외 대신 0** 이다 — 그리는 쪽이 예외로 죽으면 안 된다)
- `FilledCells` 는 `VisibleCells` 를 넘지 않는다
- **`currentSaturation > 0` 이면 결과가 최소 1 이다** — `ceil` 이 그것을 보장한다. 한 입 먹었는데 칸이 하나도 안 차면 먹은 것이 화면에 없는 것과 같다

### 선행 산출물 의존성

없음.

### 밸런스 수치

**없다.** 칸 수(`cellCapacity`)는 인자로 받는다 — 프리팹의 연출 수치이며 이 클래스가 알 필요가 없다 (M5 D7).

### 제약

- **`UnityEngine` 을 참조하지 않는다.** `Mathf` 도 쓰지 않는다 — `System.Math` 로 충분하다. `grep -c "UnityEngine" <파일>` 이 0 이어야 한다 (`SoundBudget` 이 이미 그 기준을 통과한 형태다)
- `Runtime` 은 `Presentation` 을 모른다 (asmdef §3)
- 이벤트 구독 해제는 구독자의 몫이다 — 이 세 클래스는 `IDisposable` 이 아니다
- 정적 가변 상태를 만들지 않는다 (RULE-01). `SaturationGauge` 는 `static class` 지만 **필드가 없다**
- 난수 금지 — 애초에 쓸 일이 없다

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
일시정지  Pause_WhenRunning_RaisesChangedOnce
          Pause_Twice_RaisesChangedOnce            ← 중복 억제
          Resume_WhenNotPaused_DoesNotRaise
설정      SetMasterVolume_AboveOne_ClampsToOne
          SetMasterVolume_Negative_ClampsToZero
          SetMasterVolume_SameValue_DoesNotRaise
          Load_CorruptedVolume_PassesThroughClamp  ← Load 가 Set 을 거치는가
포화도    FilledCells_MaxWithinCapacity_EqualsCurrent   ← 정상 구간은 근사가 없다
          FilledCells_MaxAboveCapacity_ScalesDown
          FilledCells_OneEaten_ReturnsAtLeastOne
          FilledCells_Full_EqualsVisibleCells
          VisibleCells_MaxZero_ReturnsZero
```

> **`FilledCells_MaxWithinCapacity_EqualsCurrent` 에 구체값을 박는다.** *"둘이 같다"* 만 확인하면 상수를 돌려주는 구현에서도 통과한다 ([`tests.md`](../../.claude/rules/tests.md) §3). `(current 3, max 5, capacity 8) → 3` 처럼 값을 직접 쓰고, 같은 테스트에 `(current 3, max 99, capacity 8) → 1` 같은 **반례를 함께 박는다**.

> **`Pause_Twice_RaisesChangedOnce` 에 다른 억제 장치가 끼지 않게 한다.** 구독을 한 번만 걸고 카운터로 센다 — 이벤트가 두 번 와도 다른 무언가가 막아 통과하는 형태를 피한다 ([`tests.md`](../../.claude/rules/tests.md) §3 «다른 장치가 주입을 가릴 수 있다»).

### 주입 실측

| 주입 | 예측 | 실제 |
|---|---|---|
| `PauseState` 의 중복 억제 제거 | 1건 | **2건** — `Pause_Twice_RaisesChangedOnce` · `Resume_WhenNotPaused_DoesNotRaise` |
| `FilledCells` 의 올림 → 내림 | 1건 | **2건** — 큰 상한 테스트와 **정상 구간 테스트 안의 반례**가 함께 |
| `SetMasterVolume` 이 자르기 **전** 값으로 비교 | 1건 | 예측대로 **1건** |
| `VisibleCells` 가 상한을 무시 | 1건 | **3건** |

> **반례가 값을 했다.** `FilledCells_MaxWithinCapacity_EqualsCurrent` 안에 박아 둔
> `(current 3, max 99, capacity 8) → 1` 한 줄이 올림→내림 주입을 함께 잡았다. 그 줄이 없었으면
> "정상 구간에서 current 를 그대로 돌려준다" 는 계약만 남고, 접히는 쪽은 별도 테스트
> 하나에만 걸렸을 것이다 ([`tests.md`](../../.claude/rules/tests.md) §3).

### 안 쓴 테스트 하나 — `Load` 는 우회할 길이 없다

작업서는 *"`Load` 가 프로퍼티가 아니라 `Set*` 를 거치는지"* 를 테스트하라고 적었지만,
`MasterVolume` 의 세터가 `private` 이라 **저장소가 우회할 방법 자체가 없다.** 타입 시스템이
이미 지키는 것을 테스트로 다시 확인하면 검증은 안 늘고 코드만 는다 (step-01 에서 지운
테스트와 같은 이유).

손상된 값이 실제로 문제가 되는 지점은 **`PlayerPrefs` 를 읽는 구현**이고, 그 테스트는
구현이 생기는 step-10 의 몫이다.

### 예정에 없던 것 하나 — 숫자가 아닌 값

`Math.Min`/`Math.Max` 는 NaN 을 **자르지 못한다.** 비교가 전부 `false` 라 그대로 통과하고,
그 뒤로는 볼륨이 영영 복구되지 않는다 (`NaN != NaN` 이라 같은 값 가드에도 안 걸린다).

슬라이더는 NaN 을 만들지 않지만 **저장소는 만들 수 있다** — 손상된 `PlayerPrefs` 를 읽는
경로가 step-10 에 실재한다. 무시하는 것으로 막고 `SetMasterVolume_NotANumber_IsIgnored` 로
고정했다.

**Red 는 실제로 관측했다** — `CS0234: SushiDefense.Settings` · `CS0246: GameSettings` ·
`CS0246: PauseState`, 아직 안 쓴 심볼만 지목하는 컴파일 실패였다.

### 완료 판정

- [x] 네 파일 전부 `UnityEngine` 참조 **0** — `Math` 는 `System` 쪽을 쓴다
- [x] `grep -rn "timeScale" Assets/Code/` = **0건**
- [x] EditMode Green — **602/602**
- [x] 주입 4건을 실제로 넣어 보고 표를 정정했다 (넷 다 예측보다 넓게 잡혔다)
- [x] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(stage): add pause gate, settings model and saturation gauge
```

---

## 금지 사항

- `Time.timeScale` 을 쓰지 않는다 (README D4). 이 어셈블리는 `UnityEngine` 자체를 참조하지 않는다
- `PlayerPrefs` 를 여기서 부르지 않는다 — `ISettingsStore` 구현은 step-10 의 `Presentation` 쪽이다
- 세 클래스를 서로 참조하게 만들지 않는다
- 다른 어셈블리 파일을 수정하지 않는다
