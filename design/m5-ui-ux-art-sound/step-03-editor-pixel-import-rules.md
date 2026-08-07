# Step 03: 픽셀 아트 임포트 규칙 자동 적용

- **영역:** `editor` — 어셈블리 `Editor`
- **선행 단계:** 없음
- **후행 단계:** step-04 가 만드는 모든 텍스처가 이 규칙을 통과한다. **순서가 뒤집히면 13장을 손으로 다시 임포트해야 한다**

---

## 목적

원문 계획이 요구한 *"Filter Mode `Point`, Mip Maps 끄기, 압축 신중히"* 를 **코드로 강제한다.**

인스펙터 수작업으로 두면 반드시 샌다 — 스프라이트를 추가할 때마다 사람이 네 개 항목을 기억해야 하고, 하나만 빠져도 픽셀 아트가 흐릿하게 뭉개진다. 증상이 "약간 이상함" 이라 발견도 늦다.

`Assets/Code/Scripts/Editor/` 는 지금 `.asmdef` 만 있고 스크립트가 0개다. **이 단계가 첫 입주자다.**

이 규칙은 M5 가 끝나도 살아남는다. M6 의 UI 스프라이트, 나중에 들어올 사람이 그린 원본 픽셀 아트까지 전부 자동으로 맞춰진다 — **M5 가 남기는 가장 오래 가는 자산이다.**

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Editor/Import/PixelArtImportSettings.cs` — 생성 (순수 결정 로직)
- `Assets/Code/Scripts/Editor/Import/PixelArtTextureImporter.cs` — 생성 (`AssetPostprocessor`)
- `Assets/Tests/EditMode/Editor/PixelArtImportSettingsTests.cs` — 생성
- `Assets/Tests/EditMode/Tests.EditMode.asmdef` — **수정 (§7 승인 필요)**

### 핵심 심볼

```csharp
namespace SushiDefense.EditorTools.Import
{
    /// <summary>
    /// 픽셀 아트 텍스처에 적용할 값. <b>결정만 한다</b> — 임포터를 만지지 않으므로
    /// EditMode 에서 값만 확인할 수 있다.
    /// </summary>
    public static class PixelArtImportSettings
    {
        public const int PixelsPerUnit = 32;

        /// <summary>이 경로가 픽셀 아트 규칙의 대상인가.</summary>
        public static bool AppliesTo(string assetPath);

        /// <summary>대상 텍스처에 적용할 설정을 계산한다.</summary>
        public static PixelArtImportPlan PlanFor(string assetPath);
    }

