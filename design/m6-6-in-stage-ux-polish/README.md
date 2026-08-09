# M6.6 — 인스테이지 UX 다듬기

## 한 줄 요약

손님 정보 창을 **누른 손님 옆으로** 옮기고, **카드가 읽히도록** 배경의 띠와 글자 여백을
손보고, **손님 머리 위를 정리**한다 — 넷 다 «틀린 것» 이 아니라 «덜 좋은 것» 이다.

## 원문

[`docs/plan/M6.6-in-stage-ux-polish.md`](../../docs/plan/M6.6-in-stage-ux-polish.md) —
M6.5 완료 후 WebGL 실플레이 리포트(2026-08-09)에서 **버그를 뺀 나머지**.

---

## 착수 전 실측

계획서가 «있을 것» 이라고 적은 것들을 먼저 확인했다. 아래는 **지금 저장소의 사실**이다.

| 항목 | 실측 | 근거 |
|---|---|---|
| P2(항목 이름 붙이기) | **이미 들어가 있다** | `CustomerInspectorPresenter.Tick` 이 `상태 …` · `포화도 3/5` · `소화 2초 남음` 을 만든다 |
| 정보 창 위치 | Canvas 직하 `CustomerInspectorPanel`, 200×280, anchor·pivot `(1,0)`, anchoredPos `(-8, 48)` | `Assets/Level/Scenes/Stage01.unity` |
| Canvas | `renderMode 0` = **ScreenSpaceOverlay** | 같은 씬. 좌표 변환에 UI 카메라가 `null` 이라는 뜻이다 |
| `Card.prefab` | 128×192. `DetailLabel` anchoredPos `(0,-96)` size `(-16, 88)`, **`m_margin` 전부 0**, **`m_lineSpacing` 0**, fontSize 12 | `Assets/Code/Scripts/Presentation/UI/Card.prefab` |
| 카드 틀 띠 | 캔버스 좌표 `y 50~60`. 9-slice 테두리 8 이므로 **늘어나는 구간은 `y 8~55`** → **겹치는 6줄(50~55)이 176px 로 늘어난다** | `scripts/make-ui-sprites.py` `card_frame` |
| `card-frame` 9-slice | 임포터가 아니라 **스프라이트 시트 항목**에 있다. `UiSpriteAssetTests` 는 `Sprite.border` 를 읽어 8 을 확인한다 | 생성기의 `SLICE_BORDERS` 에는 `card-frame` 이 **없다**(출력용 메모일 뿐이라 무해) |
| 포화도 칸 | `SaturationBar` y `-0.62`, 칸 8개 x `-0.49…0.49`(간격 0.14), **scale 0.12** | `Customer.prefab` |
| 손님 몸통 | 32×32 @PPU 32 → **1 유닛**, 피벗 중앙이므로 바닥이 `y -0.5` | 같은 프리팹 |
| 백과사전 | 카탈로그 초밥 8 + 손님 3 = **11**, 씬 자리 **정확히 11개**(6×2 격자, 128×192) | `CardCatalog.asset` · `Main.unity` |

**포화도 칸은 크기도 자리도 틀렸다.** `bar-cell` 은 8×8@PPU32 = 0.25유닛인데 scale 0.12 를
곱해 **0.03유닛(≈1픽셀)** 로 그려지고, 간격은 0.14 이므로 **틈이 칸보다 네 배 넓다.** 그리고
바가 앉은 `y -0.62` 는 **테이블 스프라이트(자리 기준 `-1.85 … +0.15`) 한복판**이다 —
«너무 작고 위치가 애매하다» 의 정체가 이 둘이다. 씬 좌표를 더 재면:

| 물체 | 자리 기준 y | 월드 y |
|---|---|---|
| 테이블 (`table.png` 64×64, 중앙 피벗) | `-1.85` … **`+0.15`** | `-3.85` … `-1.85` |
| 손님 몸통 | `-0.5` … `+0.5` | `-2.5` … `-1.5` |
| 소화 배지 | `+0.6` … `+1.1` | `-1.4` … `-0.9` |
| **벨트 레일** (16.5×1.4 @ y 0) | — | **`-0.7` … `+0.7`** |

자리는 월드 `x = -4, 0, 4, 6` — **자리 2·3 은 간격이 2 유닛뿐이라 테이블 둘이 정확히
맞닿는다.** 이 두 줄이 배치 선택지를 거의 다 지운다 (D2-1).

---

## 아키텍처 결정

