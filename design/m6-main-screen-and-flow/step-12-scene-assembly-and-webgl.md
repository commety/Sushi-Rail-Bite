# Step 12: `Main.unity` 신설 · Build Settings · 문서 정정

- **영역:** `scene` — 씬 애셋 + Build Settings + 문서 정정
- **선행 단계:** step-01 ~ step-11 **전부**
- **후행 단계:** [step-13](step-13-stage01-scene-assembly.md) — `Stage01` 조립

> ## 범위가 줄었다 (아키텍트 결정)
>
> **`Stage01` 조립과 WebGL 확인은 [step-13](step-13-stage01-scene-assembly.md) 으로 갈라졌다.**
> 인스테이지 조립이 메인 씬보다 크고, 신설이 아니라 기존 씬 개조라 성격이 다르다.
>
> 이 단계에서 끝난 것:
>
> - `Main.unity` 신설 + `MainSceneTests` (`2a0bf57`)
> - Build Settings 에 `Main` 첫 번째 등록 + `BuildSettingsTests` (`516a5a9`)
> - `SampleScene.unity` 삭제 (승인, GUID 참조 0건 확인)
> - 패널 닫힘 → 메뉴 복귀 버그 수정 + `MainBootstrap` (`1dec412`)
> - UI 클릭음 재생 경로 — 버튼 전용 (`4b84294`)
> - 원문 문서 정정 (§8)
>
> **step-13 으로 넘어간 것**: §2 `Stage01` 조립 전부 · §5 의 `Stage01SceneTests` 확장 ·
> §6 재생 확인 · §7 WebGL 확인. 조사 결과는 step-13 에 실측으로 옮겨 적었다 —
> 다시 조사하지 않아도 된다.

---

## 목적

앞의 열한 단계가 만든 것을 씬에 놓고, **브라우저에서 실제로 되는지 확인한다.**

이 단계에는 M5 가 값비싸게 배운 두 가지가 걸려 있다:

1. **씬을 Build Settings 에 등록하지 않으면 빌드는 성공하고 화면은 빈다.** 예외도 안 난다
2. **`scripts/run.sh` 는 빌드 후 공유 애셋을 원복한다.** `EditorBuildSettings.asset` 을 고쳤으면 **빌드 전에 커밋해야** 남는다 (RULE-02)

---

## 워크트리 금지

씬 편집은 한 번에 한 워크트리다 ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §3). 씬은 텍스트여도 사실상 머지가 불가능하다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 1) `Main.unity` 신설

`Assets/Level/Scenes/Main.unity` — ClaudeBridge 로 조립한다 ([`unity-editor-automation.md`](../../.claude/knowledge/unity-editor-automation.md)).

```
Main
├── Main Camera        (AudioListener 포함)
├── EventSystem        ← InputSystemUIInputModule (StandaloneInputModule 아님)
├── SceneRouter
└── MainCanvas         (Canvas + CanvasScaler + GraphicRaycaster)
    ├── MainMenuView   (TitleLabel · StartButton · SettingsButton · CodexButton)
    ├── SettingsView   (헤더 고정 · 볼륨 슬라이더 · 전체화면 토글 · 닫기)  ← 처음엔 꺼져 있다
    └── CodexView      (카드 11장 자리 · 닫기)                            ← 처음엔 꺼져 있다
```

> **`InputSystemUIInputModule` 이어야 한다.** 이 프로젝트는 `ENABLE_LEGACY_INPUT_MANAGER` 가 정의되어 있지 않아 `StandaloneInputModule` 이 런타임에 죽는다 ([`presentation-and-audio.md`](../../.claude/domain/presentation-and-audio.md) §6). **에디터에서는 경고만 뜨고 넘어갈 수 있다** — 빌드에서 클릭이 통째로 안 먹는 형태로 드러난다.

### 2) `Stage01.unity` 조립

`Stage01` 에 **`EventSystem` 이 없다** (실측 0건). 없으면 버튼도 드래그도 배달되지 않는다.

추가/변경:

| 대상 | 무엇 |
|---|---|
| `EventSystem` | **신규** — `InputSystemUIInputModule` |
| `SceneRouter` | 신규 (Quit 의 목적지) |
| `CustomerHandView` | 신규 — 손패 카드 자리 (명부 상한만큼) |
| `DeckPanelView` + 덱 버튼 | 신규 |
| `StageMenuView` + 메뉴 버튼 | 신규 |
| `RewardSelectionView` | **개조** — 카드 3장 + 건너뛰기 버튼 |
| `TableSlot*/CustomerView` | **개조** — 포화도 칸 8개 + 소화중 배지 |
| `PlacementInput` 오브젝트 | **삭제** (step-04 이 컴포넌트를 지웠다) |
| `StageBootstrap` | 새 참조 배선 |
| **프리팹·뷰의 빈 스프라이트 자리** | **step-11 에서 넘어온 것** — 아래 |

