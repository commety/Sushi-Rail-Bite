using NUnit.Framework;
using SushiDefense.Stages;

namespace SushiDefense.Tests.EditMode.Stages
{
    /// <summary>
    /// <see cref="PauseState"/> 는 <b>멈춰 있나</b> 하나만 답한다.
    ///
    /// <para>
    /// 시간을 흘릴지 말지는 씬 진입점이 이 값을 보고 정한다. 여기에 <c>Time.timeScale</c> 이
    /// 없는 이유는 그것이 전역이라 누가 멈췄는지 추적할 수 없기 때문이며, 무엇보다
    /// 이 파일이 프레임을 세지 않고 돌 수 있어야 하기 때문이다.
    /// </para>
    /// </summary>
    public sealed class PauseStateTests
    {
        private PauseState _pause;
        private int _changedCount;
        private bool _lastValue;

        [SetUp]
        public void SetUp()
        {
            _pause = new PauseState();
            _changedCount = 0;
            _lastValue = false;
            _pause.Changed += OnChanged;
        }

        [TearDown]
        public void TearDown()
        {
            _pause.Changed -= OnChanged;
        }

        [Test]
        public void Fresh_IsNotPaused()
        {
            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void Pause_WhenRunning_RaisesChangedOnceWithTrue()
        {
            _pause.Pause();

            Assert.IsTrue(_pause.IsPaused);
            Assert.AreEqual(1, _changedCount);
            Assert.IsTrue(_lastValue);
        }

        /// <summary>
        /// 값이 바뀐 때만 알린다. 같은 전이를 여러 번 흘리면 구독자(버튼 표시·오디오)가
        /// 같은 일을 여러 번 한다.
        /// </summary>
        [Test]
        public void Pause_Twice_RaisesChangedOnce()
        {
            _pause.Pause();
            _pause.Pause();

            Assert.IsTrue(_pause.IsPaused);
            Assert.AreEqual(1, _changedCount);
        }

        [Test]
        public void Resume_WhenPaused_RaisesChangedOnceWithFalse()
        {
            _pause.Pause();
            _changedCount = 0;

            _pause.Resume();

            Assert.IsFalse(_pause.IsPaused);
            Assert.AreEqual(1, _changedCount);
            Assert.IsFalse(_lastValue);
        }

        [Test]
        public void Resume_WhenNotPaused_DoesNotRaise()
        {
            _pause.Resume();

            Assert.IsFalse(_pause.IsPaused);
            Assert.AreEqual(0, _changedCount);
        }

        /// <summary>
        /// 한 번 쓰고 마는 장치가 아니다. 중복 억제를 "이미 발행했다" 플래그로 구현하면
        /// 두 번째 일시정지가 조용히 사라진다.
        /// </summary>
        [Test]
        public void PauseResumePause_RaisesThreeTimes()
        {
            _pause.Pause();
            _pause.Resume();
            _pause.Pause();

            Assert.IsTrue(_pause.IsPaused);
            Assert.AreEqual(3, _changedCount);
        }

        private void OnChanged(bool paused)
        {
            _changedCount++;
            _lastValue = paused;
        }
    }
}
