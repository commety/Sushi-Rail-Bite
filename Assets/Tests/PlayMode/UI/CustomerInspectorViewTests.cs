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
            _view.ShowCustomer("먹보", "범위 3\n대역 100~150");

            Assert.IsTrue(_view.IsShowing);
            Assert.AreEqual("먹보", LabelText("NameLabel"));
            StringAssert.Contains("100~150", LabelText("StatsLabel"));
        }

        [Test]
        public void RefreshLive_WritesEveryLiveLabel()
        {
            _view.ShowCustomer("기본", "범위 3");

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
            _view.ShowCustomer("먹보", "범위 3");

            _view.RefreshLive("대기 중", "0/8", string.Empty);

            Assert.AreEqual("먹보", LabelText("NameLabel"));
            StringAssert.Contains("범위 3", LabelText("StatsLabel"));
        }

        [Test]
        public void Hide_TurnsThePanelOff()
        {
            _view.ShowCustomer("기본", "범위 3");

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

            view.ShowCustomer("소식", "범위 3");

            Assert.AreEqual("소식", LabelTextOf(view, "NameLabel"),
                            "꺼진 채로는 라벨을 못 찾는다 — Awake 순서에 기대고 있다");
        }

        /// <summary>
        /// 좌표가 <b>실제로 결과를 가르는지</b> 본다.
        ///
        /// <para>
        /// <b>절대 위치를 박지 않는다.</b> <c>WorldToScreenPoint</c> 는 카메라의 픽셀 크기를
        /// 쓰는데 그 값이 실행 환경마다 다르다 — 여기서 확인할 것은 «배선이 이어지는가» 이고,
        /// 계산 자체는 <c>PanelAnchorMathTests</c> 가 EditMode 로 덮는다.
        /// </para>
        /// <para>
        /// <b>«움직였다» 만 보지 않는다.</b> 상수를 대입하는 구현도 그건 통과한다 — 오른쪽
        /// 손님이 더 오른쪽에 뜬다는 것까지 함께 본다.
        /// </para>
        /// </summary>
        [Test]
        public void AnchorTo_DifferentCustomers_LandAtDifferentPlaces()
        {
            var view = NewAnchoredView(out _);

            view.AnchorTo(new Vector3(-4f, -2f, 0f));
            var left = ((RectTransform)view.transform).anchoredPosition;

            view.AnchorTo(new Vector3(4f, -2f, 0f));
            var right = ((RectTransform)view.transform).anchoredPosition;

            Assert.AreNotEqual(left, right, "좌표가 달라도 같은 자리에 뜬다");
            Assert.Less(left.x, right.x, "오른쪽 손님인데 창이 더 왼쪽에 떴다");
        }

        /// <summary>
        /// 화면 밖 손님을 눌러도 창은 <b>부모 사각형 안</b>에 남는다. 가장자리 자리가 실제로
        /// 이 경우다 — 씬의 오른쪽 끝 자리는 화면비에 따라 잘린다.
        /// </summary>
        [Test]
        public void AnchorTo_FarOutsideWorldPoint_KeepsThePanelInside()
        {
            var view = NewAnchoredView(out var parent);
            var rect = (RectTransform)view.transform;

            view.AnchorTo(new Vector3(500f, 500f, 0f));

            var min = rect.anchoredPosition - Vector2.Scale(rect.pivot, rect.rect.size);
            var max = min + rect.rect.size;

            Assert.GreaterOrEqual(min.x, parent.rect.xMin, "창이 왼쪽으로 새 나갔다");
            Assert.GreaterOrEqual(min.y, parent.rect.yMin, "창이 아래로 새 나갔다");
            Assert.LessOrEqual(max.x, parent.rect.xMax, "창이 오른쪽으로 새 나갔다");
            Assert.LessOrEqual(max.y, parent.rect.yMax, "창이 위로 새 나갔다");
        }

        /// <summary>
        /// 씬에서 이 창은 <b>꺼진 채로</b> 시작한다. 꺼진 오브젝트의 <c>Awake</c> 는 켜질
        /// 때까지 오지 않으므로, 자리를 잡는 일이 거기에만 기대면 첫 열기가 어긋난다.
        /// </summary>
        [Test]
        public void AnchorTo_WhileInactive_StillMovesThePanel()
        {
            var view = NewAnchoredView(out _, active: false);
            var before = ((RectTransform)view.transform).anchoredPosition;

            view.AnchorTo(new Vector3(4f, -2f, 0f));

            Assert.AreNotEqual(before, ((RectTransform)view.transform).anchoredPosition,
                               "꺼진 채로는 자리를 못 잡는다 — Awake 순서에 기대고 있다");
        }

        /// <summary>
        /// 창의 아래변이 손님 <b>위</b>에 온다. 간격이 없으면 창이 손님을 그대로 덮어,
        /// 정작 누른 대상이 안 보인다.
        ///
        /// <para>
        /// 손님의 로컬 좌표는 <b>입력</b>이라 테스트가 직접 구한다 — 검증 대상은 그 위로
        /// 띄우는 간격 쪽이다.
        /// </para>
        /// <para>
        /// <b>창을 작게 줄여 둔다.</b> 배치 모드의 화면 크기는 고정이 아니라, 원래 크기로는
        /// 화면에 따라 클램프가 걸려 창이 도로 손님 위를 덮을 수 있다 — 그러면 간격이 있든
        /// 없든 결과가 같아져 이 테스트가 <b>간격을 못 본다.</b> 클램프 쪽은
        /// <see cref="AnchorTo_FarOutsideWorldPoint_KeepsThePanelInside"/> 가 따로 본다.
        /// </para>
        /// </summary>
        [Test]
        public void AnchorTo_Customer_SitsAboveThem()
        {
            var view = NewAnchoredView(out var parent);
            var rect = (RectTransform)view.transform;
            rect.sizeDelta = new Vector2(60f, 80f);

            var world = new Vector3(0f, -2f, 0f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, Camera.main.WorldToScreenPoint(world), null, out var seat);

            view.AnchorTo(world);

            var bottom = rect.anchoredPosition.y - rect.pivot.y * rect.rect.height;

            Assert.Greater(bottom, seat.y, "창이 손님을 덮는다 — 위로 띄우는 간격이 없다");
        }

        /// <summary>
        /// 창은 <b>손님 위로 자란다.</b> 피벗이 위쪽이면 창이 손님을 덮는다 — 배치가 아니라
        /// 동작 계약이라 씬이 아니라 코드가 정한다.
        /// </summary>
        [Test]
        public void AnchorTo_Anywhere_KeepsTheBottomCenterPivot()
        {
            var view = NewAnchoredView(out _);

            view.AnchorTo(new Vector3(0f, -2f, 0f));

            Assert.AreEqual(new Vector2(0.5f, 0f), ((RectTransform)view.transform).pivot);
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
        private CustomerInspectorView NewView(bool active, RectTransform parent = null)
        {
            var root = new GameObject("CustomerInspectorPanel", typeof(RectTransform));
            _garbage.Add(root);
            root.SetActive(active);

            // 부모를 <b>컴포넌트보다 먼저</b> 물린다. 나중에 붙이면 Awake 가 부모 없이 돌아,
            // 거기서 자리를 잡는 코드가 조용히 건너뛰어진다.
            if (parent != null)
            {
                var rect = (RectTransform)root.transform;
                rect.SetParent(parent, false);
                rect.sizeDelta = new Vector2(200f, 280f);
            }

            foreach (var name in new[]
                     {
                         "NameLabel", "StatsLabel",
                         "StateLabel", "SaturationLabel", "RemainingLabel"
                     })
            {
                var child = new GameObject(name);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<TextMeshProUGUI>();
            }

            return root.AddComponent<CustomerInspectorView>();
        }

        /// <summary>
        /// 자리를 잡을 수 있는 모양으로 세운다 — 캔버스 노릇을 할 부모 <c>RectTransform</c> 과
        /// 월드 좌표를 옮길 카메라가 있어야 한다.
        ///
        /// <para>
        /// <b>카메라를 물려 주지 않는다.</b> <c>Camera.main</c> 폴백을 그대로 태운다 —
        /// 씬이 타는 경로가 그쪽이고, 참조를 주입하면 그 분기가 죽어도 초록이 된다
        /// (<c>.claude/rules/tests.md</c> §1).
        /// </para>
        /// <para>
        /// 카메라는 <b>직교 · z −10</b> 이다. 기본 원근 카메라를 원점에 두면 좌표 왕복이
        /// 깨진다.
        /// </para>
        /// </summary>
        private CustomerInspectorView NewAnchoredView(out RectTransform parent, bool active = true)
        {
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            _garbage.Add(camera.gameObject);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.tag = "MainCamera";

            // 진짜 Canvas 를 세운다. RectTransform 만 두면 부모의 <b>월드</b> 크기가 960×540 이
            // 되는데 카메라는 세로 10 유닛만 비추므로, 화면 좌표 → 로컬 변환이 씬과 전혀 다른
            // 배율로 돈다. Overlay 여야 canvas.worldCamera 가 null 인 것도 씬과 같아진다.
            var canvas = new GameObject("Canvas", typeof(RectTransform)).AddComponent<Canvas>();
            _garbage.Add(canvas.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parent = (RectTransform)canvas.transform;

            return NewView(active, parent);
        }
    }
}
