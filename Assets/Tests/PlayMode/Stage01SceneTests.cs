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
