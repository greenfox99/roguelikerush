using Sandbox;
using System;
using System.Linq;

public sealed class StatueCapturePoint : Component
{
	[Property] public float CaptureRadius { get; set; } = 400f;
	[Property] public float CaptureTime { get; set; } = 5f;

	[Property, Group( "Visual" )] public bool DrawCaptureRadius { get; set; } = true;
	[Property, Group( "Visual" )] public int SphereRings { get; set; } = 20;
	[Property, Group( "Visual" )] public float LineThickness { get; set; } = 2f;
	[Property, Group( "Visual" )] public float RadiusHeightOffset { get; set; } = 20f;

	[Property, Group( "Visual" )] public Color IdleRadiusColor { get; set; } = new Color( 0.20f, 0.55f, 1.00f, 0.85f );
	[Property, Group( "Visual" )] public Color CapturingRadiusColor { get; set; } = new Color( 1.00f, 0.85f, 0.20f, 0.95f );
	[Property, Group( "Visual" )] public Color CapturedRadiusColor { get; set; } = new Color( 0.25f, 1.00f, 0.35f, 0.95f );

	[Property, Group( "Captured Visual" )] public Color CapturedTint { get; set; } = new Color( 1.00f, 0.92f, 0.55f, 1.00f );
	[Property, Group( "Captured Visual" )] public bool ChangeOutlineOnCapture { get; set; } = true;
	[Property, Group( "Captured Visual" )] public Color CapturedOutlineColor { get; set; } = new Color( 0.25f, 1.00f, 0.35f, 1.00f );

	[Property, Group( "Explosion" )] public bool ExplodeOnCapture { get; set; } = true;
	[Property, Group( "Explosion" )] public float ExplodeDelay { get; set; } = 0.15f;
	[Property, Group( "Explosion" )] public GameObject CaptureExplosionFxPrefab { get; set; }

	[Property, Group( "Debris" )] public GameObject DebrisPrefab { get; set; }
	[Property, Group( "Debris" )] public int DebrisCountMin { get; set; } = 8;
	[Property, Group( "Debris" )] public int DebrisCountMax { get; set; } = 12;
	[Property, Group( "Debris" )] public float DebrisSpawnRadius { get; set; } = 16f;
	[Property, Group( "Debris" )] public float DebrisLifetime { get; set; } = 4.0f;

	[Property, Group( "Coins" )] public GameObject CoinPickupPrefab { get; set; }
	[Property, Group( "Coins" )] public int CoinBurstMin { get; set; } = 6;
	[Property, Group( "Coins" )] public int CoinBurstMax { get; set; } = 12;
	[Property, Group( "Coins" )] public float CoinSpawnRadius { get; set; } = 10f;
	[Property, Group( "Coins" )] public float CoinHorizontalForceMin { get; set; } = 180f;
	[Property, Group( "Coins" )] public float CoinHorizontalForceMax { get; set; } = 300f;
	[Property, Group( "Coins" )] public float CoinUpForceMin { get; set; } = 180f;
	[Property, Group( "Coins" )] public float CoinUpForceMax { get; set; } = 260f;
	[Property, Group( "Coins" )] public float CoinLifetime { get; set; } = 10f;

	public bool IsCaptured { get; private set; } = false;
	public float CaptureProgress { get; private set; } = 0f; // 0..1
	public bool IsPlayerInside { get; private set; } = false;

	private Player _player;
	private ModelRenderer _modelRenderer;
	private SkinnedModelRenderer _skinnedRenderer;
	private HighlightOutline _outline;
	private Collider _collider;

	private bool _explodePending;
	private float _explodeTimer;
	private bool _exploded;

	protected override void OnStart()
	{
		_player = Scene.GetAllComponents<Player>().FirstOrDefault();

		_modelRenderer = Components.Get<ModelRenderer>();
		_skinnedRenderer = Components.Get<SkinnedModelRenderer>();
		_outline = Components.Get<HighlightOutline>();
		_collider = Components.Get<Collider>();
	}

