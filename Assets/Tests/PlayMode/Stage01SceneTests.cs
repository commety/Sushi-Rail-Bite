using System.Collections;
using NUnit.Framework;
using SushiDefense;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Run;
using UnityEngine;
using UnityEngine.TestTools;

namespace SushiDefense.Tests.PlayMode
{
    /// <summary>
    /// 실제 <c>Stage01.unity</c> 를 재생해 M1 완료 판정을 확인한다.
    ///
    /// <para>
    /// 다른 통합 테스트는 구성을 코드로 세우지만, 여기서는 <b>씬 애셋 자체</b>가 제대로 물려
    /// 있는지가 검증 대상이다 — 참조가 빠지면 코드가 멀쩡해도 재생했을 때 아무 일도 안 난다.
    /// </para>
    /// <para>
    /// 씬을 Build Settings 에 등록하지 않고 경로로 연다. 등록은 <c>ProjectSettings</c> 수정이라
    /// 사람 승인 사항이다 (RULE-06).
    /// </para>
    /// </summary>
    public sealed class Stage01SceneTests
    {
        private const string ScenePath = "Assets/Level/Scenes/Stage01.unity";

        private StageBootstrap _stage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            var parameters = new UnityEngine.SceneManagement.LoadSceneParameters(
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, parameters);
            yield return null;

            _stage = Object.FindAnyObjectByType<StageBootstrap>();
            Assert.IsNotNull(_stage, $"{ScenePath} 에 StageBootstrap 이 없다");
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator Play_Scene_BuildsRuntimeGraph()
        {
            yield return null;

            Assert.IsNotNull(_stage.Belt, "벨트가 조립되지 않았다 — StageConfig 참조를 확인하라");
            Assert.IsNotNull(_stage.Coordinator);
            Assert.IsNotNull(_stage.Placement);
            Assert.IsNotNull(_stage.Run, "런 상태가 조립되지 않았다");
            Assert.IsNotNull(_stage.Stage, "스테이지 컨트롤러가 조립되지 않았다");
        }

        /// <summary>
        /// <b>씬의 명부가 비어 있으면 아무도 앉힐 수 없다.</b> 다른 씬 테스트는 자기가 들고 온
        /// <c>CustomerData</c> 로 직접 배치하므로 이 구멍을 지나친다 — 실제 플레이 경로는
        /// 배치 껍데기의 명부를 타므로 그쪽을 본다.
        ///
        /// <para>
        /// 직렬화 필드 이름이 바뀌면 Unity 가 옛 값을 조용히 버린다. 그때 코드도 테스트도
        /// 멀쩡한데 씬만 죽으므로, 이 테스트가 그 사고를 잡는다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_HasPlaceableCustomerInRoster()
        {
            yield return null;

            Assert.Greater(_stage.Run.Customers.Count, 0,
                           "씬의 손님 명부가 비었다 — StageBootstrap 의 시작 손님 참조를 확인하라");

            var controller = Object.FindAnyObjectByType<CustomerPlacementController>();
            Assert.IsNotNull(controller, "씬에 배치 껍데기가 없다");
            Assert.IsNotNull(controller.PendingCustomer,
                             "배치 예정 손님이 없다 — 명부가 껍데기에 물리지 않았다");
        }

        /// <summary>
        /// 보상 화면은 <b>카탈로그와 뷰가 둘 다 씬에 물려야</b> 선다. 카탈로그는 애셋이라
        /// 자동 해석 대상이 아니므로 인스펙터 참조가 빠지면 조용히 <c>null</c> 이 되고,
        /// 클리어해도 아무 화면이 안 뜬다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_HasRewardScreenWired()
        {
            yield return null;

            Assert.IsNotNull(_stage.Rewards,
                             "보상 프레젠터가 없다 — RewardCatalog 또는 RewardSelectionView 참조를 확인하라");
        }

        /// <summary>
        /// 보상으로 줄 수 있는 카드가 실제로 있는지 본다. 카탈로그가 물려 있어도 미보유
        /// 카드가 0개면 화면이 늘 "받을 보상 없음" 이라, 코드는 멀쩡한데 기능이 죽는다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_RewardPoolHasUnownedCards()
        {
            yield return null;

            _stage.Rewards.Open(_stage.Run);

            Assert.Greater(_stage.Rewards.OfferCount, 0,
                           "제시할 보상이 없다 — 시작 덱과 보상 풀이 완전히 겹친다");

            // 건너뛰기가 런을 건드리지 않는다는 것은 EditMode 가 검증한다. 여기서는
            // 화면을 열어 둔 채 테스트가 끝나지 않도록 닫기만 한다.
            _stage.Rewards.Skip();
        }