- **D1. P4(백과사전 탭)는 이번 마일스톤에서 뺀다.** — 아키텍트 판단. 틀을 둘로 나누면
  M6 D3(«카드 틀은 하나»)이 깨지고 «카드 모양을 고칠 때 두 곳» 이 된다. P3·P6 으로 카드가
  읽히게 되면 «빈 공간» 의 체감이 달라질 수 있으므로 **그 뒤에 다시 본다.** 계획서의 P4 항목은
  그대로 남겨 둔다.
- **D2. P5 는 «머리 위 라벨을 통째로 지운다».** — 아키텍트 판단(2026-08-09 추가 지시).
  필드에서 손님 위에 남는 것은 **포화도 · 소화 중 · 먹는 중** 셋뿐이다. 이름(=유형)도 상시
  표시하지 않는다.
  - `customer-kinds.md` 가 M2.5 의 존재 이유로 적은 *"어느 손님이 무엇을 노리는지가 자리 옆에
    있어야 한다"* 는 **이 결정으로 무효가 된다** — 그 역할을 정보 창(M6.5)과 유형별 스프라이트
    (M5)가 나눠 진다. **이 문서 갱신을 step-03 의 완료 조건에 포함**한다. 문서를 그대로 두면
    다음 세션이 «규칙을 어긴 상태» 로 읽는다.
  - 대신 **«한 번에 한 손님만 볼 수 있다»** 는 차이가 남는다. 자리 셋을 나란히 비교할
    방법이 사라지므로, 실플레이에서 그게 문제로 드러나면 되돌릴 곳은 여기다.
- **D2-1. 포화도는 손님 오른쪽 세로바로 옮기고, 칸마다 배경판을 깐다.** — 아키텍트 승인.
  - **테이블이 자리 기준 좌우 ±1 유닛을 덮는다.** 손님 몸통은 ±0.5 뿐이라 좌우 어느 쪽에 두어도
    테이블 위이고, 벗어나려면 |x| > 1 인데 자리 2·3 은 간격이 2 유닛뿐이라 이웃 테이블에 닿는다 —
    **위치로는 못 푼다.** 배경이 무엇이든 읽히게 만드는 쪽이 자리마다 다른 사정을 쫓아다니지 않는다
  - **머리 위로 쌓는 안은 예산이 없다.** 머리 끝(월드 −1.5)에서 **벨트 레일 바닥(−0.7)** 까지
    0.8 유닛인데 배지가 이미 0.5 를 쓴다. 배지를 올려 그 아래 포화도를 넣으면 레일까지
    **0.05 유닛(≈1.6px)** 만 남고 그 위로 초밥이 지나간다
  - 세로바는 배지와 **다른 축**을 쓴다 — «소화중은 그때만 나온다» 에 기대는 대신 **애초에 서로를
    신경 쓸 필요가 없어진다**
- **D2-2. 배경판은 칸 하나하나의 «자식» 이다.** 통짜 판 한 장이 아니다.
  `SaturationBarView.Bind` 가 `VisibleCells(max, capacity)` 로 **손님마다 칸 수를 다르게** 켠다
  (소식가 3칸 · 먹보 8칸). 통짜 판은 소식가에게 빈 판이 길게 남고, 그것은 이 파일이 이미 경고한
  *"회색 칸이 «못 채운 칸» 으로 읽힌다"* 와 같은 오독이다. 자식으로 두면 칸이 꺼질 때 함께 꺼져
  **길이가 저절로 맞고 `SaturationBarView` 는 한 줄도 안 바뀐다.**
- **D2-3. 배경판 스프라이트는 9-slice 로 만들지 않는다.** 테두리를 주려면 `.meta` 의 스프라이트
  시트 항목을 손봐야 하는데 **에이전트는 `.meta` 를 편집할 수 없다**(RULE-03) — M6.5 에서 카드
  틀의 테두리를 사람이 Sprite Editor 로 넣어야 했던 것과 같은 벽이다. 원본 비율 그대로 균일
  배율로 쓰면 그 벽이 없다.
- **D3. «먹는 중» 은 지금 있는 몸통 색이 그대로 진다.** 새 표시물을 만들지 않는다 —
  `CustomerView.Apply` 가 `Eating` 에 노란빛을, `Digesting` 에 회색빛을, 대기에 파란빛을
  이미 칠한다. 지시의 «먹는 중 인터페이스» 를 **새 배지를 만들라는 요구로 읽지 않았다.**
  전용 표시물이 필요하다면 그것은 별도 항목이다.
- **D4. 정보 창은 «열 때 위치를 잡고 고정» 이다.** 따라다니지 않는다. 벨트는 움직여도 손님은
  자리에 앉아 있어 따라갈 대상이 안 움직이고, 매 프레임 `WorldToScreenPoint` 는 그대로
  `Update` 경로 비용이다(§4.3). 계획서도 *"고정으로 충분해 보인다"* 로 적었다.
