# Step 02: 카드 설명의 여백과 행간 (P3)

- **영역:** `presentation` — 애셋(`Card.prefab`) + `Tests.EditMode`
- **선행 단계:** **step-01 필수** — 배경의 띠가 걷힌 뒤에 글자를 맞춘다. 얼룩진 배경 위에서
  여백을 맞추면 반드시 다시 맞추게 된다
- **후행 단계:** 없음

---

## 목적

> 카드의 정보 영역의 글 상하 간격이 좁아서 가독성이 저하된다.
> 글 시작 위치를 옮겨야 한다(패딩). 현재는 카드 변(side)부터 글이 시작해 답답하다.

실측으로 원인이 확인됐다. `Card.prefab` 의 `DetailLabel` 은

- `m_margin: {x: 0, y: 0, z: 0, w: 0}` — **TMP 여백이 통째로 0**
- `m_lineSpacing: 0` — 행간이 기본값
- `m_SizeDelta: {x: -16, y: 88}` — 좌우 8px 은 **RectTransform 이 낸 것**이라 글자가 사각형의
  변에 그대로 붙는다

즉 «패딩이 좁다» 가 아니라 **패딩이 없다.**

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/UI/Card.prefab` — 수정 (`DetailLabel`, 필요하면 `NameLabel`)
- `Assets/Tests/EditMode/UI/CardPrefabTests.cs` — 수정 (**계약만** 추가, 아래 참고)

### 어떻게 고치나 — ClaudeBridge

프리팹은 손으로 YAML 을 고치지 않는다. `.claude-bridge/inbox/` 에 커맨드를 떨어뜨리고
`./scripts/bridge-run.sh` 를 돌린다.

- `Prefab.Open` → `Component.SetField` ×N → `Prefab.Save` → `Prefab.Close`
- `m_margin` 은 `Vector4` 다. `ValueCodec` 이 `Vector4` 를 지원하므로
  `valueType: "UnityEngine.Vector4"` + `valueJson: {"x":10,"y":4,"z":10,"w":4}` 로 넘어간다
- `m_lineSpacing` 은 `float`
- **`Component.SetRectTransform` 을 쓰지 않는다.** `sizeDelta` 를 줄여 여백을 흉내 내면 글자가
  들어갈 폭 자체가 줄어 줄바꿈 위치가 바뀐다. 여백은 TMP 의 몫이다

> 배치 안에서 알 수 없는 `instanceId` 가 필요한 op(`Reflection.Invoke`)는 쓰지 않는다 —
> 이 단계는 경로와 타입만으로 끝난다.

### 값 (출발점 — 사람이 확정한다)

| 필드 | 지금 | 제안 |
|---|---|---|
| `DetailLabel.m_margin` | `(0,0,0,0)` | `(10, 4, 10, 4)` |
| `DetailLabel.m_lineSpacing` | `0` | `12` |

- **이 값은 결정이 아니라 출발점이다.** 넣고 끝내지 말고, 사람이 화면을 보고 조정할 수 있게
  «어디를 어떻게 만지면 되는지»를 보고에 적는다
- `NameLabel`(size `(-8, 14)`)도 같은 증상이면 함께 손본다. **다만 한 번에 하나씩** 바꾸고
  화면으로 확인한다 — 둘을 같이 바꾸면 어느 쪽이 답답했는지 알 수 없다
- **행간을 키우면 5줄짜리 손님 카드가 넘칠 수 있다.** `DetailLabel` 높이는 88 이고 손님은
  5행이다. 넘치면 행간을 줄이는 쪽이 먼저이고, 그래도 모자라면 **보고하고 멈춘다** —
  카드 크기를 바꾸는 것은 네 화면에 걸친 결정이다 (M6 D3)

### 테스트 — 값을 박지 않는다

`CardPrefabTests` 는 스스로 이렇게 적어 두었다:

> **크기·색·간격은 단언하지 않는다.** 연출 수치라 사람이 만질 값이고, 박으면 레이아웃을
> 손볼 때마다 테스트가 깨진다.

**그 방침을 지킨다.** 대신 **유무만** 본다:

```csharp
[Test] public void Prefab_DetailLabel_HasHorizontalPadding()   // margin.x > 0 && margin.z > 0
[Test] public void Prefab_DetailLabel_HasLineSpacing()          // lineSpacing > 0
```

- 이것은 «값» 이 아니라 **«글자가 변에 붙지 않는다» 는 계약**이라 눈으로 튜닝해도 안 깨진다
- 클래스 doc 의 «단언하지 않는다» 문단 옆에 **왜 이 둘만 예외인지** 한 줄 남긴다. 안 적으면
  다음 사람이 방침이 뒤집힌 줄 알고 크기까지 박는다

### 밸런스 수치

없다. 전부 연출 수치이며 프리팹에 산다 (M5 D7).

### 제약

- **RULE-03 — `.meta` 를 편집하지 않는다**
- 이 단계는 **`.cs` 로직을 바꾸지 않는다.** `CardCaption` 의 문구·행 구성은 그대로다 —
  줄 수를 줄여 «답답함» 을 푸는 것은 다른 결정이다
- **`Main.unity` · `Stage01.unity` 를 열지 않는다.** 씬의 카드 22장은 프리팹 인스턴스라
  프리팹만 고치면 따라온다. 씬을 여는 순간 step-06 과 충돌한다
  ([`rules/parallel-work.md`](../../.claude/rules/parallel-work.md) §3)

### 완료 판정

- [ ] `./scripts/bridge-run.sh` 결과 `.claude-bridge/outbox/*.json` 이 전부 `ok:true`
- [ ] `git diff --stat` 에 `Card.prefab` 과 테스트 파일만
- [ ] `./tests/preflight.sh` 전부 `[ok]`
- [ ] `Play_Scene_CardsMatchThePrefabSize`(양쪽 씬)가 **여전히 통과** — 프리팹 크기를 안 건드렸다
- [ ] **화면으로 확인** — 이 단계는 헤드리스로 판정되지 않는다. `/run editor` 로 카드를 띄우거나
      `/run webgl` 로 빌드해 **사람이 본다**

### 예상 커밋 메시지

```
fix(ui): give the card detail text room to breathe
```

---

## 금지 사항

- **연출 수치를 테스트에 박지 않는다.** 유무만 본다
- 카드 크기(128×192)·자식 구성·폰트를 바꾸지 않는다
- 씬 파일을 열지 않는다