        [UnityTest]
        public IEnumerator Play_Scene_SpawnsSushiOntoBelt()
        {
            yield return WaitSeconds(3f);

            Assert.IsNotEmpty(_stage.Belt.ActiveSushi, "재생하면 초밥이 벨트에 오른다");
        }

        [UnityTest]
        public IEnumerator Play_Scene_DeckHasSeveralPricedSushi()
        {
            // 초밥이 1종이면 타겟팅 거리가 전부 같아 M2 의 배정 규칙이 화면에서
            // 관측되지 않는다. 밸런스 애셋이 되돌아가면 여기서 잡는다.
            yield return null;

            var prices = new System.Collections.Generic.HashSet<int>();
            foreach (var entry in _stage.StageConfig.SpawnTable)
            {
                Assert.IsNotNull(entry.Sushi, "덱에 빈 슬롯이 있다");
                prices.Add(entry.Sushi.Price);
            }

            Assert.GreaterOrEqual(prices.Count, 3, "가격이 서로 다른 초밥이 최소 3종은 있어야 한다");
        }

        [UnityTest]
        public IEnumerator Play_Scene_HudShowsStageProgress()
        {
            yield return WaitSeconds(1f);

            Assert.IsNotNull(_stage.Hud, "씬에 HUD 가 없다");
            Assert.IsNotEmpty(_stage.Hud.RevenueText);
            Assert.IsNotEmpty(_stage.Hud.WalletText);
            Assert.IsNotEmpty(_stage.Hud.PlacementText);
            Assert.IsNotEmpty(_stage.Hud.WaitingText, "대기 인원 표시가 없다");
        }

        [UnityTest]
        public IEnumerator Play_Scene_CustomersHaveMeaningfulBands()
        {
            // 대역이 0~0 이면 모든 초밥이 대역 밖 + 폭 0(최강 전문가)이라 배정이 뒤집힌다.
            // 스키마를 바꾸면 Unity 가 옛 필드를 조용히 버리고 새 필드를 0 으로 채우므로,
            // 밸런스 값을 채우는 것을 잊으면 아무 에러 없이 그 상태로 굴러간다.
            yield return null;

            var kinds = new[] { "Customer.Standard", "Customer.SmallEater" };
            var widths = new System.Collections.Generic.List<int>();

            foreach (var kind in kinds)
            {
                var data = LoadCustomer(kind);
                Assert.IsNotNull(data, $"{kind} 애셋이 없다");
                Assert.GreaterOrEqual(data.TargetingMax, 100,
                                      $"{kind} 의 대역 상한이 가격 하한(100) 아래다 — 값이 비었다");
                Assert.LessOrEqual(data.TargetingMin, data.TargetingMax, $"{kind} 의 대역이 뒤집혔다");
                widths.Add(data.TargetingMax - data.TargetingMin);
            }

            // 폭이 같으면 정렬 키 4가 동률이 되어 손님 유형이 경합에서 구분되지 않는다.
            // 그러면 배치 순서가 승자를 정하는, M2.5 가 없애려던 그림으로 되돌아간다.
            Assert.AreNotEqual(widths[0], widths[1],
                               "기본과 소식의 대역 폭이 같다 — 전문가 우선(키 4)이 작동하지 않는다");
        }

        [UnityTest]
        public IEnumerator Play_Scene_SpecialistBandIsNarrowerThanGeneralist()
        {
            // 소식은 좁은 고가대 전문가여야 한다. 상한을 덱 최고가보다 한참 위로 잡으면
            // 아무 일도 하지 않는 구간이 폭만 부풀려, 키 4에서 범용가에게 진다.
            yield return null;

            var generalist = LoadCustomer("Customer.Standard");
            var specialist = LoadCustomer("Customer.SmallEater");

            Assert.Less(specialist.TargetingMax - specialist.TargetingMin,
                        generalist.TargetingMax - generalist.TargetingMin,
                        "소식의 대역이 기본보다 넓다 — 겹치는 가격을 기본에게 빼앗긴다");
        }

