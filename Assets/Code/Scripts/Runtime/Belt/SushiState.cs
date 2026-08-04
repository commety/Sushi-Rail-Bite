namespace SushiDefense.Belt
{
    /// <summary>
    /// 벨트 위 초밥 1개의 처리 단계.
    ///
    /// <para>
    /// <see cref="Claimed"/> 가 따로 있는 이유: 배정이 확정된 시점과 실제로 먹히는 시점 사이에
    /// 손님의 "먹는 시간"이 있다. 그 구간을 표시하지 않으면 두 손님이 같은 초밥을 먹는다
    /// (<c>.claude/domain/data-model.md</c> §1).
    /// </para>
    /// </summary>
    public enum SushiState
    {
        /// <summary>벨트 위에 있고 아직 아무에게도 배정되지 않았다.</summary>
        OnBelt = 0,

        /// <summary>어느 손님에게 배정됐고 소비되는 중이다. 다른 손님은 가져갈 수 없다.</summary>
        Claimed = 1,

        /// <summary>소비가 끝났다. 벨트에서 내려가 풀로 돌아갈 대상이다.</summary>
        Consumed = 2
    }
}
