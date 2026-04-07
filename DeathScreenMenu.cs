using Sandbox;
using Sandbox.UI;
using System;
using System.Globalization;
using System.Linq;

namespace YourGame.UI;

public sealed class DeathScreenMenu : PanelComponent
{
	[Property, Title( "Death Screen Image" ), ImageAssetPath]
	public string BackgroundImage { get; set; }

	[Property, Title( "Retry Scene Path" )]
	public string RetryScenePath { get; set; } = "scenes/alpha1.scene";

	[Property, Title( "Main Menu Scene Path" )]
	public string MainMenuScenePath { get; set; } = "main_menu.scene";

	[Property, Title( "Death Screen Music" )]
	public SoundEvent DeathScreenMusic { get; set; }

	[Property]
	public bool BlockEscape { get; set; } = true;

	[Property]
	public bool ForceMouseVisible { get; set; } = true;

	[Property, Title( "Show Debug Button Bounds" )]
	public bool ShowDebugButtonBounds { get; set; } = false;

	[Property, Group( "Opening Animation" )]
	public float FadeInDuration { get; set; } = 0.55f;

	[Property, Group( "Opening Animation" )]
	public float StartScale { get; set; } = 1.06f;

	[Property, Group( "Opening Animation" )]
	public float StartFlashAlpha { get; set; } = 0.22f;

	[Property, Group( "Opening Animation" )]
	public Color FlashColor { get; set; } = new Color( 0.85f, 0.10f, 0.10f, 1f );

	[Property, Group( "Retry Button" )]
	public float RetryButtonLeftPercent { get; set; } = 30.2f;

	[Property, Group( "Retry Button" )]
	public float RetryButtonTopPercent { get; set; } = 48.9f;

	[Property, Group( "Retry Button" )]
	public float RetryButtonWidthPercent { get; set; } = 39.2f;

	[Property, Group( "Retry Button" )]
	public float RetryButtonHeightPercent { get; set; } = 8.1f;

	[Property, Group( "Main Menu Button" )]
	public float MainMenuButtonLeftPercent { get; set; } = 30.2f;

	[Property, Group( "Main Menu Button" )]
	public float MainMenuButtonTopPercent { get; set; } = 61.0f;

	[Property, Group( "Main Menu Button" )]
	public float MainMenuButtonWidthPercent { get; set; } = 39.2f;

	[Property, Group( "Main Menu Button" )]
	public float MainMenuButtonHeightPercent { get; set; } = 8.1f;

	[Property, Group( "Debug" )]
	public Color DebugRetryBorderColor { get; set; } = new Color( 0.1f, 1.0f, 0.1f, 0.95f );

	[Property, Group( "Debug" )]
	public Color DebugRetryFillColor { get; set; } = new Color( 0.1f, 1.0f, 0.1f, 0.10f );

	[Property, Group( "Debug" )]
	public Color DebugMenuBorderColor { get; set; } = new Color( 1.0f, 0.35f, 0.15f, 0.95f );

	[Property, Group( "Debug" )]
	public Color DebugMenuFillColor { get; set; } = new Color( 1.0f, 0.35f, 0.15f, 0.10f );

	private Panel _background;
	private Panel _flashOverlay;
	private Panel _retryButton;
	private Panel _mainMenuButton;
	private Panel _retryDebug;
	private Panel _menuDebug;

	private bool _visible;
	private bool _isOpening;
	private float _openStartReal;
	private float _oldTimeScale = 1f;

	private string _lastBackground;
	private SoundHandle _deathMusicHandle;

	protected override void OnTreeFirstBuilt()
	{
		base.OnTreeFirstBuilt();

		BuildUi();
		ApplyBackground();
		ApplyButtonLayouts();
		ApplyDebugState();
		SetVisible( false );
		ApplyOpeningVisual( 1f );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		ApplyBackground();
		ApplyButtonLayouts();
		ApplyDebugState();

		if ( !_visible )
			return;

		if ( BlockEscape && Input.EscapePressed )
			Input.EscapePressed = false;

		if ( ForceMouseVisible )
			Mouse.Visibility = MouseVisibility.Visible;

		UpdateOpeningAnimation();
	}

