---
name: run
description: Unity 플레이어를 빌드하고 실행하거나(`mac|win|linux|webgl|android|ios`), Editor GUI를 띄우거나(`editor`), 큐에 쌓인 ClaudeBridge 커맨드를 헤드리스로 일괄 실행(`bridge`)합니다. 빌드 산출물은 프로젝트 상위 `builds/{label}-{branch}-{shortsha}[-dirty]-{target}-{timestamp}/`에 저장. 인자 없으면 현재 OS로 빌드.
---

# Skill: /run

인자 모드에 따라 세 가지 흐름:

| 인자 | 동작 | 내부 호출 |
|---|---|---|
| 없음 / `mac` / `win` / `linux` / `webgl` / `android` / `ios` | 플레이어 빌드 + 런치 | `scripts/run.sh {platform}` |
| `editor` | Unity Editor GUI를 현재 프로젝트로 연다. 빌드 안 함 | `scripts/run-editor.sh` |
| `bridge` | `.claude-bridge/inbox/` 의 JSON 커맨드들을 헤드리스 Unity로 일괄 실행 | `scripts/bridge-run.sh` |

각 모드의 실제 작업 파일:
- 빌드: [`scripts/run.sh`](../../../scripts/run.sh) + [`Assets/Editor/RunBuildCommand.cs`](../../../Assets/Editor/RunBuildCommand.cs) — 실제 타입은 `Editor.RunBuildCommand.Build`
- Editor: [`scripts/run-editor.sh`](../../../scripts/run-editor.sh) (`open -a Unity --args -projectPath`)
- Bridge: [`scripts/bridge-run.sh`](../../../scripts/bridge-run.sh) + [`Assets/Editor/ClaudeBridge/ClaudeBridgeBatch.cs`](../../../Assets/Editor/ClaudeBridge/ClaudeBridgeBatch.cs) — 실제 타입은 `Editor.ClaudeBridge.ClaudeBridgeBatch.Run`

> **네임스페이스 주의**: 게임 코드는 `SushiDefense.*` 지만, 이 하네스 타입들은 `Editor` / `Editor.ClaudeBridge` 다 (`Project.` 접두 없음). `-executeMethod` 인자를 손볼 일이 생기면 이 점을 확인한다. 실제 호출 지점은 `scripts/run.sh`, `scripts/bridge-run.sh`, `scripts/claude-bridge-mcp/claude_bridge_mcp/server.py` 세 곳이며 **셋이 항상 같아야 한다.**
> 빌드 파이프라인 변경은 사람의 승인 사항이다 (`CLAUDE.md` §7).

이 스킬 파일은 인자 분기와 출력 해석만 담당한다.

---

## 1. 인자

### 빌드

- **`web`** / `webgl` → `WebGL` — **이 프로젝트의 실제 배포 타깃** (`CLAUDE.md` §1.2). 릴리즈 검증은 반드시 이걸로 한다.
- **없음** → 현재 OS로 기본 (`mac` / `win` / `linux` 자동 감지). 빠른 로컬 확인용.
- **`mac`** / `macos` / `osx` → `StandaloneOSX`
- **`win`** / `windows` → `StandaloneWindows64`
- **`linux`** → `StandaloneLinux64`
- **`android`** / **`ios`** → 지원은 되지만 **이 프로젝트의 타깃이 아니다**. 사용자가 명시적으로 요구하지 않으면 쓰지 않는다.

> Standalone 빌드가 통과해도 WebGL 이 통과한다는 보장이 없다. WebGL 은 IL2CPP + Managed Code Stripping 경로라 리플렉션·`JsonUtility` 관련 실패가 여기서만 나온다 (`.claude/knowledge/unity-scripting-gotchas.md` §3). 릴리즈 전에는 `webgl` 로 한 번 돌린다.

### 편집
- **`editor`** → Unity Editor GUI 실행. 빌드 없음. Unity가 이미 같은 프로젝트로 떠 있으면 그 인스턴스가 전면화됨.

### 브릿지 (헤드리스)
- **`bridge`** → `.claude-bridge/inbox/*.json` 에 드롭된 커맨드를 Unity `-batchmode`로 일괄 처리. 결과는 `.claude-bridge/outbox/*.json`. 사용자가 Editor를 열어 둘 필요 없음.

---

## 2. 실행

프로젝트 루트(메인 또는 워크트리) 에서 인자별로:

