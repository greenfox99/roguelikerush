using Sandbox;
using Sandbox.Rendering;

public sealed class TimeSlowScreenFX : Component
{
	[Property] public float BlueAmount { get; set; } = 0.20f;
	[Property] public float FadeInSpeed { get; set; } = 7f;
	[Property] public float FadeOutSpeed { get; set; } = 4f;

	[Property] public float FlashStrength { get; set; } = 0.16f;
	[Property] public float FlashFadeSpeed { get; set; } = 5f;

	private float _strength = 0f;
	private float _flash = 0f;
	private bool _wasActive = false;

	protected override void OnUpdate()
	{
		if ( Scene.Camera == null )
			return;

		bool active = TimeSlowSystem.IsActive;

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

		// ===== Мягкая синяя виньетка по краям =====
		DrawInsetVignette(
			hud,
			sw,
			sh,
			new Color( 0.18f, 0.45f, 1.0f, BlueAmount * _strength )
		);

		// ===== Лёгкий общий холодный тон =====
		hud.DrawRect(
			new Rect( 0, 0, sw, sh ),
			new Color( 0.08f, 0.16f, 0.35f, 0.035f * _strength )
		);

		// ===== Вспышка при активации =====
		if ( _flash > 0.001f )
		{
			hud.DrawRect(
				new Rect( 0, 0, sw, sh ),
				new Color( 0.80f, 0.92f, 1.0f, FlashStrength * _flash )
			);
		}
	}

	private void DrawInsetVignette( HudPainter hud, float sw, float sh, Color baseColor )
	{
		// много тонких рамок от края к центру
		// так выглядит сильно мягче, чем 4 жирных прямоугольника
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

			// top
			hud.DrawRect(
				new Rect( insetX0, insetY0, sw - insetX0 * 2f, insetY1 - insetY0 ),
				c
			);

			// bottom
			hud.DrawRect(
				new Rect( insetX0, sh - insetY1, sw - insetX0 * 2f, insetY1 - insetY0 ),
				c
			);

			// left
			hud.DrawRect(
				new Rect( insetX0, insetY0, insetX1 - insetX0, sh - insetY0 * 2f ),
				c
			);

			// right
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
