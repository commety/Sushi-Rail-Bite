# Codex 등 다른 에이전트 세팅

Claude Code 이외의 에이전트(OpenAI Codex, Cursor, Copilot, Gemini CLI 등)로 이 저장소에서 작업할 때의 지침이다.

**핵심 전제 하나만 기억하면 된다.**

> 지침 파일은 저장소에 **평범한 마크다운으로** 들어 있다. Claude Code 는 이걸 자동으로 읽지만, **다른 에이전트는 자동으로 읽지 않는다.** 그래서 사람이 세션 시작 때 "읽어라" 고 한 번 지시해 줘야 한다.

---

## 1. 무엇이 자동이고 무엇이 수동인가

| 항목 | Claude Code | 다른 에이전트 |
|---|---|---|
| 프로젝트 규칙 로드 | `CLAUDE.md` 자동 주입 | **`AGENTS.md` 를 읽으라고 지시** (Codex 는 자동 인식하기도 함) |
| 지침 선별 로드 | `.claude/INDEX.md` 를 스킬이 활용 | **수동** — 인덱스를 읽으라고 지시 |
| 스킬 `/명령` | 슬래시 커맨드로 동작 | **수동** — 해당 `SKILL.md` 를 읽고 절차대로 하라고 지시 |
| 서브 에이전트 | `task-researcher` / `task-engineer` 자동 등록 | **수동** — `agents/*.md` 를 역할 프롬프트로 붙여 사용 |
| 세션 시작 훅 | `settings.json` 이 실행 | 없음 — 워크트리 쓰면 `./scripts/ensure-worktree-setup.sh` 직접 실행 |
| 권한 프리어프루브 | `settings.json` 적용 | 각 도구의 자체 정책을 따름 |
| ClaudeBridge MCP | `.mcp.json` 자동 | **수동 등록** (§3) |

---

## 2. `AGENTS.md` — 다른 에이전트용 진입점

루트의 [`AGENTS.md`](../../AGENTS.md) 는 [`CLAUDE.md`](../../CLAUDE.md) 와 **내용이 완전히 동일한 미러**다. Codex 계열이 `AGENTS.md` 규약을 따르기 때문에 두 벌을 유지한다.

> ⚠️ **한쪽만 고치면 안 된다.** 규칙을 바꿀 때는 두 파일을 항상 함께 갱신한다. 동기 여부는 이 명령으로 확인한다:
>
> ```bash
> diff CLAUDE.md AGENTS.md && echo "동기화 OK"
> ```

---

## 3. ClaudeBridge MCP 를 다른 에이전트에 붙이기

MCP 서버 자체는 도구 중립적인 Python 프로그램이라, MCP 를 지원하는 에이전트면 붙일 수 있다. 이 저장소의 `.mcp.json` 이 쓰는 실행 정의는 다음과 같다:

| 키 | 값 |
|---|---|
| 서버 이름 | `claude-bridge` |
| command | `uv` |
| args | `run`, `--directory`, `scripts/claude-bridge-mcp`, `claude-bridge-mcp` |
| 작업 디렉터리 | 저장소 루트 (경로가 상대 경로다) |

즉 다음과 동등하게 뜬다:

```bash
uv run --directory scripts/claude-bridge-mcp claude-bridge-mcp
```

이 정의를 쓰는 에이전트의 MCP 설정 형식에 맞춰 옮겨 적는다 (Codex CLI 는 `~/.codex/config.toml`, Cursor 는 `.cursor/mcp.json` 등 — **형식은 각 도구의 최신 문서를 확인할 것**. 여기 적힌 값은 서버 이름·실행 커맨드·인자·작업 디렉터리 네 가지다).

노출되는 툴: `unity_bridge_status`, `unity_call`, `unity_batch_flush`.

**MCP 를 못 붙이는 환경이라면** — 파일 기반으로 우회할 수 있다. `.claude-bridge/inbox/<id>.json` 에 커맨드를 직접 쓰고 아래를 실행하면 된다:

```bash
./scripts/bridge-run.sh
```

결과는 `.claude-bridge/outbox/*.json` 에 떨어진다. MCP 없이도 Bridge 전체 기능을 쓸 수 있다.

---

## 4. 세션 시작 지시 템플릿

