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

`Screen.fullScreen = true` 는 **요청이지 대입이 아니다.** 같은 프레임에 다시 읽으면 옛 값이 나오고, 브라우저는 사용자 제스처를 요구한다. 에디터에서는 거의 즉시 반영돼 **빌드에서만 드러난다.**

**제스처 밖의 요청은 무시되지 않는다 — 미뤄진다.** 브라우저가 그것을 큐에 남겼다가 **다음 제스처에 실행**하므로, 시작할 때 저장값을 적용하면 **사용자가 무엇을 누르든 그 순간 전체화면이 된다.** 설정을 연 적도 없는데 화면이 뒤집히므로 원인과 결과가 멀어져 찾기 어렵다.

→ **불러온 값을 화면에 밀어 넣지 않는다.** 화면 상태는 브라우저가 들고 있으므로, 시작할 때는 **읽어서 모델을 맞추고** 실제 전환은 사용자가 토글을 누를 때만 한다. 소리처럼 제스처가 필요 없는 설정은 그대로 적용해도 된다 — 둘을 한 「적용」에 묶은 것이 사고의 원인이었다.

→ **화면은 「의도」를 그리고, 실제 값은 따라잡는다.** 누른 직후 `Screen.fullScreen` 을 읽어 표시를 갱신하면 옛 값이 그려져 **한 번 더 눌러야 바뀌는 토글**이 된다.

또한 사용자는 `Esc` 로 **우리 UI 를 거치지 않고** 전체화면을 빠져나올 수 있다. 실제 값이 우리가 마지막으로 관측한 값과 달라졌는지 주기적으로 확인해 표시를 맞춰야 한다 — 이때 「적용」을 다시 부르면 안 된다. 사용자가 이미 한 일을 우리가 되풀이하는 것이라 진동한다.

### 4-7. 임포터 설정과 실제 스프라이트가 다를 수 있다

`TextureImporter.spriteBorder` 는 **Single 모드용**이다. `spriteMode` 가 Multiple 이면 Unity 는 **스프라이트 시트 항목의 `border`** 를 쓰고 최상위 값은 무시한다. 둘이 어긋나면 **임포터에는 테두리가 있는데 화면은 그냥 늘어난다.**

→ **검사는 입력이 아니라 결과를 읽는다.** `AssetDatabase.LoadAssetAtPath<Sprite>` 로 실제 스프라이트를 열어 `Sprite.border` 를 본다. 임포터 필드를 보는 테스트는 「넣었는데 화면은 그대로」를 영영 못 잡는다 — 실제로 그 형태로 9-slice 가 오래 죽어 있었다.

같은 함정이 pivot·`spritePixelsPerUnit` 에도 있다. **입력을 검증하는 테스트는 결과를 보장하지 않는다.**

### 4-8. 시트 `.png` 이름을 바꿔도 칸 이름은 따라오지 않는다

파일을 개명해도 `.meta` 안의 하위 스프라이트 이름은 **옛 이름 그대로 남는다.** 당장은 아무 일도 없다 — 클립·프리팹은 이름이 아니라 `internalID` 로 물고 있기 때문이다.

**다음 재임포트가 터뜨린다.** 슬라이서가 파일 이름 기준으로 칸 이름을 다시 짓는 순간 새 `internalID` 가 발급되고, 옛 ID 를 물고 있던 참조는 **`null` 로 역직렬화된다.** 예외도 로그도 없다. 개명 시점과 고장 시점이 **몇 커밋 떨어져 있어** 원인 추적이 어렵다.

→ 개명했으면 **그 자리에서** `ImportAsset(path, ForceUpdate)` 로 칸 이름을 맞추고, 그 시트를 쓰는 클립을 다시 굽는다. **「지금 멀쩡하다」는 안전의 근거가 아니다** — 임포터의 `GetVersion()` 을 올리는 것만으로도 터진다.

> 위 여덟은 모두 `#if`·컴파일 심볼·애셋 검증 테스트로 **에디터에서 미리 고정할 수 있다.**
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

## 6. uGUI 는 화면 좌표와 캔버스 좌표가 다르다

