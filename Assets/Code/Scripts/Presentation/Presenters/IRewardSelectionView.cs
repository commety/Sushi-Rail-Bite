using System.Collections.Generic;
using SushiDefense.Run;

namespace SushiDefense.UI
{
    /// <summary>
    /// 보상 화면이 프레젠터에게 제공하는 것.
    ///
    /// <para>
    /// <b><c>MonoBehaviour</c>·<c>Transform</c> 이 하나도 없다.</b> 프레젠터를 EditMode 로
    /// 검증하기 위해서다 (<c>CLAUDE.md</c> §3.6).
    /// </para>
    /// <para>
    /// <b>이름 문자열이 아니라 <see cref="RewardOffer"/> 를 넘긴다.</b> M5 까지는 문자열
    /// 목록이었는데, 카드에는 <b>아이콘</b>이 필요하고 아이콘은 데이터에 있다. 어느
    /// <c>Show</c> 를 부를지는 <c>Kind</c> 를 보고 <b>뷰가 아니라 부르는 쪽</b>이 정한다 —
    /// <c>IDeckPanelView</c> 와 같은 기준이다.
    /// </para>
    /// </summary>
    public interface IRewardSelectionView
    {
        /// <summary>
        /// 후보를 화면에 올린다. <b>빈 목록도 온다</b> — 그때는 "받을 보상 없음" 을 표시하고
        /// 건너뛰기만 남긴다. 그 버튼이 <b>유일한 출구</b>다.
        /// </summary>
        void ShowOffers(IReadOnlyList<RewardOffer> offers);

        /// <summary>화면을 내린다.</summary>
        void Hide();
    }
}
