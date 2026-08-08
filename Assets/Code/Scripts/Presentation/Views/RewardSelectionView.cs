using System.Collections.Generic;
using SushiDefense.Run;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SushiDefense.UI
{
    /// <summary>
    /// 보상 선택 화면 — 후보를 <b>카드로</b> 늘어놓고 눌러서 고른다.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 무엇을 제시할지도, 고른 것이 유효한지도
    /// <see cref="RewardSelectionPresenter"/> 가 답한다. 여기서 되물으면 두 판정이 어긋날 수
    /// 있고 EditMode 로 검증할 수도 없다 (<c>CLAUDE.md</c> §3.2·§3.6).
    /// </para>
    /// <para>
    /// <b>카드가 자기 번호를 모른다.</b> 눌린 카드가 몇 번째인지는 이 화면이 자기 배열에서
    /// 찾아 넘긴다 — 그래야 카드를 다른 화면과 그대로 공유할 수 있다.
    /// </para>
    /// <para>
    /// 숫자 키·<c>Esc</c> 경로는 <b>남긴다.</b> 이미 동작하고 키보드가 있으면 더 빠르다 —
    /// 지울 이유가 없다. 다만 조작 안내는 버튼이 대신하므로 문구에서 뺀다.
    /// </para>
    /// </summary>
    public sealed class RewardSelectionView : MonoBehaviour, IRewardSelectionView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string TitleLabelName = "RewardOffersLabel";

        private const string SkipButtonName = "SkipButton";

        /// <summary>고를 수 있는 후보가 있을 때의 제목.</summary>
        private const string PickNotice = "보상 선택";

        /// <summary>고를 수 있는 후보가 없을 때의 문구.</summary>
        private const string NoRewardNotice = "받을 보상 없음";

        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private CardView[] _cards;
        [SerializeField] private Button _skipButton;

        private RewardSelectionPresenter _presenter;

        /// <summary>지금 표시 중인 제목 문구. 검증용이다.</summary>
        public string OffersText { get; private set; } = string.Empty;

        /// <summary>지금 그린 카드 수. 검증용이다.</summary>
        public int ShownCardCount { get; private set; }

        /// <summary>화면이 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(RewardSelectionPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>
        /// 인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.
        ///
        /// <para>
        /// <b>구독을 다시 건다.</b> <see cref="Awake"/> 가 먼저 돌므로, 여기서 참조를
        /// 갈아 끼우면 그때 걸어 둔 구독이 엉뚱한 오브젝트에 남는다 — 새 버튼은 눌러도
        /// 아무 일이 없고, 화면에는 버튼이 멀쩡히 보이므로 눈으로 구분되지 않는다.
        /// </para>
        /// </summary>
        public void Initialize(CardView[] cards, TMP_Text titleLabel, Button skipButton)
        {
            Unsubscribe();

            _cards = cards;
            _titleLabel = titleLabel;
            _skipButton = skipButton;

            Subscribe();
        }

        /// <inheritdoc />
        public void ShowOffers(IReadOnlyList<RewardOffer> offers)
        {
            IsShowing = true;
            ShownCardCount = 0;

            if (_cards != null)
            {
                for (var i = 0; i < _cards.Length; i++)
                {
                    var card = _cards[i];
                    if (card == null)
                    {
                        continue;
                    }

                    if (offers != null && i < offers.Count)
                    {
                        Draw(card, offers[i]);
                        ShownCardCount++;
                    }
                    else
                    {
                        card.Clear();
                    }
                }
            }

            OffersText = ShownCardCount == 0 ? NoRewardNotice : PickNotice;
            HudLabel.Write(_titleLabel, OffersText);

            // 후보가 0개여도 건너뛰기는 남는다 — 그때는 그것이 **유일한 출구**다.
            SetSkipActive(true);
            gameObject.SetActive(true);
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;
            ShownCardCount = 0;
            OffersText = string.Empty;

            if (_cards != null)
            {
                for (var i = 0; i < _cards.Length; i++)
                {
                    _cards[i]?.Clear();
                }
            }

            HudLabel.Write(_titleLabel, OffersText);
            SetSkipActive(false);
        }

        private void Awake()
        {
            _titleLabel = HudLabel.Resolve(transform, _titleLabel, TitleLabelName);

            if (_cards == null || _cards.Length == 0)
            {
                _cards = GetComponentsInChildren<CardView>(true);
            }

            // 이름으로 찾는 폴백이 없으면, 인스펙터에 물리는 것을 잊었을 때 건너뛰기가
            // 조용히 죽는다 — 후보가 0개일 때는 그것이 유일한 출구라 화면에 갇힌다.
            if (_skipButton == null)
            {
                var child = transform.Find(SkipButtonName);
                if (child != null)
                {
                    child.TryGetComponent(out _skipButton);
                }
            }

            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_skipButton != null)
            {
                _skipButton.onClick.AddListener(OnSkipClicked);
            }

            if (_cards == null)
            {
                return;
            }

            for (var i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null)
                {
                    _cards[i].Clicked += OnCardClicked;
                }
            }
        }

        private void Unsubscribe()
        {
            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveListener(OnSkipClicked);
            }

            if (_cards == null)
            {
                return;
            }

            for (var i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null)
                {
                    _cards[i].Clicked -= OnCardClicked;
                }
            }
        }

        /// <summary>
        /// 어느 <c>Show</c> 를 부를지 <see cref="RewardOffer.Kind"/> 로 정한다.
        /// <b>카드가 분기하지 않는다</b> — 부르는 쪽이 무엇을 넘기는지 이미 안다.
        /// </summary>
        private static void Draw(CardView card, RewardOffer offer)
        {
            if (offer.Kind == RewardKind.SushiCard)
            {
                card.Show(offer.Sushi);
            }
            else
            {
                card.Show(offer.Customer);
            }
        }

        /// <summary>
        /// 눌린 카드가 몇 번째인지 배열에서 찾아 넘긴다. <b>유효한 번호인지는 프레젠터가
        /// 답한다</b> — 범위 밖이면 <c>false</c> 를 돌려주고 화면을 그대로 두는 계약이 이미 있다.
        /// </summary>
        private void OnCardClicked(CardView card)
        {
            if (_presenter == null || _cards == null)
            {
                return;
            }

            for (var i = 0; i < _cards.Length; i++)
            {
                if (ReferenceEquals(_cards[i], card))
                {
                    _presenter.Choose(i);
                    return;
                }
            }
        }

        private void OnSkipClicked()
        {
            _presenter?.Skip();
        }

        private void SetSkipActive(bool active)
        {
            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// 숫자 키로 고르고 <c>Esc</c> 로 건너뛴다. 버튼이 생겼어도 <b>지우지 않는다</b> —
        /// 이미 동작하고 키보드가 있으면 더 빠르다.
        ///
        /// <para>
        /// <b>이 경로는 테스트가 밟지 않는다.</b> 화면이 열려야 돌고 키보드 장치가 있어야
        /// 하므로 배치 모드에서는 재현되지 않는다 —
        /// <c>.claude/rules/tests.md</c> §1 의 «가드가 걸린 <c>Update</c>» 그대로다.
        /// 전제(레거시 입력이 꺼져 있다)는 <c>InputBackendTests</c> 가 컴파일 시점에 고정한다.
        /// </para>
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
    }
}
