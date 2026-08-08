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


        /// <summary>
        /// HUD 라벨이 <b>Canvas 위에</b> 있는지 본다. 월드스페이스로 되돌아가면 브라우저 창
        /// 크기가 바뀔 때 잘리는데, 코드로 세운 하네스는 그것을 못 잡는다 (§1).
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_HudLabelsLiveOnCanvas()
        {
            yield return WaitSeconds(0.2f);

            var labels = _stage.GetComponentsInChildren<TMPro.TMP_Text>(true);
            Assert.IsNotEmpty(labels, "씬에 TMP 라벨이 하나도 없다");

            foreach (var label in labels)
            {
                Assert.IsNotNull(label.GetComponentInParent<Canvas>(),
                                 $"{label.name} 이 Canvas 밖에 있다");
            }
        }

        /// <summary>
        /// 라벨이 <b>한글 폰트를 물고 있는지</b> 본다. 비어 있으면 TMP 가 기본 폰트로
        /// 대신하는데 거기엔 한글이 없어 화면이 두부가 된다 — 빌드해야만 드러나는 실패다.
        /// </summary>
        [UnityTest]
        public IEnumerator Play_Scene_LabelsUseKoreanFont()
        {
            yield return WaitSeconds(0.2f);

            foreach (var label in _stage.GetComponentsInChildren<TMPro.TMP_Text>(true))
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
    }
}