- **D5. 화면 밖으로 나가면 «뒤집지 않고 밀어 넣는다».** 뒤집기는 «어느 쪽으로» 라는 두 번째
  규칙이 필요하고, 그 규칙이 자리마다 다르게 보인다. 밀어 넣기는 규칙이 하나(캔버스 안)라
  자리 수가 늘어도 그대로 성립한다.
- **D6. 좌표 계산을 순수 정적으로 뺀다** (`PanelAnchorMath`). 클램프는 «화면 밖» 이 곧
  실패 조건인데, `RectTransform` 을 세우는 PlayMode 로만 검증하면 경계값을 넣기 번거롭다.
  변환(월드→화면→캔버스 로컬)은 Unity 가, **클램프는 우리가** 하고 그것만 EditMode 로 본다.
- **D7. P3(여백·행간)에는 수치를 박는 테스트를 넣지 않는다.** `CardPrefabTests` 가 스스로
  *"크기·색·간격은 단언하지 않는다 — 연출 수치라 사람이 만질 값"* 이라고 적어 두었다. 대신
  **«여백이 0 이 아니다» 라는 계약**만 본다. 값이 아니라 유무를 보는 것이라 눈으로 튜닝해도
  안 깨진다.
- **D8. P6 의 «단색» 은 테스트로 박아도 된다.** 요구 자체가 «가운데를 단색으로» 라서,
  픽셀을 세는 검사가 이번만은 브리틀하지 않다 — 그림을 손보는 자유도가 그 영역에는 애초에
  없다. 다만 **늘어나는 영역만** 본다. 모서리 못은 테두리(0~7) 안이라 대상이 아니다.
- **D9. `.png` 는 손으로 칠하지 않는다.** 생성기를 고치고 다시 돌린다 — 손으로 칠하면
  `make-ui-sprites.py` 를 한 번만 돌려도 띠가 돌아온다.

---

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| — | (어셈블리 아님) | `scripts/make-ui-sprites.py` — 카드 틀 생성기 |
| 애셋 | — | `Assets/Art/Sprites/UI/card-frame*.png` 재생성, `Card.prefab`, `Customer.prefab` |
| presentation | `Presentation` | `CustomerView`, `CustomerInspectorView`, `CustomerTapRouter`, `StageBootstrap`, 신규 `PanelAnchorMath` |
| tests | `Tests.EditMode` / `Tests.PlayMode` | 스프라이트 픽셀·프리팹 계약·앵커 계산·씬 배선 |
| 씬 | — | `Assets/Level/Scenes/Stage01.unity` (정보 창 앵커·피벗) |

**`Runtime` · `Runtime.Data` 를 건드리지 않는다.** 이번 마일스톤은 전부 표현 계층이다 —
새 SO 필드도, 밸런스 수치도 없다.

## 의존성 그래프

```
scripts/make-ui-sprites.py ─┬─► card-frame*.png ──► Card.prefab 글자 배치 (배경이 정해진 뒤 여백을 잰다)
                            └─► bar-slot.png ─────► Customer.prefab 칸의 배경판

SushiDefense.UI.PanelAnchorMath          (순수 계산)
        │
        ▼
SushiDefense.UI.CustomerInspectorView.AnchorTo(worldPoint)
        ▲
        │ 월드 좌표를 넘긴다
SushiDefense.Customers.CustomerTapRouter ──► StageBootstrap.BuildInspector
        │
        ▼
Stage01.unity 의 CustomerInspectorPanel (앵커·피벗 재설정)

SushiDefense.Customers.CustomerView(라벨 제거) ──► Customer.prefab(BandLabel 삭제 · 세로바 · 배경판)
```

## 새 밸런스 수치

**없다.** 이번에 바뀌는 값은 전부 **연출 수치**이며 SO 가 아니라 프리팹·씬·생성기에 산다
(M5 D7). 다음 값들이 그에 해당하고, **전부 사람이 눈으로 정한다**:

| 값 | 사는 곳 | 지금 | 제안(출발점) |
|---|---|---|---|
| 카드 설명 좌우 여백 | `Card.prefab` `DetailLabel.m_margin` | `(0,0,0,0)` | `(10, 4, 10, 4)` |
| 카드 설명 행간 | 같은 라벨 `m_lineSpacing` | `0` | `12` |
| 포화도 칸 크기 | `Customer.prefab` 칸 `localScale` | `0.12` | `0.46` (칸끼리 거의 맞닿는다) |
| 포화도 바 위치 | `Customer.prefab` `SaturationBar` | `(0, -0.62)` | **`(0.65, 0)`** — 축을 맞바꾼 꼴 |
| 배경판 크기 | `bar-slot.png` (신규) | — | `12×12` (칸 8×8 보다 사방 2px) |
| 정보 창 손님과의 간격 | `CustomerInspectorView` 직렬화 필드 | — | `40`(캔버스 단위 — 1 월드 유닛 ≈ 54 이므로 머리 위로 나오려면 27 초과) |
| 정보 창 화면 가장자리 여백 | 같은 곳 | — | `8` |

