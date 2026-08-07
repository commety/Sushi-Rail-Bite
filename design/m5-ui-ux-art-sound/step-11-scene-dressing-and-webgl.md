# Step 11: 배경 · 벨트 비주얼 · 레이아웃 + WebGL 실측

- **영역:** `scene` — 씬 애셋 + `Tests.PlayMode` + 문서
- **선행 단계:** **step-01 ~ 10 전부**
- **후행 단계:** 없음 (M5 마감)

---

## 목적

앞의 열 단계가 만든 것을 `Stage01.unity` 에 실제로 배치하고, **WebGL 빌드로 확인한다.**

원문 계획의 남은 항목이 여기 있다 — *배경, 초밥 라인*. 그리고 지금까지의 모든 판단이 실제로 맞았는지 확인하는 자리다: 한글이 나오는가, 드로우콜이 늘지 않는가, 소리가 들리는가, 초기 로드가 감당되는가.

**에디터에서 멀쩡한데 빌드에서 죽는 것이 이 마일스톤의 대표 실패 모드다** (한글 폰트, 자동재생 정책, 아틀라스). 빌드해 보지 않으면 M5 는 끝난 것이 아니다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 수정 파일

- `Assets/Level/Scenes/Stage01.unity` — 수정
- `Assets/Tests/PlayMode/Stage01SceneTests.cs` — 추가
- `docs/plan/M5-M9-later.md` — 수정 (§M5 병렬 가능 문단 정정)
- `.claude/domain/presentation-and-audio.md` — 생성
- `.claude/INDEX.md` — 수정 (새 도메인 문서 등록)

> **씬 편집은 한 번에 한 워크트리만** ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §3). 이 단계는 메인 프로젝트에서 단독으로 실행한다.

### 씬에 들어갈 것

| 오브젝트 | 무엇 |
|---|---|
| `Background` | 가게 배경. `raycastTarget` 을 **끈다** — 켜 두면 손님 배치 클릭이 UI 에 먹힌다 |
| `BeltRail` | 벨트 라인 비주얼. 지금 `BeltStart`/`BeltEnd` 는 빈 `Transform` 이라 **레일이 화면에 없다** — 초밥이 허공을 떠다닌다 |
| `Canvas` | step-10 의 Screen Space Overlay. 기존 라벨 7개를 자식으로 |
| `AudioDirector` | step-07. `AudioSource` 두 개(BGM/SFX) |
| `EffectPool` | step-08 |

**기존 오브젝트를 지우지 않는다.** `Hud`·`RewardView`·`StageTransition`·`TableSlot0~3`·`BeltStart`/`BeltEnd`·`SushiPool` 은 `StageBootstrap.ResolveMissingReferences` 가 **이름으로 찾는다.** 이름을 바꾸거나 계층을 옮기면 참조가 조용히 끊긴다.

> **M3 에서 직렬화 필드 이름을 바꿔 씬의 손님 참조가 통째로 날아간 적이 있다.** 코드도 테스트도 멀쩡한데 재생만 죽는 형태라 가장 비싼 실패 모드다 (`tests.md` §1).

### 조립 방법

ClaudeBridge 를 쓴다 (`.claude/knowledge/unity-editor-automation.md`):

```
Scene.Open      → Assets/Level/Scenes/Stage01.unity
GameObject.Create / Component.Add / Component.SetField / Component.SetRectTransform
Prefab.InstantiateAsChild
Scene.Save
```

**커맨드를 10개 이상 쌓았으면 `/run bridge` 로 일괄 실행한다** — op 마다 Editor 를 왕복하면 느리다. Editor 가 이미 열려 있으면 배치 모드 진입이 실패하므로 `unity_bridge_status()` 로 먼저 확인한다.

### `Stage01SceneTests` 에 추가할 것

**씬 애셋 자체가 검증 대상인 것은 이 파일뿐이다** (`tests.md` §1). 인스펙터 참조·오브젝트 존재·활성 상태에 기대는 변경은 전부 여기에 남긴다.

