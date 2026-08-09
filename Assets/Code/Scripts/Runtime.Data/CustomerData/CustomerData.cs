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
        [SerializeField, Min(0)] private int _targetingMin;
        [SerializeField, Min(0)] private int _targetingMax;
        [SerializeField, Min(0f)] private float _eatSeconds;
        [SerializeField, Min(1)] private int _maxSaturation = 1;
        [SerializeField, Min(0f)] private float _digestSeconds;
        [SerializeField, Min(0)] private int _recruitCost;
        [SerializeField, Min(1)] private int _population = 1;
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
        /// 이 손님이 선호하는 가격대의 <b>하한</b>.
        ///
        /// <para>
        /// <b>자격 조건이 아니다.</b> 이름이 <c>Min</c> 이라 "이 아래는 못 먹는다" 로 읽히기 쉽지만
        /// 정확히 반대다 — 대역 밖 초밥도 <b>반드시</b> 먹는다. 대역이 정하는 것은 *무엇을 · 언제*
        /// 이지 *먹을 수 있는가* 가 아니며, 영구 배제는 금지다 (<c>CLAUDE.md</c> §1.1-3a).
        /// </para>
        /// </summary>
        public int TargetingMin => _targetingMin;

        /// <summary>
        /// 이 손님이 선호하는 가격대의 <b>상한</b>. <see cref="TargetingMin"/> 이상임이 보장된다.
        ///
        /// <para>
        /// 대역 밖 거리는 <c>max(0, min − 가격, 가격 − max)</c> 이고 <b>대역 안은 전부 0</b> 이다.
        /// 그 동점은 배정 정렬 키 2(고가 우선)가 깬다.
        /// </para>
        /// <para>
        /// 폭(<c>max − min</c>)도 배정에 쓰인다 — 좁은 대역이 넓은 대역을 이긴다. 다만 그 산술은
        /// 여기가 아니라 <c>Runtime</c> 의 <c>TargetingPriority</c> 에 있다. <c>Runtime.Data</c> 는
        /// 계약이지 계산이 아니다.
        /// </para>
        /// </summary>
        public int TargetingMax => _targetingMax;

        /// <summary>초밥 1개를 소비하는 데 걸리는 시간(초).</summary>
        public float EatSeconds => _eatSeconds;

        /// <summary>이 값까지 차면 포화되어 소화 상태로 넘어간다.</summary>
        public int MaxSaturation => _maxSaturation;

        /// <summary>포화 후 다시 먹을 수 있게 되기까지의 휴식 시간(초).</summary>
        public float DigestSeconds => _digestSeconds;

        /// <summary>이 손님을 배치할 때 영입 재화에서 빠지는 비용.</summary>
        public int RecruitCost => _recruitCost;

        /// <summary>
        /// 이 손님이 배치 한도에서 차지하는 몫.
        ///
        /// <para>
        /// <b>자리 수가 아니다.</b> 인구수가 2여도 앉는 자리는 하나이며, 화면에 «반쯤 앉은
        /// 손님» 은 없다. 그래서 이름이 <c>SeatCost</c> 가 아니다 — 자리를 뜻하는 이름을 붙이면
        /// 다음 사람이 반드시 자리 점유로 구현한다.
        /// </para>
        /// <para>
        /// <b>여기서는 세지 않는다.</b> 합계와 한도 비교는 <c>Runtime</c> 의
        /// <c>CustomerPlacementService</c> 한 곳이다 — <c>Runtime.Data</c> 는 계약이지 계산이
        /// 아니고, 산술이 흩어지면 한도를 고칠 때 절반만 고쳐진다.
        /// </para>
        /// </summary>
        public int Population => _population;

        /// <summary>테이블·UI 에 그릴 스프라이트.</summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// 음수·0 방어. <c>[Min]</c> 은 인스펙터 입력만 막으므로 여기서 한 번 더 조인다.
        /// 최대 포화도가 0 이면 손님이 아무것도 못 먹으므로 1 이 하한이다.
        /// 인구수도 1 이 하한이다 — 0 이면 배치 한도가 이 손님을 세지 않아 한 자리에 무한히
        /// 앉힐 수 있게 된다.
        /// 타겟팅 대역은 <c>min ≤ max</c> 가 유일한 구조 불변식이다 — <b>대역이 좁다·넓다,
        /// 덱 가격대와 맞다·아니다는 검증하지 않는다.</b> 그건 밸런스이지 오류가 아니다.
        /// </summary>
        internal void OnValidate()
        {
            _reach = Mathf.Max(0f, _reach);
            _targetingMin = Mathf.Max(0, _targetingMin);

            // 뒤집힌 대역은 "안" 이 공집합이라 모든 초밥이 대역 밖이 되고, 폭이 음수라
            // 정렬 키(좁을수록 우선)가 뒤집힌다. 상한을 하한까지 끌어올려 막는다.
            _targetingMax = Mathf.Max(_targetingMin, _targetingMax);
            _eatSeconds = Mathf.Max(0f, _eatSeconds);
            _maxSaturation = Mathf.Max(1, _maxSaturation);
            _digestSeconds = Mathf.Max(0f, _digestSeconds);
            _recruitCost = Mathf.Max(0, _recruitCost);
            _population = Mathf.Max(1, _population);
        }
    }
}
