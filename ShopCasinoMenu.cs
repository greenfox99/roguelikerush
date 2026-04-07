using Sandbox;
using Sandbox.Rendering;
using System;
using System.Linq;

public sealed class ShopCasinoMenu : Component
{
	// refs
	[Property] public PlayerStats PlayerStats { get; set; }
	[Property] public PlayerHealth PlayerHealth { get; set; }
	[Property] public ShopUpgradeMenu UpgradeMenu { get; set; }

	// sounds (optional)
	[Property] public SoundEvent TickSound { get; set; }
	[Property] public SoundEvent WinSound { get; set; }
	[Property] public SoundEvent LoseSound { get; set; }

	// UI
	[Property] public float OverlayAlpha { get; set; } = 0.62f;
	[Property] public float PanelWidth { get; set; } = 980f;
	[Property] public float PanelHeight { get; set; } = 560f;

	// bet
	[Property] public int MinBet { get; set; } = 1;

	// spin timing
	[Property] public float SpinDuration { get; set; } = 1.75f;

	// reel animation (красиво)
	[Property] public float SpinMinTick { get; set; } = 0.028f; // старт очень быстро
	[Property] public float SpinMaxTick { get; set; } = 0.18f;  // финиш медленно
	[Property] public float SpinEasePower { get; set; } = 2.5f; // как резко тормозит
	[Property] public float ReelRowHeight { get; set; } = 30f;  // расстояние между строками
	[Property] public float TickPulseStrength { get; set; } = 0.10f;
	[Property] public float TickPulseDecay { get; set; } = 12f;
	[Property] public float ReelShakePixels { get; set; } = 6f;
	[Property] public float ResultFlashSeconds { get; set; } = 0.25f;

	// casino outcomes probabilities (редактируешь в инспекторе)
	[Property] public float ChanceX12 { get; set; } = 0.45f;
	[Property] public float ChanceX15 { get; set; } = 0.25f;
	[Property] public float ChanceX20 { get; set; } = 0.10f;
	[Property] public float ChanceLoseHp { get; set; } = 0.15f;
	[Property] public float ChanceLoseUpgrade { get; set; } = 0.05f;

	// punish
	[Property] public float LoseHpAmount { get; set; } = 25f;

	// state
	private bool _isOpen;
	public bool IsOpen => _isOpen;

	private int _bet = 50;

	private bool _spinning;
	private float _spinStartReal;
	private float _nextTickReal;
	private float _lastTickReal;
	private float _currentTickInterval;

	private float _tickPulseReal;
	private float _flashUntilReal;

	private string _resultText = "";
	private Color _resultColor = new Color( 1, 1, 1, 0.85f );

	private Random _rng;

	// stolen upgrade animation
	private bool _stolenAnimActive;
	private float _stolenAnimStartReal;
	private float _stolenAnimDuration = 0.85f;
	private string _stolenUpgradeName = "";

	// reel
	private static readonly string[] ReelSymbols =
	{
		"✨ x1.2",
		"🔥 x1.5",
		"💎 x2.0",
		"💀 -HP",
		"🦝 -апгрейд",
		"😵 0"
	};

	private int _reelIndex;

	protected override void OnStart()
	{
		_rng = new Random( (int)(Time.Now * 1000f) ^ GameObject.Id.GetHashCode() );

		PlayerStats ??= Components.Get<PlayerStats>() ?? Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		PlayerHealth ??= Scene.GetAllComponents<PlayerHealth>().FirstOrDefault();
		UpgradeMenu ??= Scene.GetAllComponents<ShopUpgradeMenu>().FirstOrDefault();

		_reelIndex = _rng.Next( 0, ReelSymbols.Length );
	}

	public void Open()
	{
		_isOpen = true;
		_spinning = false;
		_resultText = "";
	}

	public void Close()
	{
		_isOpen = false;
		_spinning = false;
	}

	protected override void OnUpdate()
	{
		if ( !_isOpen ) return;

		// clamp ставки
		if ( _bet < MinBet ) _bet = MinBet;

		if ( _spinning ) UpdateSpin();
		else HandleInput();

		Draw();
	}

