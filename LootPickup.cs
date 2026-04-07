using Sandbox;
using System;
using System.Linq;

public enum LootType
{
	Coins = 0,
	Exp = 1
}

public sealed class LootPickup : Component
{
	[Property] public LootType Type { get; set; } = LootType.Coins;
	[Property] public int Amount { get; set; } = 1;

	[Property, Group( "Collect" )] public float CollectDistance { get; set; } = 70f;
	[Property, Group( "Collect" )] public float MagnetDistance { get; set; } = 240f;
	[Property, Group( "Collect" )] public float MagnetSpeed { get; set; } = 1200f;
	[Property, Group( "Collect" )] public float LifetimeSeconds { get; set; } = 180f;

	[Property, Group( "Visual" )] public float SpinDegPerSec { get; set; } = 180f;
	[Property, Group( "Visual" )] public float BobHeight { get; set; } = 6f;
	[Property, Group( "Visual" )] public float BobSpeed { get; set; } = 3f;

	[Property, Group( "Stack Label" )] public bool ShowStackLabel { get; set; } = true;
	[Property, Group( "Stack Label" )] public float LabelHeight { get; set; } = 42f;
	[Property, Group( "Stack Label" )] public float LabelScale { get; set; } = 0.25f;
	[Property, Group( "Stack Label" )] public float LabelFontSize { get; set; } = 64f;
	[Property, Group( "Stack Label" )] public Color CoinsLabelColor { get; set; } = new Color( 1.0f, 0.92f, 0.25f, 1f );
	[Property, Group( "Stack Label" )] public Color ExpLabelColor { get; set; } = new Color( 0.35f, 0.85f, 1.00f, 1f );
	[Property, Group( "Stack Label" )] public bool LabelYawOnly { get; set; } = true;

	[Property, Group( "FX" )] public GameObject CollectEffectPrefab { get; set; }
	[Property, Group( "FX" )] public SoundEvent CollectSound { get; set; }

	private Vector3 _basePos;
	private float _lifeGameTime;

	private Player _player;
	private PlayerStats _stats;
	private PlayerLevel _level;

	private TimeSince _refindTimer;

	private GameObject _labelGo;
	private TextRenderer _label;

	protected override void OnStart()
	{
		_basePos = Transform.Position;
		_lifeGameTime = 0f;

		FindRefs();
		UpdateStackLabel();
		UpdateVisualPosition();
	}

	protected override void OnDestroy()
	{
		DestroyLabel();
		base.OnDestroy();
	}

	public void Setup( LootType type, int amount )
	{
		Type = type;
		Amount = Math.Max( 1, amount );
		UpdateStackLabel();
	}

	private void FindRefs()
	{
		_player = Scene.GetAllComponents<Player>().FirstOrDefault();
		_stats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		_level = Scene.GetAllComponents<PlayerLevel>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		_lifeGameTime += Time.Delta;

		if ( LifetimeSeconds > 0f && _lifeGameTime > LifetimeSeconds )
		{
			GameObject.Destroy();
			return;
		}

		if ( _refindTimer > 0.8f && (_player == null || !_player.IsValid || _stats == null || !_stats.IsValid || _level == null || !_level.IsValid) )
		{
			_refindTimer = 0f;
			FindRefs();
		}

		Transform.Rotation *= Rotation.FromYaw( SpinDegPerSec * Time.Delta );

		Vector3 collectorPos = default;

		if ( _player != null && _player.IsValid )
		{
			collectorPos = _player.Transform.Position;
		}
		else if ( Scene.Camera != null )
		{
			collectorPos = Scene.Camera.Transform.Position;
		}
		else
		{
			UpdateVisualPosition();
			UpdateLabelTransform();
			return;
		}

		Vector3 targetPos = collectorPos + Vector3.Up * 40f;

		float currentMagnetDistance = MagnetDistance;
		float currentCollectDistance = CollectDistance;
		float currentMagnetSpeed = MagnetSpeed;

		var magnet = MagnetAbilityController.FindIn( Scene );
		if ( magnet != null && magnet.Unlocked && magnet.IsActive )
		{
			currentMagnetDistance = MathF.Max( currentMagnetDistance, magnet.GlobalMagnetDistance );
			currentCollectDistance = MathF.Max( currentCollectDistance, magnet.GlobalCollectDistance );
			currentMagnetSpeed *= MathF.Max( 1f, magnet.ActiveMagnetSpeedMultiplier );
		}

		float d = Vector3.DistanceBetween( _basePos, targetPos );

		if ( d <= currentMagnetDistance )
		{
			Vector3 toTarget = targetPos - _basePos;
			float len = toTarget.Length;

			if ( len > 0.001f )
			{
				Vector3 dir = toTarget / len;
				_basePos += dir * currentMagnetSpeed * Time.Delta;
			}
		}

		UpdateVisualPosition();
		UpdateLabelTransform();

		if ( d <= currentCollectDistance )
		{
			CollectNow();
		}
	}

	private void UpdateVisualPosition()
	{
		float bob = MathF.Sin( Time.Now * BobSpeed ) * BobHeight;
		Transform.Position = _basePos + Vector3.Up * bob;
	}

	private void UpdateStackLabel()
	{
		if ( !ShowStackLabel || Amount <= 1 )
		{
			DestroyLabel();
			return;
		}

		EnsureLabel();

		if ( _label == null || !_label.IsValid )
			return;

		_label.Text = $"x{Amount}";
		_label.FontSize = LabelFontSize;
		_label.Scale = LabelScale;
		_label.HorizontalAlignment = TextRenderer.HAlignment.Center;
		_label.VerticalAlignment = TextRenderer.VAlignment.Center;
		_label.Color = (Type == LootType.Coins) ? CoinsLabelColor : ExpLabelColor;
	}

	private void EnsureLabel()
	{
		if ( _labelGo != null && _labelGo.IsValid && _label != null && _label.IsValid )
			return;

		_labelGo = new GameObject( true, "LootStackLabel" );
		_labelGo.SetParent( GameObject );

		_label = _labelGo.Components.Create<TextRenderer>();
	}

	private void UpdateLabelTransform()
	{
		if ( _labelGo == null || !_labelGo.IsValid || _label == null || !_label.IsValid )
			return;

		_labelGo.Transform.Position = Transform.Position + Vector3.Up * LabelHeight;

		var cam = Scene.Camera;
		if ( cam == null ) return;

		Vector3 dir = cam.Transform.Position - _labelGo.Transform.Position;

		if ( LabelYawOnly )
			dir = new Vector3( dir.x, dir.y, 0f );

		if ( dir.Length < 0.001f ) return;

		_labelGo.Transform.Rotation = Rotation.LookAt( dir.Normal, Vector3.Up );
	}

	private void DestroyLabel()
	{
		if ( _labelGo != null && _labelGo.IsValid )
		{
			_labelGo.Destroy();
		}

		_labelGo = null;
		_label = null;
	}

	private void CollectNow()
	{
		int amt = Math.Max( 1, Amount );

		if ( Type == LootType.Coins )
		{
			_stats?.AddCoins( amt );
		}
		else
		{
			_level?.AddExp( amt );
			_stats?.AddExp( amt );
		}

		if ( CollectSound != null )
			Sound.Play( CollectSound, Transform.Position );

		if ( CollectEffectPrefab != null )
		{
			var fx = CollectEffectPrefab.Clone( Transform.Position );
			fx.Name = "LootCollectFX";
		}

		GameObject.Destroy();
	}
}
