using Sandbox;
using Sandbox.Rendering;
using System;
using System.Linq;
using YourGame.UI;

public sealed class ShopManager : Component
{
	[Property] public PlayerStats PlayerStats { get; set; }
	[Property] public PlayerHealth PlayerHealth { get; set; }
	[Property] public PlayerStaff PlayerStaff { get; set; }
	[Property] public GameHUD GameHUD { get; set; }

	[Property] public ShopUpgradeMenu UpgradeMenu { get; set; }
	[Property] public ShopSkillsMenu SkillsMenu { get; set; }
	[Property] public ShopCasinoMenu CasinoMenu { get; set; }

	[Property] public float UseDebounceSeconds { get; set; } = 0.20f;
	[Property] public float ReopenCooldownSeconds { get; set; } = 0.25f;

	[Property, Group( "Exit Protection" )] public bool EnableExitProtection { get; set; } = true;
	[Property, Group( "Exit Protection" )] public float ExitProtectionDuration { get; set; } = 3.0f;
	[Property, Group( "Exit Protection" )] public float ExitProtectionCooldownSeconds { get; set; } = 30.0f;

	[Property] public float OverlayAlpha { get; set; } = 0.60f;

	[Property] public float PanelWidth { get; set; } = 920f;
	[Property] public float PanelHeight { get; set; } = 520f;
	[Property] public float PanelRadius { get; set; } = 18f;
	[Property] public float PanelBorder { get; set; } = 2f;

	[Property] public Color PanelBg { get; set; } = new Color( 0.08f, 0.10f, 0.13f, 0.92f );
	[Property] public Color PanelBorderColor { get; set; } = new Color( 1f, 1f, 1f, 0.10f );

	[Property] public Color TitleColor { get; set; } = new Color( 0.25f, 0.95f, 1.0f, 1.0f );
	[Property] public Color SubColor { get; set; } = new Color( 1f, 1f, 1f, 0.70f );

	private bool _isOpen;
	private float _oldTimeScale = 1f;

	private float _blockCloseUntilReal;
	private float _blockOpenUntilReal;
	private float _nextExitProtectionReadyAtReal = 0f;

	private MouseVisibility _oldMouseVisibility;

	public bool IsOpen => _isOpen;
	public bool IsBlockingEscape => _isOpen;
	public bool CanOpenFromUse => !_isOpen && RealTime.Now >= _blockOpenUntilReal;
	public bool CanTriggerExitProtection => RealTime.Now >= _nextExitProtectionReadyAtReal;
	public float ExitProtectionCooldownLeft => MathF.Max( 0f, _nextExitProtectionReadyAtReal - RealTime.Now );

	protected override void OnStart()
	{
		GameLocalization.EnsureLoaded();
		EnsureRefs();
	}

	private void EnsureRefs()
	{
		PlayerStats ??= Components.Get<PlayerStats>() ?? Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		PlayerHealth ??= Components.Get<PlayerHealth>() ?? Scene.GetAllComponents<PlayerHealth>().FirstOrDefault();
		PlayerStaff ??= Components.Get<PlayerStaff>() ?? Scene.GetAllComponents<PlayerStaff>().FirstOrDefault();
		GameHUD ??= Components.Get<GameHUD>() ?? Scene.GetAllComponents<GameHUD>().FirstOrDefault();

		UpgradeMenu ??= Components.Get<ShopUpgradeMenu>() ?? Scene.GetAllComponents<ShopUpgradeMenu>().FirstOrDefault();
		SkillsMenu ??= Components.Get<ShopSkillsMenu>() ?? Scene.GetAllComponents<ShopSkillsMenu>().FirstOrDefault();
		CasinoMenu ??= Components.Get<ShopCasinoMenu>() ?? Scene.GetAllComponents<ShopCasinoMenu>().FirstOrDefault();
	}

	public void Open()
	{
		if ( _isOpen ) return;

		EnsureRefs();

		_isOpen = true;
		_oldTimeScale = Scene.TimeScale;
		Scene.TimeScale = 0f;

		_blockCloseUntilReal = RealTime.Now + UseDebounceSeconds;

		_oldMouseVisibility = Mouse.Visibility;
		Mouse.Visibility = MouseVisibility.Visible;
	}

	public void Close()
	{
		if ( !_isOpen ) return;

		_isOpen = false;

		UpgradeMenu?.Close();
		SkillsMenu?.Close();
		CasinoMenu?.Close();

		Scene.TimeScale = _oldTimeScale;

		_blockOpenUntilReal = RealTime.Now + ReopenCooldownSeconds;

		Mouse.Visibility = _oldMouseVisibility;

		TryActivateExitProtection();
	}

