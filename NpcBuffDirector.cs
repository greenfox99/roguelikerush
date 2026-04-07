using Sandbox;
using System;
using System.Linq;

/// <summary>
/// Каждую минуту во время Fighting выбирает случайный баф NPC и стакует бесконечно.
/// Применяет всем живым (NPC + MummyNPC).
/// Даёт HUD уведомление.
/// </summary>
public sealed class NpcBuffDirector : Component
{
	public static NpcBuffDirector Instance { get; private set; }

	[Property] public float IntervalSeconds { get; set; } = 60f;
	[Property] public float PerStackPercent { get; set; } = 5f;
	[Property] public bool OnlyDuringFight { get; set; } = true;
	[Property] public bool DebugMode { get; set; } = true;

	// ВАЖНО: struct хранится тут, но менять его надо через re-assign (см. OnUpdate)
	public NpcBuffState CurrentState { get; private set; } = NpcBuffState.Default;

	public event Action<string> OnBuffAnnounced;

	private GameManager _gm;
	private GameHUD _hud;
	private TimeSince _timer;

	private readonly Random _rng = new Random();

	protected override void OnStart()
	{
		Instance = this;

		_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		_hud = Scene.GetAllComponents<GameHUD>().FirstOrDefault();

		if ( _hud != null )
			OnBuffAnnounced += _hud.AddNpcBuffToast;

		_timer = 0f;

		if ( DebugMode )
			Log.Info( $"🧪 NpcBuffDirector started. Interval={IntervalSeconds}s, Stack={PerStackPercent}%." );
	}

	protected override void OnDestroy()
	{
		if ( _hud != null )
			OnBuffAnnounced -= _hud.AddNpcBuffToast;

		if ( Instance == this )
			Instance = null;
	}

	protected override void OnUpdate()
	{
		if ( IntervalSeconds <= 0.05f ) return;

		if ( OnlyDuringFight && !IsFighting() )
			return;

		if ( _timer < IntervalSeconds )
			return;

		_timer = 0f;

		var kind = PickRandomKind();

		// ✅ КЛЮЧЕВОЙ ФИКС: struct из property — это копия. Меняем локально и присваиваем обратно.
		var state = CurrentState;
		state.ApplyStack( kind, PerStackPercent );
		CurrentState = state;

		ApplyToAllAlive();

		string msg = BuildText( kind );
		OnBuffAnnounced?.Invoke( msg );

		if ( DebugMode ) Log.Info( $"📣 {msg}" );
	}

	public void ApplyTo( GameObject npcGo )
	{
		if ( npcGo == null || !npcGo.IsValid ) return;

		var r = npcGo.Components.Get<NpcBuffReceiver>( true );
		if ( r == null ) r = npcGo.Components.Create<NpcBuffReceiver>();

		r.ApplyGlobalState( CurrentState );
	}

	private void ApplyToAllAlive()
	{
		foreach ( var g in Scene.GetAllComponents<NPC>() )
		{
			if ( g == null || !g.IsValid ) continue;
			if ( g.Health <= 0f ) continue;
			ApplyTo( g.GameObject );
		}

		foreach ( var m in Scene.GetAllComponents<MummyNPC>() )
		{
			if ( m == null || !m.IsValid ) continue;
			if ( m.Health <= 0f ) continue;
			ApplyTo( m.GameObject );
		}
	}

	private bool IsFighting()
	{
		if ( _gm == null || !_gm.IsValid )
			_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();

		if ( _gm == null ) return true;
		return _gm.CurrentState == GameManager.GameState.Fighting;
	}

	private NpcBuffKind PickRandomKind()
	{
		int v = _rng.Next( 0, 3 ); // 0..2
		return v switch
		{
			0 => NpcBuffKind.MoveSpeed,
			1 => NpcBuffKind.Damage,
			_ => NpcBuffKind.Resistance
		};
	}

	private string BuildText( NpcBuffKind kind )
	{
		int stacks = CurrentState.GetStacks( kind );

		return kind switch
		{
			NpcBuffKind.MoveSpeed => $"👻🧟 NPC баф: +{PerStackPercent:F0}% скорость передвижения (x{stacks})",
			NpcBuffKind.Damage => $"👻🧟 NPC баф: +{PerStackPercent:F0}% урон (x{stacks})",
			NpcBuffKind.Resistance => $"👻🧟 NPC баф: +{PerStackPercent:F0}% сопротивляемость урону (x{stacks})",
			_ => $"👻🧟 NPC баф (x{stacks})"
		};
	}
}
