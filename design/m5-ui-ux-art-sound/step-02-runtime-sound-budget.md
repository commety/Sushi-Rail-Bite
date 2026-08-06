# Step 02: 효과음 겹침 제어 — `SoundBudget`

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** 없음 (step-01 과 논리적 순서일 뿐, 컴파일 의존 없음)
- **후행 단계:** step-07 의 `AudioDirector` 가 이 판정을 물어본다

---

## 목적

**원문 계획이 명시적으로 요구한 과제다** — *"효과음 겹침 … 동시 재생 수 제한·쿨다운·볼륨 덕킹을 설계에 넣는다."*

손님 4명이 각자 초밥을 먹으면 같은 효과음이 같은 프레임에 넷 겹친다. 그대로 재생하면 볼륨이 4배가 되어 찢어진다. 이 단계는 **"지금 이 소리를 내도 되는가" 를 판정하는 순수 C# 로직**을 만든다.

`AudioSource` 를 모른다. 시계도 모른다 — 시각을 인자로 받는다. 그래서 EditMode 에서 "0.05초 안에 셋" 을 프레임 없이 재현할 수 있다 (`.claude/rules/tests.md` §1).

여기에 **자동재생 게이트**도 함께 둔다. WebGL 은 사용자 제스처 전까지 오디오 컨텍스트가 잠기므로, 잠금 상태의 재생 요청은 소리가 나지 않은 채 **소모된다** — 큐가 밀리는 게 아니라 그냥 사라진다. 그 판정도 로직이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

- `Assets/Code/Scripts/Runtime/Audio/SoundBudget.cs` — 생성
- `Assets/Code/Scripts/Runtime/Audio/AudioUnlockGate.cs` — 생성
- `Assets/Tests/EditMode/Audio/SoundBudgetTests.cs` — 생성
- `Assets/Tests/EditMode/Audio/AudioUnlockGateTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.Audio
{
    /// <summary>동시 재생 상한과 큐별 쿨다운을 판정한다. 재생하지 않는다.</summary>
    public sealed class SoundBudget
    {
        public SoundBudget(int maxConcurrent);

        /// <summary>지금 살아 있는 재생 수. 만료된 것은 세지 않는다.</summary>
        public int ActiveCount(float now);

        /// <summary>
        /// 재생을 요청한다. 허용되면 예산을 소모하고 <c>true</c>.
        /// </summary>
        /// <param name="cueId">쿨다운을 구분하는 키. 큐마다 고유하면 값 자체는 무관하다.</param>
        /// <param name="now">호출 시각(초). 단조 증가여야 한다.</param>
        public bool TryPlay(int cueId, float now, float cooldownSeconds, float durationSeconds);

        /// <summary>스테이지 재시작처럼 시간이 되감기는 지점에서 상태를 비운다.</summary>
        public void Reset();
    }

    /// <summary>첫 사용자 입력 전까지 재생을 막는다 (브라우저 자동재생 정책).</summary>
    public sealed class AudioUnlockGate
    {
        public bool IsUnlocked { get; }

        /// <summary>첫 입력에서 한 번 열린다. 두 번째 호출부터는 아무 일도 없다.</summary>
        public void Unlock();

        /// <summary>열린 뒤 처음 한 번만 <c>true</c> — BGM 을 여기서 시작한다.</summary>
        public bool TryConsumeUnlockMoment();
    }
}
```

**두 클래스를 합치지 않는다.** 예산은 "너무 많다/너무 잦다" 를, 게이트는 "아직 아무것도 안 된다" 를 본다. 합치면 *"소리가 안 났다"* 의 원인이 둘 중 어느 쪽인지 테스트에서 구분되지 않는다 — 자격과 타이밍을 섞지 않는 이 프로젝트의 기존 원칙과 같은 이유다 (`scripts.md` §2).

### 선행 산출물 의존성

없음. `AudioBankSO` 를 참조하지 않는다 — 쿨다운·상한을 **인자로** 받는다. 그래야 `Runtime` 이 `Runtime.Data` 의 새 타입에 묶이지 않고, 테스트가 SO 를 만들지 않아도 된다.

### 밸런스 수치

없음. 전부 인자다.

### 제약

