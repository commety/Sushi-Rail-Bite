# Step 13: `Stage01.unity` 조립

- **영역:** `scene` — 씬 애셋 + 프리팹
- **선행 단계:** step-01 ~ step-12 **전부**
- **후행 단계:** 없음 (M6 의 마지막)

> **step-12 에서 갈라져 나온 작업이다** (아키텍트 결정). step-12 는 `Main.unity` 까지만
> 하고 끊었다 — 인스테이지 조립이 메인 씬보다 크고, 기존 씬 개조라 성격이 다르다.
> **아래 실측은 step-12 진행 중에 확인한 것이므로 다시 조사하지 않아도 된다.**

---

## 목적

step-03 ~ step-08 이 만든 **인스테이지 화면을 씬에 놓는다.** 지금은 코드로만 존재하고
씬에는 한 번도 놓인 적이 없다.

---

## 실측 — 조사 결과 (step-12 에서 확인)

### 1. `StageBootstrap` 의 null 참조는 **버그가 아니다**

씬의 `StageBootstrap` 은 직렬화 참조가 **9개 전부 `{fileID: 0}`** 다:

```
_viewPool · _beltView · _beltStart · _beltEnd · _placementController
_slots: [] · _hud · _rewardView · _transitionView · _audioDirector · _effectDirector · _hand
```

이력을 보면 커밋마다 늘어 왔다 (`1d8a25c` 4/9 → `e681062` 9/9). 처음엔 M3 식 참조
유실로 보였으나, `StageBootstrap.ResolveMissingReferences()` 가 **그 필드 전부를**
런타임에 `GetComponentInChildren<T>(true)` 로 찾는다.

> **따라서 이 씬의 계약은 «인스펙터 배선» 이 아니라 «오브젝트가 존재하기만 하면 된다» 이다.**
> 인스펙터를 채우려 애쓸 필요가 없고, 대신 **오브젝트가 `Stage` 하위에 있어야 한다.**
> 이 사실을 모르면 «참조가 다 비었다」를 고치려다 시간을 버린다.

### 2. 씬에 **버튼이 한 개도 없다**

씬의 스크립트 컴포넌트를 전부 뽑은 결과:

| 있음 | 없음 |
|---|---|
| `SushiBeltView` · `SushiPoolBehaviour` · `TableSlotView` · `CustomerHandView` · `CustomerPlacementController` · `StageHudView` · `RewardSelectionView` · `StageTransitionView` · `AudioDirector` · `EffectDirector` | **`EventSystem`** · **`SceneRouter`** · **`DeckPanelView`** · **`StageMenuView`** · `CardView` · `CustomerCardDrag` · `SlotPicker` · `UiClickSound` · `Button`(0개) · `GraphicRaycaster` |

`Stage01SceneTests` 가 초록인 것은 그것이 **스테이지 로직만** 보기 때문이다
([`tests.md`](../../.claude/rules/tests.md) §1 «코드로 세운 하네스는 씬 사고를 못 잡는다»).

### 3. 프리팹은 앞서 끝나 있다

| 프리팹 | 상태 |
|---|---|
| `Customer.prefab` | **`SaturationBar`(Cell0~Cell7) + `DigestingBadge` 이미 있음** — step-12 §2 의 «포화도 칸·배지 개조» 는 프리팹 레벨에서 완료 |
| `Card.prefab` | `CardView`+`Button`+`Image`+라벨 완비. **스프라이트만 비어 있다** |
| `CustomerCard.prefab` | `CustomerCardDrag` 만 있는 **빈 상태** — 내용을 채워야 한다 |

### 4. `PlacementInput` 은 이미 0건이다

`grep -c "PlacementInput" Assets/Level/Scenes/Stage01.unity` = **0**,
`m_Script: {fileID: 0}`(Missing Script) 도 **0**. step-12 의 해당 항목은 확인만 하면 된다.

---

## 할 일

1. **`EventSystem`**(+`InputSystemUIInputModule`) · `Canvas` 에 `GraphicRaycaster` · **`SceneRouter`**
   — 라우터는 `Stage` 하위에 둔다 (§1 의 해석기가 찾는 범위)
2. **`CustomerCard.prefab` 내용 채우기** → `CustomerHandView._cards` (명부 상한만큼)
3. **`DeckPanelView`** — 패널 + `CardView` 자리 + 덱 버튼(`icon-deck`)
4. **`StageMenuView`** — 패널 + 버튼 6개(열기·닫기·멈춤·재개·재시작·나가기, `icon-menu`)
5. **`RewardSelectionView` 개조** — `CardView` 3장 + 건너뛰기 버튼
6. **`TableSlotView._seatVisual`** → `Customer.prefab` 인스턴스 4개
7. **스프라이트 배선** — `Card.prefab` 에 `card-frame`, 버튼에 `button`/`button-pressed`(**`Type = Sliced`**), 패널에 `panel`
8. **`UiClickSound`** 를 Canvas 에 (버튼 전용 — 카드는 제외된다)
9. **`Stage01SceneTests` 확장** — 아래

### 조립 방법

**일회용 에디터 스크립트로 세우고 그 스크립트는 지운다.** `Main.unity` 를 그렇게 했다
(커밋 `2a0bf57`). 브리지 op 은 `GameObject.Create` 가 `RectTransform` 을 안 붙여
오브젝트당 4~5 콜이 들고, 여기는 Main 보다 크다.

두 가지 함정을 미리 안다:

- **`Assets/Editor/`(harness)는 게임 타입을 못 본다.** 스크립트는
  `Assets/Code/Scripts/Editor/` 에 둔다
- **거기도 `Unity.TextMeshPro` 를 참조하지 않는다.** TMP 는 컴파일 타임 타입 없이
  `Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro")` + `SerializedObject` 로 다룬다.
  직렬화 이름은 실측했다: `m_text` · `m_fontAsset` · `m_fontSize` · `m_fontColor` ·
  `m_HorizontalAlignment`(1=Left, 2=Center) · `m_VerticalAlignment`(512=Middle)
- asmdef 를 고쳐 우회하지 않는다 — §7 승인 사항이다

### 테스트 계획

```
Stage01_HasEventSystem                   ← 지금 0건인 것
Stage01_UsesInputSystemUIModule          ← 타입 이름으로 본다 (asmdef 참조 회피)
Stage01_HasHandDeckAndMenuViews
Stage01_HasNoPlacementInput              ← 이미 0건. 회귀 방지로 고정
Stage01_CustomerViewHasSaturationCells
Stage01_EveryLabelHasAFont               ← MainSceneTests 와 같은 이유
Stage01_ClickSoundHooksButtonsOnly       ← 카드가 딸려 들어가면 실패
```

`MainSceneTests` 를 그대로 본떠 쓸 수 있다.

### 완료 판정

- [ ] 두 씬 모두 `EventSystem` + `InputSystemUIInputModule` 을 갖는다
- [ ] `Stage01SceneTests` 확장분이 초록이다
- [ ] 재생 확인 — step-12 §6 의 10항목
- [ ] WebGL 확인 — step-12 §7 의 6항목 (**두부 0 포함**)
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(scene): wire the in-stage ui into stage01
```

---

## 금지 사항

- **워크트리에서 씬을 편집하지 않는다** (한 번에 한 워크트리)
- 씬 조립 스크립트를 저장소에 남기지 않는다 (R8)
- asmdef 를 임시로 고치지 않는다 (§7)
- 씬에서 확인한 문제를 씬에서 임시 봉합하지 않는다 — 원인을 고친다
