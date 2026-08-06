using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 세 손님 유형이 <b>같은 코드 경로</b>를 타는지 지키는 방어선.
    ///
    /// <para>
    /// <b>이 파일의 기대 산출물은 "프로덕션 코드 변경 0" 이다.</b> M2 의 자격 판정과 M2.5 의
    /// 대역 배정은 손님 유형을 모르도록 설계돼 있고, 유형은 <c>CustomerData</c> 값의 차이일
    /// 뿐이다. 그 설계가 옳았는지는 세 유형이 <b>하나도 안 고치고 다르게 동작하는지</b>로만
    /// 증명된다. 통과시키려고 <c>Runtime</c> 을 건드려야 한다면 그건 성과가 아니라 결함 발견이다.
    /// </para>
    /// <para>
    /// 동시에 이 마일스톤에서 가장 들어오기 쉬운 오해를 막는다 — <i>"소식 손님인데 싼 걸
    /// 먹네? 버그 아냐?"</i> <b>버그가 아니다.</b> 대역은 *무엇을 · 언제* 를 정할 뿐
    /// *먹을 수 있는가* 를 정하지 않는다 (<c>CLAUDE.md</c> §1.1-3a). 이 오해가 코드로
    /// 들어가면 손님이 눈앞의 초밥을 두고 굶는다. 아래 자격 테스트 2개가 유일한 방어선이다.
    /// </para>
    /// <para>
    /// <b>디스크의 밸런스 애셋을 로드하지 않는다</b> (<c>.claude/rules/tests.md</c> §4).
    /// 아래 값이 애셋과 같은 것은 읽는 사람을 위한 것이지 결합이 아니다 — 여기가 지키는
    /// 것은 "폭이 좁은 쪽이 이긴다" 이지 "먹보는 100~150 이다" 가 아니다.
    /// </para>
    /// </summary>
    public sealed class CustomerKindParityTests
    {
        // 3분할 대역 (M4 착수 시 확정). 폭이 서로 달라야 키 4가 드러난다.
        private const int BigEaterMin = 100;
        private const int BigEaterMax = 150;   // 폭 50 — 가장 좁다
        private const int NormalMin = 150;
        private const int NormalMax = 250;     // 폭 100 — 가장 넓다
        private const int SmallEaterMin = 250;
        private const int SmallEaterMax = 320; // 폭 70

        private const int BigEaterSaturation = 8;
        private const int SmallEaterSaturation = 3;

        /// <summary>덱의 저가·고가 대표. 어느 쪽도 세 대역 중 하나에만 든다.</summary>
        private const int CheapPrice = 120;
        private const int ExpensivePrice = 300;

        /// <summary>모든 대역 밖. 자격이 가격을 보지 않음을 보이는 데 쓴다.</summary>
        private const int OutOfEveryBandPrice = 400;

        /// <summary>경계값 — 먹보와 기본 <b>둘 다</b> 대역 거리 0 이다.</summary>
        private const int BoundaryPrice = 150;

        private const float Reach = 100f;
        private const float EatSeconds = 1f;

        private readonly List<Object> _disposables = new();

        private List<CustomerLogic> _customers;
        private Dictionary<CustomerLogic, CandidateSet> _candidates;
        private List<ClaimCandidatePair> _results;
        private SushiClaimResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new SushiClaimResolver();
            _customers = new List<CustomerLogic>();
            _candidates = new Dictionary<CustomerLogic, CandidateSet>();
            _results = new List<ClaimCandidatePair>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        // ── 자격 — 핵심 방어선 ──────────────────────────────────
        //
        // 대역이 게이트가 아님을 고정한다. 여기가 깨지면 가격이 자격 경로로 샌 것이고,
        // 손님이 눈앞의 초밥을 두고 구경하게 된다.

        /// <summary>
        /// 소식 손님의 대역은 250~320 인데 눈앞에 120 짜리뿐이다. <b>먹는다.</b>
        /// 대역이 아무리 멀어도 자격은 범위·포화도·상태만 본다.
        /// </summary>
        [Test]
        public void CanTake_SmallEaterOnlyCheapSushi_ReturnsTrue()
        {
            var smallEater = NewCustomer(0, SmallEaterMin, SmallEaterMax);
            var cheap = NewSushi(0, CheapPrice);

            Assert.IsTrue(smallEater.CanTake(cheap),
                          "대역 밖이라고 못 먹게 만들면 손님이 굶습니다 (CLAUDE.md §1.1-3a)");
        }

        /// <summary>
        /// 먹보의 대역은 100~150 인데 눈앞에 400 짜리뿐이다. 역시 <b>먹는다.</b>
        /// 반대 방향으로도 게이트가 아님을 함께 박는다 — 한쪽만 보면 상한만 뚫린
        /// 구현이 통과한다.
        /// </summary>
        [Test]
        public void CanTake_BigEaterOnlyExpensiveSushi_ReturnsTrue()
        {
            var bigEater = NewCustomer(0, BigEaterMin, BigEaterMax);
            var expensive = NewSushi(0, OutOfEveryBandPrice);

            Assert.IsTrue(bigEater.CanTake(expensive),
                          "대역 위쪽으로 벗어난 초밥도 자격은 통과합니다");
        }

        // ── 배정 1:N — 선호가 실제로 체감되나 ────────────────────

        /// <summary>
        /// 소식은 비싼 쪽을 집는다. <b>싼 초밥에 낮은 SeqNo</b> 를 준 것이 핵심이다 —
        /// 나란히 두면 "먼저 올라온 것" 을 고르는 구현도 통과한다.
        /// </summary>
        [Test]
        public void Resolve_SmallEaterMixedPrices_TakesExpensive()
        {
            var smallEater = NewCustomer(0, SmallEaterMin, SmallEaterMax);
            var cheap = Recognize(smallEater, NewSushi(0, CheapPrice));
            var expensive = Recognize(smallEater, NewSushi(9, ExpensivePrice));

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(expensive, _results[0].Sushi, "초밥 SeqNo 가 뒤여도 대역 안이 먼저입니다");
            Assert.AreEqual(SushiState.OnBelt, cheap.State);
        }

        /// <summary>
        /// 먹보는 싼 쪽을 집는다. 위 테스트의 <b>거울</b>이며, 둘이 함께 있어야
        /// <b>"고가 우선(키 2)이 최우선 키가 아니다"</b> 가 증명된다 — 하나만 있으면
        /// 언제나 비싼 것을 고르는 구현도 통과한다.
        ///
        /// <para>여기서는 <b>비싼 초밥에 낮은 SeqNo</b> 를 준다.</para>
        /// </summary>
        [Test]
        public void Resolve_BigEaterMixedPrices_TakesCheap()
        {
            var bigEater = NewCustomer(0, BigEaterMin, BigEaterMax);
            var expensive = Recognize(bigEater, NewSushi(0, ExpensivePrice));
            var cheap = Recognize(bigEater, NewSushi(9, CheapPrice));

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(cheap, _results[0].Sushi, "대역 거리(키 1)가 고가 우선(키 2)보다 앞섭니다");
            Assert.AreEqual(SushiState.OnBelt, expensive.State);
        }

        // ── 배정 N:1 — 같은 초밥을 두고 누가 이기나 ──────────────
        //
        // 초밥을 **하나만** 둔다. 둘이면 키 1~3 에서 갈려 손님 키가 아예 불리지 않는다.

        /// <summary>
        /// 비싼 초밥은 소식이 가져간다. <b>소식에게 더 높은 손님 SeqNo</b> 를 준다 —
        /// 손님 SeqNo(키 5)만 보는 구현이면 먹보가 이긴다.
        /// </summary>
        [Test]
        public void Resolve_ExpensiveSushiSmallEaterVsBigEater_SmallEaterWins()
        {
            var bigEater = NewCustomer(0, BigEaterMin, BigEaterMax);
            var smallEater = NewCustomer(1, SmallEaterMin, SmallEaterMax);
            var expensive = NewSushi(0, ExpensivePrice);
            Recognize(bigEater, expensive);
            Recognize(smallEater, expensive);

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(smallEater, _results[0].Customer, "배치가 늦어도 대역이 맞는 쪽이 이깁니다");
        }

        /// <summary>
        /// 싼 초밥은 먹보가 가져간다. 위와 <b>방향이 반대</b>여야 쌍이 의미를 갖는다 —
        /// 여기서는 <b>먹보에게 더 높은 손님 SeqNo</b> 를 준다.
        /// </summary>
        [Test]
        public void Resolve_CheapSushiSmallEaterVsBigEater_BigEaterWins()
        {
            var smallEater = NewCustomer(0, SmallEaterMin, SmallEaterMax);
            var bigEater = NewCustomer(1, BigEaterMin, BigEaterMax);
            var cheap = NewSushi(0, CheapPrice);
            Recognize(smallEater, cheap);
            Recognize(bigEater, cheap);

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(bigEater, _results[0].Customer);
        }

        /// <summary>
        /// 가격 150 은 먹보(100~150)와 기본(150~250) <b>둘 다 대역 거리 0</b> 이라 키 1~3 이
        /// 전부 동률이고, 대역 폭(키 4)이 승자를 정한다 — 폭 50 인 먹보가 이긴다.
        ///
        /// <para>
        /// <b>기본에게 낮은 SeqNo</b> 를 준다. 폭(키 4)이 손님 SeqNo(키 5)보다 앞선다는
        /// 것이 이 테스트의 전부이며, 순서를 나란히 두면 아무것도 증명하지 못한다.
        /// </para>
        /// <para>
        /// 3분할 대역의 경계값이 실제로 이렇게 갈린다는 것을 고정한다 — 밸런스를 볼 때
        /// 알고 있어야 하는 지점이다.
        /// </para>
        /// </summary>
        [Test]
        public void Resolve_BoundaryPricedSushiNormalVsBigEater_BigEaterWins()
        {
            var normal = NewCustomer(0, NormalMin, NormalMax);
            var bigEater = NewCustomer(1, BigEaterMin, BigEaterMax);
            var boundary = NewSushi(0, BoundaryPrice);
            Recognize(normal, boundary);
            Recognize(bigEater, boundary);

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(bigEater, _results[0].Customer, "좁은 대역이 배치 순서를 이깁니다");
        }

        // ── 타이밍 — 기다림 ≠ 굶기 ──────────────────────────────

        /// <summary>
        /// 소식 앞에 대역 밖 저가 하나뿐이면 <b>기다리다가 이탈 시각에 집는다.</b>
        ///
        /// <para>
        /// 세 값을 한 테스트에 함께 박는다 — 이탈 전이 <c>Nothing</c> 이 아니라
        /// <c>Waiting</c> 이라는 것(굶는 게 아니라 기다리는 것), 그리고 그 기다림이
        /// <b>유한 시간 안에</b> <c>Due</c> 로 바뀐다는 것. <c>Waiting</c> 만 확인하면
        /// 영영 확정되지 않는 구현도 통과한다.
        /// </para>
        /// </summary>
        [Test]
        public void Claim_SmallEaterOnlyCheapSushi_TakesItBeforeExit()
        {
            const float exitAt = 5f;
            var deadline = new ClaimDeadline(new ClaimPairComparer());
            var smallEater = NewCustomer(0, SmallEaterMin, SmallEaterMax);
            var candidates = _candidates[smallEater];
            candidates.Recognize(NewSushi(0, CheapPrice), exitAt);

            Assert.AreEqual(ClaimTiming.Waiting, deadline.Evaluate(smallEater, candidates, 0f),
                            "대역 밖이면 기다립니다 — 못 먹는 것이 아닙니다");
            Assert.AreEqual(ClaimTiming.Due, deadline.Evaluate(smallEater, candidates, exitAt),
                            "이탈 시각이 되면 반드시 집습니다");
        }

        // ── 포화도 — 유형 차이가 값에서 나온다 ───────────────────

        /// <summary>
        /// 먹보는 8개, 소식은 3개를 먹고 포화된다. <b>"둘이 다르다" 로 끝내지 않고</b>
        /// 구체값을 함께 박는다 — 상수를 돌려주는 구현에서도 "다르다" 는 통과할 수 있다.
        /// </summary>
        [Test]
        public void Eat_BigEaterVsSmallEater_BigEaterEatsMoreBeforeFull()
        {
            var bigEaterBites = BitesUntilFull(BigEaterSaturation);
            var smallEaterBites = BitesUntilFull(SmallEaterSaturation);

            Assert.AreEqual(BigEaterSaturation, bigEaterBites);
            Assert.AreEqual(SmallEaterSaturation, smallEaterBites);
            Assert.Greater(bigEaterBites, smallEaterBites);
        }

        /// <summary>
        /// 포화도 1 짜리 초밥을 <see cref="CustomerState.Digesting"/> 이 될 때까지 먹인다.
        /// 상한을 둬 무한 루프가 테스트를 매달지 않게 한다.
        /// </summary>
        private int BitesUntilFull(int maxSaturation)
        {
            var machine = NewAppetiteMachine(maxSaturation);
            var bites = 0;

            while (machine.State.State == CustomerState.Idle && bites < maxSaturation + 5)
            {
                machine.BeginEating(1);
                machine.Tick(EatSeconds);
                bites++;
            }

            Assert.AreEqual(CustomerState.Digesting, machine.State.State,
                            "포화되면 소화로 넘어가야 합니다");
            return bites;
        }

        private CustomerAppetiteMachine NewAppetiteMachine(int maxSaturation)
        {
            var data = NewCustomerData(BigEaterMin, BigEaterMax, maxSaturation);
            SerializedFieldSetter.SetFloat(data, "_eatSeconds", EatSeconds);
            SerializedFieldSetter.SetFloat(data, "_digestSeconds", 1f);

            return new CustomerAppetiteMachine(new CustomerRuntimeState(data, 0));
        }

        private void Resolve() => _resolver.Resolve(_customers, _candidates, _results);

        private CustomerLogic NewCustomer(int sequenceNumber, int targetingMin, int targetingMax)
        {
            var data = NewCustomerData(targetingMin, targetingMax, BigEaterSaturation);
            var customer = new CustomerLogic(new CustomerRuntimeState(data, sequenceNumber), 0f);

            _customers.Add(customer);
            _candidates[customer] = new CandidateSet(0f);
            return customer;
        }

        /// <summary>
        /// 집기 범위와 포화도는 <b>넉넉하게</b> 준다. 이 파일이 보는 것은 대역이 만드는
        /// 차이이지, 범위나 포화도가 만드는 차이가 아니다.
        /// </summary>
        private CustomerData NewCustomerData(int targetingMin, int targetingMax, int maxSaturation)
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(data);

            SerializedFieldSetter.SetFloat(data, "_reach", Reach);
            SerializedFieldSetter.SetTargetingBand(data, targetingMin, targetingMax);
            SerializedFieldSetter.SetInt(data, "_maxSaturation", maxSaturation);
            return data;
        }

        private SushiItem NewSushi(int sequenceNumber, int price)
        {
            return new SushiItem(StageConfigBuilder.CreateSushi(_disposables, price), sequenceNumber);
        }

        private SushiItem Recognize(CustomerLogic customer, SushiItem sushi)
        {
            _candidates[customer].Recognize(sushi, 0f);
            return sushi;
        }
    }
}
