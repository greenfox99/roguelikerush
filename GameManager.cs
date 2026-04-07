using Sandbox;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Логика музыки:
/// - Waiting -> случайный трек из PreBattlePlaylist
/// - Preparing -> музыка НЕ меняется вообще
/// - Fighting без босса -> случайный трек из BattlePlaylist
/// - Fighting с боссом -> случайный трек из BossPlaylist
/// 
/// При смерти игрока вызывай StopMusicForDeath(),
/// чтобы GameManager полностью перестал управлять музыкой.
/// </summary>
public sealed class GameManager : Component
{
	public enum GameState
	{
		Waiting,
		Preparing,
		Fighting
	}

	public GameState CurrentState { get; private set; } = GameState.Waiting;

	[Property] public float PrepareTime { get; set; } = 10f;

	[Property, Group( "Music" )] public List<SoundEvent> PreBattlePlaylist { get; set; } = new();
	[Property, Group( "Music" )] public List<SoundEvent> BattlePlaylist { get; set; } = new();
	[Property, Group( "Music" )] public List<SoundEvent> BossPlaylist { get; set; } = new();

	[Property, Group( "Boss" )] public string BossObjectName { get; set; } = "npc_boss";

	[Property] public GameObject SpawnerObject { get; set; }

	private float _prepareTimer;
	private bool _hasStartedFight = false;
	private bool _musicStoppedForDeath = false;

	private DualNPCSpawner _spawner;

	private SoundHandle _musicHandle;
	private List<SoundEvent> _currentPlaylist;
	private int _lastTrackIndex = -1;

	protected override void OnStart()
	{
		CurrentState = GameState.Waiting;

		if ( SpawnerObject != null )
		{
			_spawner = SpawnerObject.Components.Get<DualNPCSpawner>();
			if ( _spawner != null )
			{
				_spawner.Enabled = false;
				Log.Info( "🚫 DualNPCSpawner отключен" );
			}
			else
			{
				Log.Error( "❌ На SpawnerObject нет DualNPCSpawner!" );
			}
		}
		else
		{
			Log.Error( "❌ SpawnerObject не назначен в GameManager!" );
		}

		UpdateWaitingMusic();

		Log.Info( "🎮 Игра загружена. Нажми F для начала боя!" );
	}

	protected override void OnUpdate()
	{
		if ( _musicStoppedForDeath )
			return;

		if ( CurrentState == GameState.Waiting )
		{
			// В ожидании pre-battle может играть/переключаться
			UpdateWaitingMusic();

			if ( Input.Pressed( "Flashlight" ) )
			{
				StartPreparation();
			}
		}
		else if ( CurrentState == GameState.Preparing )
		{
			// Во время подготовки музыку НЕ трогаем вообще
			_prepareTimer -= Time.Delta;

			if ( _prepareTimer <= 0f && !_hasStartedFight )
			{
				StartFight();
			}
		}
		else if ( CurrentState == GameState.Fighting )
		{
			UpdateFightMusic();
		}
	}

	protected override void OnDisabled()
	{
		StopCurrentMusic();
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		StopCurrentMusic();
		base.OnDestroy();
	}

	private void StartPreparation()
	{
		CurrentState = GameState.Preparing;
		_prepareTimer = PrepareTime;
		_hasStartedFight = false;

		if ( _spawner != null )
			_spawner.Enabled = false;

		var lootSpawner = Scene.GetAllComponents<LootSpawner>().FirstOrDefault();
		if ( lootSpawner != null )
			lootSpawner.StopSpawning();

		// ВАЖНО:
		// тут НЕ запускаем новую музыку и НЕ переключаем трек
		Log.Info( $"⚔️ Подготовка к бою! {PrepareTime} секунд..." );
	}

