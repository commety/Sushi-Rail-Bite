using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.UI;
using TMPro;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.UI
{
    /// <summary>
    /// 화면은 <b>판정하지 않는다.</b> 프레젠터가 정한 번호를 문구로 옮기는 것까지만 본다 —
    /// 다음 스테이지가 있는지·넘어가도 되는지는 EditMode 의
    /// <c>StageTransitionPresenterTests</c> 가 검증한다.
    ///
    /// <para>
    /// PlayMode 는 느리므로 <b>씬·컴포넌트가 실제로 필요한 것만</b> 여기 둔다
    /// (<c>.claude/rules/tests.md</c> §1).
    /// </para>
    /// </summary>
    public sealed class StageTransitionViewTests
    {
        private readonly List<GameObject> _objects = new();

        private StageTransitionView _view;
        private TMP_Text _label;

        [SetUp]
        public void SetUp()
        {
            _label = NewObject("StageTransitionLabel").AddComponent<TextMeshPro>();
            _view = NewObject("StageTransition").AddComponent<StageTransitionView>();
            SetLabel(_view, "_messageLabel", _label);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        /// <summary>
        /// 깬 번호만 보면 다음 번호가 빠진 구현이 통과한다. <b>둘 다</b> 확인한다.
        /// </summary>
        [Test]
        public void ShowStageCleared_Stage1_WritesBothNumbers()
        {
            _view.ShowStageCleared(1, 2);

            Assert.IsTrue(_view.IsShowing);
            StringAssert.Contains("스테이지 1", _label.text);
            StringAssert.Contains("스테이지 2", _label.text);
        }

        /// <summary>
        /// 다른 지점에서도 <b>자기 번호</b>를 쓰는지 본다 — 위 테스트만 있으면 늘 1 과 2 를
        /// 쓰는 구현이 통과한다.
        /// </summary>
        [Test]
        public void ShowStageCleared_MidRun_WritesItsOwnNumbers()
        {
            _view.ShowStageCleared(2, 3);

            StringAssert.Contains("스테이지 2", _label.text);
            StringAssert.Contains("스테이지 3", _label.text);
        }

        [Test]
        public void ShowRunComplete_LastStage_WritesCompletionText()
        {
            _view.ShowRunComplete(3);

            Assert.IsTrue(_view.IsShowing);
            StringAssert.Contains("스테이지 3", _label.text);
            StringAssert.Contains("완료", _label.text);
        }

        /// <summary>
        /// 두 화면이 <b>구분되는 문구</b>여야 한다. 같은 문구를 쓰면 플레이어가 런이 끝난
        /// 것을 알 수 없다.
        /// </summary>
        [Test]
        public void ShowRunComplete_ComparedToStageCleared_UsesDifferentText()
        {
            _view.ShowStageCleared(3, 4);
            var clearedText = _label.text;

            _view.ShowRunComplete(3);

            Assert.AreNotEqual(clearedText, _label.text);
        }

        [Test]
        public void ShowStageCleared_ThenRunComplete_ReplacesInsteadOfAppending()
        {
            _view.ShowStageCleared(1, 2);

            _view.ShowRunComplete(3);

            StringAssert.Contains("스테이지 3", _label.text);
            StringAssert.DoesNotContain("스테이지 1", _label.text);
        }

        [Test]
        public void Hide_AfterShow_ClearsLabel()
        {
            _view.ShowStageCleared(1, 2);

            _view.Hide();

            Assert.IsEmpty(_label.text);
        }

        [Test]
        public void IsShowing_AfterHide_IsFalse()
        {
            _view.ShowRunComplete(3);

            _view.Hide();

            Assert.IsFalse(_view.IsShowing);
        }

        private static void SetLabel(StageTransitionView view, string fieldName, TMP_Text label)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(view);
            serialized.FindProperty(fieldName).objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go;
        }
    }
}
