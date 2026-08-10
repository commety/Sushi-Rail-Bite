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

        // ── 가려짐 ─────────────────────────────────────────────

        /// <summary>
        /// 혼자 떠 있는 창은 가려지지 않는다. <b>반례를 함께 박는다</b> — 항상 <c>false</c> 를
        /// 돌려주는 구현도 이것만으로는 통과한다.
        /// </summary>
        // ── 설정은 남의 위에 겹친다 (M7) ─────────────────────────────────

        [Test]
        public void TryOpen_SettingsWhileMenuIsOpen_KeepsTheMenu()
        {
            // 메뉴에서 여는 화면인데 메뉴를 밀어내면 닫았을 때 돌아갈 곳이 없다.
            _arbiter.TryOpen(StageWindow.Menu);

            Assert.IsTrue(_arbiter.TryOpen(StageWindow.Settings));
            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Menu));
            CollectionAssert.IsEmpty(_closed, "메뉴에 닫으라는 요청이 갔다");
        }

        [Test]
        public void TryOpen_SettingsWhileMenuIsOpen_CoversTheMenu()
        {
            // 겹치기만 하고 밑을 못 누르게 막지 않으면, 설정 뒤의 «나가기» 가 그대로 눌린다.
            _arbiter.TryOpen(StageWindow.Menu);

            _arbiter.TryOpen(StageWindow.Settings);

            Assert.IsTrue(_arbiter.IsCovered(StageWindow.Menu));
            Assert.IsFalse(_arbiter.IsCovered(StageWindow.Settings));
        }

        [Test]
        public void Close_Settings_RevealsTheMenuAgain()
        {
            _arbiter.TryOpen(StageWindow.Menu);
            _arbiter.TryOpen(StageWindow.Settings);

            _arbiter.Close(StageWindow.Settings);

            Assert.IsTrue(_arbiter.IsOpen(StageWindow.Menu));
            Assert.IsFalse(_arbiter.IsCovered(StageWindow.Menu), "메뉴가 가려진 채로 남았다");
        }

        [Test]
        public void TryOpen_DeckWhileSettingsIsOpen_IsRefused()
        {
            // 겹치는 창이라고 아무나 위로 올려 주는 것은 아니다. 설정이 가장 위다.
            _arbiter.TryOpen(StageWindow.Settings);

            Assert.IsFalse(_arbiter.TryOpen(StageWindow.Deck));
        }

        [Test]
        public void TryOpen_SettingsOverRewardAndDeck_LeavesBothOpen()
        {
            // 겹침 예외가 둘(보상 밑 · 설정 위)이라 동시에 셋까지 뜬다. 그 이상은 없다.
            _arbiter.TryOpen(StageWindow.Reward);
            _arbiter.TryOpen(StageWindow.Deck);

            _arbiter.TryOpen(StageWindow.Settings);

            Assert.AreEqual(3, _arbiter.OpenCount);
            Assert.IsTrue(_arbiter.IsCovered(StageWindow.Reward));
            Assert.IsTrue(_arbiter.IsCovered(StageWindow.Deck));
            Assert.IsFalse(_arbiter.IsCovered(StageWindow.Settings));
        }

        [Test]
        public void IsCovered_OnlyWindow_ReturnsFalse()
        {
            _arbiter.TryOpen(StageWindow.Reward);

            Assert.IsFalse(_arbiter.IsCovered(StageWindow.Reward));

            _arbiter.TryOpen(StageWindow.Menu);

            Assert.IsTrue(_arbiter.IsCovered(StageWindow.Reward), "위에 창이 떴는데 안 가려졌다");
        }

        /// <summary>
        /// 위에 뜬 창 자신은 가려지지 않는다 — 조작 대상은 <b>가장 위 하나</b>다.
        /// </summary>
        [Test]
        public void IsCovered_TopWindow_ReturnsFalse()
        {
            _arbiter.TryOpen(StageWindow.Reward);
            _arbiter.TryOpen(StageWindow.Menu);

            Assert.IsFalse(_arbiter.IsCovered(StageWindow.Menu));
        }

        /// <summary>
        /// 위의 창이 닫히면 <b>다시 드러난다.</b> 이 경로가 없으면 보상 창이 가려진 채 남아
        /// 영영 눌리지 않는다 — 닫을 방법이 그 창의 버튼뿐이라 런이 멈춘다.
        /// </summary>
        [Test]
        public void IsCovered_AfterTopCloses_ReturnsFalseAgain()
        {
            _arbiter.TryOpen(StageWindow.Reward);
            _arbiter.TryOpen(StageWindow.Menu);
            _arbiter.Close(StageWindow.Menu);

            Assert.IsFalse(_arbiter.IsCovered(StageWindow.Reward));
        }

        /// <summary>
        /// 바뀔 때만 알린다. <b>값과 순서를 함께 본다</b> — «두 번 왔다» 만 세면 두 번 다
        /// <c>true</c> 인 구현도 통과한다.
        /// </summary>
        [Test]
        public void CoverageChanged_CoveredThenRevealed_ReportsBoth()
        {
            var log = new List<(StageWindow Window, bool Covered)>();
            _arbiter.CoverageChanged += (w, c) => log.Add((w, c));

            _arbiter.TryOpen(StageWindow.Reward);
            Assert.IsEmpty(log, "혼자 떴을 뿐인데 알림이 갔다");

            _arbiter.TryOpen(StageWindow.Menu);
            _arbiter.Close(StageWindow.Menu);

            Assert.AreEqual(new[] { (StageWindow.Reward, true), (StageWindow.Reward, false) }, log);
        }

        /// <summary>
        /// 같은 상태로는 다시 알리지 않는다. 이미 떠 있는 창을 다시 열어 보는 것은 흔한
        /// 경로이고, 그때마다 알리면 받는 쪽이 매번 <c>CanvasGroup</c> 을 건드린다.
        /// </summary>
        [Test]
        public void CoverageChanged_ReopeningTheSameTop_DoesNotRepeat()
        {
            var log = new List<(StageWindow Window, bool Covered)>();
            _arbiter.TryOpen(StageWindow.Reward);
            _arbiter.TryOpen(StageWindow.Menu);
            _arbiter.CoverageChanged += (w, c) => log.Add((w, c));

            _arbiter.TryOpen(StageWindow.Menu);

            Assert.IsEmpty(log);
        }
    }
}
