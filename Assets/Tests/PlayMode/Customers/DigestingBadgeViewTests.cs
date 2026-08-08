using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 소화중 배지는 <b>상태를 모른다.</b> <c>Show</c>/<c>Hide</c> 만 받는다 — 뷰가 상태를
    /// 되물으면 상태 머신의 진실이 둘이 된다 (<c>CLAUDE.md</c> §3.2·§3.5).
    ///
    /// <para>
    /// 그래서 배지 자체의 테스트는 짧고, 실제로 확인해야 할 것은 <b>어떤 상태에서 뜨는가</b>다.
    /// 대기(파란 틴트)와 소화(회색 틴트)는 둘 다 "지금 안 먹는 상태" 라 구현에서 뭉뚱그리기
    /// 쉬우므로 그 경계를 여기서 고정한다.
    /// </para>
    /// </summary>
    public sealed class DigestingBadgeViewTests
    {
        private readonly List<Object> _garbage = new();

        [Test]
        public void Fresh_IsHidden()
        {
            var badge = NewBadge();

            Assert.IsFalse(badge.IsShowing);
        }

        [Test]
        public void Show_ThenHide_TogglesVisibility()
        {
            var badge = NewBadge();

            badge.Show();
            Assert.IsTrue(badge.IsShowing);

            badge.Hide();
            Assert.IsFalse(badge.IsShowing);
        }

        /// <summary>
        /// 배지가 <c>CustomerState</c> 를 참조하면 판정이 두 곳에 살게 된다. 타입 이름이
        /// 소스에 없다는 것으로 그 계약을 고정한다 — 완료 판정의 <c>grep</c> 과 같은 것을
        /// 본다.
        /// </summary>
        [Test]
        public void Badge_DoesNotDependOnCustomerState()
        {
            var fields = typeof(DigestingBadgeView).GetFields(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public);

            foreach (var field in fields)
            {
                Assert.AreNotEqual(typeof(CustomerState), field.FieldType,
                                   $"배지가 상태를 들고 있다: {field.Name}");
            }
        }

        // ── 어떤 상태에서 뜨는가 ────────────────────────────────────────

        [UnityTest]
        public IEnumerator CustomerView_Digesting_ShowsBadge()
        {
            var view = NewCustomerView(out var badge, out _);
            var logic = NewLogic();

            view.Bind(logic, null);
            logic.State.State = CustomerState.Digesting;
            yield return null;

            Assert.IsTrue(badge.IsShowing);
        }

        [UnityTest]
        public IEnumerator CustomerView_DigestingEnds_HidesBadge()
        {
            var view = NewCustomerView(out var badge, out _);
            var logic = NewLogic();

            view.Bind(logic, null);
            logic.State.State = CustomerState.Digesting;
            yield return null;

            logic.State.State = CustomerState.Idle;
            yield return null;

            Assert.IsFalse(badge.IsShowing);
        }

        /// <summary>
        /// <b>먹는 중에는 뜨지 않는다.</b> 소화와 먹기를 뭉뚱그리면 배지가 거의 항상 떠 있어
        /// 아무것도 알려 주지 않게 된다.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomerView_Eating_DoesNotShowBadge()
        {
            var view = NewCustomerView(out var badge, out _);
            var logic = NewLogic();

            view.Bind(logic, null);
            logic.State.State = CustomerState.Eating;
            yield return null;

            Assert.IsFalse(badge.IsShowing);
        }

        /// <summary>
        /// 자리를 갈아탈 때 직전 손님의 배지가 남으면, 방금 앉은 손님이 소화 중인 것처럼 보인다.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomerView_Rebind_ResetsBadgeAndBar()
        {
            var view = NewCustomerView(out var badge, out var bar);
            var logic = NewLogic();

            view.Bind(logic, null);
            logic.State.State = CustomerState.Digesting;
            logic.State.CurrentSaturation = 4;
            yield return null;

            view.Bind(null, null);

            Assert.IsFalse(badge.IsShowing);
            Assert.AreEqual(0, bar.ShownVisibleCells);
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

        private DigestingBadgeView NewBadge()
        {
            var go = new GameObject("DigestingBadge");
            _garbage.Add(go);
            go.AddComponent<SpriteRenderer>();
            return go.AddComponent<DigestingBadgeView>();
        }

        private CustomerView NewCustomerView(out DigestingBadgeView badge, out SaturationBarView bar)
        {
            var root = new GameObject("Customer");
            _garbage.Add(root);
            root.AddComponent<SpriteRenderer>();

            var badgeGo = new GameObject("DigestingBadge");
            badgeGo.transform.SetParent(root.transform, false);
            badgeGo.AddComponent<SpriteRenderer>();
            badge = badgeGo.AddComponent<DigestingBadgeView>();

            var barGo = new GameObject("SaturationBar");
            barGo.transform.SetParent(root.transform, false);
            var cells = new SpriteRenderer[8];
            for (var i = 0; i < cells.Length; i++)
            {
                var cell = new GameObject($"Cell{i}");
                cell.transform.SetParent(barGo.transform, false);
                cells[i] = cell.AddComponent<SpriteRenderer>();
            }

            bar = barGo.AddComponent<SaturationBarView>();
            bar.InitializeCells(cells);

            return root.AddComponent<CustomerView>();
        }

        private CustomerLogic NewLogic()
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _garbage.Add(data);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(data);
            serialized.FindProperty("_maxSaturation").intValue = 5;
            serialized.FindProperty("_reach").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return new CustomerLogic(new CustomerRuntimeState(data, 0), 0f);
        }
    }
}
