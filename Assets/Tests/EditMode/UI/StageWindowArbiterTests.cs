using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 창이 겹쳐 뜨지 않게 하는 규칙. 덱 화면을 열어 둔 채 메뉴 버튼을 누르면 두 패널이
    /// 겹쳐 글자가 서로를 뚫고 나왔다.
    ///
    /// <para>
    /// <b>«둘이 같다» 만 확인하지 않는다</b> (<c>.claude/rules/tests.md</c> §3). 우선순위를
    /// 보는 테스트는 <b>낮은 것을 먼저 열고 높은 것으로 덮는 순서</b>와 그 반대를 모두 두어,
    /// «나중에 연 것이 이긴다» 로 구현해도 통과하지 않게 한다.
    /// </para>
    /// </summary>
    public sealed class StageWindowArbiterTests
    {
        private StageWindowArbiter _arbiter;
        private List<StageWindow> _closed;

        [SetUp]
        public void SetUp()
        {
            _arbiter = new StageWindowArbiter();
            _closed = new List<StageWindow>();
            _arbiter.CloseRequested += w => _closed.Add(w);
        }

        [Test]
        public void TryOpen_Fresh_Opens()
        {
            Assert.IsTrue(_arbiter.TryOpen(StageWindow.Deck));

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Deck));
            Assert.AreEqual(1, _arbiter.OpenCount);
        }

        [Test]
        public void TryOpen_Twice_DoesNotEvictItself()
        {
            _arbiter.TryOpen(StageWindow.Deck);

            Assert.IsTrue(_arbiter.TryOpen(StageWindow.Deck));

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Deck));
            CollectionAssert.IsEmpty(_closed);
        }

        /// <summary>덱을 열어 둔 채 메뉴를 누르는, 리포트에 실제로 적힌 경로다.</summary>
        [Test]
        public void TryOpen_MenuWhileDeckOpen_ClosesDeck()
        {
            _arbiter.TryOpen(StageWindow.Deck);

            Assert.IsTrue(_arbiter.TryOpen(StageWindow.Menu));

            Assert.IsFalse(_arbiter.IsOpen(StageWindow.Deck));
            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Menu));
            CollectionAssert.AreEqual(new[] { StageWindow.Deck }, _closed);
        }

        /// <summary>
        /// 반대 순서. 이것이 없으면 «나중에 연 것이 이긴다» 는 구현이 위 테스트를 통과한다.
        /// 낮은 창은 <b>높은 창을 밀어내지 못하고 자기가 물러난다.</b>
        /// </summary>
        [Test]
        public void TryOpen_DeckWhileMenuOpen_Refused()
        {
            _arbiter.TryOpen(StageWindow.Menu);

            Assert.IsFalse(_arbiter.TryOpen(StageWindow.Deck));

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Menu));
            Assert.IsFalse(_arbiter.IsOpen(StageWindow.Deck));
            CollectionAssert.IsEmpty(_closed);
        }

        [Test]
        public void TryOpen_CustomerInfoWhileAnythingOpen_Refused()
        {
            _arbiter.TryOpen(StageWindow.Reward);

            Assert.IsFalse(_arbiter.TryOpen(StageWindow.CustomerInfo));

            Assert.IsFalse(_arbiter.IsOpen(StageWindow.CustomerInfo));
            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Reward),
                          "가장 아래 창이 남을 밀어내면 우선순위가 뒤집힌다");
        }

        [Test]
        public void TryOpen_CustomerInfoAlone_Opens()
        {
            Assert.IsTrue(_arbiter.TryOpen(StageWindow.CustomerInfo));

            Assert.AreEqual(1, _arbiter.OpenCount);
        }

        /// <summary>보상 위에는 <b>딱 하나</b> 겹칠 수 있다 — 그것이 유일한 예외다.</summary>
        [Test]
        public void TryOpen_DeckWhileRewardOpen_BothStayOpen()
        {
            _arbiter.TryOpen(StageWindow.Reward);

            Assert.IsTrue(_arbiter.TryOpen(StageWindow.Deck));

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Reward));
            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Deck));
            Assert.AreEqual(2, _arbiter.OpenCount);
            CollectionAssert.IsEmpty(_closed);
        }

        /// <summary>
        /// 겹침은 <b>하나까지</b>다. 보상 위에 덱이 떠 있을 때 메뉴를 열면 덱만 물러나고
        /// 보상은 남는다 — 셋이 동시에 뜨지 않는다.
        /// </summary>
        [Test]
        public void TryOpen_MenuOverRewardAndDeck_KeepsRewardDropsDeck()
        {
            _arbiter.TryOpen(StageWindow.Reward);
            _arbiter.TryOpen(StageWindow.Deck);

            Assert.IsTrue(_arbiter.TryOpen(StageWindow.Menu));

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Reward));
            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Menu));
            Assert.AreEqual(2, _arbiter.OpenCount);
            CollectionAssert.AreEqual(new[] { StageWindow.Deck }, _closed);
        }

        /// <summary>
        /// 보상은 <b>거절당하지 않는다.</b> 우선순위로만 따지면 덱보다 아래라 막히는데,
        /// 보상은 «닫으면 다음 판» 이라 나중에 다시 열 방법이 없다 — 밑으로 깔린다.
        /// </summary>
        [Test]
        public void TryOpen_RewardWhileHigherWindowOpen_StillOpensBeneath()
        {
            _arbiter.TryOpen(StageWindow.Deck);

            Assert.IsTrue(_arbiter.TryOpen(StageWindow.Reward));

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Reward));
            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Deck));
            CollectionAssert.IsEmpty(_closed);
        }

        /// <summary>
        /// 알림을 받은 쪽은 자기 <c>Close</c> 를 부른다. 그 호출이 방금 열린 창을 지우면
        /// 화면이 통째로 사라진다 — 목록에서 <b>먼저 빼고</b> 알리는 이유다.
        /// </summary>
        [Test]
        public void CloseRequested_HandlerClosesItself_DoesNotDisturbNewWindow()
        {
            _arbiter.CloseRequested += w => _arbiter.Close(w);
            _arbiter.TryOpen(StageWindow.Deck);

            _arbiter.TryOpen(StageWindow.Menu);

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Menu));
            Assert.AreEqual(1, _arbiter.OpenCount);
        }

        [Test]
        public void Close_NotOpen_DoesNothing()
        {
            _arbiter.TryOpen(StageWindow.Menu);

            _arbiter.Close(StageWindow.Deck);

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Menu));
            Assert.AreEqual(1, _arbiter.OpenCount);
        }

        [Test]
        public void Close_ThenLowerWindow_CanOpen()
        {
            _arbiter.TryOpen(StageWindow.Menu);
            _arbiter.Close(StageWindow.Menu);

            Assert.IsTrue(_arbiter.TryOpen(StageWindow.CustomerInfo));
        }
    }
}
