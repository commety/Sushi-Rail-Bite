# Unity Scripting — 고위험 함정

다른 `knowledge/*.md`·`CLAUDE.md`·`RULES.md` 와 겹치지 않고, 모델이 **자주 틀리는** 항목만. Unity 2022.3 LTS 기준.

---

## 1. 직렬화 (Serialization)

### 1-1. 깊이 7 제한

중첩된 `[Serializable]` class/struct/List/배열이 **7 단계를 넘으면 그 아래는 조용히 저장 안 됨**. 런타임 기본값으로 돌아온다.

- 재귀 트리 (노드가 자식 `List<Node>`) 는 이 벽에 거의 항상 부딪힌다.
- 회피: 자식을 `UnityEngine.Object` 파생 (ScriptableObject 등) 으로 **참조**로 끊기 → 직렬화가 참조 지점에서 종료.

### 1-2. null 필드 자동 부활

`[Serializable]` 커스텀 class 타입 필드가 `null` 이면, Unity 는 **그 타입의 기본 생성자로 새 인스턴스를 만들어 채운다.** `if (field == null)` 기반 로직이 망가진다.

- "비어 있음" 을 표현해야 하면 옵션:
  - `bool hasValue` 플래그를 같이 둔다 (가장 단순)
  - 타입을 `UnityEngine.Object` 파생으로 바꾼다 — Object 참조는 null 을 유지함
  - `[SerializeReference]` + 명시적 null — 이 속성은 예외적으로 null 을 허용

### 1-3. 인라인 복제

non-`UnityEngine.Object` `[Serializable]` 클래스는 **값처럼 인라인 직렬화**. 두 필드가 같은 인스턴스를 참조해도 저장→로드 후엔 **별개의 두 객체**가 된다. 공유가 필요하면 ScriptableObject 로 빼서 그걸 참조.

### 1-4. `[SerializeReference]` (2019.3+) — 다형성·null 허용

`Animal[]` 에 `Dog`/`Cat` 을 섞고 싶거나 null 을 유지하고 싶을 때:

```csharp
[SerializeReference] public IAction[] actions; // Dog/Cat/null 모두 OK
```

주의: reference 로 저장되므로 동일 asset 내부에서의 **참조 공유**도 유지된다 (§1-3 회피책으로도 쓸 수 있다). 단, GUID 기반이 아닌 내부 ID 라 에셋 경계 넘어서는 못 쓴다.

### 1-5. `ISerializationCallbackReceiver` 로 Dictionary 저장

Unity 는 `Dictionary<K,V>` 를 직렬화하지 않는다. 정석 패턴:

```csharp
public class Table : MonoBehaviour, ISerializationCallbackReceiver {
    [SerializeField] List<string> _keys = new();
    [SerializeField] List<int>    _vals = new();
    public Dictionary<string,int> Runtime = new();

    public void OnBeforeSerialize() {
        _keys.Clear(); _vals.Clear();
        foreach (var kv in Runtime) { _keys.Add(kv.Key); _vals.Add(kv.Value); }
    }
    public void OnAfterDeserialize() {
        Runtime.Clear();
        for (int i = 0; i < _keys.Count; i++) Runtime[_keys[i]] = _vals[i];
    }
}
```

`OnAfterDeserialize` 는 **메인 스레드가 아닐 수 있음** → Unity API 호출 금지. 순수 데이터 변환만.

---

## 2. 코루틴 중단 조건 — 자주 틀리는 것

| 트리거 | 코루틴 중단? |
|---|---|
| `StopCoroutine(handle)` / `StopAllCoroutines()` | ✅ |
| `gameObject.SetActive(false)` | ✅ 즉시 |
| `Destroy(gameObject)` / `Destroy(component)` | ✅ |
| **`behaviour.enabled = false`** | ❌ **계속 돈다** |
| 씬 언로드 | ✅ |

→ `enabled=false` 로 컴포넌트를 "꺼도" 그 MB가 시작시킨 코루틴은 살아서 상태를 계속 바꾼다. 끄려면 반드시 명시적 `StopCoroutine` 또는 GO 비활성.

### `WaitForSeconds` vs `WaitForSecondsRealtime`

