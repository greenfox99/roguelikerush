using Sandbox;
using Sandbox.Rendering;
using System.Linq;

public sealed class ShopSkillsMenu : Component
{
	[Property] public PlayerSkills PlayerSkills { get; set; }
	[Property] public PlayerStats PlayerStats { get; set; }

	[Property] public float OverlayAlpha { get; set; } = 0.62f;

	// Базовые размеры панели. Реальный размер дополнительно масштабируется от разрешения экрана.
	[Property] public float PanelWidth { get; set; } = 1480f;
	[Property] public float PanelHeight { get; set; } = 860f;
	[Property] public float Radius { get; set; } = 20f;

	private bool _isOpen;
	public bool IsOpen => _isOpen;

	private int _selected = 0;
	private float _scroll = 0f;

	protected override void OnStart()
	{
		PlayerSkills ??= Scene.GetAllComponents<PlayerSkills>().FirstOrDefault();
		PlayerStats ??= Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
	}

	public void Open()
	{
		_isOpen = true;
		_selected = 0;
		_scroll = 0f;
	}

	public void Close()
	{
		_isOpen = false;
	}

	protected override void OnUpdate()
	{
		if ( !_isOpen ) return;
		if ( Scene.Camera == null ) return;

		PlayerSkills ??= Scene.GetAllComponents<PlayerSkills>().FirstOrDefault();
		PlayerStats ??= Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		if ( PlayerSkills == null ) return;

		_scroll -= Input.MouseWheel.y * 100f;

		Draw();
		HandleClicks();
	}

	private void HandleClicks()
	{
		if ( !Input.Pressed( "attack1" ) ) return;
		if ( PlayerSkills == null || PlayerSkills.Skills == null || PlayerSkills.Skills.Count <= 0 ) return;

		var layout = GetLayout();
		Vector2 m = Mouse.Position;

		if ( RectContains( layout.ListRect, m ) )
		{
			int idx = IndexFromMouse( m, layout.ListRect );
			if ( idx >= 0 && idx < PlayerSkills.Skills.Count )
				_selected = idx;
		}

		_selected = System.Math.Clamp( _selected, 0, PlayerSkills.Skills.Count - 1 );

		if ( RectContains( layout.BuyRect, m ) )
		{
			var def = PlayerSkills.Skills[_selected];
			PlayerSkills.TryBuySkill( def.Id );
		}

		if ( RectContains( layout.EquipQRect, m ) )
		{
			var def = PlayerSkills.Skills[_selected];
			PlayerSkills.TryEquipSkill( 1, def.Id );
		}

		if ( RectContains( layout.EquipRRect, m ) )
		{
			var def = PlayerSkills.Skills[_selected];
			PlayerSkills.TryEquipSkill( 2, def.Id );
		}
	}

	private int IndexFromMouse( Vector2 mouse, Rect listRect )
	{
		float rowH = GetRowHeight();
		float y = mouse.y - listRect.Top + _scroll;
		if ( y < 0 ) return -1;
		return (int)(y / rowH);
	}

