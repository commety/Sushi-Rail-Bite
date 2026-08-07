# Step 11: UI 스프라이트 · 클릭음 · **한글 서브셋 재굽기**

- **영역:** `assets` — 어셈블리 없음 (`Assets/Art/` · `Assets/Audio/` · `Assets/Level/Balance/`)
- **선행 단계:** step-03 ~ step-10 **전부** (문구가 코드에서 추출된다)
- **후행 단계:** step-12 가 씬에 물린다

---

## 목적

M6 이 만든 화면에 **그림과 소리를 채우고, 무엇보다 한글이 두부(□)로 나오지 않게 한다.**

> **이 단계를 미루면 M6 은 WebGL 에서 읽을 수 없는 화면이 된다.** 현재 폰트 애셋 `SushiRailBite-KR.asset` 은 199자(한글 99)만 굽혀 있고, M6 이 더한 문구 — *게임 시작 · 설정 · 백과사전 · 볼륨 · 전체화면 · 일시정지 · 재시작 · 나가기 · 덱 · 건너뛰기* — 는 **하나도 들어 있지 않다.** 에디터에서는 시스템 폰트가 메워 주므로 **빌드해야만 드러난다** ([`unity-scripting-gotchas.md`](../../.claude/knowledge/unity-scripting-gotchas.md) §4-1).

---

## 워크트리 금지

`Assets/Art/` · `Assets/Audio/` 는 워크트리에서 **심링크**다 (RULE-02). 여기서 새 파일을 만들면 파일은 메인 프로젝트에 생기고 **워크트리의 `git status` 가 보지 못한다** ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §2).

**이 단계는 메인 프로젝트에서만 진행한다.**

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 1) 한글 서브셋 재추출 → 재굽기 (가장 중요)

```bash
python3 scripts/extract-charset.py
git diff --stat Assets/Art/Fonts/charset-ko.txt
```

스크립트는 `Assets/Code/Scripts/Presentation/**/*.cs` 의 **문자열 리터럴**(주석 제외)과 밸런스 애셋의 `_displayName`·`_description` 에서 글자를 모은다. M6 의 코드가 전부 들어온 뒤에 돌려야 한 번에 끝난다.

그다음:

- `KoreanFontCoverageTests` 가 **빨개진다.** 이것이 정상이다 — D8(M5)이 설계한 동작이다
- **폰트 굽기는 사람이 한다 (§7).** TMP Font Asset Creator 를 에이전트가 돌리지 않는다. 설정은 M5 와 같아야 한다:

| 항목 | 값 | 이유 |
|---|---|---|
| Render Mode | **`RASTER`** | SDF 는 모서리를 둥글려 픽셀을 죽인다 |
| Sampling Point Size | **12** (폰트 네이티브 높이의 정수배) | |
| Character Set | Custom — `Assets/Art/Fonts/charset-ko.txt` | |
| Atlas | 필요한 최소 (256 → 512 로 올라갈 수 있다) | 늘어난 글자 수만큼 |

- 굽고 나서 **테스트가 다시 초록인지 확인한다.** 초록이 아니면 빠진 글자가 있는 것이고, 그 목록이 `HasCharacters(text, out missing)` 로 나온다

> **아틀라스 크기가 한 단계 올라가는지 기록한다.** 초기 로드 크기에 그대로 얹히며, M8 의 기준선이 된다.

### 2) UI 스프라이트

`Assets/Art/Sprites/UI/` 에 추가한다. 전부 **PPU 32 · Point 필터**가 자동 적용된다 (`PixelArtImportSettings`) — 인스펙터에서 맞추지 않는다.

| 파일 | 크기 | 쓰이는 곳 |
|---|---|---|
| `card-frame.png` | 48×64 | 카드 틀 (step-03) — **등급 무관 하나** (README D3) |
| `card-frame-disabled.png` | 48×64 | 못 놓는 손님 카드 (step-04) |
| `badge-digesting.png` | **16×16** | 소화중 배지 (step-05) — 크기가 요구사항이다 |
| `bar-cell.png` | 8×8 | 포화도 칸 (step-05) |
| `button.png` · `button-pressed.png` | 9-slice | 메뉴·설정·건너뛰기 버튼 |
| `panel.png` | 9-slice | 덱·메뉴·설정·사전 패널 배경 |
| `icon-deck.png` · `icon-menu.png` | 16×16 | 인스테이지 버튼 |

