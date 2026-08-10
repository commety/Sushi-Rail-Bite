using TMPro;
using UnityEngine;

namespace SushiDefense.UI
{
    /// <summary>
    /// 손님 정보 창의 화면. <b>판정하지 않는다</b> — 프레젠터가 만든 글자를 라벨로 옮기기만
    /// 한다 (<c>CLAUDE.md</c> §3.2·§3.6).
    ///
    /// <para>
    /// <b>정적 줄과 실시간 줄이 다른 메서드로 들어온다.</b> 매 프레임 도는 것은 뒤쪽뿐이라,
    /// 한 메서드가 여섯 라벨을 전부 쓰면 안 바뀐 스탯 문자열까지 매 프레임 대입하게 된다.
    /// </para>
    /// </summary>
    public sealed class CustomerInspectorView : MonoBehaviour, ICustomerInspectorView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string NameLabelName = "NameLabel";

        private const string StatsLabelName = "StatsLabel";
        private const string StateLabelName = "StateLabel";
        private const string SaturationLabelName = "SaturationLabel";
        private const string RemainingLabelName = "RemainingLabel";

        /// <summary>
        /// 창의 피벗. 손님 <b>위로 자라야</b> 하므로 아래변 가운데다 — 위쪽 피벗을 주면
        /// 창이 손님을 덮는다.
        /// </summary>
        private static readonly Vector2 PanelPivot = new(0.5f, 0f);

        /// <summary>켜고 끌 대상. 비어 있으면 이 오브젝트다.</summary>
        [SerializeField] private GameObject _panel;

        /// <summary>
        /// 월드 좌표를 화면으로 옮길 카메라. 비어 있으면 주 카메라로 대신한다 —
        /// 씬 진입점이 카메라를 들고 있지 않아 인스펙터로 물릴 수도 없다.
        /// </summary>
        [SerializeField] private Camera _worldCamera;

        /// <summary>
        /// 손님과 창 아래변 사이. 연출 수치라 SO 가 아니라 여기 있다 (M5 D7).
        /// 몸통이 1 월드 유닛이고 화면에서 1 유닛 ≈ 54 캔버스 단위라, 머리 위로 나오려면
        /// 27 보다 커야 한다.
        /// </summary>
        [SerializeField, Min(0f)] private float _gap = 40f;

        /// <summary>화면 가장자리에서 띄울 여백.</summary>
        [SerializeField, Min(0f)] private float _edgeMargin = 8f;

        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _statsLabel;
        [SerializeField] private TMP_Text _stateLabel;
        [SerializeField] private TMP_Text _saturationLabel;
        [SerializeField] private TMP_Text _remainingLabel;

        /// <summary>참조를 이미 챙겼나.</summary>
        private bool _resolved;

        /// <summary>지금 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <inheritdoc />
        public void ShowCustomer(string name, string stats)
        {
            Resolve();

            HudLabel.Write(_nameLabel, name);
            HudLabel.Write(_statsLabel, stats);

            IsShowing = true;
            Apply(true);
        }

        /// <inheritdoc />
        public void RefreshLive(string state, string saturation, string remaining)
        {
            Resolve();

            HudLabel.Write(_stateLabel, state);
            HudLabel.Write(_saturationLabel, saturation);
            HudLabel.Write(_remainingLabel, remaining);
        }

        /// <inheritdoc />
        public void Hide()
        {
            Resolve();

            IsShowing = false;
            Apply(false);
        }