	private void Draw()
	{
		var hud = Scene.Camera.Hud;
		var panel = GetPanelRect();
		float ui = GetUiScale();

		hud.DrawRect( new Rect( 0, 0, Screen.Width, Screen.Height ), new Color( 0, 0, 0, OverlayAlpha ) );

		DrawRoundedRect(
			hud,
			panel.Left,
			panel.Top,
			panel.Width,
			panel.Height,
			new Color( 0.08f, 0.10f, 0.13f, 0.94f ),
			Radius * ui,
			2f,
			new Color( 1, 1, 1, 0.10f )
		);

		hud.DrawText(
			new TextRendering.Scope( "СКИЛЛЫ", new Color( 1, 1, 1, 0.95f ), 46f * ui ),
			new Rect( panel.Left, panel.Top + 24f * ui, panel.Width, 46f * ui ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( $"🪙 Монет: {PlayerStats?.Coins ?? 0}", new Color( 1, 1, 1, 0.70f ), 22f * ui ),
			new Rect( panel.Left, panel.Top + 80f * ui, panel.Width, 28f * ui ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( "ЛКМ: выбрать / купить / экип • ESC / E: назад", new Color( 1, 1, 1, 0.55f ), 19f * ui ),
			new Rect( panel.Left, panel.Bottom - 42f * ui, panel.Width, 24f * ui ),
			TextFlag.Center
		);

		var layout = GetLayout();

		DrawList( hud, layout.ListRect );
		DrawDetails( hud, layout.DetailsRect, layout.BuyRect, layout.EquipQRect, layout.EquipRRect );
	}

	private void DrawList( HudPainter hud, Rect listRect )
	{
		if ( PlayerSkills == null || PlayerSkills.Skills == null ) return;

		float ui = GetUiScale();
		float rowH = GetRowHeight();

		DrawRoundedRect(
			hud,
			listRect.Left,
			listRect.Top,
			listRect.Width,
			listRect.Height,
			new Color( 1, 1, 1, 0.03f ),
			16f * ui,
			2f,
			new Color( 1, 1, 1, 0.08f )
		);

		float contentH = PlayerSkills.Skills.Count * rowH;
		float maxScroll = System.MathF.Max( 0f, contentH - listRect.Height );
		_scroll = Clamp( _scroll, 0f, maxScroll );

		Vector2 m = Mouse.Position;

		for ( int i = 0; i < PlayerSkills.Skills.Count; i++ )
		{
			float y = listRect.Top + i * rowH - _scroll;
			if ( y + rowH < listRect.Top ) continue;
			if ( y > listRect.Bottom ) break;

			var row = new Rect(
				listRect.Left + 12f * ui,
				y + 7f * ui,
				listRect.Width - 24f * ui,
				rowH - 14f * ui
			);

			bool hover = RectContains( row, m );
			bool selected = (i == _selected);

			Color bg = selected ? new Color( 0.22f, 0.30f, 0.40f, 0.78f )
				: hover ? new Color( 0.16f, 0.20f, 0.28f, 0.67f )
				: new Color( 0.10f, 0.12f, 0.16f, 0.58f );

			DrawRoundedRect( hud, row.Left, row.Top, row.Width, row.Height, bg, 13f * ui, 2f, new Color( 1, 1, 1, 0.08f ) );

			var s = PlayerSkills.Skills[i];
			bool unlocked = PlayerSkills.IsUnlocked( s.Id );
			bool canAfford = (PlayerStats?.Coins ?? 0) >= s.Price;

			var iconRect = new Rect( row.Left + 14f * ui, row.Top + 10f * ui, 54f * ui, row.Height - 20f * ui );
			if ( s.IconTexture != null && s.IconTexture.IsValid )
			{
				hud.DrawTexture( s.IconTexture, iconRect );
			}
			else
			{
				hud.DrawText(
					new TextRendering.Scope( s.Icon ?? "⭐", new Color( 1, 1, 1, 0.95f ), 30f * ui ),
					new Rect( row.Left + 14f * ui, row.Top + 10f * ui, 54f * ui, row.Height - 10f * ui ),
					TextFlag.Left
				);
			}

			hud.DrawText(
				new TextRendering.Scope( s.Name, new Color( 1, 1, 1, 0.95f ), 22f * ui ),
				new Rect( row.Left + 78f * ui, row.Top + 12f * ui, row.Width - 160f * ui, 26f * ui ),
				TextFlag.Left
			);

			hud.DrawText(
				new TextRendering.Scope( s.Description, new Color( 1, 1, 1, 0.55f ), 16f * ui ),
				new Rect( row.Left + 78f * ui, row.Top + 40f * ui, row.Width - 160f * ui, 22f * ui ),
				TextFlag.Left
			);

			string right = unlocked ? "✅" : (canAfford ? $"🪙 {s.Price}" : $"💸 {s.Price}");
			Color rightCol = unlocked
				? new Color( 0.7f, 1f, 0.7f, 0.95f )
				: (canAfford ? new Color( 0.7f, 1f, 0.7f, 0.95f ) : new Color( 1f, 0.55f, 0.55f, 0.95f ));

			hud.DrawText(
				new TextRendering.Scope( right, rightCol, 18f * ui ),
				new Rect( row.Left, row.Top + 18f * ui, row.Width - 16f * ui, 24f * ui ),
				TextFlag.Right
			);
		}
	}

	private void DrawDetails( HudPainter hud, Rect detRect, Rect buyRect, Rect equipQ, Rect equipR )
	{
		if ( PlayerSkills == null || PlayerSkills.Skills == null || PlayerSkills.Skills.Count <= 0 ) return;

		float ui = GetUiScale();

		DrawRoundedRect(
			hud,
			detRect.Left,
			detRect.Top,
			detRect.Width,
			detRect.Height,
			new Color( 1, 1, 1, 0.03f ),
			16f * ui,
			2f,
			new Color( 1, 1, 1, 0.08f )
		);

		_selected = System.Math.Clamp( _selected, 0, PlayerSkills.Skills.Count - 1 );
		var s = PlayerSkills.Skills[_selected];

		bool unlocked = PlayerSkills.IsUnlocked( s.Id );
		int coins = PlayerStats?.Coins ?? 0;
		bool canAfford = coins >= s.Price;

		var bigIconRect = new Rect(
			detRect.Left + (detRect.Width * 0.5f) - (58f * ui),
			detRect.Top + 26f * ui,
			116f * ui,
			116f * ui
		);

		if ( s.IconTexture != null && s.IconTexture.IsValid )
		{
			hud.DrawTexture( s.IconTexture, bigIconRect );
		}
		else
		{
			hud.DrawText(
				new TextRendering.Scope( s.Icon ?? "⭐", new Color( 1, 1, 1, 0.95f ), 68f * ui ),
				new Rect( detRect.Left, detRect.Top + 20f * ui, detRect.Width, 110f * ui ),
				TextFlag.Center
			);
		}

		hud.DrawText(
			new TextRendering.Scope( s.Name, new Color( 1, 1, 1, 0.95f ), 30f * ui ),
			new Rect( detRect.Left + 20f * ui, detRect.Top + 152f * ui, detRect.Width - 40f * ui, 34f * ui ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( s.Description, new Color( 1, 1, 1, 0.72f ), 18f * ui ),
			new Rect( detRect.Left + 28f * ui, detRect.Top + 194f * ui, detRect.Width - 56f * ui, 84f * ui ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( $"Cooldown: {s.Cooldown:0.0}s", new Color( 1, 1, 1, 0.55f ), 18f * ui ),
			new Rect( detRect.Left, detRect.Top + 292f * ui, detRect.Width, 24f * ui ),
			TextFlag.Center
		);

		if ( s.Effect == SkillEffectType.BallLightning ||
			 s.Effect == SkillEffectType.TimeSlow ||
			 s.Effect == SkillEffectType.Armadillo )
		{
			hud.DrawText(
				new TextRendering.Scope( $"Duration: {s.BuffDuration:0.0}s", new Color( 1, 1, 1, 0.55f ), 18f * ui ),
				new Rect( detRect.Left, detRect.Top + 322f * ui, detRect.Width, 24f * ui ),
				TextFlag.Center
			);
		}

		Vector2 m = Mouse.Position;
		bool buyHover = RectContains( buyRect, m );

		Color buyBg =
			unlocked ? new Color( 0.25f, 0.25f, 0.25f, 0.85f ) :
			!canAfford ? new Color( 0.45f, 0.20f, 0.20f, 0.90f ) :
			buyHover ? new Color( 0.20f, 0.65f, 0.25f, 0.95f ) :
			new Color( 0.18f, 0.55f, 0.22f, 0.90f );

		DrawRoundedRect( hud, buyRect.Left, buyRect.Top, buyRect.Width, buyRect.Height, buyBg, 14f * ui, 2f, new Color( 1, 1, 1, 0.10f ) );

		string buyText = unlocked ? "УЖЕ КУПЛЕНО ✅" : (canAfford ? $"КУПИТЬ за 🪙 {s.Price}" : $"НЕ ХВАТАЕТ 🪙 (надо {s.Price})");

		hud.DrawText(
			new TextRendering.Scope( buyText, new Color( 1, 1, 1, 0.95f ), 21f * ui ),
			new Rect( buyRect.Left, buyRect.Top + 14f * ui, buyRect.Width, buyRect.Height ),
			TextFlag.Center
		);

		DrawEquipButton( hud, equipQ, "ЭКИПНУТЬ на Q", 1, s.Id );
		DrawEquipButton( hud, equipR, "ЭКИПНУТЬ на R", 2, s.Id );
	}

	private void DrawEquipButton( HudPainter hud, Rect r, string text, int slot, string id )
	{
		float ui = GetUiScale();
		Vector2 m = Mouse.Position;
		bool hover = RectContains( r, m );

		bool equipped =
			(slot == 1 && PlayerSkills.Slot1Id == id) ||
			(slot == 2 && PlayerSkills.Slot2Id == id);

		Color bg =
			!PlayerSkills.IsUnlocked( id ) ? new Color( 0.20f, 0.20f, 0.20f, 0.70f ) :
			equipped ? new Color( 0.20f, 0.35f, 0.70f, 0.90f ) :
			hover ? new Color( 0.18f, 0.30f, 0.55f, 0.90f ) :
			new Color( 0.14f, 0.22f, 0.40f, 0.85f );

		DrawRoundedRect( hud, r.Left, r.Top, r.Width, r.Height, bg, 12f * ui, 2f, new Color( 1, 1, 1, 0.10f ) );

		string final = equipped ? "✅ УЖЕ НА СЛОТЕ" : text;

		hud.DrawText(
			new TextRendering.Scope( final, new Color( 1, 1, 1, 0.95f ), 18f * ui ),
			new Rect( r.Left, r.Top + 10f * ui, r.Width, r.Height ),
			TextFlag.Center
		);
	}

	private (Rect ListRect, Rect DetailsRect, Rect BuyRect, Rect EquipQRect, Rect EquipRRect) GetLayout()
	{
		Rect panel = GetPanelRect();
		float ui = GetUiScale();

		float pad = 28f * ui;
		float gap = 26f * ui;
		float topOffset = 124f * ui;
		float bottomPad = 34f * ui;

		float contentTop = panel.Top + topOffset;
		float contentHeight = panel.Height - topOffset - bottomPad;

		float listWidth = panel.Width * 0.54f;

		var list = new Rect(
			panel.Left + pad,
			contentTop,
			listWidth,
			contentHeight
		);

		var det = new Rect(
			list.Right + gap,
			contentTop,
			panel.Right - (list.Right + gap) - pad,
			contentHeight
		);

		var buy = new Rect(
			det.Left + 40f * ui,
			det.Bottom - 188f * ui,
			det.Width - 80f * ui,
			58f * ui
		);

		var eqQ = new Rect(
			det.Left + 40f * ui,
			det.Bottom - 118f * ui,
			det.Width - 80f * ui,
			50f * ui
		);

		var eqR = new Rect(
			det.Left + 40f * ui,
			det.Bottom - 58f * ui,
			det.Width - 80f * ui,
			50f * ui
		);

		return (list, det, buy, eqQ, eqR);
	}

	private Rect GetPanelRect()
	{
		float w = System.MathF.Max( PanelWidth, Screen.Width * 0.74f );
		float h = System.MathF.Max( PanelHeight, Screen.Height * 0.74f );

		float maxW = Screen.Width * 0.92f;
		float maxH = Screen.Height * 0.88f;

		if ( w > maxW ) w = maxW;
		if ( h > maxH ) h = maxH;

		float x = (Screen.Width - w) * 0.5f;
		float y = (Screen.Height - h) * 0.5f;

		return new Rect( x, y, w, h );
	}

	private float GetUiScale()
	{
		Rect panel = GetPanelRect();
		float sx = panel.Width / 1500f;
		float sy = panel.Height / 900f;
		float scale = sx < sy ? sx : sy;
		return Clamp( scale, 1f, 1.25f );
	}

	private float GetRowHeight()
	{
		float row = 78f * GetUiScale();
		if ( row < 72f ) row = 72f;
		return row;
	}

	private static bool RectContains( Rect r, Vector2 p )
	{
		return p.x >= r.Left && p.x <= r.Right && p.y >= r.Top && p.y <= r.Bottom;
	}

	private static void DrawRoundedRect( HudPainter hud, float x, float y, float w, float h, Color fill, float radius, float border, Color borderColor )
	{
		var r = new Rect( x, y, w, h );
		var corner = new Vector4( radius, radius, radius, radius );
		var bw = new Vector4( border, border, border, border );
		hud.DrawRect( r, fill, corner, bw, borderColor );
	}

	private static float Clamp( float v, float min, float max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}
}
