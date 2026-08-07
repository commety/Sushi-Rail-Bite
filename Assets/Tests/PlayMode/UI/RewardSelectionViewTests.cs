using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.UI;
using TMPro;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.UI
{
    /// <summary>
    /// 화면은 <b>계산하지 않는다.</b> 프레젠터가 정한 이름 목록을 라벨로 옮기는 것까지만
    /// 본다 — 무엇을 제시할지·고른 것이 유효한지는 EditMode 의
    /// <c>RewardSelectionPresenterTests</c> 가 검증한다.
    ///
    /// <para>
    /// PlayMode 는 느리므로 <b>씬·컴포넌트가 실제로 필요한 것만</b> 여기 둔다
    /// (<c>.claude/rules/tests.md</c> §1).
    /// </para>
    /// </summary>
    public sealed class RewardSelectionViewTests
    {
        private readonly List<GameObject> _objects = new();

        private RewardSelectionView _view;
        private TMP_Text _label;

        [SetUp]
        public void SetUp()
        {
            _label = NewObject("RewardOffersLabel").AddComponent<TextMeshPro>();
            _view = NewObject("RewardView").AddComponent<RewardSelectionView>();
            SetLabel(_view, "_offersLabel", _label);
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

        [Test]
        public void ShowOffers_ThreeNames_WritesAllThree()
        {
            _view.ShowOffers(new[] { "장어", "방어", "소식좌" });

            Assert.IsTrue(_view.IsShowing);
            StringAssert.Contains("장어", _label.text);
            StringAssert.Contains("방어", _label.text);
            StringAssert.Contains("소식좌", _label.text);
        }

        /// <summary>고를 번호가 함께 보여야 숫자 키 조작을 알 수 있다.</summary>
        [Test]
        public void ShowOffers_Names_AreNumberedFromOne()
        {
            _view.ShowOffers(new[] { "장어", "방어" });

            StringAssert.Contains("1. 장어", _label.text);
            StringAssert.Contains("2. 방어", _label.text);
        }

        /// <summary>
        /// 후보가 0개인 것은 정상이다. 빈 화면을 그리면 보상 시스템이 고장 난 것과
        /// 구분되지 않으므로 안내 문구를 낸다.
        /// </summary>
        [Test]
        public void ShowOffers_Empty_WritesNoRewardNotice()
        {
            _view.ShowOffers(System.Array.Empty<string>());

            Assert.IsTrue(_view.IsShowing);
            Assert.IsNotEmpty(_label.text);
            StringAssert.Contains("없음", _label.text);
        }

        [Test]
        public void Hide_AfterShow_ClearsLabel()
        {
            _view.ShowOffers(new[] { "장어" });

            _view.Hide();

            Assert.IsFalse(_view.IsShowing);
            Assert.IsEmpty(_label.text);
        }

        [Test]
        public void ShowOffers_Twice_ReplacesInsteadOfAppending()
        {
            _view.ShowOffers(new[] { "장어" });

            _view.ShowOffers(new[] { "방어" });

            StringAssert.Contains("방어", _label.text);
            StringAssert.DoesNotContain("장어", _label.text);
        }

        private static void SetLabel(RewardSelectionView view, string fieldName, TMP_Text label)
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
