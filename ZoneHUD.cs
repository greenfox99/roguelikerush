using Sandbox;
using Sandbox.Rendering;
using System;

/// <summary>
/// HUD-предупреждение для игрока: пульсирующий "страшный" текст.
/// Вешается на игрока автоматически зоной EscalatingDamageZone.
/// </summary>
public sealed class ZoneWarningHUD : Component
{
	[Property] public string Text { get; set; } = "Вы разозлили продавца!";

	[Property] public float FontSize { get; set; } = 44f;
	[Property] public float PulseSpeed { get; set; } = 3.2f;
	[Property] public float PulseScale { get; set; } = 0.06f;

	[Property] public Color TextColor { get; set; } = new Color( 1f, 0.25f, 0.18f, 1f );
	[Property] public Color GlowColor { get; set; } = new Color( 1f, 0.05f, 0.02f, 0.35f );

	/// <summary>Где рисовать: 0.15 = ближе к верху, 0.5 = центр.</summary>
	[Property] public float ScreenY { get; set; } = 0.18f;

	[Property] public bool DebugMode { get; set; } = false;

	private bool _visible;
	private float _showRealTime;

	public void Show( string text = null )
	{
		if ( !string.IsNullOrWhiteSpace( text ) )
			Text = text;

		_visible = true;
		_showRealTime = RealTime.Now;

		if ( DebugMode )
			Log.Info( $"👁️ ZoneWarningHUD show: {Text}" );
	}

	public void Hide()
	{
		_visible = false;

		if ( DebugMode )
			Log.Info( "👁️ ZoneWarningHUD hide" );
	}

	protected override void OnUpdate()
	{
		if ( !_visible ) return;
		if ( Scene.Camera == null ) return;

		var hud = Scene.Camera.Hud;

		float t = (RealTime.Now - _showRealTime);
		float pulse = 0.5f + 0.5f * MathF.Sin( t * PulseSpeed * MathF.PI * 2f ); // 0..1
		float scale = 1f + (pulse - 0.5f) * 2f * PulseScale; // 1±
		float alpha = 0.85f + pulse * 0.15f;

		float w = Screen.Width;
		float h = Screen.Height;

		float rectW = w * 0.95f;
		float rectH = FontSize * 1.4f;

		float left = (w - rectW) * 0.5f;
		float top = h * ScreenY;

		var rect = new Rect( left, top, rectW, rectH );

		// Glow: рисуем несколько раз с небольшим оффсетом
		DrawCentered( hud, Text, rect, GlowColor.WithAlpha( GlowColor.a * alpha ), FontSize * scale, 0, -2 );
		DrawCentered( hud, Text, rect, GlowColor.WithAlpha( GlowColor.a * alpha ), FontSize * scale, 2, 0 );
		DrawCentered( hud, Text, rect, GlowColor.WithAlpha( GlowColor.a * alpha ), FontSize * scale, -2, 0 );
		DrawCentered( hud, Text, rect, GlowColor.WithAlpha( GlowColor.a * alpha ), FontSize * scale, 0, 2 );

		// Main text
		DrawCentered( hud, Text, rect, TextColor.WithAlpha( alpha ), FontSize * scale, 0, 0 );
	}

	private void DrawCentered( HudPainter hud, string text, Rect rect, Color color, float size, float ox, float oy )
	{
		// Rect в Sandbox использует Left/Top/Width/Height (не x/y/w/h)
		var r = new Rect( rect.Left + ox, rect.Top + oy, rect.Width, rect.Height );
		var scope = new TextRendering.Scope( text ?? "", color, size );
		hud.DrawText( scope, r, TextFlag.Center );
	}
}
