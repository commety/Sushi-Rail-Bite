using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// <see cref="RunConfig"/> 는 <b>순서만</b> 담는다. 검증할 구조 불변식이 없다는 것이
    /// 이 파일이 고정하는 계약이다.
    ///
    /// <para>
    /// 빈 목록도, <c>null</c> 항목도 오류가 아니다 — 씬을 조금씩 조립하는 동안 반쯤 채워진
    /// 애셋이 정상 상태이기 때문이다. <c>null</c> 을 거르는 책임은 <c>Runtime</c> 의
    /// 진행 판정 한 곳에 있고, 거르는 지점이 둘이면 어느 쪽이 지켰는지 알 수 없게 된다
    /// (<c>.claude/rules/scriptable-object.md</c> §6).
    /// </para>
    /// </summary>
    public sealed class RunConfigTests
    {
        private RunConfig _config;
        private List<Object> _disposables;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RunConfig>();
            _disposables = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            for (var i = 0; i < _disposables.Count; i++)
            {
                Object.DestroyImmediate(_disposables[i]);
            }
        }

        [Test]
        public void Stages_FreshConfig_IsEmptyNotNull()
        {
            Assert.IsNotNull(_config.Stages);
            Assert.IsEmpty(_config.Stages);
        }

        [Test]
        public void StageCount_FreshConfig_IsZero()
        {
            Assert.AreEqual(0, _config.StageCount);
        }

        [Test]
        public void StageCount_ThreeStages_IsThree()
        {
            AppendStage(CreateStage(1));
            AppendStage(CreateStage(2));
            AppendStage(CreateStage(3));

            Assert.AreEqual(3, _config.StageCount);
        }

        /// <summary>
        /// <b>순서의 진실은 목록이다.</b> <c>StageConfig.StageNumber</c> 는 표시용이라
        /// 목록 인덱스와 대조하지 않는다 — 대조를 규칙으로 만들면 순서를 바꿀 때마다
        /// 애셋 두 곳을 고쳐야 한다.
        ///
        /// <para>
        /// 그래서 스테이지 번호를 <b>인덱스와 엇갈리게</b> 준다. 나란히 두면 번호로
        /// 정렬하는 잘못된 구현도 통과한다.
        /// </para>
        /// </summary>
        [Test]
        public void Stages_ThreeStages_PreservesAuthoredOrder()
        {
            var third = CreateStage(3);
            var first = CreateStage(1);
            var second = CreateStage(2);

            AppendStage(third);
            AppendStage(first);
            AppendStage(second);

            Assert.AreSame(third, _config.Stages[0]);
            Assert.AreSame(first, _config.Stages[1]);
            Assert.AreSame(second, _config.Stages[2]);
            Assert.AreEqual(3, _config.Stages[0].StageNumber);
        }

        /// <summary>
        /// 빈 슬롯은 인스펙터에서 목록을 늘리면 자연히 생긴다. 스키마는 그대로 담고,
        /// 거르는 일은 진행 판정에 맡긴다.
        ///
        /// <para>
        /// <c>null</c> 을 <b>가운데</b>에 둔다. 끝에 두면 뒤를 잘라내는 구현과 그대로 담는
        /// 구현이 구분되지 않는다.
        /// </para>
        /// </summary>
        [Test]
        public void Stages_ListWithNullEntry_KeepsItAsAuthored()
        {
            var first = CreateStage(1);
            var last = CreateStage(2);

            AppendStage(first);
            AppendStage(null);
            AppendStage(last);

            Assert.AreEqual(3, _config.StageCount);
            Assert.AreSame(first, _config.Stages[0]);
            Assert.IsNull(_config.Stages[1]);
            Assert.AreSame(last, _config.Stages[2]);
        }

        [Test]
        public void DisplayName_SetByInspector_IsExposed()
        {
            SerializedFieldSetter.SetString(_config, "_displayName", "데모 런");

            Assert.AreEqual("데모 런", _config.DisplayName);
        }

        private StageConfig CreateStage(int stageNumber)
        {
            var stage = ScriptableObject.CreateInstance<StageConfig>();
            SerializedFieldSetter.SetInt(stage, "_stageNumber", stageNumber);
            _disposables.Add(stage);
            return stage;
        }

        private void AppendStage(StageConfig stage)
        {
            SerializedFieldSetter.AppendObject(_config, "_stages", stage);
        }
    }
}
