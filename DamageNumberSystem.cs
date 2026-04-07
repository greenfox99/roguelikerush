using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class DamageNumberSystem : Component
{
	public enum DamageNumberKind
	{
		Normal = 0,
		Crit = 1,
		Execute = 2
	}

	[Property] public bool EnabledInGame { get; set; } = true;

	[Property] public float Lifetime { get; set; } = 0.9f;
	[Property] public float RiseSpeed { get; set; } = 80f;
	[Property] public float SideJitter { get; set; } = 14f;
	[Property] public float UpOffset { get; set; } = 65f;

	// 🔥 УВЕЛИЧЕННЫЙ РАЗМЕР
	[Property] public float StartTextSize { get; set; } = 30f;
	[Property] public float EndTextSize { get; set; } = 20f;

	[Property] public Color NormalColor { get; set; } = new Color( 1f, 1f, 1f, 1f );
	[Property] public Color CritColor { get; set; } = new Color( 1f, 0.9f, 0.25f, 1f );
	[Property] public Color ExecuteColor { get; set; } = new Color( 1f, 0.45f, 0.35f, 1f );

	[Property] public bool DebugLog { get; set; } = false;
	[Property] public int MaxActivePopups { get; set; } = 96;

	private static DamageNumberSystem _instance;

	private readonly List<Popup> _active = new( 128 );
	private readonly Stack<Popup> _pool = new( 128 );

	private Random _rng;

	private sealed class Popup
	{
		public Vector3 WorldPos;
		public Vector3 Velocity;
		public float Age;
		public float Life;
		public int Amount;
		public DamageNumberKind Kind;
	}

	protected override void OnStart()
	{
		_rng = new Random( (int)(Time.Now * 1000f) ^ GameObject.Id.GetHashCode() );
		_instance = this;
	}

	protected override void OnDestroy()
	{
		if ( ReferenceEquals( _instance, this ) )
			_instance = null;
	}

	protected override void OnUpdate()
	{
		if ( !EnabledInGame ) return;
		if ( Scene?.Camera == null ) return;
		if ( _active.Count == 0 ) return;

		float dt = Time.Delta;
		if ( dt <= 0f ) dt = 0.016f;

		for ( int i = _active.Count - 1; i >= 0; i-- )
		{
			var p = _active[i];
			p.Age += dt;

			if ( p.Age >= p.Life )
			{
				RecycleAt( i );
				continue;
			}

			p.WorldPos += p.Velocity * dt;
			p.Velocity *= MathF.Pow( 0.0001f, dt );

			Vector2 screen = Scene.Camera.PointToScreenPixels( p.WorldPos, out bool behind );
			if ( behind ) continue;

			const float margin = 40f;
			if ( screen.x < -margin || screen.y < -margin || screen.x > Screen.Width + margin || screen.y > Screen.Height + margin )
				continue;

			float t = p.Age / MathF.Max( 0.001f, p.Life );
			t = Clamp01( t );

			float ease = 1f - MathF.Pow( 1f - t, 3f );
			float size = Lerp( StartTextSize, EndTextSize, ease );
			float alpha = Clamp01( (1f - ease) * 1.2f );

			Color c = GetColor( p.Kind ).WithAlpha( alpha );

			// 🔥 МИНУС ПЕРЕД ЧИСЛОМ
			string text = "-" + p.Amount.ToString();

			DebugOverlay.ScreenText( screen, text, size, color: c );
		}
	}

	private Color GetColor( DamageNumberKind kind )
	{
		return kind switch
		{
			DamageNumberKind.Crit => CritColor,
			DamageNumberKind.Execute => ExecuteColor,
			_ => NormalColor
		};
	}

	private void RecycleAt( int index )
	{
		var p = _active[index];
		_active.RemoveAt( index );

		p.Age = 0f;
		p.Amount = 0;
		p.Kind = DamageNumberKind.Normal;
		p.WorldPos = default;
		p.Velocity = default;
		p.Life = 0f;

		_pool.Push( p );
	}

	private Popup GetPopup()
	{
		if ( _pool.Count > 0 )
			return _pool.Pop();

		return new Popup();
	}

	public void SpawnDamage( Vector3 targetWorldPos, int amount, DamageNumberKind kind = DamageNumberKind.Normal )
	{
		if ( !EnabledInGame ) return;
		if ( Scene?.Camera == null ) return;
		if ( amount <= 0 ) return;

		while ( _active.Count >= MaxActivePopups )
			RecycleAt( 0 );

		var p = GetPopup();

		Vector3 jitter = new Vector3(
			FloatRange( -SideJitter, SideJitter ),
			FloatRange( -SideJitter, SideJitter ),
			0f
		);

		p.WorldPos = targetWorldPos + Vector3.Up * UpOffset + jitter;
		p.Velocity = Vector3.Up * RiseSpeed;

		p.Age = 0f;
		p.Life = MathF.Max( 0.15f, Lifetime );
		p.Amount = amount;
		p.Kind = kind;

		_active.Add( p );

		if ( DebugLog )
			Log.Info( $"DMG POPUP: {amount} ({kind})" );
	}

	private float FloatRange( float min, float max )
	{
		return min + (float)_rng.NextDouble() * (max - min);
	}

	private static float Clamp01( float v )
	{
		if ( v < 0f ) return 0f;
		if ( v > 1f ) return 1f;
		return v;
	}

	private static float Lerp( float a, float b, float t )
	{
		t = Clamp01( t );
		return a + (b - a) * t;
	}

	public static DamageNumberSystem Get( Scene scene )
	{
		if ( scene == null ) return null;

		if ( _instance != null && _instance.IsValid && _instance.Scene == scene )
			return _instance;

		return scene.GetAllComponents<DamageNumberSystem>().FirstOrDefault();
	}
}