`PointerEventData.delta`·`position` 은 **화면 픽셀**이고 `RectTransform.anchoredPosition` 은 **캔버스 단위**다. `CanvasScaler` 가 배율을 걸면 둘이 어긋나는데, 델타를 **누적해서 더하면 오차도 함께 쌓여** 끌수록 벌어진다. 배율이 1인 개발 창에서는 안 보이고 전체화면에서 커진다.

```csharp
// ❌ 화면 델타를 캔버스 좌표에 누적한다 — 배율만큼 어긋나고 오차가 쌓인다
_rect.anchoredPosition += eventData.delta;

// ✅ 매번 변환해 대입한다. 누적이 없으므로 오차도 안 쌓인다
if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
        (RectTransform)_rect.parent, eventData.position, eventData.pressEventCamera, out var local))
{
    _rect.anchoredPosition = local + _grabOffset;   // 집은 지점의 차이를 기억해 순간이동을 막는다
}
```

> **테스트는 배율을 1이 아닌 값으로 세운다.** 1이면 두 좌표계가 같아져 틀린 구현도 통과한다.

### 6-1. `IsPointerOverGameObject()` 는 실행 순서에 의존한다

이 API 는 **입력 모듈이 그 프레임에 이미 돌았는지**에 달려 있다. 우리 `Update` 가 먼저 돌면 「uGUI 위가 아니다」가 나오고, 그 순서는 씬마다 다르다 — **어떤 씬에서는 맞고 어떤 씬에서는 틀린다.**

→ 순서에 기대지 않으려면 `EventSystem.RaycastAll` 로 **직접 묻는다.** 결과 목록은 재사용한다.

### 6-2. 가상 입력은 `InputTestFixture` 를 지나야 장치에 닿는다

`InputSystem.AddDevice<Mouse>()` 로 장치를 만들고 `QueueStateEvent` 로 상태를 넣어도, 픽스처 밖에서는 **이벤트가 장치에 반영되지 않는다** — 네이티브 백엔드가 상태를 소유한 채이기 때문이다. 누름 0회, 버튼 레벨 `000` 으로 실측됐다.

→ 테스트 클래스가 `InputTestFixture` 를 **상속**하고 그쪽의 `Press`/`Release`/`Set` 을 쓴다. `Setup`/`TearDown` 을 `override` 하고 `base` 를 부른다. 그것 없이 `InputSystem.Update()` 를 직접 부르면 **프레임보다 먼저 소비되어** `MonoBehaviour.Update` 가 도는 시점에는 `wasPressedThisFrame` 이 이미 내려가 있다.

> 이 셋은 **입력을 테스트로 덮으려 할 때 반드시 만난다.** 그리고 6-1 은 테스트를 붙이는
> 과정에서 프로덕션 버그로 드러났다 — 안 붙였으면 「가끔 틀린다」로 만났을 것이다.

## 7. 켜진 `Animator` 는 자기가 애니메이션하는 값을 매 프레임 되쓴다

클립이 묶고 있는 프로퍼티(`m_Sprite`·transform·색…)의 **소유권은 `Animator` 에 있다.** 코드로 대입해도 다음 평가에서 클립 값으로 덮여, **한 프레임 깜빡이고 사라진다.** 예외도 경고도 없다.

- 정적인 그림을 보이려면 **`Animator` 를 끈다** (`enabled = false`). 성능이 아니라 소유권 문제다
- `LateUpdate` 에서 쓰면 그 프레임은 살아남지만 **다음 프레임에 다시 덮인다** — 순서로 이길 수 없다
- 반대로 **클립이 건드리지 않는 프로퍼티는 안전하다.** 스프라이트를 애니메이션하면서 색은 코드가 계속 쥐고 있어도 충돌하지 않는다

> 「대입했는데 화면이 그대로」일 때 가장 먼저 볼 것은 그 오브젝트에 `Animator` 가 붙어
> 있는지, 그 클립이 그 프로퍼티를 묶고 있는지다. 참조가 끊긴 것으로 오해하고 애셋 쪽을
> 파면 한참 돌아간다.
