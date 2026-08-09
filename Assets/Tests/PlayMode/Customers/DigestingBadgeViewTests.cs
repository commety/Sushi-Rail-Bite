using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using TMPro;
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

            badge.Show(3);
            Assert.IsTrue(badge.IsShowing);

            badge.Hide();
            Assert.IsFalse(badge.IsShowing);
        }

        /// <summary>
        /// 프로퍼티와 <b>실제 라벨</b>을 함께 본다. 프로퍼티만 보면 글자를 어디에도 쓰지 않는
        /// 구현이 통과하는데, 그것이 방금 <c>BandLabel</c> 에서 실제로 났던 사고다.
        /// </summary>
        [Test]
        public void Show_WithSeconds_WritesTheNumber()
        {
            var badge = NewBadge();
            var label = badge.GetComponentInChildren<TMP_Text>(true);

            badge.Show(3);

            Assert.AreEqual("3", badge.RemainingText);
            Assert.AreEqual("3", label.text, "라벨에 안 쓰였다 — 화면에는 안 나온다");
        }

        /// <summary>
        /// 남은 글자가 지워지지 않으면 <b>다음 소화가 시작될 때 직전 숫자가 한 프레임
        /// 번쩍인다</b> — 풀에서 그림을 되돌리지 않을 때와 같은 형태다.
        /// </summary>
        [Test]
        public void Hide_AfterShow_ClearsTheNumber()
        {
            var badge = NewBadge();
            badge.Show(3);

            badge.Hide();

            Assert.IsEmpty(badge.RemainingText);
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

        // ── 남은 초 ──────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator CustomerView_Digesting_ShowsRemainingSeconds()
        {
            var view = NewCustomerView(out var badge, out _);
            var logic = NewLogic();

            view.Bind(logic, null);
            logic.State.State = CustomerState.Digesting;
            logic.State.RemainingDigestSeconds = 2.4f;
            yield return null;

            // 올림이다. 0.3초 남았을 때 «0» 보다 «1» 이 낫고, 실제로 0 이 되는 순간은
            // 소화가 끝나는 시점뿐이다 (StageHudView.RefreshTime 과 같은 판단).
            Assert.AreEqual("3", badge.RemainingText);
        }

        /// <summary>
        /// 값이 바뀐 프레임에만 쓴다 (<c>CLAUDE.md</c> §4.3). <b>같은 초 안에서 시간이
        /// 흐르는</b> 상황을 골라야 «매 프레임 새로 만드는» 구현이 걸린다.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomerView_SameSecondTwice_DoesNotRewrite()
        {
            var view = NewCustomerView(out var badge, out _);
            var logic = NewLogic();

            view.Bind(logic, null);
            logic.State.State = CustomerState.Digesting;
            logic.State.RemainingDigestSeconds = 2.9f;
            yield return null;
            var first = badge.RemainingText;

            logic.State.RemainingDigestSeconds = 2.1f;
            yield return null;

            Assert.IsTrue(ReferenceEquals(first, badge.RemainingText),
                          "같은 초인데 문자열을 새로 만들었다");
        }

        /// <summary>
        /// 위와 짝이다. 하나만 두면 <b>아예 갱신하지 않는</b> 구현도 통과한다.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomerView_SecondCrossed_Rewrites()
        {
            var view = NewCustomerView(out var badge, out _);
            var logic = NewLogic();

            view.Bind(logic, null);
            logic.State.State = CustomerState.Digesting;
            logic.State.RemainingDigestSeconds = 2.9f;
            yield return null;

            logic.State.RemainingDigestSeconds = 1.4f;
            yield return null;

            Assert.AreEqual("2", badge.RemainingText);
        }

        /// <summary>
        /// <b>소화를 두 번 돌린다.</b> 한 번만 돌리면 캐시를 되돌리지 않는 구현이 통과하는데,
        /// 그 증상은 «두 번째 소화부터 숫자가 안 뜬다» 라 눈으로만 드러난다 — 이 프로젝트가
        /// 반복해 겪은 «두 번째부터/가끔» 형태다
        /// (<c>.claude/knowledge/unity-scripting-gotchas.md</c> §5).
        /// </summary>
        [UnityTest]
        public IEnumerator CustomerView_SecondDigestion_ShowsTheNumberAgain()
        {
            var view = NewCustomerView(out var badge, out _);
            var logic = NewLogic();

            view.Bind(logic, null);
            logic.State.State = CustomerState.Digesting;
            logic.State.RemainingDigestSeconds = 3f;
            yield return null;

            logic.State.State = CustomerState.Idle;
            logic.State.RemainingDigestSeconds = 0f;
            yield return null;

            logic.State.State = CustomerState.Digesting;
            logic.State.RemainingDigestSeconds = 3f;
            yield return null;

            Assert.AreEqual("3", badge.RemainingText, "두 번째 소화에서 숫자가 안 그려졌다");
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
            AddRemainingLabel(go);
            return go.AddComponent<DigestingBadgeView>();
        }

        /// <summary>
        /// 배지 안의 숫자 라벨. <b>이름이 계약이다</b> — <c>DigestingBadgeView</c> 가 인스펙터가
        /// 비면 자기 하위에서 이 이름으로 찾는다. 애셋 쪽 존재 여부는 <c>CustomerPrefabTests</c>
        /// 가 따로 본다 (코드 하네스는 프리팹을 안 보므로).
        /// </summary>
        private static TMP_Text AddRemainingLabel(GameObject badge)
        {
            var go = new GameObject("RemainingLabel");
            go.transform.SetParent(badge.transform, false);
            return go.AddComponent<TextMeshPro>();
        }

        private CustomerView NewCustomerView(out DigestingBadgeView badge, out SaturationBarView bar)
        {
            var root = new GameObject("Customer");
            _garbage.Add(root);
            root.AddComponent<SpriteRenderer>();

            var badgeGo = new GameObject("DigestingBadge");
            badgeGo.transform.SetParent(root.transform, false);
            badgeGo.AddComponent<SpriteRenderer>();
            AddRemainingLabel(badgeGo);
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