- `MonoBehaviour` 가 아니다. `UnityEngine` 참조를 만들지 않는다 (`Mathf` 도 쓰지 않는다 — `System.Math` 로 충분하다)
- **난수 금지.** `tests/preflight.sh` 의 두 가드(배정 난수 / 전역 난수)가 `Runtime` 전체를 본다. 허용 목록은 `Runtime/Run/` 뿐이다 (`tests.md` §5)
- **할당 없는 경로를 만든다.** `TryPlay` 는 매 먹힘마다 불린다. LINQ·`new`·문자열 결합을 넣지 않는다 — WebGL 에서 GC 스파이크가 그대로 프레임 히칭이 된다 (`scripts.md` §4). 만료 목록은 고정 배열 또는 미리 잡은 리스트를 재사용한다
- `static` 필드를 두지 않는다. 두게 되면 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 초기화가 함께 필요하다 (RULE-01)
- public API 에 `///` XML 문서 주석

### 테스트 계획 (TDD — 실패부터)

**공허하게 통과하지 않게 두 축을 엇갈려 둔다** (`tests.md` §3). 쿨다운을 볼 때는 상한을 넉넉히, 상한을 볼 때는 쿨다운을 `0` 으로 준다. 나란히 두면 한쪽만 구현해도 전부 통과한다.

```
쿨다운  TryPlay_SameCueWithinCooldown_ReturnsFalse       ← 상한은 넉넉히
        TryPlay_SameCueAfterCooldown_ReturnsTrue
        TryPlay_DifferentCueWithinCooldown_ReturnsTrue   ← 쿨다운은 큐별이다
        TryPlay_ZeroCooldown_AlwaysAllowsUntilLimit
상한    TryPlay_AtConcurrentLimit_ReturnsFalse           ← 쿨다운은 0
        TryPlay_AfterOldestExpired_ReturnsTrue           ← 자리가 다시 난다
        ActiveCount_AfterAllExpired_IsZero
경계    TryPlay_MaxConcurrentOne_AllowsOneAtATime
        Reset_AfterReset_AllowsImmediately
게이트  TryPlay_WhileLocked_...                          ← 게이트는 예산 밖이다. 여기서 테스트하지 않는다
        Unlock_BeforeUnlock_IsUnlockedIsFalse
        Unlock_Twice_ConsumeMomentOnlyOnce
```

**"둘이 같다" 만 확인하지 않는다** (`tests.md` §3). `ActiveCount` 는 구체값을 박는다 — 상수를 돌려주는 구현에서도 통과하면 안 된다.

### 주입으로 공허함을 확인한다

Green 이 된 뒤 계약을 **하나씩** 깨뜨리고 지목한 테스트가 실제로 실패하는지 본 뒤 되돌린다:

| 주입 | 잡혀야 할 테스트 |
|---|---|
| 쿨다운 비교를 `>` → `>=` | 경계 테스트 |
| 쿨다운 키를 큐별 → 전역 하나 | `TryPlay_DifferentCueWithinCooldown_ReturnsTrue` |
| 만료 판정 제거 (한 번 차면 영영 안 빠짐) | `TryPlay_AfterOldestExpired_ReturnsTrue` |

**어느 테스트가 잡을지는 돌려 보기 전까지 모른다** — M4 에서 세 번 빗나갔다. 위 표는 가설이고, **실측으로 정정해 이 파일에 남긴다.**

주입은 `sed`/`perl` 이 아니라 편집 도구로 넣는다 — `perl` 은 C# 보간 문자열의 `$"` 를 자기 변수로 해석해 파일을 조용히 망가뜨린다.

### 완료 판정

- [ ] `grep -rn "class SoundBudget\|class AudioUnlockGate" Assets/Code/Scripts/Runtime/Audio/`
- [ ] `grep -c "UnityEngine" Assets/Code/Scripts/Runtime/Audio/*.cs` 가 **0**
- [ ] `./tests/run-tests.sh` Green
- [ ] 위 주입 3건을 실제로 넣어 보고, **잡힌 테스트 이름을 이 파일의 표에 실측으로 채운다**
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(audio): add sound budget and unlock gate
```

---

## 금지 사항

- `AudioSource`·`AudioClip`·`Time.time` 을 쓰지 않는다. 시각은 인자다
- 볼륨 덕킹(다른 소리를 줄이는 것)을 이 클래스에 넣지 않는다. 그것은 재생 장치의 일이고, 지금은 **큐별 고정 볼륨**으로 시작한다 (README D7)
- 다른 어셈블리 파일을 수정하지 않는다