	private void HandleInput()
	{
		if ( Input.Pressed( "attack1" ) )
		{
			Vector2 m = Mouse.Position;
			var ui = GetUiRects();

			if ( RectContains( ui.Minus100, m ) ) _bet = Math.Max( MinBet, _bet - 100 );
			if ( RectContains( ui.Minus10, m ) ) _bet = Math.Max( MinBet, _bet - 10 );
			if ( RectContains( ui.Plus10, m ) ) _bet += 10;
			if ( RectContains( ui.Plus100, m ) ) _bet += 100;

			if ( RectContains( ui.MaxBtn, m ) )
			{
				int coins = PlayerStats?.Coins ?? 0;
				_bet = Math.Max( MinBet, coins );
			}

			if ( RectContains( ui.SpinBtn, m ) )
				StartSpin();
		}
	}

	private void StartSpin()
	{
		if ( PlayerStats == null )
		{
			SetResult( "PlayerStats не найден 😵", new Color( 1f, 0.55f, 0.55f, 0.95f ) );
			return;
		}

		int coins = PlayerStats.Coins;
		if ( coins < MinBet )
		{
			SetResult( "Ты нищий для казика 😭🪙", new Color( 1f, 0.55f, 0.55f, 0.95f ) );
			return;
		}

		if ( _bet > coins ) _bet = coins;

		// списываем ставку (ставка — риск)
		if ( !PlayerStats.TrySpendCoins( _bet ) )
		{
			SetResult( "Не смог списать ставку (TrySpendCoins?)", new Color( 1f, 0.55f, 0.55f, 0.95f ) );
			return;
		}

		_spinning = true;
		_spinStartReal = RealTime.Now;

		_reelIndex = _rng.Next( 0, ReelSymbols.Length );
		_lastTickReal = RealTime.Now;

		_currentTickInterval = SpinMinTick;
		_nextTickReal = RealTime.Now + _currentTickInterval;

		_tickPulseReal = RealTime.Now;
		_resultText = "";
	}

	private void UpdateSpin()
	{
		float now = RealTime.Now;

		float p = (now - _spinStartReal) / MathF.Max( 0.001f, SpinDuration );
		p = Clamp01( p );

		// быстро -> медленно (ease-out)
		float ease = EaseOutPow( p, SpinEasePower );
		_currentTickInterval = Lerp( SpinMinTick, SpinMaxTick, ease );

		if ( now >= _nextTickReal )
		{
			_reelIndex = (_reelIndex + 1) % ReelSymbols.Length;

			_lastTickReal = now;
			_tickPulseReal = now;

			_nextTickReal = now + _currentTickInterval;

			if ( TickSound != null )
				Sound.Play( TickSound, Scene.Camera?.Transform.Position ?? Transform.Position );
		}

		if ( p >= 1f )
		{
			_spinning = false;
			ResolveResult();
		}
	}

	private void ResolveResult()
	{
		// нормализуем шансы
		float sum = ChanceX12 + ChanceX15 + ChanceX20 + ChanceLoseHp + ChanceLoseUpgrade;
		if ( sum <= 0.0001f ) sum = 1f;

		float roll = (float)_rng.NextDouble() * sum;

		float a = 0f;

		a += ChanceX12;
		if ( roll < a ) { Win( 1.2f ); return; }

		a += ChanceX15;
		if ( roll < a ) { Win( 1.5f ); return; }

		a += ChanceX20;
		if ( roll < a ) { Win( 2.0f ); return; }

		a += ChanceLoseHp;
		if ( roll < a ) { LoseHp(); return; }

		a += ChanceLoseUpgrade;
		if ( roll < a ) { LoseUpgrade(); return; }

		// остаток
		ReelStopOn( "😵 0" );
		SetResult( $"😵 Проебал ставку {_bet}🪙", new Color( 1f, 0.6f, 0.6f, 0.95f ) );
		if ( LoseSound != null ) Sound.Play( LoseSound, Scene.Camera?.Transform.Position ?? Transform.Position );
	}

	private void Win( float mult )
	{
		// ставка уже списана, payout = bet * mult
		int payout = (int)MathF.Round( _bet * mult );
		PlayerStats.AddCoins( payout );

		if ( mult >= 2.0f ) ReelStopOn( "💎 x2.0" );
		else if ( mult >= 1.5f ) ReelStopOn( "🔥 x1.5" );
		else ReelStopOn( "✨ x1.2" );

		SetResult( $"🎉 ВЫИГРЫШ! x{mult:0.0} → +{payout}🪙", new Color( 0.7f, 1f, 0.7f, 0.95f ) );

		if ( WinSound != null )
			Sound.Play( WinSound, Scene.Camera?.Transform.Position ?? Transform.Position );
	}

