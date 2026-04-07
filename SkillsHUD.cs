using Sandbox;
using Sandbox.Rendering;
using System.Linq;
using YourGame.UI;

public sealed class SkillsHUD : Component
{
	// Насколько увеличить скиллы (3.0 = x3, 4.0 = x4)
	[Property] public float Scale { get; set; } = 3.2f;

	// Горизонтальный зазор от краёв стамины до панели скилла
	[Property] public float GapFromStamina { get; set; } = 18f;

	// Доп. смещение по Y (вверх/вниз)
	[Property] public float VerticalOffset { get; set; } = 0f;

	// Визуал
	[Property] public float Radius { get; set; } = 12f;
	[Property] public Color Bg { get; set; } = new Color( 0, 0, 0, 0.55f );
	[Property] public Color Border { get; set; } = new Color( 1, 1, 1, 0.08f );
	[Property] public Color Text { get; set; } = new Color( 1, 1, 1, 0.92f );
	[Property] public Color Muted { get; set; } = new Color( 1, 1, 1, 0.60f );
	[Property] public Color CdFill { get; set; } = new Color( 0.35f, 0.85f, 1.0f, 0.90f );

	private PlayerSkills _skills;
	private GameHUD _gameHud;

	protected override void OnStart()
	{
		GameLocalization.EnsureLoaded();
		_skills = Scene.GetAllComponents<PlayerSkills>().FirstOrDefault();
		_gameHud = Scene.GetAllComponents<GameHUD>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		if ( Scene.Camera == null ) return;

		_skills ??= Scene.GetAllComponents<PlayerSkills>().FirstOrDefault();
		_gameHud ??= Scene.GetAllComponents<GameHUD>().FirstOrDefault();
		if ( _skills == null ) return;

		Draw();
	}

	private void Draw()
	{
		var hud = Scene.Camera.Hud;

		float s = Scale;

		float baseWidth = 320f;
		float baseHeight = 58f;

		// ширина растёт мягче
		float panelW = baseWidth * (0.85f * s);
		float panelH = baseHeight * s;

		// забираем размеры стамины из твоего GameHUD (если есть)
		float staminaW = (_gameHud != null) ? _gameHud.StaminaBarWidth : 520f;
		float staminaH = (_gameHud != null) ? _gameHud.StaminaBarHeight : 18f;
		float staminaBottomOffset = (_gameHud != null) ? _gameHud.StaminaBarBottomOffset : 70f;

		float staminaX = (Screen.Width * 0.5f) - (staminaW * 0.5f);
		float staminaY = Screen.Height - staminaBottomOffset;
		float staminaRight = staminaX + staminaW;

		// панели "между стаминой и углами"
		float leftX = (staminaX - GapFromStamina) - panelW;
		float rightX = staminaRight + GapFromStamina;

		// выравнивание по низу стамины
		float staminaBottom = staminaY + staminaH;
		float y = staminaBottom - panelH + VerticalOffset;

		leftX = Clamp( leftX, 8f, Screen.Width - panelW - 8f );
		rightX = Clamp( rightX, 8f, Screen.Width - panelW - 8f );
		y = Clamp( y, 8f, Screen.Height - panelH - 8f );

		DrawSlot( hud, leftX, y, panelW, panelH, "Q", _skills.Slot1Id, _skills.Slot1CooldownLeft, s );
		DrawSlot( hud, rightX, y, panelW, panelH, "R", _skills.Slot2Id, _skills.Slot2CooldownLeft, s );
	}

