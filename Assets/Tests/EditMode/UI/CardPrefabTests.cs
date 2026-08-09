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

        [Test]
        public void Prefab_HasCostLabelChild()
        {
            var label = _prefab.transform.Find("CostLabel");

            Assert.IsNotNull(label, "CostLabel 자식이 없다");
            Assert.IsNotNull(label.GetComponent<TMP_Text>());
        }

        /// <summary>
        /// 동전은 <b>카드의 직접 자식</b>이어야 한다. <see cref="CardView"/> 는
        /// <c>transform.Find</c> 로 한 겹만 찾으므로, 한 단계라도 깊어지면 조용히 못 찾고
        /// 값 없는 동전이 초밥 카드에 남는다.
        /// </summary>
        [Test]
        public void Prefab_HasCoinChild()
        {
            Assert.IsNotNull(_prefab.transform.Find("Coin"), "Coin 자식이 없다");
        }

        /// <summary>
        /// <b>비용 라벨도 직접 자식이다.</b> 동전 밑에 넣고 싶어지는 자리인데, 그러면 위와
        /// 같은 이유로 영영 안 찾힌다.
        /// </summary>
        [Test]
        public void Prefab_CostLabel_IsNotUnderTheCoin()
        {
            var label = _prefab.transform.Find("CostLabel");

            Assert.AreSame(_prefab.transform, label.parent);
        }

        /// <summary>
        /// 설명 글이 <b>카드 변에 붙지 않는다.</b> 여백이 0 이면 글자가 사각형 모서리에서
        /// 바로 시작해 답답하게 읽힌다 — 배경의 띠를 걷어내도(카드 틀 생성기) 이쪽이 0 이면
        /// 그대로다.
        ///
        /// <para>
        /// <b>값을 박지 않는 것이 이 클래스의 방침</b>인데(위 doc) 여기만 예외인 이유는,
        /// 보는 것이 «10 인가» 가 아니라 <b>«0 이 아닌가»</b> 이기 때문이다. 눈으로 여백을
        /// 조정해도 이 검사는 안 깨지고, <b>누가 0 으로 되돌리면</b> 깨진다.
        /// </para>
        /// </summary>
        [Test]
        public void Prefab_DetailLabel_HasHorizontalPadding()
        {
            var margin = _prefab.transform.Find("DetailLabel").GetComponent<TMP_Text>().margin;

            Assert.Greater(margin.x, 0f, "왼쪽 여백이 없다 — 글이 카드 변에서 시작한다");
            Assert.Greater(margin.z, 0f, "오른쪽 여백이 없다");
        }

        /// <summary>
        /// 행간이 붙어 있다. 손님 카드는 다섯 줄이라 기본 간격으로는 줄이 서로 붙어 읽힌다.
        ///
        /// <para>
        /// <b>상한도 함께 본다.</b> 폰트가 12pt·행 높이 12 라 다섯 줄이 들어갈 여유는
        /// 라벨 높이 88 에서 위아래 여백을 뺀 만큼뿐이다 — 간격을 크게 주면 아래 두 줄이
        /// 상자 밖으로 밀린다. 줄바꿈이 꺼져 있어(<c>TextWrappingMode 0</c>) 아무도 못 막는다.
        /// </para>
        /// </summary>
        [Test]
        public void Prefab_DetailLabel_HasLineSpacing()
        {
            var label = _prefab.transform.Find("DetailLabel").GetComponent<TMP_Text>();
            var rows = 5;
            var pitch = label.font.faceInfo.ascentLine - label.font.faceInfo.descentLine
                        + label.lineSpacing;
            var usable = ((RectTransform)label.transform).rect.height
                         - label.margin.y - label.margin.w;

            Assert.Greater(label.lineSpacing, 0f, "행간이 기본값이다 — 줄이 서로 붙는다");
            Assert.LessOrEqual((rows - 1) * pitch + pitch, usable,
                               $"손님 카드 {rows}줄이 라벨 높이를 넘는다 — 아래 줄이 상자 밖으로 밀린다");
        }

        /// <summary>
        /// 폰트가 비면 한글·숫자가 두부(□)가 된다. WebGL 은 OS 폰트에 접근할 수 없고,
        /// 에디터에서는 시스템 폰트가 메워 주므로 <b>빌드해야만 드러난다.</b>
        /// </summary>
        [Test]
        public void Prefab_EveryLabel_HasFont()
        {
            foreach (var name in new[] { "NameLabel", "DetailLabel", "CostLabel" })
            {
                var label = _prefab.transform.Find(name).GetComponent<TMP_Text>();
                Assert.IsNotNull(label.font, $"{name} 의 폰트가 비었다");
            }
        }
    }
}
