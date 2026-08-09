using System;
using SushiDefense.Customers;
using SushiDefense.Data;

namespace SushiDefense.UI
{
    /// <summary>
    /// 앉아 있는 손님 하나의 스탯과 지금 상태를 보여 준다. <b>Unity API 를 모른다</b> —
    /// 뷰는 인터페이스로만 본다 (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 손님마다 다른 값이 자리 옆 대역 라벨 두 줄에만 있어서, <b>왜 이 손님이 저 초밥을
    /// 노리는지</b>를 알 방법이 없었다. 범위·먹는 속도·포화도·소화 시간은 배치 결정을
    /// 좌우하는데 카드를 놓고 나면 어디에서도 다시 볼 수 없다.
    /// </para>
    /// <para>
    /// <b>읽기 전용이다.</b> 여기서 손님을 물리거나 옮기지 않는다 — 조작이 붙으면 이 창이
    /// 조정자의 «가장 아래» 자리에 있을 이유가 없어진다.
    /// </para>
    /// <para>
    /// <b>일시정지를 건드리지 않는다.</b> 정보 조회는 멈추는 일이 아니라는 판단은 M6 D8 이
    /// 덱 보기에 대해 이미 내렸다 — 멈추면 «정보 창을 열어 시간을 번다» 가 생긴다.
    /// 그래서 <b>멈춤을 쥔 객체를 생성자에서 받지도 않는다</b>: 받으면 언젠가 쓰게 된다.
    /// </para>
    /// </summary>
    public sealed class CustomerInspectorPresenter
    {
        private readonly ICustomerInspectorView _view;
        private readonly StageWindowArbiter _windows;

        /// <summary>
        /// 마지막으로 그린 실시간 값. <b>값이 안 바뀌면 뷰를 부르지 않는다</b> —
        /// <see cref="Tick"/> 은 매 프레임 도는 경로라 매번 문자열을 만들면 WebGL 에서
        /// GC 스파이크가 그대로 히칭이 된다 (§4.3).
        /// </summary>
        private CustomerState _shownState;

        private int _shownSaturation = -1;
        private int _shownRemaining = -1;

        /// <summary>화면이 떠 있나.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>지금 보고 있는 손님. 닫혀 있으면 <c>null</c>.</summary>
        public CustomerLogic Target { get; private set; }

        public CustomerInspectorPresenter(ICustomerInspectorView view, StageWindowArbiter windows)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));
        }

        /// <summary>
        /// 이 손님의 정보를 연다. 더 높은 창이 떠 있으면 열리지 않는다 — 정보 조회는
        /// <b>가장 약한 요구</b>라 남을 밀어내지 않고 스스로 물러난다.
        ///
        /// <para>
        /// 이미 열려 있을 때 다른 손님을 넘기면 <b>대상만 갈아탄다.</b> 닫았다 여는 경로를
        /// 타면 조정자 자리가 한 번 비는데, 그 틈에 다른 창이 끼면 정보 창이 사라진다.
        /// </para>
        /// </summary>
        /// <param name="customer"><c>null</c> 이면 아무 일도 하지 않는다.</param>
        public void Open(CustomerLogic customer)
        {
            if (customer == null || !_windows.TryOpen(StageWindow.CustomerInfo))
            {
                return;
            }

            Target = customer;
            IsOpen = true;

            var data = customer.State.Data;
            _view.ShowCustomer(CardCaption.NameOf(data), KindLabel(data.Kind), StatsOf(data));

            // 여는 순간 현재 값이 보여야 한다. 첫 변화가 올 때까지 빈 줄이면 안 된다.
            Invalidate();
            Tick();
        }

        /// <summary>
        /// 창을 닫는다. 이미 닫혀 있으면 아무 일도 하지 않는다 — 닫힌 화면을 또 닫으면
        /// 뷰가 같은 일을 두 번 한다.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            Target = null;
            _windows.Close(StageWindow.CustomerInfo);
            _view.Hide();
        }

        /// <summary>
        /// 살아 있는 줄을 다시 그린다. <b>값이 바뀐 프레임에만</b> 뷰를 부른다.
        ///
        /// <para>
        /// 소화 남은 시간은 <b>초 단위로 잘라</b> 비교한다. 올림인 이유는 0.3초 남았을 때
        /// <c>0</c> 보다 <c>1</c> 이 낫기 때문이며, 실제로 0 이 되는 순간은 소화가 끝나는
        /// 시점뿐이다 — <c>StageHudView.RefreshTime</c> 과 같은 판단이다.
        /// </para>
        /// </summary>
        public void Tick()
        {
            if (!IsOpen)
            {
                return;
            }

            var state = Target.State;
            var digesting = state.State == CustomerState.Digesting;
            var remaining = digesting ? (int)Math.Ceiling(state.RemainingDigestSeconds) : -1;

            if (state.State == _shownState
                && state.CurrentSaturation == _shownSaturation
                && remaining == _shownRemaining)
            {
                return;
            }

            _shownState = state.State;
            _shownSaturation = state.CurrentSaturation;
            _shownRemaining = remaining;

            _view.RefreshLive(StateLabel(state.State),
                              $"{state.CurrentSaturation}/{state.Data.MaxSaturation}",
                              digesting ? remaining.ToString() : string.Empty);
        }

        /// <summary>
        /// 다음 <see cref="Tick"/> 이 반드시 그리게 한다. 대상을 갈아탈 때 직전 손님과 값이
        /// 같으면 변화 감지가 막아, <b>다른 손님인데 이전 값이 남는다.</b>
        /// </summary>
        private void Invalidate()
        {
            _shownState = (CustomerState)(-1);
            _shownSaturation = -1;
            _shownRemaining = -2;
        }

        /// <summary>
        /// 유형을 사람이 읽는 말로. <b>여기서 분기하는 것은 규칙 위반이 아니다</b> —
        /// <c>CustomerKind</c> 는 그 자신이 «표시·정렬용이지 로직 분기용이 아니다» 라고
        /// 적어 두었고, 이것이 그 표시다.
        /// </summary>
        private static string KindLabel(CustomerKind kind)
        {
            return kind switch
            {
                CustomerKind.SmallEater => "소식",
                CustomerKind.BigEater => "먹보",
                _ => "기본"
            };
        }

        private static string StateLabel(CustomerState state)
        {
            return state switch
            {
                CustomerState.Eating => "먹는 중",
                CustomerState.Digesting => "소화 중",
                _ => "대기 중"
            };
        }

        /// <summary>
        /// 배치 결정을 좌우하는 값들. <b>한 번만 만든다</b> — 정적 데이터라 런타임에 바뀌지
        /// 않는다.
        /// </summary>
        private static string StatsOf(CustomerData data)
        {
            return $"범위 {data.Reach}\n"
                   + $"대역 {data.TargetingMin}~{data.TargetingMax}\n"
                   + $"먹는 시간 {data.EatSeconds}초\n"
                   + $"포화도 {data.MaxSaturation}\n"
                   + $"소화 시간 {data.DigestSeconds}초\n"
                   + $"영입 비용 {data.RecruitCost}\n"
                   + $"인구수 {data.Population}";
        }
    }
}
