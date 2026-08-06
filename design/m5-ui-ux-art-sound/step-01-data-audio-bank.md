# Step 01: 오디오 뱅크 SO 스키마

- **영역:** `data` — 어셈블리 `Runtime.Data`
- **선행 단계:** 없음
- **후행 단계:** step-06 이 이 타입의 `.asset` 을 만들고, step-07 이 읽는다

---

## 목적

효과음·BGM 을 어디서 가져오고 얼마나 자주 낼지를 **데이터로** 정의한다. 지금 저장소에는 `AudioClip` 을 참조하는 코드가 한 줄도 없으므로 이 단계가 오디오의 첫 계약이 된다.

핵심은 **쿨다운과 동시 재생 상한이 SO 에 있다**는 것이다. 원문 계획이 "효과음 겹침" 을 명시적 과제로 올렸고, 겹침이 거슬리는지 아닌지는 플레이하며 조정하는 값이다 — 코드에 박으면 조정할 때마다 컴파일해야 한다 (`CLAUDE.md` §3.1).

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

- `Assets/Code/Scripts/Runtime.Data/Audio/AudioCue.cs` — 생성
- `Assets/Code/Scripts/Runtime.Data/Audio/AudioBankSO.cs` — 생성
- `Assets/Tests/EditMode/Data/AudioCueTests.cs` — 생성
- `Assets/Tests/EditMode/Data/AudioBankTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.Data
{
    /// <summary>큐 하나 — 무엇을, 얼마나 크게, 얼마나 자주.</summary>
    [Serializable]
    public sealed class AudioCue
    {
        public AudioClip Clip { get; }
        public float Volume { get; }          // [Range(0f, 1f)], 기본 1f
        public float CooldownSeconds { get; } // [Min(0f)]
        public bool HasClip { get; }          // Clip != null

        internal void Clamp();                // 뱅크가 대신 불러 준다 (아래)
    }

    [CreateAssetMenu(menuName = "SushiRailBite/Audio Bank", fileName = "AudioBank")]
    public sealed class AudioBankSO : ScriptableObject
    {
        public AudioCue SushiEaten { get; }
        public AudioCue CustomerPlaced { get; }
        public AudioCue RewardPicked { get; }
        public AudioCue StageAdvanced { get; }
        public AudioCue StageCleared { get; }
        public AudioCue StageFailed { get; }

        public AudioCue Bgm { get; }
        public int MaxConcurrentSfx { get; }  // [Min(1)], 기본 1

        internal void OnValidate();           // 상한 클램프 + 큐 7개에 Clamp() 전달
    }
}
```

`AudioCue` 는 `[Serializable] class` 다 — `struct` 로 하면 인스펙터에서 기본값(볼륨 0)이 조용히 들어가 **소리가 안 나는데 원인을 못 찾는** 상태가 된다.

**중첩된 `[Serializable]` 타입에는 Unity 의 검증 훅이 직접 오지 않는다.** 그래서 `AudioCue.Clamp()` 를 `internal` 로 두고 뱅크가 대신 불러 준다 — `SushiData` 가 어트리뷰트 위에 한 겹 더 조이는 것과 같은 처리이며, `Runtime.Data/AssemblyInfo.cs` 의 `InternalsVisibleTo("Tests.EditMode")` 로 테스트에서 검증한다.

### 선행 산출물 의존성

없음.

### 밸런스 수치

