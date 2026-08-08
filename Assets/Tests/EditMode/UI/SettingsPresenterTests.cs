using System;
using NUnit.Framework;
using SushiDefense.Settings;
using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 설정의 로직. <b>Unity API 를 모른다</b> — <c>PlayerPrefs</c>·<c>Screen</c>·
    /// <c>AudioListener</c> 가 전부 인터페이스 뒤에 있어, 실제 저장소나 화면 없이
    /// «즉시 적용되는가 · 언제 저장되는가» 를 확인할 수 있다 (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// <b>세 층을 테스트에서도 분리한다</b> — 값(<see cref="GameSettings"/>) · 저장 ·
    /// 적용. 합쳐서 보면 <i>"소리가 안 줄었다"</i> 의 원인이 셋 중 어느 쪽인지 알 수 없다.
    /// </para>
    /// </summary>
    public sealed class SettingsPresenterTests
    {
        private FakeSettingsView _view;
        private GameSettings _settings;
        private FakeSettingsStore _store;
        private FakeSettingsApplier _applier;
        private SettingsPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeSettingsView();
            _settings = new GameSettings();
            _store = new FakeSettingsStore();
            _applier = new FakeSettingsApplier();
            _presenter = new SettingsPresenter(_view, _settings, _store, _applier);
        }

        // ── 복원 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 볼륨은 <b>설정 화면을 열기 전부터</b> 맞아 있어야 한다. 복원을 여는 시점으로
        /// 미루면 한 번도 설정을 안 연 사람에게는 저장된 값이 영영 안 먹는다.
        /// </summary>
        [Test]
        public void Constructor_RestoresStoredValuesAndApplies()
        {
            var view = new FakeSettingsView();
            var settings = new GameSettings();
            var store = new FakeSettingsStore { StoredVolume = 0.25f, StoredFullscreen = true };
            var applier = new FakeSettingsApplier();

            _ = new SettingsPresenter(view, settings, store, applier);

            Assert.AreEqual(1, store.LoadCount);
            Assert.AreEqual(0.25f, settings.MasterVolume, 0.0001f);
            Assert.AreEqual(1, applier.ApplyCount, "복원만 하고 안 먹이면 소리가 그대로다");
            Assert.AreEqual(0.25f, applier.LastVolume, 0.0001f);
        }

        /// <summary>세울 때 저장까지 하면, 켜기만 해도 기본값이 디스크에 굳는다.</summary>
        [Test]
        public void Constructor_DoesNotSave()
        {
            Assert.AreEqual(0, _store.SaveCount);
        }

        [Test]
        public void Constructor_DoesNotOpen()
        {
            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(0, _view.ShowCount);
        }

        // ── 열기 ───────────────────────────────────────────────────────────

        [Test]
        public void Open_ShowsCurrentValues()
        {
            _presenter.SetMasterVolume(0.4f);
            _presenter.SetFullscreen(true);
            _view.Reset();

            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
            Assert.AreEqual(0.4f, _view.LastVolume, 0.0001f);
            Assert.IsTrue(_view.LastFullscreen);
        }

        /// <summary>
        /// 저장소가 돌려준 값이 화면까지 와야 한다. 중간에서 기본값으로 덮으면 설정을
        /// 저장해도 다음에 열었을 때 그대로 보이지 않는다.
        /// </summary>
        [Test]
        public void Open_AfterStoreLoad_ShowsStoredValues()
        {
            var view = new FakeSettingsView();
            var store = new FakeSettingsStore { StoredVolume = 0.15f };
            var presenter = new SettingsPresenter(view, new GameSettings(), store,
                                                  new FakeSettingsApplier());

            presenter.Open();

            Assert.AreEqual(0.15f, view.LastVolume, 0.0001f);
        }

        /// <summary>같은 상태를 두 번 그리면 조작 중인 슬라이더가 제자리로 튄다.</summary>
        [Test]
        public void Open_Twice_ShowsOnce()
        {
            _presenter.Open();

            _presenter.Open();

            Assert.AreEqual(1, _view.ShowCount);
        }

        // ── 적용은 즉시 ─────────────────────────────────────────────────────

        /// <summary>
        /// 횟수만 세면 <b>아무 값이나</b> 먹이는 구현이 통과한다. 넘어간 값을 함께 박는다.
        /// </summary>
        [Test]
        public void SetMasterVolume_AppliesImmediately()
        {
            _presenter.Open();
            var before = _applier.ApplyCount;

            _presenter.SetMasterVolume(0.3f);

            Assert.AreEqual(before + 1, _applier.ApplyCount);
            Assert.AreEqual(0.3f, _applier.LastVolume, 0.0001f);
        }

        [Test]
        public void SetFullscreen_AppliesImmediately()
        {
            _presenter.Open();
            var before = _applier.ApplyCount;

            _presenter.SetFullscreen(true);

            Assert.AreEqual(before + 1, _applier.ApplyCount);
            Assert.IsTrue(_applier.LastFullscreen);
        }

        /// <summary>
        /// <see cref="Close_SavesOnce"/> 와 <b>짝</b>이다. 하나만 있으면 "매번 저장" 과
        /// "한 번도 저장 안 함" 중 하나가 통과한다.
        /// </summary>
        [Test]
        public void SetMasterVolume_DoesNotSaveYet()
        {
            _presenter.Open();

            _presenter.SetMasterVolume(0.3f);
            _presenter.SetMasterVolume(0.6f);

            Assert.AreEqual(0, _store.SaveCount, "슬라이더를 끄는 동안 프레임마다 디스크를 쓴다");
        }

        [Test]
        public void SetFullscreen_DoesNotSaveYet()
        {
            _presenter.Open();

            _presenter.SetFullscreen(true);

            Assert.AreEqual(0, _store.SaveCount);
        }

        /// <summary>
        /// 모델이 자른 값이 <b>화면까지</b> 와야 한다. step-02 의 클램프 테스트는 모델이
        /// 자르는지를 보고, 여기는 잘린 값이 도달하는지를 본다 — 프레젠터가 원본을 그대로
        /// 넘기면 슬라이더와 실제 볼륨이 어긋난다.
        /// </summary>
        [Test]
        public void SetMasterVolume_AboveOne_ShowsClampedValue()
        {
            _presenter.Open();

            _presenter.SetMasterVolume(1.5f);

            Assert.AreEqual(1f, _view.LastVolume, 0.0001f);
        }

        [Test]
        public void SetMasterVolume_BelowZero_ShowsClampedValue()
        {
            _presenter.Open();

            _presenter.SetMasterVolume(-0.5f);

            Assert.AreEqual(0f, _view.LastVolume, 0.0001f);
        }

        // ── 전체화면은 비동기다 ──────────────────────────────────────────────

        /// <summary>
        /// <b>리포트에 적힌 «두 번 눌러야 바뀐다» 를 재현한다.</b> 브라우저의 전체화면
        /// 전환은 비동기라, 누른 그 프레임에는 엔진 값이 아직 옛 값이다. 그 값을 되읽어
        /// 그리면 방금 켠 토글이 곧바로 꺼진 모습으로 되돌아간다.
        /// </summary>
        [Test]
        public void SetFullscreen_ApplierLagsOneFrame_ShowsTheChosenValue()
        {
            _presenter.Open();
            _applier.RefuseFullscreen = true;

            _presenter.SetFullscreen(true);

            Assert.IsTrue(_view.LastFullscreen, "고른 값이 화면에서 되돌아가면 두 번 눌러야 한다");
        }

        /// <summary>
        /// 뒤늦게 엔진이 따라잡아도 <b>토글이 다시 흔들리지 않는다.</b> 위 테스트와 짝이며,
        /// 하나만 두면 «엔진 값을 아예 안 본다» 는 구현도 통과한다.
        /// </summary>
        [Test]
        public void SyncFullscreen_ApplierCatchesUp_DoesNotFlipBack()
        {
            _presenter.Open();
            _applier.RefuseFullscreen = true;
            _presenter.SetFullscreen(true);
            _view.Reset();

            _applier.Fullscreen = true;

            Assert.IsFalse(_presenter.SyncFullscreen(), "우리가 시킨 변화는 다시 그릴 것이 없다");
            Assert.AreEqual(0, _view.ShowCount);
        }

        /// <summary>
        /// <c>Esc</c> 로 브라우저가 스스로 창 모드로 돌아간 경우. 그 경로는 콜백이 없어
        /// <b>물어보는 수밖에 없고</b>, 안 물어보면 토글이 켜진 채로 거짓을 말한다.
        /// </summary>
        [Test]
        public void SyncFullscreen_ExitedOutsideTheApp_TurnsToggleOff()
        {
            _presenter.SetFullscreen(true);
            _presenter.Open();
            _view.Reset();

            _applier.Fullscreen = false;

            Assert.IsTrue(_presenter.SyncFullscreen());
            Assert.AreEqual(1, _view.ShowCount);
            Assert.IsFalse(_view.LastFullscreen);
        }

        /// <summary>
        /// 되돌린 것을 <b>다시 먹이지 않는다.</b> 여기서 적용하면 브라우저가 방금 나온
        /// 전체화면으로 도로 밀어 넣는다.
        /// </summary>
        [Test]
        public void SyncFullscreen_ExitedOutsideTheApp_DoesNotReapply()
        {
            _presenter.SetFullscreen(true);
            var before = _applier.ApplyCount;

            _applier.Fullscreen = false;
            _presenter.SyncFullscreen();

            Assert.AreEqual(before, _applier.ApplyCount);
        }

        [Test]
        public void SyncFullscreen_NothingChanged_ReportsNoChange()
        {
            _presenter.Open();
            _view.Reset();

            Assert.IsFalse(_presenter.SyncFullscreen());
            Assert.AreEqual(0, _view.ShowCount, "매 프레임 다시 그리면 슬라이더가 제자리로 튄다");
        }

        /// <summary>거부된 것은 표시일 뿐, <b>선호는 남는다</b> — 저장되는 값은 고른 쪽이다.</summary>
        [Test]
        public void SetFullscreen_WhenApplierRefuses_StillSavesThePreference()
        {
            _presenter.Open();
            _applier.RefuseFullscreen = true;
            _presenter.SetFullscreen(true);

            _presenter.Close();

            Assert.IsTrue(_store.SavedFullscreen);
        }

        // ── 저장은 닫을 때 ───────────────────────────────────────────────────

        [Test]
        public void Close_SavesOnce()
        {
            _presenter.Open();
            _presenter.SetMasterVolume(0.3f);

            _presenter.Close();

            Assert.AreEqual(1, _store.SaveCount);
            Assert.AreEqual(0.3f, _store.SavedVolume, 0.0001f);
        }

        [Test]
        public void Close_Twice_SavesOnce()
        {
            _presenter.Open();

            _presenter.Close();
            _presenter.Close();

            Assert.AreEqual(1, _store.SaveCount);
        }

        /// <summary>
        /// <b>닫힘을 알리지 않으면 빈 화면에 갇힌다.</b> 메인 메뉴는 설정을 열 때 내려가고,
        /// 다시 여는 유일한 신호가 이것이다.
        /// </summary>
        [Test]
        public void Close_RaisesClosedOnce()
        {
            var closed = 0;
            _presenter.Closed += () => closed++;
            _presenter.Open();

            _presenter.Close();

            Assert.AreEqual(1, closed);
        }

        [Test]
        public void Close_Twice_RaisesClosedOnce()
        {
            var closed = 0;
            _presenter.Closed += () => closed++;
            _presenter.Open();

            _presenter.Close();
            _presenter.Close();

            Assert.AreEqual(1, closed);
        }

        [Test]
        public void Close_WhenNeverOpened_RaisesNothing()
        {
            var closed = 0;
            _presenter.Closed += () => closed++;

            _presenter.Close();

            Assert.AreEqual(0, closed);
        }

        /// <summary>구독자가 없어도 닫힘 자체는 정상이다 — 저장은 이미 끝나 있어야 한다.</summary>
        [Test]
        public void Close_NoSubscriber_StillSaves()
        {
            _presenter.Open();

            Assert.DoesNotThrow(() => _presenter.Close());
            Assert.AreEqual(1, _store.SaveCount);
        }

        [Test]
        public void Close_WhenNeverOpened_DoesNotSave()
        {
            _presenter.Close();

            Assert.AreEqual(0, _store.SaveCount);
            Assert.AreEqual(0, _view.HideCount);
        }

        [Test]
        public void Close_AfterOpen_HidesView()
        {
            _presenter.Open();

            _presenter.Close();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _view.HideCount);
        }

        /// <summary>닫았다 다시 열면 마지막 값이 그대로 보여야 한다.</summary>
        [Test]
        public void Open_AfterCloseWithChanges_ShowsTheChangedValue()
        {
            _presenter.Open();
            _presenter.SetMasterVolume(0.2f);
            _presenter.Close();
            _view.Reset();

            _presenter.Open();

            Assert.AreEqual(0.2f, _view.LastVolume, 0.0001f);
        }

        /// <summary>
        /// 다시 열 때 저장소를 <b>또 읽지 않는다.</b> 읽으면 적용해 두고 아직 저장하지
        /// 않은 값이 되돌아가, 화면과 실제 소리가 어긋난다.
        /// </summary>
        [Test]
        public void Open_Reopened_DoesNotReloadFromStore()
        {
            _presenter.Open();
            _presenter.Close();

            _presenter.Open();

            Assert.AreEqual(1, _store.LoadCount);
        }

        // ── 생성자 ─────────────────────────────────────────────────────────

        [Test]
        public void Constructor_NullView_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SettingsPresenter(null, _settings, _store, _applier));
        }

        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SettingsPresenter(_view, null, _store, _applier));
        }

        [Test]
        public void Constructor_NullStore_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SettingsPresenter(_view, _settings, null, _applier));
        }

        [Test]
        public void Constructor_NullApplier_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SettingsPresenter(_view, _settings, _store, null));
        }

        // ── 손으로 쓴 스텁 ───────────────────────────────────────────────────

        private sealed class FakeSettingsView : ISettingsView
        {
            public int ShowCount { get; private set; }

            public int HideCount { get; private set; }

            public float LastVolume { get; private set; } = float.NaN;

            public bool LastFullscreen { get; private set; }

            public void ShowSettings(float masterVolume, bool fullscreen)
            {
                ShowCount++;
                LastVolume = masterVolume;
                LastFullscreen = fullscreen;
            }

            public void Hide()
            {
                HideCount++;
            }

            public void Reset()
            {
                ShowCount = 0;
                HideCount = 0;
            }
        }

        private sealed class FakeSettingsStore : ISettingsStore
        {
            /// <summary>남아 있는 값. <c>NaN</c> 이면 <i>"저장된 적 없음"</i> 이다.</summary>
            public float StoredVolume { get; set; } = float.NaN;

            public bool StoredFullscreen { get; set; }

            public int LoadCount { get; private set; }

            public int SaveCount { get; private set; }

            public float SavedVolume { get; private set; } = float.NaN;

            public bool SavedFullscreen { get; private set; }

            public void Load(GameSettings into)
            {
                LoadCount++;

                if (!float.IsNaN(StoredVolume))
                {
                    into.SetMasterVolume(StoredVolume);
                }

                into.SetFullscreen(StoredFullscreen);
            }

            public void Save(GameSettings from)
            {
                SaveCount++;
                SavedVolume = from.MasterVolume;
                SavedFullscreen = from.Fullscreen;
            }
        }

        /// <summary>
        /// 엔진 대신 값을 받아 둔다. <see cref="RefuseFullscreen"/> 은 브라우저가 제스처
        /// 밖의 전체화면 요청을 무시하는 상황을 재현한다.
        /// </summary>
        private sealed class FakeSettingsApplier : ISettingsApplier
        {
            public int ApplyCount { get; private set; }

            public float LastVolume { get; private set; } = float.NaN;

            public bool LastFullscreen { get; private set; }

            public bool RefuseFullscreen { get; set; }

            public bool Fullscreen { get; set; }

            public bool IsFullscreen => Fullscreen;

            public void Apply(GameSettings settings)
            {
                ApplyCount++;
                LastVolume = settings.MasterVolume;
                LastFullscreen = settings.Fullscreen;

                if (!RefuseFullscreen)
                {
                    Fullscreen = settings.Fullscreen;
                }
            }
        }
    }
}
