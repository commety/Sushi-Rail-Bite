namespace SushiDefense.EditorTools.Tiles
{
    /// <summary>
    /// 타일 애셋의 이름. 원본 파일 이름(<c>map-tile-3</c>)이 아니라 <b>쓰임</b>으로 부른다 —
    /// 번호로 부르면 배치 계획을 읽을 때 어느 것이 벽이고 어느 것이 바닥인지 알 수 없다.
    ///
    /// <para>
    /// 이 이름이 곧 <c>Assets/Level/Tiles/</c> 의 애셋 파일 이름이다. 한쪽만 고치면 배치
    /// 도구가 타일을 못 찾고, 그 증상은 «칠했는데 아무것도 안 보인다» 로 나타난다.
    /// </para>
    /// </summary>
    public static class StageTileId
    {
        /// <summary>바닥.</summary>
        public const string Floor = "Floor";

        /// <summary>벽과 맞닿는 바닥 줄. 벽 아래 한 줄에만 깔린다.</summary>
        public const string FloorWallEdge = "FloorWallEdge";

        /// <summary>벽.</summary>
        public const string Wall = "Wall";

        /// <summary>벽의 왼쪽 끝.</summary>
        public const string WallCornerLeft = "WallCornerLeft";

        /// <summary>벽의 오른쪽 끝.</summary>
        public const string WallCornerRight = "WallCornerRight";

        /// <summary>벽에 뚫린 창문.</summary>
        public const string Window = "Window";

        /// <summary>바닥에 놓이는 장식물.</summary>
        public const string Decor = "Decor";

        /// <summary>가로로 흐르는 벨트.</summary>
        public const string BeltHorizontal = "BeltHorizontal";

        /// <summary>세로로 흐르는 벨트. 지금 스테이지에는 쓰이지 않는다.</summary>
        public const string BeltVertical = "BeltVertical";

        /// <summary>초밥이 나오는 구멍. 벨트의 시작 쪽에 놓인다.</summary>
        public const string BeltMouthLeft = "BeltMouthLeft";

        /// <summary>초밥이 들어가는 구멍. 벨트의 끝 쪽에 놓인다.</summary>
        public const string BeltMouthRight = "BeltMouthRight";
    }
}