        /// <summary>
        /// 이 월드 좌표 <b>위에</b> 창을 세운다. 화면 밖으로 나가면 안으로 민다.
        ///
        /// <para>
        /// <b>여는 순간 한 번만 부른다.</b> 따라다니게 하면 매 프레임
        /// <c>WorldToScreenPoint</c> 가 <c>Update</c> 경로 비용이 되는데(§4.3), 벨트는 움직여도
        /// 손님은 자리에 앉아 있어 따라갈 대상이 없다.
        /// </para>
        /// <para>
        /// <b>인터페이스에 없다.</b> 프레젠터는 좌표를 모르고 글자만 넘긴다 — 좌표가 계약에
        /// 들어가면 «어디에 뜨나» 가 순수 C# 쪽으로 새어 나간다 (<c>CLAUDE.md</c> §3.6).
        /// </para>
        /// </summary>
        public void AnchorTo(Vector3 worldPoint)
        {
            Resolve();

            var rect = Target.transform as RectTransform;
            if (rect == null || rect.parent is not RectTransform parent)
            {
                return;
            }

            var world = _worldCamera != null ? _worldCamera : Camera.main;
            if (world == null)
            {
                return;
            }

            AlignToParent(rect, parent);

            // ScreenSpaceOverlay 면 canvas.worldCamera 가 null 이고, 그 null 이 맞는 인자다.
            // Camera.main 을 넘기면 좌표가 통째로 어긋난다.
            var canvas = GetComponentInParent<Canvas>();
            var uiCamera = canvas != null ? canvas.worldCamera : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, world.WorldToScreenPoint(worldPoint), uiCamera, out var local))
            {
                return;
            }

            rect.anchoredPosition = PanelAnchorMath.ClampInside(
                local + new Vector2(0f, _gap), rect.rect.size, rect.pivot, parent.rect, _edgeMargin);
        }

        private void Awake()
        {
            Resolve();
            Hide();
        }

        /// <summary>
        /// 자기 참조를 <b>한 번만</b> 챙긴다. <see cref="Awake"/> 뿐 아니라 그리는 경로에서도
        /// 부르므로 <b>활성화 순서에 기대지 않는다.</b>
        ///
        /// <para>
        /// 이 창은 씬에서 <b>꺼진 채로</b> 시작한다. 꺼진 오브젝트의 <c>Awake</c> 는 켜질
        /// 때까지 오지 않으므로, 챙기는 일을 거기에만 두면 <b>첫 번째 열기에서 라벨이 전부
        /// <c>null</c></b> 이라 빈 창이 뜬다 — 예외도 경고도 없고 두 번째부터 정상으로 보이는
        /// 것이 그 서명이다 (<c>CardView</c>·<c>CustomerView</c> 와 같은 사고다).
        /// </para>
        /// </summary>
        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            if (_panel == null)
            {
                _panel = gameObject;
            }

            // 탭 전에도 씬의 모양이 재생 중과 같아야 한다 — 어긋나 있으면 에디터에서 본
            // 자리와 실제로 뜨는 자리가 달라 배치를 두 번 맞추게 된다.
            if (Target.transform is RectTransform rect && rect.parent is RectTransform parent)
            {
                AlignToParent(rect, parent);
            }

            _nameLabel = HudLabel.Resolve(transform, _nameLabel, NameLabelName);
            _statsLabel = HudLabel.Resolve(transform, _statsLabel, StatsLabelName);
            _stateLabel = HudLabel.Resolve(transform, _stateLabel, StateLabelName);
            _saturationLabel = HudLabel.Resolve(transform, _saturationLabel, SaturationLabelName);
            _remainingLabel = HudLabel.Resolve(transform, _remainingLabel, RemainingLabelName);
        }

        /// <summary>
        /// 켜고 끄는 유일한 경로. <see cref="Awake"/> 가 돌기 전에 불릴 수 있어 대상을
        /// 여기서 한 번 더 확인한다.
        /// </summary>
        private void Apply(bool showing)
        {
            Target.SetActive(showing);
        }

        /// <summary>켜고 끄고 옮길 대상. 비어 있으면 이 오브젝트다.</summary>
        private GameObject Target => _panel != null ? _panel : gameObject;

        /// <summary>
        /// 앵커를 <b>부모의 피벗에 맞춘다.</b> 그래야
        /// <see cref="RectTransformUtility.ScreenPointToLocalPointInRectangle"/> 이 준 로컬
        /// 좌표가 곧 <c>anchoredPosition</c> 이 된다 — 어긋나면 «조금 빗나간 위치» 라
        /// 눈으로는 원인을 못 찾는다.
        ///
        /// <para>
        /// <b>배치가 아니라 이 창의 동작 계약</b>이라 씬에 맡기지 않고 코드가 못박는다.
        /// 멱등이므로 여러 번 불려도 된다.
        /// </para>
        /// </summary>
        private static void AlignToParent(RectTransform rect, RectTransform parent)
        {
            rect.anchorMin = parent.pivot;
            rect.anchorMax = parent.pivot;
            rect.pivot = PanelPivot;
        }
    }
}
