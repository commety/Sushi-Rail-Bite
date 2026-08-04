using System.Collections.Generic;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 스테이지 1개의 정적 정의 — 클리어 조건, 배치 제한, 벨트·스폰 구성.
    /// 경과 시간·현재 매출 같은 진행 상태는 여기 두지 않는다 (<c>CLAUDE.md</c> §3.1).
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Stage Config", fileName = "StageConfig")]
    public sealed class StageConfig : ScriptableObject
    {
        /// <summary>
        /// 제한 시간의 구조적 하한. 밸런스 값이 아니라 "0초짜리 스테이지는 성립하지 않는다"는
        /// 불변 조건이라 코드에 둔다. 실제 제한 시간은 애셋에서 사람이 정한다.
        /// </summary>
        private const float MinimumTimeLimitSeconds = 1f;

        /// <summary>
        /// 스폰 간격·벨트 길이의 구조적 하한. 0 이면 초밥이 한 틱에 무한히 쏟아지거나
        /// 시작점이 곧 끝점이 되어 벨트가 성립하지 않는다. 실제 값은 애셋에서 사람이 정한다.
        /// </summary>
        private const float MinimumPositiveSeconds = 0.01f;

        [SerializeField, Min(1)] private int _stageNumber = 1;
        [SerializeField] private string _displayName;
        [SerializeField, Min(1)] private int _targetRevenue = 1;
        [SerializeField, Min(1f)] private float _timeLimitSeconds = 1f;
        [SerializeField, Min(1)] private int _maxPlacedCustomers = 1;
        [SerializeField, Min(0)] private int _initialRecruitBudget;
        [SerializeField, Min(0f)] private float _beltSpeed;
        [SerializeField, Min(0.01f)] private float _spawnIntervalSeconds = 1f;
        [SerializeField, Min(0.01f)] private float _beltLength = 1f;
        [SerializeField, Min(0f)] private float _recognitionLatchSeconds;
        [SerializeField] private List<TableSlotDefinition> _tableSlots = new();
        [SerializeField] private List<SushiSpawnEntry> _spawnTable = new();
        [SerializeField] private List<BonusObjective> _bonusObjectives = new();

        /// <summary>진행 표시용 스테이지 번호.</summary>
        public int StageNumber => _stageNumber;

        /// <summary>UI 표시용 스테이지 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>제한 시간 안에 이만큼 벌면 클리어다.</summary>
        public int TargetRevenue => _targetRevenue;

        /// <summary>스테이지 제한 시간(초).</summary>
        public float TimeLimitSeconds => _timeLimitSeconds;

        /// <summary>이 스테이지에 배치할 수 있는 손님 수의 상한.</summary>
        public int MaxPlacedCustomers => _maxPlacedCustomers;

        /// <summary>스테이지 시작 시 지급되는 영입 재화.</summary>
        public int InitialRecruitBudget => _initialRecruitBudget;

        /// <summary>벨트 위 초밥이 흐르는 속도.</summary>
        public float BeltSpeed => _beltSpeed;

        /// <summary>초밥이 시작점에 오르는 간격(초). 스테이지 내내 계속 스폰된다.</summary>
        public float SpawnIntervalSeconds => _spawnIntervalSeconds;

        /// <summary>
        /// 벨트의 1차원 길이. 초밥이 이 좌표에 닿으면 끝점 도달로 보고 풀에 반납된다
        /// (순환하지 않는다).
        /// </summary>
        public float BeltLength => _beltLength;

        /// <summary>
        /// 인식된 초밥을 후보로 붙들어 두는 시간(초).
        ///
        /// <para>
        /// 손님은 한 번 인식한 초밥을 범위를 벗어나도 놓지 않는다 — 계산 지연 때문에 눈앞에서
        /// 놓치는 그림을 막기 위해서다 (<c>CLAUDE.md</c> §1.1-3c). 다만 상한이 없으면 한참
        /// 지나간 초밥까지 집게 되므로 이 시간이 지나면 후보에서 만료된다.
        /// </para>
        /// <para>
        /// <b>0 은 "상한 없음"으로 읽는다.</b> "즉시 만료"가 아니다 — 0 을 즉시 만료로 구현하면
        /// 래치 자체가 꺼져 §1.1-3c 가 무너진다.
        /// </para>
        /// </summary>
        public float RecognitionLatchSeconds => _recognitionLatchSeconds;

        /// <summary>손님을 앉힐 수 있는 자리 목록.</summary>
        public IReadOnlyList<TableSlotDefinition> TableSlots => _tableSlots;

        /// <summary>어떤 초밥이 어떤 비중으로 벨트에 오르는지.</summary>
        public IReadOnlyList<SushiSpawnEntry> SpawnTable => _spawnTable;

        /// <summary>추가 보상을 주는 목표 목록.</summary>
        public IReadOnlyList<BonusObjective> BonusObjectives => _bonusObjectives;

        /// <summary>
        /// 클리어가 불가능해지는 값을 막는다. 목표 매출·제한 시간·최대 배치 수가 0 이면
        /// 스테이지 자체가 성립하지 않으므로 하한은 0 이 아니라 1 이다.
        /// </summary>
        internal void OnValidate()
        {
            _stageNumber = Mathf.Max(1, _stageNumber);
            _targetRevenue = Mathf.Max(1, _targetRevenue);
            _timeLimitSeconds = Mathf.Max(MinimumTimeLimitSeconds, _timeLimitSeconds);
            _maxPlacedCustomers = Mathf.Max(1, _maxPlacedCustomers);
            _initialRecruitBudget = Mathf.Max(0, _initialRecruitBudget);
            _beltSpeed = Mathf.Max(0f, _beltSpeed);
            _spawnIntervalSeconds = Mathf.Max(MinimumPositiveSeconds, _spawnIntervalSeconds);
            _beltLength = Mathf.Max(MinimumPositiveSeconds, _beltLength);

            // 0 은 "상한 없음" 이라 유효한 값이다. 하한만 막는다.
            _recognitionLatchSeconds = Mathf.Max(0f, _recognitionLatchSeconds);
        }
    }
}
