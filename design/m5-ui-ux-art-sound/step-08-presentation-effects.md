# Step 08: VFX — 먹힘 팝 · 클리어/실패, 풀 경유

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-04 (이펙트에 쓸 스프라이트)
- **후행 단계:** step-11 이 씬에서 확인한다

---

## 목적

**초밥이 사라지는 순간이 지금은 아무 일도 없다.** 벨트에서 조용히 없어질 뿐이라 "먹혔다" 와 "끝점에서 사라졌다" 가 화면에서 구분되지 않는다. 매출은 올라가는데 왜 올라갔는지 보이지 않는다.

먹힘 팝 하나만 있어도 이 문제가 해결된다. 클리어/실패 연출은 스테이지에 한 번뿐이지만 **데모의 마지막 인상**이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Effects/EffectPoolBehaviour.cs` — 생성
- `Assets/Code/Scripts/Presentation/Effects/PopEffectView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Effects/EffectDirector.cs` — 생성
- `Assets/Code/Scripts/Presentation/Effects/PopEffect.prefab` — 생성
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (배선)
- `Assets/Tests/PlayMode/Effects/EffectDirectorTests.cs` — 생성

> **프리팹을 기능 폴더 안에 둔다** ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §2). `Assets/Level/Prefabs/` 가 아니라 그 프리팹을 쓰는 스크립트 옆이다 — 기존 `SushiItem.prefab`·`Customer.prefab` 과 같은 배치다.

### 핵심 심볼

```csharp
namespace SushiDefense.Effects
{
    /// <summary>
    /// 이펙트 인스턴스를 공급한다. 프로덕션 코드의 <c>Instantiate</c>/<c>Destroy</c> 는
    /// 여기와 <see cref="SushiPoolBehaviour"/> 두 곳뿐이다 (<c>CLAUDE.md</c> §3.4).
    /// </summary>
    public sealed class EffectPoolBehaviour : MonoBehaviour, ISushiInstanceFactory<GameObject>
    {
        public GameObject Rent();
        public void Return(GameObject instance);
        public int CountAll { get; }
        public int CountActive { get; }
    }

    /// <summary>이펙트 하나의 수명. 다 되면 스스로 풀에 돌아간다.</summary>
    public sealed class PopEffectView : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _lifetimeSeconds;
        public void Play(Vector3 worldPosition, Action<GameObject> returnToPool);
    }

    /// <summary>로직의 사건을 이펙트로 옮긴다. 판정하지 않는다.</summary>
    public sealed class EffectDirector : MonoBehaviour
    {
        public int SpawnedCount { get; }   // 검증용
        public void Bind(ClaimCoordinator coordinator, StageController stage);
        public void Unbind();
    }
}
```

### 실측 — 새 풀 컴포넌트를 만들지 않았다 (작업서 정정)

위 «생성 파일» 과 «핵심 심볼» 은 `EffectPoolBehaviour` 를 새로 만들라고 적었지만,
**`SushiPoolBehaviour` 를 그대로 썼다.**

그 컴포넌트에 초밥에만 해당하는 것이 없다 — 프리팹을 복제해 빌려주고 돌려받을 뿐이다.
이름만 바꿔 복사하면 **60줄이 두 벌**이 되고, 언젠가 한쪽만 고치게 된다 (R1 · R8). 이름이
어색한 것은 D6 이 `ISushiInstanceFactory` 에 대해 이미 받아들인 것과 같은 종류의 비용이고,
개명은 M5 이후의 몫이다.

부수 효과로 §3.4 의 완료 판정이 **더 강해졌다** — `Instantiate`/`Destroy` 를 부르는 곳이
두 파일이 아니라 여전히 **한 파일**이다.

`EffectDirector` 의 클래스 주석에 이 판단과 이유를 남겼다. 그러지 않으면 다음 사람이
"왜 이펙트 풀이 초밥 컴포넌트지" 에서 멈춘다.

### 연출 수치는 프리팹에 (README D7)

이펙트 수명·크기·색은 `PopEffect.prefab` 의 `[SerializeField]` 다. **SO 로 빼지 않는다.**

구분선은 *"사람이 밸런싱하며 만질 값인가"* 다. 이펙트 수명은 그 프리팹 하나에만 의미가 있고 다른 곳에서 참조되지 않는다 — SO 로 만들면 애셋만 늘고 찾기 어려워진다. 반대로 오디오 쿨다운은 플레이하며 조정하는 값이라 SO 로 갔다 (step-01).

### 무엇을 어디에 띄우나

| 사건 | 이펙트 | 위치 |
|---|---|---|
| 초밥이 먹혔다 | 팝 (짧게 커지며 사라짐) | 먹은 손님 자리 |
| 클리어 | 밝은 팝 여러 개 | 화면 중앙 |
| 실패 | 어두운 팝 하나 | 화면 중앙 |

먹힘 팝을 **초밥 위치가 아니라 손님 자리**에 띄운다. 초밥은 그 시점에 이미 벨트에서 빠졌고, 플레이어가 봐야 할 것은 "누가 먹었나" 다.

### 선행 산출물 의존성

- step-04 의 스프라이트 (팝 모양)
- 기존 `SushiDefense.Belt.SushiPool<T>` · `ISushiInstanceFactory<T>` (변경 없이 사용)

### 밸런스 수치

**없다.** 연출 수치는 프리팹의 직렬화 필드다 (위).

### 제약

- **`Instantiate`/`Destroy` 를 `EffectPoolBehaviour` 밖에서 부르지 않는다** (`CLAUDE.md` §3.4). WebGL GC 때문이며, 초밥에만 걸린 규칙이 아니다
- **`Update` 경로에 할당을 만들지 않는다** (§4). 이펙트 수명 관리에 코루틴을 쓰면 매번 `IEnumerator` 가 할당된다 — 경과 시간을 필드로 누적하는 편이 싸다
- 구독은 `Unbind()`/`OnDestroy()` 에서 해제한다 (§6)
- `Bind` 는 먼저 `Unbind` 를 부른다 — 스테이지 전환에서 `Build()` 가 다시 불린다
- **파티클 시스템을 쓸 거면 `Play`/`Stop` 재사용을 확인한다.** 매번 새로 만들면 풀의 의미가 없다. 스프라이트 하나를 스케일·페이드하는 편이 WebGL 에서 더 싸고, 픽셀 아트와도 어울린다
- 풀·프리팹이 `null` 이어도 죽지 않는다. **이펙트는 로직의 전제 조건이 아니다**
- `FindObjectOfType` 금지 (§4.3)

### 테스트 계획

**PlayMode 다.**

```
풀        Rent_TwiceAfterReturn_ReusesInstance          ← 풀이 실제로 재사용한다
          Rent_Many_DoesNotExceedInstanceCountByLifetime