다른 에이전트로 작업을 시작할 때 **맨 처음 이걸 붙여넣는다.** Claude Code 의 `/task-start` 가 자동으로 하는 일을 수동으로 시키는 것이다.

```
이 저장소는 사람+AI 협업 전제로 지침이 파일로 관리된다. 작업 전에 아래를 순서대로 읽어라.

1. AGENTS.md — 프로젝트 규칙 전문 (용어, 아키텍처, 컨벤션, TDD, Git 전략, 사람 승인 영역)
2. RULES.md — 불변 제약 6개. 항상 스캔
3. .claude/INDEX.md — 지침 라우팅 인덱스. 전부 읽지 말고,
   이번 작업 키워드와 매칭되는 항목만 골라서 해당 파일을 열어라

읽은 뒤 다음을 선언하고 시작해라:
- 매칭해서 연 지침 파일 / 건너뛴 파일
- 이번 작업에 걸리는 RULE-NN
- 건드릴 어셈블리와 파일
- 먼저 실패시킬 EditMode 테스트 (이 프로젝트는 TDD 가 기본값이다)
- CLAUDE.md §7(사람 승인 영역)에 해당하는 항목이 있는지

작업: <여기에 실제 작업 내용>
```

작업이 끝날 때는:

```
작업을 마무리해라. .claude/skills/task-done/SKILL.md 의 절차를 따르되,
특히 STEP 2.5 커밋 전 체크리스트(포맷/컴파일/테스트/에셋 유출 점검)를 반드시 수행하고
통과 여부를 보고해라. 커밋·머지는 네가 실행하지 말고 명령만 제시해라.
```

---

## 5. 스킬을 수동으로 쓰는 법

스킬은 그냥 마크다운 절차서다. 슬래시 커맨드가 없는 환경에서는 **파일을 지목**하면 된다.

```
.claude/skills/qa/SKILL.md 를 읽고 그 절차대로 이번 변경을 QA 해라.
```

```
.claude/skills/design/SKILL.md 를 읽고, 아래 기획을 단계별 작업서로 분해해라.
기획: <내용>
```

스킬 목록과 각각의 용도는 [skills.md](skills.md) 에 있다. 문서에 `/qa` 처럼 적힌 것은 전부 "`.claude/skills/qa/SKILL.md` 절차" 로 바꿔 읽으면 된다.

---

## 6. 서브 에이전트를 수동으로 쓰는 법

`.claude/agents/` 의 두 파일은 역할 프롬프트다. 멀티 에이전트를 지원하지 않는 도구에서는 **한 세션 안에서 두 단계로 나눠** 쓰면 효과가 비슷하다.

1. **리서치 단계** — `.claude/agents/task-researcher.md` 를 읽히고, **코드를 고치지 말고** 구현 설계서(A~H 섹션)만 산출하게 한다.
2. **구현 단계** — 그 설계서를 그대로 붙여넣고 `.claude/agents/task-engineer.md` 를 읽힌 뒤, **설계서 밖의 판단 없이** 실행하게 한다.

이렇게 쪼개는 이유는 "조사하면서 동시에 고치다가 범위가 번지는 것"을 막기 위해서다. 설계서가 불완전하면 구현으로 넘어가지 말고 리서치로 되돌린다.

---

## 7. 어떤 에이전트를 쓰든 지켜야 하는 것

도구가 달라도 아래는 동일하게 적용된다. 위반하면 사람이 뒷수습해야 한다.

- **`RULES.md` 6개** — Domain Reload 유발 금지, 심링크 폴더에 파일 생성 금지, `.meta` 직접 편집 금지, 물리는 `FixedUpdate` 에서만, `async` 에 `CancellationToken`, `ProjectSettings` 수정 금지
- **`CLAUDE.md` §7 사람 승인 영역** — 머지·브랜치 삭제·패키지 추가·`ProjectSettings`·밸런스 SO 일괄 수정·원본 에셋 취급. 에이전트는 **제안하고 멈춘다**
- **TDD** — 새 로직에는 대응 EditMode 테스트가 같은 커밋에 있어야 한다
- **밸런스 수치는 SO 에** — 코드 상수 금지
- **타겟팅은 우선순위용이지 제약이 아니다** — `CLAUDE.md` §1.1-3a
- **원본 에셋(스프라이트·음원)을 외부에 업로드하지 않는다** — `CLAUDE.md` §9
