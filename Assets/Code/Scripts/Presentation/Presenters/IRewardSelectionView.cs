using System.Collections.Generic;

namespace SushiDefense.UI
{
    /// <summary>
    /// 보상 화면이 프레젠터에게 제공하는 것.
    ///
    /// <para>
    /// <b>Unity 타입이 하나도 없다.</b> 프레젠터를 EditMode 로 검증하기 위해서다
    /// (<c>CLAUDE.md</c> §3.6). 카드 객체가 아니라 <b>이름 문자열</b>만 넘기는 것도 같은
    /// 이유이며, 뷰가 <c>SushiData</c>·<c>CustomerData</c> 를 구분해 그릴 필요가 없어진다.
    /// </para>
    /// </summary>
    public interface IRewardSelectionView
    {
        /// <summary>
        /// 후보를 화면에 올린다. <b>빈 목록도 온다</b> — 그때는 "받을 보상 없음" 을 표시한다.
        /// </summary>
        void ShowOffers(IReadOnlyList<string> offerNames);

        /// <summary>화면을 내린다.</summary>
        void Hide();
    }
}
