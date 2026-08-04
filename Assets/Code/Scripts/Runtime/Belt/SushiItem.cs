using SushiDefense.Data;

namespace SushiDefense.Belt
{
    /// <summary>
    /// 벨트 위 초밥 1개의 런타임 상태. 가격·포화도 같은 정적 값은 <see cref="Data"/> 에서 읽고,
    /// 여기에는 변하는 것만 둔다 (<c>CLAUDE.md</c> §3.1).
    /// </summary>
    public sealed class SushiItem
    {
        /// <summary>배정된 손님이 없음을 나타내는 순차번호.</summary>
        public const int NoCustomer = -1;

        /// <summary>이 초밥의 정적 데이터. 읽기 전용으로만 쓴다.</summary>
        public SushiData Data { get; private set; }

        /// <summary>스폰 순서로 받은 번호. 배정 동률을 끝내는 타이브레이커다.</summary>
        public int SequenceNumber { get; private set; }

        /// <summary>현재 처리 단계.</summary>
        public SushiState State { get; private set; }

        /// <summary>이 초밥을 가져간 손님의 순차번호. 배정 전에는 <see cref="NoCustomer"/>.</summary>
        public int ClaimedByCustomerSequenceNumber { get; private set; }

        /// <summary>벨트 위 진행도. 이동 자체는 벨트(M1)가 갱신한다.</summary>
        public float BeltPosition { get; set; }

        /// <summary>스폰 시점의 정적 데이터와 순차번호로 초기화한다.</summary>
        public SushiItem(SushiData data, int sequenceNumber)
        {
            ResetForReuse(data, sequenceNumber);
        }

        /// <summary>
        /// 이 초밥을 특정 손님에게 배정한다.
        ///
        /// <para>
        /// 이미 배정됐거나 소비된 초밥은 <c>false</c> 를 돌려주고 상태를 바꾸지 않는다 —
        /// 이 방어가 없으면 배정 확정과 실제 소비 사이의 "먹는 시간" 동안 두 번째 손님이
        /// 같은 초밥을 가져간다.
        /// </para>
        /// </summary>
        public bool TryClaim(int customerSequenceNumber)
        {
            if (State != SushiState.OnBelt)
            {
                return false;
            }

            State = SushiState.Claimed;
            ClaimedByCustomerSequenceNumber = customerSequenceNumber;
            return true;
        }

        /// <summary>배정된 초밥의 소비를 확정한다. 배정되지 않은 초밥에는 실패한다.</summary>
        public bool TryConsume()
        {
            if (State != SushiState.Claimed)
            {
                return false;
            }

            State = SushiState.Consumed;
            return true;
        }

        /// <summary>
        /// 풀에서 다시 대여될 때 새 초밥으로 초기화한다 (<c>SushiPool</c>, step-05).
        /// 이전 대여의 배정·진행도가 남아 있으면 안 된다.
        /// </summary>
        public void ResetForReuse(SushiData data, int sequenceNumber)
        {
            Data = data;
            SequenceNumber = sequenceNumber;
            State = SushiState.OnBelt;
            ClaimedByCustomerSequenceNumber = NoCustomer;
            BeltPosition = 0f;
        }
    }
}
