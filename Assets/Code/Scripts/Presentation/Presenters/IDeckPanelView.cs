using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.UI
{
    /// <summary>
    /// 덱 화면이 프레젠터에게 제공하는 것.
    ///
    /// <para>
    /// <b><c>MonoBehaviour</c>·<c>Transform</c> 이 하나도 없다.</b> 프레젠터를 EditMode 로
    /// 검증하기 위해서다 (<c>CLAUDE.md</c> §3.6). <see cref="SushiData"/> 는
    /// <c>ScriptableObject</c> 지만 그리려면 아이콘이 필요하고, 테스트는
    /// <c>ScriptableObject.CreateInstance</c> 로 만들 수 있다
    /// (<c>.claude/rules/tests.md</c> §4) — 금지되는 것은 뷰가 씬 객체를 프레젠터에게
    /// 노출하는 쪽이다.
    /// </para>
    /// </summary>
    public interface IDeckPanelView
    {
        /// <summary>
        /// 덱을 화면에 올린다. <b>빈 목록도 온다</b> — 그때는 "덱이 비어 있음" 을 표시한다.
        /// 조용히 안 열면 덱 버튼이 고장 난 것과 구분되지 않는다.
        /// </summary>
        void ShowDeck(IReadOnlyList<SushiData> cards);

        /// <summary>화면을 내린다.</summary>
        void Hide();
    }
}