	private void StartFight()
	{
		CurrentState = GameState.Fighting;
		_hasStartedFight = true;

		Log.Info( "🔥 БОЙ НАЧАЛСЯ!" );

		if ( _spawner != null )
		{
			_spawner.Enabled = true;
			Log.Info( "🌀 DualNPCSpawner включен!" );
		}

		var lootSpawner = Scene.GetAllComponents<LootSpawner>().FirstOrDefault();
		if ( lootSpawner != null )
			lootSpawner.StartSpawning();

		UpdateFightMusic( forceRestart: true );
	}

	private void UpdateWaitingMusic()
	{
		if ( PreBattlePlaylist == null || PreBattlePlaylist.Count == 0 )
			return;

		if ( _currentPlaylist != PreBattlePlaylist )
		{
			_currentPlaylist = PreBattlePlaylist;
			_lastTrackIndex = -1;
			PlayRandomTrackFromCurrentPlaylist();
			return;
		}

		if ( _musicHandle == null || !_musicHandle.IsValid || _musicHandle.Finished || !_musicHandle.IsPlaying )
		{
			PlayRandomTrackFromCurrentPlaylist();
		}
	}

	private void UpdateFightMusic( bool forceRestart = false )
	{
		var desiredPlaylist = HasActiveBoss() ? BossPlaylist : BattlePlaylist;

		if ( desiredPlaylist == null || desiredPlaylist.Count == 0 )
		{
			StopCurrentMusic();
			_currentPlaylist = null;
			_lastTrackIndex = -1;
			return;
		}

		bool playlistChanged = !ReferenceEquals( _currentPlaylist, desiredPlaylist );

		if ( forceRestart || playlistChanged )
		{
			_currentPlaylist = desiredPlaylist;
			_lastTrackIndex = -1;
			PlayRandomTrackFromCurrentPlaylist();
			return;
		}

		if ( _musicHandle == null || !_musicHandle.IsValid || _musicHandle.Finished || !_musicHandle.IsPlaying )
		{
			PlayRandomTrackFromCurrentPlaylist();
		}
	}

	private void PlayRandomTrackFromCurrentPlaylist()
	{
		if ( _currentPlaylist == null || _currentPlaylist.Count == 0 )
			return;

		var validTracks = _currentPlaylist.Where( x => x != null ).ToList();
		if ( validTracks.Count == 0 )
			return;

		StopCurrentMusic();

		int nextIndex;

		if ( validTracks.Count == 1 )
		{
			nextIndex = 0;
		}
		else
		{
			do
			{
				nextIndex = Game.Random.Int( 0, validTracks.Count - 1 );
			}
			while ( nextIndex == _lastTrackIndex );
		}

		_lastTrackIndex = nextIndex;

		var nextTrack = validTracks[nextIndex];
		_musicHandle = Sound.Play( nextTrack );

		Log.Info( $"🎵 Играет трек: {nextTrack.ResourceName}" );
	}

	private void StopCurrentMusic()
	{
		if ( _musicHandle != null )
		{
			_musicHandle.Stop();
			_musicHandle = null;
		}
	}

	public void StopMusicForDeath()
	{
		_musicStoppedForDeath = true;
		StopCurrentMusic();
		_currentPlaylist = null;
		_lastTrackIndex = -1;
	}

	public void ResetMusicAfterDeath()
	{
		_musicStoppedForDeath = false;

		if ( CurrentState == GameState.Waiting )
			UpdateWaitingMusic();
		else if ( CurrentState == GameState.Fighting )
			UpdateFightMusic( forceRestart: true );
	}

	private bool HasActiveBoss()
	{
		var boss = Scene.GetAllComponents<BossNPC>()
			.FirstOrDefault( x =>
				x != null &&
				x.Enabled &&
				x.GameObject != null &&
				x.GameObject.IsValid &&
				x.GameObject.Name == BossObjectName );

		return boss != null;
	}

	public float GetPrepareTimeLeft() => _prepareTimer;
	public float GetPrepareProgress() => 1f - (_prepareTimer / PrepareTime);
}