이 단계는 **필드만 정의한다.** 값은 step-06 에서 `.asset` 에 들어가며 §7 승인 대상이다. 제안값은 [README](README.md#새-밸런스-수치) 참조.

### 제약

- `[SerializeField] private` + 읽기 전용 프로퍼티. `public` 필드 금지 (`scriptable-object.md` §3)
- 수치 필드에 범위 제약 — `[Range(0f, 1f)]` 볼륨, `[Min(0f)]` 쿨다운, `[Min(1)]` 상한
- public API 에 `///` XML 문서 주석 — `Runtime.Data` 는 다른 모든 어셈블리가 읽는 계약이다
- **`Runtime.Data` 는 계산하지 않는다** (`scriptable-object.md` §4·§7). "지금 재생해도 되나" 는 여기 없다 — step-02 의 `SoundBudget` 이 한다. 이 SO 는 값을 들고만 있는다
- `Runtime` · `Presentation` 을 참조하지 않는다
- **검증에 밸런스를 섞지 않는다** (§6). "볼륨이 너무 작다", "쿨다운이 길다" 는 오류가 아니라 기획이다. 구조 불변식만 본다
- **금지형을 주석에서 이름으로 부르지 않는다** (`scripts.md` §7) — `tests/preflight.sh` 의 grep 가드는 코드와 주석을 구분하지 못한다. *"검증 훅을 두지 않는다"* 처럼 가리키되 이름은 부르지 않는다

### 테스트 계획 (TDD — 실패부터)

`AudioCueTests`:
- `HasClip_NoClip_ReturnsFalse` / `HasClip_WithClip_ReturnsTrue`
- `Volume_Default_IsWithinUnitRange`
- `CooldownSeconds_Default_IsNotNegative`

`AudioBankTests`:
- `MaxConcurrentSfx_Default_IsAtLeastOne` — 0 이면 소리가 하나도 안 난다. 기본값이 그 상태이면 안 된다
- `Cues_FreshInstance_AreNotNull` — `[Serializable] class` 필드는 인스펙터가 채우기 전에 `null` 일 수 있다. step-07 이 `NullReferenceException` 으로 죽는 경로를 여기서 막는다
- `Bgm_FreshInstance_HasNoClip` — 뱅크를 만들자마자 소리가 나면 안 된다

SO 는 테스트 안에서 `ScriptableObject.CreateInstance<AudioBankSO>()` 로 만든다. **디스크의 애셋을 로드하지 않는다** (`tests.md` §4) — 값이 바뀔 때마다 테스트가 깨진다.

`AudioClip` 은 `AudioClip.Create(...)` 로 만들 수 있다. private 필드 주입은 기존 `Assets/Tests/EditMode/Data/SerializedFieldSetter.cs` 를 쓴다.

### 주입 실측 (완료 후 기록)

계약을 하나씩 깨뜨려 지목한 테스트가 실제로 실패하는지 확인한 결과다. **예측이 아니라 실측이다.**

| 주입 | 예측 | 실제 |
|---|---|---|
| `_volume = 1f` 초기값 제거 | `Volume_FreshCue_IsFullyAudible` | 예측대로 **1건** |
| `OnValidate` 가 첫 큐만 조임 | `OnValidate_NegativeVolumeInEveryCue_ClampsAll` | 예측대로 **1건** (`CustomerPlaced` 에서 걸림) |
| 큐의 `= new()` 초기값 제거 | 1건 | **2건** — `Cues_FreshBank_AreNotNull` + `Cues_FreshBank_HaveNoClip`(NRE) |
| `Clamp01` → `Max(0f, …)` (상한 제거) | `Clamp_VolumeAboveOne_ClampsToOne` | 예측대로 **1건** |

> **세 번째에서 배운 것**: `SerializedFieldSetter` 를 거치는 테스트는 초기값을 지워도 **살아남는다.**
> `ApplyModifiedPropertiesWithoutUndo()` 가 역직렬화를 태우고, 그때 Unity 가 `null` 인
> `[Serializable]` 클래스 필드를 기본 생성자로 되살리기 때문이다
> ([`unity-scripting-gotchas.md`](../../.claude/knowledge/unity-scripting-gotchas.md) §1-2).
> 죽은 것은 **직렬화 경로를 전혀 건드리지 않는** 두 테스트뿐이었다.

### 완료 판정

- [x] `grep -rn "class AudioBankSO" Assets/Code/Scripts/Runtime.Data/` 로 정의 확인
- [ ] `grep -rn "SushiDefense.Belt\|SushiDefense.Customers\|UnityEngine.UI" Assets/Code/Scripts/Runtime.Data/Audio/` 가 **0건** — 의존 방향 확인
- [ ] `./tests/run-tests.sh` Green
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(audio): add audio bank scriptable object
```

---

## 금지 사항

- 이 단계에서 `AudioSource` 를 언급하지 않는다. `Runtime.Data` 는 재생 장치를 모른다
- 큐를 문자열 키 딕셔너리로 만들지 않는다. `JsonUtility`/인스펙터가 `Dictionary` 를 직렬화하지 못하고, 오타가 컴파일에 걸리지 않는다 — **이름 붙은 필드**로 둔다
- `.asset` 파일을 만들지 않는다. step-06 의 몫이다
- 다른 어셈블리 파일을 수정하지 않는다
