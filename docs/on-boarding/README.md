# 온보딩 — Sushi Rail Bite

새로 합류한 사람이 **이 문서만 위에서 아래로 따라가면** 개발 환경이 서고, AI 에이전트에게 제대로 일을 시킬 수 있게 되는 것이 목표다.

이 프로젝트는 **사람 + AI 에이전트가 같이 작업하는 것을 전제로** 구성돼 있다. 저장소에는 코드뿐 아니라 **에이전트가 읽는 지침 계층**(`.claude/`)이 함께 들어 있고, 이게 프로젝트의 일부다. 지침을 무시하고 에이전트를 굴리면 결과물이 컨벤션을 벗어나므로, 아래 흐름을 한 번은 읽고 시작한다.

---

## 0. 5분 요약 (급하면 여기만)

| 무엇 | 어디 |
|---|---|
| 게임이 뭔지 | [`README.md`](../../README.md) (루트) |
| 프로젝트 규칙 전문 | [`CLAUDE.md`](../../CLAUDE.md) — Codex 등 다른 에이전트는 [`AGENTS.md`](../../AGENTS.md) (내용 동일) |
| 어기면 시스템이 깨지는 규칙 6개 | [`RULES.md`](../../RULES.md) |
| 에이전트 지침 라우팅 인덱스 | [`.claude/INDEX.md`](../../.claude/INDEX.md) |

```bash
git clone https://github.com/commety/Sushi-Rail-Bite.git
```

클론 후 Claude Code 에서 프로젝트 폴더를 열고 아래 한 줄이면 환경 세팅은 끝난다.

```
/setup
```

세부는 [claude-code.md](claude-code.md). Codex·Cursor 등 다른 에이전트를 쓴다면 [other-agents.md](other-agents.md).

---

## 1. 프로젝트 기본 정보

| 항목 | 값 |
|---|---|
| 장르 | 타워 디펜스 × 덱빌딩 × 로그라이트 (회전초밥) |
| 엔진 | Unity **6000.5.6f1** (Unity 6.5) |
| 렌더 파이프라인 | URP (2D Renderer) |
| 배포 타깃 | **WebGL** (웹 데모) |
| 언어 | C# |
| 개발 방식 | **TDD 기본값** — 실패 테스트 → 최소 구현 → 리팩터 |
| 브랜치 전략 | Git Flow + 3-way handshake (`feat/*` → PR → 사람 승인 후 머지) |

Unity 버전은 `ProjectSettings/ProjectVersion.txt` 가 정답이다. Unity Hub 에서 **정확히 이 버전**을 설치한다. 버전이 다르면 프로젝트를 여는 순간 에셋이 업그레이드되어 되돌리기 어렵다.

---

## 2. 저장소 지도

```
Sushi-Rail-Bite/
├── CLAUDE.md / AGENTS.md    # 프로젝트 규칙 전문 (두 파일 내용 동일 — 항상 같이 고친다)
├── RULES.md                 # 불변 제약 6개. 어기면 에디터가 멈추거나 GUID 가 깨진다
├── .claude/                 # 에이전트 지침 계층 (아래 §3)
├── .mcp.json                # ClaudeBridge MCP 서버 등록
├── docs/                    # 이 문서 포함, 사람이 읽는 문서
├── scripts/                 # 빌드·에디터 실행·워크트리·Bridge 하네스
├── tests/                   # CI 파이프라인·린터 설정 (게임 테스트 아님)
└── Assets/
    ├── Code/Scripts/        # 게임 코드 — Runtime / Runtime.Data / Presentation / Editor
    ├── Tests/               # Unity Test Framework (EditMode / PlayMode)
    ├── Editor/              # 에이전트 하네스 (ClaudeBridge) — 게임 코드 아님
    ├── Art/ Audio/ Settings/ Level/
    └── ...
```

> **지금 상태**: `Assets/Code/Scripts/` 는 비어 있고 `.asmdef` 도 아직 없다. 위 구조는 **앞으로 만들 목표 구조**이며, 최초 어셈블리 생성은 아키텍트 승인 사항이다 (`CLAUDE.md` §7).

### 헷갈리기 쉬운 두 쌍

- **`RULES.md`(루트)** vs **`.claude/knowledge/RULES.md`** — 전자는 이 프로젝트의 불변 제약 6개. 후자는 Chris Zimmerman 의 범용 코딩 규약 21개. 완전히 다른 문서다.
- **`Assets/Editor/`** vs **`Assets/Code/Scripts/Editor/`** — 전자는 에이전트 하네스(ClaudeBridge, 네임스페이스 `Editor.*`). 후자는 게임용 에디터 툴링(`SushiDefense.*`). 게임 코드가 전자에 의존하면 안 된다.

---

## 3. `.claude/` 는 무엇인가

에이전트가 읽는 지침이 **계층별로** 들어 있다. 통째로 읽으면 토큰 낭비라, [`INDEX.md`](../../.claude/INDEX.md) 가 라우팅 인덱스 역할을 한다 — 에이전트는 인덱스를 먼저 읽고 작업과 관련된 파일만 펼친다.

