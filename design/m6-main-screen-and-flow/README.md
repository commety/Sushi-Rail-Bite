# M6 — 메인 화면 · 스테이지 진입 · 조작

## 한 줄 요약

게임에 **입구와 출구**를 만든다 — 메인 화면에서 들어가고, 스테이지 안에서 카드를 끌어다 손님을 앉히고 덱을 보고 일시정지하고, 보상을 카드로 고르고, 메인으로 나온다.

## 원문

[`docs/plan/M5-M9-later.md`](../../docs/plan/M5-M9-later.md) §M6 — *메인 화면, 백과사전, 튜토리얼, 설정, 진입 전 손님 고르기·덱빌딩.*

여기에 아키텍트가 이번에 추가한 6개 요구가 붙는다:

| # | 요구 | 이 작업서에서 |
|---|---|---|
| 1 | 손님 카드 **드래그앤드롭** 배치 / **덱 UI 버튼** / **Menu 버튼**(Pause·Play·Restart·Quit) | step-04 · step-06 · step-07 |
| 2 | 메인 화면 = 게임 시작 · 설정 · 백과사전. 설정은 볼륨 · 전체화면만 | step-09 · step-10 |
| 3 | **재사용 가능한 카드 틀** — 대표 이미지만 끼워 넣는다 | step-03 |
| 4 | 보상 선택도 **카드**로, 클릭 가능, 최하위에 **건너뛰기** 버튼 | step-08 |
| 5 | **포화도 UI** — 칸 바, 찰 때마다 노란색 | step-05 |
| 6 | 비활성 시 손님 위 **16×16px 소화중 표시** | step-05 |

## 아키텍트 결정 (4문 4답)

| 물음 | 답 |
|---|---|
| 카드 틀이 등급마다 다른가 | **등급 무관 단일 틀** — 아이콘·이름·수치만 데이터에서 온다 |
| 메인과 스테이지를 어떻게 나누나 | **`Main.unity` + `Stage01.unity` 2씬** |
| 기존 클릭 배치는 | **드래그앤드롭으로 대체** — 입력 경로를 하나로 |
| 설정의 볼륨은 | **마스터 하나** |

## 착수 전 실측 — 지금 저장소의 상태

작업서를 쓰기 전에 확인한 사실이다. 추측이 아니라 `grep`·파일 조회 결과다.

| 확인 대상 | 상태 | 영향 |
|---|---|---|
| **`EventSystem`** | `Stage01.unity` 에 **0건** | **uGUI 버튼·드래그가 하나도 동작하지 않는다.** 씬에 `EventSystem` + `InputSystemUIInputModule` 이 없으면 클릭이 아예 배달되지 않는다 → `Main` 은 step-12 에서 넣었고, `Stage01` 은 step-13 |
| `Presentation.asmdef` 참조 | `Runtime`, `Runtime.Data`, `Unity.TextMeshPro`, `Unity.InputSystem` | **`UnityEngine.UI` 가 없다.** `Button`·`Image`·`Slider`·`IBeginDragHandler` 가 전부 그 어셈블리다 → §7 승인 (step-03) |
| `SceneManager.LoadScene` | 프로덕션 코드 **0건** (테스트·브리지에만 있음) | 씬 전환이 통째로 신규 → step-09 |
| `Time.timeScale` | **0건** | 일시정지 방식이 아직 정해지지 않았다 → D4 |
| `Screen.*` · `AudioListener.volume` | **0건** | 설정 적용 경로가 통째로 신규 → step-10 |
| `RewardCatalog.asset` | 초밥 **3장** · 손님 **2명**뿐 (보상 전용 풀) | **백과사전의 출처가 될 수 없다.** 시작 덱 5종이 빠져 있다 → step-01 이 `CardCatalog` 를 신설 |
| `IRewardSelectionView.ShowOffers` | `IReadOnlyList<string>` | 카드에는 아이콘이 필요하다. **계약을 바꿔야 한다** → D2 |
| 이름 폴백 로직 | `RewardOffer.DisplayName` · `CustomerView.NameOf` · `StageHudView.DisplayNameOf` **3벌** | M6 이 카드까지 더하면 4벌이 된다 → D7 |
| `CustomerData.MaxSaturation` | 기본 5 · 소식 3 · 먹보 8 · Placeholder **99** | 포화도 칸을 고정 배열로 두면 99 에서 깨진다 → D6 |
| `SushiData` | `_description` 필드가 **없다** (`CustomerData` 에만 있다) | 백과사전의 초밥 항목은 수치로만 채운다 → step-10 |
| `charset-ko.txt` | 199자 (한글 99) | M6 문구 전부가 **두부**가 된다. 재추출 + 재굽기 필요 → step-11 |
| `Assets/Level/UI/` | `.gitkeep` 뿐 | UI 프리팹의 자리는 **기능 폴더**다 ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §2) |
| `Assets/Level/Scenes/SampleScene.unity` | 파일은 남아 있고 Build Settings 에는 없다 | step-12 에서 **지웠다** (승인, GUID 참조 0건) |

