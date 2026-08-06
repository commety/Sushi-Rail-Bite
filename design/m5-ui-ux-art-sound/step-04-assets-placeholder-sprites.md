# Step 04: 플레이스홀더 스프라이트 13종 + Sprite Atlas

- **영역:** `assets` — 어셈블리 없음 (애셋 제작)
- **선행 단계:** **step-03 필수.** 임포트 규칙이 먼저 있어야 새 텍스처가 자동으로 맞춰진다
- **후행 단계:** step-05 가 이 스프라이트를 화면에 그리고, step-08 이 이펙트에 쓴다

---

## 목적

초밥 9종 · 손님 3종 · UI 아이콘을 **종류가 눈으로 구분되는** 임시 스프라이트로 만든다.

지금은 `SushiBlock.png` 하나가 9종 전부를 그린다. 벨트 위에서 100원짜리 달걀과 400원짜리 성게가 똑같이 보이면, 플레이어는 **대역 규칙을 배울 방법이 없다** — M2.5 를 한 이유가 정확히 그것이었다.

**이것은 임시물이다.** 최종은 일본풍 2D 픽셀 아트이고 사람이 그린다. 이 단계의 목표는 그림이 아니라 **교체 가능한 자리**를 만드는 것 — 원본이 들어오면 같은 경로에 같은 이름으로 덮으면 끝나야 한다 (코드 변경 0).

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 `/make-asset sprite` 로 진행한다.

### 생성 파일

```
Assets/Art/Sprites/Sushi/
  sushi-egg.png         달걀   130
  sushi-squid.png       한치   120
  sushi-flounder.png    광어   150
  sushi-salmon.png      연어   150
  sushi-yellowtail.png  방어   190
  sushi-tuna.png        참치   250
  sushi-eel.png         장어   300
  sushi-seaurchin.png   성게   400
  sushi-placeholder.png (기존 밸런스 애셋용)

Assets/Art/Sprites/Customers/
  customer-standard.png   기본  100~300  영입 20
  customer-bigeater.png   먹보  100~150  영입 60
  customer-smalleater.png 소식  250~320  영입 40

Assets/Art/Sprites/UI/
  icon-coin.png / icon-clock.png / icon-plate.png

Assets/Art/Sprites/Sushi.spriteatlas
Assets/Art/Sprites/UI.spriteatlas
```

> **`Assets/Art/` 는 워크트리에서 심링크다.** 이 단계는 **메인 프로젝트에서만** 실행한다. 워크트리에서 만들면 파일이 메인 프로젝트에 생기고 워크트리 git 이 보지 못한다 ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §2, README «원문 정정»).

### 제작 방법

`/make-asset` §4-4-A 의 SVG 경로 — `Sprite.ImportFromSvg` op. 외부 바이너리 불필요.

```
unity_call("Sprite.ImportFromSvg", {
  svgText: "<svg viewBox=\"0 0 32 32\">...</svg>",
  pngPath: "Assets/Art/Sprites/Sushi/sushi-tuna.png",
  width: 32, height: 32,
  pixelsPerUnit: 32,
  filterMode: "Point",
  compression: "None"
})
```

**`Sprite.ImportFromSvg` 는 그래픽 컨텍스트를 요구한다** (`-nographics` 불가). `./scripts/bridge-run.sh` 기본 설정으로 동작한다. Editor 가 이미 열려 있으면 배치 모드 진입이 실패하므로 `unity_bridge_status()` 로 먼저 확인한다.

### 구분 규칙 — 무엇으로 종류를 알아보게 할 것인가

**가격이 한눈에 읽혀야 한다.** 대역 규칙이 이 게임의 핵심이므로, 임시 스프라이트가 지켜야 할 것은 "예쁨" 이 아니라 **가격 순서의 가독성**이다.

| 축 | 무엇을 태울까 | 왜 |
|---|---|---|
| **색상** | 초밥 종류 (흰살·붉은살·노랑·보라) | 종류 구분. 최종 아트에서도 남는 축이다 |
| **접시 테두리 색** | 가격 티어 (100대 / 150~200대 / 250~300대 / 400) | **회전초밥의 실제 관습**이다. 최종 아트에서도 그대로 통한다 |
| **크기** | 쓰지 않는다 | 벨트 간격이 일정해서 크기 차이는 겹침·정렬 문제를 만든다 |

