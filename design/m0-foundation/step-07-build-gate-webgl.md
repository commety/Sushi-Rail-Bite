# Step 07: 빌드 관문 — WebGL 스모크

- **영역:** 관문 — 새 코드 없음
- **선행 단계:** step-01 ~ step-06 전부 완료·머지
- **후행 단계:** M1 착수

---

## 목적

**M0 의 핵심 관문이다.** WebGL 은 IL2CPP + Managed Code Stripping 경로라 에디터에서는 멀쩡한데 거기서만 터지는 실패(리플렉션·`JsonUtility` 관련 `MissingMethodException`, 스트리핑된 타입)가 있다. 로직을 다 쌓은 뒤 발견하면 수정 범위가 커진다 — **지금 빈 씬으로 한 번 통과시켜 둔다** (플랜 README §3-(3)).

여기서 빌드가 실패하는 것은 사고가 아니라 **이 단계의 목적이 달성된 것**이다 (M0 §리스크). 원인을 보고하고 고친다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 수행한다. **새 C# 파일을 만들지 않는다.**

### 씬 확인 (수정 없음)

`Assets/Level/Scenes/SampleScene.unity` 가 이미 `ProjectSettings/EditorBuildSettings.asset` 의 `m_Scenes` 에 `enabled: 1` 로 등록돼 있다 (확인함). **새 씬을 만들지 않고 이걸 그대로 쓴다.** `ProjectSettings/` 는 RULE-06 · §7 로 에이전트가 못 고친다.

- 등록 상태 확인: `grep -A3 "m_Scenes:" ProjectSettings/EditorBuildSettings.asset`
- 씬이 빠져 있거나 `enabled: 0` 이면 **직접 고치지 말고 아키텍트에게 보고**한다

### 실행 순서

1. 전체 검사
   ```bash
   ./tests/preflight.sh
   ```
   `CLAUDE.md` §8 체크리스트 전량. 실패하면 여기서 멈추고 보고한다.

2. 컴파일 확인
   ```bash
   ./scripts/bridge-run.sh
   ```
   에러 0.

3. WebGL 빌드
   ```bash
   ./scripts/run.sh webgl
   ```
   `/run webgl` 스킬이 부르는 것과 같은 스크립트다. 산출물은 프로젝트 상위 `builds/{label}-{branch}-{shortsha}[-dirty]-WebGL-{timestamp}/`.

   > **`Unity -batchmode` 를 손으로 조립하지 않는다.** `Unity` 는 PATH 에 없고, 손으로 쓰면 `-quit` 이 붙어 빌드가 끝나기 전에 에디터가 내려간다 (`CLAUDE.md` §8).

4. 빌드 로그 확인 — `builds/.../unity-build.log` 에서 `error`·`Stripping`·`MissingMethod` 를 검색한다. **빌드 성공 종료 코드만 믿지 않는다**

### WebGL 모듈이 없을 때

`scripts/run.sh` 가 `WebGLSupport` 모듈 부재를 감지하면 안내를 낸다. Unity Hub 에서 `6000.5.6f1` 의 **WebGL Build Support** 를 설치해야 한다 — **모듈 설치는 사람이 한다.** 에이전트는 필요한 모듈명과 버전을 보고하고 멈춘다.

### 선행 산출물 의존성

step-01~06 의 모든 산출물. 어셈블리 6개가 IL2CPP 스트리핑을 통과하는지가 실질적 검증 대상이다.

### 밸런스 수치

없음.

### 제약

- **`ProjectSettings/` 를 수정하지 않는다** (RULE-06 · §7). 빌드 설정 변경이 필요하면 변경안을 제시하고 승인을 기다린다
- 씬 파일을 수정하지 않는다 (`.claude/rules/parallel-work.md` §3)
- 빌드 산출물(`builds/`)을 커밋하지 않는다 — 프로젝트 **상위** 폴더라 원래 저장소 밖이지만, `git status` 로 한 번 확인한다
- 원본 에셋(png/psd/wav 등)이 diff 에 섞이지 않았는지 확인 (`CLAUDE.md` §9)

### 완료 판정 (= M0 완료 판정)

- [ ] `find Assets -name "*.asmdef" | wc -l` → `6`, 의존 방향 단방향
- [ ] `Runtime.Data` 에 SO 3종 + 각각 데이터 검증 EditMode 테스트
- [ ] 이벤트 채널 SO 1종 + 발행/구독 EditMode 검증
- [ ] `SushiPool` 재사용 검증 — EditMode(step-05) + PlayMode(step-06) 통과
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] `./scripts/bridge-run.sh` 컴파일 에러 0
- [ ] `./tests/preflight.sh` 전 항목 통과
- [ ] **`./scripts/run.sh webgl` 빌드 성공** + `unity-build.log` 에 error 0
- [ ] `git status --short ProjectSettings/` → 변경 없음

### 보고 형식

빌드 후 아키텍트에게 아래를 보고한다:

```
M0 빌드 관문
  ✓ preflight    — {통과 항목 수}
  ✓ EditMode     — {N}/{N}
  ✓ PlayMode     — {N}/{N}
  ✓ WebGL 빌드   — builds/{디렉터리명}
    · 빌드 시간 {초}
    · 로그 경고 {건} (내역: ...)
```

이후 [`docs/plan/README.md`](../../docs/plan/README.md) §1 표의 M0 상태를 `☑` 로, [`docs/plan/M0-foundation.md`](../../docs/plan/M0-foundation.md) 의 완료 판정 체크박스를 채우는 것을 **제안**한다 (문서 갱신도 PR 로).

### 예상 커밋 메시지

```
chore(plan): mark M0 foundation complete
```

---

## 금지 사항

- 빌드를 통과시키려고 `ProjectSettings` 나 스트리핑 레벨을 임의로 낮추지 않는다. 원인을 보고한다.
- 테스트가 깨졌을 때 `[Ignore]` 로 덮거나 지우지 않는다 (`.claude/rules/tests.md` §7).
- `main`/`dev` 에 직접 커밋하지 않는다. `feat/*` → PR → 사람 승인 (§6).