## 아키텍처 결정

- **D1. 런은 스테이지 씬 안에서만 산다. `DontDestroyOnLoad` 를 만들지 않는다.**
  원문은 *"M3 의 `RunState` 가 씬을 넘어 유지돼야 하므로 Q1(저장/영속)의 답이 여기서도 쓰인다"* 고 적었는데, **2씬 구조에서는 그 전제가 서지 않는다.** 3스테이지 전부가 `Stage01.unity` 한 씬 안에서 `StageBootstrap.Build()` 재호출로 진행되기 때문이다 — 런이 씬 경계를 넘는 순간이 없다. `게임 시작` 은 새 런을 열고, `Quit` 은 런을 버린다. **저장/이어하기는 M6 의 범위가 아니다.** 이 결정이 세션 객체·정적 상태(RULE-01)·직렬화 계층을 통째로 이번 마일스톤에서 뺀다.

- **D2. `IRewardSelectionView` 의 계약을 바꾼다. M5 의 D2 를 여기서 푼다.**
  M5 는 *"뷰의 public 계약을 바꾸지 않는다"* 를 회귀 그물로 삼았지만, 문자열 목록으로는 카드에 아이콘을 그릴 수 없다. `ShowOffers(IReadOnlyList<string>)` → `ShowOffers(IReadOnlyList<RewardOffer>)` 로 바꾼다. **바꾸는 것이 하나뿐임을 명시**하고, `RewardSelectionPresenter` 의 나머지 계약(`Choose`·`Skip`·`OfferCount`·`IsOpen`·이벤트)은 손대지 않는다 — 프레젠터 테스트가 그대로 살아 그물 역할을 계속한다.

- **D3. 카드는 틀 하나다. 등급으로 분기하지 않는다.**
  카드 프리팹은 `CardView` 하나이고 아이콘·이름·수치만 데이터에서 온다. 등급 개념을 지금 코드에 넣지 않는 이유는 **넣을 데이터가 없기 때문**이다 — `SushiData`·`CustomerData` 에 등급 필드가 없고, 만들면 밸런스 애셋 12장 일괄 수정(§7)이 따라온다. 가격 티어는 이미 **접시 테두리 색**이 지고 있다 (M5, [`presentation-and-audio.md`](../../.claude/domain/presentation-and-audio.md) §1) — 같은 정보를 카드 테두리로 한 번 더 표현하면 축이 둘로 갈린다. 등급이 실제 기획으로 확정되면 그때 프리팹 배리언트를 늘린다.

- **D4. 일시정지는 `Time.timeScale` 을 건드리지 않는다.**
  이 게임의 시간은 전부 `StageBootstrap.Update` → `StageController.Tick(Time.deltaTime)` 한 줄을 지난다. **그 한 줄을 건너뛰면 벨트·손님·시계가 전부 멈춘다.** `timeScale = 0` 은 UI 애니메이션과 `WaitForSeconds` 까지 함께 얼리고([`unity-scripting-gotchas.md`](../../.claude/knowledge/unity-scripting-gotchas.md) §2), 무엇보다 **전역 상태라 누가 풀었는지 추적할 수 없다.** 게이트는 순수 객체(`PauseState`)로 두어 EditMode 로 검증한다.

- **D5. 프레젠터는 `Presentation` 어셈블리에 남는다 — 다만 Unity 를 모른다.**
  기존 `RewardSelectionPresenter`·`StageTransitionPresenter` 가 이미 그 자리에 있고 `using UnityEngine` 이 없다. M6 의 프레젠터 다섯(`MainMenu`·`Settings`·`Codex`·`StageMenu`·`DeckPanel`)도 같은 규칙을 따른다. **씬 전환·`PlayerPrefs`·`Screen` 같은 Unity 호출은 인터페이스 뒤로 민다** (`ISceneRouter`·`ISettingsStore`) — 그래야 "게임 시작을 누르면 스테이지로 간다" 를 EditMode 로 확인할 수 있다.