	private void TryActivateExitProtection()
	{
		if ( !EnableExitProtection )
			return;

		if ( ExitProtectionDuration <= 0f || ExitProtectionCooldownSeconds < 0f )
			return;

		if ( !CanTriggerExitProtection )
			return;

		EnsureRefs();

		bool applied = false;

		if ( PlayerHealth != null )
		{
			PlayerHealth.GrantTemporaryInvulnerability( ExitProtectionDuration );
			applied = true;
		}

		if ( PlayerStaff != null )
		{
			PlayerStaff.LockAttacks( ExitProtectionDuration );
			applied = true;
		}

		if ( applied )
			_nextExitProtectionReadyAtReal = RealTime.Now + ExitProtectionCooldownSeconds;
	}

	public bool HandleEscapeAsBack()
	{
		if ( !_isOpen )
			return false;

		EnsureRefs();

		// Пока магазин открыт, ESC должен считаться обработанным,
		// даже если close-debounce ещё не дал закрыть окно.
		if ( RealTime.Now < _blockCloseUntilReal )
			return true;

		if ( UpgradeMenu != null && UpgradeMenu.IsOpen )
		{
			UpgradeMenu.Close();
			_blockCloseUntilReal = RealTime.Now + UseDebounceSeconds;
			return true;
		}

		if ( SkillsMenu != null && SkillsMenu.IsOpen )
		{
			SkillsMenu.Close();
			_blockCloseUntilReal = RealTime.Now + UseDebounceSeconds;
			return true;
		}

		if ( CasinoMenu != null && CasinoMenu.IsOpen )
		{
			CasinoMenu.Close();
			_blockCloseUntilReal = RealTime.Now + UseDebounceSeconds;
			return true;
		}

		Close();
		return true;
	}

	protected override void OnUpdate()
	{
		if ( !_isOpen ) return;

		EnsureRefs();

		// E — назад/закрыть
		if ( Input.Pressed( "use" ) && RealTime.Now >= _blockCloseUntilReal )
		{
			if ( UpgradeMenu != null && UpgradeMenu.IsOpen ) { UpgradeMenu.Close(); _blockCloseUntilReal = RealTime.Now + UseDebounceSeconds; return; }
			if ( SkillsMenu != null && SkillsMenu.IsOpen ) { SkillsMenu.Close(); _blockCloseUntilReal = RealTime.Now + UseDebounceSeconds; return; }
			if ( CasinoMenu != null && CasinoMenu.IsOpen ) { CasinoMenu.Close(); _blockCloseUntilReal = RealTime.Now + UseDebounceSeconds; return; }

			Close();
			return;
		}

		// если открыто подменю — главное меню не рисуем
		if ( UpgradeMenu != null && UpgradeMenu.IsOpen ) return;
		if ( SkillsMenu != null && SkillsMenu.IsOpen ) return;
		if ( CasinoMenu != null && CasinoMenu.IsOpen ) return;

		// кнопки 1/2/3
		if ( Input.Pressed( "Slot1" ) ) { UpgradeMenu?.Open(); _blockCloseUntilReal = RealTime.Now + UseDebounceSeconds; }
		if ( Input.Pressed( "Slot2" ) ) { SkillsMenu?.Open(); _blockCloseUntilReal = RealTime.Now + UseDebounceSeconds; }
		if ( Input.Pressed( "Slot3" ) ) { CasinoMenu?.Open(); _blockCloseUntilReal = RealTime.Now + UseDebounceSeconds; }

		DrawMain();
	}

