using System;
using SushiDefense.Run;

namespace SushiDefense.UI
{
    /// <summary>
    /// 지금 초밥 덱에 무엇이 있는지 보여 준다. <b>Unity API 를 모른다</b> — 뷰는 인터페이스로만
    /// 본다 (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 보상으로 얻은 초밥이 화면 어디에도 없었다. 벨트에 언젠가 흘러가긴 하지만 지금 덱에
    /// 무엇이 있는지 확인할 방법이 없어, <b>로그라이트의 축적감이 통째로 안 보였다.</b>
    /// </para>
    /// <para>
    /// <b>읽기 전용이다.</b> 덱 편집은 기획이 없다 — 카드는 보상으로만 늘고 빼는 경로가
    /// 없다 (<c>SushiDeck</c> 의 클래스 주석).
    /// </para>
    /// <para>
    /// <b>일시정지를 건드리지 않는다.</b> 덱 확인은 정보 조회이고 멈추는 것은 메뉴의 일이다.
    /// 둘을 묶으면 "덱을 열면 왜 시간이 멈추지" 와 "일시정지하려고 덱을 연다" 가 동시에
    /// 생기고, 한 화면이 두 가지를 하면 나중에 한쪽만 고쳐진다.
    /// </para>
    /// </summary>
    public sealed class DeckPanelPresenter
    {
        private readonly IDeckPanelView _view;

        /// <summary>
        /// 이 런의 덱. <b>스테이지 설정이 아니다</b> — 스폰 구성을 읽으면 보상으로 얻은
        /// 카드가 영영 안 보인다. 덱의 진실은 런에 있다 (M3).
        /// </summary>
        private readonly RunState _run;

        /// <summary>화면이 떠 있나.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>지금 보여 주고 있는 카드 수. 닫혀 있으면 0 이다.</summary>
        public int CardCount { get; private set; }

        /// <summary>
        /// 런을 생성자에서 받는다. 덱 버튼은 인자를 넘길 방법이 없고, 뷰에 런을 들려 주면
        /// <b>화면이 런 상태를 알게 되어</b> 인터페이스가 Unity 밖 타입까지 끌고 온다.
        /// 프레젠터의 수명은 런과 같다 — <c>Rewards</c>·<c>Transition</c> 과 같은 자리다.
        /// </summary>
        public DeckPanelPresenter(IDeckPanelView view, RunState run)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        /// <summary>덱을 연다. <b>빈 덱이어도 연다.</b></summary>
        public void Open()
        {
            var cards = _run.Sushi.Cards;

            IsOpen = true;
            CardCount = cards.Count;
            _view.ShowDeck(cards);
        }

        /// <summary>
        /// 덱을 닫는다. 이미 닫혀 있으면 아무 일도 하지 않는다 — 닫힌 화면을 또 닫으면
        /// 뷰가 같은 일을 두 번 한다.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            CardCount = 0;
            _view.Hide();
        }

        /// <summary>덱 버튼이 부른다. 열려 있으면 닫고 닫혀 있으면 연다.</summary>
        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }
    }
}