    public readonly struct PixelArtImportPlan
    {
        public bool IsPixelArt { get; }
        public FilterMode FilterMode { get; }          // Point
        public bool MipmapEnabled { get; }             // false
        public int PixelsPerUnit { get; }
        public TextureImporterCompression Compression { get; }
        public bool AlphaIsTransparency { get; }       // true
        public SpriteMeshType MeshType { get; }        // FullRect
        public int MaxTextureSize { get; }
    }
}
```

```csharp
namespace SushiDefense.EditorTools.Import
{
    /// <summary>계획을 임포터에 옮긴다. 판단하지 않는다.</summary>
    internal sealed class PixelArtTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture();
    }
}
```

**둘로 나누는 이유**: `AssetPostprocessor` 는 Unity 가 임포트 중에 부르는 콜백이라 테스트에서 직접 호출할 수 없다. 판단을 `PixelArtImportSettings` 로 빼면 *"이 경로에 어떤 설정이 적용되나"* 를 EditMode 로 확인할 수 있다 — `MonoBehaviour` 에서 로직을 빼는 것과 같은 이유다 (`CLAUDE.md` §3.2).

### 적용 대상 경로

```
Assets/Art/Sprites/**          ← 최종 픽셀 아트 (CLAUDE.md §10 정본)
Assets/Level/Placeholder/**    ← 기존 블록 placeholder
```

> **PPU 는 32 이고, 기존 플레이스홀더 2장은 이 단계에서 32×32 로 재작업한다.**
> 착수 시 실측: `SushiBlock.png` · `CustomerBlock.png` 이 **64×64 @ PPU 64 = 1 유닛**이었다.
> PPU 32 만 강제하면 이 둘이 화면에서 2배가 되어, 벨트 길이 20·자리 4·8·12·16 에 맞춰 놓은
> 배치가 어긋난다. **같은 경로에 덮어쓰면 `.meta` 가 남아 GUID 가 보존**되므로
> `SushiItem.prefab` · `Customer.prefab` 의 참조는 끊기지 않는다 (실측으로 확인).
>
> **`maxTextureSize` 는 규칙에 넣지 않는다.** step-11 의 배경 이미지가 정당하게 클 수 있고,
> 캡을 걸면 사람이 의도한 해상도를 조용히 반토막 낸다. 규칙은 *"빠지면 언제나 틀린 것"* 만
> 강제한다.

`Assets/Art/Sprites/UI/` 도 포함한다. UI 아이콘도 픽셀 아트다.

**대상 밖 경로에서는 아무것도 하지 않는다.** `AppliesTo` 가 `false` 면 임포터가 손을 떼야 한다 — 나중에 들어올 사진·일러스트·폰트 아틀라스까지 `Point` 로 만들면 안 된다.

### 압축 설정에 대한 판단

원문이 *"압축 신중히 — 블록 압축은 픽셀 아트에서 색이 뭉개진다"* 라고 적었다. 배포 타깃이 WebGL 이므로 두 요구가 부딪힌다:

| | 색 보존 | 용량 |
|---|---|---|
| `Uncompressed` | 완벽 | 큼 — WebGL 초기 로드에 직결 |
| `Compressed` (DXT) | 픽셀 경계가 뭉개짐 | 작음 |

**`Uncompressed` 로 시작한다.** 데모의 스프라이트는 작고 종류가 적어(13종 × 32px 급) 절대 용량이 작다. step-11 의 WebGL 실측에서 초기 로드가 문제로 드러나면 그때 조정하고, 이유를 `.claude/domain/` 에 남긴다. **먼저 뭉개고 나중에 되돌리는 것보다 이쪽이 싸다.**

### 선행 산출물 의존성

없음.

### 밸런스 수치

**없다.** PPU·필터 모드는 밸런스가 아니라 파이프라인 설정이다 (README D7). SO 로 빼지 않는다 — 값이 갈리는 것 자체가 버그이고, 사람이 플레이하며 조정할 값이 아니다. 대신 **`PixelArtImportSettings` 한 파일 한 곳**에만 둔다.

### 제약

- `Editor.asmdef` 는 `includePlatforms: ["Editor"]` 다. 이 코드는 빌드에 들어가지 않는다
- **`Editor` 어셈블리에 게임 로직을 두지 않는다.** 여기는 툴링이다
- `Editor.asmdef` 의 `autoReferenced: false` 를 바꾸지 않는다 (RULE-01 — Domain Reload)
- 네임스페이스는 `Editor.asmdef` 의 `rootNamespace` 인 `SushiDefense.EditorTools` 아래 (`Assets/Editor/` 의 ClaudeBridge 와 혼동하지 않는다 — 그쪽은 하네스 인프라이고 `Editor.ClaudeBridge` 네임스페이스를 쓴다)
- **`.meta` 파일을 직접 수정하지 않는다** (RULE-03). 임포트 설정은 임포터가 쓰게 한다

### §7 승인 요청 — asmdef 수정

`Tests.EditMode` 가 `Editor` 어셈블리를 참조해야 `PixelArtImportSettings` 를 테스트할 수 있다.

```diff
  "references": [
    "Runtime",
    "Runtime.Data",
    "Presentation",
+   "Editor",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
```

- 순환 없음 — `Editor` 는 `Tests.*` 를 참조하지 않는다
- 방향 위반 없음 — 프로덕션이 테스트를 참조하는 것이 금지이지 그 반대는 정상이다 (`asmdef.md` §3)
- 두 어셈블리 모두 `includePlatforms: ["Editor"]` 라 플랫폼 충돌 없음

**이 diff 를 제시하고 승인을 받은 뒤 진행한다.** 승인 전에는 `.asmdef` 를 건드리지 않는다.

### 테스트 계획 (TDD — 실패부터)

```
대상 판정  AppliesTo_ArtSpritesPath_ReturnsTrue
           AppliesTo_PlaceholderPath_ReturnsTrue
           AppliesTo_AudioPath_ReturnsFalse
           AppliesTo_ArtSpritesSubfolder_ReturnsTrue      ← UI/ 같은 하위도 포함
           AppliesTo_PathContainingArtSpritesMidway_ReturnsFalse  ← 부분 문자열 오탐 방지
설정       PlanFor_PixelArt_UsesPointFilter
           PlanFor_PixelArt_DisablesMipmaps
           PlanFor_PixelArt_KeepsAlphaTransparency
           PlanFor_NonPixelArt_IsNotPixelArt
```

**부분 문자열 오탐 테스트를 빠뜨리지 않는다.** 경로 경계를 보는 구현인지 확인하는 반례를 같은 테스트 파일에 박는다.

### 주입 실측 — 여기서 공허한 테스트를 하나 잡았다

| 주입 | 예측 | 실제 |
|---|---|---|
| `StartsWith` → `Contains` | `AppliesTo_PathContainingRootMidway_ReturnsFalse` | **0건 — 전량 통과** |
| 루트에서 끝 슬래시 제거 | 1건 | 예측대로 **1건** (`AppliesTo_SiblingFolderWithSamePrefix_ReturnsFalse`) |
| 폰트 폴더를 규칙 대상에 포함 | 1건 | 예측대로 **1건** (`AppliesTo_FontsPath_ReturnsFalse`) |
| `Point` → `Bilinear` | 1건 | 예측대로 **1건** (`PlanFor_PixelArt_UsesPointFilter`) |

> **첫 번째가 이 단계의 수확이다.** *"부분 문자열 오탐 반례를 박았다"* 고 믿고 쓴
> `"Assets/Level/Fan Art/Sprites/x.png"` 는 **`Contains` 구현으로도 통과한다** — 루트가
> `"Assets/Art/Sprites/"` 라 `Assets/` 접두 때문에 애초에 매칭되지 않기 때문이다.
> 반례를 쓴 사람(나)조차 그 반례가 무엇을 배제하는지 착각했다.
>
> `tests.md` §3 의 처방대로 되돌리고 **테스트를 먼저 추가**했다 —
> `"Packages/com.vendor.kit/Assets/Art/Sprites/icon.png"` 처럼 **루트가 경로 중간에 통째로
> 박힌** 경우라야 `Contains` 를 배제한다. 추가 후 같은 주입을 다시 넣어 실제로 잡히는 것을
> 확인했다.
>
> 교훈: **반례는 "비슷해 보이는 문자열" 이 아니라 "잘못된 구현이 실제로 참을 돌려주는
> 입력" 이어야 한다.** 둘은 다르며, 눈으로는 구분되지 않는다.

### 완료 판정

- [ ] `grep -rn "class PixelArtImportSettings" Assets/Code/Scripts/Editor/`
- [ ] `grep -c "Editor" Assets/Tests/EditMode/Tests.EditMode.asmdef` — 참조 추가 확인 (**승인 후**)
- [ ] `Assets/Level/Placeholder/SushiBlock.png` 을 강제 재임포트했을 때 `.meta` 의 `filterMode` 가 `0`(Point), `mipmaps: enableMipMap: 0` 인지 확인 — `.meta` 를 **읽어서** 확인하는 것이지 고치는 것이 아니다
- [ ] `./tests/run-tests.sh` Green
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(editor): enforce pixel art texture import settings
```

---

## 금지 사항

- 승인 없이 `.asmdef` 를 수정하지 않는다 (`CLAUDE.md` §7)
- `.meta` 파일을 직접 편집하지 않는다 (RULE-03)
- `ProjectSettings/` 를 건드리지 않는다 — Sprite Atlas 설정은 step-04 다
- 대상 경로 밖의 텍스처에 손대지 않는다
- 다른 어셈블리 파일을 수정하지 않는다