> 제안값은 **출발점이지 결정이 아니다.** 각 단계는 값을 넣고 멈추는 것이 아니라
> **사람이 화면을 보고 조정할 수 있는 상태**로 끝내야 한다.

## 사람의 승인이 필요한 것 (`CLAUDE.md` §7 · §9)

| # | 무엇 | 어느 단계 |
|---|---|---|
| A1 | **원본 애셋 커밋** — `card-frame.png` · `card-frame-disabled.png` 재생성본 + `bar-slot.png` 신규 (§9) | step-01 |
| A2 | 연출 수치 확정 — 여백·행간·칸 크기·창 간격 (위 표) | step-01·02·03·06 |
| A3 | `.meta` 는 손대지 않는다 (RULE-03). PNG 를 같은 48×64 로 다시 찍으므로 9-slice 는 그대로 남아야 하며, **남았는지 테스트로 확인**한다 | step-01 |

> 워크트리에서 돌린다면 **RULE-02** 가 추가로 걸린다. `Assets/Art/` 는 심링크라 워크트리
> git 이 새 파일을 못 보고, `scripts/run.sh` 는 빌드 뒤 그 폴더의 미커밋 변경을 되돌린다 —
> **PNG 를 커밋한 뒤에 빌드한다.** (현재 주 저장소에서는 실제 폴더라 해당 없음.)

---

## 단계

| # | 파일 | 영역 | 다루는 것 |
|---|---|---|---|
| 01 | [step-01-editor-card-frame-center.md](step-01-editor-card-frame-center.md) | editor/애셋 | **P6** 카드 틀 띠 제거 + **`bar-slot` 신규** |
| 02 | [step-02-presentation-card-text-layout.md](step-02-presentation-card-text-layout.md) | presentation | **P3** 카드 설명의 여백·행간 |
| 03 | [step-03-presentation-customer-overlays.md](step-03-presentation-customer-overlays.md) | presentation | **P5** 머리 위 라벨 제거 + 포화도 세로바·배경판 |
| 04 | [step-04-presentation-panel-anchor-math.md](step-04-presentation-panel-anchor-math.md) | presentation | **P1-a** 클램프 계산(순수) |
| 05 | [step-05-presentation-inspector-anchoring.md](step-05-presentation-inspector-anchoring.md) | presentation | **P1-b** 뷰·라우터·부트스트랩 배선 |
| 06 | [step-06-scene-inspector-follows-customer.md](step-06-scene-inspector-follows-customer.md) | 씬 | **P1-c** `Stage01.unity` 조립 + 씬 테스트 + WebGL 확인 |

## 병렬 실행 가능성

```
           ┌─► step-02        (배경이 정해진 뒤에 글자 여백을 잰다)
step-01 ───┤
           └─► step-03        (bar-slot 스프라이트를 물어야 한다)

step-04 ──► step-05 ──► step-06
```

- **step-01 과 step-04 는 서로 독립이다.** 건드리는 파일이 겹치지 않는다.
- **step-02 는 step-01 뒤에 둔다.** 얼룩진 배경 위에서 여백을 맞추면 다시 맞추게 된다.
- **step-03 도 step-01 뒤로 옮겼다.** 배경판 스프라이트가 거기서 나온다 — 애셋을 두 단계가
  나눠 만들면 §9 승인을 두 번 받게 되고 `make-ui-sprites.py` 를 둘이 동시에 고친다.
- step-02 와 step-03 은 **서로 병렬**이다 (`Card.prefab` vs `Customer.prefab`).
- **step-05 · step-06 은 직렬이다.** 05 가 정하는 시그니처를 06 이 씬에서 쓴다.
- **씬을 건드리는 단계는 step-06 하나뿐이다.** `.unity` 는 텍스트여도 사실상 merge 불가라
  병렬로 두지 않는다([`rules/parallel-work.md`](../../.claude/rules/parallel-work.md) §3).

## 이 계획이 정하지 않는 것

- **P4(백과사전 탭·초밥 설명문)** — D1 로 뺐다. 되살릴 때는 계획서의 P4 절이 그대로 입력이다.
- 연출 수치의 **최종값** — 위 표는 출발점이고 사람이 화면에서 정한다.
- 카드 틀의 **색·무늬** — 이번에 지우는 것은 «늘어나서 얼룩이 되는 띠» 하나이지 디자인 개편이
  아니다.
