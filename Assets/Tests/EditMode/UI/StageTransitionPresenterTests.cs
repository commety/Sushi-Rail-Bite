using System;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 스테이지를 깬 뒤 다음 판으로 넘어가기 전의 한 박자. <b>프레젠터는 Unity API 를
    /// 모른다</b> — 그래서 이 파일이 EditMode 에 있고 뷰는 손으로 쓴 스텁으로 갈아 끼운다
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// <b>화면을 여는 것과 상태를 바꾸는 것은 다른 일이다.</b> <c>Open</c> 이 스테이지
    /// 번호를 올리면 플레이어가 확인 입력을 하기도 전에 HUD 의 번호가 바뀐다. 상태는
    /// <c>Proceed</c> 에서 바뀐다 — 이 파일의 절반이 그 경계를 지킨다.
    /// </para>
    /// </summary>
    public sealed class StageTransitionPresenterTests
    {
        private const int Seed = 31;

        private readonly List<Object> _disposables = new();

        private FakeStageTransitionView _view;
        private RunState _run;
        private RunProgression _progression;
        private StageTransitionPresenter _presenter;
        private List<StageConfig> _advancedTo;
        private int _runCompletedCount;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeStageTransitionView();
            _run = new RunState(null, null, Seed);
            _advancedTo = new List<StageConfig>();
            _runCompletedCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        // ── 열기 — 상태를 바꾸지 않는다 ─────────────────────────

        /// <summary>
        /// <b>스테이지 1 에서 열지 않는다.</b> 1 에서 열면 <c>(1, 2)</c> 라서 "항상 1 을
        /// 넘기는" 구현도 통과한다. 한 번 진행시켜 <c>(2, 3)</c> 을 확인한다.
        /// </summary>
        [Test]
        public void Open_MidRun_ShowsClearedWithNextNumber()
        {
            Build(3);
            _progression.AdvanceAfterClear();

            _presenter.Open();

            Assert.AreEqual(1, _view.ClearedCalls.Count);
            Assert.AreEqual((2, 3), _view.ClearedCalls[0]);
            Assert.IsEmpty(_view.RunCompleteCalls);
        }

        [Test]
        public void Open_LastStage_ShowsRunComplete()
        {
            Build(2);
            _progression.AdvanceAfterClear();

            _presenter.Open();

            Assert.IsTrue(_presenter.IsRunFinale);
            Assert.AreEqual(1, _view.RunCompleteCalls.Count);
            Assert.AreEqual(2, _view.RunCompleteCalls[0]);
            Assert.IsEmpty(_view.ClearedCalls, "런 종료에는 다음 스테이지 안내가 없습니다");
        }

        [Test]
        public void Open_MidRun_DoesNotAdvanceRunState()
        {
            Build(3);

            _presenter.Open();

            Assert.AreEqual(1, _run.StageNumber, "화면을 여는 것만으로 스테이지가 넘어가면 안 됩니다");
            Assert.IsTrue(_presenter.IsOpen);
        }

        [Test]
        public void Open_Twice_DoesNotDoubleAdvance()
        {
            Build(3);

            _presenter.Open();
            _presenter.Open();

            Assert.AreEqual(1, _run.StageNumber);
        }

        // ── 진행 — 여기서 상태가 바뀐다 ─────────────────────────

        /// <summary>
        /// <c>AreSame</c> 으로 인스턴스를 확인한다. <c>IsNotNull</c> 이면 지금 스테이지를
        /// 그대로 넘기는 구현이 통과한다.
        /// </summary>
        [Test]
        public void Proceed_MidRun_RaisesStageAdvancedWithNextStage()
        {
            var stages = NewStages(3);
            Build(stages);

            _presenter.Open();
            _presenter.Proceed();

            Assert.AreEqual(1, _advancedTo.Count);
            Assert.AreSame(stages[1], _advancedTo[0]);
            Assert.AreEqual(0, _runCompletedCount);
        }

        [Test]
        public void Proceed_MidRun_ReturnsTrue()
        {
            Build(3);
            _presenter.Open();

            Assert.IsTrue(_presenter.Proceed());
            Assert.AreEqual(2, _run.StageNumber);
        }

        [Test]
        public void Proceed_LastStage_RaisesRunCompleted()
        {
            Build(2);
            _progression.AdvanceAfterClear();
            _presenter.Open();

            Assert.IsFalse(_presenter.Proceed());
            Assert.AreEqual(1, _runCompletedCount);
            Assert.IsTrue(_progression.IsRunComplete);
        }

        [Test]
        public void Proceed_LastStage_DoesNotRaiseStageAdvanced()
        {
            Build(1);
            _presenter.Open();

            _presenter.Proceed();

            Assert.IsEmpty(_advancedTo, "갈 곳이 없는데 다음 스테이지를 알리면 안 됩니다");
        }

        [Test]
        public void Proceed_NotOpen_ReturnsFalseAndRaisesNothing()
        {
            Build(3);

            Assert.IsFalse(_presenter.Proceed());
            Assert.IsEmpty(_advancedTo);
            Assert.AreEqual(0, _runCompletedCount);
            Assert.AreEqual(1, _run.StageNumber, "닫힌 화면의 확인 입력이 런을 움직이면 안 됩니다");
        }

        [Test]
        public void Proceed_Twice_RaisesOnce()
        {
            Build(3);
            _presenter.Open();

            _presenter.Proceed();
            _presenter.Proceed();

            Assert.AreEqual(1, _advancedTo.Count);
            Assert.AreEqual(2, _run.StageNumber);
        }

        // ── 닫기 ────────────────────────────────────────────────

        /// <summary>
        /// <b>순서를 본다.</b> 구독자가 <c>Build()</c> 를 부르는데(step-08) 그때 화면이 아직
        /// 떠 있으면 새 스테이지 위에 전환 화면이 겹쳐 남는다. "둘 다 일어났다" 만 보면
        /// 순서가 뒤집힌 구현도 통과하므로, 핸들러 <b>안에서</b> 닫힘 횟수를 읽는다.
        /// </summary>
        [Test]
        public void Proceed_Any_HidesViewBeforeRaisingEvent()
        {
            Build(3);
            var hideCountWhenRaised = -1;
            _presenter.StageAdvanced += _ => hideCountWhenRaised = _view.HideCount;
            _presenter.Open();

            _presenter.Proceed();

            Assert.AreEqual(1, hideCountWhenRaised, "이벤트를 발행하기 전에 화면을 내려야 합니다");
        }

        [Test]
        public void IsOpen_AfterProceed_IsFalse()
        {
            Build(3);
            _presenter.Open();

            _presenter.Proceed();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.IsFalse(_presenter.IsRunFinale, "닫힌 화면은 어떤 화면도 아닙니다");
        }

        // ── 방어 ────────────────────────────────────────────────

        [Test]
        public void Ctor_NullView_Throws()
        {
            Build(1);

            Assert.Throws<ArgumentNullException>(
                () => new StageTransitionPresenter(null, _progression));
        }

        [Test]
        public void Ctor_NullProgression_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StageTransitionPresenter(_view, null));
        }

        private void Build(int stageCount)
        {
            Build(NewStages(stageCount));
        }

        private void Build(List<StageConfig> stages)
        {
            _progression = new RunProgression(stages, _run);
            _presenter = new StageTransitionPresenter(_view, _progression);
            _presenter.StageAdvanced += stage => _advancedTo.Add(stage);
            _presenter.RunCompleted += () => _runCompletedCount++;
        }

        private List<StageConfig> NewStages(int count)
        {
            var stages = new List<StageConfig>();
            for (var i = 0; i < count; i++)
            {
                var stage = ScriptableObject.CreateInstance<StageConfig>();
                _disposables.Add(stage);
                stages.Add(stage);
            }

            return stages;
        }
    }
}