```
Play_Scene_HasCanvasWithHudLabels          ← Canvas 아래에 라벨이 붙어 있다
Play_Scene_HudLabelsUseKoreanFont          ← 폰트가 물려 있다 (두부 방지 최종 관문)
Play_Scene_HasAudioDirectorWired           ← 뱅크·소스가 물려 있다
Play_Scene_HasEffectPoolWired
Play_Scene_SushiSpritesDifferByType        ← step-05 가 씬에서도 동작한다
Play_Scene_BackgroundDoesNotBlockRaycast   ← 배치 클릭이 막히지 않는다
Play_Scene_BeltRailSpansStartToEnd         ← 레일이 벨트 구간을 덮는다
```

`Play_Scene_BackgroundDoesNotBlockRaycast` 는 **이 단계에서 가장 나기 쉬운 사고**를 잡는다. 전체 화면 배경 이미지의 `raycastTarget` 이 켜져 있으면 손님을 앉힐 수 없게 되는데, 다른 모든 테스트는 코드로 `Placement.Place(...)` 를 직접 불러서 **전부 초록으로 통과한다.**

### WebGL 실측

**빌드해서 브라우저에서 연다.** 확인 항목:

| 항목 | 기준 | 실패 시 |
|---|---|---|
| **한글 표시** | HUD·전환·손님 이름에 두부(□)가 **0개** | step-09 의 서브셋에 글자를 추가 |
| **초기 로드 크기** | 값을 **기록**한다 (M8 의 기준선) | 압축·아틀라스 재검토 |
| **드로우콜** | 초밥 종류가 늘어도 **비례해 늘지 않는다** | 아틀라스가 안 걸린 것 — step-04 재확인 |
| **BGM** | 첫 클릭 **이후** 재생된다 | 자동재생 게이트 확인 (step-07) |
| **SFX 겹침** | 손님 4명이 동시에 먹어도 찢어지지 않는다 | 쿨다운·상한 조정 (step-06, §7 승인) |
| **창 크기 변경** | HUD 가 유지된다 | `CanvasScaler` 설정 (step-10) |
| **프레임** | 히칭이 없다 | `Update` 경로 할당 확인 |
| **Enter 입력** | 전환·보상 화면이 Enter 로 넘어간다 | 아래 |

> **미해결: 레거시 `Input` 이 이 프로젝트에서 동작하는가.**
> `ProjectSettings` 의 `activeInputHandler` 가 `1` 인데 `RewardSelectionView` 와
> `StageTransitionView` 는 `UnityEngine.Input.GetKeyDown` 을 쓴다. 브리지로 `Input.GetKeyDown`
> 을 불러 봤을 때 `TargetInvocationException` 이 났지만 **내부 예외를 확인하지 못해 확정하지
> 못했다** — 배치모드라서 났을 수도 있다.
>
> 두 뷰의 `Update` 는 `_presenter.IsOpen` 이 아니면 즉시 반환하므로, **PlayMode 테스트는 이
> 경로를 아예 밟지 않는다.** 재생해야만 드러난다.
>
> step-07 의 `AudioDirector` 는 이 답에 의존하지 않게 설계했다(첫 배치 클릭으로 잠금 해제).
> 하지만 **Enter 가 안 먹으면 스테이지 전환 자체가 막힌다** — 여기서 반드시 확인한다.

> WebGL 은 **싱글 스레드**다. `Task.Run` 류는 동작하지 않는다. M5 가 그런 코드를 넣지 않았는지 함께 확인한다 — [`unity-webgl-performance.md`](../../.claude/knowledge/unity-webgl-performance.md) §0

빌드는 `Assets/Settings/Build Profiles/` 의 기존 프로필을 쓴다. **프로필을 수정하지 않는다** (§7 — 빌드 파이프라인).

### 원문 정정 — `docs/plan/M5-M9-later.md`

