# Step 01: 어셈블리 골격 — `.asmdef` 6개

- **영역:** 골격 — 어셈블리 전체
- **선행 단계:** 없음. 단, **사람 아키텍트의 명시적 승인이 있어야 착수한다** (`CLAUDE.md` §7)
- **후행 단계:** step-02 ~ step-06 전부가 이 단계 없이는 시작할 수 없다

---

## 목적

`Assets/Code/Scripts/` 는 지금 `.gitkeep` 하나뿐이고 저장소에 `.asmdef` 가 **하나도 없다**(확인함). 이 상태로 코드를 쓰면 전부 `Assembly-CSharp` 에 뭉치고, 나중에 어셈블리를 나눌 때 네임스페이스·참조·테스트를 전부 다시 손대야 한다. 여기서 빈 어셈블리 6개와 단방향 의존만 세운다. **C# 코드는 한 줄도 쓰지 않는다.**

---

## ⛔ 착수 전 승인 게이트

이 단계는 `CLAUDE.md` §7 + RULE-01 에 걸린다. 에이전트가 단독으로 실행하지 않는다.

아키텍트에게 아래를 제시하고 **"승인"이라는 답을 받은 뒤에만** 파일을 만든다:

1. 생성할 6개 경로와 각 `.asmdef` 의 전체 JSON (아래 그대로)
2. 의존 방향 그래프
3. `autoReferenced: false` 유지 확인 (RULE-01 — `true` 면 Domain Reload)

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 수행한다.

### 생성 파일 (6개, 전부 신규)

```
Assets/Code/Scripts/Runtime/Runtime.asmdef
Assets/Code/Scripts/Runtime.Data/Runtime.Data.asmdef
Assets/Code/Scripts/Presentation/Presentation.asmdef
Assets/Code/Scripts/Editor/Editor.asmdef
Assets/Tests/EditMode/Tests.EditMode.asmdef
Assets/Tests/PlayMode/Tests.PlayMode.asmdef
```

`Assets/Code/Scripts/.gitkeep` 는 하위 폴더가 생기면 지워도 된다. `Assets/Tests/*/.gitkeep` 도 마찬가지.

### 각 파일 내용

**`Runtime.Data.asmdef`** — 최하위. 아무것도 참조하지 않는다.
```json
{
  "name": "Runtime.Data",
  "rootNamespace": "SushiDefense.Data",
  "references": [],
  "autoReferenced": false
}
```

**`Runtime.asmdef`**
```json
{
  "name": "Runtime",
  "rootNamespace": "SushiDefense",
  "references": ["Runtime.Data"],
  "autoReferenced": false
}
```

**`Presentation.asmdef`**
```json
{
  "name": "Presentation",
  "rootNamespace": "SushiDefense",
  "references": ["Runtime", "Runtime.Data"],
  "autoReferenced": false
}
```

**`Editor.asmdef`** — 게임용 에디터 툴링. M0 에서는 내용이 비어 있다.
```json
{
  "name": "Editor",
  "rootNamespace": "SushiDefense.EditorTools",
  "references": ["Runtime", "Runtime.Data", "Presentation"],
  "includePlatforms": ["Editor"],
  "autoReferenced": false
}
```

**`Tests.EditMode.asmdef`**
```json
{
  "name": "Tests.EditMode",
  "rootNamespace": "SushiDefense.Tests.EditMode",
  "references": [
    "Runtime",
    "Runtime.Data",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "overrideReferences": true,
  "autoReferenced": false
}
```

**`Tests.PlayMode.asmdef`**
```json
{
  "name": "Tests.PlayMode",
  "rootNamespace": "SushiDefense.Tests.PlayMode",
  "references": [
    "Runtime",
    "Runtime.Data",
    "Presentation",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "overrideReferences": true,
  "autoReferenced": false
}
```

### 선행 산출물 의존성

없음.

### 밸런스 수치

없음.

### 제약

- **RULE-01**: `autoReferenced` 는 전부 `false`. `[InitializeOnLoad]` 를 새로 만들지 않는다
- **RULE-06**: `ProjectSettings/` 를 수정하지 않는다. 이 단계는 `Assets/` 만 건드린다
- 의존 순환 금지. `Runtime.Data` 는 어떤 게임 어셈블리도 참조하지 않는다
- `Assets/Editor/`(ClaudeBridge 하네스)는 **건드리지 않는다.** 여기 만드는 `Editor.asmdef` 는 `Assets/Code/Scripts/Editor/` 쪽이고 별개다 (`CLAUDE.md` §10)
- `.meta` 파일을 손으로 만들거나 편집하지 않는다 (RULE-03). Unity 가 생성하게 둔다 — `./scripts/bridge-run.sh` 또는 에디터 임포트 1회로 채워진다

### 완료 판정

- [ ] `find Assets -name "*.asmdef" | wc -l` → `6`
- [ ] `grep -r '"autoReferenced": true' Assets/` → 결과 없음 (RULE-01)
- [ ] `.asmdef` 6개에 대응하는 `.meta` 가 생성됨 (`find Assets -name "*.asmdef.meta" | wc -l` → `6`)
- [ ] `./scripts/bridge-run.sh` — 컴파일 에러 0
- [ ] `./tests/run-tests.sh` — 테스트 0건이어도 **종료 코드가 2(컴파일 실패)가 아님**
- [ ] `git status --short ProjectSettings/` → 변경 없음

### 예상 커밋 메시지

```
build(asmdef): scaffold six assembly definitions
```

---

## 금지 사항

- **C# 파일을 만들지 않는다.** 이 단계는 골격뿐이다.
- 승인 없이 `.asmdef` 를 생성하지 않는다.
- 위 JSON 의 `name`·`references`·`autoReferenced` 를 임의로 바꾸지 않는다. 바꿔야 할 이유가 보이면 멈추고 아키텍트에게 보고한다.
- `Packages/manifest.json` 에 패키지를 추가하지 않는다 (§7).