	private void LoseHp()
	{
		ReelStopOn( "💀 -HP" );

		if ( PlayerHealth == null )
		{
			SetResult( $"💀 -{LoseHpAmount:0} HP (PlayerHealth не найден)", new Color( 1f, 0.55f, 0.55f, 0.95f ) );
			if ( LoseSound != null ) Sound.Play( LoseSound, Scene.Camera?.Transform.Position ?? Transform.Position );
			return;
		}

		PlayerHealth.TakeDamage( LoseHpAmount );
		SetResult( $"💀 Неудача: -{LoseHpAmount:0} HP", new Color( 1f, 0.55f, 0.55f, 0.95f ) );

		if ( LoseSound != null )
			Sound.Play( LoseSound, Scene.Camera?.Transform.Position ?? Transform.Position );
	}

	private void LoseUpgrade()
	{
		ReelStopOn( "🦝 -апгрейд" );

		// ✅ украдём только если реально есть у игрока (это проверяет UpgradeMenu)
		if ( UpgradeMenu != null && UpgradeMenu.TryStealRandomUpgrade( out var stolen ) )
		{
			StartStolenAnim( stolen );

			SetResult( $"🦝 Казик СПИЗДИЛ апгрейд: {stolen}", new Color( 1f, 0.75f, 0.45f, 0.95f ) );

			if ( LoseSound != null )
				Sound.Play( LoseSound, Scene.Camera?.Transform.Position ?? Transform.Position );

			return;
		}

		// нечего воровать — накажем HP
		LoseHp();
	}

	private void StartStolenAnim( string upgradeName )
	{
		_stolenAnimActive = true;
		_stolenAnimStartReal = RealTime.Now;
		_stolenUpgradeName = upgradeName ?? "???";
	}

	private void ReelStopOn( string symbol )
	{
		for ( int i = 0; i < ReelSymbols.Length; i++ )
		{
			if ( ReelSymbols[i] == symbol )
			{
				_reelIndex = i;
				return;
			}
		}
	}

	private void SetResult( string text, Color color )
	{
		_resultText = text;
		_resultColor = color;
		_flashUntilReal = RealTime.Now + ResultFlashSeconds;
	}

	// -------------------- DRAW --------------------

	private void Draw()
	{
		if ( Scene.Camera == null ) return;
		var hud = Scene.Camera.Hud;

		hud.DrawRect( new Rect( 0, 0, Screen.Width, Screen.Height ), new Color( 0, 0, 0, OverlayAlpha ) );

		float cx = Screen.Width * 0.5f;
		float cy = Screen.Height * 0.5f;

		float px = cx - PanelWidth * 0.5f;
		float py = cy - PanelHeight * 0.5f;

		DrawRoundedRect( hud, px, py, PanelWidth, PanelHeight, new Color( 0.08f, 0.10f, 0.13f, 0.92f ), 18f, 2f, new Color( 1f, 1f, 1f, 0.10f ) );

		DrawCenteredText( hud, "ДЕПНУТЬ В КАЗИК 🎰", new Rect( px, py + 18, PanelWidth, 52 ), new Color( 0.25f, 0.95f, 1.0f, 1f ), 38f );

		int coins = PlayerStats?.Coins ?? 0;
		DrawCenteredText( hud, $"🪙 Монет: {coins}", new Rect( px, py + 70, PanelWidth, 24 ), new Color( 1, 1, 1, 0.70f ), 18f );
		DrawCenteredText( hud, "ESC / E — назад", new Rect( px, py + PanelHeight - 46, PanelWidth, 24 ), new Color( 1, 1, 1, 0.55f ), 18f );

		var ui = GetUiRects();

		// slot reel
		DrawReel( hud, ui.Slot );

		// bet
		DrawCenteredText( hud, $"Ставка: {_bet} 🪙", new Rect( px, py + 250, PanelWidth, 26 ), new Color( 1, 1, 1, 0.85f ), 22f );

		// buttons
		DrawBtn( hud, ui.Minus100, "-100", !_spinning );
		DrawBtn( hud, ui.Minus10, "-10", !_spinning );
		DrawBtn( hud, ui.Plus10, "+10", !_spinning );
		DrawBtn( hud, ui.Plus100, "+100", !_spinning );
		DrawBtn( hud, ui.MaxBtn, "MAX", !_spinning );

		DrawBtn( hud, ui.SpinBtn, _spinning ? "КРУТИМ..." : "ДЕПНУТЬ", !_spinning );

		// result text
		if ( !string.IsNullOrEmpty( _resultText ) )
			DrawCenteredText( hud, _resultText, new Rect( px + 40, py + 410, PanelWidth - 80, 64 ), _resultColor, 20f );

		// stolen anim overlay
		DrawStolenAnim( hud, ui.Slot );
	}

