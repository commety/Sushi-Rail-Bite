using SushiDefense.Data;

namespace SushiDefense.UI
{
    /// <summary>
    /// 카드에 쓸 문구를 만든다. <b>순수 계산이라 뷰 없이 검증된다.</b>
    ///
    /// <para>
    /// <i>"표시 이름이 비면 애셋 이름"</i> 이 M5 까지 세 곳에 흩어져 있었다. 카드가 뜨는
    /// 화면이 넷으로 늘면서 같은 손님이 화면마다 다르게 불릴 위험이 그만큼 커졌으므로
    /// 여기 한 벌로 모은다. <c>Runtime</c> 쪽의 <c>RewardOffer.DisplayName</c> 은 어셈블리가
    /// 달라 남아 있다.
    /// </para>
    /// <para>
    /// <b>초밥과 손님을 하나의 메서드로 합치지 않는다.</b> 타입으로 분기하는 순간 그 분기가
    /// 판정이 되고, 카드를 그리는 쪽이 무엇을 넘길지 이미 알고 있다 —
    /// <c>IStageTransitionView</c> 가 클리어와 런 종료를 나눈 것과 같은 판단이다.
    /// </para>
    /// </summary>
    public static class CardCaption
    {
        /// <summary>
        /// 카드에 쓸 초밥 이름. 표시 이름이 비어 있으면 애셋 이름으로 대신한다 —
        /// 빈 칸이 그려지면 카드가 고장 난 것처럼 보인다.
        /// </summary>
        public static string NameOf(SushiData sushi)
        {
            return sushi == null ? string.Empty : Fallback(sushi.DisplayName, sushi.name);
        }

        /// <summary>카드에 쓸 손님 이름. <see cref="NameOf(SushiData)"/> 와 같은 규칙이다.</summary>
        public static string NameOf(CustomerData customer)
        {
            return customer == null ? string.Empty : Fallback(customer.DisplayName, customer.name);
        }

        /// <summary>
        /// 초밥 카드의 수치 줄 — <b>얼마짜리인가</b>와 <b>얼마나 배부른가</b>.
        ///
        /// <para>
        /// 가격은 이 게임의 핵심 규칙(선호 대역)의 입력이고, 포화도는 한 손님이 몇 개나
        /// 먹을 수 있는지를 정한다. 둘 다 없으면 카드가 그림과 이름뿐이 된다.
        /// </para>
        /// </summary>
        public static string DetailOf(SushiData sushi)
        {
            return sushi == null ? string.Empty : $"{sushi.Price} · 포화 {sushi.SaturationAmount}";
        }

        /// <summary>
        /// 손님 카드의 수치 줄 — <b>무엇을 노리는가</b>와 <b>얼마나 드는가</b>.
        ///
        /// <para>
        /// 영입 비용을 함께 적는 것은 <c>RewardOffer.DisplayName</c> 이 이미 내린 판단이다 —
        /// 비용을 모르면 얻고 나서 예산이 모자라 못 앉히는 상황을 고르는 시점에 예측할 수 없다.
        /// </para>
        /// <para>
        /// 대역은 하한과 상한을 <b>둘 다</b> 적는다. 한쪽만 쓰면 좁은 손님과 넓은 손님이
        /// 카드에서 같아 보이는데, 폭이 배정 순위를 가르는 값이라 플레이어가 알아야 한다.
        /// </para>
        /// </summary>
        public static string DetailOf(CustomerData customer)
        {
            return customer == null
                ? string.Empty
                : $"{customer.TargetingMin}~{customer.TargetingMax} · 영입 {customer.RecruitCost}";
        }

        private static string Fallback(string displayName, string assetName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? assetName : displayName;
        }
    }
}
