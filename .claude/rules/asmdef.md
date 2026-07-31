---
description: .asmdef 파일 수정 시 적용되는 규칙
globs: ["**/*.asmdef"]
---

# 어셈블리 정의 파일 규칙

## 1. 불변 제약

- `autoReferenced` 는 반드시 `false` 로 유지한다. (RULES.md RULE-01 — `true` 로 바꾸면 Domain Reload)
- 의존성 순환을 만들지 않는다.
- **새 어셈블리 추가는 아키텍트 승인 사항이다** (`CLAUDE.md` §7). 에이전트는 제안만 하고 `.asmdef` 를 직접 만들지 않는다.
- `defineConstraints` 로 에디터 전용 어셈블리를 명확히 분리한다.

## 2. 이 프로젝트의 어셈블리 구성 (`CLAUDE.md` §3.7)

```
Runtime.asmdef         Assets/Code/Scripts/Runtime/        순수 게임 로직 (Unity API 의존 최소)
Runtime.Data.asmdef    Assets/Code/Scripts/Runtime.Data/   ScriptableObject 정의 (의존 최소)
Presentation.asmdef    Assets/Code/Scripts/Presentation/   MonoBehaviour, UI, View
Editor.asmdef          Assets/Code/Scripts/Editor/         게임용 에디터 툴링
Tests.EditMode.asmdef  Assets/Tests/EditMode/              EditMode 단위 테스트
Tests.PlayMode.asmdef  Assets/Tests/PlayMode/              PlayMode 통합 테스트
```

> 현재 저장소에는 아직 `.asmdef` 가 하나도 없다 (`Assets/Code/Scripts/` 는 비어 있음). 위 구성은 **앞으로 만들 목표 구조**이며, 최초 생성 역시 아키텍트 승인 대상이다.

## 3. 의존 방향 (단방향)

```
Runtime.Data  ←  Runtime  ←  Presentation
      ↑              ↑            ↑
      └── Tests.EditMode / Tests.PlayMode ──┘
```

- `Tests.*` 는 `Runtime` · `Runtime.Data` 를 참조해도 되지만 **역방향은 절대 금지** (`CLAUDE.md` §3.7).
- `Runtime` 은 `Presentation` 을 모른다. UI 로 값을 보내야 하면 C# `event` 또는 SO 이벤트 채널(`~EventChannelSO`, `Runtime.Data`)로 의존을 역전시킨다.
- `Runtime.Data` 가 가장 아래다. 다른 게임 어셈블리를 참조하지 않는다.

## 4. 테스트 어셈블리 체크

- `Tests.EditMode` 는 `"includePlatforms": ["Editor"]` 를 유지한다.
- 두 테스트 asmdef 모두 `"references"` 에 `UnityEngine.TestRunner` · `UnityEditor.TestRunner`, `"precompiledReferences"` 에 `nunit.framework.dll`, `"defineConstraints"` 에 `UNITY_INCLUDE_TESTS` 가 필요하다. 빠지면 Test Runner 가 테스트를 발견하지 못한다.

## 5. 에이전트 툴링 어셈블리

`Assets/Editor/`(ClaudeBridge, RunBuildCommand)는 게임 어셈블리 구성과 별개인 **하네스 인프라**다. 게임 코드가 여기에 의존하게 만들지 않는다.
