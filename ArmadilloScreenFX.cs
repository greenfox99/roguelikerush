using Sandbox;
using Sandbox.Rendering;

public sealed class ArmadilloScreenFX : Component
{
	[Property] public float WhiteAmount { get; set; } = 0.22f;
	[Property] public float FadeInSpeed { get; set; } = 8f;
	[Property] public float FadeOutSpeed { get; set; } = 4f;

	[Property] public float FlashStrength { get; set; } = 0.18f;
	[Property] public float FlashFadeSpeed { get; set; } = 5f;

	private float _strength = 0f;
	private float _flash = 0f;
	private bool _wasActive = false;

	protected override void OnUpdate()
	{
		if ( Scene.Camera == null )
			return;

		bool active = PlayerHealth.Instance != null && PlayerHealth.Instance.IsArmadilloActive;

		if ( active && !_wasActive )
			_flash = 1f;

		_wasActive = active;

		float target = active ? 1f : 0f;
		float speed = active ? FadeInSpeed : FadeOutSpeed;

		_strength = Lerp( _strength, target, Time.Delta * speed );
		_flash = Lerp( _flash, 0f, Time.Delta * FlashFadeSpeed );

		if ( _strength <= 0.001f && _flash <= 0.001f )
			return;

		DrawFX();
	}

	private void DrawFX()
	{
		var hud = Scene.Camera.Hud;

		float sw = Screen.Width;
		float sh = Screen.Height;

		DrawInsetVignette(
			hud,
			sw,
			sh,
			new Color( 1f, 1f, 1f, WhiteAmount * _strength )
		);

		hud.DrawRect(
			new Rect( 0, 0, sw, sh ),
			new Color( 1f, 1f, 1f, 0.020f * _strength )
		);

		if ( _flash > 0.001f )
		{
			hud.DrawRect(
				new Rect( 0, 0, sw, sh ),
				new Color( 1f, 1f, 1f, FlashStrength * _flash )
			);
		}
	}

	private void DrawInsetVignette( HudPainter hud, float sw, float sh, Color baseColor )
	{
		int steps = 18;

		float maxInsetX = sw * 0.11f;
		float maxInsetY = sh * 0.11f;

		for ( int i = 0; i < steps; i++ )
		{
			float t0 = i / (float)steps;
			float t1 = (i + 1) / (float)steps;

			float insetX0 = maxInsetX * t0;
			float insetY0 = maxInsetY * t0;

			float insetX1 = maxInsetX * t1;
			float insetY1 = maxInsetY * t1;

			float alpha = baseColor.a * (1f - t0) * (1f - t0) * 0.85f;
			Color c = new Color( baseColor.r, baseColor.g, baseColor.b, alpha );

			hud.DrawRect(
				new Rect( insetX0, insetY0, sw - insetX0 * 2f, insetY1 - insetY0 ),
				c
			);

			hud.DrawRect(
				new Rect( insetX0, sh - insetY1, sw - insetX0 * 2f, insetY1 - insetY0 ),
				c
			);

			hud.DrawRect(
				new Rect( insetX0, insetY0, insetX1 - insetX0, sh - insetY0 * 2f ),
				c
			);

			hud.DrawRect(
				new Rect( sw - insetX1, insetY0, insetX1 - insetX0, sh - insetY0 * 2f ),
				c
			);
		}
	}

	private float Lerp( float a, float b, float t )
	{
		t = Clamp01( t );
		return a + (b - a) * t;
	}

	private float Clamp01( float v )
	{
		if ( v < 0f ) return 0f;
		if ( v > 1f ) return 1f;
		return v;
	}
}
