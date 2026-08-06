# Step 06: 전환 화면 뷰

- **영역:** `presentation` — 어셈블리 `Presentation` (+ `Tests.PlayMode`)
- **선행 단계:** step-05 완료 필요 (`IStageTransitionView` 시그니처 확정)
- **후행 단계:** step-08 이 씬 진입점에서 프레젠터와 묶는다. step-10 이 씬에 오브젝트를 만든다

---

## 목적

프레젠터가 정한 문구를 화면에 옮긴다. **규칙이 하나도 없다** — 무엇을 보여줄지, 다음 스테이지가 있는지는 프레젠터가 이미 답했다.

`RewardSelectionView` 와 **완전히 같은 형태**로 만든다. 씬에 Canvas 가 없고 전부 월드 스페이스라 `TextMesh` 를 쓴다. **M6 에서 제대로 된 UI 로 교체될 placeholder 다.**

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

- `Assets/Code/Scripts/Presentation/Views/StageTransitionView.cs` — 생성
- `Assets/Tests/PlayMode/UI/StageTransitionViewTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    public sealed class StageTransitionView : MonoBehaviour, IStageTransitionView
    {
        private const string MessageLabelName = "StageTransitionLabel";

        [SerializeField] private TextMesh _messageLabel;

        public string MessageText { get; private set; }   // 검증용
        public bool IsShowing { get; private set; }

        public void Bind(StageTransitionPresenter presenter);

        public void ShowStageCleared(int clearedStageNumber, int nextStageNumber);
        public void ShowRunComplete(int clearedStageNumber);
        public void Hide();
    }
}
```

### 표시 문구

| 상태 | 문구 |
|---|---|
| 스테이지 클리어 | `스테이지 {N} 클리어 — 다음: 스테이지 {N+1} (Enter)` |
| 런 종료 | `스테이지 {N} 클리어 — 런 완료! (Enter)` |

**조작 키를 문구에 넣는다.** `RewardSelectionView` 가 `(Esc)` 를 넣은 것과 같은 이유 — placeholder UI 에 버튼이 없으므로 문구가 유일한 안내다.

### 입력

`Update()` 에서 Enter/Return 을 읽어 `_presenter?.Proceed()` 를 부른다. **화면이 떠 있을 때만 읽는다** (`IsShowing` 가드). 기존 `RewardSelectionView` 의 입력 처리 방식을 그대로 따른다 — 그 파일이 쓰는 입력 API 를 확인하고 **같은 것을 쓴다** (새 입력 시스템을 도입하지 않는다, §7).

### `PlaceholderLabel` 재사용

라벨 해석(`Resolve`)과 쓰기(`Write`)는 기존 `Assets/Code/Scripts/Presentation/UI/PlaceholderLabel.cs` 를 쓴다. 인스펙터가 비면 자기 하위에서 `StageTransitionLabel` 을 찾는다 — **씬 전역 탐색이 아니다** (§4.3).

### 테스트 목록 (`StageTransitionViewTests`)

기존 `Assets/Tests/PlayMode/UI/RewardSelectionViewTests.cs` 의 `SetUp`/`TearDown` 형태를 그대로 따른다 (`GameObject` 생성 → `SerializedObject` 로 라벨 주입 → `DestroyImmediate`).

```
ShowStageCleared_Stage1_WritesBothNumbers      ← "1" 과 "2" 가 모두 문구에 있다
ShowRunComplete_LastStage_WritesCompletionText
ShowStageCleared_ThenRunComplete_ReplacesInsteadOfAppending
Hide_AfterShow_ClearsLabel
IsShowing_AfterHide_IsFalse
```

> `ShowStageCleared_Stage1_WritesBothNumbers` 에서 **"1" 만 확인하지 않는다.** 다음 번호가 빠진 구현이 통과한다. 두 숫자를 모두 `StringAssert.Contains` 로 박고, 클리어 문구와 종료 문구가 **서로 다른지**도 확인한다.

### 선행 산출물 의존성

- `SushiDefense.UI.IStageTransitionView` · `StageTransitionPresenter` — step-05

### 밸런스 수치

없음. 문구는 표시 문자열이지 밸런스가 아니다.

### 제약

- **판정하지 않는다.** 다음 스테이지 유무를 뷰가 되묻지 않는다 (§3.2·§3.6)
- 이벤트 구독(`+=`)을 쓰면 `OnDestroy`/`OnDisable` 에서 반드시 해제한다 ([`rules/scripts.md`](../../.claude/rules/scripts.md) §6). **이 뷰는 구독이 필요 없는 설계**이므로 필요해졌다면 설계를 다시 본다
- `Update()` 안에서 문자열 결합·할당을 만들지 않는다 — 배포 타깃이 WebGL 이다 (§4.3). 문구는 `Show*` 시점에 한 번만 만든다
- `Instantiate`/`Destroy` 를 프로덕션 코드에서 부르지 않는다 (§3.4)
- PlayMode 테스트는 느리다. **씬·컴포넌트가 실제로 필요한 것만** 여기 둔다

### 완료 판정

- [ ] `grep -rn "HasNextStage\|IsRunComplete\|AdvanceAfterClear" Assets/Code/Scripts/Presentation/Views/StageTransitionView.cs` 가 **0건** (뷰가 판정하지 않는다)
- [ ] `./tests/run-tests.sh all` PlayMode 포함 전량 Green
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(stage): add placeholder stage transition view
```

---

## 금지 사항

- 씬을 편집하지 않는다 (step-10). 이 단계는 **컴포넌트만** 만든다.
- `StageBootstrap` 을 건드리지 않는다 (step-08).
- 새 UI 패키지·TextMeshPro 도입 금지 (§7). 기존 `TextMesh` 방식을 유지한다.
