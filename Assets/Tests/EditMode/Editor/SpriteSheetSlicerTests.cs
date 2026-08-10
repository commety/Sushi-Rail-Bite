using NUnit.Framework;
using SushiDefense.EditorTools.Import;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Editor
{
    public sealed class SpriteSheetSlicerTests
    {
        private const int Cell = 32;

        [Test]
        public void Slice_FourFrameStrip_ProducesFourCells()
        {
            var slices = SpriteSheetSlicer.Slice("idle", 128, 32, Cell);

            Assert.AreEqual(4, slices.Length);
        }

        [Test]
        public void Slice_FourFrameStrip_LaysCellsOutLeftToRight()
        {
            var slices = SpriteSheetSlicer.Slice("idle", 128, 32, Cell);

            // 구체값을 박는다. "x 가 서로 다르다" 만 보면 오른쪽에서 왼쪽으로 세는 구현도
            // 통과하고, 그러면 애니메이션이 거꾸로 재생된다.
            Assert.AreEqual(new Rect(0, 0, Cell, Cell), slices[0].rect);
            Assert.AreEqual(new Rect(32, 0, Cell, Cell), slices[1].rect);
            Assert.AreEqual(new Rect(96, 0, Cell, Cell), slices[3].rect);
        }

        [Test]
        public void Slice_MultipleRows_StartsAtTheTopRow()
        {
            // 텍스처 좌표는 아래에서 위로 자란다. 뒤집지 않은 구현은 첫 칸이 y=0 이 되어
            // 프레임 순서가 행 단위로 뒤집힌다.
            var slices = SpriteSheetSlicer.Slice("sheet", 64, 64, Cell);

            Assert.AreEqual(4, slices.Length);
            Assert.AreEqual(32, slices[0].rect.y);
            Assert.AreEqual(0, slices[2].rect.y);
        }

        [Test]
        public void Slice_MultipleRows_FinishesARowBeforeDescending()
        {
            var slices = SpriteSheetSlicer.Slice("sheet", 64, 64, Cell);

            Assert.AreEqual(new Rect(32, 32, Cell, Cell), slices[1].rect);
        }

        [Test]
        public void Slice_AnyCell_IsNamedInOrder()
        {
            var slices = SpriteSheetSlicer.Slice("customer-standard-idle", 128, 32, Cell);

            Assert.AreEqual("customer-standard-idle_0", slices[0].name);
            Assert.AreEqual("customer-standard-idle_3", slices[3].name);
        }

        [Test]
        public void Slice_AnyCell_PivotsAtTheCentre()
        {
            // 낱장 스프라이트와 기준점이 달라지면, 같은 손님이 애니메이션을 켜는 순간
            // 반 칸 튄다.
            var slices = SpriteSheetSlicer.Slice("idle", 128, 32, Cell);

            Assert.AreEqual(new Vector2(0.5f, 0.5f), slices[0].pivot);
            Assert.AreEqual((int)SpriteAlignment.Center, slices[0].alignment);
        }

        [Test]
        public void Slice_SheetNarrowerThanACell_ProducesNothing()
        {
            Assert.IsEmpty(SpriteSheetSlicer.Slice("tiny", 16, 16, Cell));
        }

        [Test]
        public void Slice_RaggedSheet_DropsTheLeftoverStrip()
        {
            // 반 칸짜리 프레임은 애니메이션에서 그냥 잘린 그림으로 보인다.
            var slices = SpriteSheetSlicer.Slice("ragged", 80, 32, Cell);

            Assert.AreEqual(2, slices.Length);
        }

        [Test]
        public void Slice_ZeroCell_ProducesNothingInsteadOfDividingByZero()
        {
            Assert.IsEmpty(SpriteSheetSlicer.Slice("x", 128, 32, 0));
        }
    }
}
