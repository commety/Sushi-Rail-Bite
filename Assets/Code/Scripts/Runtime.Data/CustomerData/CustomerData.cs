using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 손님 1종의 정적 밸런스 데이터. 읽기 전용 템플릿이며 런타임에 값을 쓰지 않는다 —
    /// 가변 상태는 <c>CustomerRuntimeState</c>(Runtime) 가 갖는다 (<c>CLAUDE.md</c> §3.1).
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Customer Data", fileName = "CustomerData")]
    public sealed class CustomerData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private CustomerKind _kind = CustomerKind.Normal;
        [SerializeField, Min(0f)] private float _reach;
        [SerializeField, Min(0)] private int _targetingPrice;
        [SerializeField, Min(0f)] private float _eatSeconds;
        [SerializeField, Min(1)] private int _maxSaturation = 1;
        [SerializeField, Min(0f)] private float _digestSeconds;
        [SerializeField, Min(0)] private int _recruitCost;
        [SerializeField] private Sprite _icon;

        /// <summary>덱·저장에서 이 손님을 가리키는 안정적 키.</summary>
        public string Id => _id;

        /// <summary>UI 표시용 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>UI·백과사전에 쓰는 설명문.</summary>
        public string Description => _description;

        /// <summary>표시·정렬용 유형. 로직 분기에 쓰지 않는다.</summary>
        public CustomerKind Kind => _kind;

        /// <summary>벨트 위에서 초밥을 집을 수 있는 물리적 범위. 자격 판정의 한 축이다.</summary>
        public float Reach => _reach;

        /// <summary>
        /// 이 손님이 노리는 <b>단일 가격</b>. TD 의 공격력에 대응한다.
        /// <b>자격 조건이 아니다</b> — 배정에서 <c>−|가격 − 타겟팅|</c> 로 순위를 매길 때만 쓰이고,
        /// 더 맞는 대안이 없으면 아무리 먼 가격도 먹는다 (<c>CLAUDE.md</c> §1.1-3a).
        /// </summary>
        public int TargetingPrice => _targetingPrice;

        /// <summary>초밥 1개를 소비하는 데 걸리는 시간(초).</summary>
        public float EatSeconds => _eatSeconds;

        /// <summary>이 값까지 차면 포화되어 소화 상태로 넘어간다.</summary>
        public int MaxSaturation => _maxSaturation;

        /// <summary>포화 후 다시 먹을 수 있게 되기까지의 휴식 시간(초).</summary>
        public float DigestSeconds => _digestSeconds;

        /// <summary>이 손님을 배치할 때 영입 재화에서 빠지는 비용.</summary>
        public int RecruitCost => _recruitCost;

        /// <summary>테이블·UI 에 그릴 스프라이트.</summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// 음수·0 방어. <c>[Min]</c> 은 인스펙터 입력만 막으므로 여기서 한 번 더 조인다.
        /// 최대 포화도가 0 이면 손님이 아무것도 못 먹으므로 1 이 하한이다.
        /// </summary>
        internal void OnValidate()
        {
            _reach = Mathf.Max(0f, _reach);
            _targetingPrice = Mathf.Max(0, _targetingPrice);
            _eatSeconds = Mathf.Max(0f, _eatSeconds);
            _maxSaturation = Mathf.Max(1, _maxSaturation);
            _digestSeconds = Mathf.Max(0f, _digestSeconds);
            _recruitCost = Mathf.Max(0, _recruitCost);
        }
    }
}
