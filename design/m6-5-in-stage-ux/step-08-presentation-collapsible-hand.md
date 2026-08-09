# Step 08: 접히는 손패

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-07 (커진 카드) · **step-03 이 머지된 뒤에 시작한다** (같은 파일)
- **후행 단계:** step-09 가 씬에 컨테이너를 놓는다

---

## 목적

카드가 128×192 가 되면 **인스테이지 손패가 화면을 먹는다.** 평소에는 아래로 내려 일부만 보이고, 마우스를 가져가면 올라온다.

---

## ⚠ 먼저 — 파일 충돌

**step-03 도 `CustomerHandView.cs` 를 고친다** ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §3). 이 단계는 step-03 이 `dev` 에 머지된 뒤 **rebase 하고** 시작한다. 병렬로 가면 merge conflict 가 거의 확실하다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한다. STEP 3.5 에서 step-03 의 머지 여부를 확인한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Customers/CustomerHandView.cs` — 수정
- `Assets/Tests/PlayMode/Customers/CustomerHandViewTests.cs` — 수정

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    public sealed class CustomerHandView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        /// <summary>
        /// 접힘·펼침으로 움직일 대상. <b>카드가 아니라 그 부모다</b> — 카드를 직접 옮기면
        /// 드래그가 돌아갈 자리(<c>CustomerCardDrag._home</c>)가 접힌 위치를 가리킨다.
        /// </summary>
        [SerializeField] private RectTransform _tray;

        /// <summary>접혔을 때 화면에 남길 높이(px). 연출 수치다.</summary>
        [SerializeField, Min(0f)] private float _peekHeight = 48f;

        /// <summary>펼쳐지는 데 걸리는 시간(초). 0 이면 즉시.</summary>
        [SerializeField, Min(0f)] private float _slideSeconds = 0.12f;

        /// <summary>지금 펼쳐져 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsExpanded { get; private set; }

        /// <summary>포인터를 거치지 않는 진입점. 테스트·터치 폴백이 쓴다.</summary>
        public void SetExpanded(bool expanded);
    }
}
```

### 왜 컨테이너를 움직이나 (README D9)

`CustomerCardDrag._home` 은 `Resolve()` 시점의 `anchoredPosition` 이고, 드래그가 끝나면 무조건 거기로 돌아간다.

- 카드 자체를 애니메이션하면 → **카드가 올라온 상태에서 끌었을 때 `_home` 이 접힌 위치**를 가리켜 카드가 화면 밖으로 돌아간다
- 부모 `_tray` 를 움직이면 → 카드의 로컬 좌표가 변하지 않아 `_home` 이 계속 유효하다

**`CustomerCardDrag` 를 이 단계에서 고치지 않는다.** 고쳐야 할 것 같으면 컨테이너를 안 움직이고 있는 것이다.

### 호버와 탭 (배포 타깃이 웹이다)

- **호버는 터치에 없다.** 모바일 브라우저가 사정권이므로 `IPointerClickHandler` 로 **탭 토글** 경로를 같이 연다
- 펼친 상태에서 카드를 끌면 접히지 않는다 — 드래그 중에는 `IsExpanded` 를 유지한다. 드래그가 끝나면 포인터 위치에 따라 정한다
- **접혀 있어도 카드를 끌 수 있어야 하는가**: 아니다. 접힘 상태의 노출 영역은 «올리는 손잡이» 이고, 배치는 펼친 뒤에 한다. 접힌 상태에서 시작된 드래그는 **먼저 펼치고** 그 프레임의 드래그는 무시한다 — 반쯤 보이는 카드를 끌어 자리에 놓는 조작은 좌표가 어긋난다

### 애니메이션

`_slideSeconds` 동안 `_tray.anchoredPosition.y` 를 보간한다.

