# Step 09: `Stage01` 조립과 WebGL 확인

- **영역:** `scene` — 씬 애셋 + `Tests.PlayMode`
- **선행 단계:** step-03 · 04 · 06 · 08 **전부**
- **후행 단계:** 없음 (M6.5 의 마지막)

---

## 목적

앞 단계들이 만든 것을 **화면에 실제로 놓는다.** 이 프로젝트에서 가장 비싼 실패 모드는 «코드도 테스트도 멀쩡한데 재생만 죽는» 형태이고, 그 원인은 거의 항상 씬이다 ([`tests.md`](../../.claude/rules/tests.md) §1).

---

## ⚠ 씬 편집은 직렬이다

`Assets/Level/Scenes/Stage01.unity` 는 **한 번에 한 워크트리만** 고친다 ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §3). 씬은 텍스트여도 사실상 merge 불가다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한다.

### 생성/수정 파일

- `Assets/Level/Scenes/Stage01.unity` — 수정 (§7-5 승인)
- `Assets/Tests/PlayMode/Stage01SceneTests.cs` — 수정
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 필요하면 `ResolveMissingReferences` 보강

### 씬에 놓을 것

| 무엇 | 어디에 | 이름 규약 |
|---|---|---|
| 정보 창 패널 | 인스테이지 Canvas 아래, **꺼진 채로** | `CustomerInspectorPanel` + 라벨 6개 |
| 손패 컨테이너 | 기존 손패 아래 | `Tray` — `CustomerHandView._tray` 가 잡는다 |
| 배지 라벨 | `Customer.prefab` 안 (프리팹이라 씬 작업 아님) | `RemainingLabel` |
| 클릭 라우터 | 배치 컨트롤러 옆 | `CustomerTapRouter` 컴포넌트 |

- **패널은 꺼진 채로 둔다.** 그래서 `Awake` 가 안 돌고, 그래서 step-06 의 멱등 `Resolve()` 가 필요하다 — 둘은 한 쌍이다
- 라벨 이름은 뷰의 상수와 맺은 약속이다. **하나만 틀려도 그 칸이 조용히 비고 예외는 나지 않는다**

### 씬 테스트 (`Stage01SceneTests`)

**코드로 세운 하네스는 씬 사고를 못 잡는다.** 이 프로젝트에서 씬 애셋 자체를 보는 것은 이 파일뿐이다.

```
Scene_HasCustomerInspectorPanel_AndItStartsHidden
Scene_InspectorPanel_HasEveryLabel            ← 라벨 6개를 이름으로
Scene_HasCustomerTapRouter
Scene_TapRouter_HasEverySlotWired
Scene_Hand_HasTrayAssigned                    ← _tray 가 비면 손패가 안 접힌다
Scene_TappingASeatedCustomer_OpensTheInspector ← 배치 → 탭 → 열림까지 씬에서 한 번
```

- 마지막 하나는 **뷰 계층을 지나는** 통합 확인이다. `Placement.Place(...)` 를 직접 부르는 기존 하네스는 자리·좌석이 화면에서 사라져도 초록이므로, 여기서는 **실제 자리 오브젝트를 통해** 확인한다

### WebGL 확인 (사람이 본다)

```bash
./scripts/run.sh webgl
```

브라우저에서 확인할 것:

- [ ] 손님을 클릭하면 정보 창이 뜨고, 포화도·상태가 **실시간으로** 움직인다
- [ ] 정보 창이 떠 있어도 **판이 계속 돈다** (벨트가 움직인다)
- [ ] 메뉴를 연 상태에서 손님을 클릭하면 **정보 창이 안 뜬다**
- [ ] 소화 중 배지에 숫자가 보이고 줄어든다
- [ ] 먹보를 앉히면 `손님` 표시가 **2 늘어난다**
- [ ] 손패가 접혀 있다가 마우스를 올리면 올라온다. **올라온 상태에서 카드를 끌어 앉혀도** 카드가 제자리로 돌아온다
- [ ] 한글 두부 **0**, 콘솔 예외 **0**
- [ ] 카드 프레임이 뭉개지지 않는다 (§7-3 9-slice 가 들어간 뒤)

> **«스크린샷이 나온다» 를 «게임이 돈다» 로 읽지 않는다.** 브라우저 패널이 숨겨지면 `requestAnimationFrame` 이 사실상 멈춘다 — 스크린샷은 강제 페인트라 그림은 나오지만 게임 시간은 흐르지 않고, **스테이지 시계가 60 에서 안 움직이는 것**으로 드러난다 ([`presentation-and-audio.md`](../../.claude/domain/presentation-and-audio.md) §9). 드래그·호버는 여러 프레임에 걸쳐야 성립해서 자동화로는 끝까지 가지 않는다 — **사람이 본다.**

### 빌드 크기 기록

`.claude/domain/presentation-and-audio.md` §8 의 표에 M6.5 열을 더한다. M6 은 43.00 MB 였다.

### 제약

- **Build Settings 를 건드리지 않는다.** 씬이 늘지 않는다
- `scripts/run.sh` 는 빌드 후 **공유 애셋의 미커밋 변경을 되돌린다.** `EditorBuildSettings.asset` 같은 것을 고쳤다면 **먼저 커밋한다**
- 심링크 폴더(`Assets/Art/`·`Audio/`·`Settings/`)에 새 파일을 만들지 않는다 (RULE-02 / `parallel-work.md` §2)
- `.meta` 를 편집하지 않는다 (RULE-03)

### 완료 판정

- [ ] `Stage01SceneTests` 6건 추가, 전량 Green
- [ ] `./tests/preflight.sh` 전 항목 `[ok]`
- [ ] WebGL 체크리스트 8항목을 **사람이** 확인
- [ ] 빌드 크기 기록
- [ ] 깨뜨려 보기: 씬에서 `CustomerInspectorPanel` 을 비활성이 아니라 **삭제**하면 어느 테스트가 죽는지 확인하고 되돌린다. **예측하지 말고 실제로 돌려 본다** — M4 에서 세 번 빗나갔다 (`tests.md` §3)

### 예상 커밋 메시지

```
feat(scene): wire the inspector and the folding hand into stage01
```

---

## 금지 사항

- 씬을 두 워크트리에서 동시에 열지 않는다.
- `Assets/Art/` 에 새 파일을 만들지 않는다.
- WebGL 확인을 «스크린샷이 나왔으니 됐다» 로 대신하지 않는다.
- 로그 전문을 대화창에 붙여넣지 않는다 — 요약 + 원인 라인만.
