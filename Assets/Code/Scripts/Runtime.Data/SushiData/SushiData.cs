using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 초밥 1종의 정적 밸런스 데이터. 읽기 전용 템플릿이며 런타임에 값을 쓰지 않는다 —
    /// 가변 상태는 <c>SushiItem</c>(Runtime) 이 갖는다 (<c>CLAUDE.md</c> §3.1).
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Sushi Data", fileName = "SushiData")]
    public sealed class SushiData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, Min(0)] private int _price;
        [SerializeField, Min(0)] private int _saturationAmount;
        [SerializeField] private SushiTrait _trait = SushiTrait.None;
        [SerializeField] private Sprite _icon;

        /// <summary>덱·저장·백과사전에서 이 초밥을 가리키는 안정적 키.</summary>
        public string Id => _id;

        /// <summary>UI 표시용 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// 가격. 매출 기여량이자 배정 우선순위의 거리 기준이다 — 손님의
        /// <see cref="CustomerData.TargetingPrice"/> 와의 거리로 순위가 정해진다 (<c>CLAUDE.md</c> §1.1-3a).
        /// </summary>
        public int Price => _price;

        /// <summary>이 초밥을 먹은 손님의 포화도를 채우는 양.</summary>
        public int SaturationAmount => _saturationAmount;

        /// <summary>시너지 버프 발동 키 (M3). 기본값은 <see cref="SushiTrait.None"/>.</summary>
        public SushiTrait Trait => _trait;

        /// <summary>벨트 위·UI 에 그릴 스프라이트.</summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// 음수 방어. <c>[Min]</c> 은 인스펙터 입력만 막고 직렬화된 이상값·코드 대입은 통과시키므로
        /// 여기서 한 번 더 조인다.
        /// </summary>
        internal void OnValidate()
        {
            _price = Mathf.Max(0, _price);
            _saturationAmount = Mathf.Max(0, _saturationAmount);
        }
    }
}
