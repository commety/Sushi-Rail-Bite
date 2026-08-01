# Claude Code 세팅

이 프로젝트의 **1차 에이전트 환경**. `.claude/` 지침, 스킬(`/명령`), 서브 에이전트, ClaudeBridge MCP 가 전부 자동으로 연결된다.

---

## 1. 사전 준비 (사람이 하는 것)

| 항목 | 비고 |
|---|---|
| **Claude Code** | 데스크탑 앱(Mac/Windows) 또는 CLI(`claude`). 둘 다 `.mcp.json`·`.claude/`·`CLAUDE.md` 를 공유한다 |
| **Unity Hub + Unity 6000.5.6f1** | 버전은 `ProjectSettings/ProjectVersion.txt` 기준. <https://unity.com/download> |
| **Unity 모듈: WebGL Build Support** | 배포 타깃이 WebGL 이다. Unity Hub → 해당 버전 → Add modules |
| **Git** | — |

> **Claude Desktop(채팅 앱, claude.ai)은 다른 제품이다.** `claude_desktop_config.json` 은 이 프로젝트와 연동되지 않는다. 반드시 **Claude Code** 를 쓴다.

`uv`(Python 패키지 매니저)는 미리 설치할 필요 없다 — 다음 단계에서 에이전트가 알아서 처리한다.

---

## 2. 세팅 — `/setup` 한 줄

```bash
git clone https://github.com/commety/Sushi-Rail-Bite.git
```

Claude Code 에서 이 폴더를 열고:

```
/setup
```

에이전트가 순서대로 처리한다:

1. **OS 감지** (mac/windows/linux)
2. **`uv` 확인** → 없으면 설치까지 직접 실행 (네트워크 다운로드라 승인 1회)
3. **`.mcp.json` 확인** — 저장소에 기본 커밋돼 있음
4. **Unity Editor 설치·버전 확인**
5. **ClaudeBridge MCP 연결 테스트**

사람이 타이핑할 것은 **`/setup` 한 번 + 권한 다이얼로그 "Approve"** 뿐이다. "이 명령을 터미널에 붙여넣으세요" 같은 안내가 나오면 그건 스킬이 제대로 안 돌고 있는 것이다.

이미 세팅돼 있으면 검사만 하고 바로 끝난다. 다른 컴퓨터로 옮겼을 때 다시 돌리면 된다.

### `.mcp.json` 승인

세션 시작 시 MCP 서버 권한 다이얼로그가 뜨면 **Approve**. 승인해야 `claude-bridge` 툴(에디터 조작)이 붙는다.

---

## 3. 자동으로 걸리는 것들

### 세션 시작 훅

`.claude/settings.json` 의 `SessionStart` 훅이 `scripts/ensure-worktree-setup.sh` 를 돌린다. 워크트리에서 열었을 때 공유 폴더 심링크를 자동으로 맞춰 준다. 메인 저장소에서 열면 아무 것도 하지 않고 끝난다.

### 권한 프리어프루브

`.claude/settings.json` 에 무해한 명령(`uv --version`, `git status`, `./scripts/run-editor.sh`, Bridge MCP 툴 등)을 미리 허용해 뒀다. 덕분에 대부분의 작업에서 권한 프롬프트가 뜨지 않는다. 네트워크 다운로드나 파일 변경 같은 진짜 위험한 것만 물어본다.

---

## 4. ClaudeBridge — 에이전트가 Unity 를 직접 조작하는 통로

이 프로젝트의 핵심 장치다. **에이전트가 사용자에게 "Play 눌러 보세요 / 인스펙터 확인해 주세요" 라고 시키지 않는다.** 씬 조립, 컴포넌트 값 확인, Play 모드 진입까지 에이전트가 직접 한다.

```
Claude Code
   ↓  unity_call / unity_batch_flush / unity_bridge_status   (MCP, Python)
.claude-bridge/inbox/*.json
   ↓
ClaudeBridge (Unity C# Editor)  ← Assets/Editor/ClaudeBridge/
   ↓
.claude-bridge/outbox/*.json
```

**두 가지 모드**

| 모드 | 언제 | 방법 |
|---|---|---|
| **GUI 상주** | 화면을 보면서 단계별로 확인할 때 | `/run editor` 로 Unity 실행 → `Window > Claude Bridge > Start` |
| **헤드리스 배치** | 에디터를 안 켜고 한꺼번에 처리 | `/run bridge` |

> ⚠️ **같은 프로젝트로 Unity 가 이미 열려 있으면 헤드리스 배치가 실패한다** (파일 락). 에이전트는 `unity_bridge_status` 로 먼저 확인하게 돼 있지만, 실패하면 에디터를 닫고 재시도한다.

