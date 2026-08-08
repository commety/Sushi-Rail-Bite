using System;
using SushiDefense.Data;

namespace SushiDefense.UI
{
    /// <summary>
    /// 백과사전의 로직 — 이 게임에 <b>존재하는</b> 카드를 늘어놓는다. Unity API 를 모른다
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// <b>읽는 곳은 <see cref="CardCatalog"/> 하나다.</b> 보상 추첨이 보는 목록은 이것이
    /// 아니다 — 저쪽은 <i>보상으로 줄 수 있는</i> 카드라 시작 덱의 초밥이 빠져 있고,
    /// 그것으로 사전을 그리면 처음부터 갖고 있던 카드가 사전에서 사라진다.
    /// </para>
    /// <para>
    /// <b>소유 여부로 가리지 않는다.</b> 사전은 무엇이 있는지 알려 주는 화면이고, 미획득
    /// 카드를 숨기면 사전이 아니다 — 그래서 런 상태를 아예 받지 않는다.
    /// </para>
    /// </summary>
    public sealed class CodexPresenter
    {
        private readonly ICodexView _view;
        private readonly CardCatalog _catalog;

        /// <summary>화면이 떠 있나.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 지금 보여 주고 있는 항목 수. <b>닫혀 있으면 0 이다</b> —
        /// <c>DeckPanelPresenter.CardCount</c> 와 같은 계약이다.
        /// </summary>
        public int EntryCount { get; private set; }

        public CodexPresenter(ICodexView view, CardCatalog catalog)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>사전을 연다. <b>목록이 비어 있어도 연다.</b></summary>
        public void Open()
        {
            var sushi = _catalog.AllSushi;
            var customers = _catalog.AllCustomers;

            IsOpen = true;
            EntryCount = sushi.Count + customers.Count;
            _view.ShowEntries(sushi, customers);
        }

        /// <summary>
        /// 사전을 닫는다. 이미 닫혀 있으면 아무 일도 하지 않는다 — 닫힌 화면을 또 닫으면
        /// 뷰가 같은 일을 두 번 한다.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            EntryCount = 0;
            _view.Hide();
        }
    }
}
