using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SushiDefense.Data;
using UnityEditor;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// 디스크의 <c>CardCatalog.asset</c> 이 <b>다른 목록들을 덮는지</b>만 본다.
    ///
    /// <para>
    /// 밸런스 애셋을 로드하는 테스트는 원칙적으로 피하지만(<c>.claude/rules/tests.md</c> §4),
    /// 그 금지는 <b>수치</b>에 대한 것이다. 여기서 보는 것은 <b>목록의 정합성</b>이다 —
    /// 사전과 보상 풀과 스폰 구성이 카드를 각각 나열하므로, 카드를 추가하고 한쪽만 채우는
    /// 사고가 실제로 가능하다. 코드로 합치는 대신(둘은 다른 질문에 답한다) 이 파일이 지킨다.
    /// </para>
    /// <para>
    /// <b>가격도 순서도 단언하지 않는다.</b> 둘 다 M9 에서 다시 만질 값이라, 박으면 밸런싱할
    /// 때마다 테스트가 깨진다.
    /// </para>
    /// </summary>
    public sealed class CardCatalogAssetTests
    {
        private const string CatalogPath = "Assets/Level/Balance/CardCatalog.asset";
        private const string RewardCatalogPath = "Assets/Level/Balance/RewardCatalog.asset";
        private const string RunConfigPath = "Assets/Level/Balance/Run.Demo.asset";

        private CardCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
        }

        [Test]
        public void Asset_Exists()
        {
            // 스크립트 참조가 깨져 있으면 로드가 null 로 떨어진다 — 손으로 쓴 YAML 이
            // 실제로 역직렬화됐는지를 이 한 줄이 지킨다.
            Assert.IsNotNull(_catalog, $"{CatalogPath} 를 로드하지 못했다");
        }

        [Test]
        public void Catalog_HasNoEmptySlot()
        {
            // 인스펙터에서 목록을 늘리면 자연히 생긴다. 스키마는 허용하지만 실제 애셋에
            // 남아 있으면 사전에 빈 카드가 그려진다.
            CollectionAssert.DoesNotContain(_catalog.AllSushi, null);
            CollectionAssert.DoesNotContain(_catalog.AllCustomers, null);
        }

        /// <summary>
        /// 모든 카드가 <b>자기 그림</b>을 갖는지 본다. 손님은 유형을 실루엣으로만 구별하므로
        /// (<c>.claude/domain/customer-kinds.md</c> §1) 그림이 비면 자리 셋이 똑같아진다.
        ///
        /// <para>
        /// <b>이 단언은 실제로 깨진 적이 있다.</b> 손님 그림을 4방향으로 다시 그리면서 옛
        /// 파일이 지워졌고, 세 손님의 아이콘 참조가 통째로 끊겼다. 끊긴 참조는
        /// <c>null</c> 로 역직렬화되므로 예외도 로그도 없이 <b>프리팹의 placeholder 블록이
        /// 그대로 남는다</b> — 화면을 보기 전에는 아무도 모른다.
        /// </para>
        /// </summary>
        [Test]
        public void EveryCard_HasItsOwnIcon()
        {
            var icons = new HashSet<UnityEngine.Sprite>();

            foreach (var sushi in _catalog.AllSushi)
            {
                Assert.IsNotNull(sushi.Icon, $"{sushi.name} 에 그림이 없다");
                Assert.IsTrue(icons.Add(sushi.Icon), $"{sushi.name} 이 남의 그림을 쓴다");
            }

            foreach (var customer in _catalog.AllCustomers)
            {
                Assert.IsNotNull(customer.Icon, $"{customer.name} 에 그림이 없다");
                Assert.IsTrue(icons.Add(customer.Icon), $"{customer.name} 이 남의 그림을 쓴다");
            }
        }

        [Test]
        public void Catalog_HasNoDuplicate()
        {
            // 같은 카드를 두 번 적으면 사전에 두 번 나온다. 눈으로는 잘 안 보인다.
            Assert.AreEqual(_catalog.AllSushi.Count,
                            new HashSet<SushiData>(_catalog.AllSushi).Count, "초밥 중복");
            Assert.AreEqual(_catalog.AllCustomers.Count,
                            new HashSet<CustomerData>(_catalog.AllCustomers).Count, "손님 중복");
        }

        /// <summary>
        /// 보상으로 나올 수 있는 카드는 <b>반드시</b> 사전에 있어야 한다. 없으면 방금 받은
        /// 카드를 사전에서 찾을 수 없는 상태가 된다.
        /// </summary>
        [Test]
        public void Catalog_CoversRewardPool()
        {
            var rewards = AssetDatabase.LoadAssetAtPath<RewardCatalog>(RewardCatalogPath);
            Assert.IsNotNull(rewards, $"{RewardCatalogPath} 를 로드하지 못했다");

            CollectionAssert.IsSubsetOf(rewards.SushiPool, _catalog.AllSushi,
                                        "보상 초밥이 사전에 없다");
            CollectionAssert.IsSubsetOf(rewards.CustomerPool, _catalog.AllCustomers,
                                        "보상 손님이 사전에 없다");
        }

        /// <summary>
        /// 벨트에 실제로 도는 초밥은 <b>반드시</b> 사전에 있어야 한다. 플레이어가 화면에서
        /// 보고 있는 것을 사전에서 못 찾으면 사전이 아니다.
        ///
        /// <para>
        /// 스테이지를 <c>RunConfig</c> 로 순회한다 — 경로를 하나씩 적으면 스테이지가 늘 때
        /// 이 테스트만 조용히 뒤처진다.
        /// </para>
        /// </summary>
        [Test]
        public void Catalog_CoversEveryStageSpawnTable()
        {
            var run = AssetDatabase.LoadAssetAtPath<RunConfig>(RunConfigPath);
            Assert.IsNotNull(run, $"{RunConfigPath} 를 로드하지 못했다");

            var spawned = new List<SushiData>();
            for (var i = 0; i < run.Stages.Count; i++)
            {
                var stage = run.Stages[i];
                if (stage == null)
                {
                    continue;
                }

                var table = stage.SpawnTable;
                for (var e = 0; e < table.Count; e++)
                {
                    var sushi = table[e]?.Sushi;
                    if (sushi != null)
                    {
                        spawned.Add(sushi);
                    }
                }
            }

            // 스폰 구성이 통째로 비면 이 테스트가 아무것도 검사하지 않게 된다 —
            // 그 상태로 통과하는 것을 막는다.
            CollectionAssert.IsNotEmpty(spawned, "어느 스테이지에도 스폰 구성이 없다");
            CollectionAssert.IsSubsetOf(spawned, _catalog.AllSushi, "벨트에 도는 초밥이 사전에 없다");
        }

        /// <summary>
        /// 시작 명부의 손님도 사전에 있어야 한다. 씬의 배열을 읽지 않고
        /// <b>보상 풀에 없는 손님</b>으로 대신 확인한다 — 씬을 로드하는 것은 EditMode 의 일이
        /// 아니고, 손님 셋 중 보상으로 나오는 둘 말고 하나가 곧 시작 손님이다.
        /// </summary>
        [Test]
        public void Catalog_HasCustomerOutsideRewardPool()
        {
            var rewards = AssetDatabase.LoadAssetAtPath<RewardCatalog>(RewardCatalogPath);

            var startingOnly = 0;
            for (var i = 0; i < _catalog.AllCustomers.Count; i++)
            {
                if (!rewards.CustomerPool.Contains(_catalog.AllCustomers[i]))
                {
                    startingOnly++;
                }
            }

            Assert.GreaterOrEqual(startingOnly, 1, "보상으로 얻을 수 없는 손님이 사전에 없다");
        }
    }
}