자세한 op 레퍼런스는 [`Assets/Editor/ClaudeBridge/README.md`](../../Assets/Editor/ClaudeBridge/README.md), 운용 지침은 [`.claude/knowledge/unity-editor-automation.md`](../../.claude/knowledge/unity-editor-automation.md).

---

## 5. 빌드·실행

```
/run webgl     # 실제 배포 타깃 — 릴리즈 검증은 반드시 이걸로
/run           # 현재 OS 로 빠른 로컬 확인
/run editor    # Unity Editor GUI 실행
/run bridge    # 큐에 쌓인 Bridge 커맨드 헤드리스 일괄 실행
```

빌드 산출물은 **프로젝트 상위** `builds/{label}-{branch}-{sha}-{target}-{timestamp}/` 에 떨어진다. 저장소 안이 아니라서 커밋에 섞이지 않는다.

> Standalone 빌드가 통과해도 WebGL 이 통과한다는 보장은 없다. WebGL 은 IL2CPP + Managed Code Stripping 경로라 리플렉션·`JsonUtility` 관련 실패가 여기서만 난다. 릴리즈 전 `webgl` 로 한 번 돌린다.

---

## 6. 테스트

TDD 가 기본값이고, **EditMode 전량 통과가 커밋 게이트**다 (`CLAUDE.md` §8).

```bash
Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml -quit
```

- **EditMode** (`Assets/Tests/EditMode/`) — 순수 로직. 테스트의 대부분이 여기 있어야 한다
- **PlayMode** (`Assets/Tests/PlayMode/`) — 씬·코루틴 통합. 느리므로 프레임 정확성이 실제로 필요할 때만

"MonoBehaviour 라서 EditMode 로 못 짜겠다" 면 그건 테스트 문제가 아니라 **로직이 MonoBehaviour 안에 갇혀 있다는 신호**다. 로직을 순수 C# 클래스로 빼는 것이 먼저다.

---

## 7. 문제 해결

| 증상 | 원인 | 대응 |
|---|---|---|
| `No LSP server available for file type: .cs` | OmniSharp 미연결 | `/lsp-setup` — 에이전트가 자동 호출하기도 한다 |
| `/run bridge` 가 "project already open" 으로 실패 | 같은 프로젝트로 Unity 가 떠 있음 | 에디터를 닫고 재시도 |
| Editor 에 `Claude Bridge` 메뉴가 없음 | `Assets/Editor/ClaudeBridge/` 컴파일 안 됨 | Unity 에서 `Assets → Reimport All` |
| `executeMethod ... not found` | 스크립트의 `-executeMethod` 값과 실제 네임스페이스 불일치 | 하네스 타입은 `Editor.*` (게임 코드 `SushiDefense.*` 와 다름). 호출 지점 3곳(`scripts/run.sh`, `scripts/bridge-run.sh`, `scripts/claude-bridge-mcp/.../server.py`)이 항상 같아야 한다 |
| `No enabled scenes` | 빌드 세팅에 씬 미등록 | 씬은 `Assets/Level/Scenes/`. `File → Build Settings` 에서 추가 |
| MCP 툴이 안 보임 | `.mcp.json` 승인 안 됨 / 세션이 오래됨 | 세션을 새로 시작하고 Approve |
| 워크트리에서 만든 에셋이 `git status` 에 안 뜸 | 심링크 폴더에 만들었다 (RULE-02) | `Assets/Art/` 등이 아니라 기능 폴더에 만든다. [`.claude/rules/parallel-work.md`](../../.claude/rules/parallel-work.md) |

---

## 8. 병렬 작업 (워크트리)

여러 에이전트를 동시에 굴릴 때 쓴다.

```bash
./scripts/create-worktrees.sh      # 워크트리 생성
./scripts/run-editor.sh            # 각 워크트리 루트에서 → 새 Unity 인스턴스로 뜬다
./scripts/cleanup-worktrees.sh     # 정리
```

`Assets/Art/`·`Audio/`·`Settings/` 등은 메인을 가리키는 **심링크**라 워크트리에서 새 파일을 만들면 안 된다 (RULE-02). 반면 **`Assets/Level/`(씬·프리팹)은 심링크가 아니라 워크트리마다 독립 사본**이다 — 씬은 사실상 머지가 불가능하므로 **씬 편집은 한 번에 한 워크트리만** 한다.

규칙 전문: [`.claude/rules/parallel-work.md`](../../.claude/rules/parallel-work.md)