| | `timeScale = 0` 일 때 | 용도 |
|---|---|---|
| `new WaitForSeconds(t)` | **영원히 멈춤** | 게임 내 시간 (일시정지 영향 받음) |
| `new WaitForSecondsRealtime(t)` | 그대로 진행 | 일시정지 UI·메뉴 애니메이션·토스트 |

`Pause()` 만들면서 `WaitForSeconds` 쓴 타이머가 얼어붙는 버그 흔함. 실시간 기준이 맞다면 Realtime 쪽.

---

## 3. IL2CPP Managed Code Stripping (iOS·콘솔·WebGL 빌드)

IL2CPP 백엔드는 스트리핑을 **끌 수 없다.** 런타임에 사용하는 타입·메서드가 정적 참조로 잡히지 않으면 링커가 제거 → `MissingMethodException` / `Type not found`.

### 잘리는 대표 케이스

- `Type.GetType("Foo.Bar")` / `Activator.CreateInstance(type)` — 문자열로만 참조되는 타입
- `JsonUtility.FromJson<T>` 의 `T` 가 어디서도 `new T()` 되지 않을 때 (순수 역직렬화 대상 POCO)
- `MakeGenericMethod` / `MakeGenericType` — AOT 가 조합을 미리 알아야 함
- `[SerializeField] MyPoco x;` 의 `MyPoco` 가 데이터로만 등장

### 보존 방법

#### `[Preserve]` — 소스에 직접

```csharp
using UnityEngine.Scripting;

[Preserve]
public class SaveDataV2 {
    [Preserve] public int level;
}
```

#### `link.xml` — `Assets/` 어디든

```xml
<linker>
  <assembly fullname="Assembly-CSharp">
    <type fullname="SushiDefense.Data.SushiData" preserve="all"/>
    <type fullname="SushiDefense.Scoring.*"/>
  </assembly>
  <assembly fullname="ThirdPartyPlugin" ignoreIfMissing="1">
    <type fullname="ThirdParty.Foo" preserve="all"/>
  </assembly>
</linker>
```

- `preserve="all"`: 타입·멤버 전부 유지
- `ignoreIfMissing="1"`: 해당 어셈블리가 빌드에 없어도 에러 안 냄 (플러그인 방어)
- 네임스페이스 와일드카드 `SushiDefense.X.*` 로 묶기 가능

### IL2CPP 에서 아예 깨지는 것

- `System.Reflection.Emit` (런타임 IL 생성) — AOT 불가
- 동적 어셈블리 로드 (`Assembly.Load(byte[])`) — 일부 구성만 가능, 대부분 실패

### 실전 흐름

1. 개발 빌드: Managed Stripping Level = **Minimal/Low**
2. 릴리스 직전 **High** 전환 → 주요 경로 플레이테스트
3. `MissingMethodException` / `TypeLoadException` 뜨면 해당 타입을 `link.xml` 에 등록
4. 플러그인은 기본적으로 `ignoreIfMissing="1" preserve="all"` 로 선방어

---

## 4. 에디터에서만 멀쩡한 것들

**에디터가 대신 메워 주는 것**은 플레이어에서 사라진다. 아래는 전부 빌드해야 드러난다.

### 4-1. 폰트 폴백이 없다

에디터는 시스템 폰트로 없는 글자를 메운다. **WebGL 은 OS 폰트에 접근할 수 없어** 폰트 애셋에 없는 글자가 두부(□)로 나온다. 내장 `LegacyRuntime.ttf`(Arial)에는 한글·CJK 가 없다.

→ 화면에 나갈 글자를 폰트에 굽고, `TMP_FontAsset.HasCharacters(text, out missing)` 로 커버리지를 테스트에 고정한다. **문구를 바꾸면 깨져야 정상인 테스트다.**

### 4-2. 입력 백엔드가 코드와 어긋날 수 있다

Player Settings 의 Active Input Handling 이 Input System 전용이면 `ENABLE_LEGACY_INPUT_MANAGER` 가 정의되지 않고, **`UnityEngine.Input` 은 런타임에 `InvalidOperationException` 을 던진다.** 컴파일은 통과한다.

→ `#if ENABLE_LEGACY_INPUT_MANAGER` / `#if ENABLE_INPUT_SYSTEM` 로 전제를 테스트에 박는다. 컴파일 심볼이라 확정적이다.

