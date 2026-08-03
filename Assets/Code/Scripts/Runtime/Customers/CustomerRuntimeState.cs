using SushiDefense.Data;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배치된 손님 1명의 런타임 상태. 집기 범위·타겟팅 같은 정적 값은 <see cref="Data"/> 에서
    /// 읽고, 여기에는 변하는 것만 둔다 (<c>CLAUDE.md</c> §3.1).
    ///
    /// <para>
    /// <b>여기에는 판정이 없다.</b> 자격 판정·배정·타이머 진행은 M2 의 <c>CustomerLogic</c> 이
    /// 가져간다. 이 클래스는 값 보관과 그로부터 바로 나오는 파생 값까지만 맡는다.
    /// </para>
    /// </summary>
    public sealed class CustomerRuntimeState
    {
        /// <summary>이 손님의 정적 데이터. 읽기 전용으로만 쓴다.</summary>
        public CustomerData Data { get; }

        /// <summary>배치 순서로 받은 번호. 배정 동률을 끝내는 마지막 타이브레이커다.</summary>
        public int SequenceNumber { get; }

        /// <summary>현재 상태.</summary>
        public CustomerState State { get; internal set; }

        /// <summary>지금까지 찬 포화도. <see cref="CustomerData.MaxSaturation"/> 에 닿으면 포화다.</summary>
        public int CurrentSaturation { get; internal set; }

        /// <summary>먹는 중인 초밥이 끝나기까지 남은 시간(초).</summary>
        public float RemainingEatSeconds { get; internal set; }

        /// <summary>소화가 끝나기까지 남은 시간(초).</summary>
        public float RemainingDigestSeconds { get; internal set; }

        /// <summary>
        /// 아직 더 먹을 수 있는가. <b>자격 판정의 한 축</b>이며, 나머지 두 축인 집기 범위·상태와
        /// 함께 쓰인다. 가격은 여기에 끼지 않는다 (<c>CLAUDE.md</c> §1.1-3a).
        /// </summary>
        public bool HasSaturationHeadroom => CurrentSaturation < Data.MaxSaturation;

        /// <summary>배치 시점의 정적 데이터와 순차번호로 초기화한다.</summary>
        public CustomerRuntimeState(CustomerData data, int sequenceNumber)
        {
            Data = data;
            SequenceNumber = sequenceNumber;
            State = CustomerState.Idle;
            CurrentSaturation = 0;
            RemainingEatSeconds = 0f;
            RemainingDigestSeconds = 0f;
        }
    }
}
