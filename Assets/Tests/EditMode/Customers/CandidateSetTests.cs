using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 래치는 <b>이탈 시각 기준</b>이다 — "인식하고 N초" 가 아니라 "범위를 벗어나고 N초".
    ///
    /// <para>
    /// 그래서 이 테스트들은 <b>인식 시각과 이탈 시각을 일부러 멀리 벌려 둔다.</b> 둘이
    /// 가까우면 어느 기준으로 재도 같은 답이 나와, 잘못된 구현을 잡지 못한다.
    /// </para>
    /// </summary>
    public sealed class CandidateSetTests
    {
        private const float Latch = 1f;
        private const float NoLatch = 0f;

        /// <summary>
        /// stage01 의 실제 비율에서 온 값 — 범위 통과 시간 3.0초 (<c>2 × reach 3 / speed 2</c>)
        /// 에 래치 1.0초. <b>통과 시간이 래치보다 길다</b>는 이 관계가 핵심이며,
        /// 임의의 큰 래치로 짜면 옛 구현으로도 통과해 아무것도 증명하지 못한다.
        /// </summary>
        private const float ExitAt = 3f;

        [Test]
        public void Recognize_NewSushi_AddsToCandidates()
        {
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);

            var added = set.Recognize(sushi, ExitAt);

            Assert.IsTrue(added);
            Assert.AreEqual(1, set.Count);
            Assert.AreSame(sushi, set.Items[0]);
        }

        [Test]
        public void Recognize_NewSushi_StoresExitTime()
        {
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), ExitAt);

            Assert.AreEqual(ExitAt, set.ExitAtOf(0));
        }

        [Test]
        public void Recognize_SameSushiTwice_DoesNotDuplicate()
        {
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, ExitAt);

            var addedAgain = set.Recognize(sushi, 99f);

            Assert.IsFalse(addedAgain);
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void Recognize_SameSushiTwice_KeepsOriginalExitTime()
        {
            // 이탈 시각은 기하로 정해진다. 다시 계산해도 같은 값이어야 하고, 다르다면
            // 벨트 속도가 바뀌었다는 뜻이라 별개 문제다. 재인식이 값을 밀지 않는다.
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, ExitAt);

            set.Recognize(sushi, 99f);

            Assert.AreEqual(ExitAt, set.ExitAtOf(0));
        }

        [Test]
        public void Recognize_MultipleSushi_PreservesRecognitionOrder()
        {
            var set = new CandidateSet(Latch);
            var first = NewSushi(0);
            var second = NewSushi(1);
            var third = NewSushi(2);

            set.Recognize(second, ExitAt);
            set.Recognize(third, ExitAt);
            set.Recognize(first, ExitAt);

            Assert.AreSame(second, set.Items[0]);
            Assert.AreSame(third, set.Items[1]);
            Assert.AreSame(first, set.Items[2]);
        }

        [Test]
        public void Recognize_MultipleSushi_ExitTimesStayIndexAligned()
        {
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), 1f);
            set.Recognize(NewSushi(1), 2f);
            set.Recognize(NewSushi(2), 3f);

            Assert.AreEqual(1f, set.ExitAtOf(0));
            Assert.AreEqual(2f, set.ExitAtOf(1));
            Assert.AreEqual(3f, set.ExitAtOf(2));
        }

        [Test]
        public void Forget_EatenByOther_RemovesFromCandidates()
        {
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, ExitAt);

            var removed = set.Forget(sushi);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void Forget_UnknownSushi_ReturnsFalse()
        {
            var set = new CandidateSet(Latch);

            Assert.IsFalse(set.Forget(NewSushi(0)));
        }

        [Test]
        public void Forget_MiddleCandidate_KeepsRemainingOrder()
        {
            var set = new CandidateSet(Latch);
            var first = NewSushi(0);
            var second = NewSushi(1);
            var third = NewSushi(2);
            set.Recognize(first, ExitAt);
            set.Recognize(second, ExitAt);
            set.Recognize(third, ExitAt);

            set.Forget(second);

            Assert.AreSame(first, set.Items[0]);
            Assert.AreSame(third, set.Items[1]);
        }

        [Test]
        public void Forget_MiddleCandidate_KeepsRemainingExitTimesAligned()
        {
            // 두 리스트를 함께 당기지 않으면 여기서 어긋난다 — 남은 후보가 남의
            // 마감시한을 물려받아 엉뚱한 시점에 확정되거나 만료된다.
            var set = new CandidateSet(Latch);
            var first = NewSushi(0);
            var second = NewSushi(1);
            var third = NewSushi(2);
            set.Recognize(first, 1f);
            set.Recognize(second, 2f);
            set.Recognize(third, 3f);

            set.Forget(second);

            Assert.AreEqual(1f, set.ExitAtOf(0));
            Assert.AreEqual(3f, set.ExitAtOf(1));
        }

        [Test]
        public void Clear_LostEligibility_EmptiesSet()
        {
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), ExitAt);
            set.Recognize(NewSushi(1), ExitAt);

            set.Clear();

            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void Recognize_AfterClear_WorksAgain()
        {
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, ExitAt);
            set.Clear();

            Assert.IsTrue(set.Recognize(sushi, ExitAt));
        }

        // ── 래치 — 이탈 시각 기준 (M2.5 D4) ─────────────────────

        [Test]
        public void ExpirePastLatch_StillInsideReach_KeepsCandidate()
        {
            // 이 마일스톤을 하게 만든 결함이다. 인식 기준(now − 인식 > 1.0)으로 재면
            // 범위 안에 멀쩡히 있는 초밥이 t=2.0 에 후보에서 빠지고, 대역 밖 초밥을
            // 기다리게 만든 손님은 이탈 1초 전에 대상을 잃어 굶는다.
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), ExitAt);

            set.ExpirePastLatch(2f);

            Assert.AreEqual(1, set.Count, "범위 안(이탈 3.0초)인데 만료됐다");
        }

        [Test]
        public void ExpirePastLatch_JustAfterExit_KeepsCandidate()
        {
            // 래치의 본래 목적 — 이탈 직후의 계산 지연을 받쳐 준다 (§1.1-3c).
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), ExitAt);

            set.ExpirePastLatch(ExitAt + Latch - 0.01f);

            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void ExpirePastLatch_BeyondExitPlusLatch_RemovesCandidate()
        {
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), ExitAt);

            set.ExpirePastLatch(ExitAt + Latch + 0.01f);

            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void ExpirePastLatch_ZeroLatch_NeverExpires()
        {
            // 0 은 "상한 없음" 규약이다 (StageConfig.RecognitionLatchSeconds).
            // "이탈 즉시 만료" 로 해석하면 마감시한과 같은 틱에 후보가 사라진다.
            var set = new CandidateSet(NoLatch);
            set.Recognize(NewSushi(0), ExitAt);

            set.ExpirePastLatch(99999f);

            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void ExpirePastLatch_MixedExitTimes_RemovesOnlyExpired()
        {
            // 먼저 인식된 쪽이 먼저 만료되는 것이 아니다 — **먼저 나가는 쪽**이 먼저다.
            // 인식 순서와 이탈 순서를 엇갈리게 둬 기준을 못박는다.
            var set = new CandidateSet(Latch);
            var leavesLate = NewSushi(0);
            var leavesEarly = NewSushi(1);
            set.Recognize(leavesLate, 5f);
            set.Recognize(leavesEarly, 1f);

            set.ExpirePastLatch(3f);

            Assert.AreEqual(1, set.Count);
            Assert.AreSame(leavesLate, set.Items[0], "나중에 나가는 쪽이 남는다");
        }

        [Test]
        public void Recognize_SushiPastReachEdge_StaysCandidate()
        {
            // 위치는 만료 기준이 아니다. 시각으로만 판단한다.
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, ExitAt);

            sushi.BeltPosition += 9999f;
            set.ExpirePastLatch(1f);

            Assert.AreEqual(1, set.Count);
        }

        private static SushiItem NewSushi(int sequenceNumber)
        {
            return new SushiItem(null, sequenceNumber);
        }
    }
}
