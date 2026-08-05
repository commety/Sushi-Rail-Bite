# tests/ — 피드백 루프

에이전트와 사람이 **자기 작업의 성패를 사람에게 묻지 않고 확인**하기 위한 실행 계층이다.

`.claude/skills/` 는 *무엇이 좋은 코드인가*를 알려주는 지시문이고, 여기는 *지금 이 코드가 좋은가*를 판정하는 실행 파일이다. 둘은 대체 관계가 아니다.

> 게임플레이 단위 테스트는 여기가 아니라 `Assets/Tests/{EditMode,PlayMode}` 에 있다 (Unity Test Framework 가 `Assets/` 밖을 탐색하지 못한다). 이 디렉토리는 **그 스위트를 돌리고 결과를 요약하는 도구**다.

## 명령

```bash
./tests/preflight.sh          # 커밋 전 체크리스트 전체 (CLAUDE.md §8)
```

```bash
./tests/preflight.sh --fast   # Unity 를 띄우지 않는 검사만 (수 초)
```

```bash
./tests/run-tests.sh          # EditMode 스위트
```

```bash
./tests/run-tests.sh all      # EditMode + PlayMode
```

```bash
./tests/lint.sh --fix         # 포맷 위반 자동 수정
```

## 구성

| 파일 | 역할 |
|---|---|
| `preflight.sh` | §8 체크리스트 집계. 한 항목이 실패해도 끝까지 돌고 표로 보고 |
| `run-tests.sh` | Unity Test Framework 헤드리스 실행 |
| `parse-results.py` | NUnit3 XML → 요약. 컴파일 실패와 테스트 실패를 구분 |
| `lint.sh` | `.editorconfig` 기준 포맷 검사 (`dotnet format` 래퍼) |
| `../.editorconfig` | 포맷·네이밍 규칙. **루트 고정** — `dotnet format` 이 루트에서만 찾는다 |
| `../scripts/lib/unity-path.sh` | Unity 실행 파일 경로 해석. `run.sh` 와 공유 |
| `../scripts/lib/shared-assets.sh` | RULE-02 공유 폴더 오염 감지·원복. `run.sh` 와 공유 |

`results/` 는 실행 산출물(NUnit XML·Unity 로그)이며 `.gitignore` 대상이다. **점으로 시작하는 이름을 쓰지 않는다** — Unity 가 `-testResults`/`-logFile` 경로에서 숨김 디렉토리를 거부한다(`.results is not a valid directory name`).

## 종료 코드

`preflight.sh` 는 0(통과) / 1(실패)만 쓴다. 개별 도구는 더 나눈다:

| 코드 | 의미 |
|---|---|
| 0 | 통과 |
| 1 | 테스트 실패 · 포맷 위반 |
| 2 | **컴파일 실패** — 테스트가 아예 돌지 않음 |
| 3 | 결과 불명 (XML 없음/파손, 또는 **테스트 0개**) |
| 4 | Unity 에디터가 프로젝트를 점유 중 |
| 6 / 7 | `.sln` 없음 / `dotnet` SDK 없음 |

**테스트 0개를 통과로 보지 않는다.** Unity 는 테스트가 0개일 때 `result="Passed"` 로 보고한다 — 실제 출력으로 확인했다. TDD 가 기본값인 프로젝트에서(`CLAUDE.md` §5) 스위트가 빈 채 Green 이 나오면 게이트가 조용히 무력화된다.

단 `preflight.sh` 는 둘을 구분한다:

| 상황 | 판정 |
|---|---|
| `Assets/Tests/**/*.cs` 가 하나도 없다 | `skip` — 스위트가 아직 없다 (M0 이전) |
| 테스트 파일은 있는데 0개가 발견됐다 | `FAIL` — `.asmdef` 오설정이다 |

## 자주 걸리는 것

**`exit 4` — 에디터 점유.** 배치모드는 프로젝트 락을 단독으로 잡는다. ClaudeBridge 용 에디터를 띄워둔 채로는 못 돌린다. 에디터를 닫거나, 에디터 안의 Test Runner 창을 쓴다.

**`exit 6` — `.sln` 없음.** `.sln`/`.csproj` 는 Unity 생성물이고 `.gitignore` 대상이다. 클론 직후에는 존재하지 않는다. `./scripts/run-editor.sh` 로 에디터를 한 번 열면 생성된다.

