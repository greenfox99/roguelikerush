using Sandbox;
using Sandbox.Rendering;

public sealed class TeleportScreenFX : Component
{
	// как быстро "схлопывается" экран
	[Property] public float CloseTime { get; set; } = 0.08f;

	// как быстро "раскрывается" обратно
	[Property] public float OpenTime { get; set; } = 0.12f;

	// насколько сильно схлопнуть (0.05 = почти точка)
	[Property] public float MinHoleScale { get; set; } = 0.06f;

	// затемнение
	[Property] public float MaxAlpha { get; set; } = 0.95f;

	private bool _playing;
	private float _startReal;

	public float CloseDuration => CloseTime;

	public void Play()
	{
		_playing = true;
		_startReal = RealTime.Now;
	}

	protected override void OnUpdate()
	{
		if ( !_playing ) return;
		if ( Scene.Camera == null ) return;

		float elapsed = RealTime.Now - _startReal;
		float total = CloseTime + OpenTime;

		if ( elapsed >= total )
		{
			_playing = false;
			return;
		}

		// tClose: 0..1
		// tOpen:  0..1
		float holeScale;

		if ( elapsed <= CloseTime )
		{
			float t = elapsed / System.MathF.Max( 0.0001f, CloseTime );
			t = Clamp01( t );
			t = EaseIn( t );

			// 1 -> Min
			holeScale = Lerp( 1f, MinHoleScale, t );
		}
		else
		{
			float t = (elapsed - CloseTime) / System.MathF.Max( 0.0001f, OpenTime );
			t = Clamp01( t );
			t = EaseOut( t );

			// Min -> 1
			holeScale = Lerp( MinHoleScale, 1f, t );
		}

		float alpha = MaxAlpha * (1f - holeScale);
		DrawIris( holeScale, alpha );
	}

	private void DrawIris( float holeScale, float alpha )
	{
		var hud = Scene.Camera.Hud;

		float sw = Screen.Width;
		float sh = Screen.Height;

		float holeW = sw * holeScale;
		float holeH = sh * holeScale;

		float cx = sw * 0.5f;
		float cy = sh * 0.5f;

		float left = cx - holeW * 0.5f;
		float right = cx + holeW * 0.5f;
		float top = cy - holeH * 0.5f;
		float bottom = cy + holeH * 0.5f;

		var c = new Color( 0, 0, 0, alpha );

		// top
		hud.DrawRect( new Rect( 0, 0, sw, top ), c );
		// bottom
		hud.DrawRect( new Rect( 0, bottom, sw, sh - bottom ), c );
		// left
		hud.DrawRect( new Rect( 0, top, left, holeH ), c );
		// right
		hud.DrawRect( new Rect( right, top, sw - right, holeH ), c );
	}

	private static float Clamp01( float v ) => v < 0 ? 0 : (v > 1 ? 1 : v);
	private static float Lerp( float a, float b, float t ) => a + (b - a) * Clamp01( t );

	private static float EaseIn( float t ) => t * t;                    // квадратичный
	private static float EaseOut( float t ) => 1f - (1f - t) * (1f - t);
}
