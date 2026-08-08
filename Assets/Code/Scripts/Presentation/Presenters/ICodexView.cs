using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.UI
{
    /// <summary>
    /// 백과사전 화면이 프레젠터에게 제공하는 것.
    ///
    /// <para>
    /// <b><c>MonoBehaviour</c>·<c>Transform</c> 이 하나도 없다.</b> 프레젠터를 EditMode 로
    /// 검증하기 위해서다 — <see cref="SushiData"/> 가 <c>ScriptableObject</c> 인 것은
    /// <see cref="IDeckPanelView"/> 가 이미 내린 판단과 같다.
    /// </para>
    /// <para>
    /// <b>초밥과 손님을 따로 받는다.</b> 하나로 합치면 카드를 그릴 때 무엇인지 다시
    /// 알아내야 하고, 그 판정이 화면으로 새어 들어간다.
    /// </para>
    /// </summary>
    public interface ICodexView
    {
        /// <summary>
        /// 사전을 화면에 올린다. <b>빈 목록도 온다</b> — 조용히 안 열면 사전 버튼이 고장 난
        /// 것과 구분되지 않는다.
        /// </summary>
        void ShowEntries(IReadOnlyList<SushiData> sushi, IReadOnlyList<CustomerData> customers);

        /// <summary>화면을 내린다.</summary>
        void Hide();
    }
}
