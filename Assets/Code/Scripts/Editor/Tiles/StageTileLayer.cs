namespace SushiDefense.EditorTools.Tiles
{
    /// <summary>
    /// 타일이 올라가는 판. 판이 갈리는 기준은 «무엇을 그리나» 가 아니라 <b>어느 높이에
    /// 놓이나 · 무엇보다 앞에 나오나</b> 둘뿐이다.
    ///
    /// <para>
    /// 벨트 두 판이 따로 있는 이유: 구멍은 벨트 <b>위에</b> 겹쳐 그려야 하는데, 한 판에서는
    /// 한 칸에 타일이 하나뿐이라 벨트를 지워야 한다. 그러면 구멍 뒤가 뚫린다.
    /// </para>
    /// </summary>
    public enum StageTileLayer
    {
        /// <summary>바닥. 가장 뒤.</summary>
        Floor = 0,

        /// <summary>벽과 바닥 장식물.</summary>
        Wall = 1,

        /// <summary>벨트. 칸의 절반만큼 내려 y=0 에 중심이 오게 놓인다.</summary>
        Belt = 2,

        /// <summary>벨트 위에 겹치는 것 — 구멍.</summary>
        BeltProps = 3
    }
}