	protected override void OnDisabled()
	{
		ForceHideState();
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		ForceHideState();
		base.OnDestroy();
	}

	public void OpenDeathScreen()
	{
		if ( _visible )
			return;

		var gameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		gameManager?.StopMusicForDeath();

		_oldTimeScale = Scene.TimeScale;
		Scene.TimeScale = 0f;

		SetVisible( true );

		_isOpening = true;
		_openStartReal = RealTime.Now;
		ApplyOpeningVisual( 0f );

		PlayDeathScreenMusic();
	}

	public void CloseDeathScreen()
	{
		if ( !_visible )
			return;

		StopDeathScreenMusic();
		Scene.TimeScale = _oldTimeScale;
		SetVisible( false );
	}

	private void BuildUi()
	{
		Panel.Style.Set( "width", "100%" );
		Panel.Style.Set( "height", "100%" );
		Panel.Style.Set( "display", "none" );
		Panel.Style.Set( "pointer-events", "none" );
		Panel.Style.Dirty();

		_background = new Panel { Parent = Panel };
		_background.Style.Set( "position", "absolute" );
		_background.Style.Set( "left", "0" );
		_background.Style.Set( "top", "0" );
		_background.Style.Set( "width", "100%" );
		_background.Style.Set( "height", "100%" );
		_background.Style.Set( "background-repeat", "no-repeat" );
		_background.Style.Set( "background-position", "0 0" );
		_background.Style.Set( "background-size", "100% 100%" );
		_background.Style.Set( "transform-origin", "50% 50%" );
		_background.Style.Set( "pointer-events", "none" );
		_background.Style.Dirty();

		_flashOverlay = new Panel { Parent = Panel };
		_flashOverlay.Style.Set( "position", "absolute" );
		_flashOverlay.Style.Set( "left", "0" );
		_flashOverlay.Style.Set( "top", "0" );
		_flashOverlay.Style.Set( "width", "100%" );
		_flashOverlay.Style.Set( "height", "100%" );
		_flashOverlay.Style.Set( "background-color", Rgba( FlashColor ) );
		_flashOverlay.Style.Set( "pointer-events", "none" );
		_flashOverlay.Style.Set( "z-index", "5" );
		_flashOverlay.Style.Dirty();

		_retryButton = new Panel { Parent = Panel };
		_retryButton.Style.Set( "position", "absolute" );
		_retryButton.Style.Set( "background-color", "rgba(255,255,255,0.001)" );
		_retryButton.Style.Set( "pointer-events", "all" );
		_retryButton.Style.Set( "z-index", "20" );
		_retryButton.Style.Dirty();
		_retryButton.AddEventListener( "onclick", OnRetryClicked );

		_mainMenuButton = new Panel { Parent = Panel };
		_mainMenuButton.Style.Set( "position", "absolute" );
		_mainMenuButton.Style.Set( "background-color", "rgba(255,255,255,0.001)" );
		_mainMenuButton.Style.Set( "pointer-events", "all" );
		_mainMenuButton.Style.Set( "z-index", "20" );
		_mainMenuButton.Style.Dirty();
		_mainMenuButton.AddEventListener( "onclick", OnMainMenuClicked );

		_retryDebug = new Panel { Parent = Panel };
		_retryDebug.Style.Set( "position", "absolute" );
		_retryDebug.Style.Set( "pointer-events", "none" );
		_retryDebug.Style.Set( "z-index", "30" );
		_retryDebug.Style.Dirty();

		_menuDebug = new Panel { Parent = Panel };
		_menuDebug.Style.Set( "position", "absolute" );
		_menuDebug.Style.Set( "pointer-events", "none" );
		_menuDebug.Style.Set( "z-index", "30" );
		_menuDebug.Style.Dirty();
	}

