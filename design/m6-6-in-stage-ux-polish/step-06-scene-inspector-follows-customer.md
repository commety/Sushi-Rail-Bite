# Step 06: 씬 조립과 확인 (P1-c)

- **영역:** 씬 (`Assets/Level/Scenes/Stage01.unity`) + `Tests.PlayMode`
- **선행 단계:** **step-05 필수** — `AnchorTo` 와 라우터 시그니처가 있어야 한다
- **후행 단계:** 없음 (마일스톤의 끝)

---

## 목적

코드가 다 돼도 **씬이 그 모양이 아니면 화면은 그대로다.** 이 프로젝트에서 가장 비싼 실패
모드가 그것이다 — 코드도 테스트도 멀쩡한데 재생만 죽는다
([`rules/tests.md`](../../.claude/rules/tests.md) §1).

정보 창을 «우하단 고정» 에서 «손님 위» 로 옮기는 마지막 조각을 씬에서 맞추고, **사람이 눈으로
확인**한다.

### 지금 씬의 사실

| 오브젝트 | 부모 | anchoredPos | size | anchor | pivot |
|---|---|---|---|---|---|
| `CustomerInspectorPanel` | `Canvas` | `(-8, 48)` | `200×280` | `(1,0)` | `(1,0)` |
| `OpenButton`(메뉴) | `StageMenu` | `(-8, 8)` | `32×32` | `(1,0)` | `(1,0)` |
| `ToggleButton`(덱) | `DeckPanel` | `(-48, 8)` | `32×32` | `(1,0)` | `(1,0)` |
| 자리 | — | 월드 `x = -4, 0, 4, 6`, `y = -2` | — | — | — |
| 카메라 | — | `(0,0,-10)`, 직교, `size 5` | — | — | — |
| Canvas | — | 기준 해상도 `960×540`, 높이 기준(`match 1`), **Overlay** | — | — | — |

**두 아이콘은 화면 아래 오른쪽 끝**에 있고, 손님은 화면 가운데(캔버스 `y ≈ -108`)에 앉는다.
창이 손님 **위로** 자라므로 아이콘 근처로 내려올 일이 없다 — 다만 **그것을 확인하는 것이
테스트의 몫**이지 가정이 아니다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Level/Scenes/Stage01.unity` — 수정
- `Assets/Tests/PlayMode/Stage01SceneTests.cs` — 수정 (겹침 검사 갱신 + 폴백 검사 추가)

### 1) 씬 조립 — ClaudeBridge

`.claude-bridge/inbox/` 에 커맨드를 떨어뜨리고 `./scripts/bridge-run.sh`.

`Scene.Open` → `Component.SetRectTransform` → `Component.SetField` → `Scene.Save`

**`CustomerInspectorPanel`**:

- `pivot` → `(0.5, 0)`, `anchorMin` = `anchorMax` = **부모(`Canvas`)의 `pivot` 과 같은 값**
- `anchoredPosition` → 편집 중 보기 좋은 자리 아무 곳 (런타임에 덮인다). `(0, 0)` 이면 충분
- **크기 `200×280` 은 그대로 둔다**

> 런타임에는 `CustomerInspectorView.Resolve` 가 같은 값을 다시 못박는다(step-05). 씬에도
> 맞춰 두는 이유는 **에디터에서 보이는 모양과 재생 중 모양이 달라지지 않게** 하기 위해서다.

**`_worldCamera` 는 씬에서 물리지 않는다.** `Component.SetField` 로 씬 안의 오브젝트를 넘기려면
`instanceId` 가 필요한데 그 값은 배치 도중에 알 수 없다
([`knowledge/unity-editor-automation.md`](../../.claude/knowledge/unity-editor-automation.md)).
`Camera.main` 폴백으로 간다 — `CustomerTapRouter.ResolveCamera` 가 이미 쓰는 방식이다.

**그 폴백이 곧 사각지대다.** 테스트가 카메라를 물려 주면 «비었을 때 찾는» 분기가 죽는다 —
M5 에서 `HudLabel.Resolve` 와 `EffectDirector._pool` 이 그렇게 빠져나갔다. **그래서 아래 4번의
폴백 테스트가 선택이 아니다.**

### 2) 겹침 검사를 «움직인 뒤» 로 바꾼다

`Play_Scene_InspectorPanel_DoesNotCoverTheIcons` 는 지금 **씬에 적힌 자리**를 잰다. 창이
런타임에 움직이게 됐으므로 그 검사는 **아무것도 지키지 않는다.**

바꿀 모양:

```
자리마다: 손님을 앉히고 → 그 자리를 눌러 창을 열고 → 한 프레임 뒤
          패널이 OpenButton · ToggleButton 과 겹치지 않는다
