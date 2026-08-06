using UnityEditor;
using UnityEngine;

namespace SushiDefense.EditorTools.Import
{
    /// <summary>
    /// 텍스처 한 장에 적용할 임포트 설정 묶음.
    ///
    /// <para>
    /// <b>여기 담은 것은 "빠지면 언제나 틀린 것" 뿐이다.</b> 최대 해상도 같은 상한은 넣지
    /// 않는다 — 배경처럼 정당하게 큰 이미지를 조용히 반토막 내기 때문이다. 용량은 규칙이
    /// 아니라 실측으로 다룬다.
    /// </para>
    /// </summary>
    public readonly struct PixelArtImportPlan
    {
        private PixelArtImportPlan(bool isPixelArt)
        {
            IsPixelArt = isPixelArt;
            FilterMode = FilterMode.Point;
            MipmapEnabled = false;
            PixelsPerUnit = PixelArtImportSettings.PixelsPerUnit;
            Compression = TextureImporterCompression.Uncompressed;
            AlphaIsTransparency = true;
            MeshType = SpriteMeshType.FullRect;
            TextureType = TextureImporterType.Sprite;
        }

        /// <summary>이 계획을 적용해야 하는가. <c>false</c> 면 임포터는 손을 뗀다.</summary>
        public bool IsPixelArt { get; }

        /// <summary>보간 없이 픽셀 그대로. 이것이 빠지면 픽셀 아트가 흐려진다.</summary>
        public FilterMode FilterMode { get; }

        /// <summary>축소 밉맵. 픽셀 아트에는 쓰지 않는다 — 축소본이 원본의 격자를 지운다.</summary>
        public bool MipmapEnabled { get; }

        /// <summary>1 유닛을 채우는 픽셀 수.</summary>
        public int PixelsPerUnit { get; }

        /// <summary>
        /// 블록 압축은 픽셀 경계에서 색을 뭉갠다. 배포 타깃(WebGL)의 초기 로드와 맞바꾸는
        /// 값이라, <b>색을 지키는 쪽에서 시작하고</b> 실측 뒤에 다시 본다 — 먼저 뭉개고
        /// 나중에 되돌리는 것보다 싸다.
        /// </summary>
        public TextureImporterCompression Compression { get; }

        /// <summary>투명 픽셀 가장자리의 색 번짐을 막는다.</summary>
        public bool AlphaIsTransparency { get; }

        /// <summary>
        /// 스프라이트 메시. 작은 스프라이트에서는 꽉 찬 사각형이 낫다 — 윤곽을 따는 메시는
        /// 정점이 늘고 아틀라스 패킹에서 가장자리가 잘릴 수 있다.
        /// </summary>
        public SpriteMeshType MeshType { get; }

        /// <summary>스프라이트 폴더에 들어온 텍스처는 스프라이트다.</summary>
        public TextureImporterType TextureType { get; }

        /// <summary>픽셀 아트에 적용할 계획.</summary>
        public static PixelArtImportPlan PixelArt()
        {
            return new PixelArtImportPlan(true);
        }
    }
}
