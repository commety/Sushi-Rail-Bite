namespace SushiDefense.Data
{
    /// <summary>
    /// 초밥 1개가 소비됐을 때 채널에 실리는 값.
    ///
    /// <b>참조 타입을 싣지 않는다.</b> <c>SushiItem</c> 이나 <c>CustomerRuntimeState</c> 를 실으면
    /// <c>Runtime.Data</c> 가 <c>Runtime</c> 을 역참조하게 되어 어셈블리 그래프가 깨진다
    /// (<c>design/m0-foundation/README.md</c> D2). 필요한 값만 원시 타입으로 복사해 넘긴다.
    /// </summary>
    public readonly struct SushiEatenPayload
    {
        /// <summary>먹힌 초밥의 가격. 매출에 그대로 더해진다.</summary>
        public int Price { get; }

        /// <summary>이 초밥이 손님의 포화도를 채운 양.</summary>
        public int SaturationAmount { get; }

        /// <summary>먹힌 초밥의 순차번호. 같은 초밥에 대한 중복 처리를 걸러낼 때 쓴다.</summary>
        public int SushiSequenceNumber { get; }

        /// <summary>먹은 손님의 순차번호.</summary>
        public int CustomerSequenceNumber { get; }

        /// <summary>소비 사실을 구성하는 네 값을 묶는다.</summary>
        public SushiEatenPayload(int price, int saturationAmount,
                                 int sushiSequenceNumber, int customerSequenceNumber)
        {
            Price = price;
            SaturationAmount = saturationAmount;
            SushiSequenceNumber = sushiSequenceNumber;
            CustomerSequenceNumber = customerSequenceNumber;
        }
    }
}
