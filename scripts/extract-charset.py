#!/usr/bin/env python3
"""TMP 폰트 서브셋용 글자 목록을 만들고, 폰트가 못 그리는 문구를 잡아낸다.

WebGL 은 OS 폰트에 접근할 수 없어, 폰트 애셋에 없는 글자는 두부(□)로 나온다.

**목록의 출처가 M6 에서 바뀌었다.** 예전에는 소스의 문자열 리터럴에서 글자를 모았는데,
그러면 문구를 한 줄 고칠 때마다 사람이 TMP Font Asset Creator 를 다시 열어야 했다 (§7).
백과사전 설명·튜토리얼처럼 글이 늘어나는 화면에서는 그 왕복이 계속 붙고, 한 번
빠뜨리면 **빌드해야만 드러나는 두부**가 된다 — 이 프로젝트에서 가장 비싼 실패 모드다.

그래서 지금은 **폰트가 그릴 수 있는 글자를 전부 굽는다.** 실측 근거:

    글리프 최대 9x12 px + 패딩 1 -> 셀 154 px^2
    1024x1024 아틀라스 용량 ~6,800 글리프 / 이 목록 ~3,250 글리프 = 48% 점유
    Alpha8 텍스처 1 MB (256x256 일 때 64 KB)

즉 **전체를 담아도 아틀라스 한 장**이다. 예전 주석이 걱정한 "489자면 512<->1024 를
가른다" 는 실측해 보니 아낄 대상이 아니었다.

한자(640)와 가나(185)는 **뺀다.** 아틀라스 크기는 그대로지만 애셋의 글리프 메타데이터가
0.5MB 가량 늘고, 화면에 쓸 계획이 없다. 쓰게 되면 EXCLUDED 에서 그 범위를 지우면 된다.

    python3 scripts/extract-charset.py

폰트를 바꾸면 다시 돌린다. **문구를 바꾸는 것으로는 다시 돌릴 필요가 없다** — 그것이
이번 변경의 목적이다.

리터럴 검사는 남는다. 목적이 바뀌었을 뿐이다: 이제는 서브셋을 만드는 대신
**폰트에 아예 없는 글자를 쓰고 있는지** 본다. 이 폰트는 현대 한글 11,172 음절 중
2,791 자만 갖고 있어서, 드문 음절(예: 뷁)은 서브셋을 아무리 넓혀도 두부가 된다.
"""

import pathlib
import re
import struct
import sys
import unicodedata

ROOT = pathlib.Path(__file__).resolve().parent.parent

FONT = ROOT / "Assets/Art/Fonts/x10y12pxDenkiChipHangul.ttf"
OUT = ROOT / "Assets/Art/Fonts/charset-ko.txt"

# 화면 문구가 사는 곳 — 이제는 검사 대상이지 목록의 출처가 아니다
CODE_GLOBS = ["Assets/Code/Scripts/Presentation/**/*.cs"]
ASSET_GLOBS = ["Assets/Level/Balance/*.asset"]

HANGUL = re.compile(r"[가-힣]")

# C# 문자열 리터럴. 이스케이프된 따옴표를 건너뛴다.
STRING_LITERAL = re.compile(r'"(?:[^"\\]|\\.)*"')

# 굽지 않을 범위. 아틀라스 크기는 그대로지만 글리프 메타데이터가 늘고, 쓸 계획이 없다.
EXCLUDED = (
    (0x4E00, 0x9FFF),   # 한자 (CJK Unified Ideographs)
    (0xF900, 0xFAFF),   # 한자 (CJK Compatibility Ideographs)
    (0x3040, 0x30FF),   # 히라가나 · 가타카나
)

# 그려지지 않는 유니코드 분류. cmap 에 있어도 굽기가 건너뛰므로, 목록에 남기면
# "목록에는 있는데 폰트에는 없는" 상태가 되어 커버리지 테스트가 **영영 빨간불**이 된다.
# 폭 있는 공백(Zs)은 남긴다 — 글리프가 있고 줄 나눔에 쓰인다.
UNRENDERABLE = frozenset(("Cc", "Cf", "Cs", "Co", "Cn", "Zl", "Zp"))