	private void DrawReel( HudPainter hud, Rect slot )
	{
		float now = RealTime.Now;

		// background
		DrawRoundedRect( hud, slot.Left, slot.Top, slot.Width, slot.Height,
			new Color( 1, 1, 1, 0.04f ), 16f, 2f, new Color( 1, 1, 1, 0.10f ) );

		// shake
		float shake = _spinning ? ReelShakePixels : 0f;
		float sx = _spinning ? MathF.Sin( now * 38f ) * shake : 0f;
		float sy = _spinning ? MathF.Cos( now * 31f ) * shake : 0f;

		// tick pulse
		float pulse = MathF.Exp( -(now - _tickPulseReal) * TickPulseDecay );
		float midScale = 1f + TickPulseStrength * pulse;

		// smooth scroll between ticks
		float t = (now - _lastTickReal) / MathF.Max( 0.001f, _currentTickInterval );
		t = Clamp01( t );
		float scroll = EaseInPow( t, 2.2f ) * ReelRowHeight;

		int len = ReelSymbols.Length;
		int topI = (_reelIndex - 1 + len) % len;
		int midI = _reelIndex;
		int botI = (_reelIndex + 1) % len;

		float centerY = slot.Top + slot.Height * 0.5f + sy;

		// center band
		hud.DrawRect( new Rect( slot.Left + 12, centerY - 22, slot.Width - 24, 44 ), new Color( 1, 1, 1, 0.03f ) );

		// result flash
		if ( now < _flashUntilReal )
		{
			float start = _flashUntilReal - ResultFlashSeconds;
			float f = 1f - (now - start) / MathF.Max( 0.001f, ResultFlashSeconds );
			f = Clamp01( f );
			float a = 0.22f * f;
			DrawRoundedRect( hud, slot.Left, slot.Top, slot.Width, slot.Height, new Color( 1f, 1f, 1f, a ), 16f, 0f, new Color( 0, 0, 0, 0 ) );
		}

		// top
		DrawCenteredText( hud, ReelSymbols[topI],
			new Rect( slot.Left + sx, centerY - ReelRowHeight - scroll - 18, slot.Width, 40 ),
			new Color( 1, 1, 1, 0.35f ), 24f );

		// mid
		DrawCenteredText( hud, ReelSymbols[midI],
			new Rect( slot.Left + sx, centerY - scroll - 20, slot.Width, 44 ),
			new Color( 1, 1, 1, 0.95f ), 34f * midScale );

		// bottom
		DrawCenteredText( hud, ReelSymbols[botI],
			new Rect( slot.Left + sx, centerY + ReelRowHeight - scroll - 18, slot.Width, 40 ),
			new Color( 1, 1, 1, 0.35f ), 24f );
	}

