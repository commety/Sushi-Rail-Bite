#!/usr/bin/env python3
"""M6 UI 스프라이트를 픽셀 단위로 그려 PNG 로 쓴다.

**SVG 래스터화를 쓰지 않는다.** `Sprite.ImportFromSvg` 는 픽셀 아트에 두 가지가 맞지 않다:

1. 출력이 **정방형 POT 로 강제**된다 — `card-frame` 의 48x64 를 만들 수 없고 64x64 로 스냅된다
2. 테셀레이션 + MSAA 는 8x8 셀에서 형태를 뭉갠다. 픽셀 아트는 안티에일리어싱이 없어야 한다

여기서는 픽셀을 직접 찍는다. 파이썬 stdlib 만 쓰고(zlib + struct), 크기가 정확하며,
경계가 하드하다. 팔레트는 기존 스프라이트(icon-coin / belt-rail / icon-plate / bg-wall)에서
읽어 온 값이라 새 스프라이트가 기존 화면과 같은 톤에 있다.

    python3 scripts/make-ui-sprites.py

임포트 설정(PPU 32 · Point · 무압축 · FullRect)은 `PixelArtImportSettings` 가 자동 적용한다.
9-slice 테두리는 텍스처가 아니라 임포터의 값이라 여기서 정하지 않는다 — `SLICE_BORDERS` 에
적어 두고 임포트 뒤에 따로 물린다.
"""

import pathlib
import struct
import sys
import zlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT_DIR = ROOT / "Assets/Art/Sprites/UI"

# 기존 스프라이트에서 뽑은 팔레트. 새 UI 가 벨트·접시·벽과 같은 톤에 있어야 한다.
WOOD_LIGHT = (0x7A, 0x56, 0x3A)
WOOD_MID = (0x6B, 0x4A, 0x32)
WOOD_DARK = (0x4E, 0x35, 0x24)
GOLD = (0xE8, 0xC2, 0x4A)
GOLD_DARK = (0xA0, 0x84, 0x1C)
GOLD_LIGHT = (0xF5, 0xDC, 0x8A)
PLATE = (0xE8, 0xE8, 0xEC)
PLATE_DARK = (0xB0, 0xB0, 0xBA)
RAIL = (0x3A, 0x3F, 0x4A)
RAIL_LIGHT = (0x56, 0x5D, 0x6C)
RAIL_DARK = (0x27, 0x2B, 0x33)

CLEAR = (0, 0, 0, 0)

# 9-slice 테두리 (left, bottom, right, top). 임포터에 물릴 값이다.
SLICE_BORDERS = {
    "button": (6, 6, 6, 6),
    "button-pressed": (6, 6, 6, 6),
    "panel": (8, 8, 8, 8),
    "card-frame": (8, 8, 8, 8),
    "card-frame-disabled": (8, 8, 8, 8),
}