| 폴더 | 내용 |
|---|---|
| `INDEX.md` | 선별 로드용 라우팅 인덱스. **에이전트가 제일 먼저 읽는 파일** |
| `knowledge/` | 프로젝트 무관 범용 지식 — C#/.NET, Unity 성능(WebGL), 스크립팅 함정, QA 10원칙, 디버깅 10원칙 |
| `rules/` | 경로별 규칙 — `scripts.md`(.cs), `scriptable-object.md`(SO/밸런스), `tests.md`(TDD), `asmdef.md`, `parallel-work.md`(워크트리) |
| `domain/` | 이 프로젝트만의 기획·시스템 지식. `/task-done` 이 쌓아 간다 (현재는 템플릿만) |
| `skills/` | 에이전트 작업 공정 11개 → [skills.md](skills.md) |
| `agents/` | 서브 에이전트 2개 — `task-researcher`(설계) / `task-engineer`(구현) |
| `settings.json` | 권한 허용 목록 + 세션 시작 훅 |
| `tests/` | **다른 프로젝트에서 스킬을 검증한 아카이브.** 이 저장소의 사실이 아니다 (파일 상단에 경고 배너 있음) |

---

## 4. 반드시 알고 시작할 규칙 3가지

전문은 `CLAUDE.md` 에 있지만, 이 셋은 신규 인원이 가장 자주 어긴다.

### (1) 밸런스 수치를 코드에 쓰지 않는다

초밥 가격, 집기 범위, 먹는 속도, 포화도, 소화 시간, 목표 매출 — 전부 `ScriptableObject` 필드다. 기획을 바꿀 때 코드를 건드리지 않아야 한다. (`CLAUDE.md` §3.1)

### (2) 타겟팅 대역은 "먹을 수 있는 조건"이 아니다

가장 자주 잘못 구현되는 지점이다. **판정을 세 갈래로 분리**해서 생각한다 (`CLAUDE.md` §1.1-3a).

| 갈래 | 질문 | 판정 기준 | 대역 |
|---|---|---|---|
| **자격** | 뭐라도 집을 수 있나 | 범위 ∧ 포화도 여유 ∧ 상태 | **안 씀** |
| **배정** | 누가 무엇을 가져가나 | 대역 밖 거리 → 고가 → 초밥 SeqNo → 대역 폭 → 손님 SeqNo | 씀 |
| **타이밍** | 지금 확정하나 기다리나 | 대역 안이면 즉시 / 아니면 최선 후보의 이탈 직전 | 씀 |

- **자격에 가격이 들어가면 위반이다.** 더 맞는 대안이 없으면 손님은 **대역에서 아무리 먼 초밥도 먹는다**.
- **기다림과 굶기는 다르다.** 대역 밖만 있어 유예 중인 것은 정상이고, 유한 시간 안에 반드시 집는다.
- 손님이 눈앞의 초밥을 **영영** 집지 않는 상황은 **버그**다.

```csharp
// ❌ 이런 코드는 기획 위반 — 대역 밖이라고 자격을 거부
if (TargetingPriority.BandDistance(sushi.Price, min, max) > 0)
    return false;
```

타겟팅은 **가격 대역**(`min`~`max`)이다. 손님 유형의 차이는 대역의 **위치와 폭**에서 나온다 — 좁은 고가대 대역은 맞는 초밥이 드물어 자주 기다리고, 넓은 저가대 대역은 거의 즉시 먹는다.

> 왜 단일 값에서 대역으로 바꿨고 당시 반대 논증이 어떻게 해소됐는지는 [`.claude/domain/sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §6 에 있다.

배정에 **난수는 없다** — 순차번호가 동률을 끝낸다 (§1.1-3b). 탐색은 **이벤트 기반**이고 인식은 **래치**된다 (§1.1-3c).

상세 플로우는 [`.claude/domain/sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md).

### (3) 사람이 판단할 영역은 에이전트가 실행하지 않는다

`git merge` / `push --force` / PR 머지 / 브랜치 삭제 / `main`·`release/*` 직접 커밋 / 새 어셈블리·패키지 추가 / `ProjectSettings` 수정 / 원본 에셋 삭제·업로드 / 밸런스 SO 일괄 수정. (`CLAUDE.md` §7)

에이전트는 **제안하고 멈춘다.** 에이전트가 이걸 그냥 실행하려 하면 지침이 안 먹고 있다는 신호다.

---

## 5. 다음 읽을 것

| 문서 | 언제 |
|---|---|
| [claude-code.md](claude-code.md) | Claude Code 로 작업한다 — 설치·세팅·문제 해결 |
| [other-agents.md](other-agents.md) | Codex·Cursor 등 다른 에이전트로 작업한다 |
| [skills.md](skills.md) | **스킬 카탈로그 + 에이전트에게 지시하는 법.** 세팅 끝났으면 여기가 본론 |
