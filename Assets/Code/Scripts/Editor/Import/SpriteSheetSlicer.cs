using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.EditorTools.Import
{
    /// <summary>
    /// 시트 한 장을 정사각 칸으로 <b>자르기만</b> 한다. 임포터도 애셋도 모르므로 EditMode 로
    /// 확인할 수 있다 — <see cref="PixelArtImportSettings"/> 가 임포터를 만지지 않는 것과
    /// 같은 이유다.
    ///
    /// <para>
    /// 순서가 계약이다: <b>왼쪽에서 오른쪽, 위에서 아래.</b> 애니메이션 프레임 순서가 여기서
    /// 정해지므로, 아래에서 위로 세면 걷는 손님이 거꾸로 걷는다. 유니티의 격자 자르기와도
    /// 같은 순서라 사람이 손으로 자른 결과와 어긋나지 않는다.
    /// </para>
    /// </summary>
    public static class SpriteSheetSlicer
    {
        /// <summary>칸의 기준점. 발밑이 아니라 가운데다 — 낱장 스프라이트와 같아야 한다.</summary>
        private const SpriteAlignment CellAlignment = SpriteAlignment.Center;

        /// <summary>
        /// 시트를 <paramref name="cell"/> 픽셀짜리 정사각 칸으로 나눈 목록을 돌려준다.
        /// 칸 크기가 시트를 나누어떨어뜨리지 않으면 <b>남는 자투리는 버린다</b> — 반 칸짜리
        /// 프레임은 애니메이션에서 그냥 잘린 그림으로 보인다.
        /// </summary>
        /// <param name="baseName">칸 이름의 앞부분. 보통 파일 이름이다.</param>
        public static SpriteMetaData[] Slice(string baseName, int width, int height, int cell)
        {
            if (cell <= 0 || width < cell || height < cell)
            {
                return System.Array.Empty<SpriteMetaData>();
            }

            var columns = width / cell;
            var rows = height / cell;
            var slices = new List<SpriteMetaData>(columns * rows);

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    // 텍스처 좌표는 아래에서 위로 자란다. 위에서 아래로 세려면 뒤집어야 한다.
                    var y = height - (row + 1) * cell;
                    slices.Add(new SpriteMetaData
                    {
                        name = $"{baseName}_{slices.Count}",
                        rect = new Rect(column * cell, y, cell, cell),
                        alignment = (int)CellAlignment,
                        pivot = new Vector2(0.5f, 0.5f)
                    });
                }
            }

            return slices.ToArray();
        }
    }
}