	protected override void OnUpdate()
	{
		if ( _player == null || !_player.IsValid )
			_player = Scene.GetAllComponents<Player>().FirstOrDefault();

		if ( !IsCaptured && _player != null && _player.IsValid )
		{
			float dist = Vector3.DistanceBetween( Transform.Position, _player.Transform.Position );
			IsPlayerInside = dist <= CaptureRadius;

			if ( IsPlayerInside )
			{
				CaptureProgress += Time.Delta / CaptureTime;
				CaptureProgress = MathF.Min( CaptureProgress, 1f );

				if ( CaptureProgress >= 1f )
					Capture();
			}
			else
			{
				CaptureProgress = 0f;
			}
		}
		else if ( IsCaptured )
		{
			IsPlayerInside = false;

			if ( _explodePending && !_exploded )
			{
				_explodeTimer -= Time.Delta;
				if ( _explodeTimer <= 0f )
					DoExplosion();
			}
		}

		if ( DrawCaptureRadius )
			DrawRadius();
	}

	private void Capture()
	{
		if ( IsCaptured ) return;

		IsCaptured = true;
		IsPlayerInside = false;
		CaptureProgress = 1f;

		int reward = StatueRewardSystem.RegisterCaptureAndGetReward();

		ApplyCapturedVisuals();
		SpawnRewardCoins( reward );

		if ( ExplodeOnCapture )
		{
			_explodePending = true;
			_explodeTimer = MathF.Max( 0f, ExplodeDelay );
		}

		Log.Info( $"🗿 Статуя захвачена: {GameObject?.Name} | Награда: {reward} золота" );
	}

	private void ApplyCapturedVisuals()
	{
		if ( _modelRenderer != null && _modelRenderer.IsValid )
			_modelRenderer.Tint = CapturedTint;

		if ( _skinnedRenderer != null && _skinnedRenderer.IsValid )
			_skinnedRenderer.Tint = CapturedTint;

		if ( ChangeOutlineOnCapture && _outline != null && _outline.IsValid )
		{
			_outline.Enabled = true;
			_outline.Color = CapturedOutlineColor;
		}
	}

	private void DoExplosion()
	{
		if ( _exploded ) return;
		_exploded = true;
		_explodePending = false;

		Vector3 center = Transform.Position + Vector3.Up * 20f;

		if ( CaptureExplosionFxPrefab != null && CaptureExplosionFxPrefab.IsValid )
		{
			var fx = CaptureExplosionFxPrefab.Clone();
			fx.Transform.Position = center;
			fx.Transform.Rotation = Rotation.Identity;
			fx.Enabled = true;
		}

		SpawnDebrisBurst( center );

		if ( _modelRenderer != null && _modelRenderer.IsValid )
			_modelRenderer.Enabled = false;

		if ( _skinnedRenderer != null && _skinnedRenderer.IsValid )
			_skinnedRenderer.Enabled = false;

		if ( _outline != null && _outline.IsValid )
			_outline.Enabled = false;

		if ( _collider != null && _collider.IsValid )
			_collider.Enabled = false;
	}

