using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Scoring;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 뷰는 상태를 <b>읽기만</b> 한다. 전이는 <c>CustomerAppetiteMachine</c> 이, "기다리는 중"
    /// 인지는 <c>ClaimCoordinator</c> 가 정하고 그 판정 자체는 EditMode 가 검증한다 —
    /// 여기서는 화면 반영만 본다.
    /// </summary>
    public sealed class CustomerViewTests
    {
        /// <summary>덱 초밥 가격. 손님 대역을 이 값 밖에 두면 조율자가 대기로 판정한다.</summary>
        private const int DeckPrice = 200;

        private readonly List<GameObject> _objects = new();
        private readonly List<Object> _assets = new();

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
            _assets.Add(_customerData);
            SetBand(100, 300);

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

            foreach (var asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void Bind_Idle_ShowsIdleImmediately()
        {
            _view.Bind(_logic, null);

            Assert.AreEqual(CustomerState.Idle, _view.ShownState);
        }

        [Test]
        public void Bind_CustomerWithIcon_ShowsThatSprite()
        {
            var icon = NewSprite();

            SetIcon(icon);
            _view.Bind(_logic, null);

            Assert.AreSame(icon, _view.ShownSprite);
        }

        [Test]
        public void Bind_CustomerWithoutIcon_KeepsPrefabSprite()
        {
            var prefabSprite = NewSprite();
            _body.sprite = prefabSprite;

            _view.Bind(_logic, null);

            // 손님이 화면에서 사라지면 자리가 비어 보인다 — 아이콘을 아직 안 채운
            // 애셋에서도 그림은 남아야 한다.
            Assert.AreSame(prefabSprite, _view.ShownSprite);
        }

        /// <summary>
        /// 그림과 상태 색은 다른 채널이다. 유형을 색으로 구분하면 상태 색과 싸우므로
        /// 유형은 <b>실루엣</b>이 맡고 색은 상태가 그대로 쓴다 (M5).
        /// </summary>
        [Test]
        public void Bind_CustomerWithIcon_StillAppliesStateTint()
        {
            SetIcon(NewSprite());

            _view.Bind(_logic, null);

            Assert.AreEqual(Color.white, _body.color, "Idle 은 틴트 없이 흰색이다");
        }

        /// <summary>
        /// 라벨은 <b>이름과 대역을 함께</b> 보여 준다. 유형이 셋으로 늘면 대역 숫자만으로는
        /// "이게 소식인가 먹보인가" 를 매번 역산해야 한다 (M4).
        /// </summary>
        [Test]
        public void Bind_Customer_ShowsNameAndTargetingBand()
        {
            SetDisplayName("소식");
            SetBand(300, 550);

            _view.Bind(_logic, null);

            StringAssert.Contains("소식", _view.BandText);
            StringAssert.Contains("300~550", _view.BandText);
        }

        /// <summary>
        /// 다른 유형으로도 <b>자기 값</b>을 쓰는지 본다 — 위 테스트만 있으면 상수를
        /// 돌려주는 구현이 통과한다.
        /// </summary>
        [Test]
        public void Bind_BigEaterData_ShowsItsOwnNameAndBand()
        {
            SetDisplayName("먹보");
            SetBand(100, 150);

            _view.Bind(_logic, null);

            StringAssert.Contains("먹보", _view.BandText);
            StringAssert.Contains("100~150", _view.BandText);
            StringAssert.DoesNotContain("소식", _view.BandText);
        }

        /// <summary>
        /// 이름을 아직 안 채운 애셋에서 빈 줄이 나오면 라벨이 고장 난 것처럼 보인다.
        /// 애셋 이름으로 대신한다.
        /// </summary>
        [Test]
        public void Bind_DataWithoutDisplayName_FallsBackToAssetName()
        {
            _customerData.name = "Customer.Nameless";
            SetDisplayName(string.Empty);
            SetBand(100, 150);

            _view.Bind(_logic, null);

            StringAssert.Contains("Customer.Nameless", _view.BandText);
        }

        [Test]
        public void Bind_Null_ClearsBandText()
        {
            _view.Bind(_logic, null);

            _view.Bind(null, null);

            Assert.IsEmpty(_view.BandText);
        }

        [Test]
        public void LateUpdate_StateBecameEating_ReflectsIt()
        {
            _view.Bind(_logic, null);
            _appetite.BeginEating(1);

            Pump();

            Assert.AreEqual(CustomerState.Eating, _view.ShownState);
        }

        [Test]
        public void LateUpdate_EatingAndDigesting_UseDifferentColors()
        {
            // 색 값 자체는 인스펙터 튜닝 대상이라 고정하지 않는다. 세 상태가 서로
            // 구분된다는 것만 본다 — 같으면 화면에서 상태를 읽을 수 없다.
            _view.Bind(_logic, null);
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
        public void LateUpdate_WaitingCustomer_ShowsDistinctColor()
        {
            // 조율자를 실제로 돌려 대기 판정을 만든다. 밖에서 플래그를 세우면 뷰가
            // 실제로 만날 일 없는 조합을 검증하게 된다.
            SetBand(400, 500);
            using var stage = new Stage(_assets, _customerData);
            var waiting = stage.PlaceAt(0f);
            _view.Bind(waiting, stage.Coordinator);
            var idle = _body.color;

            stage.Coordinator.Tick(1f);
            Pump();

            Assert.IsTrue(stage.Coordinator.IsWaiting(waiting), "전제: 조율자가 대기로 판정했다");
            Assert.IsTrue(_view.ShownWaiting);
            Assert.AreNotEqual(idle, _body.color, "대기가 대기 아님과 구분되지 않는다");
        }

        [Test]
        public void LateUpdate_EatingCustomer_IsNotShownWaiting()
        {
            // 먹는 중이 대기보다 정보가 크다. 뒤집히면 화면이 계속 '기다리는 중' 이 된다.
            SetBand(100, 300);
            using var stage = new Stage(_assets, _customerData);
            var eater = stage.PlaceAt(0f);
            _view.Bind(eater, stage.Coordinator);

            stage.Coordinator.Tick(1f);
            Pump();

            Assert.IsFalse(_view.ShownWaiting);
        }

        [Test]
        public void LateUpdate_NothingChanged_LeavesColorAlone()
        {
            // 변화 감지가 실제로 작동하는지 본다. ShownWaiting 만 보면 매 프레임 다시
            // 써도 값이 같아 통과한다 — 색을 밖에서 오염시켜 두고 복구되지 않음을 본다.
            _view.Bind(_logic, null);
            _body.color = Color.magenta;

            Pump();

            Assert.AreEqual(Color.magenta, _body.color, "바뀌지 않았는데 다시 썼다");
        }

        [Test]
        public void LateUpdate_NoLogicBound_DoesNotThrow()
        {
            Assert.DoesNotThrow(Pump);
        }

        [Test]
        public void Bind_NullCoordinator_DoesNotThrow()
        {
            _view.Bind(_logic, null);

            Assert.DoesNotThrow(Pump);
            Assert.IsFalse(_view.ShownWaiting);
        }

        /// <summary>뷰의 프레임 갱신을 한 번 돌린다.</summary>
        private void Pump() =>
            _view.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

        /// <summary>
        /// 대역만 건드린다. <b>최대 포화도를 함께 손대지 않는다</b> — 기본값 1 이라야
        /// 한 입에 포화돼 Eating → Digesting 전이를 한 틱으로 볼 수 있다.
        /// </summary>
        private void SetDisplayName(string displayName)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(_customerData);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private Sprite NewSprite()
        {
            var texture = new Texture2D(1, 1);
            _assets.Add(texture);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _assets.Add(sprite);
            return sprite;
        }

        /// <summary>
        /// 아이콘을 밀어 넣는다. <c>SerializedObject</c> 대신 리플렉션을 쓰는 이유는 그쪽이
        /// <c>UnityEditor</c> 의존이라 플레이어에서 조용히 아무 일도 하지 않기 때문이다 —
        /// 테스트가 통과한 채로 검증을 잃는 형태가 된다.
        /// </summary>
        private void SetIcon(Sprite icon)
        {
            var field = typeof(CustomerData).GetField(
                "_icon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, "직렬화 필드 '_icon' 을 찾지 못했습니다 — 이름이 바뀌었는지 확인하세요.");
            field.SetValue(_customerData, icon);
        }

        private void SetBand(int min, int max)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(_customerData);
            serialized.FindProperty("_targetingMin").intValue = min;
            serialized.FindProperty("_targetingMax").intValue = max;

            // 범위가 0 이면 초밥이 영영 인식되지 않아 대기 판정 자체가 나오지 않는다.
            serialized.FindProperty("_reach").floatValue = 50f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go;
        }

        /// <summary>대기 판정을 실제 경로로 만들기 위한 최소 무대.</summary>
        private sealed class Stage : System.IDisposable
        {
            private readonly CustomerData _data;
            private int _nextSequence;

            public ClaimCoordinator Coordinator { get; }

            public Stage(List<Object> assets, CustomerData data)
            {
                _data = data;

                var sushi = ScriptableObject.CreateInstance<SushiData>();
                assets.Add(sushi);
#if UNITY_EDITOR
                var serialized = new UnityEditor.SerializedObject(sushi);
                serialized.FindProperty("_price").intValue = DeckPrice;
                serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

                var config = StageConfigTestFactory.Create(beltSpeed: 10f, spawnInterval: 1f,
                                                           beltLength: 100f, spawnSushi: sushi);
                assets.Add(config);

                var belt = new SushiBelt(config, SushiDeck.FromSpawnTable(config).Cards,
                                  new SequenceNumberIssuer(),
                                         new SushiPool<SushiItem>(new SushiItemFactory()));
                Coordinator = new ClaimCoordinator(belt, config, new RevenueLedger(),
                                                   new RecruitWallet(0));
            }

            public CustomerLogic PlaceAt(float beltPosition)
            {
                var logic = new CustomerLogic(new CustomerRuntimeState(_data, _nextSequence++),
                                              beltPosition);
                Coordinator.PlaceCustomer(logic);
                return logic;
            }

            public void Dispose() => Coordinator.Dispose();
        }
    }
}