#### step-11 에서 넘어온 스프라이트 배선 (아키텍트 결정)

step-11 이 스프라이트 9종을 만들었지만 **프리팹에 물리지는 않았다.** 어차피 에디터를 열어야
하는 작업이라 씬 조립과 묶는 편이 왕복이 적다는 판단이다.

| 스프라이트 | 물릴 곳 |
|---|---|
| `card-frame` · `card-frame-disabled` | `Card.prefab` 의 배경 `Image` (못 놓는 손님 카드는 후자) |
| `badge-digesting` | `DigestingBadgeView` 의 `Image` — **16×16 크기 그대로** |
| `bar-cell` | `SaturationBarView` 의 칸 8개 |
| `button` · `button-pressed` | 메뉴·설정·건너뛰기·닫기 버튼의 `Image` (9-slice, `Type = Sliced`) |
| `panel` | 덱·메뉴·설정·사전 패널 배경 (9-slice, `Type = Sliced`) |
| `icon-deck` · `icon-menu` | 인스테이지 두 버튼의 아이콘 |

> **9-slice 는 `Image.type` 을 `Sliced` 로 바꿔야 먹는다.** 임포터 테두리만 넣고 `Simple` 로
> 두면 테두리가 무시되고 통째로 늘어난다 — 값은 맞는데 화면만 찌그러지는 형태라 눈으로
> 원인을 못 찾는다. 테두리 자체는 `UiSpriteAssetTests` 가 이미 고정했다.

> **아이콘이 비면 프리팹의 그림을 그대로 둔다** 는 M5 규칙 때문에, 배선을 빠뜨려도 **코드도
> 테스트도 초록이다.** 씬 테스트(§5)에 스프라이트가 실제로 물렸는지 보는 항목을 넣는다.

### 3) Build Settings (§7 · RULE-06)

```diff
  m_Scenes:
+ - enabled: 1
+   path: Assets/Level/Scenes/Main.unity
  - enabled: 1
    path: Assets/Level/Scenes/Stage01.unity
```

**`Main` 이 첫 번째다** — 빌드를 열면 메인 화면이 떠야 한다.

**승인 후 즉시 커밋한다.** `run.sh` 가 빌드 후 되돌리므로, 커밋하지 않으면 다음 빌드가 다시 이전 상태를 싣는다 (RULE-02 «원복은 의도한 변경도 함께 지운다»).

### 4) `SampleScene.unity` 처분 (§7)

파일은 남아 있고 Build Settings 에는 없다. **지울지 물어본다.** 에이전트가 단독으로 씬을 지우지 않는다.

### 5) 씬 테스트 — 하네스가 구조적으로 못 밟는 것을 여기서 잡는다

코드로 세운 PlayMode 하네스는 **뷰 계층을 지나친다** ([`tests.md`](../../.claude/rules/tests.md) §1). 씬 애셋 자체를 보는 테스트는 `Stage01SceneTests` 뿐이었다 — M6 은 씬이 둘이므로 하나 더 필요하다.

```
Assets/Tests/PlayMode/MainSceneTests.cs        — 신규
Assets/Tests/PlayMode/Stage01SceneTests.cs     — 확장
Assets/Tests/EditMode/BuildSettingsTests.cs    — 신규
```

봐야 할 것:

```
빌드 등록  BuildSettings_ContainsMainScene          ← M5 가 빠뜨린 그 자리
           BuildSettings_MainIsFirst
           BuildSettings_PathsMatchSceneNames       ← SceneNames 상수와 실제 경로 대조
Main 씬    MainScene_HasEventSystem
           MainScene_UsesInputSystemUIModule        ← 레거시 모듈이면 실패
           MainScene_HasMainMenuView
           MainScene_SettingsAndCodexStartHidden
Stage 씬   Stage01_HasEventSystem                   ← 지금 0건인 것
           Stage01_UsesInputSystemUIModule
           Stage01_HasHandDeckAndMenuViews
           Stage01_HasNoPlacementInput              ← 지운 컴포넌트가 씬에 남지 않았는가
           Stage01_CustomerViewHasSaturationCells
```

> **`BuildSettings_PathsMatchSceneNames` 가 step-09 의 오타를 잡는 유일한 지점이다.** `SceneNames.Stage` 를 `"Stage1"` 로 잘못 써도 컴파일은 통과하고 프레젠터 테스트도 통과한다 — 실제 경로와 대조해야만 드러난다.

> **`Stage01_HasNoPlacementInput` 을 잊지 않는다.** 컴포넌트를 지우면 씬에는 **Missing Script** 로 남는다. 조용하고, 재생하면 경고만 뜬다.

### 6) 재생 확인 (사람이 눈으로)