```
./scripts/run.sh {platform}      # 빌드
./scripts/run-editor.sh          # Editor GUI
./scripts/bridge-run.sh          # ClaudeBridge 헤드리스 처리
```

## 3. 출력 구조

```
<project-parent>/builds/
└── {label}-{branch}-{shortsha}[-dirty]-{target}-{timestamp}/
    ├── BUILD_INFO.txt       ← 레이블/브랜치/커밋/Unity 버전/호스트 등 핑거프린트
    ├── unity-build.log      ← 배치모드 Unity 로그 전문
    └── <player artifact>    ← .app / .exe / WebGL 디렉토리 / .apk / .xcodeproj 등
```

`label` 의미:
- `main` — 현재 디렉토리가 primary git worktree일 때
- `agent-0` / `agent-1` / ... — 워크트리일 때 해당 디렉토리 이름

---

## 4. 오류 해석

스크립트는 종료 코드로 실패 원인을 구분합니다. 그대로 사용자에게 전달하세요.

### 공통
| 종료 코드 | 의미 | 대응 |
|---|---|---|
| `2` | 프로젝트 루트가 아님 / 인자 오타 | 경로 확인 후 재시도 |
| `3` | Unity Editor 미설치 | 스크립트가 출력한 Unity Hub 설치 안내를 그대로 전달. 버전은 `ProjectSettings/ProjectVersion.txt` 기준 |

### 빌드 (`run.sh`) 추가
| 종료 코드 | 의미 | 대응 |
|---|---|---|
| `4` | **플랫폼 모듈 미설치** | 스크립트가 출력한 Unity Hub GUI 절차 + CLI 명령을 그대로 전달. 임의로 모듈을 자동 설치하지 말 것 (시간 오래 걸림, 대화형 프롬프트 가능성) |
| `5` | 빌드 실패 | `unity-build.log` 끝 40줄을 스크립트가 출력함. 그 중 컴파일 에러·예외 스택트레이스를 요약해 보고 |

### 브릿지 (`bridge-run.sh`) 추가
| 종료 코드 | 의미 | 대응 |
|---|---|---|
| `1` | 일부/전부 커맨드 실패 | `.claude-bridge/outbox/*.json` 의 `ok:false` 항목 찾아서 `error` 필드 요약. 파일별 `id`로 어느 커맨드가 실패했는지 추적 |
| `4` | Unity 프로세스 자체 실패 | `.claude-bridge/logs/bridge-*.log` 말미 확인. 주로 컴파일 에러 또는 `Run` 메서드 실행 중 예외 |

### 자주 나오는 빌드 실패

- **`No enabled scenes`** — 아직 씬이 등록되지 않은 초기 상태에서는 정상. 이 프로젝트의 씬 위치는 `Assets/Level/Scenes/` 다. 사용자에게 `File → Build Settings` 에서 씬을 추가하라고 안내한다.
- **컴파일 에러** — 로그 말미의 `error CS` 라인을 찾아 원인을 짚는다. 현재 세션에서 수정한 파일과 연관 있는지 먼저 확인.
- **`executeMethod ... not found`** — 스크립트의 `-executeMethod` 값과 실제 C# 네임스페이스(`Editor.*`)가 어긋난 것이다. 위 세 호출 지점을 대조하고, 스크립트 수정은 승인 사항이므로 보고 후 고친다.
- **WebGL 빌드만 실패 / 런타임에서 `MissingMethodException`·`TypeLoadException`** — IL2CPP Managed Code Stripping. `link.xml` 또는 `[Preserve]` 가 필요하다. `.claude/knowledge/unity-scripting-gotchas.md` §3 참고.

---

## 5. 금지 사항

- **빌드 스크립트·`RunBuildCommand.cs`·`ClaudeBridgeBatch.cs`를 임의로 수정하지 않는다.** 수정이 필요해 보이면 아키텍트에게 보고 후 승인받는다.
- **플랫폼 모듈을 자동 설치하지 않는다.** 가이드만 제시.
- **다른 워크트리의 빌드 결과를 삭제하지 않는다.** `builds/` 정리는 별도 지시가 있을 때만.
- **`unity-build.log` / `bridge-*.log` 전문을 대화창에 붙여넣지 않는다.** 요약 + 원인 라인 위주로 보고.
- **`bridge` 모드에서 Editor가 이미 열려 있으면 경고한다.** 같은 프로젝트를 동시에 여는 Unity 인스턴스는 파일 락으로 충돌한다. 사용자에게 "Editor 닫고 다시 시도" 안내.
