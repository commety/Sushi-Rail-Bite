# Step 10: HUD · 보상 · 전환을 uGUI Canvas 로

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-09 (한글 폰트) — **폰트 없이 전환하면 화면이 전부 두부가 된다**
- **후행 단계:** step-11 이 씬에서 레이아웃을 잡는다. M6 의 메인화면·덱빌딩이 이 토대를 그대로 쓴다

---

## 목적

인스테이지 UI 가 전부 월드스페이스 `TextMesh` 다. Canvas 가 없다.

**브라우저 창 크기가 바뀌면 무너진다.** 월드스페이스 텍스트는 카메라 뷰포트에 고정되지 않아서, 창을 좁히면 잘리고 넓히면 화면 구석에 떠 있는다. 웹 데모에서 창 크기는 사용자가 언제든 바꾸는 값이다.

M6 의 메인화면·덱빌딩은 어차피 Canvas 를 깔아야 한다. **여기서 미리 깔면 M6 이 그만큼 싸진다.**

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 수정 파일

- `Assets/Code/Scripts/Presentation/Presentation.asmdef` — **수정 (§7 승인 필요)**
- `Assets/Code/Scripts/Presentation/UI/PlaceholderLabel.cs` → `HudLabel.cs` — 개명·수정
- `Assets/Code/Scripts/Presentation/UI/StageHudView.cs` — 수정
- `Assets/Code/Scripts/Presentation/Views/RewardSelectionView.cs` — 수정
- `Assets/Code/Scripts/Presentation/Views/StageTransitionView.cs` — 수정
- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정 (대역 라벨)
- 테스트 4종 — 필요한 만큼만 수정

### §7 승인 요청 — asmdef 수정

```diff
  {
    "name": "Presentation",
    "rootNamespace": "SushiDefense",
-   "references": ["Runtime", "Runtime.Data"],
+   "references": ["Runtime", "Runtime.Data", "Unity.TextMeshPro"],
    "autoReferenced": false
  }
```

- `com.unity.ugui` 2.5.0 이 이미 `manifest.json` 에 있다. **새 패키지 추가가 아니다** — 참조만 는다
- `autoReferenced: false` 를 유지한다 (RULE-01)
- 의존 방향 위반 없음 — `Presentation` 이 가장 위다
- `Tests.EditMode` 와 `Tests.PlayMode` 도 TMP 타입을 보게 되면 같은 참조가 필요할 수 있다. **필요해지면 함께 요청한다**

**이 diff 를 제시하고 승인을 받은 뒤 진행한다.**

### 핵심 원칙 — 뷰의 public 계약을 바꾸지 않는다 (README D2)

```csharp
// 유지 — 이 프로퍼티들이 전환 작업의 회귀 그물이다
public string RevenueText { get; private set; }
public string WalletText { get; private set; }
public string PlacementText { get; private set; }
public string WaitingText { get; private set; }
public string TimeText { get; private set; }
public string OutcomeText { get; private set; }
public string PendingCustomerText { get; private set; }
public string BandText { get; private set; }   // CustomerView

// 유지 — 시그니처 그대로
public void Bind(...);
public void Unbind();

// 바뀌는 것은 내부뿐
- [SerializeField] private TextMesh _revenueLabel;
+ [SerializeField] private TMP_Text _revenueLabel;
```

**`StageHudViewTests` · `RewardSelectionViewTests` · `StageTransitionViewTests` · `CustomerViewTests` 를 고치지 않고 통과시키는 것이 이 단계의 목표다.** 계약까지 같이 바꾸면 테스트를 고치면서 하는 전환이 되고, 그 순간 그물이 사라진다.

테스트를 고쳐야만 통과한다면 **계약을 바꾸고 있다는 신호다.** 멈추고 왜 그런지 보고한다.

### `PlaceholderLabel` → `HudLabel`

지금 클래스 주석이 *"M6 에서 제대로 된 UI 로 교체되며 사라진다"* 고 적혀 있는데, **이 단계가 그 교체다.** 더는 placeholder 가 아니므로 이름과 주석을 함께 정정한다.

역할은 그대로 유지한다:
- 인스펙터가 비면 **자기 하위 계층에서만** 이름으로 찾아 채운다 (`Find`/`FindObjectOfType` 이 아니다 — §4.3)
- 라벨이 없어도 조용히 넘어간다 (**표시는 로직의 전제 조건이 아니다**)
- 폰트를 채우는 부분은 step-09 의 TMP 폰트 애셋을 가리키게 바꾼다

`TextMesh` 는 폰트가 비면 아무것도 그리지 않았다. **`TMP_Text` 도 마찬가지다** — `font` 가 `null` 이면 조용히 빈 화면이 된다. 폴백 경로를 유지한다.

### D1 — 주석 네 곳을 정정한다

`PlaceholderLabel` · `StageHudView` · `RewardSelectionView` · `StageTransitionView` 가 *"M6 에서 교체된다"* 고 적고 있다. 원문 계획은 UI 를 M5 에 두었으므로 **경계를 여기서 확정한다**:

> 스테이지 안에서 뜨는 것 = M5 / 메인화면·덱빌딩·설정·백과사전 = M6