	private void DrawSlot( HudPainter hud, float x, float y, float w, float h, string keyHint, string id, float cdLeft, float s )
	{
		DrawRoundedRect( hud, x, y, w, h, Bg, Radius * s, 1.5f * s, Border );

		string icon = "❔";
		string name = T( "skillshud.empty.name", "Пусто", "Empty" );
		string desc = T( "skillshud.empty.desc", "Купи скилл в магазине", "Buy a skill in shop" );
		Texture tex = null;

		if ( !string.IsNullOrEmpty( id ) )
		{
			var def = _skills.GetDef( id );
			if ( def != null )
			{
				icon = def.Icon ?? "⭐";
				name = GetSkillDisplayName( id, def.Name );
				desc = GetSkillDisplayDescription( id, def.Description );
				tex = def.IconTexture;
			}
		}

		// размеры текста
		float iconTextSize = 26f * s;
		float nameSize = 18f * s;
		float descSize = 13f * s;
		float keySize = 18f * s;

		// лейаут
		float pad = 10f * s;
		float iconW = 44f * s;
		float left = x + pad;

		// ✅ ИКОНКА: если есть Texture — рисуем её, иначе эмодзи
		var iconRect = new Rect( left, y + (8f * s), iconW, h - (16f * s) );
		if ( tex != null && tex.IsValid )
		{
			hud.DrawTexture( tex, iconRect );
		}
		else
		{
			hud.DrawText(
				new TextRendering.Scope( icon, Text, iconTextSize ),
				new Rect( left, y, iconW, h ),
				TextFlag.Center
			);
		}

		hud.DrawText(
			new TextRendering.Scope( keyHint, new Color( 1f, 0.92f, 0.20f, 0.95f ), keySize ),
			new Rect( left + iconW + (6f * s), y + (6f * s), 40f * s, 24f * s ),
			TextFlag.Left
		);

		hud.DrawText(
			new TextRendering.Scope( name, Text, nameSize ),
			new Rect( left + iconW + (46f * s), y + (6f * s), w - (iconW + 2 * pad), 26f * s ),
			TextFlag.Left
		);

		hud.DrawText(
			new TextRendering.Scope( desc, Muted, descSize ),
			new Rect( left + iconW + (46f * s), y + (30f * s), w - (iconW + 2 * pad), 24f * s ),
			TextFlag.Left
		);

		// кулдаун бар
		if ( cdLeft > 0.01f )
		{
			float barW = w - (2f * pad);
			float barH = 8f * s;

			float bx = x + pad;
			float by = y + h - pad - barH;

			DrawRoundedRect( hud, bx, by, barW, barH, new Color( 1, 1, 1, 0.10f ), barH * 0.5f, 0f, new Color( 0, 0, 0, 0 ) );

			float cd = 1f;
			if ( !string.IsNullOrEmpty( id ) )
			{
				var def = _skills.GetDef( id );
				if ( def != null ) cd = def.Cooldown;
			}

			float t = cd > 0.001f ? (cdLeft / cd) : 1f;
			t = Clamp01( t );

			DrawRoundedRect( hud, bx, by, barW * t, barH, CdFill, barH * 0.5f, 0f, new Color( 0, 0, 0, 0 ) );
		}
	}


	private string T( string key, string russianFallback, string englishFallback )
	{
		GameLocalization.EnsureLoaded();
		return GameLocalization.T( key, GameLocalization.IsLanguage( "ru" ) ? russianFallback : englishFallback );
	}

	private string GetSkillDisplayName( string id, string fallbackName )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return fallbackName ?? string.Empty;

		if ( !string.IsNullOrWhiteSpace( fallbackName ) && fallbackName.StartsWith( "#", System.StringComparison.Ordinal ) )
			return GameLocalization.T( fallbackName, fallbackName );

		return GameLocalization.T( $"skillshud.skill.{id}.name", fallbackName ?? id );
	}

	private string GetSkillDisplayDescription( string id, string fallbackDescription )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return fallbackDescription ?? string.Empty;

		if ( !string.IsNullOrWhiteSpace( fallbackDescription ) && fallbackDescription.StartsWith( "#", System.StringComparison.Ordinal ) )
			return GameLocalization.T( fallbackDescription, fallbackDescription );

		return GameLocalization.T( $"skillshud.skill.{id}.desc", fallbackDescription ?? string.Empty );
	}

	private static float Clamp01( float v ) => v < 0 ? 0 : (v > 1 ? 1 : v);

	private static float Clamp( float v, float min, float max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}

	private static void DrawRoundedRect( HudPainter hud, float x, float y, float w, float h, Color fill, float radius, float border, Color borderColor )
	{
		var r = new Rect( x, y, w, h );
		var corner = new Vector4( radius, radius, radius, radius );
		var bw = new Vector4( border, border, border, border );
		hud.DrawRect( r, fill, corner, bw, borderColor );
	}
}