- **D6. 포화도 칸은 하나의 식으로 처리한다. 분기하지 않는다.**
  `MaxSaturation` 은 3~8 이 정상이지만 Placeholder 애셋에 **99** 가 들어 있다. 칸을 `MaxSaturation` 개 만들면 99칸이 그려지고, `Instantiate` 는 풀 밖에서 금지다(§3.4). 프리팹에 칸을 **8개 미리 놓고**, 표시 칸 수 `min(max, 칸수)` · 채워진 칸 `ceil(표시칸수 × current / max)` 로 계산한다. `max ≤ 칸수` 이면 이 식은 `current` 와 정확히 같아지므로 **정상 구간에 근사가 끼지 않는다.** 산술은 `SaturationGauge` 한 곳이다.

- **D7. 이름 폴백을 네 번째로 복사하지 않는다.**
  *"표시 이름이 비면 애셋 이름"* 이 이미 세 곳에 있다. 카드가 네 번째가 되면 같은 손님이 화면마다 다르게 불릴 위험이 그만큼 는다. step-03 이 `CardCaption` 에 한 벌로 두고 `CustomerView`·`StageHudView` 의 것을 **거기로 접는다** (둘 다 `Presentation`, 같은 단계). `RewardOffer.DisplayName` 은 `Runtime` 이라 남기되, step-08 에서 **카드가 대신하면 쓰이지 않게 되는지** 확인하고 그때 처분한다.

- **D8. 덱 보기는 일시정지하지 않는다.**
  덱 확인은 정보 조회이고, 멈추는 것은 Menu 의 일이다. 둘을 묶으면 "덱을 열면 왜 시간이 멈추지" 와 "일시정지하려고 덱을 연다" 가 동시에 생긴다. 한 화면이 두 가지를 하면 나중에 한쪽만 고쳐진다.

- **D9. 튜토리얼과 덱빌딩 편집 화면은 M6 이 하지 않는다.**
  원문 §M6 과 `CLAUDE.md` §1.3 은 *"메인 화면: 스테이지 선택 + 덱빌딩"* 을 적고 있지만, 이번 요구는 메인을 **버튼 3개**로 못박았고 손님 선택은 스테이지 안 드래그앤드롭으로 옮겼다. **덱 편집은 아직 기획이 없다** — 로그라이트 덱은 보상으로만 자라고 카드를 빼는 경로가 없다(`SushiDeck` 주석). 편집 화면을 지금 만들면 편집할 대상이 없다. 원문과의 차이는 step-12 에서 `docs/plan/M5-M9-later.md` §M6 에 정정으로 남겼다.

- **D10. 애셋 등록을 완료 판정에 박는다.**
  M5 는 Build Settings 에 씬을 등록하지 않아 **빌드가 성공하고 화면은 빈** 상태를 만들었다 ([`tests.md`](../../.claude/rules/tests.md) §1 «애셋 등록·설정»). M6 은 씬을 하나 더 만든다 — 같은 사고가 두 배로 가능하다. `Main.unity` 의 등록 여부를 **테스트가 본다** (step-12).

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | `Runtime.Data` | `CardCatalog` — 백과사전·덱 보기의 출처 / `AudioBankSO` UI 클릭 큐 |
| runtime | `Runtime` | `PauseState` · `GameSettings` · `ISettingsStore` · `SaturationGauge` — 판정과 계산 |
| presentation | `Presentation` | 카드 틀, 드래그 배치, 손님 상태 UI, 덱·메뉴·보상·메인·설정·백과사전 |
| tests | `Tests.EditMode` / `Tests.PlayMode` | 위 전부 + 씬 애셋 검증 |
| — | (애셋) | UI 스프라이트 · 카드 프리팹 · 한글 서브셋 재굽기 · 씬 2장 |

## 의존성 그래프

```
SushiDefense.Data.CardCatalog ─────────────► SushiDefense.UI.CodexPresenter
SushiDefense.Stages.PauseState ────────────► SushiDefense.UI.StageMenuPresenter ──► StageBootstrap.Update
SushiDefense.Settings.GameSettings ────────► SushiDefense.UI.SettingsPresenter ───► ISettingsStore (Presentation 구현)
SushiDefense.Customers.SaturationGauge ────► SushiDefense.Customers.SaturationBarView
SushiDefense.Run.RewardOffer ──────────────► SushiDefense.UI.CardView
                                             SushiDefense.UI.ISceneRouter ───────► SceneRouter (SceneManager)
```

`Runtime` 은 `Presentation` 을 여전히 모른다. 씬 전환·저장·화면 설정은 **인터페이스가 방향을 뒤집는다** (D5).