## 범위 밖

**CI/CD 파이프라인은 만들지 않는다.** 프로토타이핑 단계이고, Unity 를 CI 러너에서 돌리려면 라이선스 활성화가 필요해 1인 개발에서 비용이 얻는 것보다 크다. 배포를 실제로 다루는 M8 에서 재검토한다 → [`docs/plan/M5-M9-later.md`](../docs/plan/M5-M9-later.md)

그때까지는 **이 로컬 루프가 유일한 자동 신호**다.

## 검사 대상은 좁게 시작한다

설계가 흔들리는 단계에서 테스트를 두껍게 깔면 기획이 바뀔 때마다 같이 깨지고, 결국 사람이 테스트를 지운다 — [`.claude/rules/tests.md`](../.claude/rules/tests.md) §7 이 금지한 바로 그 행동이다.

| | 방침 |
|---|---|
| **잠근다** | 집기 배정(`SushiClaimResolver`) — 스펙 확정, 난수 없음, 순수 C#. 회귀 방어선을 촘촘히 |
| **연다** | 밸런스 수치·UI·스테이지 플로우 — 계속 바뀐다. 두껍게 깔면 부채 |

`preflight.sh` 의 **배정 난수 금지**·**대역 산술 격리**·**자격 경로 청정** 검사가 그 "잠그는" 쪽이다. 셋 다 이 프로젝트에서 반복적으로 잘못 구현되는 지점이라(`CLAUDE.md` §1.1-3a) 정적 검사로 고정해 뒀다. `Assets/Code/Scripts/Runtime/` 이 생기면 자동으로 활성화된다.

**가드를 넣었으면 죽지 않았는지 확인한다.** 통과만 보고 넘어가면 패턴이 어긋나 아무것도 안 잡는 검사가 조용히 남는다. 위반을 임시로 주입해 FAIL 하는 것을 본 뒤 되돌린다.

## 빌드는 공유 애셋을 더럽힌다 — 하네스가 원복한다

`./scripts/run.sh` 가 `-buildTarget` 으로 플랫폼을 전환하면, Unity 는 셰이더 스트리핑 상태(`m_Prefilter*`)와 플랫폼 배칭 설정을 **URP 애셋에 되쓴다.** Unity 의 정상 동작이라 막을 수 없다.

문제는 그 파일들이 RULE-02 심링크 폴더(`Assets/Settings/`)에 있다는 것이다. 워크트리에서는 그 워크트리의 git 이 변경을 보지 못하고 **메인 프로젝트**가 더러워지며, 메인에서 작업할 때는 빌드 부산물이 그대로 커밋에 섞인다 — M2 에서 실제로 한 번 섞여 들어가 amend 했다.

두 겹으로 막는다:

| | 무엇 |
|---|---|
| `run.sh` 의 `EXIT` 트랩 | 빌드가 끝나면(**실패해도**) `shared_assets_restore` 로 되돌리고 무엇을 되돌렸는지 출력 |
| `preflight.sh` 검사 3 | 심링크 폴더 **9곳 전체**를 커밋 직전에 다시 확인 |

검사 3 은 원래 `ProjectSettings/`·`Packages/` 만 봤다. `Assets/Settings/` 가 빠져 있어 이 오염을 **구조적으로 못 잡았다.** 보호 목록은 [`../scripts/ensure-worktree-setup.sh`](../scripts/ensure-worktree-setup.sh) 에서 직접 읽는다 — RULE-02 가 "세 곳이 어긋나면 오탐·미탐을 낸다" 고 못박은 지점이라 복제하지 않는다.

> **원복은 추적 중인 파일의 미스테이지 변경만 건드린다.** 빌드가 만드는 것이 정확히 그것이고, 스테이지된 변경까지 지우면 사람이 의도한 작업이 날아간다.
>
> `run-tests.sh`·`bridge-run.sh` 에는 트랩을 걸지 않았다. 둘은 `-buildTarget` 을 넘기지 않아 플랫폼 전환이 없고, 실제로 오염을 낸 적이 없다. 필요해지면 같은 라이브러리를 source 하면 된다.
