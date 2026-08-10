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
        /// 카드 우상단 동전에 들어갈 값. <b>손님만 비용이 있다.</b>
        ///
        /// <para>
        /// 초밥 쪽이 빈 문자열을 돌려주는 것은 실수가 아니라 계약이다 — 부르는 쪽이 그것으로
        /// 동전을 끈다. 여기서 <c>"0"</c> 을 돌려주면 초밥 카드에 공짜라는 값이 붙는다.
        /// </para>
        /// </summary>
        public static string CostOf(CustomerData customer)
        {
            return customer == null ? string.Empty : customer.RecruitCost.ToString();
        }

        /// <inheritdoc cref="CostOf(CustomerData)" />
        public static string CostOf(SushiData sushi)
        {
            return string.Empty;
        }

        /// <summary>
        /// 초밥 카드의 수치 <b>행</b> — <b>얼마짜리인가</b>와 <b>얼마나 배부른가</b>.
        ///
        /// <para>
        /// 가격은 이 게임의 핵심 규칙(선호 대역)의 입력이고, 포화도는 한 손님이 몇 개나
        /// 먹을 수 있는지를 정한다. 둘 다 없으면 카드가 그림과 이름뿐이 된다.
        /// </para>
        /// <para>
        /// <b>손님보다 행이 적다.</b> <c>SushiData</c> 가 가진 것이 둘뿐이라 데이터에서 이미
        /// 확정된 차이이며, 줄 수를 맞추려 하면 빈 행이 생긴다.
        /// </para>
        /// </summary>
        public static string DetailOf(SushiData sushi)
        {
            return sushi == null
                ? string.Empty
                : $"가격 {sushi.Price}\n포화 {sushi.SaturationAmount}";
        }

        /// <summary>
        /// 손님 카드의 수치 <b>행</b> — 배치 결정을 좌우하는 값들.
        ///
        /// <para>
        /// 대역은 하한과 상한을 <b>둘 다</b> 적는다. 한쪽만 쓰면 좁은 손님과 넓은 손님이
        /// 카드에서 같아 보이는데, 폭이 배정 순위를 가르는 값이라 플레이어가 알아야 한다.
        /// </para>
        /// <para>
        /// <b>영입 비용은 여기 없다.</b> 카드 우상단 동전이 지므로(<see cref="CostOf(CustomerData)"/>)
        /// 여기 또 적으면 같은 값이 카드에 두 번 나온다.
        /// </para>
        /// </summary>
        public static string DetailOf(CustomerData customer)
        {
            return customer == null
                ? string.Empty
                : $"범위 {customer.Reach}\n"
                  + $"대역 {customer.TargetingMin}~{customer.TargetingMax}\n"
                  + $"포화도 {customer.MaxSaturation}\n"
                  + $"소화 {customer.DigestSeconds}초\n"
                  + $"인구수 {customer.Population}";
        }

        private static string Fallback(string displayName, string assetName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? assetName : displayName;
        }
    }
}