- SVG 를 직접 그려 `Sprite.ImportFromSvg` 로 넣는다 ([`unity-editor-automation.md`](../../.claude/knowledge/unity-editor-automation.md))
- **색은 눈으로 보지 말고 픽셀 값을 대조한다.** Linear 프로젝트에서 sRGB RenderTexture 를 쓰면 결과가 통째로 밝아지는데 *"좀 밝은가?"* 로는 구분되지 않는다 — `SvgOps` 가 이미 `Linear` 로 잡혀 있는지 확인한다
- **아틀라스에 넣는다.** `UI.spriteatlasv2` 가 이미 있다. 새 스프라이트가 그 폴더에 들어가면 자동으로 잡히는지 확인한다 — 안 잡히면 아틀라스 설정은 `.meta` 안이라 **사람이 인스펙터에서** 넣는다 (RULE-03)
- 최종 아트는 사람이 그린 픽셀 아트다. 이것들은 **교체 가능한 자리**이며 원본을 덮지 않는다 (§7·§9)

### 3) UI 클릭음

`/synth` 로 만든다. `Assets/Audio/Sound/sfx-ui-click.wav`.

- 짧게(0.05초 안팎), 다른 효과음보다 **작게**. 메뉴에서 연타된다
- 임포트 설정은 `AudioImportSettings` 가 자동 적용한다 (`Sound/` → `DecompressOnLoad` · Vorbis · Preload)
- `AudioBank.asset` 의 `_uiClick` 에 물린다 (**§7 승인** — step-01 이 제안한 볼륨 0.4 · 간격 0.05 를 함께 확정)

### 4) 배선 확인

step-03~10 이 만든 프리팹의 비어 있는 스프라이트 자리를 채운다. **코드는 고치지 않는다** — 스프라이트가 비어도 동작하도록 이미 만들어져 있고, 그것이 M5 가 세운 규칙이다 (*"아이콘이 비면 프리팹의 그림을 그대로 둔다"*).

### 밸런스 수치

| 값 | 어디에 | 제안 | 승인 |
|---|---|---|---|
| UI 클릭 볼륨 | `AudioBank.asset` | `0.4` | §7 |
| UI 클릭 간격(초) | `AudioBank.asset` | `0.05` | §7 |

### 제약

- **워크트리 금지** (RULE-02) — 메인 프로젝트에서만
- `.meta` 를 직접 편집하지 않는다 (RULE-03). 아틀라스 설정은 인스펙터에서 사람이
- **새 애셋 커밋은 §9 확인 사항** — 원본 유출 방지. 스크린샷을 붙일 때는 저해상도로
- 폰트 라이선스(`OFL.txt`)는 이미 저장소에 있다. 크레딧 화면에 표기하는 것은 M6 의 몫이었으나 화면 3개 제약으로 빠졌다 — **M8 배포 페이지로 넘긴다**
- `Assets/Art/Fonts/` 는 픽셀 임포트 규칙의 **대상 밖**이다. 규칙 경로를 건드리지 않는다

### 완료 판정

- [ ] `python3 scripts/extract-charset.py` 를 돌렸고 `charset-ko.txt` 가 늘었다
- [ ] **폰트를 사람이 다시 구웠고 `KoreanFontCoverageTests` 가 초록이다**
- [ ] `TMP_FontAsset.HasCharacters` 로 M6 신규 문구 전부를 확인했다
- [ ] 새 스프라이트가 전부 PPU 32 · Point · 무압축으로 임포트됐다 (`PixelArtImportPlan` 자동 적용 확인)
- [ ] `badge-digesting.png` 가 **정확히 16×16 px** — 텍스처 크기를 테스트로 고정한다
- [ ] `sfx-ui-click.wav` 가 `DecompressOnLoad` · Vorbis 로 임포트됐다
- [ ] `AudioBank.asset` 변경이 §7 승인 후 적용됐다
- [ ] 아틀라스 크기 변화를 기록했다 (M8 기준선)
- [ ] `./tests/preflight.sh` 전 항목 PASS — **애셋 유출 검사 포함** (§9)

### 예상 커밋 메시지

```
feat(ui): add menu sprites, click sfx and rebaked korean subset
```

---

## 금지 사항

- **워크트리에서 이 단계를 진행하지 않는다**
- `.meta` 파일을 직접 편집하지 않는다 (RULE-03)
- 폰트를 에이전트가 굽지 않는다 (§7)
- 사람이 그린 원본 스프라이트를 덮어쓰지 않는다 (§9)
- 등급별 카드 틀을 만들지 않는다 (README D3)
- 승인 없이 `.asset` 수치를 바꾸지 않는다
