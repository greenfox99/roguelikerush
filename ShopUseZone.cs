using Sandbox;
using Sandbox.Rendering;
using System.Linq;

public sealed class ShopUseZone : Component
{
	[Property] public ShopTrigger Trigger { get; set; }
	[Property] public ShopManager Shop { get; set; }

	// Надпись
	[Property] public string PromptText { get; set; } = "Открыть магазин \"E\"";

	// Позиция на экране (0..1)
	[Property] public float PromptScreenY { get; set; } = 0.78f;

	// Визуал
	[Property] public float FontSize { get; set; } = 24f;
	[Property] public float PaddingX { get; set; } = 18f;
	[Property] public float PaddingY { get; set; } = 10f;
	[Property] public float Radius { get; set; } = 14f;

	[Property] public Color Bg { get; set; } = new Color( 0f, 0f, 0f, 0.55f );
	[Property] public Color Border { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property] public Color TextColor { get; set; } = new Color( 1f, 1f, 1f, 0.95f );

	protected override void OnStart()
	{
		Trigger ??= Components.Get<ShopTrigger>( FindMode.EnabledInSelfAndDescendants );
		Shop ??= Scene.GetAllComponents<ShopManager>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		if ( Trigger == null || Shop == null )
			return;

		// если игрок не в зоне — ничего
		if ( !Trigger.PlayerInside )
			return;

		// ✅ показываем подсказку только когда магазин можно открыть
		if ( Shop.CanOpenFromUse )
			DrawPrompt();

		// открытие
		if ( Input.Pressed( "use" ) && Shop.CanOpenFromUse )
		{
			Shop.Open();
		}
	}

	private void DrawPrompt()
	{
		if ( Scene.Camera == null ) return;

		var hud = Scene.Camera.Hud;

		// простая оценка ширины текста (чтобы не городить измерение текста)
		float approxTextW = (PromptText.Length * FontSize) * 0.56f;
		float w = approxTextW + PaddingX * 2f;
		float h = FontSize + PaddingY * 2f;

		float x = (Screen.Width * 0.5f) - (w * 0.5f);
		float y = Screen.Height * PromptScreenY - (h * 0.5f);

		DrawRoundedRect( hud, x, y, w, h, Bg, Radius, 2f, Border );

		hud.DrawText(
			new TextRendering.Scope( PromptText, TextColor, FontSize ),
			new Rect( x, y, w, h ),
			TextFlag.Center
		);
	}

	private static void DrawRoundedRect( HudPainter hud, float x, float y, float w, float h, Color fill, float radius, float border, Color borderColor )
	{
		var r = new Rect( x, y, w, h );
		var corner = new Vector4( radius, radius, radius, radius );
		var bw = new Vector4( border, border, border, border );
		hud.DrawRect( r, fill, corner, bw, borderColor );
	}
}
