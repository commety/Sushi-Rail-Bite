using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.Stages
{
    /// <summary>
    /// 일시정지가 <b>실제로 시간을 멈추는지</b> 본다.
    ///
    /// <para>
    /// 이 게임의 시간은 전부 씬 진입점의 <c>Tick</c> 한 줄을 지난다. 그 줄을 건너뛰면
    /// 벨트·손님·시계가 함께 서므로, 확인해야 할 것은 <b>그 한 줄이 실제로 걸리는가</b>다 —
    /// 게이트 자체의 전이 규칙은 <c>PauseStateTests</c> 가 EditMode 로 이미 덮는다.
    /// </para>
    /// <para>
    /// <c>Time.timeScale</c> 을 쓰지 않으므로 이 테스트가 전역 상태를 오염시키지 않는다.
    /// </para>
    /// </summary>
    public sealed class StagePauseTests
    {
        private const float Speed = 10f;
        private const float Interval = 0.2f;
        private const float Length = 60f;

        private readonly List<GameObject> _objects = new();
        private readonly List<Object> _assets = new();

        private StageBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            var sushi = NewAsset<SushiData>();
            var customer = NewAsset<CustomerData>();
            var config = StageConfigTestFactory.Create(Speed, Interval, Length, sushi);
            _assets.Add(config);

            var prefab = NewObject("SushiPrefab");
            prefab.AddComponent<SushiItemView>();
            prefab.SetActive(false);

            var pool = NewObject("SushiPool").AddComponent<SushiPoolBehaviour>();
            pool.Initialize(prefab, 0);

            var beltStart = NewObject("BeltStart").transform;
            var beltEnd = NewObject("BeltEnd").transform;
            beltEnd.position = new Vector3(Length, 0f, 0f);

            var beltView = NewObject("BeltView").AddComponent<SushiBeltView>();
            var seatVisual = NewObject("SeatVisual").AddComponent<CustomerView>();

            var slot = NewObject("TableSlot").AddComponent<TableSlotView>();
            slot.Initialize(slotIndex: 0, beltPosition: 12f, seatVisual);

            var placement = NewObject("Placement").AddComponent<CustomerPlacementController>();

            _bootstrap = NewObject("StageBootstrap").AddComponent<StageBootstrap>();
            _bootstrap.Initialize(config, pool, beltView, beltStart, beltEnd,
                                  placement, new[] { slot }, new[] { customer });
            _bootstrap.Build();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                Object.DestroyImmediate(go);
            }

            foreach (var asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _objects.Clear();
            _assets.Clear();
        }

        [Test]
        public void Fresh_StageIsNotPaused()
        {
            Assert.IsNotNull(_bootstrap.Pause, "씬 진입점이 일시정지 게이트를 들고 있지 않다");
            Assert.IsFalse(_bootstrap.Pause.IsPaused);
        }

        /// <summary>
        /// <b>「값이 같다」로만 쓰지 않는다.</b> 멈춘 뒤 여러 프레임을 흘리고, 재개한 뒤에는
        /// 실제로 움직였는지를 같은 테스트에 함께 박는다 — 그러지 않으면 벨트가 아예 안 도는
        /// 구현에서도 통과한다 (<c>.claude/rules/tests.md</c> §3).
        /// </summary>
        [UnityTest]
        public IEnumerator Paused_BeltStops_AndResumeMovesAgain()
        {
            yield return WaitFrames(5);
            var beforePause = _bootstrap.Stage.RemainingSeconds;
            Assert.Less(beforePause, _bootstrap.ActiveStage.TimeLimitSeconds,
                        "멈추기 전에는 시간이 흘러야 한다");

            _bootstrap.Pause.Pause();
            var atPause = _bootstrap.Stage.RemainingSeconds;
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(atPause, _bootstrap.Stage.RemainingSeconds, 1e-4f,
                            "멈춘 동안 시계가 흘렀다");

            _bootstrap.Pause.Resume();
            yield return WaitFrames(5);

            Assert.Less(_bootstrap.Stage.RemainingSeconds, atPause, "재개했는데 시간이 안 흐른다");
        }

        /// <summary>
        /// 시계만 멈추고 벨트가 흐르면 초밥이 소리 없이 끝점으로 사라진다. 같은
        /// <c>Tick</c> 한 줄을 지나므로 함께 서야 한다.
        /// </summary>
        [UnityTest]
        public IEnumerator Paused_SushiOnBeltDoesNotMove()
        {
            // **프레임이 아니라 게임 시간으로 기다린다.** 배치 모드는 프레임이 매우 짧아
            // 수백 프레임이 스폰 간격 한 번에도 못 미친다 — 프레임 수로 세면 스폰 전에
            // 관측하고 "아무것도 없다" 로 실패한다.
            yield return WaitForSushi(maxSeconds: 3f);

            var item = FirstSushi();
            Assert.IsNotNull(item, "벨트에 초밥이 하나도 없어 이 테스트가 아무것도 보지 못한다");

            _bootstrap.Pause.Pause();
            var frozen = item.BeltPosition;
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(frozen, item.BeltPosition, 1e-4f, "멈춘 동안 벨트가 흘렀다");
        }

        /// <summary>
        /// 재시작한 판이 멈춘 채로 열리면 고장으로 보인다. 게이트는 런 수명이라 판이
        /// 바뀌어도 살아 있지만, 새 판은 흐르는 상태로 시작해야 한다.
        /// </summary>
        [UnityTest]
        public IEnumerator Retry_WhilePaused_StartsRunning()
        {
            _bootstrap.Pause.Pause();

            _bootstrap.Retry();
            yield return WaitFrames(5);

            Assert.IsFalse(_bootstrap.Pause.IsPaused);
            Assert.Less(_bootstrap.Stage.RemainingSeconds, _bootstrap.ActiveStage.TimeLimitSeconds);
        }

        /// <summary>
        /// 게이트가 <b>런 수명</b>임을 고정한다. 판마다 새로 만들면 멈춘 상태가 조용히
        /// 풀리거나, 메뉴가 들고 있는 게이트가 죽은 객체를 가리킨다.
        /// </summary>
        [Test]
        public void Retry_KeepsTheSamePauseGate()
        {
            var before = _bootstrap.Pause;

            _bootstrap.Retry();

            Assert.AreSame(before, _bootstrap.Pause);
        }

        private SushiItem FirstSushi()
        {
            var active = _bootstrap.Belt.ActiveSushi;
            return active.Count > 0 ? active[0] : null;
        }

        private IEnumerator WaitForSushi(float maxSeconds)
        {
            const float Step = 0.05f;

            for (var waited = 0f; waited < maxSeconds && FirstSushi() == null; waited += Step)
            {
                yield return new WaitForSeconds(Step);
            }
        }

        private static IEnumerator WaitFrames(int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go;
        }
    }
}
