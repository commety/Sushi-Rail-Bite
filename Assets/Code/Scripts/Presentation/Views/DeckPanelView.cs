using System.Collections.Generic;
using SushiDefense.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SushiDefense.UI
{
    /// <summary>
    /// 지금 초밥 덱을 카드로 늘어놓는 화면.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 무엇을 보여줄지도 열려 있는지도
    /// <see cref="DeckPanelPresenter"/> 가 답한다 — <c>RewardSelectionView</c> 와 같은 형태다.
    /// </para>
    /// <para>
    /// <b>컴포넌트가 패널 바깥에 산다.</b> 덱 버튼은 패널이 닫혀 있을 때도 눌려야 하므로,
    /// 이 컴포넌트가 붙은 오브젝트는 늘 켜져 있고 <see cref="_panelRoot"/> 만 켜고 끈다
    /// (<c>DigestingBadgeView</c> 와 같은 형태다).
    /// </para>
    /// </summary>
    public sealed class DeckPanelView : MonoBehaviour, IDeckPanelView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string PanelRootName = "Panel";

        private const string EmptyNoticeName = "EmptyNotice";

        /// <summary>덱이 비었을 때의 문구.</summary>
        private const string EmptyNotice = "덱이 비어 있음";

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CardView[] _cards;
        [SerializeField] private TMP_Text _emptyNotice;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Button _closeButton;

        private DeckPanelPresenter _presenter;

        /// <summary>화면이 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>지금 그린 카드 수. 검증용이다.</summary>
        public int ShownCardCount { get; private set; }

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(DeckPanelPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.</summary>
        public void Initialize(GameObject panelRoot, CardView[] cards, TMP_Text emptyNotice)
        {
            _panelRoot = panelRoot;
            _cards = cards;
            _emptyNotice = emptyNotice;
        }

        /// <inheritdoc />
        public void ShowDeck(IReadOnlyList<SushiData> cards)
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

                    // 자리보다 덱이 길면 있는 만큼만 그린다. 예외를 내면 씬을 조금씩
                    // 조립하는 동안 나머지를 아무것도 확인할 수 없다.
                    if (cards != null && i < cards.Count)
                    {
                        card.Show(cards[i]);
                        ShownCardCount++;
                    }
                    else
                    {
                        card.Clear();
                    }
                }
            }

            ShowNotice(cards == null || cards.Count == 0);
            SetPanelActive(true);
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;
            ShownCardCount = 0;

            if (_cards != null)
            {
                for (var i = 0; i < _cards.Length; i++)
                {
                    _cards[i]?.Clear();
                }
            }

            ShowNotice(false);
            SetPanelActive(false);
        }

        private void Awake()
        {
            if (_panelRoot == null)
            {
                var child = transform.Find(PanelRootName);
                _panelRoot = child != null ? child.gameObject : null;
            }

            _emptyNotice = HudLabel.Resolve(_panelRoot != null ? _panelRoot.transform : transform,
                                            _emptyNotice, EmptyNoticeName);

            if (_cards == null || _cards.Length == 0)
            {
                _cards = GetComponentsInChildren<CardView>(true);
            }

            HudLabel.Write(_emptyNotice, EmptyNotice);

            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(OnToggleClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(OnToggleClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnToggleClicked()
        {
            _presenter?.Toggle();
        }

        private void OnCloseClicked()
        {
            _presenter?.Close();
        }

        /// <summary>
        /// 문구는 <see cref="Awake"/> 에서 한 번만 쓴다 — 여기서는 켜고 끄기만 한다.
        /// 매번 쓰면 열 때마다 문자열이 하나씩 생긴다 (§4.3).
        /// </summary>
        private void ShowNotice(bool visible)
        {
            if (_emptyNotice != null)
            {
                _emptyNotice.gameObject.SetActive(visible);
            }
        }

        private void SetPanelActive(bool active)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(active);
            }
        }
    }
}
