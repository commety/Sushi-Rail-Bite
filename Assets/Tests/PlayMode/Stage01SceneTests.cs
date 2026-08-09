using System.Collections;
using NUnit.Framework;
using SushiDefense;
using SushiDefense.Audio;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Navigation;
using SushiDefense.Run;
using SushiDefense.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
        /// 명부를 손패 카드로 늘어놓고 그 카드를 자리에 끌어다 놓으므로, 손패까지 값이
        /// 닿았는지를 본다.
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

            var hand = Object.FindAnyObjectByType<CustomerHandView>(FindObjectsInactive.Include);
            Assert.IsNotNull(hand, "씬에 손패가 없다");
            Assert.IsNotNull(hand.CustomerAt(0),
                             "손패 첫 카드가 비었다 — 명부가 손패에 물리지 않았다");
            Assert.IsTrue(hand.IsAvailableAt(0),
                          "시작 손님을 아무 자리에도 놓을 수 없다 — 시작 예산 또는 자리 배치를 확인하라");
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

        /// <summary>
        /// 보상 화면이 <b>처음에는 내려가 있는지</b> 본다.
        ///
        /// <para>
        /// <c>ShowOffers</c> 는 오브젝트를 켜는데 <c>Hide</c> 는 카드와 건너뛰기만 끄고 있었다.
        /// 화면에 배경이 없던 동안에는 그 비대칭이 보이지 않았지만, M6 에서 패널을 깔자마자
        /// <b>스테이지 시작부터 보상 화면이 판을 덮었다.</b>
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_RewardScreenStartsHidden()
        {
            yield return null;

            var view = Object.FindAnyObjectByType<RewardSelectionView>(FindObjectsInactive.Include);

            Assert.IsNotNull(view, "씬에 보상 화면이 없다");
            Assert.IsFalse(view.gameObject.activeInHierarchy,
                           "판이 시작부터 보상 화면에 덮여 있다");
            Assert.IsFalse(view.IsShowing);
        }

        /// <summary>
        /// 보상 카드를 열었을 때 <b>글자가 실제로 라벨에 닿는지</b> 본다.
        ///
        /// <para>
        /// <c>CardView.NameText</c> 는 라벨이 없어도 채워지는 프로퍼티라 그것만 보면 공허하다
        /// (<c>.claude/rules/tests.md</c> §3). 카드는 껐다 켜는 구조여서 <c>CardView.Awake</c> 가
        /// 아직 안 돈 상태로 <c>Show</c> 가 불릴 수 있는 자리이며, 그러면 글자가 <c>null</c>
        /// 라벨로 흘러가 <b>빈 카드</b>가 뜬다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_RewardCardsRenderText()
        {
            yield return null;

            _stage.Rewards.Open(_stage.Run);
            yield return null;

            var view = Object.FindAnyObjectByType<RewardSelectionView>(FindObjectsInactive.Include);
            Assert.Greater(view.ShownCardCount, 0, "전제: 제시할 보상이 있다");

            var drawnCards = 0;
            foreach (var card in view.GetComponentsInChildren<CardView>(true))
            {
                if (!card.IsShowing)
                {
                    continue;
                }

                drawnCards++;
                Assert.IsNotEmpty(card.NameText, $"{card.name} 이 이름을 들고 있지 않다");

                var reachedALabel = false;
                foreach (var label in card.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    reachedALabel |= !string.IsNullOrEmpty(label.text);
                }

                Assert.IsTrue(reachedALabel,
                              $"{card.name} 의 글자가 라벨에 닿지 않았다 — 빈 카드가 뜬다");
            }

            Assert.AreEqual(view.ShownCardCount, drawnCards, "그렸다고 센 카드 수와 실제가 다르다");

            _stage.Rewards.Skip();
        }

        /// <summary>
        /// 덱 화면의 카드도 글자가 라벨에 닿는지 본다 — 배선이 끊기면 빈 카드가 뜬다.
        ///
        /// <para>
        /// <b>이 테스트는 활성화 순서 문제를 잡지 못한다.</b> 참조 해석을 <c>Awake</c> 하나로
        /// 되돌려 실측했더니 보상 카드만 빈 카드가 되고 <b>덱 카드는 멀쩡했다</b> — 계층 순서상
        /// 덱 카드의 <c>Awake</c> 는 제때 돈다. 같은 모양이라고 같이 깨지지는 않으므로,
        /// 그 계약을 지키는 것은 <c>Play_Scene_RewardCardsRenderText</c> 쪽이다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_DeckCardsRenderText()
        {
            yield return null;

            _stage.Deck.Open();
            yield return null;

            var view = Object.FindAnyObjectByType<DeckPanelView>(FindObjectsInactive.Include);
            Assert.Greater(view.ShownCardCount, 0, "전제: 덱에 초밥이 있다");

            var drawnCards = 0;
            foreach (var card in view.GetComponentsInChildren<CardView>(true))
            {
                if (!card.IsShowing)
                {
                    continue;
                }

                drawnCards++;

                var reachedALabel = false;
                foreach (var label in card.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    reachedALabel |= !string.IsNullOrEmpty(label.text);
                }

                Assert.IsTrue(reachedALabel,
                              $"{card.name} 의 글자가 라벨에 닿지 않았다 — 빈 카드가 뜬다");
            }

            Assert.AreEqual(view.ShownCardCount, drawnCards, "그렸다고 센 카드 수와 실제가 다르다");

            _stage.Deck.Close();
        }

        /// <summary>
        /// <b>손패 카드에 글자와 그림이 실제로 나가는지</b> 본다. 플레이어가 가장 먼저 만지는
        /// 화면인데 이것을 보는 테스트가 하나도 없었다.
        ///
        /// <para>
        /// <b>이 테스트는 실행 순서 문제를 잡지 못한다.</b> 손패가 빈 카드로 뜬 원인은
        /// <c>CustomerCardDrag</c> 가 자기 <c>Awake</c> 를 전제한 것이었는데, 고친 것을 되돌려
        /// 실측했더니 <b>에디터에서는 통과한다</b> — 에디터의 <c>Awake</c> 순서가 플레이어와
        /// 반대라 <c>Card</c> 가 이미 채워져 있다. 실제로 터진 곳은 WebGL 뿐이었다.
        /// 그 계약은 <c>CustomerHandViewTests.Bind_CardAwakeHasNotRun_StillDrawsInsteadOfThrowing</c>
        /// 이 순서를 손으로 만들어 지킨다.
        /// </para>
        /// <para>
        /// 그래도 남긴다 — 명부가 카드에 닿는 배선이 끊기면 여기서 잡힌다. 기존 손패 테스트는
        /// <c>DropAt</c> 을 직접 부르거나 카드를 코드로 만들어 물려서 이 경로를 지나간다
        /// (<c>.claude/rules/tests.md</c> §1).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_HandCardsRenderText()
        {
            yield return null;

            var hand = Object.FindAnyObjectByType<CustomerHandView>(FindObjectsInactive.Include);
            Assert.IsNotNull(hand, "씬에 손패가 없다");
            Assert.Greater(hand.ShownCardCount, 0, "전제: 명부에 손님이 있어 카드가 그려진다");

            var drawnCards = 0;
            foreach (var drag in hand.GetComponentsInChildren<CustomerCardDrag>(true))
            {
                if (drag.Customer == null)
                {
                    continue;
                }

                drawnCards++;
                Assert.IsNotNull(drag.Card, $"{drag.name} 이 카드 표현을 물지 못했다");
                Assert.IsTrue(drag.Card.IsShowing, $"{drag.name} 이 내용을 그리지 않았다");

                var reachedALabel = false;
                foreach (var label in drag.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    reachedALabel |= !string.IsNullOrEmpty(label.text);
                }

                Assert.IsTrue(reachedALabel,
                              $"{drag.name} 의 글자가 라벨에 닿지 않았다 — 빈 카드가 뜬다");
            }

            Assert.AreEqual(hand.ShownCardCount, drawnCards, "그렸다고 센 카드 수와 실제가 다르다");
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

        /// <summary>
        /// 덱의 초밥이 <b>서로 다른 그림</b>을 갖는지 본다. 가격 대역이 이 게임의 핵심
        /// 규칙(M2.5)인데 벨트 위 초밥이 전부 같아 보이면 플레이어가 그 규칙을 배울 수 없다.
        ///
        /// <para>
        /// 코드로 세운 하네스는 밸런스 애셋을 보지 않는다 — 초밥을 새로 추가하고 아이콘을
        /// 안 물리면 여기서만 잡힌다 (<c>.claude/rules/tests.md</c> §1).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_DeckSushiHaveDistinctIcons()
        {
            yield return null;

            var icons = new System.Collections.Generic.HashSet<Sprite>();
            foreach (var entry in _stage.StageConfig.SpawnTable)
            {
                Assert.IsNotNull(entry.Sushi.Icon,
                                 $"{entry.Sushi.name} 에 아이콘이 물려 있지 않다");
                Assert.IsTrue(icons.Add(entry.Sushi.Icon),
                              $"{entry.Sushi.name} 이 다른 초밥과 같은 그림을 쓴다");
            }

            Assert.GreaterOrEqual(icons.Count, 3, "그림이 서로 다른 초밥이 최소 3종은 있어야 한다");
        }


        // ── 손님 정보 창 · 접히는 손패 (M6.5) ────────────────────────────

        /// <summary>
        /// 정보 창이 씬에 있고 <b>꺼진 채로 시작</b>하는지 본다. 켜진 채 남으면 판이 열리자마자
        /// 빈 창이 화면을 덮는다 — 뷰의 <c>Awake</c> 가 내리므로 재생 직후에는 반드시 꺼져 있다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_HasCustomerInspectorPanel_AndItStartsHidden()
        {
            yield return WaitSeconds(0.2f);

            var view = _stage.GetComponentInChildren<CustomerInspectorView>(true);

            Assert.IsNotNull(view, "정보 창이 씬에 없다");
            Assert.IsFalse(view.IsShowing, "정보 창이 켜진 채로 시작한다");
            Assert.IsNotNull(_stage.Inspector, "부트스트랩이 정보 창을 세우지 않았다");
        }

        /// <summary>
        /// 라벨 여섯이 <b>이름 그대로</b> 있는지 본다. 뷰는 인스펙터가 비면 자기 하위에서
        /// 이름으로 찾으므로, 하나만 틀려도 그 칸이 조용히 비고 예외는 나지 않는다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_InspectorPanel_HasEveryLabel()
        {
            yield return WaitSeconds(0.2f);

            var view = _stage.GetComponentInChildren<CustomerInspectorView>(true);

            foreach (var name in new[]
                     {
                         "NameLabel", "StatsLabel",
                         "StateLabel", "SaturationLabel", "RemainingLabel"
                     })
            {
                var label = view.transform.Find(name);
                Assert.IsNotNull(label, $"{name} 이 없다");
                Assert.IsNotNull(label.GetComponent<TMPro.TMP_Text>(), $"{name} 이 TMP 가 아니다");
                Assert.IsNotNull(label.GetComponent<TMPro.TMP_Text>().font, $"{name} 의 폰트가 비었다");
            }
        }

        [UnityTest]
        public IEnumerator Play_Scene_HasCustomerTapRouter()
        {
            yield return WaitSeconds(0.2f);

            Assert.IsNotNull(_stage.GetComponentInChildren<CustomerTapRouter>(true),
                             "클릭 라우터가 씬에 없다");
        }

        /// <summary>
        /// <b>씬을 지나 정보 창이 열리는지</b> 본다. 앞의 셋은 «있다» 만 보므로 배선이 끊겨도
        /// 통과한다 — 자리에 손님을 앉히고 그 좌표를 눌러 끝까지 간다.
        ///
        /// <para>
        /// <b>창이 옮겨졌는지도 여기서 본다.</b> 자리별 기하 검사는 <c>AnchorTo</c> 를 직접
        /// 부르므로 부트스트랩의 배선을 지나친다 — 누름에서 자리 잡기까지 이어지는지는
        /// 이 경로만 안다 (<c>.claude/rules/tests.md</c> §1 «안쪽 진입점만 부르는 테스트»).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_TappingASeatedCustomer_OpensTheInspector()
        {
            yield return WaitSeconds(0.2f);

            var slot = _stage.GetComponentInChildren<TableSlotView>(true);
            _stage.Placement.Place(_stage.Run.Customers.Members[0], slot.SlotIndex,
                                   slot.BeltPosition);
            slot.Occupy(_stage.Placement.OccupantOf(slot.SlotIndex), _stage.Coordinator);

            var panel = (RectTransform)_stage.GetComponentInChildren<CustomerInspectorView>(true)
                                             .transform;
            var parked = panel.anchoredPosition;

            var router = _stage.GetComponentInChildren<CustomerTapRouter>(true);
            var opened = router.TapAt(slot.transform.position);
            yield return null;

            Assert.IsTrue(opened, "자리를 눌렀는데 손님을 못 찾았다");
            Assert.IsTrue(_stage.Inspector.IsOpen, "정보 창이 안 열렸다");
            Assert.AreNotEqual(parked, panel.anchoredPosition,
                               "창이 씬에 적힌 자리에 그대로 있다 — 누름이 자리 잡기까지 못 갔다");
        }

        /// <summary>
        /// <b>다른 곳을 누르면 닫힌다.</b> 창에 닫기 버튼이 없고 아이콘으로 토글되지도 않아,
        /// 이 경로가 없으면 영영 안 닫힌다 — 실플레이에서 보상 화면 위에도, 다음 판에도
        /// 남았다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_TappingElsewhere_ClosesTheInspector()
        {
            yield return WaitSeconds(0.2f);

            var slot = _stage.GetComponentInChildren<TableSlotView>(true);
            _stage.Placement.Place(_stage.Run.Customers.Members[0], slot.SlotIndex,
                                   slot.BeltPosition);
            slot.Occupy(_stage.Placement.OccupantOf(slot.SlotIndex), _stage.Coordinator);

            var router = _stage.GetComponentInChildren<CustomerTapRouter>(true);
            router.TapAt(slot.transform.position);
            Assert.IsTrue(_stage.Inspector.IsOpen, "전제: 창이 열렸다");

            router.TapAt(new Vector2(999f, 999f));
            yield return null;

            Assert.IsFalse(_stage.Inspector.IsOpen, "다른 곳을 눌렀는데 창이 안 닫힌다");
        }

        /// <summary>
        /// <b>판이 끝나면 내려간다.</b> 보상은 «밑에 깔리는» 창이라 조정자가 아무것도 밀어내지
        /// 않고, 정보 창은 스스로 닫힐 길이 없다 — 그대로 두면 보상 화면 위에 남는다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_StageCleared_ClosesTheInspector()
        {
            yield return WaitSeconds(0.2f);

            var slot = _stage.GetComponentInChildren<TableSlotView>(true);
            _stage.Placement.Place(_stage.Run.Customers.Members[0], slot.SlotIndex,
                                   slot.BeltPosition);
            slot.Occupy(_stage.Placement.OccupantOf(slot.SlotIndex), _stage.Coordinator);
            _stage.GetComponentInChildren<CustomerTapRouter>(true).TapAt(slot.transform.position);
            Assert.IsTrue(_stage.Inspector.IsOpen, "전제: 창이 열렸다");

            _stage.Revenue.Add(_stage.Stage.TargetRevenue);
            _stage.Stage.Tick(0.1f);
            yield return null;

            Assert.IsFalse(_stage.Inspector.IsOpen, "클리어했는데 정보 창이 남았다");
        }

        /// <summary>
        /// 정보 창이 <b>메뉴·덱 아이콘을 가리지 않는지</b> 본다. 처음 배치에서 패널이 두
        /// 아이콘 위에 정확히 겹쳐, 창이 뜨면 둘 다 누를 수 없었다.
        ///
        /// <para>
        /// <b>자리마다 확인한다.</b> 창이 런타임에 손님을 따라 서게 됐으므로(M6.6) 씬에 적힌
        /// 자리를 재는 것은 아무것도 지키지 않는다. 자리 하나만 보면 «오른쪽 끝에서만
        /// 겹친다» 를 놓친다 — 클램프가 실제로 위험한 곳이 거기다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_InspectorPanel_DoesNotCoverTheIcons()
        {
            yield return WaitSeconds(0.2f);

            var view = _stage.GetComponentInChildren<CustomerInspectorView>(true);
            var panel = (RectTransform)view.transform;
            var slots = _stage.GetComponentsInChildren<TableSlotView>(true);

            Assert.IsNotEmpty(slots, "전제: 씬에 자리가 있다");

            foreach (var slot in slots)
            {
                view.AnchorTo(slot.transform.position);
                yield return null;

                foreach (var button in _stage.GetComponentsInChildren<Button>(true))
                {
                    if (button.name != "OpenButton" && button.name != "ToggleButton")
                    {
                        continue;
                    }

                    Assert.IsFalse(Overlaps(panel, (RectTransform)button.transform),
                                   $"자리 {slot.SlotIndex} 에서 정보 창이 {button.name} 을 덮는다");
                }
            }
        }

        /// <summary>
        /// 창이 <b>실제로 손님을 따라 서는지</b> 본다. 겹침 검사만으로는 창이 여전히 우하단에
        /// 붙어 있어도 통과한다 — 아이콘 위가 아니기만 하면 되기 때문이다.
        ///
        /// <para>
        /// <b>카메라를 물려 주지 않는다.</b> 씬은 <c>_worldCamera</c> 가 빈 채로 재생되고
        /// <c>Camera.main</c> 폴백을 탄다 — 여기서 카메라를 주입하면 그 분기가 죽어도 초록이
        /// 된다 (<c>.claude/rules/tests.md</c> §1).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_Inspector_StandsBesideEachSeat()
        {
            yield return WaitSeconds(0.2f);

            var view = _stage.GetComponentInChildren<CustomerInspectorView>(true);
            var panel = (RectTransform)view.transform;
            var canvas = (RectTransform)panel.parent;
            var slots = _stage.GetComponentsInChildren<TableSlotView>(true);
            var seen = new System.Collections.Generic.List<Vector2>();

            foreach (var slot in slots)
            {
                view.AnchorTo(slot.transform.position);
                yield return null;

                var min = panel.anchoredPosition - Vector2.Scale(panel.pivot, panel.rect.size);
                var max = min + panel.rect.size;

                Assert.GreaterOrEqual(min.x, canvas.rect.xMin, $"자리 {slot.SlotIndex}: 창이 왼쪽으로 새 나갔다");
                Assert.LessOrEqual(max.x, canvas.rect.xMax, $"자리 {slot.SlotIndex}: 창이 오른쪽으로 새 나갔다");
                Assert.GreaterOrEqual(min.y, canvas.rect.yMin, $"자리 {slot.SlotIndex}: 창이 아래로 새 나갔다");
                Assert.LessOrEqual(max.y, canvas.rect.yMax, $"자리 {slot.SlotIndex}: 창이 위로 새 나갔다");

                seen.Add(panel.anchoredPosition);
            }

            // 자리가 서로 다른 x 에 있으므로 창도 서로 달라야 한다. 같은 값이 반복되면
            // 좌표가 배선을 지나지 못하고 어딘가 고정값으로 떨어진 것이다.
            CollectionAssert.AllItemsAreUnique(seen, "자리가 달라도 창이 같은 자리에 선다");
        }

        private static bool Overlaps(RectTransform a, RectTransform b)
        {
            return WorldRect(a).Overlaps(WorldRect(b));
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y,
                            corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        /// <summary>
        /// 손패가 <b>접힐 수 있는 모양</b>인지 본다. 컨테이너를 안 물렸으면 손패 자신이 그
        /// 역할을 하므로, 여기서 확인할 것은 «움직일 대상이 카드의 부모인가» 다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_HandFolds_WithoutMovingItsCards()
        {
            yield return WaitSeconds(0.2f);

            var hand = _stage.GetComponentInChildren<CustomerHandView>(true);
            var card = (RectTransform)hand.transform.GetChild(0);
            var before = card.anchoredPosition;

            hand.SetExpanded(true);
            yield return null;
            hand.SetExpanded(false);
            yield return WaitSeconds(0.3f);

            Assert.AreEqual(before, card.anchoredPosition, "카드가 직접 움직였다");
        }

        /// <summary>
        /// <b>Canvas 밖에 있어도 되는 라벨은 손님을 따라다니는 것뿐이다</b> — 대역 문구와
        /// 소화 잔여 초. 자리를 따라 움직여야 해서 화면 좌표에 고정할 수 없다
        /// (<c>HudLabel</c> 의 클래스 주석).
        ///
        /// <para>
        /// <b>이름이 아니라 «어디 달렸나» 로 가른다.</b> 처음에는 이름 목록으로 예외를
        /// 뒀는데, 정보 창이 <c>RemainingLabel</c> 이라는 같은 이름을 쓰면서 <b>엉뚱한 곳의
        /// 동명 라벨이 예외를 타고 빠져나갔다.</b> 이름은 우연히 겹치지만 계층은 겹치지 않는다.
        /// </para>
        /// <para>
        /// <b>M6.5 이전에는 이 테스트가 모든 라벨을 검사했고 그래도 통과했다</b> — 씬에
        /// 월드스페이스 TMP 라벨이 하나도 없었기 때문이다. 대역 라벨이 레거시 <c>TextMesh</c> 라
        /// 화면에 안 나오던 상태였고, 그것을 고치자 이 가드가 비로소 자기 범위를 드러냈다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_HudLabelsLiveOnCanvas()
        {
            yield return WaitSeconds(0.2f);

            var labels = _stage.GetComponentsInChildren<TMPro.TMP_Text>(true);
            Assert.IsNotEmpty(labels, "씬에 TMP 라벨이 하나도 없다");

            var checkedAny = false;
            foreach (var label in labels)
            {
                if (label.GetComponentInParent<Canvas>(true) != null)
                {
                    continue;
                }

                checkedAny = true;
                Assert.IsNotNull(label.GetComponentInParent<CustomerView>(true),
                                 $"{label.name} 이 Canvas 밖인데 손님 아래도 아니다");
            }

            Assert.IsTrue(checkedAny,
                          "Canvas 밖 라벨이 하나도 없다 — 소화 배지의 숫자가 사라졌거나 이 테스트가 헛돈다");
        }

        /// <summary>
        /// 뒤집어서도 본다 — <b>HUD 라벨이 Canvas 밖으로 새지 않았는지.</b>
        ///
        /// <para>
        /// 위 테스트는 «Canvas 밖이면 손님 아래여야 한다» 를 보므로, 손님 아래에 있는 것은
        /// 무엇이든 통과한다. 여기서는 <b>손님 밖의 라벨은 전부 Canvas 위</b>여야 함을 본다.
        /// 둘을 합치면 라벨이 갈 수 있는 곳이 두 자리로 못박힌다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_LabelsOutsideCustomersLiveOnCanvas()
        {
            yield return WaitSeconds(0.2f);

            foreach (var label in _stage.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (label.GetComponentInParent<CustomerView>(true) != null)
                {
                    continue;
                }

                Assert.IsNotNull(label.GetComponentInParent<Canvas>(true),
                                 $"{label.name} 이 Canvas 밖에 있다");
            }
        }

        /// <summary>
        /// 라벨이 <b>한글 폰트를 물고 있는지</b> 본다. 비어 있으면 TMP 가 기본 폰트로
        /// 대신하는데 거기엔 한글이 없어 화면이 두부가 된다 — 빌드해야만 드러나는 실패다.
        ///
        /// <para>
        /// <b>씬 전체를 본다.</b> <c>Stage</c> 하위만 보면 밖에 놓인 라벨을 지나치고, 무엇보다
        /// <i>"폰트가 비었다"</i> 만 보는 검사로는 <b>엉뚱한 폰트가 물린 경우</b>를 못 잡는다 —
        /// M6 에서 <c>Card.prefab</c> 이 그 형태로 기본 SDF 폰트를 물고 있었다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_LabelsUseKoreanFont()
        {
            yield return WaitSeconds(0.2f);

            var labels = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,
                                                                 FindObjectsSortMode.None);

            Assert.IsNotEmpty(labels, "라벨을 하나도 찾지 못했다 — 씬이 비었는지 확인하라");
            foreach (var label in labels)
            {
                Assert.IsNotNull(label.font, $"{label.name} 에 폰트가 없다");
                Assert.IsTrue(label.font.HasCharacters("매출 클리어"),
                              $"{label.name} 의 폰트에 한글이 없다: {label.font.name}");
            }
        }

        [UnityTest]
        public IEnumerator Play_Scene_HasAudioDirectorWired()
        {
            yield return WaitSeconds(0.2f);

            var director = _stage.GetComponentInChildren<SushiDefense.Audio.AudioDirector>(true);
            Assert.IsNotNull(director, "씬에 AudioDirector 가 없다");
            Assert.IsFalse(director.IsBgmPlaying, "첫 입력 전에 배경음이 울리면 안 된다");
        }

        [UnityTest]
        public IEnumerator Play_Scene_HasEffectDirectorWired()
        {
            yield return WaitSeconds(0.2f);

            Assert.IsNotNull(_stage.GetComponentInChildren<SushiDefense.Effects.EffectDirector>(true),
                             "씬에 EffectDirector 가 없다");
        }

        /// <summary>
        /// 먹힘이 실제로 소리와 이펙트로 이어지는지 본다. 배선이 끊겨도 다른 테스트는
        /// 전부 초록이라, 씬에서만 조용해진다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_EatenSushi_ReachesAudioAndEffects()
        {
            yield return WaitSeconds(0.2f);

            var audio = _stage.GetComponentInChildren<SushiDefense.Audio.AudioDirector>(true);
            var effects = _stage.GetComponentInChildren<SushiDefense.Effects.EffectDirector>(true);
            audio.NotifyUserInput();

            var slot = Object.FindAnyObjectByType<TableSlotView>();
            _stage.Placement.Place(DefaultCustomer(), slot.SlotIndex, slot.BeltPosition);

            yield return WaitSeconds(15f);

            Assert.Greater(_stage.Revenue.Total, 0, "손님이 아무것도 먹지 않았다");
            Assert.Greater(audio.PlayedCount + audio.SuppressedCount, 0, "먹힘이 오디오에 닿지 않았다");
            Assert.Greater(effects.SpawnedCount, 0, "먹힘이 이펙트에 닿지 않았다");
        }


        /// <summary>
        /// <b>클릭으로 손님을 앉힐 수 있는지</b> 본다. M5 이전에는 입력 경로가 아예 없어서
        /// 재생해도 손님을 놓을 수 없었고, 그러면 먹힘도 소리도 이펙트도 일어나지 않는다 —
        /// M5 의 결과물 대부분이 관측 불가능했다.
        ///
        /// <para>
        /// 포인터를 흉내 내지 않고 <c>DropAt</c> 을 직접 부른다. 좌표 변환은 카메라의
        /// 몫이고, 여기서 볼 것은 <b>드롭이 배치로 이어지는가</b>다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_ClickOnSlot_PlacesCustomer()
        {
            yield return WaitSeconds(0.2f);

            var hand = _stage.GetComponentInChildren<CustomerHandView>(true);
            Assert.IsNotNull(hand, "씬에 손패가 없다 — 손님을 앉힐 방법이 없다");

            var slot = Object.FindAnyObjectByType<TableSlotView>();
            var placed = hand.DropAt(_stage.Run.Customers.Members[0], slot.transform.position);

            Assert.IsTrue(placed, "빈 자리에 떨어뜨렸는데 앉지 않았다");
            Assert.AreEqual(1, _stage.Placement.PlacedCount);
        }

        /// <summary>
        /// 빈 곳에 떨어뜨렸을 때 <b>엉뚱한 자리에 앉지 않는지</b> 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_ClickOnEmptySpace_PlacesNothing()
        {
            yield return WaitSeconds(0.2f);

            var hand = _stage.GetComponentInChildren<CustomerHandView>(true);

            Assert.IsFalse(hand.DropAt(_stage.Run.Customers.Members[0], new Vector2(100f, 100f)));
            Assert.AreEqual(0, _stage.Placement.PlacedCount);
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

        /// <summary>
        /// 자리에 앉으면 <b>화면에 표시가 뜨는지</b> 본다. 머리 위 라벨을 걷어냈으므로(M6.6)
        /// 남은 표시가 실제로 안 보이면 손님이 어떤 상태인지 알 방법이 통째로 사라진다.
        ///
        /// <para>
        /// <b>씬의 인스턴스를 본다.</b> 프리팹이 멀쩡해도 씬에서 오버라이드로 꺼 두면
        /// 프리팹 테스트는 전부 초록이다 (<c>.claude/rules/tests.md</c> §1).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_OccupiedSeatShowsSaturation()
        {
            var slot = Object.FindAnyObjectByType<SushiDefense.Customers.TableSlotView>();
            var logic = _stage.Placement.Place(DefaultCustomer(), slot.SlotIndex, slot.BeltPosition);
            slot.Occupy(logic, _stage.Coordinator);

            yield return null;

            Assert.IsNotNull(slot.Occupant, "자리에 손님 시각 표현이 없다 — 씬 조립을 확인하라");

            var bar = slot.Occupant.GetComponentInChildren<SushiDefense.Customers.SaturationBarView>(true);
            Assert.IsNotNull(bar, "포화도 바가 없다");
            Assert.IsTrue(bar.gameObject.activeInHierarchy, "포화도 바가 꺼져 있다");
            Assert.Greater(bar.ShownVisibleCells, 0, "포화도 칸이 하나도 안 켜졌다");
        }

        /// <summary>
        /// 씬의 손님에게 <b>상시 라벨이 붙어 있지 않은지</b> 본다. 프리팹에서 지워도
        /// 씬 인스턴스에 하나 얹혀 있으면 화면에는 그대로 나온다 — M6 에서 카드 크기가
        /// 정확히 그 형태로 어긋나 있었다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_SeatedCustomer_HasNoAlwaysOnLabel()
        {
            var slot = Object.FindAnyObjectByType<SushiDefense.Customers.TableSlotView>();
            var logic = _stage.Placement.Place(DefaultCustomer(), slot.SlotIndex, slot.BeltPosition);
            slot.Occupy(logic, _stage.Coordinator);

            yield return null;

            var labels = slot.Occupant.GetComponentsInChildren<TMPro.TMP_Text>(true);

            Assert.AreEqual(1, labels.Length, "손님 위 라벨은 소화 배지의 숫자 하나뿐이어야 한다");
            Assert.AreEqual("RemainingLabel", labels[0].name);
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

        // ── 3스테이지 런 (M4) ────────────────────────────────────
        //
        // 이 계열이 M4 의 마지막 방어선이다. 다른 PlayMode 테스트는 구성을 코드로 세우고
        // Placement.Place 를 직접 부르므로 **씬이 조용히 죽는 사고를 구조적으로 못 잡는다**
        // (step-07 에서 주입으로 확인). 씬 애셋 자체를 보는 것은 여기뿐이다.

        [UnityTest]
        public IEnumerator Play_Scene_RunHasThreeStages()
        {
            yield return null;

            Assert.IsNotNull(_stage.Progression, "런 진행이 조립되지 않았다");
            Assert.AreEqual(3, _stage.Progression.StageCount,
                            "데모는 3스테이지다 — StageBootstrap 의 RunConfig 참조를 확인하라");
            Assert.AreEqual(1, _stage.Progression.CurrentStageNumber);
        }

        [UnityTest]
        public IEnumerator Play_Scene_HasTransitionScreenWired()
        {
            yield return null;

            Assert.IsNotNull(_stage.Transition,
                             "전환 프레젠터가 없다 — StageTransitionView 오브젝트를 확인하라");
        }

        /// <summary>
        /// 씬에 자리가 <b>넷</b> 있어야 스테이지 2·3 의 4자리 구성이 성립한다. 셋뿐이면
        /// 설정에는 자리가 넷인데 화면에는 셋만 나오고, 아무도 그것을 알려 주지 않는다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_HasFourTableSlots()
        {
            yield return null;

            var slots = Object.FindObjectsByType<SushiDefense.Customers.TableSlotView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.AreEqual(4, slots.Length, "스테이지 2·3 이 자리 4개를 쓴다");
        }

        /// <summary>
        /// 스테이지 1 은 자리 정의가 셋이므로 넷째는 꺼져 있어야 한다 — 자리 바인딩이
        /// 실제로 도는지 보는 지점이다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_Stage1_UsesOnlyThreeSlots()
        {
            yield return null;

            Assert.AreEqual(3, _stage.ActiveStage.TableSlots.Count, "전제: 스테이지 1 은 자리 셋이다");

            var active = 0;
            foreach (var slot in Object.FindObjectsByType<SushiDefense.Customers.TableSlotView>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (slot.gameObject.activeSelf)
                {
                    active++;
                }
            }

            Assert.AreEqual(3, active, "정의에 없는 넷째 자리가 켜져 있다");
        }

        /// <summary>
        /// 자리의 집기 범위가 벨트 끝을 넘는지를 <b>런의 모든 스테이지</b>에 대해 본다.
        /// 스테이지 1 만 보면 2·3 의 자리 좌표(최대 16)가 검증되지 않은 채 지나간다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_EveryStageKeepsReachInsideBelt()
        {
            yield return null;

            var reach = DefaultCustomer().Reach;
            for (var i = 1; i <= _stage.Progression.StageCount; i++)
            {
                var stage = StageAt(i);
                Assert.IsNotEmpty(stage.TableSlots, $"스테이지 {i} 에 자리 정의가 없다");

                foreach (var slot in stage.TableSlots)
                {
                    Assert.Less(slot.BeltPosition + reach, stage.BeltLength,
                                $"스테이지 {i} 자리 {slot.SlotIndex} 의 집기 범위가 벨트 끝을 넘는다 — "
                                + "기다리는 손님이 초밥을 잃는다");
                }
            }
        }

        /// <summary>
        /// 두 전문가는 서로 겹치지 않고, 둘 다 범용가보다 좁다. 폭이 같으면 배정 키 4 가
        /// 동률이 되어 유형 차이가 경합에서 사라진다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_SpecialistsSitAtOppositeEnds()
        {
            yield return null;

            var bigEater = LoadCustomer("Customer.BigEater");
            var standard = LoadCustomer("Customer.Standard");
            var smallEater = LoadCustomer("Customer.SmallEater");
            Assert.IsNotNull(bigEater, "먹보 애셋이 없다");

            Assert.Less(bigEater.TargetingMax, smallEater.TargetingMin,
                        "먹보와 소식의 대역이 겹친다 — 두 전문가는 반대편 끝을 맡는다");

            Assert.Less(BandWidth(bigEater), BandWidth(standard), "먹보가 기본보다 넓다");
            Assert.Less(BandWidth(smallEater), BandWidth(standard), "소식이 기본보다 넓다");
            Assert.AreNotEqual(BandWidth(bigEater), BandWidth(smallEater),
                               "두 전문가의 폭이 같으면 겹치는 초밥에서 승자가 배치 순서로 정해진다");
        }

        /// <summary>
        /// <b>기본 손님의 대역은 덱의 가격 전 구간을 덮어야 한다.</b>
        ///
        /// <para>
        /// M4 에서 대역을 3분할(기본 150~250)로 좁혔다가 <b>스테이지 1 이 클리어 불가</b>가
        /// 됐다. 원인은 경합이 아니라 <b>타이밍</b>이다 — 대역 밖 초밥(120·300)은 이탈 직전까지
        /// 유예되므로, 명부에 기본밖에 없는 스테이지 1 에서도 손님이 그만큼 논다. 실측 소비율이
        /// 96% → 78% 로 떨어졌다.
        /// </para>
        /// <para>
        /// 겹침 자체는 의도다. 겹친 구간에서 기본이 전문가에게 양보하는 것이 전문가 우대(키 4)이고,
        /// 기본의 값어치는 "아무거나 즉시 먹는 처리량" 이다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_GeneralistBandCoversWholeDeck()
        {
            yield return null;

            var standard = LoadCustomer("Customer.Standard");
            var cheapest = int.MaxValue;
            var priciest = 0;

            foreach (var entry in _stage.ActiveStage.SpawnTable)
            {
                cheapest = Mathf.Min(cheapest, entry.Sushi.Price);
                priciest = Mathf.Max(priciest, entry.Sushi.Price);
            }

            Assert.LessOrEqual(standard.TargetingMin, cheapest,
                               $"기본의 대역 하한이 덱 최저가({cheapest})보다 높다 — 그만큼 유예로 논다");
            Assert.GreaterOrEqual(standard.TargetingMax, priciest,
                                  $"기본의 대역 상한이 덱 최고가({priciest})보다 낮다 — 그만큼 유예로 논다");
        }

        /// <summary>보상 풀에 세 유형 중 미보유분이 실제로 들어 있는지 본다.</summary>
        [UnityTest]
        public IEnumerator Play_Scene_RewardPoolOffersUnownedCustomers()
        {
            yield return null;

            var bigEater = LoadCustomer("Customer.BigEater");
            Assert.IsFalse(_stage.Run.Customers.Contains(bigEater), "전제: 먹보는 시작 명부에 없다");

            _stage.Rewards.Open(_stage.Run);

            Assert.Greater(_stage.Rewards.OfferCount, 0,
                           "제시할 보상이 없다 — 손님 풀이 시작 명부와 완전히 겹친다");
            _stage.Rewards.Skip();
        }

        // ── 인스테이지 화면 (M6) ────────────────────────────────────
        //
        // 여기부터는 **씬에 오브젝트가 놓였는가**를 본다. step-03~10 이 만든 뷰는 코드로는
        // 전부 검증돼 있었지만 씬에 한 번도 놓인 적이 없었고, 그 상태에서도 위의 테스트는
        // 전부 초록이었다 — 하네스가 뷰 계층을 우회하기 때문이다 (tests.md §1).

        /// <summary>없으면 버튼도 드래그도 배달되지 않는다.</summary>
        [Test]
        public void Stage01_HasEventSystem()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>(),
                             "EventSystem 이 없으면 클릭이 아예 배달되지 않는다");
        }

        /// <summary>
        /// 이 프로젝트는 <c>ENABLE_LEGACY_INPUT_MANAGER</c> 가 정의되어 있지 않아
        /// <c>StandaloneInputModule</c> 이 런타임에 죽는다. <b>에디터에서는 경고만 뜨고
        /// 넘어갈 수 있어</b> 빌드에서 클릭이 통째로 안 먹는 형태로 드러난다.
        /// </summary>
        [Test]
        public void Stage01_UsesInputSystemUIModule()
        {
            // 타입 이름으로 본다. Tests.PlayMode 가 Unity.InputSystem 을 참조하지 않고,
            // 테스트 하나 때문에 asmdef 를 바꾸는 것은 §7 승인 사항이다.
            var module = Object.FindAnyObjectByType<BaseInputModule>();

            Assert.IsNotNull(module, "입력 모듈이 없다");
            Assert.AreEqual("InputSystemUIInputModule", module.GetType().Name,
                            "레거시 입력 모듈이면 빌드에서 클릭이 통째로 죽는다");
        }

        /// <summary>
        /// 손패·덱·메뉴가 씬에 놓였는지 본다. 세 화면은 <see cref="StageBootstrap"/> 이
        /// 자기 하위에서 찾아 프레젠터를 세우므로, <b>프레젠터가 <c>null</c> 이면 곧 씬에
        /// 오브젝트가 없다는 뜻</b>이다 — 메뉴는 <c>SceneRouter</c> 까지 있어야 선다.
        /// </summary>
        [Test]
        public void Stage01_HasHandDeckAndMenuViews()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<CustomerHandView>(),
                             "손패가 없으면 손님을 앉힐 방법이 없다");
            Assert.IsNotNull(Object.FindAnyObjectByType<SceneRouter>(),
                             "SceneRouter 가 없으면 나가기가 아무 일도 하지 않는다");
            Assert.IsNotNull(_stage.Deck, "덱 화면이 없다 — DeckPanelView 오브젝트를 확인하라");
            Assert.IsNotNull(_stage.Menu,
                             "메뉴가 없다 — StageMenuView 또는 SceneRouter 오브젝트를 확인하라");
        }

        /// <summary>
        /// step-04 가 <c>PlacementInput</c> 컴포넌트를 지웠다. 컴포넌트를 지우면 씬에는
        /// <b>Missing Script</b> 로 남는데, 조용하고 재생해도 경고만 뜬다. 지금 0건이므로
        /// 이것은 회귀 고정이며, 특정 이름이 아니라 <b>스크립트가 빠진 컴포넌트 전부</b>를 본다.
        /// </summary>
        [Test]
        public void Stage01_HasNoPlacementInput()
        {
            foreach (var node in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                    FindObjectsSortMode.None))
            {
                foreach (var component in node.GetComponents<Component>())
                {
                    Assert.IsNotNull(component,
                                     $"{node.name} 에 스크립트가 빠진 컴포넌트가 남아 있다");
                }
            }
        }

        /// <summary>
        /// 자리마다 손님 시각 표현이 있고, 그 포화도 칸이 <b>가장 많이 먹는 손님</b>을 담을 수
        /// 있는지 본다. 칸이 모자라면 먹보의 포화도가 화면에서 잘리는데, 예외가 나지 않아
        /// 눈으로만 드러난다.
        ///
        /// <para>
        /// 배치를 거치지 않고 바에 직접 물린다 — 여기서 볼 것은 <b>프리팹의 칸 수</b>이고,
        /// 자리 넷에 먹보를 앉히려 하면 잔액·한도가 먼저 막는다.
        /// </para>
        /// </summary>
        [Test]
        public void Stage01_CustomerViewHasSaturationCells()
        {
            var bigEater = LoadCustomer("Customer.BigEater");
            var slots = Object.FindObjectsByType<TableSlotView>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None);
            Assert.IsNotEmpty(slots, "전제: 씬에 자리가 있다");
            Assert.AreEqual(8, bigEater.MaxSaturation, "전제: 먹보가 가장 많이 먹는다");

            foreach (var slot in slots)
            {
                var seat = slot.GetComponentInChildren<CustomerView>(true);
                Assert.IsNotNull(seat, $"자리 {slot.SlotIndex} 에 손님 시각 표현이 없다");

                var bar = seat.GetComponentInChildren<SaturationBarView>(true);
                Assert.IsNotNull(bar, $"자리 {slot.SlotIndex} 의 손님에 포화도 바가 없다");

                bar.Bind(new CustomerRuntimeState(bigEater, 0));
                Assert.AreEqual(bigEater.MaxSaturation, bar.ShownVisibleCells,
                                $"자리 {slot.SlotIndex} 의 포화도 칸이 먹보의 최대 포화도보다 "
                                + "적다 — 먹보의 배가 화면에서 잘린다");
            }
        }

        /// <summary>
        /// 자리마다 손님 시각 표현이 <b>정확히 하나</b>인지 본다.
        ///
        /// <para>
        /// M6 조립 전까지 자리 0·1·2 에는 <c>Customer.prefab</c> 인스턴스가 <b>둘씩</b> 있었다.
        /// <see cref="TableSlotView"/> 는 첫 번째만 찾아 물리고 <c>Vacate</c> 도 그것만 끄므로,
        /// 나머지는 <b>아무것도 물리지 않은 채 켜져 있는 유령 손님</b>으로 세 자리에 앉아
        /// 있었다. 예외도 경고도 없고, 위의 자리·포화도 테스트는 첫 번째만 보므로 전부 초록이다.
        /// </para>
        /// </summary>
        [Test]
        public void Stage01_EachSlotHasOneSeatVisual()
        {
            var slots = Object.FindObjectsByType<TableSlotView>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None);
            Assert.IsNotEmpty(slots, "전제: 씬에 자리가 있다");

            foreach (var slot in slots)
            {
                Assert.AreEqual(1, slot.GetComponentsInChildren<CustomerView>(true).Length,
                                $"자리 {slot.SlotIndex} 의 손님 시각 표현이 하나가 아니다 — "
                                + "둘째부터는 물리지도 꺼지지도 않는 유령으로 남는다");
            }
        }

        // ── 실플레이 리포트 (M6 후속) ────────────────────────────────

        /// <summary>
        /// <b>자리에 테이블 그림이 있는지 본다.</b> 리포트의 «테이블이 보이지 않는다» 는
        /// 가려진 것이 아니라 <b>씬에 렌더러가 아예 없었던</b> 것이다 — 스프라이트는
        /// 배경과 벨트 레일 둘뿐이었고, 손님이 허공에 앉아 있었다.
        ///
        /// <para>
        /// <b>손님 뷰 바깥이어야 한다.</b> 안에 두면 자리가 빌 때 손님과 함께 꺼져,
        /// 앉힐 수 있는 자리가 화면에서 사라진다.
        /// </para>
        /// </summary>
        [Test]
        public void Stage01_EachSlotHasATableVisual()
        {
            var slots = Object.FindObjectsByType<TableSlotView>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None);
            Assert.IsNotEmpty(slots, "전제: 씬에 자리가 있다");

            foreach (var slot in slots)
            {
                var table = slot.transform.Find("Table");
                Assert.IsNotNull(table, $"자리 {slot.SlotIndex} 에 테이블 그림이 없다");

                var renderer = table.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(renderer, $"자리 {slot.SlotIndex} 의 테이블에 렌더러가 없다");
                Assert.IsNotNull(renderer.sprite, $"자리 {slot.SlotIndex} 의 테이블 그림이 비었다");

                Assert.IsNull(table.GetComponentInParent<CustomerView>(),
                              $"자리 {slot.SlotIndex} 의 테이블이 손님 뷰 안에 있다 — "
                              + "자리가 비면 함께 꺼진다");
            }
        }

        /// <summary>
        /// 요구된 HUD 배치. <b>이름으로 찾는 폴백에만 기대고 있어</b>, 오브젝트가 사라지거나
        /// 이름이 바뀌면 코드는 멀쩡한 채 라벨만 조용히 빈다 (tests.md §1 «애셋 등록·설정»).
        /// </summary>
        [Test]
        public void Stage01_HudHasStageLabelAndNoPendingLabel()
        {
            var hud = Object.FindAnyObjectByType<StageHudView>();
            Assert.IsNotNull(hud, "전제: 씬에 HUD 가 있다");

            Assert.IsNotNull(hud.transform.Find("StageLabel"),
                             "스테이지 단계 라벨이 없다 — 몇 판째인지 화면에 안 나온다");
            Assert.IsNull(hud.transform.Find("PendingCustomerLabel"),
                          "「배치 예정」 라벨이 남아 있다 — 뜻을 알 수 없다는 지적을 받은 줄이다");
        }

        /// <summary>
        /// 배너는 최상단 <b>가운데</b>, 진행 정보는 최상단 <b>우측</b>에 «단계 → 손님 → 대기»
        /// 순서로. 값이 아니라 <b>순서와 정렬</b>을 본다 — 여백은 연출이고 순서는 요구다.
        /// </summary>
        [Test]
        public void Stage01_HudLabelsSitWhereTheRequirementSays()
        {
            var hud = Object.FindAnyObjectByType<StageHudView>().transform;

            var outcome = (RectTransform)hud.Find("OutcomeLabel");
            Assert.AreEqual(new Vector2(0.5f, 1f), outcome.anchorMin, "배너가 최상단 가운데가 아니다");
            Assert.Less(outcome.anchoredPosition.y, 0f, "배너가 화면 위로 잘린다 — 패딩이 없다");

            var stage = (RectTransform)hud.Find("StageLabel");
            var placement = (RectTransform)hud.Find("PlacementLabel");
            var waiting = (RectTransform)hud.Find("WaitingLabel");

            foreach (var rect in new[] { stage, placement, waiting })
            {
                Assert.AreEqual(new Vector2(1f, 1f), rect.anchorMin,
                                $"{rect.name} 이 최상단 우측에 있지 않다");
            }

            // 위에서 아래로 단계 → 손님 → 대기. anchoredPosition.y 는 아래로 갈수록 작다.
            Assert.Greater(stage.anchoredPosition.y, placement.anchoredPosition.y,
                           "스테이지 단계가 손님 수보다 아래에 있다");
            Assert.Greater(placement.anchoredPosition.y, waiting.anchoredPosition.y,
                           "손님 수가 대기 인원보다 아래에 있다");
        }

        /// <summary>
        /// 메뉴에서 「멈춤」과 「닫기」가 빠졌는지 본다. 메뉴를 여는 것이 곧 멈춤이고,
        /// 「닫기」는 실패로 열린 메뉴에서 <b>아무것도 못 하는 판</b>으로 빠져나가게 한다.
        /// </summary>
        [Test]
        public void Stage01_StageMenuHasNoPauseOrCloseButton()
        {
            var menu = Object.FindAnyObjectByType<StageMenuView>();
            Assert.IsNotNull(menu, "전제: 씬에 메뉴가 있다");

            var panel = menu.transform.Find("Panel");
            Assert.IsNotNull(panel, "전제: 메뉴에 패널이 있다");

            Assert.IsNull(panel.Find("PauseButton"), "「멈춤」이 남아 있다");
            Assert.IsNull(panel.Find("CloseButton"), "「닫기」가 남아 있다");
            Assert.IsNotNull(panel.Find("ResumeButton"), "재개가 없으면 판으로 돌아갈 수 없다");
            Assert.IsNotNull(panel.Find("RestartButton"));
            Assert.IsNotNull(panel.Find("QuitButton"));
        }

        /// <summary>
        /// 로딩 화면에서 누른 클릭이 첫 프레임에 배달되던 것을 막는 게이트. <b>입력 배달
        /// 경로에 붙어야</b> 의미가 있다.
        /// </summary>
        [Test]
        public void Stage01_EventSystemIsArmedLate()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "전제: 씬에 EventSystem 이 있다");

            Assert.IsNotNull(eventSystem.GetComponent<UiInputArmer>(),
                             "로딩 중 클릭이 그대로 배달된다 — 준비 화면에서 누른 자리로 넘어간다");
        }

        /// <summary>
        /// 클릭음이 <b>버튼에만</b> 붙었는지 씬에서 확인한다. 손패·덱·보상 카드가 함께 잡히면
        /// 카드를 누를 때도 메뉴 소리가 난다.
        /// </summary>
        [Test]
        public void Stage01_ClickSoundHooksButtonsOnly()
        {
            var clicks = Object.FindAnyObjectByType<UiClickSound>();
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);
            var cards = Object.FindObjectsByType<CardView>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);

            Assert.IsNotNull(clicks, "UiClickSound 가 없으면 버튼이 소리를 내지 않는다");

            // 카드가 0장이면 "카드를 걸러 냈다" 가 확인되지 않고, 카드 아닌 버튼이 없으면
            // "버튼에 붙었다" 가 확인되지 않는다 — 둘 다 전제로 박는다 (tests.md §3).
            Assert.Greater(cards.Length, 0, "전제: 씬에 카드가 있다");
            Assert.Greater(buttons.Length, cards.Length, "전제: 카드가 아닌 버튼도 있다");

            // 어긋나는 방향이 둘이다: 많으면 카드가 딸려 들어간 것이고, 적으면 클릭음이
            // 버튼들보다 아래에 붙어 나머지를 못 본 것이다 (주입으로 둘 다 확인했다).
            Assert.AreEqual(buttons.Length - cards.Length, clicks.HookedCount,
                            "클릭음이 잡은 버튼 수가 어긋난다 — 많으면 카드가 딸려 들어간 것이고, "
                            + "적으면 UiClickSound 가 버튼 계층 아래에 붙은 것이다");
        }

        private static int BandWidth(SushiDefense.Data.CustomerData data) =>
            data.TargetingMax - data.TargetingMin;

        /// <summary>진행 순서 <paramref name="stageNumber"/> 번째 스테이지 설정.</summary>
        private SushiDefense.Data.StageConfig StageAt(int stageNumber)
        {
            while (_stage.Progression.CurrentStageNumber < stageNumber)
            {
                _stage.Progression.AdvanceAfterClear();
            }

            return _stage.Progression.CurrentStage;
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

        /// <summary>
        /// 씬의 카드가 <b>프리팹과 같은 크기</b>인지 본다.
        ///
        /// <para>
        /// 씬 인스턴스는 크기를 <b>오버라이드로</b> 들 수 있어서, 프리팹을 키워도 씬의 카드만
        /// 옛 크기로 남는다. 그러면 라벨은 프리팹 기준으로 배치돼 있는데 틀만 작아져
        /// <b>글자가 잘린다</b> — M6.5 에서 실제로 났고, 두 씬을 각각 손으로 고쳐야 했다.
        /// 보고 있는 것이 없어서 두 번 반복한 실수라 여기 그물을 남긴다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_CardsMatchThePrefabSize()
        {
            yield return null;

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Code/Scripts/Presentation/UI/Card.prefab");
            var expected = ((RectTransform)prefab.transform).sizeDelta;

            var checkedAny = false;
            foreach (var card in Object.FindObjectsByType<CardView>(FindObjectsInactive.Include,
                                                                   FindObjectsSortMode.None))
            {
                checkedAny = true;
                Assert.AreEqual(expected, ((RectTransform)card.transform).sizeDelta,
                                $"{card.name} 이 프리팹과 다른 크기다 — 글자가 잘린다");
            }

            Assert.IsTrue(checkedAny, "씬에 카드가 하나도 없다 — 이 테스트가 헛돈다");
#endif
        }

    }
}