§M5 의 이 문장이 사실과 다르다:

> *M2 이후 로직과 병렬 가능하다. 파일이 겹치지 않으므로 워크트리를 나눠 동시에 진행할 수 있다*

`Assets/Art` 와 `Assets/Audio` 는 워크트리에서 **심링크**이고 (`scripts/ensure-worktree-setup.sh` 의 `SYMLINK_ASSET_DIRS`), M5 의 결과물 대부분이 그 폴더로 간다. 워크트리에서 만들면 파일이 메인 프로젝트에 생기고 워크트리 git 이 보지 못한다.

**애셋 단계(step-04·06·09)와 씬 단계(step-11)는 워크트리에서 실행할 수 없다**는 사실을 그 문단에 반영한다.

### 문서로 남길 것

`.claude/domain/presentation-and-audio.md` (신규):

- **아이콘 바인딩 경로** — 데이터의 `Icon` → 뷰. 새 초밥을 추가할 때 코드를 고치지 않는 이유
- **오디오 3층** — 뱅크(값) / 예산·게이트(판정) / 디렉터(재생). 어디에 무엇을 넣어야 하는지
- **자동재생 정책** — 왜 `Start()` 에서 BGM 을 재생하면 안 되는지
- **임포트 규칙의 적용 범위** — 어느 경로가 픽셀 규칙 대상이고 왜 폰트는 제외인지
- **연출 수치 vs 밸런스 수치의 구분선** (README D7)
- **`SushiEatenEventChannelSO` 의 미결 처분** (D5) — M6 의 숙제로 명시
- **실측값** — WebGL 초기 로드 크기, 드로우콜, 애셋 용량

`.claude/INDEX.md` 에 항목과 `keywords` 를 추가한다. **누락하면 다음 세션의 `/task-start` 가 선별 로드를 못 한다.**

### 밸런스 수치

없음. step-06 에서 확정된 값을 쓴다. 실측 결과 조정이 필요하면 **§7 승인을 다시 받는다.**

### 제약

- **씬 오브젝트 이름을 바꾸지 않는다** — `ResolveMissingReferences` 가 이름으로 찾는다
- `ProjectSettings/`·빌드 프로필을 수정하지 않는다 (§7·RULE-06)
- `.meta` 를 직접 편집하지 않는다 (RULE-03)
- 워크트리에서 실행하지 않는다
- 스크린샷을 외부에 올릴 때는 **저해상도/워터마크** (§9)

### 완료 판정

- [ ] `./tests/run-tests.sh all` Green
- [ ] `Stage01SceneTests` 신규 7건 통과
- [ ] **WebGL 빌드가 통과하고 브라우저에서 열린다**
- [ ] 위 실측 표 7항목 전부 기록됨
- [ ] `docs/plan/M5-M9-later.md` §M5 정정 완료
- [ ] `.claude/domain/presentation-and-audio.md` + `INDEX.md` 등록 완료
- [ ] [README 의 M5 전체 완료 판정](README.md#완료-판정-m5-전체) 9항목 전부 체크
- [ ] `./tests/preflight.sh` 전 항목 PASS (**`--fast` 가 아니다** — 마감 커밋이다)

### 예상 커밋 메시지

```
feat(stage): dress stage scene with background, rail and canvas
test(stage): cover scene wiring for canvas, audio and effects
docs(plan): correct M5 worktree parallelism claim
docs(domain): record presentation and audio decisions
```

---

## 금지 사항

- 씬 오브젝트를 이름 변경·삭제하지 않는다
- 빌드 프로필·`ProjectSettings/` 를 수정하지 않는다 (§7)
- **WebGL 실측을 건너뛰고 M5 를 끝내지 않는다.** 이 마일스톤의 대표 실패 모드가 전부 빌드에서만 드러난다
- 실측 결과를 예측으로 채우지 않는다. 값을 모르면 **모른다고 적는다**
- 워크트리에서 실행하지 않는다
