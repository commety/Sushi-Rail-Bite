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

### `ISushiInstanceFactory<T>` 를 재사용한다 (README D6)

`SushiPool<T>` 는 이미 제네릭이라 이펙트에도 그대로 쓸 수 있다. 인터페이스 이름이 초밥 전용처럼 보이지만 **M5 에서 개명하지 않는다** — 4파일짜리 리네임의 이득보다, 아트·사운드가 걸린 마일스톤에 리팩터 커밋을 섞는 비용이 크다.

**`EffectPoolBehaviour` 의 클래스 주석에 이 판단과 이유를 남긴다.** 그러지 않으면 다음 사람이 "왜 이펙트가 초밥 인터페이스를 구현하지" 에서 멈춘다.

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

### 주입으로 확인한다

| 주입 | 잡혀야 할 테스트 (가설) |
|---|---|
| `Return` 대신 `Destroy` 직접 호출 | `Rent_TwiceAfterReturn_ReusesInstance` |
| 수명 만료 시 반납 제거 | `Play_AfterLifetime_ReturnsToPool` |
| `Bind` 에서 선행 `Unbind` 제거 | `Bind_Twice_DoesNotDoubleSpawn` |

**예측은 가설이다.** 실측으로 정정한다.

### 완료 판정

- [ ] `grep -rn "Instantiate\|Destroy(" Assets/Code/Scripts/Presentation/ --include="*.cs"` 결과가 `SushiPoolBehaviour` 와 `EffectPoolBehaviour` **두 파일뿐** — §3.4 확인
- [ ] `grep -rn "ISushiInstanceFactory" Assets/Code/Scripts/Runtime/` 로 인터페이스가 **변경되지 않았음** 확인 (D6)
- [ ] `./tests/run-tests.sh all` Green
- [ ] 재생해서 먹힘 팝이 손님 자리에 뜨는지 확인
- [ ] 주입 3건 실측 후 표 정정
- [ ] `./tests/preflight.sh` 전 항목 PASS

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
