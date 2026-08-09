using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SushiDefense.Audio;
using SushiDefense.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.Audio
{
    /// <summary>
    /// 브라우저 잠금이 <b>무엇으로 풀리고, 얼마나 오래 열려 있나</b>.
    ///
    /// <para>
    /// <c>AudioDirectorTests</c> 는 <c>NotifyUserInput</c> 을 직접 불러 확인한다 — 그 우회로
    /// 때문에 «무엇이 그것을 부르는가» 가 통째로 검증에서 빠진다
    /// (<c>.claude/rules/tests.md</c> §1). 실플레이에서 <b>제목 화면의 빈 곳을 아무리 눌러도
    /// 음악이 시작되지 않았고</b>, 화면을 옮기면 열려 있던 잠금이 도로 닫혔다.
    /// </para>
    /// </summary>
    public sealed class AudioUnlockOnInputTests : InputTestFixture
    {
        private readonly List<Object> _garbage = new();

        private AudioDirector _director;
        private AudioBankSO _bank;

        public override void Setup()
        {
            base.Setup();

            AudioUnlockGate.ResetOnLoad();
            _bank = NewBank();
            _director = NewDirector();
        }

        public override void TearDown()
        {
            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();
            AudioUnlockGate.ResetOnLoad();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Update_MouseClickAnywhere_UnlocksAndStartsBgm()
        {
            var mouse = InputSystem.AddDevice<Mouse>();

            yield return null;
            Assert.IsFalse(_director.IsUnlocked, "아직 아무 입력도 없었다");

            Press(mouse.leftButton);
            yield return null;

            Assert.IsTrue(_director.IsUnlocked, "빈 곳을 눌러도 잠금이 풀려야 한다");
            Assert.AreEqual(1, _director.BgmStartCount);
        }

        [UnityTest]
        public IEnumerator Update_KeyPress_AlsoUnlocks()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            yield return null;
            Press(keyboard.spaceKey);
            yield return null;

            Assert.IsTrue(_director.IsUnlocked);
        }

        [UnityTest]
        public IEnumerator Update_NoInput_StaysLocked()
        {
            // 반례가 없으면 «항상 연다» 는 구현도 위 두 테스트를 통과한다.
            InputSystem.AddDevice<Mouse>();

            yield return null;
            yield return null;

            Assert.IsFalse(_director.IsUnlocked);
            Assert.AreEqual(0, _director.BgmStartCount);
        }

        /// <summary>
        /// 다음 화면의 진행자는 <b>기다리지 않는다.</b> 브라우저는 한 번 연 것을 다시 잠그지
        /// 않으므로, 씬을 옮겼다고 음악이 멈춰 있으면 안 된다.
        /// </summary>
        [UnityTest]
        public IEnumerator Start_OnAnAlreadyUnlockedPage_StartsBgmWithoutAnyInput()
        {
            _director.NotifyUserInput();

            var next = NewDirector();
            yield return null;

            Assert.IsTrue(next.IsUnlocked);
            Assert.AreEqual(1, next.BgmStartCount, "화면을 옮기니 음악이 다시 멈췄다");
        }

        private AudioDirector NewDirector()
        {
            var go = NewObject("AudioDirector");
            var sfx = go.AddComponent<AudioSource>();
            var bgm = go.AddComponent<AudioSource>();
            var director = go.AddComponent<AudioDirector>();

            SetField(director, "_bank", _bank);
            SetField(director, "_sfxSource", sfx);
            SetField(director, "_bgmSource", bgm);
            return director;
        }

        private AudioBankSO NewBank()
        {
            var bank = ScriptableObject.CreateInstance<AudioBankSO>();
            _garbage.Add(bank);

            var clip = AudioClip.Create("bgm", 2205, 1, 22050, false);
            _garbage.Add(clip);

            var cue = typeof(AudioBankSO)
                .GetField("_bgm", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(bank);
            cue.GetType().GetField("_clip", BindingFlags.Instance | BindingFlags.NonPublic)
               .SetValue(cue, clip);

            return bank;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }

        private static void SetField(Object target, string name, Object value)
        {
            target.GetType()
                  .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                  .SetValue(target, value);
        }
    }
}
