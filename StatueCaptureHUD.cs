using Sandbox;
using Sandbox.Rendering;
using System;
using System.Linq;
using YourGame.UI;

public sealed class StatueCaptureHUD : Component
{
	[Property] public float MaxShowDistance { get; set; } = 900f;

	[Property, Group( "Panel" )] public float PanelWidth { get; set; } = 420f;
	[Property, Group( "Panel" )] public float PanelHeight { get; set; } = 92f;
	[Property, Group( "Panel" )] public float TopOffset { get; set; } = 110f;
	[Property, Group( "Panel" )] public float CornerRadius { get; set; } = 16f;

	[Property, Group( "Colors" )] public Color PanelBg { get; set; } = new Color( 0f, 0f, 0f, 0.48f );
	[Property, Group( "Colors" )] public Color PanelBorder { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property, Group( "Colors" )] public Color TitleColor { get; set; } = new Color( 1.00f, 0.90f, 0.45f, 0.98f );
	[Property, Group( "Colors" )] public Color TextColor { get; set; } = new Color( 1f, 1f, 1f, 0.92f );
	[Property, Group( "Colors" )] public Color MutedColor { get; set; } = new Color( 1f, 1f, 1f, 0.65f );
	[Property, Group( "Colors" )] public Color ProgressBg { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property, Group( "Colors" )] public Color ProgressFill { get; set; } = new Color( 1.00f, 0.82f, 0.22f, 0.95f );

	[Property, Group( "Text" )] public float TitleSize { get; set; } = 24f;
	[Property, Group( "Text" )] public float TextSize { get; set; } = 17f;
	[Property, Group( "Text" )] public float SmallTextSize { get; set; } = 14f;

	private Player _player;

	protected override void OnStart()
	{
		GameLocalization.EnsureLoaded();
		_player = Scene.GetAllComponents<Player>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		if ( Scene.Camera == null )
			return;

		if ( _player == null || !_player.IsValid )
			_player = Scene.GetAllComponents<Player>().FirstOrDefault();

		if ( _player == null || !_player.IsValid )
			return;

		DrawHUD();
	}

	private void DrawHUD()
	{
		var target = FindBestStatue();
		if ( target == null )
			return;

		var hud = Scene.Camera.Hud;

		float w = PanelWidth;
		float h = PanelHeight;
		float x = (Screen.Width - w) * 0.5f;
		float y = TopOffset;

		// shadow
		DrawRoundedRect(
			hud,
			x + 2f, y + 3f, w, h,
			new Color( 0f, 0f, 0f, 0.30f ),
			CornerRadius,
			0f,
			new Color( 0f, 0f, 0f, 0f )
		);

		// panel
		DrawRoundedRect(
			hud,
			x, y, w, h,
			PanelBg,
			CornerRadius,
			1.5f,
			PanelBorder
		);

		string title = T( "statuecapture.title", "СБОР ДАРОВ", "GATHERING OFFERINGS" );
		string rewardText = string.Format( T( "statuecapture.reward", "Награда: 🪙 {0}", "Reward: 🪙 {0}" ), target.GetPreviewReward() );
		string stateText = target.IsPlayerInside
			? T( "statuecapture.capturing", "Идёт захват...", "Capturing..." )
			: string.Format( T( "statuecapture.move_closer", "Подойди ближе ({0} radius)", "Move closer ({0} radius)" ), MathF.Ceiling( target.CaptureRadius ) );

		hud.DrawText(
			new TextRendering.Scope( title, TitleColor, TitleSize ),
			new Rect( x, y + 8f, w, 28f ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( rewardText, TextColor, TextSize ),
			new Rect( x, y + 38f, w, 20f ),
			TextFlag.Center
		);

		float barX = x + 24f;
		float barY = y + 64f;
		float barW = w - 48f;
		float barH = 14f;

		DrawRoundedRect(
			hud,
			barX, barY, barW, barH,
			ProgressBg,
			7f,
			0f,
			new Color( 0f, 0f, 0f, 0f )
		);

		DrawRoundedRect(
			hud,
			barX, barY, barW * target.CaptureProgress, barH,
			ProgressFill,
			7f,
			0f,
			new Color( 0f, 0f, 0f, 0f )
		);

		string pctText = $"{MathF.Round( target.CaptureProgress * 100f )}%";
		hud.DrawText(
			new TextRendering.Scope( pctText, new Color( 1f, 1f, 1f, 0.9f ), SmallTextSize ),
			new Rect( barX, barY - 1f, barW, barH ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( stateText, MutedColor, SmallTextSize ),
			new Rect( x, y + h - 20f, w, 18f ),
			TextFlag.Center
		);
	}

	private string T( string key, string russianFallback, string englishFallback )
	{
		GameLocalization.EnsureLoaded();
		return GameLocalization.T( key, GameLocalization.IsLanguage( "ru" ) ? russianFallback : englishFallback );
	}

	private StatueCapturePoint FindBestStatue()
	{
		var statues = Scene.GetAllComponents<StatueCapturePoint>();
		if ( statues == null )
			return null;

		StatueCapturePoint best = null;
		float bestDist = float.MaxValue;

		foreach ( var s in statues )
		{
			if ( s == null || !s.IsValid ) continue;
			if ( s.IsCaptured ) continue;

			float dist = Vector3.DistanceBetween( _player.Transform.Position, s.Transform.Position );

			// показываем HUD только если статуя относительно рядом
			if ( dist > MaxShowDistance ) continue;

			// приоритет статуям, где игрок уже внутри радиуса
			if ( s.IsPlayerInside )
				return s;

			if ( dist < bestDist )
			{
				bestDist = dist;
				best = s;
			}
		}

		return best;
	}

	private static void DrawRoundedRect( HudPainter hud, float x, float y, float w, float h, Color fill, float radius, float border, Color borderColor )
	{
		var r = new Rect( x, y, w, h );
		var corner = new Vector4( radius, radius, radius, radius );
		var bw = new Vector4( border, border, border, border );
		hud.DrawRect( r, fill, corner, bw, borderColor );
	}
}
