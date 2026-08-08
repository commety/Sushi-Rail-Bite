using System.Collections.Generic;
using SushiDefense.Data;
using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 덱 화면의 손으로 쓴 스텁. 프레젠터가 <b>무엇을 넘겼는지</b>만 기록한다.
    ///
    /// <para>
    /// 목록을 <b>복사해</b> 둔다 — 프레젠터가 버퍼를 재사용하면 나중에 읽었을 때 내용이
    /// 이미 바뀌어 있어, 통과해야 할 테스트가 조용히 통과하거나 조용히 실패한다.
    /// </para>
    /// </summary>
    internal sealed class FakeDeckPanelView : IDeckPanelView
    {
        public int ShowCount { get; private set; }

        public int HideCount { get; private set; }

        public List<SushiData> LastShown { get; private set; }

        public void ShowDeck(IReadOnlyList<SushiData> cards)
        {
            ShowCount++;
            LastShown = new List<SushiData>(cards);
        }

        public void Hide()
        {
            HideCount++;
        }
    }
}
