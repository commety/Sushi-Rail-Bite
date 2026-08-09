using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Scoring;
using SushiDefense.Stages;
using SushiDefense.UI;
using TMPro;
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
        private const float TimeLimit = 30f;

        private readonly List<GameObject> _objects = new();
        private readonly List<Object> _assets = new();

        private StageConfig _config;
        private CustomerData _customerData;
        private RevenueLedger _revenue;
        private RecruitWallet _wallet;
        private ClaimCoordinator _coordinator;
        private CustomerPlacementService _placement;
        private StageHudView _hud;
        private StageController _stage;
        private RunProgression _progression;
        private PauseState _pause;
        private TMP_Text _revenueLabel;

        [SetUp]
        public void SetUp()
        {
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            _customerData.name = "Customer.ForHudTest";
            _assets.Add(_customerData);

            // 범위가 0 이면 초밥이 영영 인식되지 않아 대기 판정이 나오지 않는다.
            // 대역은 기본 0~0 이라 덱 초밥(가격 하한 100)이 전부 대역 밖이다.
            SetReach(_customerData, 50f);

            _config = StageConfigTestFactory.Create(beltSpeed: 10f, spawnInterval: 1f,
                                                    beltLength: 50f, spawnSushi: NewSushi());
            _assets.Add(_config);
            StageConfigTestFactory.SetInt(_config, "_maxPlacedCustomers", MaxPlaced);
            StageConfigTestFactory.SetInt(_config, "_initialRecruitBudget", InitialBudget);
            StageConfigTestFactory.SetInt(_config, "_targetRevenue", 1000);
            StageConfigTestFactory.SetFloat(_config, "_timeLimitSeconds", TimeLimit);

            var belt = new SushiBelt(_config, SushiDeck.FromSpawnTable(_config).Cards,
                                  new SequenceNumberIssuer(),
                                     new SushiPool<SushiItem>(new SushiItemFactory()));
            _revenue = new RevenueLedger();
            _wallet = new RecruitWallet(InitialBudget);
            _coordinator = new ClaimCoordinator(belt, _config, _revenue, _wallet);
            _placement = new CustomerPlacementService(_coordinator, _config,
                                                      new SequenceNumberIssuer(), _wallet,
                                                      new SushiDefense.Stages.PauseState());

            _stage = new StageController(_coordinator, _revenue, _config);

            // 스테이지 한 장짜리 런. 단계 표시가 목록에서 나오는지 보려면 진행이 필요하다.
            _progression = new RunProgression(new[] { _config },
                                              new RunState(SushiDeck.FromSpawnTable(_config),
                                                           new CustomerDeck(new[] { _customerData }),
                                                           seed: 1));
            _pause = new PauseState();

            _revenueLabel = NewObject("RevenueLabel").AddComponent<TextMeshPro>();
            _hud = NewObject("Hud").AddComponent<StageHudView>();
            SetLabel(_hud, "_revenueLabel", _revenueLabel);
            _hud.Bind(_revenue, _wallet, _placement, _coordinator, _stage, _progression, _pause);
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
            Assert.AreEqual("대기 0", _hud.WaitingText);
        }

        [Test]
        public void WaitingCustomer_UpdatesWaitingTextOnNextFrame()
        {
            // 손님 대역이 0~0 이라 덱 초밥이 전부 대역 밖이다 → 조율자가 대기로 판정한다.
            _placement.Place(_customerData, 0, 5f);
            _coordinator.Tick(1f);

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual("대기 1", _hud.WaitingText);
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

        /// <summary>
        /// <b>손님 «한 명» 만 앉힌다.</b> 둘을 앉히면 머릿수를 세는 구현으로도 2가 나와
        /// 이 테스트가 공허해진다 (<c>.claude/rules/tests.md</c> §3).
        ///
        /// <para>
        /// 위의 <see cref="PlacedCustomer_UpdatesPlacementTextOnNextFrame"/> 이 대조군이다 —
        /// 인구수 1이면 두 구현이 같은 답을 내므로, 짝으로 있어야 «인구수를 본다» 가 확정된다.
        /// </para>
        /// </summary>
        [Test]
        public void PlacedHeavyCustomer_ShowsPopulationTwo()
        {
            _placement.Place(HeavyCustomer(), 0, 5f);

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual($"손님 2/{MaxPlaced}", _hud.PlacementText);
        }

        /// <summary>
        /// 값이 바뀐 프레임에만 문자열을 만든다 (§4.3). 인구수로 바꾸면서 변화 감지가
        /// 함께 깨지지 않았는지 본다 — 문구가 같은지만 보면 매번 새로 만드는 구현도
        /// 통과하므로 <b>인스턴스</b>를 비교한다.
        /// </summary>
        [Test]
        public void PlacementText_SamePopulationTwice_DoesNotRewrite()
        {
            _placement.Place(HeavyCustomer(), 0, 5f);
            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            var first = _hud.PlacementText;

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.IsTrue(ReferenceEquals(first, _hud.PlacementText),
                          "인구수가 그대로인데 문자열을 새로 만들었다");
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

        // ── 남은 시간 · 결과 (M3) ────────────────────────────────

        [Test]
        public void TimeText_Bind_ShowsFullLimit()
        {
            Assert.AreEqual("남은 시간 30", _hud.TimeText);
        }

        [Test]
        public void TimeText_AfterTick_CountsDown()
        {
            _stage.Tick(5f);

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual("남은 시간 25", _hud.TimeText);
        }

        /// <summary>
        /// 매 프레임 문자열을 만들면 WebGL 에서 GC 스파이크가 그대로 히칭이 된다 (§4.3).
        /// 문구가 같은지만 보면 매번 새로 만드는 구현도 통과하므로 <b>인스턴스</b>를 비교한다.
        /// </summary>
        [Test]
        public void TimeText_SameSecondTwice_DoesNotRewrite()
        {
            _stage.Tick(0.1f);
            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            var first = _hud.TimeText;

            _stage.Tick(0.1f);
            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.IsTrue(ReferenceEquals(first, _hud.TimeText),
                          "같은 초인데 문자열을 새로 만들었다");
        }

        [Test]
        public void OutcomeText_InProgress_IsEmpty()
        {
            Assert.IsEmpty(_hud.OutcomeText);
        }

        [Test]
        public void OutcomeText_Cleared_ShowsClear()
        {
            _revenue.Add(1000);
            _stage.Tick(0.1f);

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual("클리어", _hud.OutcomeText);
        }

        [Test]
        public void OutcomeText_Failed_ShowsFail()
        {
            _stage.Tick(TimeLimit);

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual("실패", _hud.OutcomeText);
        }

        /// <summary>
        /// 멈춤도 같은 배너를 쓴다. 실패·클리어와 한 자리를 나눠 쓰므로 라벨을 셋으로
        /// 두면 동시에 뜰 때 겹친다.
        /// </summary>
        [Test]
        public void OutcomeText_Paused_ShowsPaused()
        {
            _pause.Pause();

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual("일시정지", _hud.OutcomeText);
        }

        [Test]
        public void OutcomeText_ResumedAfterPause_ClearsBanner()
        {
            _pause.Pause();
            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            _pause.Resume();
            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual(string.Empty, _hud.OutcomeText);
        }

        /// <summary>
        /// 실패한 판은 멈춘 판이기도 하다. 그때 알려야 하는 것은 «멈췄다» 가 아니라
        /// «졌다» 이므로 <b>결과가 멈춤을 이긴다.</b>
        /// </summary>
        [Test]
        public void OutcomeText_FailedWhilePaused_ShowsFail()
        {
            _stage.Tick(TimeLimit);
            _pause.Pause();

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual("실패", _hud.OutcomeText);
        }

        /// <summary>
        /// 단계는 <b>런의 진행 순서</b>에서 온다 — 스테이지 설정의 표시 번호가 아니다.
        /// 순서의 진실은 목록이다 (<c>RunProgression</c>).
        /// </summary>
        [Test]
        public void StageText_FreshRun_ShowsFirstStage()
        {
            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual("스테이지 1", _hud.StageText);
        }

        /// <summary>
        /// 런이 끝나면 진행 번호가 총수를 넘는다. 그대로 쓰면 «스테이지 2» 인 한 장짜리
        /// 런이 나온다 — 총수에서 자른다.
        /// </summary>
        [Test]
        public void StageText_RunComplete_DoesNotExceedStageCount()
        {
            _progression.AdvanceAfterClear();

            _hud.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual("스테이지 1", _hud.StageText);
        }

        /// <summary>
        /// 인구수 2인 손님. <see cref="_customerData"/> 를 재활용하지 않는 이유는 그것이
        /// 스위트 전체가 공유하는 «인구수 1» 대조군이기 때문이다.
        /// </summary>
        private CustomerData HeavyCustomer()
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            data.name = "Customer.Heavy";
            _assets.Add(data);

            SetReach(data, 50f);
            SetPopulation(data, 2);
            return data;
        }

        private static void SetPopulation(CustomerData data, int population)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(data);
            serialized.FindProperty("_population").intValue = population;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private static void SetReach(CustomerData data, float reach)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(data);
            serialized.FindProperty("_reach").floatValue = reach;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }


        /// <summary>
        /// 인스펙터에 라벨을 안 물린 채로도 <b>이름으로 찾아 채우는지</b> 본다.
        ///
        /// <para>
        /// 이 경로는 씬 조립이 참조를 일일이 물리지 않아도 되게 하려고 있는데, 다른 테스트는
        /// 전부 라벨을 직접 주입해서 <b>한 번도 밟지 않는다</b> — 주입으로 확인했다. 경로가
        /// 조용히 죽으면 씬에서만 라벨이 비고, 코드 하네스는 전부 초록이다
        /// (<c>.claude/rules/tests.md</c> §1).
        /// </para>
        /// </summary>
        [Test]
        public void Awake_LabelNotAssigned_ResolvesChildByName()
        {
            var host = NewObject("Hud");
            var child = NewObject("RevenueLabel");
            child.transform.SetParent(host.transform, false);
            var label = child.AddComponent<TextMeshPro>();

            // 자식이 먼저 있어야 한다 — AddComponent 가 곧바로 Awake 를 부른다.
            var hud = host.AddComponent<StageHudView>();
            hud.Bind(_revenue, _wallet, _placement, _coordinator, _stage, _progression, _pause);

            Assert.AreEqual(hud.RevenueText, label.text);
        }

        private static void SetLabel(StageHudView hud, string fieldName, TMP_Text label)
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