	private void DrawMain()
	{
		if ( Scene.Camera == null ) return;
		var hud = Scene.Camera.Hud;

		hud.DrawRect( new Rect( 0, 0, Screen.Width, Screen.Height ), new Color( 0, 0, 0, OverlayAlpha ) );

		float cx = Screen.Width * 0.5f;
		float cy = Screen.Height * 0.5f;

		float px = cx - PanelWidth * 0.5f;
		float py = cy - PanelHeight * 0.5f;

		DrawRoundedRect( hud, px, py, PanelWidth, PanelHeight, PanelBg, PanelRadius, PanelBorder, PanelBorderColor );

		hud.DrawText( new TextRendering.Scope( T( "shopmanager.title", "МАГАЗИН", "SHOP" ), TitleColor, 44f ),
			new Rect( px, py + 24, PanelWidth, 54 ), TextFlag.Center );

		int coins = PlayerStats?.Coins ?? 0;
		hud.DrawText( new TextRendering.Scope(
			string.Format( T( "shopmanager.coins", "🪙 Монет: {0}", "🪙 Coins: {0}" ), coins ),
			SubColor, 18f ),
			new Rect( px, py + 74, PanelWidth, 24 ), TextFlag.Center );

		if ( EnableExitProtection )
		{
			string cdText = CanTriggerExitProtection
				? string.Format( T( "shopmanager.exit_protection.ready", "Выходная защита: {0:0.#}с готова", "Exit protection: {0:0.#}s ready" ), ExitProtectionDuration )
				: string.Format( T( "shopmanager.exit_protection.cooldown", "Выходная защита на КД: {0:0.0}с", "Exit protection cooldown: {0:0.0}s" ), ExitProtectionCooldownLeft );

			hud.DrawText( new TextRendering.Scope( cdText, new Color( 1f, 1f, 1f, 0.58f ), 16f ),
				new Rect( px, py + 100, PanelWidth, 22 ), TextFlag.Center );
		}

		float bw = 260f;
		float bh = 150f;
		float gap = 28f;

		float total = bw * 3 + gap * 2;
		float startX = cx - total * 0.5f;

		var r1 = new Rect( startX + (bw + gap) * 0, py + 170, bw, bh );
		var r2 = new Rect( startX + (bw + gap) * 1, py + 170, bw, bh );
		var r3 = new Rect( startX + (bw + gap) * 2, py + 170, bw, bh );

		DrawBtn( hud, r1, "1", T( "shopmanager.button.upgrades.title", "Улучшения", "Upgrades" ), T( "shopmanager.button.upgrades.desc", "Покупка апгрейдов", "Buy upgrades" ) );
		DrawBtn( hud, r2, "2", T( "shopmanager.button.skills.title", "Скиллы", "Skills" ), T( "shopmanager.button.skills.desc", "Покупка + экип Q/R", "Buy + equip Q/R" ) );
		DrawBtn( hud, r3, "3", T( "shopmanager.button.casino.title", "Казик", "Casino" ), T( "shopmanager.button.casino.desc", "ДЕПНУТЬ 🎰", "PLACE A BET 🎰" ) );

		// мышка-клик
		Vector2 m = Mouse.Position;
		if ( Input.Pressed( "attack1" ) )
		{
			if ( RectContains( r1, m ) ) UpgradeMenu?.Open();
			else if ( RectContains( r2, m ) ) SkillsMenu?.Open();
			else if ( RectContains( r3, m ) ) CasinoMenu?.Open();

			_blockCloseUntilReal = RealTime.Now + UseDebounceSeconds;
		}

		hud.DrawText( new TextRendering.Scope( T( "shopmanager.close_hint", "ESC / E — закрыть", "ESC / E — close" ), new Color( 1, 1, 1, 0.55f ), 18f ),
			new Rect( px, py + PanelHeight - 54, PanelWidth, 24 ), TextFlag.Center );
	}


	private string T( string key, string russianFallback, string englishFallback )
	{
		GameLocalization.EnsureLoaded();
		return GameLocalization.T( key, GameLocalization.IsLanguage( "ru" ) ? russianFallback : englishFallback );
	}

	private void DrawBtn( HudPainter hud, Rect r, string key, string title, string desc )
	{
		Vector2 m = Mouse.Position;
		bool hover = RectContains( r, m );

		var bg = hover ? new Color( 0.12f, 0.16f, 0.22f, 0.95f ) : new Color( 0.10f, 0.12f, 0.16f, 0.92f );
		var br = hover ? new Color( 0.25f, 0.95f, 1.0f, 0.55f ) : new Color( 1f, 1f, 1f, 0.10f );

		DrawRoundedRect( hud, r.Left, r.Top, r.Width, r.Height, bg, 16f, 2f, br );

		hud.DrawText( new TextRendering.Scope( key, new Color( 1, 1, 1, 0.75f ), 18f ),
			new Rect( r.Left, r.Top + 12, r.Width, 26 ), TextFlag.Center );

		hud.DrawText( new TextRendering.Scope( title, new Color( 1, 1, 1, 0.95f ), 24f ),
			new Rect( r.Left, r.Top + 42, r.Width, 34 ), TextFlag.Center );

		hud.DrawText( new TextRendering.Scope( desc, new Color( 1, 1, 1, 0.65f ), 16f ),
			new Rect( r.Left + 10, r.Top + 82, r.Width - 20, 54 ), TextFlag.Center );
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
}