	private void OnRetryClicked()
	{
		StopDeathScreenMusic();
		Scene.TimeScale = 1f;
		Mouse.Visibility = MouseVisibility.Hidden;

		string path = NormalizeScenePath( RetryScenePath );
		if ( string.IsNullOrWhiteSpace( path ) )
		{
			Log.Warning( "RetryScenePath is empty." );
			return;
		}

		Log.Info( $"DeathScreen: retry loading '{path}'" );
		Scene.LoadFromFile( path );
	}

	private void OnMainMenuClicked()
	{
		StopDeathScreenMusic();
		Scene.TimeScale = 1f;
		Mouse.Visibility = MouseVisibility.Hidden;

		string path = NormalizeScenePath( MainMenuScenePath );
		if ( string.IsNullOrWhiteSpace( path ) )
		{
			Log.Warning( "MainMenuScenePath is empty." );
			return;
		}

		Log.Info( $"DeathScreen: main menu loading '{path}'" );
		Scene.LoadFromFile( path );
	}

	private void PlayDeathScreenMusic()
	{
		StopDeathScreenMusic();

		if ( DeathScreenMusic == null )
			return;

		_deathMusicHandle = Sound.Play( DeathScreenMusic, 0f );
	}

	private void StopDeathScreenMusic()
	{
		if ( _deathMusicHandle != null )
		{
			_deathMusicHandle.Stop();
			_deathMusicHandle = null;
		}
	}

	private string NormalizeScenePath( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
			return null;

		path = path.Trim().Replace( "\\", "/" );

		int assetsIndex = path.IndexOf( "/Assets/", StringComparison.OrdinalIgnoreCase );
		if ( assetsIndex >= 0 )
			path = path.Substring( assetsIndex + "/Assets/".Length );

		if ( path.StartsWith( "Assets/", StringComparison.OrdinalIgnoreCase ) )
			path = path.Substring( "Assets/".Length );

		if ( path.EndsWith( ".scene_c", StringComparison.OrdinalIgnoreCase ) ||
			 path.EndsWith( ".scene_d", StringComparison.OrdinalIgnoreCase ) )
		{
			int dotIndex = path.LastIndexOf( '.' );
			if ( dotIndex > 0 )
				path = path.Substring( 0, dotIndex );
		}

		if ( !path.EndsWith( ".scene", StringComparison.OrdinalIgnoreCase ) )
			path += ".scene";

		return path.ToLowerInvariant();
	}

	private void ApplyBackground()
	{
		if ( _background is null )
			return;

		if ( _lastBackground == BackgroundImage )
			return;

		_lastBackground = BackgroundImage;

		if ( string.IsNullOrWhiteSpace( BackgroundImage ) )
			_background.Style.Set( "background-image", "none" );
		else
			_background.Style.SetBackgroundImage( BackgroundImage );

		_background.Style.Dirty();
	}

	private void ApplyButtonLayouts()
	{
		ApplyRect( _retryButton, RetryButtonLeftPercent, RetryButtonTopPercent, RetryButtonWidthPercent, RetryButtonHeightPercent );
		ApplyRect( _mainMenuButton, MainMenuButtonLeftPercent, MainMenuButtonTopPercent, MainMenuButtonWidthPercent, MainMenuButtonHeightPercent );
		ApplyRect( _retryDebug, RetryButtonLeftPercent, RetryButtonTopPercent, RetryButtonWidthPercent, RetryButtonHeightPercent );
		ApplyRect( _menuDebug, MainMenuButtonLeftPercent, MainMenuButtonTopPercent, MainMenuButtonWidthPercent, MainMenuButtonHeightPercent );
	}

	private void ApplyDebugState()
	{
		if ( _retryDebug != null )
		{
			_retryDebug.Style.Set( "display", ShowDebugButtonBounds ? "flex" : "none" );
			_retryDebug.Style.Set( "background-color", Rgba( DebugRetryFillColor ) );
			_retryDebug.Style.Set( "border-width", "2px" );
			_retryDebug.Style.Set( "border-color", Rgba( DebugRetryBorderColor ) );
			_retryDebug.Style.Set( "border-radius", "8px" );
			_retryDebug.Style.Dirty();
		}

		if ( _menuDebug != null )
		{
			_menuDebug.Style.Set( "display", ShowDebugButtonBounds ? "flex" : "none" );
			_menuDebug.Style.Set( "background-color", Rgba( DebugMenuFillColor ) );
			_menuDebug.Style.Set( "border-width", "2px" );
			_menuDebug.Style.Set( "border-color", Rgba( DebugMenuBorderColor ) );
			_menuDebug.Style.Set( "border-radius", "8px" );
			_menuDebug.Style.Dirty();
		}
	}