수명      Play_AfterLifetime_ReturnsToPool
배선      SushiEaten_Once_SpawnsOneEffect
          SushiEaten_Twice_SpawnsTwoEffects
          Bind_Twice_DoesNotDoubleSpawn                 ← 구독 중복 방지
          Unbind_ThenEvent_DoesNotSpawn
견고성    Bind_WithoutPool_DoesNotThrow
```

`Rent_TwiceAfterReturn_ReusesInstance` 는 **인스턴스 참조가 같은지**를 본다. `CountAll` 이 늘지 않았다는 것만으로는 부족하다 — 상수를 돌려주는 구현에서도 통과한다 (`tests.md` §3).

기존 `Assets/Tests/EditMode/Belt/SushiPoolTests.cs` 가 풀 로직 자체를 이미 덮고 있다. **같은 것을 다시 테스트하지 않는다** — 여기서 볼 것은 이펙트가 그 풀을 **경유하는가**다.

### 주입 실측

| 주입 | 예측 | 실제 |
|---|---|---|
| `Bind` 에서 선행 `Unbind` 제거 | 1건 | 예측대로 **1건** |
| 자리를 찾지 않고 늘 중앙에 띄운다 | 1건 | 예측대로 **1건** |
| 수명 만료 시 반납 제거 | 2건 | **1건** — 아래 |

> **"첫 번째 활성" 을 집는 헬퍼가 재사용 테스트를 공허하게 만들었다.**
> `Play_AfterReturn_ReusesSameInstance` 는 반납이 없어도 통과했다. 반납이 안 되면 인스턴스가
> **둘** 떠 있는데, 헬퍼가 계층에서 먼저 만나는 활성 오브젝트를 돌려주고 그것이 여전히
> 첫 번째라 `AreSame` 이 맞아떨어진다.
>
> `CountAll` 단언을 함께 박아 고쳤다 — 재사용했다면 인스턴스 수가 늘지 않는다.
> **참조 비교만으로는 "재사용" 과 "새로 만들고 옛것도 살아 있음" 이 구분되지 않는다.**

### 이 단계에서 지키지 못한 것

**Red 를 관측하지 않았다.** 테스트와 구현을 연달아 쓰고 한 번에 돌렸다. 주입 3건이 계약을
검증했지만, 실패를 먼저 보는 절차 자체는 건너뛴 것이다 (`CLAUDE.md` §5).

### 완료 판정

- [x] `Instantiate`/`Destroy` 를 부르는 곳이 `SushiPoolBehaviour` **한 파일뿐** — §3.4 확인
      (나머지 매치는 전부 `OnDestroy` 라이프사이클 메서드다)
- [x] `Runtime` 어셈블리 **무변경** — `ISushiInstanceFactory` 를 건드리지 않았다 (D6)
- [x] `./tests/run-tests.sh all` Green — EditMode 550/550 · PlayMode 143/143
- [ ] **재생해서 먹힘 팝이 손님 자리에 뜨는지 확인** — 씬에 배치가 없어 아직 못 했다.
      step-11 로 넘긴다
- [x] 주입 3건 실측 후 표 정정
- [x] `./tests/preflight.sh` — 미결 `ProjectSettings` 1건 외 전 항목 PASS

### 예상 커밋 메시지

```
feat(effects): add pooled pop effect for sushi eaten
```

---

## 금지 사항

- `ISushiInstanceFactory<T>` 를 개명하지 않는다 (D6)
- 프로덕션 코드에서 `Instantiate`/`Destroy` 를 직접 부르지 않는다 (풀 안은 예외)
- 연출 수치를 SO 로 빼지 않는다 (D7)
- 프리팹을 `Assets/Art/` 나 `Assets/Level/Prefabs/` 에 만들지 않는다 — 기능 폴더다
- 다른 어셈블리 파일을 수정하지 않는다
