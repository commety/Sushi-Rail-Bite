using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SushiDefense.EditorTools.Tiles
{
    /// <summary>
    /// <see cref="StageTilePlan"/> 을 스테이지 씬에 칠한다. <b>무엇을 어디에 칠할지는 정하지
    /// 않는다</b> — 여기 있는 것은 판을 세우고 계획을 옮기는 일뿐이다.
    ///
    /// <para>
    /// <b>다시 돌려도 같은 결과가 나온다.</b> 판을 세울 때 이미 있는 것을 찾아 쓰고, 칠하기
    /// 전에 그 판을 비운다 — 그러지 않으면 계획에서 뺀 칸이 지난번 그림으로 남아, 코드와
    /// 화면이 어긋난 채로 굳는다.
    /// </para>
    /// </summary>
    public static class StageTilemapBuilder
    {
        private const string StageScenePath = "Assets/Level/Scenes/Stage01.unity";

        /// <summary>타일맵이 매달리는 부모. 씬 루트가 지저분해지지 않게 하나로 묶는다.</summary>
        private const string GridName = "Tiles";

        /// <summary>스테이지 오브젝트가 전부 들어 있는 루트.</summary>
        private const string StageRootName = "Stage";

        /// <summary>
        /// 타일맵이 대신하는 옛 배경. 남겨 두면 타일 뒤에 그대로 깔려 있어, 타일에 빈 칸이
        /// 생겨도 눈에 띄지 않는다 — 덮여 있는 동안은 문제가 보이지 않는 종류의 잔재다.
        /// </summary>
        private static readonly string[] ReplacedObjects = { "Background", "BeltRail" };

        /// <summary>판마다의 정렬 순서. 초밥·손님(0 이상)과 테이블(-10) 보다 뒤에 있어야 한다.</summary>
        private static readonly Dictionary<StageTileLayer, int> SortingOrders = new()
        {
            [StageTileLayer.Floor] = -100,
            [StageTileLayer.Wall] = -90,
            [StageTileLayer.Belt] = -50,
            [StageTileLayer.BeltProps] = -45
        };

        /// <summary>
        /// 벨트 판이 내려앉는 높이. 칸은 정수 좌표에만 놓이는데 벨트의 중심은 y=0 이라,
        /// <b>판 자체를 반 칸 내려야</b> 칸의 가운데가 0 에 온다.
        /// </summary>
        private const float BeltLayerOffset = -0.5f;

        /// <summary>
        /// 다시 칠한다. 실패하면 <b>예외를 먼저 로그로 남기고</b> 다시 던진다 — 이 메서드는
        /// 리플렉션으로도 불리는데, 그 경로는 안쪽 예외를 «호출 대상이 예외를 던졌습니다» 로
        /// 덮어 원인이 로그 어디에도 남지 않는다.
        /// </summary>
        [MenuItem("SushiRailBite/Art/Rebuild Stage Tilemap")]
        public static void Rebuild()
        {
            try
            {
                RebuildCore();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void RebuildCore()
        {
            StageTileAssets.BuildTiles();
            StageTileAssets.BuildPalette();

            var scene = EditorSceneManager.OpenScene(StageScenePath, OpenSceneMode.Single);
            var stage = FindStageRoot(scene.GetRootGameObjects());
            var grid = EnsureGrid(stage);

            var maps = new Dictionary<StageTileLayer, Tilemap>();
            foreach (var layer in SortingOrders.Keys)
            {
                var map = EnsureTilemap(grid.transform, layer);
                map.ClearAllTiles();
                maps[layer] = map;
            }

            foreach (var placement in StageTilePlan.Build())
            {
                maps[placement.Layer].SetTile(new Vector3Int(placement.X, placement.Y, 0),
                                              StageTileAssets.Load(placement.TileId));
            }

            RemoveReplacedObjects(stage);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SushiRailBite] 타일맵을 다시 칠했습니다 — {StageScenePath}");
        }

        private static GameObject FindStageRoot(IEnumerable<GameObject> roots)
        {
            foreach (var root in roots)
            {
                if (root.name == StageRootName)
                {
                    return root;
                }
            }

            throw new System.InvalidOperationException(
                $"씬에 '{StageRootName}' 루트가 없습니다. 씬 구조가 바뀌었는지 확인하세요.");
        }

        private static Grid EnsureGrid(GameObject stage)
        {
            var existing = stage.transform.Find(GridName);
            if (existing != null && existing.TryGetComponent<Grid>(out var found))
            {
                return found;
            }

            var go = new GameObject(GridName);
            go.transform.SetParent(stage.transform, false);
            var grid = go.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            return grid;
        }

        private static Tilemap EnsureTilemap(Transform grid, StageTileLayer layer)
        {
            var name = layer.ToString();
            var existing = grid.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name);

            if (existing == null)
            {
                go.transform.SetParent(grid, false);
            }

            // `??` 를 쓰면 안 된다. 없는 컴포넌트를 가리키는 값은 CLR 에서는 null 이 아니라
            // «죽은 참조» 라, 병합 연산자가 그것을 그대로 통과시키고 다음 줄에서 터진다
            // (<c>CLAUDE.md</c> §4.3 이 말하는 `==` 오버로드 문제다).
            if (!go.TryGetComponent<Tilemap>(out var map))
            {
                map = go.AddComponent<Tilemap>();
            }

            if (!go.TryGetComponent<TilemapRenderer>(out var renderer))
            {
                renderer = go.AddComponent<TilemapRenderer>();
            }

            renderer.sortingOrder = SortingOrders[layer];

            var offset = layer is StageTileLayer.Belt or StageTileLayer.BeltProps
                ? BeltLayerOffset
                : 0f;
            go.transform.localPosition = new Vector3(0f, offset, 0f);

            return map;
        }

        private static void RemoveReplacedObjects(GameObject stage)
        {
            foreach (var name in ReplacedObjects)
            {
                var target = stage.transform.Find(name);
                if (target != null)
                {
                    Object.DestroyImmediate(target.gameObject);
                }
            }
        }
    }
}
