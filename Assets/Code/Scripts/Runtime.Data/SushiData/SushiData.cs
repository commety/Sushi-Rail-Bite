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
        /// <summary>
        /// 가격의 구조적 하한. 밸런스 값이 아니라 "이 아래는 초밥이 아니다"는 불변조건이라
        /// 코드에 둔다 — 가격은 엔 단위이고 100 미만은 오류다.
        ///
        /// <para>
        /// 이 하한이 영입 재화 계산을 떠받친다. 재화는 <c>가격 / 10</c> 을 <b>정수 나눗셈</b>으로
        /// 누적하는데(M2 확정), 가격이 100 이상이면 초밥 1개당 최소 10 이 들어오므로 잔여분을
        /// 이월하는 장치 없이도 재화가 조용히 0 에 머무는 상황이 생기지 않는다.
        /// </para>
        /// </summary>
        private const int MinimumPrice = 100;

        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, Min(MinimumPrice)] private int _price = MinimumPrice;
        [SerializeField, Min(0)] private int _saturationAmount;
        [SerializeField] private SushiTrait _trait = SushiTrait.None;
        [SerializeField] private Sprite _icon;

        /// <summary>덱·저장·백과사전에서 이 초밥을 가리키는 안정적 키.</summary>
        public string Id => _id;

        /// <summary>UI 표시용 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// 가격. 매출 기여량이자 배정 우선순위의 거리 기준이다 — 손님의 선호 대역
        /// (<see cref="CustomerData.TargetingMin"/>~<see cref="CustomerData.TargetingMax"/>)
        /// 에서 얼마나 벗어났는지로 순위가 정해진다 (<c>CLAUDE.md</c> §1.1-3a).
        /// 스폰 빈도도 여기서 나온다 (<see cref="StageConfig.SparsityExponent"/>).
        /// <b>하한은 100 이다</b> — 엔 단위이며 그 아래는 오류로 본다.
        /// </summary>
        public int Price => _price;

        /// <summary>이 초밥을 먹은 손님의 포화도를 채우는 양.</summary>
        public int SaturationAmount => _saturationAmount;

        /// <summary>시너지 버프 발동 키 (M3). 기본값은 <see cref="SushiTrait.None"/>.</summary>
        public SushiTrait Trait => _trait;

        /// <summary>벨트 위·UI 에 그릴 스프라이트.</summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// 이상값 방어. <c>[Min]</c> 은 인스펙터 입력만 막고 직렬화된 이상값·코드 대입은 통과시키므로
        /// 여기서 한 번 더 조인다.
        /// </summary>
        internal void OnValidate()
        {
            _price = Mathf.Max(MinimumPrice, _price);
            _saturationAmount = Mathf.Max(0, _saturationAmount);
        }
    }
}
