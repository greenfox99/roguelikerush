using Sandbox;
using Sandbox.Network;
using System;
using System.Collections.Generic;

/// <summary>
/// Зона с нарастающим уроном по времени пребывания.
///
/// Дополнительно:
/// - Пока игрок внутри зоны, проигрывается "пугающий" звук (пульсом с интервалом)
/// - На HUD игрока появляется пульсирующее предупреждение: "Вы разозлили продавца!"
///
/// Multiplayer-ready:
/// - Урон наносится только на хосте (GameNetworkSystem.IsHost)
/// - Визуал/звук работает локально для каждого клиента, т.к. триггеры обрабатываются на клиенте тоже.
/// </summary>
public sealed class EscalatingDamageZone : Component, Component.ITriggerListener
{
	// =========================
	// DAMAGE
	// =========================
	[Property, Group( "Damage" )] public float TickInterval { get; set; } = 1.0f;
	[Property, Group( "Damage" )] public int StartDamagePerSecond { get; set; } = 5;
	[Property, Group( "Damage" )] public int DamageIncreasePerTick { get; set; } = 2;
	[Property, Group( "Damage" )] public int MaxDamagePerSecond { get; set; } = 500;

	/// <summary>Если true — урон тикает по RealTime (не зависит от Scene.TimeScale)</summary>
	[Property, Group( "Damage" )] public bool UseRealTime { get; set; } = true;

	// =========================
	// FEEDBACK
	// =========================
	[Property, Group( "Feedback" )] public bool EnableWarningHud { get; set; } = true;

	[Property, Group( "Feedback" )] public string WarningText { get; set; } = "Ты чё ахуел что ли?!";

	/// <summary>
	/// Звук "пугающего пульса" пока игрок в зоне.
	/// Рекомендуется короткий sting/heartbeat (0.1-0.5s).
	/// </summary>
	[Property, Group( "Feedback" )] public SoundEvent WarningPulseSound { get; set; }

	/// <summary>Интервал пульса звука (сек).</summary>
	[Property, Group( "Feedback" )] public float WarningPulseInterval { get; set; } = 0.8f;

	/// <summary>Играть пульс в позиции игрока (true) или зоны (false).</summary>
	[Property, Group( "Feedback" )] public bool PulseSoundAtPlayer { get; set; } = true;

	[Property, Group( "Debug" )] public bool DebugMode { get; set; } = false;

	private sealed class Tracked
	{
		// damage
		public float NextDamageTickTime;
		public int DamagePerSecond;

		// local feedback
		public float NextPulseTime;
		public ZoneWarningHUD Hud;
	}

	private readonly Dictionary<GameObject, Tracked> _tracked = new();
	private readonly List<GameObject> _removeBuffer = new( 16 );

	// ===== Trigger Listener =====
	public void OnTriggerEnter( Collider other )
	{
		if ( other == null || !other.IsValid ) return;

		var playerGo = TryGetPlayerRoot( other.GameObject );
		if ( playerGo == null ) return;

		if ( _tracked.ContainsKey( playerGo ) ) return;

		Sandbox.Services.Achievements.Unlock( "damage_zone" );

		float now = Now();
		float dmgInterval = MathF.Max( 0.05f, TickInterval );
		float pulseInterval = MathF.Max( 0.05f, WarningPulseInterval );

		var data = new Tracked
		{
			NextDamageTickTime = now + dmgInterval,
			DamagePerSecond = Math.Max( 0, StartDamagePerSecond ),
			NextPulseTime = now, // сразу можно пульснуть
			Hud = null
		};

		// HUD предупреждение (локально)
		if ( EnableWarningHud )
		{
			data.Hud = playerGo.Components.Get<ZoneWarningHUD>();
			if ( data.Hud == null )
				data.Hud = playerGo.Components.Create<ZoneWarningHUD>();

			data.Hud.Show( WarningText );
		}

		_tracked[playerGo] = data;

		if ( DebugMode )
			Log.Info( $"🩸 DamageZone ENTER: {playerGo.Name} dps={StartDamagePerSecond}" );
	}

