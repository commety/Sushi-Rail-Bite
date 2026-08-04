using System;
using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Belt
{
    /// <summary>
    /// 1차원 등속 벨트. 스폰 타이밍·위치 진행·끝점 판정만 갖는다.
    ///
    /// <para>
    /// <b>시간을 스스로 읽지 않는다.</b> <c>Time.deltaTime</c> 대신 <see cref="Tick"/> 인자로
    /// 받기 때문에 "3.7초가 흘렀을 때"를 프레임 대기 없이 EditMode 로 검증할 수 있다.
    /// </para>
    /// <para>
    /// 누가 이 초밥을 먹는지는 모른다. 자격·배정은 <c>SushiDefense.Customers</c> 쪽 몫이다.
    /// </para>
    /// </summary>
    public sealed class SushiBelt
    {
        private readonly StageConfig _config;
        private readonly SequenceNumberIssuer _sequenceNumbers;
        private readonly SushiPool<SushiItem> _pool;
        private readonly SpawnSequence _spawnSequence;
        private readonly List<SushiItem> _active = new();

        private float _spawnAccumulator;

        /// <summary>지금 벨트 위에 있는 초밥. 스폰 순서를 유지한다.</summary>
        public IReadOnlyList<SushiItem> ActiveSushi => _active;

        /// <summary>초밥이 시작점에 올라왔다. 뷰가 구독해 화면 오브젝트를 빌린다 (작업서 D3).</summary>
        public event Action<SushiItem> SushiSpawned;

        /// <summary>
        /// 초밥이 벨트에서 내려갔다 — 끝점 도달 또는 소비. 뷰를 반납할 시점이고,
        /// 손님 후보에서도 빠져야 하는 시점이다.
        /// </summary>
        public event Action<SushiItem> SushiRemoved;

        public SushiBelt(StageConfig config, SequenceNumberIssuer sequenceNumbers,
                         SushiPool<SushiItem> pool)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _sequenceNumbers = sequenceNumbers ?? throw new ArgumentNullException(nameof(sequenceNumbers));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _spawnSequence = new SpawnSequence(config.SpawnTable, config.SparsityExponent);
        }

        /// <summary>
        /// <paramref name="deltaSeconds"/> 만큼 시간을 흘린다.
        /// 기존 초밥 이동 → 스폰 → 끝점 반납 순으로 처리한다.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            Advance(deltaSeconds);
            SpawnDue(deltaSeconds);
            RemoveFinished();
        }

        private void Advance(float deltaSeconds)
        {
            var distance = _config.BeltSpeed * deltaSeconds;
            for (var i = 0; i < _active.Count; i++)
            {
                _active[i].BeltPosition += distance;
            }
        }

        private void SpawnDue(float deltaSeconds)
        {
            if (_spawnSequence.IsEmpty)
            {
                return;
            }

            _spawnAccumulator += deltaSeconds;
            while (_spawnAccumulator >= _config.SpawnIntervalSeconds)
            {
                _spawnAccumulator -= _config.SpawnIntervalSeconds;

                // 남은 누적분이 곧 "이 초밥이 스폰된 뒤 흐른 시간" 이다. 그만큼 앞서 놓아야
                // 틱 길이가 달라져도 같은 시각에 같은 위치가 나온다.
                Spawn(_config.BeltSpeed * _spawnAccumulator);
            }
        }

        private void Spawn(float startPosition)
        {
            var item = _pool.Rent();
            item.ResetForReuse(_spawnSequence.Next(), _sequenceNumbers.Next());
            item.BeltPosition = startPosition;

            _active.Add(item);
            SushiSpawned?.Invoke(item);
        }

        private void RemoveFinished()
        {
            // 제거가 뒤 원소를 당기므로 뒤에서부터 훑는다.
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].BeltPosition >= _config.BeltLength)
                {
                    ReturnAt(i);
                }
            }
        }

        private void ReturnAt(int index)
        {
            var item = _active[index];
            _active.RemoveAt(index);
            _pool.Return(item);
            SushiRemoved?.Invoke(item);
        }

        /// <summary>
        /// 손님이 먹어서 벨트에서 내린다. 끝점 도달과 같은 반납 경로를 탄다.
        /// 벨트에 없는 초밥이면 예외를 던진다 — 조용히 넘기면 풀 계정이 어긋난다.
        /// </summary>
        public void Remove(SushiItem item)
        {
            var index = _active.IndexOf(item);
            if (index < 0)
            {
                throw new ArgumentException("이 벨트 위에 없는 초밥입니다.", nameof(item));
            }

            ReturnAt(index);
        }
    }
}
