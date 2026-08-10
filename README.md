# Sushi Rail Bite: 회전초밥 디펜스

> **"초밥을 먹여 목표 매출을 달성하라!"**
>
> **타워 디펜스 × 덱빌딩 × 로그라이트**가 결합된 캐주얼 전략 게임

<p align="center">
  <a href="https://commety.github.io/Sushi-Rail-Bite/"><b>▶ 브라우저에서 바로 플레이</b></a>
</p>

---

## ▶ 플레이

| | |
|---|---|
| **배포 주소** | **https://commety.github.io/Sushi-Rail-Bite/** |
| 플랫폼 | 웹 브라우저 (Unity WebGL) |
| 권장 환경 | 데스크톱 Chrome / Edge / Firefox 최신 버전 |
| 최초 로딩 | 약 43 MB — 첫 실행 시 다운로드에 시간이 걸립니다 |

> 모바일 브라우저는 지원 대상이 아닙니다. 조작이 마우스 클릭 기준입니다.

## 🎬 플레이 영상

<!-- 영상 업로드 후 아래 두 줄의 VIDEO_ID 를 교체하세요.
     썸네일 클릭 시 영상으로 이동합니다. -->

[![Sushi Rail Bite 플레이 영상](https://img.youtube.com/vi/VIDEO_ID/hqdefault.jpg)](https://youtu.be/VIDEO_ID)

*(영상 준비 중입니다.)*

---

## 📌 게임 소개

`Sushi Rail Bite`는 기존 타워 디펜스의 틀을 뒤집은 **회전초밥 디펜스 게임**입니다.

라인을 따라 흘러가는 초밥(적)을 손님(타워)에게 **먹여서** 제한 시간 안에 목표 매출을 달성해야 합니다.
플레이어는 스시 장인과 그의 딸이 되어, 손님 배치와 초밥 덱 구성으로 매장을 굴려 나갑니다.

### 핵심 차별점

* **적을 놓치는 실패(Leak)가 없다**
  초밥이 라인 끝까지 흘러가도 게임 오버가 아닙니다. 중요한 것은 **시간 안에 얼마나 비싼 초밥을 많이 먹였는가**입니다.
* **손님은 굶지 않는다**
  손님의 «타겟팅»은 선호 가격대일 뿐 제약이 아닙니다. 더 맞는 대안이 없으면 아무리 취향에서 먼 초밥도 결국 먹습니다 — 다만 **범위를 벗어나기 직전까지 기다립니다.** 눈앞의 초밥을 두고 구경만 하는 손님은 없습니다.
* **모든 판정이 결정적이다**
  집기 경합에 난수가 없습니다. 같은 상황이면 언제나 같은 결과가 나옵니다.
* **스테이지 클리어 → 카드 보상**
  클리어할 때마다 새 초밥 카드나 손님을 얻어 덱과 로스터를 넓혀 갑니다.

---

## 🎮 핵심 메커니즘

```
[덱 구성] ➔ [벨트 위 초밥 등장] ➔ [손님이 인식·집기] ➔ [먹기 → 포화 → 소화] ➔ [매출 달성]
```

### 손님 (타워)

| 스탯 | 뜻 |
|---|---|
| **집기 범위 (Reach)** | 벨트에서 손을 뻗어 닿는 구간 |
| **타겟팅 (Targeting)** | 선호하는 **가격 대역** `min ~ max`. 공격력에 해당하지만 **먹을 수 있는지를 가르지 않는다** |
| **먹는 속도** | 초밥 하나를 소비하는 데 걸리는 시간 |
| **포화도 (Satiety)** | 배부르기까지 먹을 수 있는 양 |
| **소화 시간** | 포화 후 다시 먹기까지의 휴식 |
| **영입 비용 / 인구** | 배치 가격, 그리고 배치 한도에서 차지하는 몫 |

### 집기 규칙

1. **자격** — 뭐라도 집을 수 있는가는 *범위 ∧ 포화도 여유 ∧ 상태*로만 정해집니다. **가격은 자격 조건이 아닙니다.**
2. **배정** — 후보 (손님, 초밥) 쌍을 하나의 정렬 규칙으로 줄 세웁니다.
   `대역 밖 거리` → `높은 가격` → `초밥 순차번호` → `좁은 대역` → `손님 순차번호`
3. **타이밍** — 대역 **안**이면 즉시 집고, 대역 **밖**뿐이라면 그 초밥이 범위를 벗어나기 직전까지 기다립니다.

> 순차번호가 모든 동률을 끝내므로 배정에 난수가 필요 없습니다.

### 초밥 (적 / 점수)

가격과 포화도 기여량을 가집니다. 매출은 **소비된 초밥 가격의 합**입니다.

---

## 📦 콘텐츠 (v1.0.0)

| | 수 | 내용 |
|---|---|---|
| 스테이지 | 3 | 목표 매출 5,000 → 6,200 → 7,500 / 각 60초 |
| 손님 유형 | 3 | 기본 · 먹보 · 소식가 |
| 초밥 | 8 | 오징어(120) · 계란(130) · 광어/연어(150) · 방어(190) · 참치(250) · 장어(300) · 성게(400) |

**손님 유형의 성격은 대역 폭에서 나옵니다.**

| | 대역 | 먹는 속도 | 포화도 | 영입 비용 |
|---|---|---|---|---|
| 기본 | 100 ~ 300 (넓다) | 1.5s | 5 | 20 |
| 먹보 | 100 ~ 150 (저가 전문) | **1.0s** | **8** | 60 |
| 소식가 | 250 ~ 320 (고가 전문) | 2.0s | 3 | 40 |

---

## ⌨️ 조작

| 입력 | 동작 |
|---|---|
| 마우스 클릭 | 손님 배치, 카드·버튼 선택 |
| `Enter` | 스테이지 전환 화면 넘기기 |
| `1` `2` `3` | 보상 카드 선택 |
| `Esc` | 보상 화면 닫기 / 인게임 메뉴 |

---

## 🎨 아트 & 사운드

* **아트:** 레트로 감성의 손그림 픽셀 아트 (1 유닛 = 32 픽셀)
* **톤:** 듬직한 장인 아버지와 딸의 케미, 위트와 패러디
* **사운드:** 캐주얼한 동양풍 BGM과 음식 효과음

---

## 🛠 기술 스택

| 항목 | 값 |
|---|---|
| 엔진 | Unity **6000.5.6f1** (Unity 6.5) |
| 렌더 파이프라인 | URP — 2D Renderer |
| 언어 | C# |
| 입력 | Input System |
| 배포 타깃 | WebGL (압축 없음 — 정적 호스팅에 그대로 올라갑니다) |

---

## 📁 프로젝트 구조

```
Sushi-Rail-Bite/
├── Assets/
│   ├── Art/                     # 픽셀 스프라이트 · 애니메이션 시트 · 폰트
│   ├── Audio/                   # BGM · 효과음
│   ├── Code/Scripts/
│   │   ├── Runtime/             # 순수 C# 게임 로직 (Unity 의존 최소)
│   │   │   ├── Belt/            #   벨트 · 초밥 흐름
│   │   │   ├── Customers/       #   자격 판정 · 집기 · 포화 · 소화
│   │   │   ├── Run/             #   런 상태 · 덱 · 보상 추첨
│   │   │   ├── Scoring/         #   매출 누적
│   │   │   ├── Stages/          #   시계 · 클리어/실패 판정
│   │   │   ├── Audio/ · Settings/
│   │   │   └── SequenceNumberIssuer.cs   # 결정적 순서 키 발급
│   │   ├── Runtime.Data/        # ScriptableObject 정의 (밸런스 계약)
│   │   ├── Presentation/        # MonoBehaviour · View · Presenter
│   │   └── Editor/              # 에셋 파이프라인 (임포터 · 타일맵 · 애니메이션 빌더)
│   ├── Editor/                  # 빌드 진입점 (RunBuildCommand)
│   ├── Level/
│   │   ├── Balance/             # 밸런스 애셋 (스테이지 · 손님 · 초밥)
│   │   ├── Scenes/              # Main.unity · Stage01.unity
│   │   ├── Prefabs/ · Tiles/ · UI/ · Animations/
│   └── Settings/                # URP 렌더 파이프라인 애셋
├── Packages/
├── ProjectSettings/
└── scripts/
    ├── run.sh                   # 헤드리스 빌드 진입점
    └── lib/                     # Unity 경로 해석 등
```

**어셈블리는 한 방향으로만 의존합니다.**

```
Runtime.Data  ←  Runtime  ←  Presentation
```

게임 규칙(집기 판정·포화도·점수)은 `MonoBehaviour` 밖의 순수 C# 클래스에 있습니다. 밸런스 수치는 하나도 코드에 없고 전부 `ScriptableObject` 필드입니다.

---

## 🔨 빌드

Unity **6000.5.6f1** 과 **WebGL Build Support** 모듈이 필요합니다.

```bash
./scripts/run.sh webgl
```

산출물은 저장소 바깥의 `../builds/<핑거프린트>-WebGL-<타임스탬프>/` 에 나옵니다.
Unity 에디터에서 직접 빌드하려면 `File → Build Settings → WebGL` 을 사용하세요.

---

## 📄 라이선스

### 폰트

| 폰트 | 라이선스 | 전문 위치 |
|---|---|---|
| **x10y12pxDenkiChipHangul** | SIL Open Font License 1.1 | [`Assets/Art/Fonts/OFL.txt`](Assets/Art/Fonts/OFL.txt) |
| **Liberation Sans** (TextMesh Pro 기본) | SIL Open Font License 1.1 | [`Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`](Assets/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt) |

`x10y12pxDenkiChipHangul` 저작권 표시:

```
Copyright (c) 2026 Lee Minseo (quiple@quiple.dev)
Copyright (c) 2026 The x8y12pxDenkiChip Project Authors
(https://github.com/hicchicc/x8y12pxDenkiChip)
```

### 음원

| 대상 | 출처 |
|---|---|
| BGM — `main-bgm.mp3`, `stage-bgm.mp3` | **Suno AI 로 생성 — Made with [Suno](https://suno.com)** |

### 그 외

* **아트(스프라이트·애니메이션)** — 프로젝트 저작물. 무단 재배포를 금합니다.
* **코드** — 별도 라이선스가 명시되기 전까지 모든 권리를 보유합니다.
* `.gitattributes` 는 [gitattributes/gitattributes](https://github.com/gitattributes/gitattributes) (MIT),
  `.gitignore` 는 [github/gitignore](https://github.com/github/gitignore) (CC0-1.0) 템플릿을 따릅니다.

---

## 🤖 AI 에이전트 사용 명시

**이 프로젝트는 [Claude Code](https://claude.com/claude-code) (Anthropic) 를 개발 에이전트로 사용해 만들어졌습니다.**

* 설계·구현·테스트 작성의 상당 부분이 에이전트와의 협업으로 진행되었습니다.
* 에이전트는 **TDD**(실패하는 테스트 → 최소 구현 → 리팩터)를 기본 방식으로 따랐고, 커밋 전 린트·컴파일·테스트·에셋 유출 점검을 자동화된 체크리스트로 통과해야 했습니다.
* 브랜치 병합·태그·설정 변경 등 되돌리기 어려운 작업은 **사람의 승인을 거쳐야만** 수행되도록 제한했습니다.
* 에이전트 운영에 쓰인 규칙 문서·스킬·테스트 하네스는 개발 브랜치(`dev`)에 있으며, 배포 브랜치에는 게임 빌드에 필요한 파일만 남깁니다.
