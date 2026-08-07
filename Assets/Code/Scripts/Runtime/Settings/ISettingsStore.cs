namespace SushiDefense.Settings
{
    /// <summary>
    /// 설정을 어딘가에 남기고 다시 읽어 온다. <b>어디인지는 <c>Presentation</c> 이 정한다.</b>
    ///
    /// <para>
    /// 이 인터페이스가 있는 이유는 <c>Runtime</c> 이 저장 장치를 몰라야 하기 때문이다 —
    /// 알게 되면 "설정을 닫으면 저장된다" 를 확인하는 데 실제 저장소가 필요해진다.
    /// <c>ISceneRouter</c> 와 같은 방향 전환이다.
    /// </para>
    /// <para>
    /// <b><see cref="GameSettings"/> 를 돌려주지 않고 채운다.</b> 설정 객체는 화면·오디오가
    /// 이미 구독하고 있는 하나뿐인 인스턴스라, 저장소가 새로 만들어 돌려주면 구독이
    /// 죽은 객체에 남는다.
    /// </para>
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>
        /// 남아 있는 값을 <paramref name="into"/> 에 넣는다. 남은 것이 없으면 손대지 않는다 —
        /// 처음 켠 사람에게 기본값이 그대로 남아야 한다.
        /// </summary>
        void Load(GameSettings into);

        /// <summary>지금 값을 남긴다.</summary>
        void Save(GameSettings from);
    }
}