## 새 밸런스 수치

| 수치 | 들어갈 곳 | 초기값 제안 | 근거 |
|---|---|---|---|
| UI 클릭 볼륨 | `AudioBankSO._uiClick` | `0.4` | 먹힘(0.5)보다 작게. 메뉴에서 연타되는 소리다 |
| UI 클릭 간격(초) | `AudioBankSO._uiClick` | `0.05` | 연타로 뭉개지는 것만 막는다 |
| 마스터 볼륨 기본값 | (코드, `GameSettings`) | `1.0` | 밸런스가 아니라 "설정을 만진 적 없음" 의 값이다 |
| 포화도 칸 수 | (프리팹 `[SerializeField]`) | `8` | 먹보의 `MaxSaturation` 이다. 연출 수치 — D6 |
| 카드 크기·간격 | (프리팹 `[SerializeField]`) | — | 연출 수치 (M5 D7 과 같은 구분선) |
| 드래그 인정 반경 | (프리팹 `[SerializeField]`) | `1.2` | 기존 `CustomerPlacementInput._pickRadius` 를 그대로 옮긴다 |

> **`.asset` 에 값을 쓰는 것은 사람의 판단 영역이다** (`CLAUDE.md` §7). step-01·step-11 이 diff 를 제시하고 승인을 기다린다.

## §7 승인이 필요한 항목

각 단계가 그 지점에서 멈추고 요청한다. 에이전트가 단독으로 진행하지 않는다.

| # | 항목 | 단계 |
|---|---|---|
| 1 | `Presentation.asmdef` 에 `"UnityEngine.UI"` 참조 추가 | step-03 |
| 2 | `Tests.EditMode.asmdef` · `Tests.PlayMode.asmdef` 에 `"UnityEngine.UI"` 추가 | step-03 |
| 3 | `CardCatalog.asset` 신설 + 초밥 8 · 손님 3 참조 | step-01 |
| 4 | `AudioBank.asset` 에 UI 클릭 큐 값·클립 | step-01 · step-11 |
| 5 | 새 UI 스프라이트 · 효과음 커밋 (§9 원본 보호) | step-11 |
| 6 | **한글 폰트 재굽기** — TMP Font Asset Creator 는 사람이 돌린다 | step-11 |
| 7 | Build Settings 에 `Main.unity` 등록 (RULE-06) | step-12 |
| 8 | `SampleScene.unity` 처분 (삭제 여부) | step-12 — **삭제함** |

## 단계

| # | 파일 | 영역 | 무엇 |
|---|---|---|---|
| 01 | [step-01-data-card-catalog.md](step-01-data-card-catalog.md) | data | `CardCatalog` SO · UI 클릭 큐 |
| 02 | [step-02-runtime-pause-and-settings.md](step-02-runtime-pause-and-settings.md) | runtime | `PauseState` · `GameSettings` · `ISettingsStore` · `SaturationGauge` |
| 03 | [step-03-presentation-card-view.md](step-03-presentation-card-view.md) | presentation | 재사용 카드 틀 + 이름 폴백 통합 |
| 04 | [step-04-presentation-drag-placement.md](step-04-presentation-drag-placement.md) | presentation | 손님 카드 드래그앤드롭 배치 |
| 05 | [step-05-presentation-customer-status.md](step-05-presentation-customer-status.md) | presentation | 포화도 칸 바 · 소화중 배지 |
| 06 | [step-06-presentation-deck-panel.md](step-06-presentation-deck-panel.md) | presentation | 인스테이지 초밥 덱 보기 |
| 07 | [step-07-presentation-stage-menu.md](step-07-presentation-stage-menu.md) | presentation | Menu — Pause·Play·Restart·Quit |
| 08 | [step-08-presentation-reward-cards.md](step-08-presentation-reward-cards.md) | presentation | 보상 선택을 카드로 + 건너뛰기 |
| 09 | [step-09-presentation-main-menu.md](step-09-presentation-main-menu.md) | presentation | 메인 화면 + 씬 라우팅 |
| 10 | [step-10-presentation-settings-and-codex.md](step-10-presentation-settings-and-codex.md) | presentation | 설정 · 백과사전 |
| 11 | [step-11-assets-ui-sprites-and-font.md](step-11-assets-ui-sprites-and-font.md) | assets | UI 스프라이트 · 클릭음 · **한글 서브셋 재굽기** |
| 12 | [step-12-scene-assembly-and-webgl.md](step-12-scene-assembly-and-webgl.md) | scene | `Main.unity` 신설 · Build Settings · 문서 정정 |
| 13 | [step-13-stage01-scene-assembly.md](step-13-stage01-scene-assembly.md) | scene | `Stage01` 조립 · 재생·WebGL 확인 — **step-12 에서 갈라짐** |

