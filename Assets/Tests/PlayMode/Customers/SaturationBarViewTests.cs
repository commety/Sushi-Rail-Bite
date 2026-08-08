using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 포화도 바는 <b>계산하지 않는다</b> — 몇 칸인지는 <see cref="SaturationGauge"/> 가 정하고
    /// 그쪽 EditMode 테스트가 이미 덮는다. 여기서 볼 것은 그 결과가 <b>칸에 닿는가</b>다.
    ///
    /// <para>
    /// 칸은 프리팹에 미리 놓인다 (<c>CLAUDE.md</c> §3.4). 그래서 상한이 있고, 최대 포화도가
    /// 그 상한을 넘는 경우가 실제로 있다 — Placeholder 손님이 99 다.
    /// </para>
    /// </summary>
    public sealed class SaturationBarViewTests
    {
        private const int Capacity = 8;

        private readonly List<Object> _garbage = new();

        private SaturationBarView _bar;
        private SpriteRenderer[] _cells;

        [SetUp]
        public void SetUp()
        {
            var root = NewObject("SaturationBar");
            _cells = new SpriteRenderer[Capacity];
            for (var i = 0; i < Capacity; i++)
            {
                var cell = new GameObject($"Cell{i}");
                cell.transform.SetParent(root.transform, false);
                _cells[i] = cell.AddComponent<SpriteRenderer>();
            }

            _bar = root.AddComponent<SaturationBarView>();
            _bar.InitializeCells(_cells);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();
        }

        [Test]
        public void Bind_NoneEaten_FillsZeroCells()
        {
            _bar.Bind(NewState(maxSaturation: 5, current: 0));

            Assert.AreEqual(5, _bar.ShownVisibleCells);
            Assert.AreEqual(0, _bar.ShownFilledCells);
        }

        /// <summary>
        /// 상한 안에서는 <b>포화도가 곧 칸 수</b>다. 기대값을 숫자로 박아 상수를 돌려주는
        /// 구현을 배제한다.
        /// </summary>
        [Test]
        public void Refresh_PartiallyEaten_FillsExactCells()
        {
            var state = NewState(maxSaturation: 5, current: 0);
            _bar.Bind(state);

            state.CurrentSaturation = 3;
            _bar.Refresh();

            Assert.AreEqual(3, _bar.ShownFilledCells);
        }

        [Test]
        public void Refresh_Full_FillsEveryVisibleCell()
        {
            var state = NewState(maxSaturation: 5, current: 5);

            _bar.Bind(state);

            Assert.AreEqual(5, _bar.ShownFilledCells);
            Assert.AreEqual(5, _bar.ShownVisibleCells);
        }

        /// <summary>
        /// 표시 칸을 넘는 칸은 <b>끈다</b>. 회색으로 두면 "아직 못 채운 칸" 으로 읽혀
        /// 소식가(3)가 기본(5)처럼 보인다.
        /// </summary>
        [Test]
        public void Bind_MaxBelowCapacity_DisablesSpareCells()
        {
            _bar.Bind(NewState(maxSaturation: 3, current: 0));

            Assert.IsTrue(_cells[2].gameObject.activeSelf, "3칸째는 켜져 있어야 한다");
            Assert.IsFalse(_cells[3].gameObject.activeSelf, "4칸째부터는 꺼져 있어야 한다");
        }

        /// <summary>
        /// Placeholder 손님이 99 다. 칸을 그만큼 만들 수 없으므로 비례로 접힌다.
        /// </summary>
        [Test]
        public void Bind_MaxAboveCapacity_ShowsCapacityCells()
        {
            var state = NewState(maxSaturation: 99, current: 1);

            _bar.Bind(state);

            Assert.AreEqual(Capacity, _bar.ShownVisibleCells);
            Assert.AreEqual(1, _bar.ShownFilledCells, "한 입 먹었으면 최소 한 칸은 찬다");
        }

        [Test]
        public void Bind_Null_HidesEveryCell()
        {
            _bar.Bind(NewState(maxSaturation: 5, current: 2));

            _bar.Bind(null);

            Assert.AreEqual(0, _bar.ShownVisibleCells);
            Assert.AreEqual(0, _bar.ShownFilledCells);
            Assert.IsFalse(_cells[0].gameObject.activeSelf);
        }

        /// <summary>
        /// 자리를 갈아탈 때 직전 손님의 칸 수가 남으면 안 된다. <b>줄어드는 방향</b>으로
        /// 본다 — 늘어나는 방향은 덮어써지므로 정리하지 않는 구현도 통과한다.
        /// </summary>
        [Test]
        public void Bind_SmallerCustomerAfterBigger_ShrinksBar()
        {
            _bar.Bind(NewState(maxSaturation: 8, current: 8));

            _bar.Bind(NewState(maxSaturation: 3, current: 0));

            Assert.AreEqual(3, _bar.ShownVisibleCells);
            Assert.AreEqual(0, _bar.ShownFilledCells);
            Assert.IsFalse(_cells[7].gameObject.activeSelf);
        }

        /// <summary>
        /// 찬 칸과 빈 칸은 <b>다른 색</b>이어야 한다. 같으면 바가 있어도 아무것도 알려 주지
        /// 않는다.
        /// </summary>
        [Test]
        public void Refresh_PartiallyEaten_ColorsFilledAndEmptyDifferently()
        {
            var state = NewState(maxSaturation: 5, current: 2);

            _bar.Bind(state);

            Assert.AreNotEqual(_cells[0].color, _cells[4].color);
            Assert.AreEqual(_cells[0].color, _cells[1].color, "찬 칸끼리는 같은 색이다");
        }

        private CustomerRuntimeState NewState(int maxSaturation, int current)
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _garbage.Add(data);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(data);
            serialized.FindProperty("_maxSaturation").intValue = maxSaturation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return new CustomerRuntimeState(data, 0) { CurrentSaturation = current };
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }
    }
}
