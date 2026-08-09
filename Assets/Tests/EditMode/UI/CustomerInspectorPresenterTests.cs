using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using SushiDefense.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 손님 정보 창의 로직. <b>Unity API 를 모른다</b> — 뷰는 인터페이스로만 본다
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// <b>일시정지를 건드리지 않는다.</b> 정보 조회는 멈추는 일이 아니라는 판단은 M6 D8 이
    /// 덱 보기에 대해 이미 내렸다 — 멈추면 «정보 창을 열어 시간을 번다» 가 생긴다.
    /// </para>
    /// <para>
    /// 정적 줄(이름·유형·스탯)과 실시간 줄(상태·포화도·남은 소화)을 나눠 검증한다. 합치면
    /// 매 프레임 정적 문자열까지 다시 만드는 구현이 통과한다 (§4.3).
    /// </para>
    /// </summary>
    public sealed class CustomerInspectorPresenterTests
    {
        private readonly List<Object> _garbage = new();

        private FakeCustomerInspectorView _view;
        private StageWindowArbiter _windows;
        private CustomerInspectorPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeCustomerInspectorView();
            _windows = new StageWindowArbiter();
            _presenter = new CustomerInspectorPresenter(_view, _windows);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();
        }

        // ── 창 조정 ─────────────────────────────────────────────

        [Test]
        public void Fresh_IsClosed()
        {
            Assert.IsFalse(_presenter.IsOpen);
            Assert.IsNull(_presenter.Target);
            Assert.AreEqual(0, _view.ShowCount);
        }

        [Test]
        public void Open_NothingElseOpen_Shows()
        {
            _presenter.Open(NewCustomer());

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
        }

        [Test]
        public void Open_WhileMenuOpen_DoesNotShow()
        {
            _windows.TryOpen(StageWindow.Menu);

            _presenter.Open(NewCustomer());

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(0, _view.ShowCount);
        }

        [Test]
        public void Open_WhileDeckOpen_DoesNotShow()
        {
            _windows.TryOpen(StageWindow.Deck);

            _presenter.Open(NewCustomer());

            Assert.IsFalse(_presenter.IsOpen);
        }

        /// <summary>
        /// <b>보상 위에도 열리지 않는다.</b> 보상은 «밑에 깔리는» 예외라 남을 막지 않는 창인데,
        /// 정보 창은 그 예외의 수혜자가 아니다 — 가장 아래 창은 «이미 무엇이 떠 있으면 스스로
        /// 물러난다» 가 조정자의 규칙이기 때문이다
        /// (<c>.claude/domain/in-stage-windows.md</c> §1).
        ///
        /// <para>
        /// 보상이 떠 있다는 것은 판정이 이미 났다는 뜻이라, 그때 손님 스탯을 보는 일에
        /// 의미가 없다는 판단과도 맞는다.
        /// </para>
        /// </summary>
        [Test]
        public void Open_WhileRewardOpen_DoesNotShow()
        {
            _windows.TryOpen(StageWindow.Reward);

            _presenter.Open(NewCustomer());

            Assert.IsFalse(_presenter.IsOpen);
        }

        [Test]
        public void Close_WhileOpen_ReleasesTheWindow()
        {
            _presenter.Open(NewCustomer());

            _presenter.Close();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _view.HideCount);
            Assert.IsFalse(_windows.IsOpen(StageWindow.CustomerInfo),
                           "조정자에 자리가 남아 다른 창이 못 연다");
        }

        [Test]
        public void Close_WhileClosed_DoesNothing()
        {
            _presenter.Close();

            Assert.AreEqual(0, _view.HideCount);
        }

        [Test]
        public void Open_NullCustomer_DoesNotShow()
        {
            _presenter.Open(null);

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(0, _view.ShowCount);
            Assert.IsFalse(_windows.IsOpen(StageWindow.CustomerInfo),
                           "열지도 않고 조정자 자리만 먹었다");
        }

        // ── 정적 줄 ─────────────────────────────────────────────

        [Test]
        public void Open_Customer_ShowsNameKindAndStats()
        {
            _presenter.Open(NewCustomer(name: "먹보", kind: CustomerKind.BigEater,
                                        min: 100, max: 150, reach: 3f, eat: 1f,
                                        digest: 3f, cost: 60, saturation: 8, population: 2));

            Assert.AreEqual("먹보", _view.LastName);
            Assert.AreEqual("먹보", _view.LastKind, "유형이 한글로 나오지 않는다");
            StringAssert.Contains("100~150", _view.LastStats);
            StringAssert.Contains("3", _view.LastStats);
            StringAssert.Contains("60", _view.LastStats);
        }

        /// <summary>
        /// 유형 셋이 <b>서로 다른</b> 글자로 나오는지 본다. 하나만 확인하면 상수를 돌려주는
        /// 구현도 통과한다.
        /// </summary>
        [Test]
        public void Open_EachKind_ShowsADistinctLabel()
        {
            var labels = new HashSet<string>();

            foreach (var kind in new[] { CustomerKind.Normal, CustomerKind.SmallEater,
                                         CustomerKind.BigEater })
            {
                _presenter.Open(NewCustomer(kind: kind));
                labels.Add(_view.LastKind);
            }

            Assert.AreEqual(3, labels.Count, $"유형 표기가 겹친다: {string.Join(", ", labels)}");
        }

        [Test]
        public void Open_Customer_ShowsPopulation()
        {
            _presenter.Open(NewCustomer(population: 2));

            StringAssert.Contains("2", _view.LastStats, "인구수가 안 보인다");
        }

        [Test]
        public void Open_NamelessCustomer_FallsBackToAssetName()
        {
            var data = NewData(name: string.Empty);
            data.name = "Customer.Nameless";

            _presenter.Open(Seat(data));

            Assert.AreEqual("Customer.Nameless", _view.LastName);
        }

        // ── 실시간 줄 ───────────────────────────────────────────

        [Test]
        public void Open_Customer_DrawsLiveLinesImmediately()
        {
            // 여는 순간 현재 값이 보여야 한다. 첫 변화가 올 때까지 빈 줄이면 안 된다.
            _presenter.Open(NewCustomer());

            Assert.AreEqual(1, _view.RefreshCount);
            Assert.IsNotEmpty(_view.LastSaturation);
        }

        [Test]
        public void Tick_SaturationChanged_RefreshesLive()
        {
            var logic = NewCustomer(saturation: 8);
            _presenter.Open(logic);
            var before = _view.RefreshCount;

            logic.State.CurrentSaturation = 3;
            _presenter.Tick();

            Assert.AreEqual(before + 1, _view.RefreshCount);
            StringAssert.Contains("3/8", _view.LastSaturation);
        }

        /// <summary>
        /// 위와 짝이다. 하나만 두면 <b>매 프레임 다시 그리는</b> 구현이 통과한다 (§4.3).
        /// </summary>
        [Test]
        public void Tick_NothingChanged_DoesNotCallTheView()
        {
            _presenter.Open(NewCustomer());
            var before = _view.RefreshCount;

            _presenter.Tick();
            _presenter.Tick();

            Assert.AreEqual(before, _view.RefreshCount, "안 바뀌었는데 다시 그렸다");
        }

        [Test]
        public void Tick_Digesting_ShowsRemaining()
        {
            var logic = NewCustomer();
            _presenter.Open(logic);

            logic.State.State = CustomerState.Digesting;
            logic.State.RemainingDigestSeconds = 2.4f;
            _presenter.Tick();

            StringAssert.Contains("3", _view.LastRemaining, "남은 소화 시간이 안 보인다");
        }

        /// <summary>
        /// 반례. 소화 중이 아니면 <b>비워야</b> 한다 — 상수를 돌려주는 구현을 배제한다.
        /// </summary>
        [Test]
        public void Tick_NotDigesting_LeavesRemainingBlank()
        {
            var logic = NewCustomer();
            _presenter.Open(logic);

            logic.State.State = CustomerState.Eating;
            _presenter.Tick();

            Assert.IsEmpty(_view.LastRemaining);
        }

        [Test]
        public void Tick_StateChanged_ShowsTheNewState()
        {
            var logic = NewCustomer();
            _presenter.Open(logic);
            var idle = _view.LastState;

            logic.State.State = CustomerState.Eating;
            _presenter.Tick();

            Assert.AreNotEqual(idle, _view.LastState, "상태 표기가 안 바뀐다");
            Assert.IsNotEmpty(_view.LastState);
        }

        [Test]
        public void Tick_WhileClosed_DoesNothing()
        {
            _presenter.Tick();

            Assert.AreEqual(0, _view.RefreshCount);
        }

        [Test]
        public void Tick_AfterClose_DoesNothing()
        {
            var logic = NewCustomer();
            _presenter.Open(logic);
            _presenter.Close();
            var before = _view.RefreshCount;

            logic.State.CurrentSaturation = 4;
            _presenter.Tick();

            Assert.AreEqual(before, _view.RefreshCount, "닫힌 창을 계속 그린다");
        }

        // ── 대상 전환 ───────────────────────────────────────────

        /// <summary>
        /// 다른 손님을 누르면 <b>창을 닫았다 열지 않는다.</b> 닫으면 조정자 자리가 한 번
        /// 비었다 차는데, 그 틈에 다른 창이 끼면 정보 창이 사라진다.
        /// </summary>
        [Test]
        public void Open_AnotherCustomer_ReplacesTheTarget()
        {
            var first = NewCustomer();
            var second = NewCustomer(name: "소식");
            _presenter.Open(first);

            _presenter.Open(second);

            Assert.AreSame(second, _presenter.Target);
            Assert.AreEqual("소식", _view.LastName);
            Assert.AreEqual(0, _view.HideCount, "닫았다 여는 경로를 탔다");
            Assert.AreEqual(2, _view.ShowCount);
        }

        /// <summary>
        /// 대상을 갈아탈 때 <b>실시간 줄도 반드시 다시 그린다.</b>
        ///
        /// <para>
        /// 변화 감지가 보는 키는 상태·현재 포화도·남은 초 셋인데, 갓 앉은 손님 둘은 그 셋이
        /// 대개 같다(Idle · 0 · 없음). 그러면 «안 바뀌었다» 로 판정되어 <b>다른 손님인데 직전
        /// 손님의 줄이 그대로 남는다.</b>
        /// </para>
        /// <para>
        /// 그래서 <b>최대 포화도만 다른</b> 둘을 고른다 — 키는 같고 결과 문자열은 달라서,
        /// 갱신을 건너뛰면 «0/5» 가 남는 것으로 정확히 드러난다. 둘을 완전히 같게 두면
        /// 애초에 구분할 수가 없다.
        /// </para>
        /// </summary>
        [Test]
        public void Open_AnotherCustomerWithSameLiveValues_StillRedrawsLiveLines()
        {
            _presenter.Open(NewCustomer(saturation: 5));
            Assert.AreEqual("0/5", _view.LastSaturation);

            _presenter.Open(NewCustomer(saturation: 8));

            Assert.AreEqual("0/8", _view.LastSaturation,
                            "직전 손님의 포화도가 그대로 남았다");
        }

        // ── 조립 ────────────────────────────────────────────────

        private CustomerLogic NewCustomer(string name = "기본",
                                          CustomerKind kind = CustomerKind.Normal,
                                          int min = 100, int max = 300, float reach = 3f,
                                          float eat = 1.5f, float digest = 3f, int cost = 20,
                                          int saturation = 5, int population = 1)
        {
            return Seat(NewData(name, kind, min, max, reach, eat, digest, cost,
                                saturation, population));
        }

        private static CustomerLogic Seat(CustomerData data)
        {
            return new CustomerLogic(new CustomerRuntimeState(data, 0), 0f);
        }

        private CustomerData NewData(string name = "기본",
                                         CustomerKind kind = CustomerKind.Normal,
                                         int min = 100, int max = 300, float reach = 3f,
                                         float eat = 1.5f, float digest = 3f, int cost = 20,
                                         int saturation = 5, int population = 1)
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _garbage.Add(data);

            SerializedFieldSetter.SetString(data, "_displayName", name);
            SerializedFieldSetter.SetInt(data, "_kind", (int)kind);
            SerializedFieldSetter.SetTargetingBand(data, min, max);
            SerializedFieldSetter.SetFloat(data, "_reach", reach);
            SerializedFieldSetter.SetFloat(data, "_eatSeconds", eat);
            SerializedFieldSetter.SetFloat(data, "_digestSeconds", digest);
            SerializedFieldSetter.SetInt(data, "_recruitCost", cost);
            SerializedFieldSetter.SetInt(data, "_maxSaturation", saturation);
            SerializedFieldSetter.SetInt(data, "_population", population);
            return data;
        }
    }
}