- **코루틴이 아니라 `Update` 안의 보간을 쓴다.** 코루틴은 `WaitForSeconds` 가 `Time.timeScale` 에 묶이는데, 이 프로젝트는 `timeScale` 을 안 건드리는 대신 `PauseState` 를 쓴다 (M6 D4) — 손패는 멈춤 중에도 움직여야 하므로 게이트 밖에 둔다
- **`Update` 안에서 할당하지 않는다.** `Vector2` 는 구조체라 안전하다. LINQ·문자열 결합을 넣지 않는다
- 목표 위치에 닿으면 보간을 멈춘다 — 매 프레임 `anchoredPosition` 을 계속 쓰면 레이아웃이 매번 더럽혀진다

### 선행 산출물 의존성

- step-07 의 128×192 카드 — 접힘 높이가 그 크기를 전제한다

### 밸런스 수치

없다. `_peekHeight`·`_slideSeconds` 는 **연출 수치**라 프리팹의 `[SerializeField]` 다 (M5 D7).

### 제약

- `Runtime` 을 건드리지 않는다
- **`CustomerCardDrag` 를 고치지 않는다** (README D9)
- 이벤트 구독이 생기면 `OnDestroy`/`OnDisable` 에서 해제한다 ([`scripts.md`](../../.claude/rules/scripts.md) §6)
- `Update` 에서 `GetComponent` 를 부르지 않는다
- **step-03 의 변경(`Refresh`·`CanPlaceAnywhere`)에 손대지 않는다.** 같은 파일이라 스코프를 엄격히 지킨다

### 테스트 계획

```
SetExpanded_True_RaisesTheTray             ← _tray 의 y 가 목표로 간다 (구체값)
SetExpanded_False_LowersToPeekHeight       ← 반례. «올라간다» 만 보면 상수 구현도 통과한다
SetExpanded_DoesNotMoveTheCards            ← 카드의 anchoredPosition 이 그대로다.
                                              D9 의 회귀 그물 — 이게 핵심이다
DropAt_WhileExpanded_ReturnsCardToItsHome  ← 펼친 채 끌어 놓아도 카드가 제자리로
PointerEnter_Collapsed_Expands             ← 바깥 껍데기를 지나는 경로
PointerClick_Expanded_Collapses            ← 터치 폴백
PointerExit_WhileDragging_StaysExpanded
```

- **`SetExpanded_DoesNotMoveTheCards` 가 이 단계에서 가장 중요한 테스트다.** 카드를 직접 움직이는 구현으로 되돌아가면 여기서만 죽는다
- **`_slideSeconds = 0` 으로 테스트한다.** 보간이 억제 장치라, 시간이 걸리는 값으로 두면 «움직이지 않는다» 와 «아직 안 움직였다» 가 구분되지 않는다 (`tests.md` §3 «다른 장치가 주입을 가릴 수 있다» — 간격 0.06초짜리 큐에서 겪은 것과 같은 형태다)

### 완료 판정

- [ ] `grep -n "_tray" Assets/Code/Scripts/Presentation/Customers/CustomerHandView.cs` — 필드 + 보간
- [ ] `git diff --stat` 에 `CustomerCardDrag.cs` 가 **없다**
- [ ] `grep -n "StartCoroutine" Assets/Code/Scripts/Presentation/Customers/CustomerHandView.cs` — 0건
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] 깨뜨려 보기: `_tray` 대신 카드를 직접 옮기게 고치면 `SetExpanded_DoesNotMoveTheCards` 와 `DropAt_WhileExpanded_ReturnsCardToItsHome` 이 죽는지 확인하고 되돌린다

### 예상 커밋 메시지

```
feat(ui): keep the in-stage hand folded until it is reached for
```

---

## 금지 사항

- **step-03 이 머지되기 전에 시작하지 않는다.** 같은 파일이다.
- `CustomerCardDrag._home` 계산을 고치지 않는다.
- 씬을 편집하지 않는다 (step-09).
- 접힘 높이·전개 시간을 SO 로 빼지 않는다.