네 파일의 주석을 이 문장에 맞게 고친다.

### Canvas 구성

- **Screen Space - Overlay** 하나. 월드스페이스로 두면 지금 문제가 그대로 남는다
- `CanvasScaler` — `Scale With Screen Size`, 기준 해상도 고정, `Match` 는 데모 화면비에 맞춰
- **`GraphicRaycaster` 는 실제로 클릭을 받는 UI 에만.** 손님 배치 클릭이 벨트 위에서 일어나므로, 전체 화면을 덮는 Raycast Target 이 있으면 **클릭이 UI 에 먹혀 배치가 안 된다.** 배경 이미지의 `raycastTarget` 을 끈다
- 씬 오브젝트 구성 자체는 **step-11** 이다. 이 단계는 코드가 `TMP_Text` 를 받을 수 있게 만드는 데까지

### 선행 산출물 의존성

- step-09 의 `SushiRailBite-KR.asset`

### 밸런스 수치

없음. 폰트 크기·색·앵커는 씬/프리팹의 직렬화 값이다 (README D7 — 사람이 밸런싱하며 만질 값이 아니다).

### 제약

- **뷰의 public 계약을 바꾸지 않는다** (D2)
- `Update`/`LateUpdate` 경로에 할당을 만들지 않는다 (§4). 기존 뷰들이 **값이 바뀐 프레임에만** 문자열을 만드는 최적화를 이미 하고 있다 — 전환하면서 이것을 잃지 않는다
- `TMP_Text.text` 대입도 바뀐 프레임에만
- 구독 해제 경로(`Unbind`/`OnDestroy`)를 유지한다 (§6)
- `FindObjectOfType` 금지 (§4.3)
- **`CustomerView` 의 대역 라벨은 월드스페이스로 남길지 결정한다.** 손님 자리 옆에 붙어 있어야 의미가 있으므로 Screen Space Overlay 로 옮기면 자리와 어긋난다 → **월드스페이스 `TextMeshPro` (`TextMeshPro` 컴포넌트, `TextMeshProUGUI` 아님) 로 전환**하고 위치는 유지한다. 폰트 문제는 똑같이 해결된다

### 테스트 계획

**새 테스트보다 기존 테스트를 살리는 것이 우선이다.**

- 기존 PlayMode 뷰 테스트 4종이 **수정 없이** 통과해야 한다
- 추가할 것:
  - `Canvas_WindowResized_KeepsLabelsOnScreen` — 이 단계를 하는 이유 자체를 고정한다. 뷰포트를 바꾼 뒤 라벨의 스크린 좌표가 화면 안에 남는지
  - `Label_WithoutFont_DoesNotThrow` — 폴백 경로

> **코드로 세운 하네스는 씬 사고를 못 잡는다** (`tests.md` §1). 이 프로젝트의 PlayMode 테스트는 대부분 구성을 코드로 만들어 뷰 계층을 지나친다. **씬 애셋이 검증 대상인 것은 `Stage01SceneTests` 뿐**이므로, 인스펙터 참조에 기대는 변경은 step-11 에서 그쪽에도 테스트를 남긴다.

### 주입으로 확인한다

| 주입 | 잡혀야 할 테스트 (가설) |
|---|---|
| 라벨 문자열 대입 제거 | 기존 뷰 테스트 다수 |
| `Unbind` 에서 구독 해제 제거 | 기존 수명 테스트 |
| `CanvasScaler` 모드를 `Constant Pixel Size` 로 | `Canvas_WindowResized_KeepsLabelsOnScreen` |

**예측은 가설이다.** M4 에서 세 번 빗나갔고, 그중 하나는 *"자리 전부 비활성화 → 기존 PlayMode 다수"* 가 실제로는 **0건**이었다. 실측으로 정정한다.

### 완료 판정

- [ ] `grep -rn "TextMesh " Assets/Code/Scripts/Presentation/ --include="*.cs"` 가 **0건** (`TextMeshPro` 는 별개 문자열이므로 걸리지 않는다)
- [ ] `grep -rn "PlaceholderLabel" Assets/ --include="*.cs"` 가 **0건**
- [ ] `grep -c "Unity.TextMeshPro" Assets/Code/Scripts/Presentation/Presentation.asmdef` = 1 (**승인 후**)
- [ ] 기존 PlayMode 뷰 테스트 4종이 **수정 없이** Green
- [ ] `./tests/run-tests.sh all` Green
- [ ] D1 주석 정정 4곳 확인
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

asmdef 와 전환을 나눈다 — 되돌리는 이유가 다르다:

```
chore(presentation): reference textmeshpro assembly
refactor(ui): move in-stage views to ugui canvas
```

---

## 금지 사항

- 승인 없이 `.asmdef` 를 수정하지 않는다 (`CLAUDE.md` §7)
- **뷰의 public 계약을 바꾸지 않는다.** 테스트를 고쳐야 통과한다면 멈추고 보고한다
- 씬 파일을 편집하지 않는다 — step-11 이다
- 메인화면·덱빌딩 UI 를 만들지 않는다 (M6)
- 다른 어셈블리 파일을 수정하지 않는다
