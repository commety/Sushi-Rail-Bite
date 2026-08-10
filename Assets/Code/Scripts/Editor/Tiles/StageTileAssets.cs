using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace SushiDefense.EditorTools.Tiles
{
    /// <summary>
    /// 원본 스프라이트에서 타일 애셋과 팔레트를 만든다. <b>씬은 모른다</b> — 칠하는 일은
    /// <see cref="StageTilemapBuilder"/> 가 한다.
    ///
    /// <para>
    /// 여기 있는 것은 «어떤 그림이 어떤 타일이 되나» 하나뿐이다. 스프라이트를 타일맵에 바로
    /// 놓을 수는 없고 <see cref="Tile"/> 애셋을 한 번 거쳐야 하는데, 손으로 만들면 열한 개를
    /// 매번 같은 이름·같은 폴더에 만들어야 하고 하나만 어긋나도 배치 도구가 조용히 그 칸을
    /// 비운다.
    /// </para>
    /// </summary>
    public static class StageTileAssets
    {
        /// <summary>만들어진 타일이 사는 곳.</summary>
        public const string TileFolder = "Assets/Level/Tiles";

        /// <summary>사람이 붓으로 칠할 때 쓰는 팔레트.</summary>
        public const string PaletteName = "StageTilePalette";

        private const string TilemapSpriteFolder = "Assets/Art/Sprites/Tilemap";

        /// <summary>
        /// 타일 이름 → 원본 그림. 값은 <c>파일이름</c> 또는 <c>파일이름#칸번호</c> 이며,
        /// <c>#</c> 뒤 번호는 <see cref="Import.SpriteSheetSlicer"/> 가 매긴 칸 순서다.
        ///
        /// <para>
        /// 원본 파일 이름을 그대로 타일 이름으로 쓰지 않는 이유: <c>map-tile-3</c> 은 «벽의
        /// 왼쪽 끝» 이라는 사실을 어디에도 적어 두지 않는다. 그 뜻이 사는 곳은 여기 한 곳이다.
        /// </para>
        /// </summary>
        private static readonly (string TileId, string Source)[] Sources =
        {
            (StageTileId.Floor, "map-tile-1"),
            (StageTileId.FloorWallEdge, "map-tile-2"),
            (StageTileId.WallCornerLeft, "map-tile-3"),
            (StageTileId.WallCornerRight, "map-tile-4"),
            (StageTileId.Window, "map-tile-5"),
            (StageTileId.Decor, "map-tile-6"),
            (StageTileId.Wall, "map-tile-7"),
            (StageTileId.BeltHorizontal, "belt_tileset#0"),
            (StageTileId.BeltVertical, "belt_tileset#1"),
            (StageTileId.BeltMouthRight, "sushi_outgoing-tileset#0"),
            (StageTileId.BeltMouthLeft, "sushi_outgoing-tileset#1")
        };

        /// <summary>타일 이름 전부. 배치 계획이 부르는 이름과 여기가 어긋나면 칸이 빈다.</summary>
        public static IEnumerable<string> TileIds => Sources.Select(s => s.TileId);

        /// <summary>이 타일이 저장되는 경로.</summary>
        public static string PathOf(string tileId)
        {
            return $"{TileFolder}/Tile.{tileId}.asset";
        }

        /// <summary>이미 만들어진 타일을 읽는다. 없으면 <c>null</c>.</summary>
        public static Tile Load(string tileId)
        {
            return AssetDatabase.LoadAssetAtPath<Tile>(PathOf(tileId));
        }

        /// <summary>
        /// 타일 애셋을 전부 만들거나 갱신한다. 이미 있으면 그림만 갈아 끼운다 —
        /// <b>지우고 다시 만들지 않는다.</b> 새로 만들면 GUID 가 바뀌어 씬에 칠해 둔 타일이
        /// 통째로 사라진다.
        /// </summary>
        public static void BuildTiles()
        {
            EnsureFolder(TileFolder);

            foreach (var (tileId, source) in Sources)
            {
                var sprite = LoadSprite(source);
                var path = PathOf(tileId);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);

                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.color = Color.white;
                    AssetDatabase.CreateAsset(tile, path);
                    continue;
                }

                tile.sprite = sprite;
                tile.color = Color.white;
                EditorUtility.SetDirty(tile);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 붓으로 칠할 팔레트를 만든다. 이미 있으면 그대로 둔다 — 사람이 팔레트에서 타일을
        /// 옮겨 놓았을 수 있고, 다시 만들면 그 배치가 날아간다.
        ///
        /// <para>
        /// 팔레트는 <see cref="Grid"/> 를 단 프리팹이고, 타일 팔레트 창의 목록에 뜨려면
        /// <see cref="GridPalette"/> 가 그 프리팹의 하위 애셋으로 붙어 있어야 한다. 프리팹만
        /// 만들면 파일은 생기는데 창에는 나타나지 않는다.
        /// </para>
        /// </summary>
        public static void BuildPalette()
        {
            EnsureFolder(TileFolder);

            var path = $"{TileFolder}/{PaletteName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return;
            }

            var root = new GameObject(PaletteName);
            try
            {
                var grid = root.AddComponent<Grid>();
                grid.cellSize = Vector3.one;
                grid.cellLayout = GridLayout.CellLayout.Rectangle;

                var layer = new GameObject("Layer1");
                layer.transform.SetParent(root.transform);
                var tilemap = layer.AddComponent<Tilemap>();
                layer.AddComponent<TilemapRenderer>();

                var column = 0;
                foreach (var tileId in TileIds)
                {
                    tilemap.SetTile(new Vector3Int(column++, 0, 0), Load(tileId));
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);

                var palette = ScriptableObject.CreateInstance<GridPalette>();
                palette.name = "Palette Settings";
                palette.cellSizing = GridPalette.CellSizing.Manual;
                palette.transparencySortMode = TransparencySortMode.Default;
                AssetDatabase.AddObjectToAsset(palette, prefab);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 원본 그림 하나를 읽는다. 시트 안의 칸이면 <c>#</c> 뒤 번호로 고른다.
        /// 못 찾으면 던진다 — 조용히 <c>null</c> 을 물리면 그림 없는 타일이 만들어져
        /// 칠했는데 아무것도 안 보이는 상태가 된다.
        /// </summary>
        private static Sprite LoadSprite(string source)
        {
            var hash = source.IndexOf('#');
            var file = hash < 0 ? source : source[..hash];
            var path = $"{TilemapSpriteFolder}/{file}.png";

            if (hash < 0)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(path)
                       ?? throw new InvalidOperationException($"스프라이트가 없습니다: {path}");
            }

            var index = int.Parse(source[(hash + 1)..]);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();

            if (index >= sprites.Length)
            {
                throw new InvalidOperationException(
                    $"{path} 에 칸이 {sprites.Length} 개뿐입니다 ({index} 번을 찾음). "
                    + "임포트 모드가 Multiple 인지 확인하세요.");
            }

            // LoadAllAssetsAtPath 는 순서를 약속하지 않는다. 이름 끝의 번호로 다시 고른다.
            return sprites.FirstOrDefault(s => s.name.EndsWith($"_{index}", StringComparison.Ordinal))
                   ?? throw new InvalidOperationException($"{path} 에서 {index} 번 칸을 못 찾았습니다.");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
