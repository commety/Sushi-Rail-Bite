using SushiDefense.Data;

namespace SushiDefense.Run
{
    /// <summary>
    /// 제시된 보상 한 장. <b>둘 중 하나만 채워진다</b> — <see cref="Kind"/> 가 어느 쪽인지
    /// 말하므로 화면이 분기 없이 그릴 수 있다.
    ///
    /// <para>
    /// <c>readonly struct</c> 인 이유: 화면과 프레젠터가 들고 다니는 값이라, 변경 가능하면
    /// 어디서 바뀌었는지 추적할 수 없다.
    /// </para>
    /// </summary>
    public readonly struct RewardOffer
    {
        /// <summary>이 보상이 초밥인가 손님인가.</summary>
        public RewardKind Kind { get; }

        /// <summary>초밥 보상일 때의 카드. 손님 보상이면 <c>null</c>.</summary>
        public SushiData Sushi { get; }

        /// <summary>손님 보상일 때의 손님. 초밥 보상이면 <c>null</c>.</summary>
        public CustomerData Customer { get; }

        private RewardOffer(RewardKind kind, SushiData sushi, CustomerData customer)
        {
            Kind = kind;
            Sushi = sushi;
            Customer = customer;
        }

        /// <summary>초밥 카드 보상을 만든다.</summary>
        public static RewardOffer OfSushi(SushiData sushi)
        {
            return new RewardOffer(RewardKind.SushiCard, sushi, null);
        }

        /// <summary>손님 영입 보상을 만든다.</summary>
        public static RewardOffer OfCustomer(CustomerData customer)
        {
            return new RewardOffer(RewardKind.Customer, null, customer);
        }

        /// <summary>
        /// 화면에 띄울 이름. 카드의 표시 이름이며, <b>비어 있으면 애셋 이름으로 대신한다</b> —
        /// 아직 이름을 안 채운 애셋에서 빈 칸이 그려지면 보상이 없는 것처럼 보인다.
        ///
        /// <para>
        /// 손님 보상에는 <b>영입 비용을 함께 적는다.</b> 유형마다 비용이 다르고(기본 20 ·
        /// 소식 40 · 먹보 60), 그 값을 모르면 "얻고 나서 예산이 모자라 못 앉히는" 상황을
        /// 고르는 시점에 예측할 수 없다. 초밥에는 붙이지 않는다 — 덱에 들어갈 뿐 비용이 없다.
        /// </para>
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (Kind == RewardKind.SushiCard)
                {
                    return Sushi == null ? string.Empty : NameOf(Sushi.DisplayName, Sushi.name);
                }

                if (Customer == null)
                {
                    return string.Empty;
                }

                return $"{NameOf(Customer.DisplayName, Customer.name)} (영입 {Customer.RecruitCost})";
            }
        }

        private static string NameOf(string displayName, string assetName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? assetName : displayName;
        }
    }
}