	private void SpawnRewardCoins( int totalReward )
	{
		if ( CoinPickupPrefab == null || !CoinPickupPrefab.IsValid )
			return;

		int minCount = Math.Min( CoinBurstMin, CoinBurstMax );
		int maxCount = Math.Max( CoinBurstMin, CoinBurstMax );

		int coinCount = Random.Shared.Int( minCount, maxCount + 1 );
		coinCount = Math.Max( 1, Math.Min( coinCount, totalReward ) );

		int remaining = totalReward;
		Vector3 center = Transform.Position + Vector3.Up * 18f;

		for ( int i = 0; i < coinCount; i++ )
		{
			int coinsLeft = coinCount - i;
			int value = remaining / coinsLeft;
			if ( value <= 0 ) value = 1;

			remaining -= value;

			Vector3 spawnOffset = new Vector3(
				Random.Shared.Float( -CoinSpawnRadius, CoinSpawnRadius ),
				Random.Shared.Float( -CoinSpawnRadius, CoinSpawnRadius ),
				Random.Shared.Float( 0f, 10f )
			);

			Vector3 spawnPos = center + spawnOffset;

			var obj = CoinPickupPrefab.Clone();
			obj.Transform.Position = spawnPos;
			obj.Transform.Rotation = Rotation.Random;
			obj.Enabled = true;

			var coin = obj.Components.Get<StatueRewardCoin>();
			if ( coin == null || !coin.IsValid )
				continue;

			Vector3 dir = new Vector3(
				Random.Shared.Float( -1f, 1f ),
				Random.Shared.Float( -1f, 1f ),
				Random.Shared.Float( 0.25f, 1f )
			).Normal;

			float hForce = Random.Shared.Float( CoinHorizontalForceMin, CoinHorizontalForceMax );
			float upForce = Random.Shared.Float( CoinUpForceMin, CoinUpForceMax );

			coin.CoinValue = value;
			coin.Velocity = new Vector3( dir.x, dir.y, 0f ).Normal * hForce + Vector3.Up * upForce;
			coin.AngularVelocity = new Vector3(
				Random.Shared.Float( -1080f, 1080f ),
				Random.Shared.Float( -1080f, 1080f ),
				Random.Shared.Float( -1080f, 1080f )
			);
			coin.Lifetime = CoinLifetime;
		}
	}

	private void SpawnDebrisBurst( Vector3 center )
	{
		if ( DebrisPrefab == null || !DebrisPrefab.IsValid )
			return;

		int minCount = Math.Min( DebrisCountMin, DebrisCountMax );
		int maxCount = Math.Max( DebrisCountMin, DebrisCountMax );
		int count = Random.Shared.Int( minCount, maxCount + 1 );

		for ( int i = 0; i < count; i++ )
		{
			Vector3 spawnOffset = new Vector3(
				Random.Shared.Float( -DebrisSpawnRadius, DebrisSpawnRadius ),
				Random.Shared.Float( -DebrisSpawnRadius, DebrisSpawnRadius ),
				Random.Shared.Float( 0f, DebrisSpawnRadius * 0.5f )
			);

			Vector3 spawnPos = center + spawnOffset;

			var obj = DebrisPrefab.Clone();
			obj.Transform.Position = spawnPos;
			obj.Transform.Rotation = Rotation.Random;
			obj.Enabled = true;

			var debris = obj.Components.Get<StatueDebris>();
			if ( debris == null || !debris.IsValid )
				continue;

			Vector3 outward = spawnOffset;
			outward.z = MathF.Max( outward.z, 8f );

			if ( outward.Length < 0.001f )
			{
				outward = new Vector3(
					Random.Shared.Float( -1f, 1f ),
					Random.Shared.Float( -1f, 1f ),
					Random.Shared.Float( 0.2f, 1f )
				);
			}

			outward = outward.Normal;

			float horizontalForce = Random.Shared.Float( 220f, 320f );
			float upForce = Random.Shared.Float( 180f, 280f );

			Vector3 velocity =
				new Vector3( outward.x, outward.y, 0f ).Normal * horizontalForce
				+ Vector3.Up * upForce;

			debris.Velocity = velocity;

			debris.AngularVelocity = new Vector3(
				Random.Shared.Float( -1080f, 1080f ),
				Random.Shared.Float( -1080f, 1080f ),
				Random.Shared.Float( -1080f, 1080f )
			);

			debris.Lifetime = DebrisLifetime;
		}
	}

	private void DrawRadius()
	{
		if ( _exploded )
			return;

		var gizmo = Gizmo.Draw;

		Color color;
		if ( IsCaptured )
			color = CapturedRadiusColor;
		else if ( IsPlayerInside )
			color = CapturingRadiusColor;
		else
			color = IdleRadiusColor;

		gizmo.Color = color;
		gizmo.LineThickness = LineThickness;

		Vector3 center = Transform.Position + Vector3.Up * RadiusHeightOffset;
		gizmo.LineSphere( center, CaptureRadius, SphereRings );
	}

	public int GetPreviewReward()
	{
		if ( IsCaptured ) return 0;
		return StatueRewardSystem.GetCurrentReward();
	}
}
