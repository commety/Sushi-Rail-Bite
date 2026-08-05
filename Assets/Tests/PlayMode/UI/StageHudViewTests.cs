using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Scoring;
using SushiDefense.UI;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.UI
{
    /// <summary>
    /// HUD 는 <b>계산하지 않는다.</b> 원장·지갑·배치 서비스가 이미 정한 값을 문자열로 옮기는
    /// 것까지만 검증한다 — 잔액이 모자라 배치가 거부되는 판정은 EditMode 의
    /// <c>CustomerPlacementServiceTests</c> 가 본다.
    /// </summary>
    public sealed class StageHudViewTests
    {
        private const int InitialBudget = 40;
        private const int MaxPlaced = 3;

        private readonly List<GameObject> _objects = new();
        private readonly List<Object> _assets = new();

        private StageConfig _config;
        private CustomerData _customerData;
        private RevenueLedger _revenue;
        private RecruitWallet _wallet;
        private ClaimCoordinator _coordinator;
        private CustomerPlacementService _placement;
        private StageHudView _hud;
        private TextMesh _revenueLabel;

        [SetUp]
        public void SetUp()
        {
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            _assets.Add(_customerData);

            _config = StageConfigTestFactory.Create(beltSpeed: 10f, spawnInterval: 1f,
                                                    beltLength: 50f, spawnSushi: NewSushi());
            _assets.Add(_config);
            StageConfigTestFactory.SetInt(_config, "_maxPlacedCustomers", MaxPlaced);
            StageConfigTestFactory.SetInt(_config, "_initialRecruitBudget", InitialBudget);
            StageConfigTestFactory.SetInt(_config, "_targetRevenue", 1000);

            var belt = new SushiBelt(_config, new SequenceNumberIssuer(),
                                     new SushiPool<SushiItem>(new SushiItemFactory()));
            _revenue = new RevenueLedger();
            _wallet = new RecruitWallet(InitialBudget);
            _coordinator = new ClaimCoordinator(belt, _config, _revenue, _wallet);
            _placement = new CustomerPlacementService(_coordinator, _config,
                                                      new SequenceNumberIssuer(), _wallet);

            _revenueLabel = NewObject("RevenueLabel").AddComponent<TextMesh>();
            _hud = NewObject("Hud").AddComponent<StageHudView>();
            SetLabel(_hud, "_revenueLabel", _revenueLabel);
            _hud.Bind(_revenue, _wallet, _placement, _config);
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator.Dispose();

            foreach (var go in _objects)
            {
                Object.DestroyImmediate(go);
            }

            _objects.Clear();

            foreach (var asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void Bind_Immediately_ShowsCurrentValues()
        {
            // 물리는 순간 현재 값이 보여야 한다. 첫 변화가 일어날 때까지 빈 화면이면 안 된다.
            Assert.AreEqual("매출 0/1000", _hud.RevenueText);
            Assert.AreEqual($"영입 재화 {InitialBudget}", _hud.WalletText);
            Assert.AreEqual($"손님 0/{MaxPlaced}", _hud.PlacementText);
        }

        [Test]
        public void Bind_WritesIntoAssignedLabel()
        {
            Assert.AreEqual("매출 0/1000", _revenueLabel.text);
        }

        [Test]
        public void RevenueChanged_UpdatesText()
        {
            _revenue.Add(350);

            Assert.AreEqual("매출 350/1000", _hud.RevenueText);
            Assert.AreEqual("매출 350/1000", _revenueLabel.text);
        }

        [Test]
        public void BalanceChanged_UpdatesText()
        {
            _wallet.AccrueFrom(300);

            Assert.AreEqual($"영입 재화 {InitialBudget + 30}", _hud.WalletText);
        }

        [Test]
        public void PlacedCustomer_UpdatesPlacementTextOnNextFrame()
        {
            _placement.Place(_customerData, 0, 5f);

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual($"손님 1/{MaxPlaced}", _hud.PlacementText);
        }

        [Test]
        public void Unbind_ThenValueChanges_TextStaysPut()
        {
            var before = _hud.RevenueText;

            _hud.Unbind();
            _revenue.Add(999);

            Assert.AreEqual(before, _hud.RevenueText, "끊은 뒤에는 반응하지 않는다");
        }

        [Test]
        public void Destroyed_ThenValueChanges_NoLongerReceivesThem()
        {
            // 파괴된 뷰가 구독에 남아 있는지를 본다 (rules/scripts.md §6).
            //
            // "예외가 안 난다" 로는 부족하다 — 파괴된 뷰가 계속 구독돼 있어도 문자열
            // 대입은 조용히 성공하고 라벨 쓰기는 Unity 의 null 오버로드에 걸려 넘어간다.
            // 즉 누수가 있어도 아무 소리가 안 난다. 값이 갱신되는지를 직접 봐야 한다.
            var hud = _hud;
            var hudObject = hud.gameObject;
            var before = hud.RevenueText;

            _objects.Remove(hudObject);
            Object.DestroyImmediate(hudObject);
            _revenue.Add(100);

            Assert.AreEqual(before, hud.RevenueText,
                            "파괴된 뷰가 아직 원장을 구독하고 있다 — OnDestroy 에서 끊어야 한다");
        }

        private static void SetLabel(StageHudView hud, string fieldName, TextMesh label)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(hud);
            serialized.FindProperty(fieldName).objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private SushiData NewSushi()
        {
            var sushi = ScriptableObject.CreateInstance<SushiData>();
            _assets.Add(sushi);
            return sushi;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go;
        }
    }
}
