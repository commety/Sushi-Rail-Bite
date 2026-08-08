using NUnit.Framework;
using SushiDefense.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 디스크의 <c>Card.prefab</c> 이 <b>실제로 그 모양인지</b> 본다.
    ///
    /// <para>
    /// <c>CardViewTests</c> 는 오브젝트를 코드로 세우므로 프리팹이 어떻게 생겼든 초록이다.
    /// 이름으로 자식을 찾는 폴백이 있어 <b>프리팹의 자식 이름이 하나만 틀려도 화면이
    /// 비는데</b>, 그것을 잡는 테스트가 여기 말고는 없다
    /// (<c>.claude/rules/tests.md</c> §1 «애셋 등록·설정»).
    /// </para>
    /// <para>
    /// <b>크기·색·간격은 단언하지 않는다.</b> 연출 수치라 사람이 만질 값이고, 박으면
    /// 레이아웃을 손볼 때마다 테스트가 깨진다.
    /// </para>
    /// </summary>
    public sealed class CardPrefabTests
    {
        private const string PrefabPath = "Assets/Code/Scripts/Presentation/UI/Card.prefab";

        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        [Test]
        public void Prefab_Exists()
        {
            Assert.IsNotNull(_prefab, $"{PrefabPath} 를 로드하지 못했다");
        }

        [Test]
        public void Prefab_HasCardView()
        {
            Assert.IsNotNull(_prefab.GetComponent<CardView>());
        }

        /// <summary>
        /// 클릭은 <c>Button</c> 이 받는다. 직접 짜지 않는 이유는 <c>EventSystem</c> 이 이미
        /// 하는 일이기 때문이다.
        /// </summary>
        [Test]
        public void Prefab_HasButton()
        {
            Assert.IsNotNull(_prefab.GetComponent<Button>());
        }

        /// <summary>
        /// 루트에 레이캐스트를 받는 그래픽이 없으면 <b>버튼이 있어도 눌리지 않는다.</b>
        /// 화면에는 카드가 멀쩡히 보이므로 눈으로는 구분되지 않는다.
        /// </summary>
        [Test]
        public void Prefab_RootGraphic_IsRaycastTarget()
        {
            var image = _prefab.GetComponent<Image>();

            Assert.IsNotNull(image, "루트에 Image 가 없다");
            Assert.IsTrue(image.raycastTarget);
        }

        [Test]
        public void Prefab_IsRectTransform()
        {
            Assert.IsInstanceOf<RectTransform>(_prefab.transform);
        }

        /// <summary>
        /// 자식 이름은 <see cref="CardView"/> 의 상수와 맺은 약속이다. 하나만 틀려도
        /// 그 칸이 조용히 비고, 예외는 나지 않는다.
        /// </summary>
        [Test]
        public void Prefab_HasIconChild()
        {
            var icon = _prefab.transform.Find("Icon");

            Assert.IsNotNull(icon, "Icon 자식이 없다");
            Assert.IsNotNull(icon.GetComponent<Image>());
        }

        [Test]
        public void Prefab_HasNameLabelChild()
        {
            var label = _prefab.transform.Find("NameLabel");

            Assert.IsNotNull(label, "NameLabel 자식이 없다");
            Assert.IsNotNull(label.GetComponent<TMP_Text>());
        }

        [Test]
        public void Prefab_HasDetailLabelChild()
        {
            var label = _prefab.transform.Find("DetailLabel");

            Assert.IsNotNull(label, "DetailLabel 자식이 없다");
            Assert.IsNotNull(label.GetComponent<TMP_Text>());
        }
    }
}
