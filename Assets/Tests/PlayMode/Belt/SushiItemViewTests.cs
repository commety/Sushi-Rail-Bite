using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.Belt
{
    /// <summary>
    /// 초밥 뷰가 <b>어느 그림을 그리는가</b>만 본다. 어떤 초밥이 언제 벨트에 오르는지는
    /// 벨트와 조율자의 몫이고 그쪽 테스트가 이미 덮는다.
    ///
    /// <para>
    /// 이 검증이 필요한 이유: 가격 대역이 이 게임의 핵심 규칙(M2.5)인데, 벨트 위 초밥이
    /// 전부 같아 보이면 플레이어가 그 규칙을 배울 방법이 없다.
    /// </para>
    /// </summary>
    public sealed class SushiItemViewTests
    {
        private readonly List<Object> _created = new();

        private Sprite _fallback;
        private Sprite _tuna;
        private Sprite _egg;

        [SetUp]
        public void SetUp()
        {
            _fallback = NewSprite(Color.magenta);
            _tuna = NewSprite(Color.red);
            _egg = NewSprite(Color.yellow);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
            {
                Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        [Test]
        public void Bind_DifferentSushiData_ShowsDifferentSprites()
        {
            var tunaView = NewView();
            var eggView = NewView();

            tunaView.Bind(new SushiItem(DataWithIcon(_tuna), 0));
            eggView.Bind(new SushiItem(DataWithIcon(_egg), 1));

            // "둘이 다르다" 만 보면 스프라이트를 뒤바꿔 대입하는 구현도 통과한다.
            // 어느 쪽이 어느 그림인지 못박는다.
            Assert.AreSame(_tuna, tunaView.ShownSprite);
            Assert.AreSame(_egg, eggView.ShownSprite);
        }

        [Test]
        public void Bind_DataWithoutIcon_KeepsPrefabSprite()
        {
            var view = NewView();

            view.Bind(new SushiItem(ScriptableObject.CreateInstance<SushiData>(), 0));

            // 빈 화면을 만들지 않는다 — 아이콘을 아직 안 채운 애셋에서 초밥이 사라지면
            // 벨트가 고장 난 것처럼 보인다.
            Assert.AreSame(_fallback, view.ShownSprite);
        }

        [Test]
        public void Release_AfterBind_RestoresPrefabSprite()
        {
            var view = NewView();
            view.Bind(new SushiItem(DataWithIcon(_tuna), 0));

            view.Release();

            Assert.AreSame(_fallback, view.ShownSprite);
        }

        [Test]
        public void Bind_AfterReleasingAnotherSushi_ShowsNewSprite()
        {
            // 풀에서 재사용된 뷰가 직전 초밥의 그림을 붙들고 있으면 첫 프레임에 엉뚱한
            // 초밥이 번쩍인다.
            var view = NewView();
            view.Bind(new SushiItem(DataWithIcon(_tuna), 0));
            view.Release();

            view.Bind(new SushiItem(DataWithIcon(_egg), 1));

            Assert.AreSame(_egg, view.ShownSprite);
        }

        [Test]
        public void Bind_ViewWithoutRenderer_DoesNotThrow()
        {
            // 하네스는 프리팹을 코드로 만들며 렌더러를 붙이지 않는다. 표시는 로직의
            // 전제 조건이 아니므로 조용히 넘어가야 한다.
            var go = NewObject("SushiItem_NoRenderer");
            var view = go.AddComponent<SushiItemView>();

            Assert.DoesNotThrow(() => view.Bind(new SushiItem(DataWithIcon(_tuna), 0)));
        }

        private SushiItemView NewView()
        {
            var go = NewObject("SushiItem");
            go.AddComponent<SpriteRenderer>().sprite = _fallback;
            return go.AddComponent<SushiItemView>();
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        private SushiData DataWithIcon(Sprite icon)
        {
            var data = ScriptableObject.CreateInstance<SushiData>();
            _created.Add(data);
            SetIcon(data, icon);
            return data;
        }

        private Sprite NewSprite(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            _created.Add(texture);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _created.Add(sprite);
            return sprite;
        }

        /// <summary>
        /// 아이콘을 밀어 넣는다. <c>SerializedObject</c> 대신 리플렉션을 쓰는 이유는 그쪽이
        /// <c>UnityEditor</c> 의존이라 플레이어에서 조용히 아무 일도 하지 않기 때문이다 —
        /// 테스트가 통과한 채로 검증을 잃는 형태가 된다.
        /// </summary>
        private static void SetIcon(SushiData data, Sprite icon)
        {
            var field = typeof(SushiData).GetField("_icon", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "직렬화 필드 '_icon' 을 찾지 못했습니다 — 이름이 바뀌었는지 확인하세요.");
            field.SetValue(data, icon);
        }
    }
}
