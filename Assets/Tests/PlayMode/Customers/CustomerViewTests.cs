using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 뷰는 상태를 <b>읽기만</b> 한다. 전이는 <c>CustomerAppetiteMachine</c> 이 정하고
    /// 그 전이 자체는 EditMode 가 검증한다 — 여기서는 화면 반영만 본다.
    /// </summary>
    public sealed class CustomerViewTests
    {
        private readonly List<GameObject> _objects = new();

        private CustomerData _customerData;
        private CustomerView _view;
        private SpriteRenderer _body;
        private CustomerLogic _logic;
        private CustomerAppetiteMachine _appetite;

        [SetUp]
        public void SetUp()
        {
            // 기본값 그대로면 최대 포화도 1 · 먹는 시간 0 · 소화 시간 0 이라,
            // 한 입 먹고 한 틱씩 흘리면 세 상태를 모두 지난다.
            _customerData = ScriptableObject.CreateInstance<CustomerData>();

            var go = NewObject("Customer");
            _body = go.AddComponent<SpriteRenderer>();
            _view = go.AddComponent<CustomerView>();

            var state = new CustomerRuntimeState(_customerData, 0);
            _logic = new CustomerLogic(state, 0f);

            // 상태를 밖에서 쓰지 않는다 — 전이는 상태 머신의 소유이고,
            // 직접 대입하면 뷰가 실제로 만날 일 없는 상태 조합을 검증하게 된다.
            _appetite = new CustomerAppetiteMachine(state);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                Object.DestroyImmediate(go);
            }

            _objects.Clear();
            Object.DestroyImmediate(_customerData);
        }

        [Test]
        public void Bind_Idle_ShowsIdleImmediately()
        {
            _view.Bind(_logic);

            Assert.AreEqual(CustomerState.Idle, _view.ShownState);
        }

        [Test]
        public void LateUpdate_StateBecameEating_ReflectsIt()
        {
            _view.Bind(_logic);
            _appetite.BeginEating(1);

            Pump();

            Assert.AreEqual(CustomerState.Eating, _view.ShownState);
        }

        [Test]
        public void LateUpdate_EatingAndDigesting_UseDifferentColors()
        {
            // 색 값 자체는 인스펙터 튜닝 대상이라 고정하지 않는다. 세 상태가 서로
            // 구분된다는 것만 본다 — 같으면 화면에서 상태를 읽을 수 없다.
            _view.Bind(_logic);
            var idle = _body.color;

            _appetite.BeginEating(1);
            Pump();
            var eating = _body.color;

            // 한 입에 포화되므로 먹기가 끝나면 소화로 넘어간다.
            _appetite.Tick(1f);
            Pump();
            var digesting = _body.color;

            Assert.AreEqual(CustomerState.Digesting, _view.ShownState,
                            "전제 조건 — 세 상태를 실제로 지나야 색 비교가 의미를 갖는다");

            Assert.AreNotEqual(idle, eating);
            Assert.AreNotEqual(eating, digesting);
            Assert.AreNotEqual(idle, digesting);
        }

        [Test]
        public void LateUpdate_NoLogicBound_DoesNotThrow()
        {
            Assert.DoesNotThrow(Pump);
        }

        /// <summary>뷰의 프레임 갱신을 한 번 돌린다.</summary>
        private void Pump() =>
            _view.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go;
        }
    }
}
