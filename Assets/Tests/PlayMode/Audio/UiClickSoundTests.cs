using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SushiDefense.Audio;
using SushiDefense.Data;
using SushiDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.Audio
{
    /// <summary>
    /// 클릭음이 <b>버튼에만</b> 붙는지 본다.
    ///
    /// <para>
    /// 카드 선택(보상·백과사전)과 손님 배치는 자기 큐가 따로 있다 (아키텍트 결정). 카드는
    /// 자기 오브젝트에 <c>Button</c> 을 달고 있어서, 가르는 규칙이 없으면 하위를 훑을 때
    /// 딸려 온다 — 그러면 메뉴 조작과 플레이가 청각적으로 구분되지 않는다.
    /// </para>
    /// <para>
    /// 헤드리스 배치모드에는 오디오 장치가 없어 실제 소리를 들을 수 없다. <c>AudioDirector</c> 가
    /// 노출하는 재생 횟수로 본다 (<c>AudioDirectorTests</c> 와 같은 방식이다).
    /// </para>
    /// </summary>
    public sealed class UiClickSoundTests
    {
        private readonly List<Object> _created = new();

        private AudioDirector _director;
        private UiClickSound _clicks;
        private Button _plainButton;
        private Button _cardButton;

        [SetUp]
        public void SetUp()
        {
            var audioGo = NewObject("AudioDirector");
            var sfx = audioGo.AddComponent<AudioSource>();
            var bgm = audioGo.AddComponent<AudioSource>();
            _director = audioGo.AddComponent<AudioDirector>();
            SetField(_director, "_bank", NewBank());
            SetField(_director, "_sfxSource", sfx);
            SetField(_director, "_bgmSource", bgm);

            var root = NewObject("Canvas");
            root.AddComponent<RectTransform>();

            _plainButton = NewButton(root, "CloseButton");
            _cardButton = NewCard(root, "Card0").GetComponent<Button>();

            _clicks = root.AddComponent<UiClickSound>();
            _clicks.Initialize(_director);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _created)
            {
                Object.DestroyImmediate(item);
            }

            _created.Clear();
        }

        [Test]
        public void ClickButton_PlaysTheClick()
        {
            _plainButton.onClick.Invoke();

            Assert.AreEqual(1, _director.PlayedCount);
        }

        /// <summary>
        /// <b>이 테스트가 이 컴포넌트의 존재 이유다.</b> 카드는 자기 오브젝트에 버튼을
        /// 달고 있어, 가르는 규칙이 빠지면 여기서 소리가 난다.
        /// </summary>
        [Test]
        public void ClickCard_DoesNotPlayTheClick()
        {
            _cardButton.onClick.Invoke();

            Assert.AreEqual(0, _director.PlayedCount,
                            "카드 선택에 UI 클릭음이 붙었다 — 메뉴 조작과 구분되지 않는다");
        }

        /// <summary>
        /// 횟수만 보면 <b>아무것도 안 거는</b> 구현이 «카드는 안 난다» 를 통과한다.
        /// 버튼 하나만 걸렸다는 구체값을 함께 박는다.
        /// </summary>
        [Test]
        public void Hooked_CountsButtonsExceptCards()
        {
            Assert.AreEqual(1, _clicks.HookedCount);
        }

        /// <summary>
        /// 브라우저 잠금은 <b>첫 제스처</b>로 풀린다. 메인 화면이 앞에 생기면서 그 제스처가
        /// 배치가 아니라 버튼 클릭이 됐다 — 여기서 풀지 않으면 메뉴에서 아무 소리도 안 난다.
        /// </summary>
        [Test]
        public void ClickButton_UnlocksAudioOnTheFirstGesture()
        {
            // 잠긴 상태에서는 눌러도 소리가 나지 않아야 정상이므로, 첫 클릭이 곧 해제이자
            // 첫 재생이다. 두 번째 클릭이 눌리지 않고 나는지까지 본다.
            _plainButton.onClick.Invoke();
            _plainButton.onClick.Invoke();

            Assert.AreEqual(2, _director.PlayedCount);
            Assert.AreEqual(0, _director.SuppressedCount);
        }

        [Test]
        public void ClickButton_AfterDestroy_DoesNothing()
        {
            Object.DestroyImmediate(_clicks);

            _plainButton.onClick.Invoke();

            Assert.AreEqual(0, _director.PlayedCount);
        }

        // ── 조립 ───────────────────────────────────────────────────────────

        private Button NewButton(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }

        private GameObject NewCard(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            foreach (var child in new[] { "Icon", "NameLabel", "DetailLabel" })
            {
                var c = new GameObject(child, typeof(RectTransform));
                c.transform.SetParent(go.transform, false);
                if (child == "Icon")
                {
                    c.AddComponent<Image>();
                }
                else
                {
                    c.AddComponent<TextMeshProUGUI>();
                }
            }

            go.AddComponent<Image>();
            go.AddComponent<Button>();
            go.AddComponent<CardView>();
            return go;
        }

        private AudioBankSO NewBank()
        {
            var bank = ScriptableObject.CreateInstance<AudioBankSO>();
            _created.Add(bank);

            var clip = AudioClip.Create("uiClick", 2205, 1, 22050, false);
            _created.Add(clip);

            var cue = typeof(AudioBankSO)
                .GetField("_uiClick", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(bank);
            Set(cue, "_clip", clip);
            Set(cue, "_volume", 0.4f);

            // 간격을 0 으로 둔다. 실제 값(0.05초)이면 연타 테스트가 «간격이 막았다» 와
            // «구독이 하나뿐이다» 를 구분하지 못한다 (tests.md §3 «다른 장치가 주입을 가린다»).
            Set(cue, "_cooldownSeconds", 0f);

            typeof(AudioBankSO)
                .GetField("_maxConcurrentSfx", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(bank, 6);
            return bank;
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType()
                  .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                  .SetValue(target, value);
        }

        private static void SetField(Object target, string name, Object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"직렬화 필드 '{name}' 을 찾지 못했습니다.");
            field.SetValue(target, value);
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }
    }
}
