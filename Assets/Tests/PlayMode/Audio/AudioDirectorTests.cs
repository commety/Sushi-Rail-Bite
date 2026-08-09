using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SushiDefense.Audio;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Scoring;
using SushiDefense.Stages;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.Audio
{
    /// <summary>
    /// 소리가 <b>났는지 / 눌렸는지</b>만 본다. 겹침 판정 자체는 EditMode 의
    /// <c>SoundBudgetTests</c> 가, 자동재생 게이트는 <c>AudioUnlockGateTests</c> 가 덮는다.
    ///
    /// <para>
    /// 헤드리스 배치모드에는 오디오 장치가 없어 실제 소리를 들을 수 없다. 그래서 뷰가
    /// <c>RevenueText</c> 를 노출하는 것과 같은 이유로 재생 횟수를 노출한다.
    /// </para>
    /// </summary>
    public sealed class AudioDirectorTests
    {
        private const int MaxConcurrent = 6;
        private const float Cooldown = 0.06f;

        private readonly List<Object> _created = new();

        private AudioBankSO _bank;
        private AudioDirector _director;
        private ClaimCoordinator _coordinator;
        private StageController _stage;
        private CustomerLogic _customer;
        private SushiItem _sushi;

        [SetUp]
        public void SetUp()
        {
            // 잠금은 페이지 단위(=`static`)라 앞 테스트가 연 것이 그대로 넘어온다.
            AudioUnlockGate.ResetOnLoad();

            _bank = NewBank();

            var go = NewObject("AudioDirector");
            var sfx = go.AddComponent<AudioSource>();
            var bgm = go.AddComponent<AudioSource>();
            _director = go.AddComponent<AudioDirector>();
            SetField(_director, "_bank", _bank);
            SetField(_director, "_sfxSource", sfx);
            SetField(_director, "_bgmSource", bgm);

            var stageConfig = StageConfigTestFactory.Create(10f, 1f, 100f, NewSushiData());
            _created.Add(stageConfig);

            var belt = new SushiBelt(stageConfig, SushiDeck.FromSpawnTable(stageConfig).Cards,
                                     new SequenceNumberIssuer(),
                                     new SushiPool<SushiItem>(new SushiItemFactory()));
            var revenue = new RevenueLedger();
            _coordinator = new ClaimCoordinator(belt, stageConfig, revenue, new RecruitWallet(0));
            _stage = new StageController(_coordinator, revenue, stageConfig);

            var customerData = ScriptableObject.CreateInstance<CustomerData>();
            _created.Add(customerData);
            _customer = new CustomerLogic(new CustomerRuntimeState(customerData, 0), 0f);
            _sushi = new SushiItem(NewSushiData(), 0);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
            {
                Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        [Test]
        public void SushiEaten_BeforeUserInput_DoesNotPlay()
        {
            // 배포 타깃은 첫 제스처 전까지 오디오가 잠겨 있다. 그 상태에서 낸 소리는
            // 밀리지 않고 사라진다.
            Bind();

            RaiseEaten();

            Assert.AreEqual(0, _director.PlayedCount);
            Assert.AreEqual(1, _director.SuppressedCount);
        }

        [Test]
        public void SushiEaten_AfterUserInput_Plays()
        {
            Bind();
            _director.NotifyUserInput();

            RaiseEaten();

            Assert.AreEqual(1, _director.PlayedCount);
        }

        [Test]
        public void SushiEaten_FourInSameFrame_SuppressesAllButFirst()
        {
            // 간격 0.06초 안에서는 같은 큐가 한 번만 난다. 넷이 동시에 먹히면
            // 하나만 울리고 셋은 버려져야 한다 — 그러지 않으면 볼륨이 네 배가 된다.
            Bind();
            _director.NotifyUserInput();

            for (var i = 0; i < 4; i++)
            {
                RaiseEaten();
            }

            Assert.AreEqual(1, _director.PlayedCount);
            Assert.AreEqual(3, _director.SuppressedCount);
        }

        [Test]
        public void StageCleared_Once_Plays()
        {
            Bind();
            _director.NotifyUserInput();

            RaiseOutcome(StageOutcome.Cleared);

            Assert.AreEqual(1, _director.PlayedCount);
        }

        [Test]
        public void NotifyUserInput_First_StartsBgm()
        {
            Bind();

            _director.NotifyUserInput();

            Assert.IsTrue(_director.IsBgmPlaying);
        }

        [Test]
        public void Bind_BeforeUserInput_DoesNotStartBgm()
        {
            Bind();

            Assert.IsFalse(_director.IsBgmPlaying);
        }

        [Test]
        public void BgmCue_DefaultTrack_IsTheStageBgm()
        {
            Assert.AreSame(_bank.Bgm, _director.BgmCue);
        }

        [Test]
        public void BgmCue_MainTrack_IsTheMainBgm()
        {
            SetTrack(BgmTrack.Main);

            Assert.AreSame(_bank.MainBgm, _director.BgmCue);
        }

        [Test]
        public void NotifyUserInput_MainTrack_PlaysTheMainClip()
        {
            // "둘이 다르다" 만 보면 큐만 갈리고 실제 재생은 스테이지 곡인 구현도 통과한다.
            SetTrack(BgmTrack.Main);

            _director.NotifyUserInput();

            Assert.AreSame(_bank.MainBgm.Clip, BgmSource().clip);
            Assert.AreNotSame(_bank.Bgm.Clip, BgmSource().clip);
        }

        [Test]
        public void BgmCue_NoBank_IsNullInsteadOfThrowing()
        {
            SetField(_director, "_bank", null);

            Assert.IsNull(_director.BgmCue);
        }

        [Test]
        public void NotifyUserInput_Twice_DoesNotRestartBgm()
        {
            Bind();
            _director.NotifyUserInput();
            var started = _director.BgmStartCount;

            _director.NotifyUserInput();

            Assert.AreEqual(started, _director.BgmStartCount);
        }

        [Test]
        public void Bind_Twice_DoesNotDoublePlay()
        {
            // Build() 는 스테이지가 넘어갈 때마다 다시 불린다. 구독이 겹치면 한 번의
            // 사건이 두 번 재생된다.
            //
            // 간격이 0 인 큐로 본다. 먹힘 큐(0.06초)로 보면 두 번째 재생을 간격이
            // 대신 막아 버려, 구독이 겹쳐도 테스트가 통과한다 — 실제로 주입해 보고 알았다.
            Bind();
            Bind();
            _director.NotifyUserInput();

            RaiseOutcome(StageOutcome.Cleared);

            Assert.AreEqual(1, _director.PlayedCount);
        }

        [Test]
        public void Unbind_ThenEaten_DoesNotPlay()
        {
            Bind();
            _director.NotifyUserInput();

            _director.Unbind();
            RaiseEaten();

            Assert.AreEqual(0, _director.PlayedCount);
        }

        [Test]
        public void Bind_WithoutBank_DoesNotThrow()
        {
            // 소리는 로직의 전제 조건이 아니다.
            var go = NewObject("AudioDirector_NoBank");
            var bare = go.AddComponent<AudioDirector>();

            Assert.DoesNotThrow(() =>
            {
                bare.Bind(_coordinator, null, _stage, null, null);
                bare.NotifyUserInput();
                RaiseEaten();
            });
        }

        private void Bind()
        {
            _director.Bind(_coordinator, null, _stage, null, null);
        }

        private void RaiseEaten()
        {
            Raise(_coordinator, "SushiEaten", _customer, _sushi);
        }

        private void RaiseOutcome(StageOutcome outcome)
        {
            Raise(_stage, "OutcomeDecided", outcome);
        }

        /// <summary>
        /// 조율자의 이벤트를 직접 발화한다. 실제 먹힘을 만들려면 벨트·자리·시간을 전부
        /// 꾸며야 하는데, 여기서 볼 것은 <b>이벤트가 소리로 이어지는가</b>뿐이다.
        /// </summary>
        private static void Raise(object target, string eventName, params object[] args)
        {
            var field = target.GetType().GetField(
                eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"이벤트 백킹 필드 '{eventName}' 을 찾지 못했습니다.");

            ((System.Delegate)field.GetValue(target))?.DynamicInvoke(args);
        }

        private void SetTrack(BgmTrack track)
        {
            typeof(AudioDirector)
                .GetField("_bgmTrack", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_director, track);
        }

        private AudioSource BgmSource()
        {
            return (AudioSource)typeof(AudioDirector)
                .GetField("_bgmSource", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_director);
        }

        private static void SetField(Object target, string name, Object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"직렬화 필드 '{name}' 을 찾지 못했습니다.");
            field.SetValue(target, value);
        }

        private AudioBankSO NewBank()
        {
            var bank = ScriptableObject.CreateInstance<AudioBankSO>();
            _created.Add(bank);

            SetCue(bank, "_sushiEaten", 0.5f, Cooldown);
            SetCue(bank, "_customerPlaced", 0.7f, 0f);
            SetCue(bank, "_rewardPicked", 0.7f, 0f);
            SetCue(bank, "_stageAdvanced", 0.7f, 0f);
            SetCue(bank, "_stageCleared", 0.8f, 0f);
            SetCue(bank, "_stageFailed", 0.8f, 0f);
            SetCue(bank, "_bgm", 0.35f, 0f);
            SetCue(bank, "_mainBgm", 0.3f, 0f);

            var max = typeof(AudioBankSO).GetField("_maxConcurrentSfx",
                BindingFlags.Instance | BindingFlags.NonPublic);
            max.SetValue(bank, MaxConcurrent);
            return bank;
        }

        private void SetCue(AudioBankSO bank, string field, float volume, float cooldown)
        {
            var cue = typeof(AudioBankSO)
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(bank);
            Assert.IsNotNull(cue, $"큐 '{field}' 가 비어 있습니다.");

            var clip = AudioClip.Create(field, 2205, 1, 22050, false);
            _created.Add(clip);

            Set(cue, "_clip", clip);
            Set(cue, "_volume", volume);
            Set(cue, "_cooldownSeconds", cooldown);
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType()
                  .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                  .SetValue(target, value);
        }

        private SushiData NewSushiData()
        {
            var data = ScriptableObject.CreateInstance<SushiData>();
            _created.Add(data);
            return data;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }
    }
}
