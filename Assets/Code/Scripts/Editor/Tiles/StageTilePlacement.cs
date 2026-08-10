namespace SushiDefense.EditorTools.Tiles
{
    /// <summary>타일 한 칸 — 어느 판의, 어느 좌표에, 무엇을.</summary>
    public readonly struct StageTilePlacement
    {
        public StageTilePlacement(StageTileLayer layer, int x, int y, string tileId)
        {
            Layer = layer;
            X = x;
            Y = y;
            TileId = tileId;
        }

        /// <summary>올라가는 판.</summary>
        public StageTileLayer Layer { get; }

        /// <summary>칸의 가로 좌표. 칸 하나가 1 유닛이다.</summary>
        public int X { get; }

        /// <summary>칸의 세로 좌표.</summary>
        public int Y { get; }

        /// <summary>놓을 타일. <see cref="StageTileId"/> 의 이름 중 하나다.</summary>
        public string TileId { get; }
    }
}
