using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 상태 전이와 타이머만 검증한다 — <b>초밥은 여기 등장하지 않는다.</b>
    ///
    /// <para>
    /// 이 클래스가 <c>SushiItem</c> 대신 포화도 기여량을 <c>int</c> 로 받는 것이 설계의
    /// 핵심이다. 가격을 볼 수 있는 경로가 타입 수준에서 없어서, "먹는 로직을 만들다가
    /// 타겟팅이 자격 쪽으로 새는" 이 마일스톤 최대 위험이 구조적으로 막힌다.
    /// </para>
    /// </summary>
    public sealed class CustomerAppetiteMachineTests
    {
        private const float EatSeconds = 2f;
        private const float DigestSeconds = 3f;
        private const int MaxSaturation = 5;

        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        // ── 먹기 시작 ─────────────────────────────────────────

        [Test]
        public void BeginEating_Idle_EntersEating()
        {
            var machine = NewMachine();

            Assert.IsTrue(machine.BeginEating(1));
            Assert.AreEqual(CustomerState.Eating, machine.State.State);
        }

        [Test]
        public void BeginEating_WhileEating_ReturnsFalse()
        {
            // 한 번에 하나 (착수 시 확정). 예약 큐를 만들지 않는다.
            var machine = NewMachine();
            machine.BeginEating(1);

            Assert.IsFalse(machine.BeginEating(1));
        }

        [Test]
        public void BeginEating_WhileDigesting_ReturnsFalse()
        {
            var machine = NewMachine();
            EatToSaturation(machine);

            Assert.AreEqual(CustomerState.Digesting, machine.State.State);
            Assert.IsFalse(machine.BeginEating(1));
        }

        // ── 먹는 시간 ─────────────────────────────────────────

        [Test]
        public void Eat_BeforeDurationElapsed_StillEating()
        {
            var machine = NewMachine();
            machine.BeginEating(1);

            machine.Tick(EatSeconds - 0.5f);

            Assert.AreEqual(CustomerState.Eating, machine.State.State);
        }

        [Test]
        public void Eat_BeforeDurationElapsed_SaturationUnchanged()
        {
            // 포화도는 시작이 아니라 완료 시점에 오른다. 시작할 때 올리면
            // "먹는 도중 포화되어 상태가 꼬이는" 경계가 생긴다.
            var machine = NewMachine();
            machine.BeginEating(3);

            machine.Tick(EatSeconds - 0.5f);

            Assert.AreEqual(0, machine.State.CurrentSaturation);
        }

        [Test]
        public void Eat_DurationElapsed_RaisesEatingFinished()
        {
            var machine = NewMachine();
            var raised = 0;
            machine.EatingFinished += () => raised++;
            machine.BeginEating(1);

            machine.Tick(EatSeconds);

            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Eat_DurationElapsed_ReturnsToIdleWhenHeadroomRemains()
        {
            var machine = NewMachine();
            machine.BeginEating(1);

            machine.Tick(EatSeconds);

            Assert.AreEqual(CustomerState.Idle, machine.State.State);
            Assert.AreEqual(1, machine.State.CurrentSaturation);
        }

        [Test]
        public void Eat_KeptTicking_DoesNotRaiseTwice()
        {
            var machine = NewMachine();
            var raised = 0;
            machine.EatingFinished += () => raised++;
            machine.BeginEating(1);
            machine.Tick(EatSeconds);

            machine.Tick(EatSeconds);

            Assert.AreEqual(1, raised);
        }

        [Test]
        public void BeginEating_ZeroEatSeconds_FinishesOnNextTick()
        {
            var machine = NewMachine(eatSeconds: 0f);
            machine.BeginEating(1);

            machine.Tick(0.016f);

            Assert.AreEqual(CustomerState.Idle, machine.State.State);
            Assert.AreEqual(1, machine.State.CurrentSaturation);
        }

        // ── 포화 · 소화 ───────────────────────────────────────

        [Test]
        public void Eat_SaturationReachesMax_EntersDigesting()
        {
            var machine = NewMachine();
            machine.BeginEating(MaxSaturation);

            machine.Tick(EatSeconds);

            Assert.AreEqual(CustomerState.Digesting, machine.State.State);
        }

        [Test]
        public void Eat_SaturationExceedsMax_ClampedAtMax()
        {
            var machine = NewMachine();
            machine.BeginEating(MaxSaturation + 99);

            machine.Tick(EatSeconds);

            Assert.AreEqual(MaxSaturation, machine.State.CurrentSaturation);
        }

        [Test]
        public void Digest_BeforeCooldown_StillDigesting()
        {
            var machine = NewMachine();
            EatToSaturation(machine);

            machine.Tick(DigestSeconds - 0.5f);

            Assert.AreEqual(CustomerState.Digesting, machine.State.State);
        }

        [Test]
        public void Digest_AfterCooldown_ReturnsToIdle()
        {
            var machine = NewMachine();
            EatToSaturation(machine);

            machine.Tick(DigestSeconds);

            Assert.AreEqual(CustomerState.Idle, machine.State.State);
        }

        [Test]
        public void Digest_AfterCooldown_ResetsSaturation()
        {
            var machine = NewMachine();
            EatToSaturation(machine);

            machine.Tick(DigestSeconds);

            Assert.AreEqual(0, machine.State.CurrentSaturation);
            Assert.IsTrue(machine.State.HasSaturationHeadroom, "소화가 끝나면 다시 먹을 수 있다");
        }

        [Test]
        public void Digest_AfterCooldown_AcceptsNextSushi()
        {
            // 자격 회복의 끝점 — 이게 맞아야 CustomerLogic.CanAcceptSushi 가 다시 참이 된다.
            var machine = NewMachine();
            EatToSaturation(machine);
            machine.Tick(DigestSeconds);

            Assert.IsTrue(machine.BeginEating(1));
        }

        // ── 경계 ──────────────────────────────────────────────

        [Test]
        public void Tick_Idle_DoesNothing()
        {
            var machine = NewMachine();
            var raised = 0;
            machine.EatingFinished += () => raised++;

            machine.Tick(100f);

            Assert.AreEqual(CustomerState.Idle, machine.State.State);
            Assert.AreEqual(0, machine.State.CurrentSaturation);
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Eat_SingleLongTick_FinishesOnce()
        {
            // 한 틱이 먹는 시간보다 훨씬 길어도 상태가 건너뛰지 않는다.
            var machine = NewMachine();
            var raised = 0;
            machine.EatingFinished += () => raised++;
            machine.BeginEating(1);

            machine.Tick(EatSeconds * 10f);

            Assert.AreEqual(1, raised);
            Assert.AreEqual(CustomerState.Idle, machine.State.State);
        }

        /// <summary>
        /// 포화까지 한 번에 먹여 <c>Digesting</c> 으로 보낸다.
        ///
        /// <para>
        /// 도달 여부를 여기서 단언한다. 이게 없으면 소화 테스트들이 "소화에 들어가지도
        /// 않은" 상태에서 통과한다 — Idle 이면 포화도 0 이고 상태도 Idle 이라 기대값과
        /// 우연히 맞아떨어지기 때문이다.
        /// </para>
        /// </summary>
        private void EatToSaturation(CustomerAppetiteMachine machine)
        {
            machine.BeginEating(MaxSaturation);
            machine.Tick(EatSeconds);

            Assert.AreEqual(CustomerState.Digesting, machine.State.State,
                            "전제 조건 — 포화까지 먹으면 소화 상태여야 한다");
        }

        private CustomerAppetiteMachine NewMachine(float eatSeconds = EatSeconds)
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(data);
            SerializedFieldSetter.SetFloat(data, "_eatSeconds", eatSeconds);
            SerializedFieldSetter.SetFloat(data, "_digestSeconds", DigestSeconds);
            SerializedFieldSetter.SetInt(data, "_maxSaturation", MaxSaturation);

            return new CustomerAppetiteMachine(new CustomerRuntimeState(data, 0));
        }
    }
}
