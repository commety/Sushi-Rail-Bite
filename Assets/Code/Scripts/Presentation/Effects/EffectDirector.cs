using System;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Stages;
using UnityEngine;

namespace SushiDefense.Effects
{
    /// <summary>
    /// 로직의 사건을 이펙트로 옮긴다. <b>판정하지 않는다.</b>
    ///
    /// <para>
    /// 초밥이 사라지는 순간이 지금까지 화면에서 아무 일도 아니었다 — "먹혔다" 와 "끝점에서
    /// 사라졌다" 가 구분되지 않아, 매출이 왜 올라갔는지 보이지 않았다.
    /// </para>
    /// <para>
    /// <b>인스턴스는 <see cref="SushiPoolBehaviour"/> 를 그대로 쓴다.</b> 그 컴포넌트에
    /// 초밥에만 해당하는 것은 없다 — 프리팹을 복제해 빌려주고 돌려받을 뿐이다. 같은 몸통을
    /// 이름만 바꿔 복사하면 60줄이 두 벌이 되고, 한쪽만 고치는 날이 온다. 이름이 어색한 것은
    /// M5 이후로 미뤄 둔 개명의 몫이다 (README D6).
    /// </para>
    /// </summary>
    public sealed class EffectDirector : MonoBehaviour
    {
        [SerializeField] private SushiPoolBehaviour _pool;
        [SerializeField] private Color _eatenTint = new(1f, 0.92f, 0.55f);
        [SerializeField] private Color _clearedTint = new(1f, 1f, 0.7f);
        [SerializeField] private Color _failedTint = new(0.55f, 0.58f, 0.68f);

        /// <summary>결과 연출이 뜨는 자리. 비어 있으면 이 오브젝트의 위치를 쓴다.</summary>
        [SerializeField] private Transform _centerAnchor;

        private ClaimCoordinator _coordinator;
        private StageController _stage;
        private TableSlotView[] _slots;

        /// <summary>수명이 끝난 이펙트를 돌려보낼 경로. 매 재생마다 만들지 않는다 (§4).</summary>
        private Action<GameObject> _returnToPool;

        /// <summary>지금까지 띄운 이펙트 수. 검증용이다.</summary>
        public int SpawnedCount { get; private set; }

        /// <summary>
        /// 이 판의 사건 출처를 물린다. 이미 물려 있으면 먼저 끊는다 — <c>Build()</c> 는
        /// 스테이지가 넘어갈 때마다 다시 불린다.
        /// </summary>
        public void Bind(ClaimCoordinator coordinator, StageController stage, TableSlotView[] slots)
        {
            Unbind();

            _coordinator = coordinator;
            _stage = stage;
            _slots = slots;

            if (_coordinator != null)
            {
                _coordinator.SushiEaten += OnSushiEaten;
            }

            if (_stage != null)
            {
                _stage.OutcomeDecided += OnOutcomeDecided;
            }
        }

        /// <summary>구독을 끊는다 (<c>.claude/rules/scripts.md</c> §6).</summary>
        public void Unbind()
        {
            if (_coordinator != null)
            {
                _coordinator.SushiEaten -= OnSushiEaten;
                _coordinator = null;
            }

            if (_stage != null)
            {
                _stage.OutcomeDecided -= OnOutcomeDecided;
                _stage = null;
            }

            _slots = null;
        }

        /// <summary>인스펙터가 비었을 때 형제 중에서 찾을 이름. 씬 조립과의 약속이다.</summary>
        private const string EffectPoolName = "EffectPool";

        private void Awake()
        {
            _returnToPool = ReturnToPool;
            ResolvePool();
        }

        /// <summary>
        /// 인스펙터에서 비어 있으면 <b>같은 부모 밑 형제</b>에서 이름으로 찾는다.
        /// <c>FindObjectOfType</c> 같은 씬 전역 탐색이 아니다 (§4.3 금지 대상).
        ///
        /// <para>
        /// 자식이 아니라 형제를 보는 이유: 풀은 인스턴스를 담아 두는 그릇이라 계층상
        /// 디렉터 아래 있을 이유가 없고, 씬에는 초밥 풀도 따로 있어 <b>타입으로 찾으면
        /// 둘 중 어느 것인지 알 수 없다.</b>
        /// </para>
        /// </summary>
        private void ResolvePool()
        {
            if (_pool != null || transform.parent == null)
            {
                return;
            }

            var sibling = transform.parent.Find(EffectPoolName);
            if (sibling != null)
            {
                sibling.TryGetComponent(out _pool);
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnSushiEaten(CustomerLogic customer, SushiItem sushi)
        {
            Spawn(SeatPositionOf(customer), _eatenTint);
        }

        private void OnOutcomeDecided(StageOutcome outcome)
        {
            if (outcome == StageOutcome.Cleared)
            {
                Spawn(CenterPosition(), _clearedTint);
            }
            else if (outcome == StageOutcome.Failed)
            {
                Spawn(CenterPosition(), _failedTint);
            }
        }

        /// <summary>
        /// 먹은 손님이 앉은 자리. 자리 수가 넷이라 훑어도 싸다 — 손님에게 화면 좌표를
        /// 들려 주면 <c>Runtime</c> 이 표현을 알게 된다.
        /// </summary>
        private Vector3 SeatPositionOf(CustomerLogic customer)
        {
            if (_slots == null)
            {
                return CenterPosition();
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot != null && slot.Occupant != null && slot.Occupant.Logic == customer)
                {
                    return slot.transform.position;
                }
            }

            return CenterPosition();
        }

        private Vector3 CenterPosition()
        {
            return _centerAnchor != null ? _centerAnchor.position : transform.position;
        }

        /// <summary>풀이 없으면 조용히 넘어간다 — 이펙트는 로직의 전제 조건이 아니다.</summary>
        private void Spawn(Vector3 worldPosition, Color tint)
        {
            if (_pool == null)
            {
                return;
            }

            var instance = _pool.Rent();
            if (!instance.TryGetComponent<PopEffectView>(out var view))
            {
                _pool.Return(instance);
                return;
            }

            view.Play(worldPosition, tint, _returnToPool);
            SpawnedCount++;
        }

        private void ReturnToPool(GameObject instance)
        {
            if (_pool != null)
            {
                _pool.Return(instance);
            }
        }
    }
}