## 병렬 실행 가능성

**M5 와 같은 이유로 실행은 직렬로 한다.** step-11 은 `Assets/Art/` 를 건드리고(심링크, RULE-02), step-12 는 씬을 건드린다(한 번에 한 워크트리, [`parallel-work.md`](../../.claude/rules/parallel-work.md) §3). 아래는 *논리적* 독립성이며, 한 단계가 막혔을 때 무엇을 먼저 돌릴지 판단하는 데 쓴다.

```
01 ──┬────────────────────────► 10
02 ──┼──► 05                    │
     ├──► 07 ──┐                │
03 ──┼──► 04   ├──► 09 ─────────┤
     ├──► 06   │                │
     └──► 08 ──┘                │
                                ▼
11 ──────────────────────────► 12
```

- **step-01 · 02 · 03 은 서로 독립이다.** 02 는 SO 타입을 컴파일 의존하지 않는다
- **step-04 · 06 · 08 은 step-03 뒤에서 병렬 가능** — 셋 다 `CardView` 를 쓰지만 서로 다른 파일을 만든다
- **step-05 는 step-02 뒤라면 언제든** — 다른 단계와 파일이 겹치지 않는다
- **step-11 은 코드 단계 전부 뒤에 와야 한다.** 폰트 서브셋이 **코드의 문자열 리터럴에서 추출**되기 때문이다 (`scripts/extract-charset.py`). 순서가 뒤집히면 굽고 나서 다시 굽는다
- **씬 조립이 마지막.** 씬은 모든 결과가 모이는 곳이다 — step-12(메인) → step-13(스테이지)

## 완료 판정 (M6 전체)

- [ ] 메인 화면에서 **게임 시작**을 누르면 스테이지가 열린다
- [ ] 손님 카드를 **끌어다 테이블에 놓으면** 손님이 앉는다. 잔액·한도·점유가 막으면 카드가 제자리로 돌아온다
- [ ] **덱 버튼**으로 지금 초밥 덱을 확인할 수 있다 — 보상으로 얻은 카드가 거기 보인다
- [ ] **Menu** 로 멈추고 · 재개하고 · 다시 시작하고 · 메인으로 나갈 수 있다. 멈춘 동안 **벨트가 서 있다**
- [ ] 보상을 **카드로 클릭해** 고른다. **건너뛰기**로 아무것도 안 받고 넘어간다
- [ ] 손님 위에 **포화도 칸**이 차고, 소화 중이면 **16×16 배지**가 뜬다
- [ ] 설정에서 **볼륨과 전체화면**이 바뀌고, 다시 켜도 유지된다
- [ ] 백과사전에서 **초밥 8종 · 손님 3종**을 볼 수 있다
- [ ] **WebGL 빌드에서 한글이 두부로 나오지 않는다** — 새 문구 전부
- [ ] `./tests/preflight.sh` 전 항목 PASS
- [ ] `Main.unity` 가 Build Settings 에 **등록되어 있고 그것을 테스트가 본다** (D10)

## 이 마일스톤이 M7~M9 에 넘기는 것

- 씬 전환 구조 — M7 의 전체 플로우 검증이 이 구조 위에서 돈다
- `CardView` — M9 밸런싱 중 카드에 수치를 더 얹고 싶어지면 여기 한 곳
- `GameSettings` · `ISettingsStore` — 저장이 필요해지면 확장 지점이 이미 인터페이스다
- **`SushiEatenEventChannelSO` 의 처분** (M5 D5 가 미룬 숙제) — 씬이 둘이 되었으니 이제 판단할 수 있다. **step-09 에서 결론을 낸다**
- 튜토리얼 · 덱 편집 화면 (D9) — 기획이 정해지면
- 스테이지 선택 화면 — 지금은 `게임 시작` 이 곧 스테이지 1 이다

## 이 마일스톤이 손대지 않는 것

- 저장 / 이어하기 (D1) — 런은 씬 안에서만 산다
- 밸런스 수치 (M9)
- 드로우콜 · 빌드 크기 최적화 (M8) — TMP `Resources/` 2.2MB 판단 포함
- 실제 픽셀 아트 원본 — step-11 은 교체 가능한 자리를 만들 뿐이다
