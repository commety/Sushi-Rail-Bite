using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.UI;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.UI
{
    /// <summary>
    /// 정보 창의 화면. <b>판정하지 않는다</b> — 프레젠터가 만든 글자를 옮기기만 한다.
    ///
    /// <para>
    /// 그래서 여기서 볼 것은 «어느 라벨에 무엇이 쓰이나» 와 «꺼진 채로 물려도 되나» 둘이다.
    /// 무엇을 적을지는 <c>CustomerInspectorPresenterTests</c> 가 본다.
    /// </para>
    /// </summary>
    public sealed class CustomerInspectorViewTests
    {
        private readonly List<Object> _garbage = new();

        private CustomerInspectorView _view;

        [SetUp]
        public void SetUp()
        {
            _view = NewView(active: true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();
        }

        [Test]
        public void Fresh_IsHidden()
        {
            Assert.IsFalse(_view.IsShowing);
        }

        [Test]
        public void ShowCustomer_WritesEveryStaticLabel()
        {
            _view.ShowCustomer("먹보", "먹보", "범위 3\n대역 100~150");

            Assert.IsTrue(_view.IsShowing);
            Assert.AreEqual("먹보", LabelText("NameLabel"));
            Assert.AreEqual("먹보", LabelText("KindLabel"));
            StringAssert.Contains("100~150", LabelText("StatsLabel"));
        }

        [Test]
        public void RefreshLive_WritesEveryLiveLabel()
        {
            _view.ShowCustomer("기본", "기본", "범위 3");

            _view.RefreshLive("소화 중", "3/5", "2");

            Assert.AreEqual("소화 중", LabelText("StateLabel"));
            Assert.AreEqual("3/5", LabelText("SaturationLabel"));
            Assert.AreEqual("2", LabelText("RemainingLabel"));
        }

        /// <summary>
        /// 실시간 줄을 다시 써도 <b>정적 줄은 흔들리지 않는다.</b> 한 메서드가 여섯을 전부
        /// 쓰면 매 프레임 스탯 문자열까지 다시 대입하게 된다.
        /// </summary>
        [Test]
        public void RefreshLive_LeavesStaticLabelsAlone()
        {
            _view.ShowCustomer("먹보", "먹보", "범위 3");

            _view.RefreshLive("대기 중", "0/8", string.Empty);

            Assert.AreEqual("먹보", LabelText("NameLabel"));
            StringAssert.Contains("범위 3", LabelText("StatsLabel"));
        }

        [Test]
        public void Hide_TurnsThePanelOff()
        {
            _view.ShowCustomer("기본", "기본", "범위 3");

            _view.Hide();

            Assert.IsFalse(_view.IsShowing);
        }

        /// <summary>
        /// <b>꺼진 채로 물려도 그려져야 한다.</b> 정보 창은 씬에서 꺼진 채 시작하는데, 꺼진
        /// 오브젝트의 <c>Awake</c> 는 켜질 때까지 오지 않는다 — 참조 수집을 거기에만 두면
        /// 첫 번째 열기에서 라벨이 전부 <c>null</c> 이라 <b>빈 창</b>이 뜬다. 예외도 경고도
        /// 없고 두 번째부터 정상으로 보이는 것이 그 서명이다
        /// (<c>.claude/knowledge/unity-scripting-gotchas.md</c> §5).
        /// </summary>
        [Test]
        public void ShowCustomer_WhileInactive_StillWrites()
        {
            var view = NewView(active: false);

            view.ShowCustomer("소식", "소식", "범위 3");

            Assert.AreEqual("소식", LabelTextOf(view, "NameLabel"),
                            "꺼진 채로는 라벨을 못 찾는다 — Awake 순서에 기대고 있다");
        }

        private string LabelText(string childName)
        {
            return LabelTextOf(_view, childName);
        }

        private static string LabelTextOf(CustomerInspectorView view, string childName)
        {
            return view.transform.Find(childName).GetComponent<TMP_Text>().text;
        }

        /// <summary>
        /// 라벨 여섯을 이름으로 미리 놓아 둔다 — 뷰가 인스펙터 없이 자기 하위에서 찾는
        /// 경로를 그대로 태우기 위해서다. 참조를 직접 주입하면 그 폴백이 죽어도 초록이
        /// 된다 (<c>.claude/rules/tests.md</c> §1).
        /// </summary>
        private CustomerInspectorView NewView(bool active)
        {
            var root = new GameObject("CustomerInspectorPanel");
            _garbage.Add(root);
            root.SetActive(active);

            foreach (var name in new[]
                     {
                         "NameLabel", "KindLabel", "StatsLabel",
                         "StateLabel", "SaturationLabel", "RemainingLabel"
                     })
            {
                var child = new GameObject(name);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<TextMeshProUGUI>();
            }

            return root.AddComponent<CustomerInspectorView>();
        }
    }
}