	private void UpdateOpeningAnimation()
	{
		if ( !_isOpening )
			return;

		float duration = MathF.Max( 0.01f, FadeInDuration );
		float t = (RealTime.Now - _openStartReal) / duration;

		if ( t >= 1f )
		{
			_isOpening = false;
			ApplyOpeningVisual( 1f );
			return;
		}

		ApplyOpeningVisual( Clamp01( t ) );
	}

	private void ApplyOpeningVisual( float t )
	{
		float eased = EaseOutCubic( t );
		float scale = Lerp( StartScale, 1f, eased );
		float opacity = eased;
		float flash = Lerp( StartFlashAlpha, 0f, eased * eased );

		if ( _background != null )
		{
			_background.Style.Set( "opacity", FloatString( opacity ) );
			_background.Style.Set( "transform", $"scale({FloatString( scale )})" );
			_background.Style.Dirty();
		}

		if ( _flashOverlay != null )
		{
			_flashOverlay.Style.Set( "opacity", FloatString( flash ) );
			_flashOverlay.Style.Dirty();
		}
	}

	private void SetVisible( bool visible )
	{
		_visible = visible;

		Panel.Style.Set( "display", visible ? "flex" : "none" );
		Panel.Style.Set( "pointer-events", visible ? "all" : "none" );
		Panel.Style.Dirty();

		if ( ForceMouseVisible )
			Mouse.Visibility = visible ? MouseVisibility.Visible : MouseVisibility.Hidden;
	}

	private void ForceHideState()
	{
		StopDeathScreenMusic();

		_visible = false;
		_isOpening = false;

		if ( Panel != null )
		{
			Panel.Style.Set( "display", "none" );
			Panel.Style.Set( "pointer-events", "none" );
			Panel.Style.Dirty();
		}

		if ( _background != null )
		{
			_background.Style.Set( "opacity", "1" );
			_background.Style.Set( "transform", "scale(1)" );
			_background.Style.Dirty();
		}

		if ( _flashOverlay != null )
		{
			_flashOverlay.Style.Set( "opacity", "0" );
			_flashOverlay.Style.Dirty();
		}

		if ( ForceMouseVisible )
			Mouse.Visibility = MouseVisibility.Hidden;
	}

	private static void ApplyRect( Panel panel, float left, float top, float width, float height )
	{
		if ( panel is null )
			return;

		panel.Style.Set( "left", Percent( left ) );
		panel.Style.Set( "top", Percent( top ) );
		panel.Style.Set( "width", Percent( width ) );
		panel.Style.Set( "height", Percent( height ) );
		panel.Style.Dirty();
	}

	private static float Clamp01( float value )
	{
		if ( value < 0f ) return 0f;
		if ( value > 1f ) return 1f;
		return value;
	}

	private static float Lerp( float a, float b, float t )
	{
		t = Clamp01( t );
		return a + (b - a) * t;
	}

	private static float EaseOutCubic( float t )
	{
		t = Clamp01( t );
		float inv = 1f - t;
		return 1f - inv * inv * inv;
	}

	private static string Percent( float value )
	{
		return value.ToString( "0.###", CultureInfo.InvariantCulture ) + "%";
	}

	private static string FloatString( float value )
	{
		return value.ToString( "0.###", CultureInfo.InvariantCulture );
	}

	private static string Rgba( Color c )
	{
		int r = (int)(c.r * 255f);
		int g = (int)(c.g * 255f);
		int b = (int)(c.b * 255f);
		return $"rgba({r},{g},{b},{c.a.ToString( "0.###", CultureInfo.InvariantCulture )})";
	}
}