손님 3종은 **실루엣**으로 구분한다 — 색은 `CustomerView` 가 상태 틴트(`Idle`/`Eating`/`Digesting`/대기)로 이미 쓰고 있다. 색까지 유형에 태우면 두 정보가 같은 채널에서 싸운다.

### Sprite Atlas

원문 계획: *"Sprite Atlas 는 이 게임에서 특히 중요하다 — 벨트 위에 여러 종류의 초밥이 동시에 돈다. 묶지 않으면 종류 수만큼 드로우콜이 늘어난다."*

- `Sushi.spriteatlas` — 초밥 9종 + 손님 3종 (같은 씬에서 동시에 그려진다)
- `UI.spriteatlas` — UI 아이콘

아틀라스에도 **Filter `Point` · Mip 끄기 · 압축 없음**을 맞춘다. 아틀라스 설정은 개별 텍스처 설정과 **따로** 살아 있어서, 여기서 흐릿하게 두면 step-03 의 규칙이 무의미해진다. 흔히 놓치는 지점이다.

### §7 승인 요청 — Sprite Atlas V2

Sprite Atlas 를 쓰려면 `ProjectSettings/EditorSettings.asset` 의 Sprite Packer 모드가 켜져 있어야 한다. **`ProjectSettings/` 수정은 사람 판단 영역이다** (`CLAUDE.md` §7, RULE-06).

현재 값을 먼저 읽어 보고하고, 변경이 필요하면 diff 를 제시한 뒤 승인을 기다린다. 승인 전에는 아틀라스 애셋만 만들어 두고 설정은 건드리지 않는다.

### 선행 산출물 의존성

- step-03 의 `PixelArtImportSettings` — 이 단계에서 만드는 텍스처가 자동으로 규칙을 통과해야 한다. **통과하지 않으면 step-03 이 덜 된 것이다**

### 밸런스 수치

없음. 이 단계는 그림만 만든다. `.asset` 의 `_icon` 을 물리는 것은 **step-05** 다.

### 제약

- **`CLAUDE.md` §9 — 원본 에셋 보호.** 이 단계가 만드는 것은 전부 프로그램 생성물이라 원본 유출 위험이 없다. 다만 **사람이 그린 원본이 이미 같은 경로에 있으면 덮지 않는다.** 덮기 전에 반드시 확인한다
- 최종 아트는 픽셀 아트다. SVG placeholder 는 임시이며, **그 사실을 파일명이나 커밋 메시지가 아니라 `.claude/domain/` 문서에 남긴다** — 파일명에 `placeholder` 를 넣으면 원본 교체 시 경로가 바뀌고 `.asset` 참조가 전부 끊긴다
- `.meta` 를 직접 만들거나 편집하지 않는다 (RULE-03). Unity 가 생성하게 한다
- 32×32 로 통일한다. PPU 32 와 맞물려 1 유닛 = 1 타일이 된다

### 완료 판정

- [ ] 13개 PNG 가 위 경로에 존재하고 `git status` 에 **untracked 로 보인다** (메인 프로젝트에서 실행했음을 증명한다)
- [ ] 각 `.meta` 의 `filterMode: 0`, `enableMipMap: 0` — step-03 규칙이 실제로 걸렸는지 **읽어서** 확인
- [ ] 두 `.spriteatlas` 가 존재하고 대상 폴더를 packable 로 잡고 있다
- [ ] 가격 티어 4단계가 접시 테두리 색으로 구분된다 (눈으로 확인 후 스크린샷 첨부 — **저해상도로** 올린다, §9)
- [ ] `./tests/preflight.sh --fast` PASS (이 단계는 코드 변경이 없으므로 `--fast` 로 충분하다)

### 예상 커밋 메시지

```
feat(art): add placeholder sprites and sprite atlases
```

---

## 금지 사항

- **워크트리에서 실행하지 않는다.** `Assets/Art/` 는 심링크다
- 사람이 그린 원본을 덮어쓰지 않는다 (`CLAUDE.md` §9)
- 승인 없이 `ProjectSettings/` 를 수정하지 않는다
- `.asset` 의 `_icon` 필드를 물리지 않는다 — step-05 다
- 코드 파일을 수정하지 않는다
