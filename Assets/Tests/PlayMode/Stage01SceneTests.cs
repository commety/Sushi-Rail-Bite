using System.Collections;
using NUnit.Framework;
using SushiDefense;
using SushiDefense.Belt;
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

        private static SushiDefense.Data.CustomerData DefaultCustomer()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<SushiDefense.Data.CustomerData>(
                "Assets/Level/Balance/Customer.Standard.asset");
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