        [UnityTest]
        public IEnumerator Play_Scene_ReachStaysInsideBelt()
        {
            // M2.5 가 만든 새 실패 모드를 막는 구성 제약이다.
            //
            // 유예가 도입되면서 손님은 대역 밖 초밥을 '범위를 벗어나기 직전' 까지 기다린다.
            // 그런데 자리의 집기 범위가 벨트 끝을 넘어가면 그 시각이 영영 오지 않는다 —
            // 초밥이 범위를 벗어나기 전에 끝점에서 사라지기 때문이다. 그 손님은 굶는다.
            //
            // 즉시 확정이던 M2 까지는 발생할 수 없던 조건이라 지금 잠근다.
            yield return null;

            var slots = Object.FindObjectsByType<SushiDefense.Customers.TableSlotView>(
                FindObjectsSortMode.None);
            Assert.IsNotEmpty(slots, "전제: 씬에 자리가 있다");

            var reach = DefaultCustomer().Reach;
            foreach (var slot in slots)
            {
                Assert.Less(slot.BeltPosition + reach, _stage.StageConfig.BeltLength,
                            $"자리 {slot.SlotIndex} 의 집기 범위가 벨트 끝을 넘는다 — "
                            + "기다리는 손님이 초밥을 잃는다");
            }
        }

        [UnityTest]
        public IEnumerator Play_Scene_OccupiedSeatShowsBand()
        {
            // 대역이 화면에 보이는지 — 이 마일스톤의 목적이 가독성이므로 선택이 아니다.
            var slot = Object.FindAnyObjectByType<SushiDefense.Customers.TableSlotView>();
            var logic = _stage.Placement.Place(DefaultCustomer(), slot.SlotIndex, slot.BeltPosition);
            slot.Occupy(logic, _stage.Coordinator);

            yield return null;

            Assert.IsNotNull(slot.Occupant, "자리에 손님 시각 표현이 없다 — 씬 조립을 확인하라");
            Assert.IsNotEmpty(slot.Occupant.BandText, "손님 옆에 대역이 표시되지 않는다");
        }

        [UnityTest]
        public IEnumerator Play_Scene_PlacedCustomerEarnsRevenue()
        {
            // M2 완료 판정의 끝 — 실제 씬에서 먹고 매출·재화가 붙는다.
            var slot = Object.FindAnyObjectByType<SushiDefense.Customers.TableSlotView>();
            _stage.Placement.Place(DefaultCustomer(), slot.SlotIndex, slot.BeltPosition);

            yield return WaitSeconds(15f);

            Assert.Greater(_stage.Revenue.Total, 0, "배치된 손님이 먹으면 매출이 오른다");
            Assert.AreEqual(_stage.Revenue.Total / 10 + _stage.StageConfig.InitialRecruitBudget
                            - DefaultCustomer().RecruitCost,
                            _stage.Wallet.Balance,
                            "잔액 = 초기 예산 − 영입 비용 + 매출/10");
        }

        [UnityTest]
        public IEnumerator Play_Scene_SushiViewsFollowModels()
        {
            yield return WaitSeconds(3f);

            var view = Object.FindAnyObjectByType<SushiItemView>();
            Assert.IsNotNull(view, "초밥 뷰가 풀에서 나오지 않았다 — 프리팹 참조를 확인하라");
            Assert.IsNotNull(view.Model);
        }

        [UnityTest]
        public IEnumerator Play_Scene_PlacedCustomerClaimsSushi()
        {
            var claimed = 0;
            _stage.Coordinator.SushiClaimed += (_, _) => claimed++;

            Assert.IsTrue(_stage.Placement.CanPlace(DefaultCustomer(), 0), "첫 자리에 배치할 수 있어야 한다");
            var slot = Object.FindAnyObjectByType<SushiDefense.Customers.TableSlotView>();
            _stage.Placement.Place(DefaultCustomer(), slot.SlotIndex, slot.BeltPosition);

            yield return WaitSeconds(12f);

            Assert.Greater(claimed, 0, "배치된 손님이 범위 안 초밥을 집는다");
        }

        private static SushiDefense.Data.CustomerData DefaultCustomer() => LoadCustomer("Customer.Standard");

        /// <summary>
        /// 밸런스 애셋을 이름으로 연다. 순수 로직 테스트에서는 금지된 방식이지만
        /// (<c>.claude/rules/tests.md</c> §4), 여기서는 <b>애셋 자체가 검증 대상</b>이다 —
        /// 코드로 만든 SO 로는 "값을 채우는 것을 잊었다" 를 잡을 수 없다.
        /// </summary>
        private static SushiDefense.Data.CustomerData LoadCustomer(string assetName)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<SushiDefense.Data.CustomerData>(
                $"Assets/Level/Balance/{assetName}.asset");
#else
            return null;
#endif
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