	private void DrawStolenAnim( HudPainter hud, Rect slot )
	{
		if ( !_stolenAnimActive ) return;

		float now = RealTime.Now;
		float t = (now - _stolenAnimStartReal) / MathF.Max( 0.001f, _stolenAnimDuration );

		if ( t >= 1f )
		{
			_stolenAnimActive = false;
			return;
		}

		t = Clamp01( t );

		float moveEase = EaseOutPow( t, 2.2f );
		float fade = 1f - EaseInPow( t, 1.6f );

		float sx = slot.Left + slot.Width * 0.5f;
		float sy = slot.Top + slot.Height * 0.5f;

		float ex = sx + 260f;
		float ey = sy - 180f;

		float x = Lerp( sx, ex, moveEase );
		float y = Lerp( sy, ey, moveEase );

		float shake = (1f - t) * 10f;
		x += MathF.Sin( now * 60f ) * shake;
		y += MathF.Cos( now * 55f ) * shake;

		float w = Lerp( 440f, 260f, moveEase );
		float h = Lerp( 92f, 70f, moveEase );

		var r = new Rect( x - w * 0.5f, y - h * 0.5f, w, h );

		var bg = new Color( 0.10f, 0.12f, 0.16f, 0.90f * fade );
		var border = new Color( 1f, 0.75f, 0.25f, 0.55f * fade );

		DrawRoundedRect( hud, r.Left, r.Top, r.Width, r.Height, bg, 14f, 2f, border );

		DrawCenteredText( hud, "🦝", new Rect( r.Left + 8, r.Top + 8, 60, r.Height - 16 ), new Color( 1f, 1f, 1f, 0.95f * fade ), 36f );

		DrawCenteredText(
			hud,
			$"Украдено: {_stolenUpgradeName}",
			new Rect( r.Left + 62, r.Top + 10, r.Width - 72, r.Height - 20 ),
			new Color( 1f, 1f, 1f, 0.92f * fade ),
			18f
		);

		float flash = (t < 0.12f) ? (1f - t / 0.12f) : 0f;
		if ( flash > 0f )
		{
			float a = 0.18f * flash * fade;
			DrawRoundedRect( hud, r.Left, r.Top, r.Width, r.Height, new Color( 1f, 1f, 1f, a ), 14f, 0f, new Color( 0, 0, 0, 0 ) );
		}
	}

	private void DrawBtn( HudPainter hud, Rect r, string text, bool enabled )
	{
		Vector2 m = Mouse.Position;
		bool hover = RectContains( r, m );

		Color bg =
			!enabled ? new Color( 0.25f, 0.25f, 0.25f, 0.70f ) :
			hover ? new Color( 0.18f, 0.55f, 0.22f, 0.90f ) :
					new Color( 0.10f, 0.12f, 0.16f, 0.80f );

		DrawRoundedRect( hud, r.Left, r.Top, r.Width, r.Height, bg, 12f, 2f, new Color( 1f, 1f, 1f, 0.10f ) );
		DrawCenteredText( hud, text, r, new Color( 1f, 1f, 1f, 0.95f ), 18f );
	}

	private (Rect Slot, Rect Minus100, Rect Minus10, Rect Plus10, Rect Plus100, Rect MaxBtn, Rect SpinBtn) GetUiRects()
	{
		float cx = Screen.Width * 0.5f;
		float cy = Screen.Height * 0.5f;

		float px = cx - PanelWidth * 0.5f;
		float py = cy - PanelHeight * 0.5f;

		var slot = new Rect( px + 180, py + 130, PanelWidth - 360, 90 );

		float y = py + 290;
		var m100 = new Rect( px + 170, y, 90, 44 );
		var m10 = new Rect( px + 270, y, 90, 44 );
		var p10 = new Rect( px + 370, y, 90, 44 );
		var p100 = new Rect( px + 470, y, 90, 44 );
		var max = new Rect( px + 570, y, 90, 44 );

		var spin = new Rect( px + 300, py + 350, PanelWidth - 600, 52 );

		return (slot, m100, m10, p10, p100, max, spin);
	}

	// -------------------- helpers --------------------

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

	private static void DrawCenteredText( HudPainter hud, string text, Rect rect, Color color, float size )
	{
		var scope = new TextRendering.Scope( text ?? "", color, size );
		hud.DrawText( scope, rect, TextFlag.Center );
	}

	private static float Clamp01( float v ) => v < 0 ? 0 : (v > 1 ? 1 : v);
	private static float Lerp( float a, float b, float t ) => a + (b - a) * Clamp01( t );

	private static float EaseOutPow( float t, float pow )
	{
		t = Clamp01( t );
		return 1f - MathF.Pow( 1f - t, pow );
	}

	private static float EaseInPow( float t, float pow )
	{
		t = Clamp01( t );
		return MathF.Pow( t, pow );
	}
}