def font_codepoints(path: pathlib.Path) -> set[int]:
    """TTF 의 cmap(format 4)에서 코드포인트를 읽는다.

    fontTools 를 쓰지 않는다 — 새 패키지 추가는 팀 합의 사항이고 (CLAUDE.md §7),
    필요한 것은 cmap 한 테이블뿐이다.
    """
    data = path.read_bytes()
    table_count = struct.unpack(">H", data[4:6])[0]

    cmap_offset = None
    for i in range(table_count):
        entry = 12 + 16 * i
        if data[entry:entry + 4] == b"cmap":
            cmap_offset = struct.unpack(">I", data[entry + 8:entry + 12])[0]
            break

    if cmap_offset is None:
        raise SystemExit(f"cmap 테이블이 없습니다: {path}")

    subtable = None
    for i in range(struct.unpack(">H", data[cmap_offset + 2:cmap_offset + 4])[0]):
        record = cmap_offset + 4 + 8 * i
        offset = struct.unpack(">I", data[record + 4:record + 8])[0]
        if struct.unpack(">H", data[cmap_offset + offset:cmap_offset + offset + 2])[0] == 4:
            subtable = cmap_offset + offset

    if subtable is None:
        raise SystemExit(f"format 4 서브테이블이 없습니다: {path}")

    seg_count = struct.unpack(">H", data[subtable + 6:subtable + 8])[0] // 2
    end_at = subtable + 14
    start_at = end_at + seg_count * 2 + 2

    codes: set[int] = set()
    for i in range(seg_count):
        end = struct.unpack(">H", data[end_at + 2 * i:end_at + 2 * i + 2])[0]
        start = struct.unpack(">H", data[start_at + 2 * i:start_at + 2 * i + 2])[0]
        if start == 0xFFFF:
            continue
        codes.update(range(start, min(end, 0xFFFE) + 1))

    return codes


def keep(code: int) -> bool:
    if unicodedata.category(chr(code)) in UNRENDERABLE:
        return False
    return not any(low <= code <= high for low, high in EXCLUDED)


def screen_text() -> set[str]:
    """화면에 나갈 수 있는 한글. 서브셋을 만들지 않고 **대조용**으로만 쓴다."""
    chars: set[str] = set()

    for pattern in CODE_GLOBS:
        for path in ROOT.glob(pattern):
            for line in path.read_text(encoding="utf-8").splitlines():
                if line.lstrip().startswith("//"):   # /// 문서 주석 포함
                    continue
                for literal in STRING_LITERAL.findall(line):
                    chars |= set(HANGUL.findall(literal))

    for pattern in ASSET_GLOBS:
        for path in ROOT.glob(pattern):
            for line in path.read_text(encoding="utf-8").splitlines():
                if "_displayName:" in line or "_description:" in line:
                    chars |= set(HANGUL.findall(line))

    return chars


def main() -> int:
    if not FONT.exists():
        raise SystemExit(f"폰트를 찾지 못했습니다: {FONT}")

    codes = sorted(c for c in font_codepoints(FONT) if keep(c))
    text = "".join(chr(c) for c in codes)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(text + "\n", encoding="utf-8")

    syllables = sum(1 for c in codes if 0xAC00 <= c <= 0xD7A3)
    print(f"{OUT.relative_to(ROOT)}: {len(codes)}자 "
          f"(한글 음절 {syllables}, 그 외 {len(codes) - syllables})")

    # 폰트가 못 그리는 문구는 서브셋을 넓혀도 두부가 된다. 유일하게 남은 실패 모드다.
    covered = {chr(c) for c in codes}
    missing = sorted(screen_text() - covered)
    if missing:
        print(f"경고: 폰트에 없는 글자를 화면 문구가 쓰고 있습니다 — {''.join(missing)}",
              file=sys.stderr)
        return 1

    print("화면 문구의 글자가 전부 폰트 안에 있습니다.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
