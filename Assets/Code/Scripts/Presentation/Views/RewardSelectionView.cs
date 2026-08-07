using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SushiDefense.UI
{
    /// <summary>
    /// 보상 선택 화면 — 후보를 줄로 나열하고 숫자 키로 고른다.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 무엇을 제시할지도, 고른 것이 유효한지도
    /// <see cref="RewardSelectionPresenter"/> 가 답한다. 여기서 되묻으면 두 판정이 어긋날 수
    /// 있고 EditMode 로 검증할 수도 없다 (<c>CLAUDE.md</c> §3.2·§3.6).
    /// </para>
    /// <para>
    /// 렌더링에 <see cref="TMPro.TMP_Text"/> 를 쓰고 Canvas 위에 산다 —
    /// <c>StageHudView</c> 와 같은 방식이다.
    /// <b>스테이지 안에서 뜨는 화면은 M5 의 몫이다</b> — M6 은 메인화면·덱빌딩·설정이며,
    /// 이 뷰는 그때 교체되는 placeholder 가 아니다.
    /// </para>
    /// </summary>
    public sealed class RewardSelectionView : MonoBehaviour, IRewardSelectionView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string OffersLabelName = "RewardOffersLabel";

        /// <summary>고를 수 있는 후보가 없을 때의 문구.</summary>
        private const string NoRewardNotice = "받을 보상 없음 (Esc)";

        [SerializeField] private TMP_Text _offersLabel;

        private readonly StringBuilder _builder = new();

        private RewardSelectionPresenter _presenter;

        /// <summary>지금 표시 중인 후보 문구. 검증용이다.</summary>
        public string OffersText { get; private set; } = string.Empty;

        /// <summary>화면이 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(RewardSelectionPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <inheritdoc />
        public void ShowOffers(IReadOnlyList<string> offerNames)
        {
            IsShowing = true;
            OffersText = Compose(offerNames);
            HudLabel.Write(_offersLabel, OffersText);
            gameObject.SetActive(true);
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;
            OffersText = string.Empty;
            HudLabel.Write(_offersLabel, OffersText);
        }

        private void Awake()
        {
            _offersLabel = HudLabel.Resolve(transform, _offersLabel, OffersLabelName);
        }

        /// <summary>
        /// 숫자 키로 고르고 <c>Esc</c> 로 건너뛴다. 클릭 히트박스를 만들지 않는다 —
        /// 실제 조작감은 M6 에서 만들 UI 의 몫이다.
        /// </summary>
        private void Update()
        {
            if (_presenter == null || !_presenter.IsOpen)
            {
                return;
            }

            // UnityEngine.Input 이 아니라 Input System 을 쓴다 — 이 프로젝트는
            // ENABLE_LEGACY_INPUT_MANAGER 가 정의되어 있지 않아 레거시 API 가 런타임에
            // 예외를 던진다 (StageTransitionView 와 같은 이유).
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _presenter.Skip();
                return;
            }

            // digit1Key 부터 순서대로 놓여 있어 인덱스를 더해 집는다.
            var digits = keyboard.digit1Key;
            for (var i = 0; i < _presenter.OfferCount && i < 9; i++)
            {
                if (keyboard[(Key)((int)digits.keyCode + i)].wasPressedThisFrame)
                {
                    _presenter.Choose(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 후보를 한 문자열로 엮는다. <see cref="StringBuilder"/> 를 재사용하는 것은
        /// 성능이라기보다 습관의 문제다 — 이 경로는 판당 한 번만 돈다.
        /// </summary>
        private string Compose(IReadOnlyList<string> offerNames)
        {
            if (offerNames.Count == 0)
            {
                return NoRewardNotice;
            }

            _builder.Clear();
            _builder.Append("보상 선택");
            for (var i = 0; i < offerNames.Count; i++)
            {
                _builder.Append('\n').Append(i + 1).Append(". ").Append(offerNames[i]);
            }

            return _builder.ToString();
        }
    }
}
