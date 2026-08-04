using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;

namespace SushiDefense.Tests.EditMode.Customers
{
    public sealed class CandidateSetTests
    {
        private const float Latch = 5f;
        private const float NoLatch = 0f;

        [Test]
        public void Recognize_NewSushi_AddsToCandidates()
        {
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);

            var added = set.Recognize(sushi, 0f);

            Assert.IsTrue(added);
            Assert.AreEqual(1, set.Count);
            Assert.AreSame(sushi, set.Items[0]);
        }

        [Test]
        public void Recognize_SameSushiTwice_DoesNotDuplicate()
        {
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, 0f);

            var addedAgain = set.Recognize(sushi, 1f);

            Assert.IsFalse(addedAgain);
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void Recognize_SameSushiTwice_KeepsOriginalRecognitionTime()
        {
            // 재인식으로 래치가 연장되면 범위 안에 머무는 동안 상한이 무의미해진다.
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, 0f);

            set.Recognize(sushi, 4f);
            set.ExpireOlderThan(6f);

            Assert.AreEqual(0, set.Count, "최초 인식 시각 기준으로 만료돼야 한다");
        }

        [Test]
        public void Recognize_MultipleSushi_PreservesRecognitionOrder()
        {
            var set = new CandidateSet(Latch);
            var first = NewSushi(0);
            var second = NewSushi(1);
            var third = NewSushi(2);

            set.Recognize(second, 0f);
            set.Recognize(third, 1f);
            set.Recognize(first, 2f);

            Assert.AreSame(second, set.Items[0]);
            Assert.AreSame(third, set.Items[1]);
            Assert.AreSame(first, set.Items[2]);
        }

        [Test]
        public void Forget_EatenByOther_RemovesFromCandidates()
        {
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, 0f);

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
            set.Recognize(first, 0f);
            set.Recognize(second, 0f);
            set.Recognize(third, 0f);

            set.Forget(second);

            Assert.AreSame(first, set.Items[0]);
            Assert.AreSame(third, set.Items[1]);
        }

        [Test]
        public void Clear_LostEligibility_EmptiesSet()
        {
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), 0f);
            set.Recognize(NewSushi(1), 0f);

            set.Clear();

            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void Recognize_AfterClear_WorksAgain()
        {
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, 0f);
            set.Clear();

            Assert.IsTrue(set.Recognize(sushi, 1f));
        }

        // ── 래치 시간 상한 (Q9) ──────────────────────────────────

        [Test]
        public void ExpireOlderThan_WithinLatchWindow_KeepsCandidate()
        {
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), 0f);

            set.ExpireOlderThan(Latch - 0.01f);

            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void ExpireOlderThan_BeyondLatchWindow_RemovesCandidate()
        {
            var set = new CandidateSet(Latch);
            set.Recognize(NewSushi(0), 0f);

            set.ExpireOlderThan(Latch + 0.01f);

            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void ExpireOlderThan_ZeroLatch_NeverExpires()
        {
            // 0 은 "상한 없음" 규약이다 (StageConfig.RecognitionLatchSeconds).
            var set = new CandidateSet(NoLatch);
            set.Recognize(NewSushi(0), 0f);

            set.ExpireOlderThan(99999f);

            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void ExpireOlderThan_MixedAges_RemovesOnlyExpired()
        {
            var set = new CandidateSet(Latch);
            var old = NewSushi(0);
            var fresh = NewSushi(1);
            set.Recognize(old, 0f);
            set.Recognize(fresh, 4f);

            set.ExpireOlderThan(6f);

            Assert.AreEqual(1, set.Count);
            Assert.AreSame(fresh, set.Items[0]);
        }

        [Test]
        public void Recognize_SushiPastReachEdge_StaysCandidate()
        {
            // 래치의 본질 — 물리적으로 범위를 벗어났다는 이유만으로는 빠지지 않는다.
            var set = new CandidateSet(Latch);
            var sushi = NewSushi(0);
            set.Recognize(sushi, 0f);

            sushi.BeltPosition += 9999f;
            set.ExpireOlderThan(1f);

            Assert.AreEqual(1, set.Count);
        }

        private static SushiItem NewSushi(int sequenceNumber)
        {
            return new SushiItem(null, sequenceNumber);
        }
    }
}
