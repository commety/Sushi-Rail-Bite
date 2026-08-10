using System.Collections.Generic;

namespace SushiDefense.EditorTools.Tiles
{
    /// <summary>
    /// 스테이지 바탕을 어떻게 칠할지 <b>정하기만</b> 한다. 타일맵도 씬도 모르므로 EditMode 로
    /// 확인할 수 있다 — <see cref="Import.PixelArtImportSettings"/> 가 임포터를 만지지 않는
    /// 것과 같은 이유다.
    ///
    /// <para>
    /// <b>왜 코드로 칠하나.</b> 손으로 칠한 타일맵은 씬 파일 안의 압축된 좌표 배열이라, 잘못
    /// 칠린 곳을 diff 로 볼 수 없고 되돌리려면 다시 손으로 칠해야 한다. 여기 규칙으로 두면
    /// 벽 한 줄을 옮기는 일이 상수 하나가 된다. 사람이 붓으로 덧칠하는 길은 그대로 열려
    /// 있다 — 팔레트가 함께 만들어진다.
    /// </para>
    /// </summary>
    public static class StageTilePlan
    {
        /// <summary>벽이 놓이는 줄. 카메라(직교 크기 5)의 맨 윗줄이다.</summary>
        public const int WallRow = 4;

        /// <summary>
        /// 바닥의 맨 아랫줄. 화면 아래 끝(-5)보다 한 줄 더 내려간다 — 딱 맞추면 반올림
        /// 한 픽셀에 화면 끝에 빈 줄이 비친다.
        /// </summary>
        public const int LowestFloorRow = -6;

        /// <summary>
        /// 가장 왼쪽 칸. 16:9 에서 보이는 범위는 ±8.9 지만 <b>더 넓은 화면비를 견디도록</b>
        /// 여유를 둔다 — 브라우저 창은 아무 비율이나 될 수 있고, 모자라면 배경이 잘려
        /// 카메라 배경색이 띠로 보인다.
        /// </summary>
        public const int FirstColumn = -12;

        /// <summary>가장 오른쪽 칸.</summary>
        public const int LastColumn = 11;

        /// <summary>벨트가 놓이는 줄. 판 자체가 반 칸 내려가 있어 이 줄의 중심이 y=0 이다.</summary>
        public const int BeltRow = 0;

        /// <summary>
        /// 초밥이 나오는 구멍의 칸. 벨트의 시작점(<c>BeltStart</c> x=-8) 바로 바깥이다.
        ///
        /// <para>
        /// <b><see cref="FirstColumn"/> 이 아니다.</b> 그쪽은 넓은 화면비를 견디려고 둔 여백이라
        /// 16:9 에서는 화면 밖이고, 거기 놓으면 구멍이 아무에게도 안 보인다 — 실제로 그렇게
        /// 놓아 보고 알았다.
        /// </para>
        /// </summary>
        public const int LeftMouthColumn = -9;

        /// <summary>초밥이 들어가는 구멍의 칸. 벨트의 끝점(<c>BeltEnd</c> x=+8) 바로 바깥이다.</summary>
        public const int RightMouthColumn = 8;

        /// <summary>창문 간격. 벽 한 줄에 네 칸마다 하나씩 뚫는다.</summary>
        private const int WindowStride = 4;

        /// <summary>창문의 위상. 끝 모서리와 겹치지 않는 값이다.</summary>
        private const int WindowPhase = 2;

        /// <summary>바닥 장식물이 놓이는 칸. 벽에 기대어 선다.</summary>
        private static readonly int[] DecorColumns = { -8, 5 };

        /// <summary>벽 바로 아래 줄. 벽과 맞닿는 바닥 타일이 여기 깔린다.</summary>
        public static int FloorEdgeRow => WallRow - 1;

        /// <summary>
        /// 칠할 칸 전부. 순서는 판 → 아래에서 위 → 왼쪽에서 오른쪽이며, 겹쳐 칠하는 칸은
        /// 없다 — 같은 판의 같은 칸이 두 번 나오면 무엇이 이겼는지 눈으로만 알 수 있다.
        /// </summary>
        public static IReadOnlyList<StageTilePlacement> Build()
        {
            var placements = new List<StageTilePlacement>();

            for (var y = LowestFloorRow; y <= FloorEdgeRow; y++)
            {
                var tile = y == FloorEdgeRow ? StageTileId.FloorWallEdge : StageTileId.Floor;
                for (var x = FirstColumn; x <= LastColumn; x++)
                {
                    placements.Add(new StageTilePlacement(StageTileLayer.Floor, x, y, tile));
                }
            }

            for (var x = FirstColumn; x <= LastColumn; x++)
            {
                placements.Add(new StageTilePlacement(StageTileLayer.Wall, x, WallRow, WallTileAt(x)));
            }

            foreach (var x in DecorColumns)
            {
                placements.Add(new StageTilePlacement(StageTileLayer.Wall, x, FloorEdgeRow,
                                                      StageTileId.Decor));
            }

            for (var x = FirstColumn; x <= LastColumn; x++)
            {
                placements.Add(new StageTilePlacement(StageTileLayer.Belt, x, BeltRow,
                                                      StageTileId.BeltHorizontal));
            }

            // 초밥은 왼쪽에서 나와 오른쪽으로 흐른다 (BeltStart x=-8 → BeltEnd x=+8).
            placements.Add(new StageTilePlacement(StageTileLayer.BeltProps, LeftMouthColumn, BeltRow,
                                                  StageTileId.BeltMouthLeft));
            placements.Add(new StageTilePlacement(StageTileLayer.BeltProps, RightMouthColumn, BeltRow,
                                                  StageTileId.BeltMouthRight));

            return placements;
        }

        /// <summary>벽 한 줄에서 이 칸에 놓일 타일.</summary>
        private static string WallTileAt(int x)
        {
            if (x == FirstColumn)
            {
                return StageTileId.WallCornerLeft;
            }

            if (x == LastColumn)
            {
                return StageTileId.WallCornerRight;
            }

            // 나머지가 0 인지만 보므로 음수 칸에서도 간격이 어긋나지 않는다. 0 이 아닌 값과
            // 비교하는 형태로 바꾸면 화면 왼쪽 절반의 창문이 오른쪽과 엇갈린다.
            return (x - WindowPhase) % WindowStride == 0 ? StageTileId.Window : StageTileId.Wall;
        }
    }
}