- [ ] 메인 → 게임 시작 → 스테이지가 뜬다
- [ ] 카드를 끌어 테이블에 놓으면 손님이 앉는다. 잔액이 모자란 카드는 흐리다
- [ ] 포화도 칸이 차오르고, 소화 중에 배지가 뜬다
- [ ] 덱 버튼 → 지금 덱이 보인다. **시간은 계속 흐른다** (D8)
- [ ] 메뉴 → 멈춘다. **벨트가 선다.** 재개하면 다시 흐른다
- [ ] 재시작 → 같은 판이 다시 열리고 명부가 유지된다
- [ ] 나가기 → 메인으로 돌아온다
- [ ] 클리어 → 보상 카드 3장 + 건너뛰기. 카드를 누르면 그것이 덱에 들어온다
- [ ] 설정 → 볼륨이 즉시 줄고, 닫았다 다시 열면 값이 유지된다
- [ ] 백과사전 → 초밥 8 · 손님 3

### 7) WebGL 빌드 확인

```bash
./scripts/run.sh webgl
```

- [ ] **한글 두부(□) 0개** — 새 문구 전부. 이것이 이 단계의 가장 큰 확인이다
- [ ] 콘솔 예외 0
- [ ] 클릭·드래그가 동작한다 (`InputSystemUIInputModule` 확인)
- [ ] 전체화면 토글이 동작한다 (WebGL 제약 — step-10 의 주석 참조)
- [ ] 창 크기를 바꿔도 HUD·패널이 유지된다 — **M5 가 확인하지 못하고 넘긴 항목이다**
- [ ] 초기 로드 크기를 재고 M5 기준선(42.45MB)과 비교해 기록한다

> **자동화 브라우저는 캔버스에 포커스를 못 줘 Unity 를 심하게 스로틀한다** (5초에 게임 1초). 소리·연출의 **귀와 눈 확인은 사람이 한다** ([`presentation-and-audio.md`](../../.claude/domain/presentation-and-audio.md) §9).

### 8) 원문 문서 정정

`docs/plan/M5-M9-later.md` §M6 이 이번 결정과 어긋난다. 고친다:

| 원문 | 실제 |
|---|---|
| *"진입 전 손님 고르기·덱빌딩"* | 손님 선택은 **스테이지 안 드래그앤드롭**으로 옮겼다. 덱 편집 화면은 만들지 않았다 (README D9) |
| *"`RunState` 가 씬을 넘어 유지돼야 하므로 Q1(저장/영속)의 답이 여기서도 쓰인다"* | **2씬 구조에서는 런이 씬을 넘지 않는다.** 저장/영속은 여전히 미결이며 M6 이 그것을 필요로 하지 않았다 (README D1) |
| *"덱빌딩 화면은 MVP 패턴 필수"* | 덱빌딩은 없지만 **MVP 는 다섯 화면에 적용됐다** — 메인·설정·백과사전·메뉴·덱 보기 |
| 튜토리얼 | M6 에서 하지 않았다 |

`.claude/domain/` 에도 M6 의 화면 계층을 남긴다 — `/task-done` 이 판단하되, **씬 두 장의 역할 분담과 `PauseState` 의 수명**은 코드에서 읽어 낼 수 없는 것이라 기록 대상이다. 새 파일을 만들면 `.claude/INDEX.md` 도 함께 갱신한다.

### 완료 판정

- [x] `Main.unity` 가 Build Settings 에 **첫 번째로** 등록됐고 **커밋됐다** (`516a5a9`)
- [x] `Main` 이 `EventSystem` + `InputSystemUIInputModule` 을 갖는다 — `Stage01` 은 step-13
- [x] 씬 테스트 — `MainSceneTests` · `BuildSettingsTests` 초록. `Stage01SceneTests` 확장은 step-13
- [x] `grep -rn "PlacementInput" Assets/Level/Scenes/` = **0건** (이미 0이었다. 회귀 고정은 step-13)
- [ ] 재생 확인 10항목 → **step-13**
- [ ] WebGL 확인 6항목 → **step-13**
- [x] `docs/plan/M5-M9-later.md` §M6 정정 완료
- [x] `./tests/run-tests.sh all` Green
- [x] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(scene): add main scene and wire in-stage ui
```

빌드 설정 변경은 **별도 커밋**으로 먼저 낸다 — 되돌릴 때 씬 조립과 분리돼야 한다.

```
chore(build): register main scene as the entry point
```

---

## 금지 사항

- **워크트리에서 씬을 편집하지 않는다**
- 승인 없이 `EditorBuildSettings.asset` 을 바꾸지 않는다 (RULE-06)
- 승인 없이 `SampleScene.unity` 를 지우지 않는다
- `StandaloneInputModule` 을 쓰지 않는다
- 빌드 설정 변경을 커밋하지 않은 채 빌드하지 않는다 (RULE-02)
- 씬에서 확인한 문제를 코드 단계로 되돌리지 않고 씬에서 임시 봉합하지 않는다 — 원인을 고친다