class Canvas:
    """좌상단 원점 픽셀 캔버스. 알파 0 으로 시작한다."""

    def __init__(self, width: int, height: int):
        self.w = width
        self.h = height
        self.px = [[CLEAR] * width for _ in range(height)]

    def set(self, x: int, y: int, rgb, alpha: int = 255) -> None:
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = (rgb[0], rgb[1], rgb[2], alpha)

    def rect(self, x: int, y: int, w: int, h: int, rgb, alpha: int = 255) -> None:
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.set(xx, yy, rgb, alpha)

    def frame(self, x: int, y: int, w: int, h: int, rgb, thickness: int = 1) -> None:
        for t in range(thickness):
            self.rect(x + t, y + t, w - 2 * t, 1, rgb)
            self.rect(x + t, y + h - 1 - t, w - 2 * t, 1, rgb)
            self.rect(x + t, y + t, 1, h - 2 * t, rgb)
            self.rect(x + w - 1 - t, y + t, 1, h - 2 * t, rgb)

    def disc(self, cx: float, cy: float, r: float, rgb) -> None:
        """계단 경계의 원. 안티에일리어싱을 넣지 않는다 — 픽셀 아트다."""
        for yy in range(self.h):
            for xx in range(self.w):
                if (xx + 0.5 - cx) ** 2 + (yy + 0.5 - cy) ** 2 <= r * r:
                    self.set(xx, yy, rgb)

    def write(self, path: pathlib.Path) -> None:
        raw = bytearray()
        for row in self.px:
            raw.append(0)  # filter type 0 (None) — 크기보다 재현성이 중요하다
            for r, g, b, a in row:
                raw += bytes((r, g, b, a))

        def chunk(tag: bytes, data: bytes) -> bytes:
            return (struct.pack(">I", len(data)) + tag + data
                    + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

        png = (b"\x89PNG\r\n\x1a\n"
               + chunk(b"IHDR", struct.pack(">IIBBBBB", self.w, self.h, 8, 6, 0, 0, 0))
               + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
               + chunk(b"IEND", b""))

        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(png)


# ── 스프라이트 ─────────────────────────────────────────────────────────────

def card_frame(disabled: bool) -> Canvas:
    """48x64 카드 틀. 등급별로 나누지 않는다 — 틀은 하나다 (README D3).

    **가운데는 비워 둔다.** 9-slice 테두리가 8 이라 `y 8~55` 가 카드 안쪽 전체(176px)로
    늘어나므로, 여기 그린 것은 무엇이든 화면에서 굵은 띠가 되어 설명 글자를 덮는다.
    한때 아래쪽에 대비용 한 단이 깔려 있었는데, 카드가 커지면서 그 위쪽 6줄이 늘어나
    **글자를 읽히게 하려던 것이 정확히 반대로 작동했다.** 대비가 필요하면 그 자리는
    카드 프리팹의 글자 영역이 진다.

    모서리 못은 테두리 영역(0~7) 안이라 늘어나지 않는다 — 그래서 남는다.
    """
    border = RAIL_LIGHT if disabled else WOOD_LIGHT
    inner = RAIL if disabled else WOOD_DARK
    fill = PLATE_DARK if disabled else PLATE
    accent = RAIL_LIGHT if disabled else GOLD

    c = Canvas(48, 64)
    c.rect(0, 0, 48, 64, border)
    c.frame(1, 1, 46, 62, inner)
    c.rect(3, 3, 42, 58, fill)

    # 못은 색만으로도 구분되게 — 못 놓는 카드는 금색을 뺀다.
    for cx, cy in ((3, 3), (43, 3), (3, 59), (43, 59)):
        c.rect(cx, cy, 2, 2, accent)
    return c


def badge_digesting() -> Canvas:
    """16x16 소화중 배지. 크기가 요구사항이라 정확히 16x16 이어야 한다."""
    c = Canvas(16, 16)
    c.disc(8, 8, 7.5, RAIL_DARK)
    c.disc(8, 8, 6.2, GOLD_DARK)
    c.disc(8, 8, 5.2, RAIL_DARK)

    # 시계 바늘 — 소화가 시간의 문제임을 형태로 말한다.
    c.rect(7, 4, 2, 5, GOLD_LIGHT)
    c.rect(8, 8, 4, 2, GOLD_LIGHT)
    return c


def bar_cell() -> Canvas:
    """8x8 포화도 칸. 칸이 채워지는 표시라 가장자리가 또렷해야 한다."""
    c = Canvas(8, 8)
    c.rect(0, 0, 8, 8, GOLD_DARK)
    c.rect(1, 1, 6, 6, GOLD)
    c.rect(1, 1, 6, 1, GOLD_LIGHT)
    # 네 귀퉁이를 깎아 칸이 이어져도 개수가 세어진다.
    for x, y in ((0, 0), (7, 0), (0, 7), (7, 7)):
        c.set(x, y, (0, 0, 0), 0)
    return c


def bar_slot() -> Canvas:
    """12x12 포화도 칸 배경판. 칸(8x8)보다 사방 2px 크다.

    손님 옆 세로바는 **테이블 스프라이트를 벗어날 수 없다** — 테이블이 자리 기준 좌우
    ±1 유닛을 덮는데 손님 몸통은 ±0.5 뿐이고, 벗어나려 |x| > 1 로 밀면 이웃 자리에 닿는다.
    그래서 위치가 아니라 **배경으로** 푼다: 뒤가 나무든 벽이든 금색 칸이 같게 읽힌다.

    **불투명이어야 한다.** 판이 칸 간격보다 커서 서로 겹치는데, 반투명이면 겹친 자리만
    두 번 어두워져 줄무늬가 생긴다. 겹치는 것은 의도다 — 그래야 칸이 이어질 때 판도
    한 줄기 기둥이 된다.

    **9-slice 가 아니다.** 원본 비율 그대로 균일 배율로 쓰므로 테두리를 물릴 일이 없다.
    """
    c = Canvas(12, 12)
    c.rect(0, 0, 12, 12, RAIL_DARK)
    c.rect(1, 1, 10, 10, RAIL)
    return c


def button(pressed: bool) -> Canvas:
    """32x32 9-slice 버튼. 테두리 6px 안쪽이 늘어난다.

    **입체는 위아래 두 줄이 아니라 네 변의 베벨로 만든다.** 두 줄만으로는 눌림과
    안 눌림이 «조금 어두운 갈색» 둘로 보여, 화면에서 상태가 바뀐 것을 알아채지 못한다.
    눌리면 밝은 면과 어두운 면이 **서로 자리를 바꾼다** — 색이 아니라 광원의 방향이
    바뀌는 것이라 한눈에 읽힌다.
    """
    c = Canvas(32, 32)
    body = WOOD_MID if pressed else WOOD_LIGHT
    lit = WOOD_LIGHT if pressed else GOLD_DARK
    shade = RAIL_DARK if pressed else WOOD_DARK

    c.rect(0, 0, 32, 32, body)

    top_color, bottom_color = (shade, lit) if pressed else (lit, shade)
    c.rect(0, 0, 32, 3, top_color)
    c.rect(0, 29, 32, 3, bottom_color)
    c.rect(0, 0, 2, 32, top_color)
    c.rect(30, 0, 2, 32, bottom_color)

    # 바깥 한 줄은 항상 가장 어둡게 — 배경이 밝든 어둡든 버튼의 외곽이 선다.
    c.frame(0, 0, 32, 32, RAIL_DARK)

    # 눌린 쪽은 안쪽에 그림자를 한 겹 더 둬 «들어갔다» 를 만든다.
    if pressed:
        c.rect(2, 3, 28, 1, RAIL_DARK)
        c.rect(2, 3, 1, 26, RAIL_DARK)
    return c


def panel() -> Canvas:
    """32x32 9-slice 패널 배경. 테두리 8px."""
    c = Canvas(32, 32)
    c.rect(0, 0, 32, 32, WOOD_DARK)
    c.frame(0, 0, 32, 32, RAIL_DARK)
    c.frame(1, 1, 30, 30, WOOD_LIGHT)
    c.frame(2, 2, 28, 28, WOOD_MID)
    c.rect(3, 3, 26, 26, RAIL, 235)   # 살짝 비쳐 뒤 화면이 죽지 않게
    return c


def icon_deck() -> Canvas:
    """16x16 덱 아이콘. 카드 석 장을 겹쳐 «묶음» 을 형태로 말한다.

    뒤에서 앞으로 그린다. **세 장의 채움을 서로 다르게** 둬야 16px 안에서 경계가 읽힌다 —
    같은 톤으로 겹치면 계단 하나로 뭉개진다.
    """
    c = Canvas(16, 16)
    cards = (((0, 0), PLATE_DARK), ((2, 2), (0xCE, 0xCE, 0xD6)), ((4, 4), PLATE))
    for (dx, dy), fill in cards:
        c.rect(dx, dy, 10, 12, RAIL_DARK)
        c.rect(dx + 1, dy + 1, 8, 10, fill)

    # 맨 앞 카드에만 금색 띠 — 어느 쪽이 위인지 색으로도 말한다.
    c.rect(6, 6, 6, 2, GOLD)
    return c


def icon_menu() -> Canvas:
    """16x16 메뉴 아이콘. 가로줄 셋."""
    c = Canvas(16, 16)
    for y in (3, 7, 11):
        c.rect(2, y, 12, 2, RAIL_DARK)
        c.rect(2, y, 12, 1, PLATE)
    return c


SPRITES = {
    "card-frame": lambda: card_frame(False),
    "card-frame-disabled": lambda: card_frame(True),
    "badge-digesting": badge_digesting,
    "bar-cell": bar_cell,
    "bar-slot": bar_slot,
    "button": lambda: button(False),
    "button-pressed": lambda: button(True),
    "panel": panel,
    "icon-deck": icon_deck,
    "icon-menu": icon_menu,
}


def main() -> int:
    for name, build in SPRITES.items():
        canvas = build()
        path = OUT_DIR / f"{name}.png"
        canvas.write(path)
        border = SLICE_BORDERS.get(name)
        note = f"  9-slice {border}" if border else ""
        print(f"{path.relative_to(ROOT)}: {canvas.w}x{canvas.h}{note}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