```

- **자리 넷을 전부 돈다.** 하나만 보면 «오른쪽 끝 자리에서만 겹친다» 를 놓친다 — 클램프가
  실제로 위험한 곳이 거기다
- `Overlaps`·`WorldRect` 헬퍼는 이미 있다. 그대로 쓴다
- **패널이 부모 사각형 안에 있는지도 같은 테스트에서 본다.** «아이콘을 안 가린다» 는
  화면 밖으로 날아가도 통과한다

### 3) 손님 위에 뜨는지 본다

겹침 검사만으로는 **창이 여전히 우하단에 있어도 통과**할 수 있다(아이콘 위가 아니기만 하면).
그러니 «옮겨졌다» 를 직접 본다:

- 서로 다른 두 자리(예: `x=-4` 와 `x=4`)에 각각 열면 패널 위치가 **서로 다르다**
- 패널의 화면 좌표가 그 손님의 화면 좌표 **근처**다 — 정확한 픽셀이 아니라 **«같은 쪽»**
  (부호·대소)로 단언한다. 픽셀을 박으면 `_gap` 을 만질 때마다 깨진다

### 4) 폴백 테스트 (빠뜨리지 않는다)

```
카메라를 물려 주지 않은 채로 창을 열어도 좌표가 잡힌다
```

씬을 그대로 재생하는 경로가 이미 그 상태이므로, **씬 테스트 안에서 `_worldCamera` 를 손대지
않는 것**이 곧 이 검사다. 다만 «손대지 않았다» 는 것이 코드에 드러나야 하므로, 테스트 doc 에
*"여기서 카메라를 주입하면 폴백이 안 밟힌다"* 를 남긴다.

### 5) 빌드로 확인 — 이 단계의 진짜 완료 조건

```bash
./scripts/run.sh webgl
```

**WebGL 이 이 프로젝트의 배포 타깃**이고, `§4`(에디터에서만 멀쩡한 것들)은 전부 빌드해야
드러난다. 확인할 것:

- 손님을 눌렀을 때 창이 **그 손님 위에** 뜬다
- **오른쪽 끝 자리**(`x=6`)에서도 창이 화면 안에 있다
- 다른 곳을 누르면 닫힌다 (M6.5 의 계약이 안 깨졌다)
- 메뉴·덱 아이콘이 **여전히 눌린다**
- step-01~03 의 결과도 같은 빌드에서 본다 — 카드 배경의 얼룩, 글자 여백, 손님 위 표시물

> 워크트리라면 **`Assets/Art/` 의 변경을 커밋한 뒤에 빌드한다** — `run.sh` 가 빌드 후 심링크
> 폴더의 미커밋 변경을 되돌린다 (RULE-02).

### 밸런스 수치

없다. `_gap`·`_edgeMargin` 은 step-05 가 만든 연출 수치이며, **이 단계에서 사람이 화면을 보고
확정한다.**

### 제약

- **RULE-03 — `.meta` 를 편집하지 않는다**
- **씬은 한 번에 한 워크트리만 연다** ([`rules/parallel-work.md`](../../.claude/rules/parallel-work.md) §3).
  이 단계가 도는 동안 다른 단계가 `Stage01.unity` 를 열지 않는다
- **`.cs` 로직을 바꾸지 않는다.** 씬을 맞추다 코드가 필요해지면 그것은 step-05 가 덜 끝난 것이다 —
  멈추고 보고한다
- ClaudeBridge 는 **`SetActive` op 이 없고 부모 변경 op 도 없다.** 그 둘이 필요해지면 조립
  방식을 다시 짠다
- 브릿지를 돌리기 전 **Unity Editor 를 닫는다.** 배치모드는 프로젝트 락을 단독으로 잡는다

### 완료 판정

- [ ] `./scripts/bridge-run.sh` 결과가 전부 `ok:true`
- [ ] `git diff --stat` 에 `Stage01.unity` 와 테스트 파일만
- [ ] `./tests/run-tests.sh all` — EditMode · PlayMode 전량 통과
- [ ] `./tests/preflight.sh` 전부 `[ok]`
- [ ] **공허 확인**: 패널의 `pivot` 을 `(1,0)` 으로 되돌리거나 `AnchorTo` 호출을 빼고 돌린다.
      2·3번 검사가 실제로 죽는지 보고 되돌린다. **실측으로** 보고한다
- [ ] `./scripts/run.sh webgl` 성공 + **빌드 크기를 M6.5(42.99MB)와 비교**해 보고
- [ ] **사람이 브라우저에서 확인** — 위 5번 목록

### 예상 커밋 메시지

```
feat(stage): float the customer inspector over its customer
```

씬과 테스트를 **같은 커밋**에 둔다. 나누면 중간 커밋에서 씬 테스트가 빨갛다.

---

## 금지 사항

- `Assets/Level/Scenes/Main.unity` 를 열지 않는다 — 이번 마일스톤과 무관하다
- `ProjectSettings/*` · `Packages/manifest.json` 을 건드리지 않는다 (RULE-06 · §7)
- 빌드가 실패했을 때 로그 전문을 붙여넣지 않는다. 원인 라인만 요약한다
- 사람의 확인 없이 «완료» 로 보고하지 않는다 — 이 단계는 헤드리스로 판정되지 않는다
