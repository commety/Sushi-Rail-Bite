using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SushiDefense.EditorTools.Tiles;

namespace SushiDefense.Tests.EditMode.Editor
{
    public sealed class StageTilePlanTests
    {
        private static IReadOnlyList<StageTilePlacement> _plan;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _plan = StageTilePlan.Build();
        }

        private static string TileAt(StageTileLayer layer, int x, int y)
        {
            var hits = _plan.Where(p => p.Layer == layer && p.X == x && p.Y == y).ToArray();
            Assert.LessOrEqual(hits.Length, 1, $"{layer} ({x},{y}) 가 두 번 칠해진다");
            return hits.Length == 1 ? hits[0].TileId : null;
        }

        [Test]
        public void Build_AnyCell_IsPaintedAtMostOnce()
        {
            // 같은 칸을 두 번 칠하면 무엇이 이겼는지 화면에서만 알 수 있다.
            var duplicates = _plan
                .GroupBy(p => (p.Layer, p.X, p.Y))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.ToString())
                .ToArray();

            Assert.IsEmpty(duplicates, string.Join(", ", duplicates));
        }

        [Test]
        public void Build_Wall_OccupiesOneRowOnly()
        {
            var rows = _plan.Where(p => p.Layer == StageTileLayer.Wall
                                        && p.TileId != StageTileId.Decor)
                            .Select(p => p.Y)
                            .Distinct()
                            .ToArray();

            Assert.AreEqual(new[] { StageTilePlan.WallRow }, rows);
        }

        [Test]
        public void Build_WallRow_StartsAndEndsWithACorner()
        {
            Assert.AreEqual(StageTileId.WallCornerLeft,
                            TileAt(StageTileLayer.Wall, StageTilePlan.FirstColumn, StageTilePlan.WallRow));
            Assert.AreEqual(StageTileId.WallCornerRight,
                            TileAt(StageTileLayer.Wall, StageTilePlan.LastColumn, StageTilePlan.WallRow));
        }

        [Test]
        public void Build_WallRow_PunchesWindowsAtTheSameStrideOnBothSides()
        {
            // 음수 칸에서 나머지 부호를 따라가는 구현을 배제한다. 화면 왼쪽 절반만 창문이
            // 어긋나는 형태라, 오른쪽만 확인하면 통과한다.
            Assert.AreEqual(StageTileId.Window, TileAt(StageTileLayer.Wall, 2, StageTilePlan.WallRow));
            Assert.AreEqual(StageTileId.Window, TileAt(StageTileLayer.Wall, -6, StageTilePlan.WallRow));
            Assert.AreEqual(StageTileId.Wall, TileAt(StageTileLayer.Wall, -5, StageTilePlan.WallRow));
        }

        [Test]
        public void Build_WallRow_NeverPunchesAWindowThroughACorner()
        {
            var corners = _plan.Where(p => p.Layer == StageTileLayer.Wall
                                           && (p.X == StageTilePlan.FirstColumn
                                               || p.X == StageTilePlan.LastColumn)
                                           && p.Y == StageTilePlan.WallRow);

            Assert.IsFalse(corners.Any(p => p.TileId == StageTileId.Window));
        }

        [Test]
        public void Build_FloorRowUnderTheWall_UsesTheEdgeTile()
        {
            Assert.AreEqual(StageTileId.FloorWallEdge,
                            TileAt(StageTileLayer.Floor, 0, StageTilePlan.FloorEdgeRow));
        }

        [Test]
        public void Build_FloorRowsBelowThat_UseThePlainTile()
        {
            Assert.AreEqual(StageTileId.Floor,
                            TileAt(StageTileLayer.Floor, 0, StageTilePlan.FloorEdgeRow - 1));
            Assert.AreEqual(StageTileId.Floor,
                            TileAt(StageTileLayer.Floor, 0, StageTilePlan.LowestFloorRow));
        }

        [Test]
        public void Build_Floor_CoversEveryVisibleColumnAndRow()
        {
            // 16:9 의 가시 범위(±8.9, ±5)를 한 칸이라도 못 덮으면 카메라 배경색이 띠로 보인다.
            for (var y = -5; y <= StageTilePlan.FloorEdgeRow; y++)
            {
                Assert.IsNotNull(TileAt(StageTileLayer.Floor, -9, y), $"(-9,{y}) 가 비었다");
                Assert.IsNotNull(TileAt(StageTileLayer.Floor, 8, y), $"(8,{y}) 가 비었다");
            }
        }

        [Test]
        public void Build_Floor_ReachesWiderThanA16By9Screen()
        {
            Assert.Less(StageTilePlan.FirstColumn, -9);
            Assert.Greater(StageTilePlan.LastColumn, 8);
        }

        [Test]
        public void Build_Belt_RunsAcrossEveryColumn()
        {
            var columns = _plan.Where(p => p.Layer == StageTileLayer.Belt).Select(p => p.X).ToArray();

            Assert.AreEqual(StageTilePlan.LastColumn - StageTilePlan.FirstColumn + 1, columns.Length);
            CollectionAssert.Contains(columns, StageTilePlan.FirstColumn);
            CollectionAssert.Contains(columns, StageTilePlan.LastColumn);
        }

        [Test]
        public void Build_BeltMouths_FaceTheDirectionSushiTravels()
        {
            // 초밥은 왼쪽에서 나와 오른쪽으로 흐른다. 좌우를 바꿔 놓으면 구멍의 어두운
            // 안쪽이 화면 밖을 향해 벽처럼 보인다.
            Assert.AreEqual(StageTileId.BeltMouthLeft,
                            TileAt(StageTileLayer.BeltProps, StageTilePlan.LeftMouthColumn,
                                   StageTilePlan.BeltRow));
            Assert.AreEqual(StageTileId.BeltMouthRight,
                            TileAt(StageTileLayer.BeltProps, StageTilePlan.RightMouthColumn,
                                   StageTilePlan.BeltRow));
        }

        [Test]
        public void Build_BeltMouths_StayInsideTheVisibleWidth()
        {
            // 여백 칸(FirstColumn/LastColumn)에 놓으면 16:9 에서 화면 밖이라 아무에게도
            // 안 보인다. 실제로 그렇게 놓아 보고 알았다.
            Assert.GreaterOrEqual(StageTilePlan.LeftMouthColumn, -9);
            Assert.LessOrEqual(StageTilePlan.RightMouthColumn, 8);

            // 그렇다고 벨트 안쪽으로 들어오면 초밥이 구멍을 통과해 지나간다.
            Assert.LessOrEqual(StageTilePlan.LeftMouthColumn, -8);
            Assert.GreaterOrEqual(StageTilePlan.RightMouthColumn, 8);
        }

        [Test]
        public void Build_BeltMouths_SitOnTopOfTheBeltInsteadOfReplacingIt()
        {
            // 같은 판에 놓으면 한 칸에 타일이 하나뿐이라 구멍 뒤가 뚫린다.
            Assert.AreEqual(StageTileId.BeltHorizontal,
                            TileAt(StageTileLayer.Belt, StageTilePlan.LeftMouthColumn,
                                   StageTilePlan.BeltRow));
        }

        [Test]
        public void Build_Decor_StandsOnTheFloorRowNextToTheWall()
        {
            var decor = _plan.Where(p => p.TileId == StageTileId.Decor).ToArray();

            Assert.IsNotEmpty(decor);
            Assert.IsTrue(decor.All(p => p.Y == StageTilePlan.FloorEdgeRow));
            Assert.IsTrue(decor.All(p => p.Layer == StageTileLayer.Wall),
                          "바닥 판에 놓으면 바닥 타일에 가려 안 보인다");
        }

        [Test]
        public void Build_TwoRuns_ProduceTheSamePlan()
        {
            var again = StageTilePlan.Build();

            CollectionAssert.AreEqual(_plan.Select(p => (p.Layer, p.X, p.Y, p.TileId)).ToArray(),
                                      again.Select(p => (p.Layer, p.X, p.Y, p.TileId)).ToArray());
        }
    }
}