### 4-3. `AudioSource.playOnAwake` 는 기본값이 켜져 있다

브라우저는 사용자 제스처 전까지 오디오를 잠그고, **잠긴 상태의 재생은 밀리지 않고 사라진다.** 자동 재생이 켜져 있으면 씬이 열리자마자 재생이 시작돼 버려지고, 나중에 제스처가 와도 이미 지나갔다 — 자동재생 게이트를 아무리 잘 만들어도 무력화된다.

### 4-4. WebGL 은 `Streaming` 로드 타입을 지원하지 않는다

지정해도 조용히 다른 모드로 떨어진다. 배경음은 `CompressedInMemory`, 짧은 효과음은 `DecompressOnLoad` 가 맞다.

### 4-5. `Resources/` 는 참조 여부와 무관하게 전부 빌드에 실린다

패키지가 만든 `Resources/` 폴더도 마찬가지다 (TMP Essential Resources 가 2MB 대). 화면에 안 나온다고 안 실리는 것이 아니다 — **폴백 경로로 쓰이는 애셋을 지우면 대신 깨진다.**

### 4-6. 전체화면 상태는 브라우저가 들고 있다

`Screen.fullScreen = true` 는 **요청이지 대입이 아니다.** 같은 프레임에 다시 읽으면 옛 값이 나오고, 브라우저가 사용자 제스처를 요구하거나 아예 거부할 수도 있다. 에디터에서는 거의 즉시 반영돼 **빌드에서만 드러난다.**

→ **화면은 「의도」를 그리고, 실제 값은 따라잡는다.** 누른 직후 `Screen.fullScreen` 을 읽어 표시를 갱신하면 옛 값이 그려져 **한 번 더 눌러야 바뀌는 토글**이 된다.

또한 사용자는 `Esc` 로 **우리 UI 를 거치지 않고** 전체화면을 빠져나올 수 있다. 실제 값이 우리가 마지막으로 관측한 값과 달라졌는지 주기적으로 확인해 표시를 맞춰야 한다 — 이때 「적용」을 다시 부르면 안 된다. 사용자가 이미 한 일을 우리가 되풀이하는 것이라 진동한다.

> 위 여섯은 모두 `#if`·컴파일 심볼·애셋 검증 테스트로 **에디터에서 미리 고정할 수 있다.**
> 빌드해서 발견하면 이미 비싸다.

## 5. 생명주기 콜백은 「만들자마자」 돌지 않는다

### 5-1. 비활성 오브젝트의 `Awake` 는 첫 활성화까지 밀린다

`Instantiate` 한 시점이 아니라 **처음 활성화되는 시점**에 돈다. `OnEnable` 도 같다. 따라서 비활성 상태에서 부른 public 메서드는 **`Awake` 가 잡아 둘 참조를 전부 `null` 로 본다.**

```csharp
// ❌ 바인딩이 활성화보다 먼저 — Awake 가 아직 안 돌아 내부 참조가 전부 null
view.Bind(data);
view.gameObject.SetActive(true);

// ✅ 켜고 나서 물린다
view.gameObject.SetActive(true);
view.Bind(data);
```

**이 버그의 서명은 「두 번째부터 정상」이다.** 같은 오브젝트를 재사용하면 이미 한 번 활성화된 뒤라 `Awake` 가 돌아 있다. 재시작하면 멀쩡해 보이므로 **간헐 버그로 오진하기 쉽다.** 객체 풀은 비활성으로 미리 만들어 두는 구조라 이 함정을 정면으로 밟는다.

→ 순서를 지키는 것만으로는 부족하다. 외부에서 부를 수 있는 진입점이라면 **멱등한 지연 초기화**를 맨 앞에 둔다. `Awake` 도 그것을 부르기만 한다.

```csharp
private bool _resolved;

private void Resolve()
{
    if (_resolved) return;
    _resolved = true;
    // 참조 수집
}

private void Awake() => Resolve();
public void Bind(Data data) { Resolve(); /* ... */ }
```

> **테스트로 고정할 수 있다.** 오브젝트를 **비활성으로 만들어 놓고** `Bind` 를 부른 뒤 결과를
> 단언하면 된다. 활성 상태로만 세우는 테스트는 이 경로를 영영 밟지 않는다.
