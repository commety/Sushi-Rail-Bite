using System;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Run
{
    /// <summary>
    /// 스테이지 진행의 계약 — 지금 몇 번째인가 · 다음이 있는가 · 런이 끝났는가.
    ///
    /// <para>
    /// <b>런 종료는 플래그가 아니라 유도값이다</b> (M4 README D3). <c>StageNumber</c> 가
    /// 스테이지 총수를 넘으면 끝난 것이고, 별도의 <c>bool</c> 을 두지 않는다 — 두 장치가
    /// 같은 것을 지키면 나중에 한쪽만 고쳐진다.
    /// </para>
    /// <para>
    /// <b>생성자가 <see cref="RunConfig"/> 가 아니라 목록을 받는 이유</b>: SO 를 통해서만
    /// 세울 수 있으면 테스트가 매번 직렬화 계층으로 목록을 밀어 넣어야 한다. 목록을 그대로
    /// 받으면 <c>Runtime</c> 이 <c>RunConfig</c> 를 몰라도 되고, 스테이지가 1개인 런(D5)도
    /// 배열 하나로 표현된다.
    /// </para>
    /// <para>
    /// <b><see cref="RunState"/> 는 스테이지 총수를 모른다</b> (D4). 총수를 아는 것은
    /// <c>RunProgression</c> 하나이며, 그래서 진행 판정이 여기 모인다.
    /// </para>
    /// </summary>
    public sealed class RunProgressionTests
    {
        private const int Seed = 4242;

        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        [Test]
        public void StageCount_ThreeStages_IsThree()
        {
            var progression = NewProgression(NewRun(), CreateStage(), CreateStage(), CreateStage());

            Assert.AreEqual(3, progression.StageCount);
        }

        /// <summary>
        /// <c>null</c> 을 <b>목록 중간</b>에 둔다. 끝에 두면 거기서 잘라내는 구현과 걸러내는
        /// 구현이 같은 답을 내 구분되지 않는다 — 잘라내는 구현은 여기서 1 을 낸다.
        /// </summary>
        [Test]
        public void StageCount_ListWithNullEntries_CountsOnlySurvivors()
        {
            var first = CreateStage();
            var second = CreateStage();

            var progression = NewProgression(NewRun(), first, null, second);

            Assert.AreEqual(2, progression.StageCount,
                            "null 은 잘라내는 게 아니라 걸러냅니다 — 뒤 항목이 살아 있어야 합니다");
            Assert.AreSame(first, progression.CurrentStage, "걸러낸 뒤에도 순서는 유지됩니다");
        }

        /// <summary>
        /// 씬을 조금씩 조립하는 동안 목록이 비어 있는 것은 정상 상태다. 터지는 것보다
        /// 빈 런으로 도는 편이 진단하기 쉽다 (<see cref="RunState"/> 의 덱 처리와 같은 판단).
        /// </summary>
        [Test]
        public void Ctor_NullStages_TreatsAsEmpty()
        {
            var progression = new RunProgression(null, NewRun());

            Assert.AreEqual(0, progression.StageCount);
            Assert.IsNull(progression.CurrentStage);
        }

        /// <summary>런이 없으면 진행할 대상 자체가 없다 — 덱과 달리 대신할 기본값이 없다.</summary>
        [Test]
        public void Ctor_NullRun_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new RunProgression(new[] { CreateStage() }, null));
        }

        [Test]
        public void CurrentStage_FreshRun_IsFirstStage()
        {
            var first = CreateStage();

            var progression = NewProgression(NewRun(), first, CreateStage(), CreateStage());

            Assert.AreSame(first, progression.CurrentStage);
        }

        /// <summary>
        /// 세 스테이지를 <b>서로 다른 인스턴스</b>로 만들고 <c>AreSame</c> 으로 본다.
        /// <c>IsNotNull</c> 만 확인하면 첫 스테이지를 계속 돌려주는 구현도 통과한다.
        /// </summary>
        [Test]
        public void CurrentStage_AfterOneAdvance_IsSecondStage()
        {
            var first = CreateStage();
            var second = CreateStage();
            var progression = NewProgression(NewRun(), first, second, CreateStage());

            progression.AdvanceAfterClear();

            Assert.AreSame(second, progression.CurrentStage);
            Assert.AreNotSame(first, progression.CurrentStage);
        }

        /// <summary>
        /// 화면이 "스테이지 2 클리어" 를 쓰려면 번호가 필요하다. <c>StageConfig.StageNumber</c>
        /// 를 쓰지 않는 이유는 그것이 <b>표시용</b>이라 목록 순서와 어긋나도 오류가 아니라고
        /// 정했기 때문이다 (step-01). 진행 순서의 번호는 여기서 나온다.
        /// </summary>
        [Test]
        public void CurrentStageNumber_FreshRun_IsOne()
        {
            var progression = NewProgression(NewRun(), CreateStage(), CreateStage());

            Assert.AreEqual(1, progression.CurrentStageNumber);
        }

        [Test]
        public void CurrentStageNumber_AfterOneAdvance_IsTwo()
        {
            var progression = NewProgression(NewRun(), CreateStage(), CreateStage(), CreateStage());

            progression.AdvanceAfterClear();

            Assert.AreEqual(2, progression.CurrentStageNumber);
        }

        [Test]
        public void CurrentStage_RunComplete_IsNull()
        {
            var progression = NewProgression(NewRun(), CreateStage(), CreateStage());

            progression.AdvanceAfterClear();
            progression.AdvanceAfterClear();

            Assert.IsNull(progression.CurrentStage);
        }

        [Test]
        public void AdvanceAfterClear_MidRun_ReturnsTrueAndMovesForward()
        {
            var run = NewRun();
            var second = CreateStage();
            var progression = NewProgression(run, CreateStage(), second, CreateStage());

            var advanced = progression.AdvanceAfterClear();

            Assert.IsTrue(advanced);
            Assert.AreEqual(2, run.StageNumber);
            Assert.AreSame(second, progression.CurrentStage);
        }

        /// <summary>
        /// 마지막을 깨면 <b>갈 곳은 없지만 클리어는 기록된다</b> — 그래야 D3 의 유도식
        /// (<c>StageNumber &gt; StageCount</c>) 이 런 종료를 알아본다. 반환값만 보고
        /// 번호를 그대로 두면 3판을 깨도 런이 끝나지 않는다.
        /// </summary>
        [Test]
        public void AdvanceAfterClear_LastStage_ReturnsFalse()
        {
            var run = NewRun();
            var progression = NewProgression(run, CreateStage(), CreateStage());
            progression.AdvanceAfterClear();

            var advanced = progression.AdvanceAfterClear();

            Assert.IsFalse(advanced, "마지막 스테이지 뒤에는 갈 곳이 없습니다");
            Assert.AreEqual(3, run.StageNumber, "그래도 클리어는 기록되어야 런이 끝납니다");
        }

        /// <summary>
        /// <c>false</c> 만 보지 않는다. 번호는 계속 오르는데 <c>false</c> 만 돌려주는 구현이
        /// 통과하면, 인덱스가 한참 벗어난 뒤에야 발견된다.
        /// </summary>
        [Test]
        public void AdvanceAfterClear_AfterRunComplete_ReturnsFalseAndDoesNotAdvance()
        {
            var run = NewRun();
            var progression = NewProgression(run, CreateStage(), CreateStage());
            progression.AdvanceAfterClear();
            progression.AdvanceAfterClear();
            var stageNumberAtCompletion = run.StageNumber;

            var advanced = progression.AdvanceAfterClear();

            Assert.IsFalse(advanced);
            Assert.AreEqual(stageNumberAtCompletion, run.StageNumber,
                            "런이 끝난 뒤의 호출은 멱등이어야 합니다");
        }

        /// <summary>
        /// 먼저 실패를 한 번 기록해 시도 횟수를 2 로 만든다. 처음부터 1 이면 이 테스트는
        /// 아무것도 검증하지 않는다.
        /// </summary>
        [Test]
        public void AdvanceAfterClear_MidRun_ResetsAttemptNumber()
        {
            var run = NewRun();
            var progression = NewProgression(run, CreateStage(), CreateStage(), CreateStage());
            run.RecordFailedAttempt();
            Assert.AreEqual(2, run.AttemptNumber, "전제가 깨졌습니다 — 진행 전 시도 횟수는 2 여야 합니다");

            progression.AdvanceAfterClear();

            Assert.AreEqual(1, run.AttemptNumber);
        }

        [Test]
        public void IsRunComplete_FreshRun_IsFalse()
        {
            var progression = NewProgression(NewRun(), CreateStage(), CreateStage(), CreateStage());

            Assert.IsFalse(progression.IsRunComplete);
        }

        /// <summary>
        /// 중간 지점에서 <c>false</c> 를 함께 박는다 — 없으면 항상 <c>true</c> 를 돌려주는
        /// 구현도 통과한다.
        /// </summary>
        [Test]
        public void IsRunComplete_AfterClearingLastStage_IsTrue()
        {
            var progression = NewProgression(NewRun(), CreateStage(), CreateStage(), CreateStage());

            progression.AdvanceAfterClear();
            progression.AdvanceAfterClear();
            Assert.IsFalse(progression.IsRunComplete, "3 스테이지 중 3 판째는 아직 런이 끝난 게 아닙니다");
            progression.AdvanceAfterClear();

            Assert.IsTrue(progression.IsRunComplete);
        }

        /// <summary>
        /// 중간 지점의 <c>true</c> 를 반례로 함께 둔다 — 상수 <c>false</c> 를 돌려주는
        /// 구현을 걸러낸다.
        /// </summary>
        [Test]
        public void HasNextStage_LastStage_IsFalse()
        {
            var progression = NewProgression(NewRun(), CreateStage(), CreateStage(), CreateStage());
            progression.AdvanceAfterClear();
            Assert.IsTrue(progression.HasNextStage, "3 스테이지 중 2 판째에는 다음이 있습니다");

            progression.AdvanceAfterClear();

            Assert.IsFalse(progression.HasNextStage);
        }

        [Test]
        public void CurrentStage_EmptyStageList_IsNullAndRunComplete()
        {
            var progression = NewProgression(NewRun());

            Assert.IsNull(progression.CurrentStage);
            Assert.IsTrue(progression.IsRunComplete);
        }

        /// <summary>스테이지 1개짜리 런이 곧 스테이지 목록을 물리지 않은 현행 동작이다 (D5).</summary>
        [Test]
        public void CurrentStage_SingleStageRun_IsThatStage()
        {
            var only = CreateStage();

            var progression = NewProgression(NewRun(), only);

            Assert.AreSame(only, progression.CurrentStage);
            Assert.IsFalse(progression.IsRunComplete);
        }

        private static RunProgression NewProgression(RunState run, params StageConfig[] stages)
        {
            return new RunProgression(stages, run);
        }

        private static RunState NewRun()
        {
            return new RunState(null, null, Seed);
        }

        /// <summary>
        /// 값을 채우지 않는다 — 이 단계가 보는 것은 <b>동일성</b>뿐이다. 값이 필요해지면
        /// <c>StageConfigBuilder</c> 를 쓴다.
        /// </summary>
        private StageConfig CreateStage()
        {
            var stage = ScriptableObject.CreateInstance<StageConfig>();
            _disposables.Add(stage);
            return stage;
        }
    }
}