	public void OnTriggerExit( Collider other )
	{
		if ( other == null || !other.IsValid ) return;

		var playerGo = TryGetPlayerRoot( other.GameObject );
		if ( playerGo == null ) return;

		if ( _tracked.TryGetValue( playerGo, out var data ) )
		{
			// скрываем HUD
			data?.Hud?.Hide();
		}

		if ( _tracked.Remove( playerGo ) && DebugMode )
			Log.Info( $"🩸 DamageZone EXIT: {playerGo.Name} (reset)" );
	}

	protected override void OnUpdate()
	{
		if ( _tracked.Count == 0 ) return;

		float now = Now();
		float dmgInterval = MathF.Max( 0.05f, TickInterval );
		float pulseInterval = MathF.Max( 0.05f, WarningPulseInterval );

		_removeBuffer.Clear();

		foreach ( var kv in _tracked )
		{
			var playerGo = kv.Key;
			var data = kv.Value;

			if ( playerGo == null || !playerGo.IsValid )
			{
				_removeBuffer.Add( playerGo );
				continue;
			}

			// 1) LOCAL FEEDBACK: звук-пульс пока внутри
			if ( WarningPulseSound != null && now >= data.NextPulseTime )
			{
				Vector3 pos = PulseSoundAtPlayer ? playerGo.Transform.Position : Transform.Position;
				Sound.Play( WarningPulseSound, pos );
				data.NextPulseTime = now + pulseInterval;
			}

			// 2) DAMAGE: только хост
			if ( !GameNetworkSystem.IsHost )
				continue;

			var hp = playerGo.Components.Get<PlayerHealth>();
			if ( hp == null || !hp.IsValid )
			{
				_removeBuffer.Add( playerGo );
				continue;
			}

			// если игрок уже мёртв — убираем
			if ( hp.CurrentHealth <= 0f )
			{
				// спрячем HUD если вдруг
				data?.Hud?.Hide();
				_removeBuffer.Add( playerGo );
				continue;
			}

			int safety = 0;
			while ( now >= data.NextDamageTickTime && safety++ < 8 )
			{
				ApplyDamageTick( playerGo, hp, data );
				data.NextDamageTickTime += dmgInterval;

				if ( hp.CurrentHealth <= 0f )
				{
					data?.Hud?.Hide();
					_removeBuffer.Add( playerGo );
					break;
				}
			}
		}

		for ( int i = 0; i < _removeBuffer.Count; i++ )
			_tracked.Remove( _removeBuffer[i] );
	}

	private void ApplyDamageTick( GameObject playerGo, PlayerHealth hp, Tracked data )
	{
		int dps = ClampInt( data.DamagePerSecond, 0, MaxDamagePerSecond );
		if ( dps <= 0 ) return;

		// Наносим урон.
		// В твоём проекте TakeDamage() публичный (ты используешь его из других мест).
		hp.TakeDamage( dps );

		if ( DebugMode )
			Log.Info( $"🩸 DamageZone TICK: {playerGo.Name} -{dps} (next={Math.Min( MaxDamagePerSecond, dps + DamageIncreasePerTick )})" );

		int next = data.DamagePerSecond + DamageIncreasePerTick;
		data.DamagePerSecond = Math.Min( MaxDamagePerSecond, next );
	}

	private float Now()
	{
		return UseRealTime ? RealTime.Now : Time.Now;
	}

	private static GameObject TryGetPlayerRoot( GameObject go )
	{
		if ( go == null || !go.IsValid ) return null;

		var pc = go.Components.Get<PlayerController>();
		if ( pc != null && pc.IsValid )
			return go;

		var cur = go.Parent;
		int hops = 0;

		while ( cur != null && cur.IsValid && hops < 8 )
		{
			pc = cur.Components.Get<PlayerController>();
			if ( pc != null && pc.IsValid )
				return cur;

			cur = cur.Parent;
			hops++;
		}

		return null;
	}

	private static int ClampInt( int v, int min, int max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}
}
