using Sandbox;

public sealed class BossMusicManager : Component
{
	public static BossMusicManager Instance { get; private set; }

	[Property] public SoundEvent DefaultMusic { get; set; }
	[Property] public float DefaultMusicFadeIn { get; set; } = 1.0f;
	[Property] public float DefaultMusicFadeOut { get; set; } = 0.5f;

	private SoundHandle _currentMusic;
	private SoundEvent _currentEvent;

	protected override void OnStart()
	{
		Instance = this;

		if ( DefaultMusic != null )
			PlayMusic( DefaultMusic, DefaultMusicFadeIn );
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
			Instance = null;

		if ( _currentMusic.IsValid )
			_currentMusic.Stop( DefaultMusicFadeOut );
	}

	public void PlayMusic( SoundEvent music, float fadeIn = 1.0f, float fadeOutOld = 0.5f )
	{
		if ( music == null )
			return;

		// если уже играет этот же трек - не перезапускаем
		if ( _currentMusic.IsValid && _currentEvent == music && _currentMusic.IsPlaying )
			return;

		if ( _currentMusic.IsValid )
			_currentMusic.Stop( fadeOutOld );

		_currentMusic = Sound.Play( music, fadeIn );
		_currentEvent = music;
	}

	public void StopMusic( float fadeOut = 0.5f )
	{
		if ( _currentMusic.IsValid )
			_currentMusic.Stop( fadeOut );

		_currentMusic = default;
		_currentEvent = null;
	}

	public void RestoreDefaultMusic()
	{
		if ( DefaultMusic != null )
			PlayMusic( DefaultMusic, DefaultMusicFadeIn, DefaultMusicFadeOut );
	}
}
