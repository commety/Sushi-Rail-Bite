#!/usr/bin/env python3
"""화면에 나갈 수 있는 글자를 모아 TMP 폰트 서브셋용 목록을 만든다.

WebGL 은 OS 폰트에 접근할 수 없어, 폰트 애셋에 없는 글자는 두부(□)로 나온다.
정적 서브셋의 유일한 실패 모드가 "나중에 추가한 문구의 글자가 빠지는 것"이므로,
목록을 손으로 관리하지 않고 **소스에서 다시 뽑을 수 있게** 한다.

    python3 scripts/extract-charset.py

문구를 추가했으면 이 스크립트를 다시 돌리고 폰트를 다시 굽는다. 빠진 글자는
`KoreanFontCoverageTests` 가 잡는다.

**주석은 담지 않는다.** 이 프로젝트는 문서 주석이 두꺼워서, 주석까지 담으면 한글이
489자가 되고 리터럴만 담으면 89자다 — 여덟 배 차이이고 아틀라스 한 단계(512 -> 1024)를
가른다. 주석 글자는 화면에 닿을 경로가 없으므로 제외가 안전하다.

반대로 어느 리터럴이 실제로 렌더되는지는 분류하지 않는다 — 예외 메시지든 라벨이든 전부
담는다. 그 분류 규칙은 반드시 어긋나고, 글자 몇 개의 비용은 무시할 만하다.
"""

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

# 화면 문구가 사는 곳
CODE_GLOBS = ["Assets/Code/Scripts/Presentation/**/*.cs"]
ASSET_GLOBS = ["Assets/Level/Balance/*.asset"]

OUT = ROOT / "Assets/Art/Fonts/charset-ko.txt"

HANGUL = re.compile(r"[가-힣]")

# C# 문자열 리터럴. 이스케이프된 따옴표를 건너뛴다.
STRING_LITERAL = re.compile(r'"(?:[^"\\]|\\.)*"')

# 숫자·영문·기호는 문구 조립에 항상 쓰인다
ASCII_SET = (
    "0123456789"
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    "abcdefghijklmnopqrstuvwxyz"
    " .,:;!?/()[]{}<>+-*=%'\"#&@~_|\\"
)

# 자주 쓰는 전각 기호 — 문구에 이미 등장한다
EXTRA = "—…·「」『』～"

# M6 이 거의 확실히 쓸 낱말만 미리 담는다. 폰트를 다시 굽는 것은 사람이 에디터에서
# 해야 하는 일이라 재작업 비용이 있지만, 예비분이 커지면 서브셋의 의미가 사라진다 —
# 확신이 높은 것만 남기고 나머지는 필요해질 때 이 스크립트를 다시 돌린다.
RESERVE = "설정시작계속다시종료저장나가기소리음악확인취소덱"


def collect() -> set[str]:
    chars: set[str] = set(ASCII_SET) | set(EXTRA) | set(RESERVE)

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
    chars = collect()
    text = "".join(sorted(chars))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(text + "\n", encoding="utf-8")

    hangul = sum(1 for c in chars if HANGUL.match(c))
    print(f"{OUT.relative_to(ROOT)}: {len(chars)}자 (한글 {hangul}, 그 외 {len(chars) - hangul})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
