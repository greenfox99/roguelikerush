using Sandbox;
using Sandbox.Rendering;
using System;
using System.Linq;

public sealed class TargetMarkerHUD : Component
{
	[Property] public PlayerStaff Staff { get; set; }

	[Property] public float WorldUpOffset { get; set; } = 85f;
	[Property] public float ScreenUpOffsetPx { get; set; } = 18f;

	[Property] public float ArrowTextSize { get; set; } = 36f;

	[Property] public float PulseSpeed { get; set; } = 6f;
	[Property] public float PulsePixels { get; set; } = 8f;

	[Property] public Color ArrowColor { get; set; } = new Color( 0.35f, 0.95f, 1.00f, 0.95f );
	[Property] public Color ShadowColor { get; set; } = new Color( 0f, 0f, 0f, 0.45f );

	protected override void OnStart()
	{
		if ( Staff == null )
			Staff = Scene.GetAllComponents<PlayerStaff>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		if ( Scene.Camera == null ) return;
		if ( Staff == null || !Staff.IsValid ) return;

		var targets = Staff.CurrentTargets;
		if ( targets == null || targets.Count == 0 ) return;

		HudPainter hud = Scene.Camera.Hud;

		for ( int i = 0; i < targets.Count; i++ )
		{
			var t = targets[i];
			if ( t == null || !t.IsValid ) continue;

			DrawArrow( hud, t.Transform.Position, i );
		}
	}

	private void DrawArrow( HudPainter hud, Vector3 worldPos, int index )
	{
		Vector3 p = worldPos + Vector3.Up * WorldUpOffset;

		bool behind;
		Vector2 screen = Scene.Camera.PointToScreenPixels( p, out behind );
		if ( behind ) return;

		float pulse = MathF.Sin( (Time.Now + index * 0.15f) * PulseSpeed ) * PulsePixels;

		float x = screen.x;
		float y = screen.y - ScreenUpOffsetPx - pulse;

		// тень
		hud.DrawText(
			new TextRendering.Scope( "▼", ShadowColor, ArrowTextSize ),
			new Vector2( x - 12f + 1f, y - 18f + 1f )
		);

		// стрелка
		hud.DrawText(
			new TextRendering.Scope( "▼", ArrowColor, ArrowTextSize ),
			new Vector2( x - 12f, y - 18f )
		);
	}
}
