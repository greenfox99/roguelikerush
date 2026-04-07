// GameHUD.cs
using Sandbox;
using Sandbox.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using YourGame.UI;

public sealed class GameHUD : Component
{
	// =========================
	// SCALE (главное)
	// =========================
	[Property, Group( "Scale" )] public float LeftHudScale { get; set; } = 1.45f;
	[Property, Group( "Scale" )] public float RightHudScale { get; set; } = 1.45f;
	[Property, Group( "Scale" )] public float CenterHudScale { get; set; } = 1.10f;

	[Property, Group( "Scale" )] public bool AutoStackLeftPanels { get; set; } = true;
	[Property, Group( "Scale" )] public float LeftStackGap { get; set; } = 12f;

	// ===== АНКЕРЫ / ПОЗИЦИИ =====
	[Property] public Vector2 HPPosition { get; set; } = new Vector2( 20, 20 );
	[Property] public Vector2 LevelPosition { get; set; } = new Vector2( 20, 85 );
	[Property] public Vector2 StatsKillsPosition { get; set; } = new Vector2( 20, 150 );

	[Property] public Vector2 AbilitiesOffsetFromRightTop { get; set; } = new Vector2( 20, 20 );

	// ===== РАЗМЕРЫ ПАНЕЛЕЙ =====
	[Property] public float HPPanelWidth { get; set; } = 360f;
	[Property] public float HPPanelHeight { get; set; } = 54f;

	[Property] public float LevelPanelWidth { get; set; } = 360f;
	[Property] public float LevelPanelHeight { get; set; } = 56f;

	[Property] public float KillsPanelWidth { get; set; } = 220f;
	[Property] public float KillsPanelHeight { get; set; } = 40f;

	[Property, Group( "Kills" )] public Texture KillsIconTexture { get; set; }
	[Property, Group( "Kills" )] public float KillsIconSize { get; set; } = 26f;
	[Property, Group( "Kills" )] public Vector2 KillsIconOffset { get; set; } = new Vector2( 10f, 7f );
	[Property, Group( "Kills" )] public float KillsTextGap { get; set; } = 10f;

	[Property] public float CoinsPanelWidth { get; set; } = 220f;
	[Property] public float CoinsPanelHeight { get; set; } = 40f;
	[Property] public float CoinsGap { get; set; } = 12f;

	[Property, Group( "Magnet Widget" )] public bool MagnetWidgetEnabled { get; set; } = true;
	[Property, Group( "Magnet Widget" )] public float MagnetWidgetHeight { get; set; } = 48f;
	[Property, Group( "Magnet Widget" )] public float MagnetWidgetGap { get; set; } = 10f;
	[Property, Group( "Magnet Widget" )] public Color MagnetWidgetFillReady { get; set; } = new Color( 0.45f, 0.85f, 1.00f, 0.95f );
	[Property, Group( "Magnet Widget" )] public Color MagnetWidgetFillActive { get; set; } = new Color( 0.35f, 1.00f, 0.65f, 0.98f );
	[Property, Group( "Magnet Widget" )] public Color MagnetWidgetBarBg { get; set; } = new Color( 1f, 1f, 1f, 0.12f );

	[Property] public float AbilitiesPanelWidth { get; set; } = 340f;

	// ===== ВИЗУАЛ =====
	[Property] public float CornerRadius { get; set; } = 12f;
	[Property] public float ShadowOffset { get; set; } = 2f;
	[Property] public float BorderWidth { get; set; } = 1.5f;

	[Property] public Color PanelBg { get; set; } = new Color( 0f, 0f, 0f, 0.55f );
	[Property] public Color PanelShadow { get; set; } = new Color( 0f, 0f, 0f, 0.35f );
	[Property] public Color BorderColor { get; set; } = new Color( 1f, 1f, 1f, 0.08f );

	[Property] public Color HpBarBg { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property] public Color HpBarFillGood { get; set; } = new Color( 0.20f, 0.95f, 0.35f, 0.95f );
	[Property] public Color HpBarFillMid { get; set; } = new Color( 1.00f, 0.80f, 0.15f, 0.95f );
	[Property] public Color HpBarFillLow { get; set; } = new Color( 1.00f, 0.25f, 0.25f, 0.95f );

	[Property] public Color ExpBarBg { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property] public Color ExpBarFill { get; set; } = new Color( 0.35f, 0.85f, 1.00f, 0.95f );

	[Property] public Color TitleColor { get; set; } = new Color( 0.75f, 0.95f, 1f, 0.95f );
	[Property] public Color TextColor { get; set; } = new Color( 1f, 1f, 1f, 0.95f );
	[Property] public Color MutedText { get; set; } = new Color( 1f, 1f, 1f, 0.65f );

	[Property] public float BigTextSize { get; set; } = 22f;
	[Property] public float TextSize { get; set; } = 18f;
	[Property] public float SmallTextSize { get; set; } = 14f;

	// =========================
	// SHIELD WIDGET
	// =========================
	[Property, Group( "Shield Widget" )] public bool ShieldWidgetEnabled { get; set; } = true;
	[Property, Group( "Shield Widget" )] public float ShieldWidgetGap { get; set; } = 10f;
	[Property, Group( "Shield Widget" )] public float ShieldWidgetScale { get; set; } = 1.00f;

	[Property, Group( "Shield Widget" )] public Color ShieldWidgetBg { get; set; } = new Color( 0f, 0f, 0f, 0.55f );
	[Property, Group( "Shield Widget" )] public Color ShieldWidgetBorder { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property, Group( "Shield Widget" )] public Color ShieldRingColor { get; set; } = new Color( 0.35f, 0.85f, 1.00f, 0.95f );

	[Property, Group( "Shield FX" )] public float ShieldHitFlashTime { get; set; } = 0.25f;
	[Property, Group( "Shield FX" )] public float ShieldGlowStrength { get; set; } = 0.30f;
	[Property, Group( "Shield FX" )] public float ShieldRegenPulseSpeed { get; set; } = 4.0f;

	[Property, Group( "Shield FX" )] public float ShieldRingThickness { get; set; } = 4.0f;
	[Property, Group( "Shield FX" )] public float ShieldGlowThickness { get; set; } = 7.0f;

	[Property, Group( "Shield FX" )] public float ShieldRotationSpeed { get; set; } = 0.80f;
	[Property, Group( "Shield FX" )] public float ShieldElectricChance { get; set; } = 0.35f;
	[Property, Group( "Shield FX" )] public float ShieldElectricIntensity { get; set; } = 1.0f;

	[Property, Group( "Shield FX" )] public float ShieldBreakTime { get; set; } = 0.35f;
	[Property, Group( "Shield FX" )] public float ShieldBreakBurstStrength { get; set; } = 1.0f;

	private float _lastShield = -1f;
	private float _shieldHitTimer = 0f;
	private float _shieldRegenPulse = 0f;

	private float _shieldRotation = 0f;
	private float _shieldBreakTimer = 0f;

	private Random _rng;

	// ===== ЦЕНТРАЛЬНЫЕ СООБЩЕНИЯ =====
	[Property] public float CenterTextSize { get; set; } = 38f;
	[Property] public Color WaitColor { get; set; } = new Color( 1, 1, 0, 1 );
	[Property] public Color PrepareColor { get; set; } = Color.Yellow;
	[Property] public Color FightColor { get; set; } = Color.Red;

	// ===== БАННЕР ЦЕНТРА =====
	[Property] public float CenterBannerWidth { get; set; } = 560f;
	[Property] public float CenterBannerTopOffset { get; set; } = 110f;
	[Property] public float CenterBannerHeight { get; set; } = 86f;
	[Property] public float CenterBannerRadius { get; set; } = 16f;
	[Property] public float CenterBannerBorder { get; set; } = 2f;
	[Property] public Color CenterBannerBg { get; set; } = new Color( 0f, 0f, 0f, 0.40f );
	[Property] public Color CenterBannerBorderColor { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property] public Color CenterBannerShadow { get; set; } = new Color( 0f, 0f, 0f, 0.35f );

	// =========================
	// SHOP EXIT PROTECTION
	// =========================
	[Property, Group( "Shop Exit Protection" )] public bool ShopExitProtectionHudEnabled { get; set; } = true;
	[Property, Group( "Shop Exit Protection" )] public float ShopExitProtectionWidth { get; set; } = 540f;
	[Property, Group( "Shop Exit Protection" )] public float ShopExitProtectionHeight { get; set; } = 110f;
	[Property, Group( "Shop Exit Protection" )] public float ShopExitProtectionCountdownSize { get; set; } = 42f;
	[Property, Group( "Shop Exit Protection" )] public float ShopExitProtectionTextSize { get; set; } = 20f;
	[Property, Group( "Shop Exit Protection" )] public Color ShopExitProtectionBg { get; set; } = new Color( 0.06f, 0.10f, 0.18f, 0.78f );
	[Property, Group( "Shop Exit Protection" )] public Color ShopExitProtectionBorder { get; set; } = new Color( 0.45f, 0.85f, 1.00f, 0.35f );
	[Property, Group( "Shop Exit Protection" )] public Color ShopExitProtectionTitleColor { get; set; } = new Color( 0.78f, 0.95f, 1.00f, 0.96f );
	[Property, Group( "Shop Exit Protection" )] public Color ShopExitProtectionCountdownColor { get; set; } = new Color( 1.00f, 0.95f, 0.70f, 0.98f );
	[Property, Group( "Shop Exit Protection" )] public Color ShopExitProtectionTextColor { get; set; } = new Color( 1f, 1f, 1f, 0.82f );
	[Property, Group( "Demon Warning" )] public bool DemonWarningEnabled { get; set; } = true;
	[Property, Group( "Demon Warning" )] public float DemonWarningTopGap { get; set; } = 10f;
	[Property, Group( "Demon Warning" )] public float DemonWarningWidth { get; set; } = 760f;
	[Property, Group( "Demon Warning" )] public float DemonWarningHeight { get; set; } = 44f;
	[Property, Group( "Demon Warning" )] public float DemonWarningRadius { get; set; } = 14f;
	[Property, Group( "Demon Warning" )] public float DemonWarningTextSize { get; set; } = 18f;
	[Property, Group( "Demon Warning" )] public Color DemonWarningBg { get; set; } = new Color( 0f, 0f, 0f, 0.46f );
	[Property, Group( "Demon Warning" )] public Color DemonWarningBorderColor { get; set; } = new Color( 1f, 0.45f, 0.20f, 0.18f );
	[Property, Group( "Demon Warning" )] public Color DemonWarningShadow { get; set; } = new Color( 0f, 0f, 0f, 0.35f );
	[Property, Group( "Demon Warning" )] public Color DemonWarningTextColor { get; set; } = new Color( 1f, 0.92f, 0.70f, 0.96f );
	[Property, Group( "Demon Warning" )] public float DemonWarningShowSeconds { get; set; } = 5f;

	private float _demonWarningTimer = 0f;
	private int _lastDemonWarningStage = -1;
	private string _activeDemonWarningToken = null;


	// =========================
	// FIGHT TIMER (top-center)
	// =========================
	[Property, Group( "Fight Timer" )] public bool FightTimerEnabled { get; set; } = true;
	[Property, Group( "Fight Timer" )] public float FightTimerTopOffset { get; set; } = 18f;
	[Property, Group( "Fight Timer" )] public float FightTimerWidth { get; set; } = 160f;
	[Property, Group( "Fight Timer" )] public float FightTimerHeight { get; set; } = 44f;
	[Property, Group( "Fight Timer" )] public float FightTimerTextSize { get; set; } = 22f;
	[Property, Group( "Fight Timer" )] public Color FightTimerTextColor { get; set; } = new Color( 1f, 1f, 1f, 0.95f );
	[Property, Group( "Fight Timer" )] public Color FightTimerBg { get; set; } = new Color( 0f, 0f, 0f, 0.45f );
	[Property, Group( "Fight Timer" )] public Color FightTimerBorderColor { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property, Group( "Fight Timer" )] public float FightTimerRadius { get; set; } = 14f;

	private float _fightTime = 0f;
	private bool _wasFighting = false;

	// =========================
	// BOSS HUD
	// =========================
	[Property, Group( "Boss HUD" )] public bool BossHudEnabled { get; set; } = true;
	[Property, Group( "Boss HUD" )] public string BossObjectName { get; set; } = "npc_boss";
	[Property, Group( "Boss HUD" )] public string BossDisplayName { get; set; } = "ДЕМОН ПРЕИСПОДНЕЙ";
	[Property, Group( "Boss HUD" )] public float BossHudTopGap { get; set; } = 8f;
	[Property, Group( "Boss HUD" )] public float BossHudWidth { get; set; } = 700f;
	[Property, Group( "Boss HUD" )] public float BossHudHeight { get; set; } = 52f;
	[Property, Group( "Boss HUD" )] public float BossHudRadius { get; set; } = 14f;
	[Property, Group( "Boss HUD" )] public float BossHudTitleSize { get; set; } = 18f;
	[Property, Group( "Boss HUD" )] public float BossHudHpTextSize { get; set; } = 14f;
	[Property, Group( "Boss HUD" )] public float BossHudLookupInterval { get; set; } = 0.15f;

	[Property, Group( "Boss HUD" )] public Color BossHudBg { get; set; } = new Color( 0.08f, 0.02f, 0.02f, 0.78f );
	[Property, Group( "Boss HUD" )] public Color BossHudBorder { get; set; } = new Color( 1.00f, 0.45f, 0.20f, 0.18f );
	[Property, Group( "Boss HUD" )] public Color BossHudShadow { get; set; } = new Color( 0f, 0f, 0f, 0.42f );
	[Property, Group( "Boss HUD" )] public Color BossHudTitleColor { get; set; } = new Color( 1.00f, 0.83f, 0.62f, 0.98f );
	[Property, Group( "Boss HUD" )] public Color BossHudTextColor { get; set; } = new Color( 1f, 0.96f, 0.92f, 0.96f );
	[Property, Group( "Boss HUD" )] public Color BossBarBg { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property, Group( "Boss HUD" )] public Color BossBarFillHigh { get; set; } = new Color( 0.95f, 0.22f, 0.18f, 0.98f );
	[Property, Group( "Boss HUD" )] public Color BossBarFillMid { get; set; } = new Color( 1.00f, 0.55f, 0.16f, 0.98f );
	[Property, Group( "Boss HUD" )] public Color BossBarFillLow { get; set; } = new Color( 1.00f, 0.86f, 0.18f, 0.98f );

	private BossNPC _bossNpc;
	private float _bossLookupTimer = 0f;

	// =========================
	// BOSS SUMMON PROMPT
	// =========================
	[Property, Group( "Boss Summon Prompt" )] public bool BossSummonPromptEnabled { get; set; } = true;
	[Property, Group( "Boss Summon Prompt" )] public float BossSummonPromptTopGap { get; set; } = 10f;
	[Property, Group( "Boss Summon Prompt" )] public float BossSummonPromptWidth { get; set; } = 360f;
	[Property, Group( "Boss Summon Prompt" )] public float BossSummonPromptHeight { get; set; } = 56f;
	[Property, Group( "Boss Summon Prompt" )] public float BossSummonPromptRadius { get; set; } = 14f;
	[Property, Group( "Boss Summon Prompt" )] public float BossSummonPromptKeySize { get; set; } = 22f;
	[Property, Group( "Boss Summon Prompt" )] public float BossSummonPromptTextSize { get; set; } = 16f;
	[Property, Group( "Boss Summon Prompt" )] public string BossSummonPromptKeyText { get; set; } = "E";

	[Property, Group( "Boss Summon Prompt" )] public Color BossSummonPromptBg { get; set; } = new Color( 0.08f, 0.02f, 0.02f, 0.82f );
	[Property, Group( "Boss Summon Prompt" )] public Color BossSummonPromptBorder { get; set; } = new Color( 1.00f, 0.45f, 0.18f, 0.22f );
	[Property, Group( "Boss Summon Prompt" )] public Color BossSummonPromptShadow { get; set; } = new Color( 0f, 0f, 0f, 0.42f );
	[Property, Group( "Boss Summon Prompt" )] public Color BossSummonPromptKeyBg { get; set; } = new Color( 0.95f, 0.45f, 0.18f, 0.95f );
	[Property, Group( "Boss Summon Prompt" )] public Color BossSummonPromptKeyTextColor { get; set; } = new Color( 1f, 1f, 1f, 0.98f );
	[Property, Group( "Boss Summon Prompt" )] public Color BossSummonPromptTextColor { get; set; } = new Color( 1f, 0.93f, 0.88f, 0.97f );

	private BossSummonTriggerZone _bossSummonZone;

	// =========================
	// DAMAGE FLASH + SHAKE
	// =========================
	[Property] public bool DamageFlashEnabled { get; set; } = true;
	[Property] public float DamageFlashTime { get; set; } = 0.18f;
	[Property] public float DamageShakeTime { get; set; } = 0.18f;
	[Property] public float DamageShakePixels { get; set; } = 5f;
	[Property] public Color DamageFlashColor { get; set; } = new Color( 1f, 0.15f, 0.15f, 0.22f );

	private float _lastHp = -1f;
	private float _damageFlashTimer = 0f;
	private float _damageShakeTimer = 0f;

	// =========================
	// KILL POPUPS + COMBO
	// =========================
	[Property] public bool KillPopupsEnabled { get; set; } = true;
	[Property] public float KillPopupLife { get; set; } = 0.90f;
	[Property] public float KillPopupRiseSpeed { get; set; } = 55f;
	[Property] public float KillPopupSpreadX { get; set; } = 18f;
	[Property] public Color KillPopupColor { get; set; } = new Color( 1f, 1f, 1f, 0.95f );
	[Property] public Color KillPopupShadow { get; set; } = new Color( 0f, 0f, 0f, 0.45f );

	[Property] public bool ComboEnabled { get; set; } = true;
	[Property] public float ComboWindow { get; set; } = 1.25f;
	[Property] public float ComboShowTime { get; set; } = 1.10f;
	[Property] public Color ComboColor { get; set; } = new Color( 1.00f, 0.80f, 0.15f, 0.95f );

	private int _lastKills = -1;
	private int _comboCount = 0;
	private float _comboTimer = 0f;
	private float _timeSinceLastKill = 999f;

	private struct KillPopup
	{
		public Vector2 Pos;
		public Vector2 Vel;
		public float TimeLeft;
		public string Text;
		public float Size;
	}

	private readonly List<KillPopup> _killPopups = new();

	// =========================
	// NPC BUFF TOASTS
	// =========================
	[Property, Group( "Npc Buff Toast" )] public bool NpcBuffToastsEnabled { get; set; } = true;
	[Property, Group( "Npc Buff Toast" )] public float NpcBuffToastSlideTime { get; set; } = 0.75f;
	[Property, Group( "Npc Buff Toast" )] public float NpcBuffToastHoldTime { get; set; } = 2.25f;
	[Property, Group( "Npc Buff Toast" )] public float NpcBuffToastWidth { get; set; } = 560f;
	[Property, Group( "Npc Buff Toast" )] public float NpcBuffToastHeight { get; set; } = 50f;

	private struct NpcBuffToast
	{
		public string Text;
		public float Age;
		public float SlideTime;
		public float HoldTime;
	}

	private readonly List<NpcBuffToast> _npcBuffToasts = new();

	// =========================
	// STAMINA BAR
	// =========================
	[Property, Group( "Stamina" )] public bool StaminaBarEnabled { get; set; } = true;
	[Property, Group( "Stamina" )] public float StaminaBarWidth { get; set; } = 520f;
	[Property, Group( "Stamina" )] public float StaminaBarHeight { get; set; } = 18f;
	[Property, Group( "Stamina" )] public float StaminaBarBottomOffset { get; set; } = 70f;
	[Property, Group( "Stamina" )] public Color StaminaBg { get; set; } = new Color( 1f, 1f, 1f, 0.12f );
	[Property, Group( "Stamina" )] public Color StaminaFill { get; set; } = new Color( 0.35f, 0.85f, 1.00f, 0.95f );
	[Property, Group( "Stamina" )] public Color StaminaBorder { get; set; } = new Color( 1f, 1f, 1f, 0.10f );

	// ===== refs =====
	private GameManager _gameManager;
	private PlayerHealth _playerHealth;
	private PlayerLevel _playerLevel;
	private PlayerStats _playerStats;
	private PlayerStamina _playerStamina;
	private PlayerStaff _playerStaff;

	private float _fightMessageTimer = 0f;
	private bool _showFightMessage = false;
	private bool _hasShownFightMessage = false;

	private readonly Dictionary<string, int> _abilityLevels = new();
	private MagnetAbilityController _magnetAbility;


	private static bool IsRuLanguage()
	{
		return GameLocalization.IsLanguage( "ru" );
	}

	private static string L( string token, string russianFallback, string englishFallback )
	{
		return GameLocalization.T( token, IsRuLanguage() ? russianFallback : englishFallback );
	}

	private static string F( string token, string russianFallback, string englishFallback, params object[] args )
	{
		string fallback = IsRuLanguage() ? russianFallback : englishFallback;
		string format = GameLocalization.T( token, fallback );
		try
		{
			return string.Format( format, args );
		}
		catch
		{
			return fallback;
		}
	}

	private static string NormalizeAbilityKey( string abilityName )
	{
		if ( string.IsNullOrWhiteSpace( abilityName ) )
			return string.Empty;

		string key = abilityName.Trim();

		return key.ToLowerInvariant() switch
		{
			"regen" or "регенерация" or "regeneration" => "regen",
			"maxhp" or "увеличение hp" or "max hp" or "increase hp" => "maxhp",
			"damage5" or "урон +5" or "damage +5" => "damage5",
			"atkspd" or "скорость атаки" or "attack speed" => "atkspd",
			"multitarget" or "мульти-луч" or "multi-beam" or "multi beam" => "multitarget",
			"range" or "дальность луча" or "beam range" or "range boost" => "range",
			"runspeed" or "скорость бега" or "run speed" => "runspeed",
			"stam_regen" or "реген стамины" or "stamina regen" => "stam_regen",
			"stam_drain" or "экономия стамины" or "stamina efficiency" or "stamina drain" => "stam_drain",
			"luck" or "удача" => "luck",
			"big_luck" or "большая удача" or "greater luck" or "big luck" => "big_luck",
			"luck_blessing" or "благословение удачи" or "luck blessing" or "blessing of luck" => "luck_blessing",
			"shield" or "щит" or "barrier" => "shield",
			"berserk" or "берсерк" => "berserk",
			"radiation" or "лучевая болезнь" or "radiation sickness" => "radiation",
			"goldfever" or "золотая лихорадка" or "gold fever" => "goldfever",
			"magnet" or "магнит" => "magnet",
			_ => key
		};
	}

	private static string ResolveBossDisplayName( string value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
			return L( "#gamehud.boss.display_name", "ДЕМОН ПРЕИСПОДНЕЙ", "INFERNAL DEMON" );

		string trimmed = value.Trim();

		if ( trimmed.StartsWith( "#", StringComparison.Ordinal ) )
			return L( trimmed, "ДЕМОН ПРЕИСПОДНЕЙ", "INFERNAL DEMON" );

		if ( string.Equals( trimmed, "ДЕМОН ПРЕИСПОДНЕЙ", StringComparison.OrdinalIgnoreCase ) ||
			 string.Equals( trimmed, "INFERNAL DEMON", StringComparison.OrdinalIgnoreCase ) )
		{
			return L( "#gamehud.boss.display_name", "ДЕМОН ПРЕИСПОДНЕЙ", "INFERNAL DEMON" );
		}

		return trimmed;
	}

	private static string ResolveBossPromptText( string value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
			return L( "#gamehud.boss_summon.default_prompt", "ДЛЯ ВЫЗОВА БОССА", "TO SUMMON THE BOSS" );

		string trimmed = value.Trim();

		if ( trimmed.StartsWith( "#", StringComparison.Ordinal ) )
			return L( trimmed, "ДЛЯ ВЫЗОВА БОССА", "TO SUMMON THE BOSS" );

		if ( string.Equals( trimmed, "ДЛЯ ВЫЗОВА БОССА", StringComparison.OrdinalIgnoreCase ) ||
			 string.Equals( trimmed, "TO SUMMON THE BOSS", StringComparison.OrdinalIgnoreCase ) )
		{
			return L( "#gamehud.boss_summon.default_prompt", "ДЛЯ ВЫЗОВА БОССА", "TO SUMMON THE BOSS" );
		}

		return trimmed;
	}

	private string GetAbilityDisplayText( string abilityName, int level )
	{
		if ( level <= 0 ) return "";

		string id = NormalizeAbilityKey( abilityName );

		return id switch
		{
			"regen" => F( "#gamehud.ability.regen", "❤️ Регенерация +{0} HP/4сек", "❤️ Regeneration +{0} HP/4s", level ),
			"maxhp" => F( "#gamehud.ability.maxhp", "💪 Макс HP +{0}", "💪 Max HP +{0}", level * 20 ),
			"damage5" => F( "#gamehud.ability.damage5", "⚡ Урон +{0}", "⚡ Damage +{0}", level * 5 ),
			"atkspd" => F( "#gamehud.ability.atkspd", "⚔️ Скорость атаки +{0}%", "⚔️ Attack speed +{0}%", level * 10 ),
			"multitarget" => F( "#gamehud.ability.multitarget", "⚡⚡ Целей: {0}", "⚡⚡ Targets: {0}", level + 1 ),
			"range" => F( "#gamehud.ability.range", "🎯 Дальность +{0}%", "🎯 Range +{0}%", level * 5 ),
			"runspeed" => F( "#gamehud.ability.runspeed", "🏃 Скорость бега +{0}%", "🏃 Run speed +{0}%", level * 10 ),
			"stam_regen" => F( "#gamehud.ability.stam_regen", "🔋 Реген стамины +{0}%", "🔋 Stamina regen +{0}%", level * 15 ),
			"stam_drain" => F( "#gamehud.ability.stam_drain", "🧃 Расход стамины -{0}%", "🧃 Stamina use -{0}%", level * 10 ),
			"luck" => F( "#gamehud.ability.luck", "🍀 Удача +{0}%", "🍀 Luck +{0}%", level ),
			"shield" => _playerHealth != null
				? F( "#gamehud.ability.shield_live", "🛡️ Щит {0}/{1}", "🛡️ Shield {0}/{1}", MathF.Ceiling( _playerHealth.CurrentShield ), MathF.Ceiling( _playerHealth.MaxShield ) )
				: F( "#gamehud.ability.shield", "🛡️ Щит +{0} (реген 15с)", "🛡️ Shield +{0} (15s regen)", level * 5 ),
			"berserk" => L( "#gamehud.ability.berserk", "😡 Берсерк", "😡 Berserk" ),
			"radiation" => F( "#gamehud.ability.radiation", "☣️ Лучевая болезнь {0}%", "☣️ Radiation sickness {0}%", level * 5 ),
			"goldfever" => F( "#gamehud.ability.goldfever", "🪙 Золото за килл +{0}", "🪙 Gold per kill +{0}", level ),
			_ => F( "#gamehud.ability.default", "{0} x{1}", "{0} x{1}", abilityName, level )
		};
	}

	private string GetDemonWarningTokenForStage( int stage )
	{
		return stage switch
		{
			0 => "#gamehud.demon.warning.stage0",
			1 => "#gamehud.demon.warning.stage1",
			2 => "#gamehud.demon.warning.stage2",
			3 => "#gamehud.demon.warning.stage3",
			_ => null
		};
	}


	protected override void OnStart()
	{
		GameLocalization.EnsureLoaded();
		_rng = new Random( (int)(Time.Now * 1000f) ^ GameObject.Id.GetHashCode() );

		_gameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		_playerHealth = Scene.GetAllComponents<PlayerHealth>().FirstOrDefault();
		_playerLevel = Scene.GetAllComponents<PlayerLevel>().FirstOrDefault();
		_playerStats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		_playerStamina = Scene.GetAllComponents<PlayerStamina>().FirstOrDefault();
		_playerStaff = Scene.GetAllComponents<PlayerStaff>().FirstOrDefault();
		_magnetAbility = MagnetAbilityController.FindIn( Scene );

		if ( _playerLevel != null )
			_playerLevel.OnAbilitySelected += AddAbility;

		if ( _playerHealth != null )
		{
			_lastHp = _playerHealth.CurrentHealth;
			_lastShield = _playerHealth.CurrentShield;
		}

		if ( _playerStats != null )
			_lastKills = _playerStats.TotalKills;

		RefreshBossReference();
		_bossSummonZone = Scene.GetAllComponents<BossSummonTriggerZone>().FirstOrDefault();
	}

	public void AddNpcBuffToast( string message )
	{
		if ( !NpcBuffToastsEnabled ) return;
		if ( string.IsNullOrWhiteSpace( message ) ) return;

		_npcBuffToasts.Add( new NpcBuffToast
		{
			Text = message,
			Age = 0f,
			SlideTime = MathF.Max( 0.05f, NpcBuffToastSlideTime ),
			HoldTime = MathF.Max( 0.05f, NpcBuffToastHoldTime )
		} );

		while ( _npcBuffToasts.Count > 3 )
			_npcBuffToasts.RemoveAt( 0 );
	}

	public void AddAbility( string abilityName )
	{
		string id = NormalizeAbilityKey( abilityName );
		if ( string.IsNullOrWhiteSpace( id ) )
			return;

		if ( id == "magnet" )
			return;

		if ( id == "luck" )
		{
			if ( _abilityLevels.ContainsKey( "luck" ) )
				_abilityLevels["luck"] += 2;
			else
				_abilityLevels["luck"] = 2;
			return;
		}

		if ( id == "big_luck" )
		{
			if ( _abilityLevels.ContainsKey( "luck" ) )
				_abilityLevels["luck"] += 8;
			else
				_abilityLevels["luck"] = 8;
			return;
		}

		if ( id == "luck_blessing" )
		{
			if ( _abilityLevels.ContainsKey( "luck" ) )
				_abilityLevels["luck"] += 15;
			else
				_abilityLevels["luck"] = 15;
			return;
		}

		if ( _abilityLevels.ContainsKey( id ) )
			_abilityLevels[id]++;
		else
			_abilityLevels[id] = 1;
	}

	public void SetAbilityLevel( string abilityName, int level )
	{
		string id = NormalizeAbilityKey( abilityName );
		if ( string.IsNullOrWhiteSpace( id ) )
			return;

		if ( id == "magnet" )
		{
			_abilityLevels.Remove( id );
			return;
		}

		if ( level <= 0 )
		{
			_abilityLevels.Remove( id );
			return;
		}

		_abilityLevels[id] = level;
	}

	protected override void OnUpdate()
	{
		if ( _gameManager == null ) return;

		if ( _playerStamina == null || !_playerStamina.IsValid )
			_playerStamina = Scene.GetAllComponents<PlayerStamina>().FirstOrDefault();

		if ( _playerStaff == null || !_playerStaff.IsValid )
			_playerStaff = Scene.GetAllComponents<PlayerStaff>().FirstOrDefault();

		if ( (_bossSummonZone == null || !_bossSummonZone.IsValid) && BossSummonPromptEnabled )
			_bossSummonZone = Scene.GetAllComponents<BossSummonTriggerZone>().FirstOrDefault();

		if ( _magnetAbility == null || !_magnetAbility.IsValid )
			_magnetAbility = MagnetAbilityController.FindIn( Scene );

		UpdateBossReference();
		UpdateDamageFeedback();
		UpdateShieldEffects();
		UpdateKillEffects();
		UpdateNpcBuffToasts();

		if ( _gameManager.CurrentState == GameManager.GameState.Fighting && !_hasShownFightMessage )
		{
			_showFightMessage = true;
			_hasShownFightMessage = true;
			_fightMessageTimer = 2.0f;
		}

		if ( _showFightMessage )
		{
			_fightMessageTimer -= Time.Delta;
			if ( _fightMessageTimer <= 0 ) _showFightMessage = false;
		}

		bool isFighting = _gameManager.CurrentState == GameManager.GameState.Fighting;
		if ( isFighting )
		{
			if ( !_wasFighting ) _fightTime = 0f;
			_fightTime += Time.Delta;
		}
		_wasFighting = isFighting;

		_shieldRotation += (ShieldRotationSpeed * MathF.PI * 2f) * Time.Delta;

		if ( _shieldBreakTimer > 0f )
			_shieldBreakTimer -= Time.Delta;

		UpdateDemonWarning();
		DrawHUD();
	}

	// =========================
	// BOSS LOOKUP
	// =========================
	private void UpdateBossReference()
	{
		if ( !BossHudEnabled )
		{
			_bossNpc = null;
			return;
		}

		_bossLookupTimer -= Time.Delta;
		if ( _bossLookupTimer > 0f && IsBossHudTargetValid( _bossNpc ) )
			return;

		RefreshBossReference();
	}

	private void RefreshBossReference()
	{
		_bossLookupTimer = MathF.Max( 0.05f, BossHudLookupInterval );

		_bossNpc = Scene.GetAllComponents<BossNPC>()
			.FirstOrDefault( IsBossHudTargetValid );
	}

	private BossNPC GetActiveBoss()
	{
		if ( IsBossHudTargetValid( _bossNpc ) )
			return _bossNpc;

		return null;
	}

	private bool IsBossHudTargetValid( BossNPC boss )
	{
		if ( boss == null || !boss.IsValid || !boss.Enabled )
			return false;

		if ( boss.GameObject == null || !boss.GameObject.IsValid )
			return false;

		if ( !string.Equals( boss.GameObject.Name, BossObjectName, StringComparison.OrdinalIgnoreCase ) )
			return false;

		if ( boss.Health <= 0f || boss.MaxHealth <= 0f )
			return false;

		return true;
	}

	private bool IsFightTimerVisible()
	{
		return FightTimerEnabled
			&& _gameManager != null
			&& _gameManager.CurrentState == GameManager.GameState.Fighting;
	}

	private bool IsBossSummonPromptVisible()
	{
		if ( !BossSummonPromptEnabled )
			return false;

		if ( _bossSummonZone == null || !_bossSummonZone.IsValid )
			return false;

		return _bossSummonZone.IsPromptVisible;
	}

	private float GetCenterTopAnchorBottomY()
	{
		float s = CenterHudScale;

		if ( IsFightTimerVisible() )
			return (FightTimerTopOffset * s) + (FightTimerHeight * s);

		return 18f * s;
	}

	private float GetBossHudTopY()
	{
		float s = CenterHudScale;
		return GetCenterTopAnchorBottomY() + (BossHudTopGap * s);
	}

	private float GetBossHudBottomY()
	{
		float s = CenterHudScale;
		return GetBossHudTopY() + (BossHudHeight * s);
	}

	private float GetBossPromptTopY()
	{
		float s = CenterHudScale;

		float baseY = GetCenterTopAnchorBottomY() + (BossSummonPromptTopGap * s);
		var boss = GetActiveBoss();

		if ( boss != null )
			baseY = GetBossHudBottomY() + (BossSummonPromptTopGap * s);

		return baseY;
	}

	private float GetBossPromptBottomY()
	{
		float s = CenterHudScale;
		return GetBossPromptTopY() + (BossSummonPromptHeight * s);
	}

	private float GetCenterOverlayBottomY()
	{
		float y = GetCenterTopAnchorBottomY();

		if ( GetActiveBoss() != null )
			y = MathF.Max( y, GetBossHudBottomY() );

		if ( IsBossSummonPromptVisible() )
			y = MathF.Max( y, GetBossPromptBottomY() );

		return y;
	}


	private bool IsBossObjectPresentOnMap()
	{
		string bossObjectName = !string.IsNullOrWhiteSpace( _gameManager?.BossObjectName )
			? _gameManager.BossObjectName
			: BossObjectName;

		if ( string.IsNullOrWhiteSpace( bossObjectName ) )
			bossObjectName = "npc_boss";

		return Scene.GetAllComponents<BossNPC>().Any( boss =>
			boss != null &&
			boss.IsValid &&
			boss.GameObject != null &&
			boss.GameObject.IsValid &&
			string.Equals( boss.GameObject.Name, bossObjectName, StringComparison.OrdinalIgnoreCase ) );
	}

	private void UpdateDemonWarning()
	{
		if ( !DemonWarningEnabled )
		{
			_activeDemonWarningToken = null;
			_demonWarningTimer = 0f;
			return;
		}

		bool isFighting = _gameManager != null && _gameManager.CurrentState == GameManager.GameState.Fighting;

		if ( !isFighting )
		{
			_activeDemonWarningToken = null;
			_demonWarningTimer = 0f;
			_lastDemonWarningStage = -1;
			return;
		}

		if ( IsBossObjectPresentOnMap() )
		{
			_activeDemonWarningToken = null;
			_demonWarningTimer = 0f;
			return;
		}

		int stage = -1;

		if ( _fightTime >= 19f * 60f )
			stage = 3;
		else if ( _fightTime >= 15f * 60f )
			stage = 2;
		else if ( _fightTime >= 5f * 60f )
			stage = 1;
		else if ( _fightTime >= 60f )
			stage = 0;

		if ( stage > _lastDemonWarningStage )
		{
			string token = GetDemonWarningTokenForStage( stage );
			if ( !string.IsNullOrWhiteSpace( token ) )
			{
				_lastDemonWarningStage = stage;
				_activeDemonWarningToken = token;
				_demonWarningTimer = MathF.Max( 0.05f, DemonWarningShowSeconds );
			}
		}

		if ( _demonWarningTimer > 0f )
		{
			_demonWarningTimer -= Time.Delta;
			if ( _demonWarningTimer <= 0f )
			{
				_demonWarningTimer = 0f;
				_activeDemonWarningToken = null;
			}
		}
	}

	private string GetDemonWarningText()
	{
		if ( _demonWarningTimer <= 0f )
			return null;

		if ( string.IsNullOrWhiteSpace( _activeDemonWarningToken ) )
			return null;

		if ( IsBossObjectPresentOnMap() )
			return null;

		return _activeDemonWarningToken switch
		{
			"#gamehud.demon.warning.stage0" => L( _activeDemonWarningToken, "Демон выйдет через 19 минут", "The demon will emerge in 19 minutes" ),
			"#gamehud.demon.warning.stage1" => L( _activeDemonWarningToken, "Демон готовится к бою через 15 минут", "The demon is preparing for battle in 15 minutes" ),
			"#gamehud.demon.warning.stage2" => L( _activeDemonWarningToken, "Демон собирает армию через 5 минут", "The demon is gathering an army in 5 minutes" ),
			"#gamehud.demon.warning.stage3" => L( _activeDemonWarningToken, "Демон выходит крушить через 1 минуту!", "The demon will emerge to destroy everything in 1 minute!" ),
			_ => null
		};
	}


	private void UpdateShieldEffects()
	{
		if ( _playerHealth == null ) return;

		float shield = _playerHealth.CurrentShield;

		if ( _lastShield < 0f )
		{
			_lastShield = shield;
			return;
		}

		if ( shield < _lastShield - 0.001f )
		{
			_shieldHitTimer = ShieldHitFlashTime;

			if ( shield <= 0.001f && _lastShield > 0.001f )
				_shieldBreakTimer = ShieldBreakTime;
		}

		if ( shield > _lastShield + 0.001f )
			_shieldRegenPulse = 1f;

		_lastShield = shield;

		if ( _shieldHitTimer > 0f )
			_shieldHitTimer -= Time.Delta;

		if ( _shieldRegenPulse > 0f )
			_shieldRegenPulse -= Time.Delta * ShieldRegenPulseSpeed;
	}

	private void UpdateNpcBuffToasts()
	{
		for ( int i = _npcBuffToasts.Count - 1; i >= 0; i-- )
		{
			var t = _npcBuffToasts[i];
			t.Age += Time.Delta;
			_npcBuffToasts[i] = t;

			if ( t.Age >= (t.SlideTime + t.HoldTime) )
				_npcBuffToasts.RemoveAt( i );
		}
	}

	private void UpdateDamageFeedback()
	{
		if ( !DamageFlashEnabled ) return;
		if ( _playerHealth == null ) return;

		float hpNow = _playerHealth.CurrentHealth;

		if ( _lastHp < 0f )
		{
			_lastHp = hpNow;
			return;
		}

		if ( hpNow < _lastHp - 0.01f )
		{
			_damageFlashTimer = DamageFlashTime;
			_damageShakeTimer = DamageShakeTime;
		}

		_lastHp = hpNow;

		if ( _damageFlashTimer > 0f ) _damageFlashTimer -= Time.Delta;
		if ( _damageShakeTimer > 0f ) _damageShakeTimer -= Time.Delta;
	}

	private void UpdateKillEffects()
	{
		_timeSinceLastKill += Time.Delta;

		if ( _playerStats != null )
		{
			int killsNow = _playerStats.TotalKills;

			if ( _lastKills < 0 ) _lastKills = killsNow;

			if ( killsNow > _lastKills )
			{
				int delta = killsNow - _lastKills;
				_lastKills = killsNow;

				if ( ComboEnabled )
				{
					if ( _timeSinceLastKill <= ComboWindow ) _comboCount += delta;
					else _comboCount = delta;

					_comboTimer = ComboShowTime;
					_timeSinceLastKill = 0f;
				}

				if ( KillPopupsEnabled )
					SpawnKillPopup( delta );
			}
		}

		if ( _comboTimer > 0f )
			_comboTimer -= Time.Delta;

		for ( int i = _killPopups.Count - 1; i >= 0; i-- )
		{
			var p = _killPopups[i];
			p.TimeLeft -= Time.Delta;

			p.Pos += p.Vel * Time.Delta;
			p.Vel = new Vector2( p.Vel.x, p.Vel.y - (KillPopupRiseSpeed * 0.25f) * Time.Delta );

			_killPopups[i] = p;

			if ( p.TimeLeft <= 0f )
				_killPopups.RemoveAt( i );
		}
	}

	private void SpawnKillPopup( int deltaKills )
	{
		float s = LeftHudScale;

		float killsX;
		float killsY;

		if ( AutoStackLeftPanels )
		{
			float gap = LeftStackGap * s;
			killsX = StatsKillsPosition.x;
			killsY = HPPosition.y + (HPPanelHeight * s) + gap + (LevelPanelHeight * s) + gap;
		}
		else
		{
			killsX = StatsKillsPosition.x;
			killsY = StatsKillsPosition.y;
		}

		float baseX = killsX + (KillsPanelWidth * s) - (40f * s);
		float baseY = killsY + (6f * s);

		float noise = MathF.Sin( (Time.Now + _killPopups.Count * 0.37f) * 19.73f );
		float rx = noise * (KillPopupSpreadX * s);

		var popup = new KillPopup
		{
			Pos = new Vector2( baseX + rx, baseY ),
			Vel = new Vector2( rx * 0.8f, -(KillPopupRiseSpeed * s) ),
			TimeLeft = KillPopupLife,
			Text = deltaKills == 1 ? "+1" : $"+{deltaKills}",
			Size = 20f * s
		};

		_killPopups.Add( popup );
	}

	// =========================
	// RENDER
	// =========================
	private void DrawHUD()
	{
		if ( Scene.Camera == null ) return;

		HudPainter hud = Scene.Camera.Hud;

		Vector2 shake = GetShakeOffset();

		DrawHP( hud, shake );
		DrawLevelAndExp( hud, shake );
		DrawKills( hud, shake );
		DrawCoins( hud, shake );
		DrawAbilities( hud, shake );
		DrawCenterMessages( hud );
		DrawShopExitProtection( hud );

		DrawFightTimer( hud );
		DrawBossHpBar( hud );
		DrawBossSummonPrompt( hud );
		DrawDemonWarning( hud );

		DrawNpcBuffToasts( hud );
		DrawStaminaBar( hud );

		DrawKillPopups( hud );
	}

	private Vector2 GetShakeOffset()
	{
		if ( _damageShakeTimer <= 0f ) return default;

		float t = _damageShakeTimer / MathF.Max( 0.001f, DamageShakeTime );
		float amp = DamageShakePixels * t;

		float sx = MathF.Sin( Time.Now * 80f ) * amp;
		float sy = MathF.Cos( Time.Now * 70f ) * amp;

		return new Vector2( sx, sy );
	}

	private float GetShopExitProtectionSecondsLeft()
	{
		float invuln = (_playerHealth != null && _playerHealth.IsValid)
			? _playerHealth.TemporaryInvulnerabilitySecondsLeft
			: 0f;

		float attackLock = (_playerStaff != null && _playerStaff.IsValid)
			? _playerStaff.AttackLockSecondsLeft
			: 0f;

		return MathF.Max( invuln, attackLock );
	}

	private void DrawShopExitProtection( HudPainter hud )
	{
		if ( !ShopExitProtectionHudEnabled ) return;

		float secondsLeft = GetShopExitProtectionSecondsLeft();
		if ( secondsLeft <= 0.001f ) return;

		float s = CenterHudScale;
		float w = ShopExitProtectionWidth * s;
		float h = ShopExitProtectionHeight * s;
		float x = (Screen.Width * 0.5f) - (w * 0.5f);
		float y = (Screen.Height * 0.5f) - (h * 0.5f);

		DrawRoundedRect( hud, x + (3f * s), y + (4f * s), w, h, new Color( 0f, 0f, 0f, 0.35f ), 18f * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, x, y, w, h, ShopExitProtectionBg, 18f * s, 2f * s, ShopExitProtectionBorder );

		hud.DrawText(
			new TextRendering.Scope( L( "#gamehud.shop_exit_protection.title", "ЗАЩИТА ПОСЛЕ МАГАЗИНА", "POST-SHOP PROTECTION" ), ShopExitProtectionTitleColor, ShopExitProtectionTextSize * s ),
			new Rect( x, y + (10f * s), w, 24f * s ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( $"{secondsLeft:0.0}{L( "#gamehud.common.seconds_suffix", "с", "s" )}", ShopExitProtectionCountdownColor, ShopExitProtectionCountdownSize * s ),
			new Rect( x, y + (30f * s), w, 40f * s ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( L( "#gamehud.shop_exit_protection.description", "Бессмертие активно • атака посохом заблокирована", "Invulnerability active • staff attack is blocked" ), ShopExitProtectionTextColor, (ShopExitProtectionTextSize - 2f) * s ),
			new Rect( x, y + (72f * s), w, 22f * s ),
			TextFlag.Center
		);
	}

	// =========================
	// FIGHT TIMER DRAW
	// =========================
	private void DrawFightTimer( HudPainter hud )
	{
		if ( !IsFightTimerVisible() ) return;

		float s = CenterHudScale;

		int totalSeconds = (int)MathF.Floor( _fightTime );
		int minutes = totalSeconds / 60;
		int seconds = totalSeconds % 60;

		string text = $"{minutes:00}:{seconds:00}";

		float w = FightTimerWidth * s;
		float h = FightTimerHeight * s;

		float x = (Screen.Width * 0.5f) - (w * 0.5f);
		float y = FightTimerTopOffset * s;

		DrawRoundedRect( hud, x + (2f * s), y + (2f * s), w, h, new Color( 0, 0, 0, 0.30f ), FightTimerRadius * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, x, y, w, h, FightTimerBg, FightTimerRadius * s, 1.5f * s, FightTimerBorderColor );

		hud.DrawText(
			new TextRendering.Scope( text, FightTimerTextColor, FightTimerTextSize * s ),
			new Rect( x, y, w, h ),
			TextFlag.Center
		);
	}

	// =========================
	// BOSS HP DRAW
	// =========================
	private void DrawBossHpBar( HudPainter hud )
	{
		if ( !BossHudEnabled ) return;

		var boss = GetActiveBoss();
		if ( boss == null ) return;

		float s = CenterHudScale;

		float w = BossHudWidth * s;
		float h = BossHudHeight * s;

		float x = (Screen.Width * 0.5f) - (w * 0.5f);
		float y = GetBossHudTopY();

		DrawRoundedRect( hud, x + (3f * s), y + (4f * s), w, h, BossHudShadow, BossHudRadius * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, x, y, w, h, BossHudBg, BossHudRadius * s, 1.8f * s, BossHudBorder );

		hud.DrawRect( new Rect( x + (12f * s), y + (10f * s), w - (24f * s), 2f * s ), new Color( 1f, 0.65f, 0.35f, 0.10f ) );

		float hp = MathF.Max( 0f, boss.Health );
		float maxHp = MathF.Max( 1f, boss.MaxHealth );
		float frac = Clamp01( hp / maxHp );

		Color fill =
			(frac > 0.60f) ? BossBarFillHigh :
			(frac > 0.25f) ? BossBarFillMid :
			BossBarFillLow;

		float padX = 16f * s;
		float titleY = y + (6f * s);

		hud.DrawText(
			new TextRendering.Scope( ResolveBossDisplayName( BossDisplayName ), BossHudTitleColor, BossHudTitleSize * s ),
			new Rect( x + padX, titleY, w - (padX * 2f), 18f * s ),
			TextFlag.Center
		);

		float barX = x + padX;
		float barY = y + h - (18f * s);
		float barW = w - (padX * 2f);
		float barH = 12f * s;

		DrawRoundedRect( hud, barX, barY, barW, barH, BossBarBg, 6f * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, barX, barY, barW * frac, barH, fill, 6f * s, 0f, new Color( 0, 0, 0, 0 ) );

		hud.DrawRect( new Rect( barX, barY, barW, 1.5f * s ), new Color( 1f, 1f, 1f, 0.06f ) );

		string hpText = $"{MathF.Ceiling( hp )}/{MathF.Ceiling( maxHp )}";
		hud.DrawText(
			new TextRendering.Scope( hpText, BossHudTextColor, BossHudHpTextSize * s ),
			new Rect( barX, y + (24f * s), barW, 14f * s ),
			TextFlag.Center
		);
	}

	// =========================
	// BOSS SUMMON PROMPT
	// =========================
	private void DrawBossSummonPrompt( HudPainter hud )
	{
		if ( !IsBossSummonPromptVisible() )
			return;

		float s = CenterHudScale;

		float w = BossSummonPromptWidth * s;
		float h = BossSummonPromptHeight * s;

		float x = (Screen.Width * 0.5f) - (w * 0.5f);
		float y = GetBossPromptTopY();

		DrawRoundedRect( hud, x + (3f * s), y + (4f * s), w, h, BossSummonPromptShadow, BossSummonPromptRadius * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, x, y, w, h, BossSummonPromptBg, BossSummonPromptRadius * s, 1.8f * s, BossSummonPromptBorder );

		float keySize = 34f * s;
		float keyX = x + (14f * s);
		float keyY = y + (h * 0.5f) - (keySize * 0.5f);

		DrawRoundedRect( hud, keyX, keyY, keySize, keySize, BossSummonPromptKeyBg, 10f * s, 0f, new Color( 0, 0, 0, 0 ) );
		hud.DrawText(
			new TextRendering.Scope( BossSummonPromptKeyText, BossSummonPromptKeyTextColor, BossSummonPromptKeySize * s ),
			new Rect( keyX, keyY, keySize, keySize ),
			TextFlag.Center
		);

		string txt = _bossSummonZone != null && !string.IsNullOrWhiteSpace( _bossSummonZone.PromptText )
			? ResolveBossPromptText( _bossSummonZone.PromptText )
			: L( "#gamehud.boss_summon.default_prompt", "ДЛЯ ВЫЗОВА БОССА", "TO SUMMON THE BOSS" );

		float textX = keyX + keySize + (14f * s);

		hud.DrawText(
			new TextRendering.Scope( txt, BossSummonPromptTextColor, BossSummonPromptTextSize * s ),
			new Rect( textX, y + (h * 0.5f) - (12f * s), w - (textX - x) - (14f * s), 24f * s ),
			TextFlag.Left
		);
	}

	// =========================
	// STAMINA BAR
	// =========================
	private void DrawStaminaBar( HudPainter hud )
	{
		if ( !StaminaBarEnabled ) return;
		if ( _playerStamina == null || !_playerStamina.IsValid ) return;

		float frac = Clamp01( _playerStamina.StaminaFraction );

		float w = StaminaBarWidth;
		float h = StaminaBarHeight;

		float x = (Screen.Width * 0.5f) - (w * 0.5f);
		float y = Screen.Height - StaminaBarBottomOffset;

		DrawRoundedRect( hud, x + 2f, y + 2f, w, h, new Color( 0, 0, 0, 0.35f ), h * 0.5f, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, x, y, w, h, StaminaBg, h * 0.5f, 1.5f, StaminaBorder );

		DrawRoundedRect( hud, x, y, w * frac, h, StaminaFill, h * 0.5f, 0f, new Color( 0, 0, 0, 0 ) );

		string txt = F( "#gamehud.stamina.label", "СТАМИНА {0:F0}%", "STAMINA {0:F0}%", frac * 100f );
		hud.DrawText( new TextRendering.Scope( txt, new Color( 1, 1, 1, 0.85f ), 14f ), new Rect( x, y - 2f, w, h ), TextFlag.Center );
	}

	// =========================
	// NPC BUFF TOASTS
	// =========================
	private void DrawNpcBuffToasts( HudPainter hud )
	{
		if ( !NpcBuffToastsEnabled ) return;
		if ( _npcBuffToasts.Count == 0 ) return;

		float s = LeftHudScale;
		float toastW = NpcBuffToastWidth * s;
		float toastH = NpcBuffToastHeight * s;

		float x = (Screen.Width * 0.5f) - (toastW * 0.5f);
		float baseY = GetCenterOverlayBottomY() + (10f * s);

		for ( int i = 0; i < _npcBuffToasts.Count; i++ )
		{
			var t = _npcBuffToasts[i];

			float rowY = baseY + i * (toastH + (8f * s));

			float alpha = 1f;
			float total = t.SlideTime + t.HoldTime;
			if ( total > 0.001f )
			{
				float lifeT = t.Age / total;
				if ( lifeT > 0.85f )
					alpha = Clamp01( 1f - ((lifeT - 0.85f) / 0.15f) );
			}

			var bg = new Color( 0f, 0f, 0f, 0.45f * alpha );
			var border = new Color( 1f, 1f, 1f, 0.10f * alpha );
			var textCol = new Color( 1f, 0.92f, 0.25f, 0.95f * alpha );

			DrawRoundedRect( hud, x + 2f, rowY + 2f, toastW, toastH, new Color( 0, 0, 0, 0.35f * alpha ), 14f * s, 0f, new Color( 0, 0, 0, 0 ) );
			DrawRoundedRect( hud, x, rowY, toastW, toastH, bg, 14f * s, 2f * s, border );

			hud.DrawText(
				new TextRendering.Scope( t.Text, textCol, 18f * s ),
				new Rect( x + (14f * s), rowY + (6f * s), toastW - (28f * s), toastH ),
				TextFlag.Left
			);
		}
	}

	// =========================
	// LEFT PANELS
	// =========================
	private void DrawHP( HudPainter hud, Vector2 offset )
	{
		if ( _playerHealth == null ) return;

		float s = LeftHudScale;

		float x = HPPosition.x + offset.x;
		float y = HPPosition.y + offset.y;
		float w = HPPanelWidth * s;
		float h = HPPanelHeight * s;

		DrawPanel( hud, x, y, w, h, s );

		if ( _damageFlashTimer > 0f )
		{
			float a = Clamp01( _damageFlashTimer / MathF.Max( 0.001f, DamageFlashTime ) );
			var c = new Color( DamageFlashColor.r, DamageFlashColor.g, DamageFlashColor.b, DamageFlashColor.a * a );
			DrawRoundedRect( hud, x, y, w, h, c, CornerRadius * s, 0f, new Color( 0, 0, 0, 0 ) );
		}

		float hp = _playerHealth.CurrentHealth;
		float max = MathF.Max( 1f, _playerHealth.MaxHealth );
		float t = Clamp01( hp / max );

		Color hpFill = (t > 0.75f) ? HpBarFillGood : (t > 0.25f ? HpBarFillMid : HpBarFillLow);

		float iconSize = h - (16f * s);
		float iconX = x + (10f * s);
		float iconY = y + (h * 0.5f) - (iconSize * 0.5f);

		DrawRoundedRect( hud, iconX, iconY, iconSize, iconSize, new Color( 1, 1, 1, 0.07f ), iconSize * 0.5f, 0f, new Color( 0, 0, 0, 0 ) );

		var heart = new TextRendering.Scope( "❤", new Color( 1f, 0.45f, 0.45f, 0.95f ), 24f * s );
		hud.DrawText( heart, new Rect( iconX, iconY, iconSize, iconSize ), TextFlag.Center );

		var hpText = new TextRendering.Scope( $"HP {hp:F0}/{max:F0}", TextColor, TextSize * s );
		hud.DrawText( hpText, new Vector2( x + iconSize + (22f * s), y + (10f * s) ) );

		float barX = x + iconSize + (22f * s);
		float barY = y + (30f * s);
		float barW = (x + w) - barX - (12f * s);
		float barH = 12f * s;

		DrawRoundedRect( hud, barX, barY, barW, barH, HpBarBg, 6f * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, barX, barY, barW * t, barH, hpFill, 6f * s, 0f, new Color( 0, 0, 0, 0 ) );

		var pct = new TextRendering.Scope( $"{(t * 100f):F0}%", new Color( 1, 1, 1, 0.85f ), SmallTextSize * s );
		hud.DrawText( pct, new Rect( barX, barY - (1f * s), barW, barH ), TextFlag.Center );

		DrawShieldWidget( hud, x, y, w, h, s );
	}

	private void DrawShieldWidget( HudPainter hud, float hpX, float hpY, float hpW, float hpH, float scale )
	{
		if ( !ShieldWidgetEnabled ) return;
		if ( _playerHealth == null ) return;
		if ( _playerHealth.MaxShield <= 0.001f ) return;

		float max = _playerHealth.MaxShield;
		float cur = _playerHealth.CurrentShield;
		float frac = Clamp01( cur / MathF.Max( 1f, max ) );

		float size = hpH * ShieldWidgetScale;

		float x = hpX + hpW + (ShieldWidgetGap * scale);
		float y = hpY + (hpH - size) * 0.5f;

		float cx = x + size * 0.5f;
		float cy = y + size * 0.5f;

		float radius = (size * 0.5f) - (4f * scale);

		DrawRoundedRect( hud, x + (ShadowOffset * scale), y + (ShadowOffset * scale), size, size, PanelShadow, size * 0.5f, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, x, y, size, size, ShieldWidgetBg, size * 0.5f, 1.5f * scale, ShieldWidgetBorder );

		float glow = ShieldGlowStrength;

		if ( _shieldRegenPulse > 0f )
			glow += 0.18f * MathF.Sin( Time.Now * 8f );

		if ( _shieldHitTimer > 0f )
		{
			float hitT = Clamp01( _shieldHitTimer / MathF.Max( 0.001f, ShieldHitFlashTime ) );
			glow += 0.45f * hitT;
		}

		if ( _shieldBreakTimer > 0f )
		{
			float bt = Clamp01( _shieldBreakTimer / MathF.Max( 0.001f, ShieldBreakTime ) );
			glow += 0.65f * bt;
		}

		var glowColor = new Color( ShieldRingColor.r, ShieldRingColor.g, ShieldRingColor.b, glow );
		DrawCircularGlow( hud, cx, cy, radius + (4f * scale), glowColor, ShieldGlowThickness * scale, _shieldRotation );

		DrawCircularBar( hud, cx, cy, radius, frac, ShieldRingThickness * scale, ShieldRingColor, _shieldRotation );
		DrawShieldElectric( hud, cx, cy, radius + (5f * scale), scale );
		DrawShieldBreakShards( hud, cx, cy, radius + (6f * scale), scale );

		hud.DrawText(
			new TextRendering.Scope( "🛡", TitleColor, 18f * scale ),
			new Rect( x, y + (2f * scale), size, 18f * scale ),
			TextFlag.Center
		);

		hud.DrawText(
			new TextRendering.Scope( $"{cur:F0}/{max:F0}", TextColor, (SmallTextSize * 0.95f) * scale ),
			new Rect( x, y + size - (18f * scale), size, 16f * scale ),
			TextFlag.Center
		);
	}

	private void DrawCircularBar( HudPainter hud, float cx, float cy, float radius, float progress, float thickness, Color baseColor, float rotation )
	{
		int segments = 72;

		float aBoost = 0f;
		if ( _shieldHitTimer > 0f )
		{
			float hitT = Clamp01( _shieldHitTimer / MathF.Max( 0.001f, ShieldHitFlashTime ) );
			aBoost = 0.25f * hitT;
		}

		var col = new Color( baseColor.r, baseColor.g, baseColor.b, MathF.Min( 1f, baseColor.a + aBoost ) );

		for ( int i = 0; i < segments; i++ )
		{
			float t0 = (float)i / segments;
			float t1 = (float)(i + 1) / segments;

			if ( t0 > progress )
				break;

			float a0 = t0 * MathF.PI * 2f - MathF.PI * 0.5f + rotation;
			float a1 = t1 * MathF.PI * 2f - MathF.PI * 0.5f + rotation;

			Vector2 p0 = new Vector2( cx + MathF.Cos( a0 ) * radius, cy + MathF.Sin( a0 ) * radius );
			Vector2 p1 = new Vector2( cx + MathF.Cos( a1 ) * radius, cy + MathF.Sin( a1 ) * radius );

			hud.DrawLine( p0, p1, thickness, col );
		}
	}

	private void DrawCircularGlow( HudPainter hud, float cx, float cy, float radius, Color color, float thickness, float rotation )
	{
		int segments = 72;

		for ( int i = 0; i < segments; i++ )
		{
			float t0 = (float)i / segments;
			float t1 = (float)(i + 1) / segments;

			float a0 = t0 * MathF.PI * 2f + rotation;
			float a1 = t1 * MathF.PI * 2f + rotation;

			Vector2 p0 = new Vector2( cx + MathF.Cos( a0 ) * radius, cy + MathF.Sin( a0 ) * radius );
			Vector2 p1 = new Vector2( cx + MathF.Cos( a1 ) * radius, cy + MathF.Sin( a1 ) * radius );

			hud.DrawLine( p0, p1, thickness, color );
		}
	}

	private void DrawShieldElectric( HudPainter hud, float cx, float cy, float radius, float scale )
	{
		if ( ShieldElectricIntensity <= 0.001f ) return;
		if ( _playerHealth == null ) return;
		if ( _playerHealth.CurrentShield <= 0.001f ) return;

		float chance = ShieldElectricChance;
		if ( _shieldHitTimer > 0f ) chance += 0.15f;
		if ( _shieldRegenPulse > 0f ) chance += 0.08f;

		if ( _rng.NextSingle() > chance ) return;

		int sparks = 2 + _rng.Next( 0, 3 );

		for ( int i = 0; i < sparks; i++ )
		{
			float a = _rng.NextSingle() * MathF.PI * 2f + _shieldRotation;

			float len = (10f + _rng.NextSingle() * 16f) * scale * ShieldElectricIntensity;
			float jitter = (6f + _rng.NextSingle() * 10f) * scale * ShieldElectricIntensity;

			Vector2 p0 = new Vector2( cx + MathF.Cos( a ) * radius, cy + MathF.Sin( a ) * radius );
			Vector2 p1 = new Vector2(
				cx + MathF.Cos( a ) * (radius + len),
				cy + MathF.Sin( a ) * (radius + len)
			);

			Vector2 mid = (p0 + p1) * 0.5f;
			mid += new Vector2(
				(_rng.NextSingle() * 2f - 1f) * jitter,
				(_rng.NextSingle() * 2f - 1f) * jitter
			);

			Color c = new Color( 0.55f, 0.95f, 1.00f, 0.85f );

			hud.DrawLine( p0, mid, 2.0f * scale, c );
			hud.DrawLine( mid, p1, 2.0f * scale, c );
		}
	}

	private void DrawShieldBreakShards( HudPainter hud, float cx, float cy, float radius, float scale )
	{
		if ( _shieldBreakTimer <= 0f ) return;

		float t = Clamp01( _shieldBreakTimer / MathF.Max( 0.001f, ShieldBreakTime ) );
		float inv = 1f - t;

		int shards = 10;
		float strength = (14f + 22f * inv) * scale * ShieldBreakBurstStrength;

		Color c = new Color( 0.70f, 0.98f, 1.00f, 0.75f * t );

		for ( int i = 0; i < shards; i++ )
		{
			float a = (i / (float)shards) * MathF.PI * 2f + _shieldRotation + (inv * 0.6f);

			Vector2 p0 = new Vector2( cx + MathF.Cos( a ) * radius, cy + MathF.Sin( a ) * radius );
			Vector2 p1 = new Vector2( cx + MathF.Cos( a ) * (radius + strength), cy + MathF.Sin( a ) * (radius + strength) );

			hud.DrawLine( p0, p1, 2.5f * scale, c );
		}
	}

	private void DrawLevelAndExp( HudPainter hud, Vector2 offset )
	{
		if ( _playerLevel == null ) return;

		float s = LeftHudScale;

		float x;
		float y;

		if ( AutoStackLeftPanels )
		{
			float gap = LeftStackGap * s;
			x = HPPosition.x + offset.x * 0.35f;
			y = (HPPosition.y + (HPPanelHeight * s) + gap) + offset.y * 0.35f;
		}
		else
		{
			x = LevelPosition.x + offset.x * 0.35f;
			y = LevelPosition.y + offset.y * 0.35f;
		}

		float w = LevelPanelWidth * s;
		float h = LevelPanelHeight * s;

		DrawPanel( hud, x, y, w, h, s );

		int lvl = _playerLevel.CurrentLevel;
		float progress = Clamp01( _playerLevel.GetLevelProgress() );
		int curExp = _playerLevel.CurrentExp;
		int nextExp = _playerLevel.GetExpNeededForNextLevel();

		float badgeW = 74f * s;
		float badgeH = h - (16f * s);
		float badgeX = x + (10f * s);
		float badgeY = y + (8f * s);

		DrawRoundedRect( hud, badgeX, badgeY, badgeW, badgeH, new Color( 0.35f, 0.85f, 1f, 0.20f ), 10f * s, 0f, new Color( 0, 0, 0, 0 ) );

		var lvlText = new TextRendering.Scope( $"LVL\n{lvl}", TitleColor, 16f * s );
		hud.DrawText( lvlText, new Vector2( badgeX + (14f * s), badgeY + (8f * s) ) );

		var expTitle = new TextRendering.Scope( L( "#gamehud.level.exp", "Опыт", "Experience" ), MutedText, SmallTextSize * s );
		hud.DrawText( expTitle, new Vector2( x + badgeW + (24f * s), y + (10f * s) ) );

		var expNumbers = new TextRendering.Scope( F( "#gamehud.level.exp_numbers", "{0}/{1} EXP", "{0}/{1} EXP", curExp, nextExp ), TextColor, SmallTextSize * s );
		hud.DrawText( expNumbers, new Vector2( x + badgeW + (24f * s), y + (28f * s) ) );

		float barX = x + badgeW + (24f * s);
		float barY = y + h - (18f * s);
		float barW = (x + w) - barX - (12f * s);
		float barH = 10f * s;

		DrawRoundedRect( hud, barX, barY, barW, barH, ExpBarBg, 6f * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, barX, barY, barW * progress, barH, ExpBarFill, 6f * s, 0f, new Color( 0, 0, 0, 0 ) );
	}

	private void DrawKills( HudPainter hud, Vector2 offset )
	{
		if ( _playerStats == null ) return;

		float s = LeftHudScale;

		float x;
		float y;

		if ( AutoStackLeftPanels )
		{
			float gap = LeftStackGap * s;
			x = HPPosition.x + offset.x * 0.2f;
			y = (HPPosition.y + (HPPanelHeight * s) + gap + (LevelPanelHeight * s) + gap) + offset.y * 0.2f;
		}
		else
		{
			x = StatsKillsPosition.x + offset.x * 0.2f;
			y = StatsKillsPosition.y + offset.y * 0.2f;
		}

		float w = KillsPanelWidth * s;
		float h = KillsPanelHeight * s;

		DrawPanel( hud, x, y, w, h, s );

		float iconSize = KillsIconSize * s;

		float ix = x + (KillsIconOffset.x * s);
		float iy = y + (KillsIconOffset.y * s);

		var iconRect = new Rect( ix, iy, iconSize, iconSize );

		if ( KillsIconTexture != null && KillsIconTexture.IsValid )
			hud.DrawTexture( KillsIconTexture, iconRect );
		else
			hud.DrawText( new TextRendering.Scope( "💀", TextColor, 20f * s ), iconRect, TextFlag.Center );

		float tx = iconRect.Right + (KillsTextGap * s);

		hud.DrawText( new TextRendering.Scope( L( "#gamehud.kills.label", "УБИТО", "KILLS" ), MutedText, SmallTextSize * s ), new Vector2( tx, y + (10f * s) ) );

		var value = new TextRendering.Scope( $"{_playerStats.TotalKills}", TextColor, BigTextSize * s );
		hud.DrawText( value, new Vector2( x + w - (46f * s), y + (6f * s) ) );

		if ( ComboEnabled && _comboTimer > 0f && _comboCount > 1 )
		{
			float a = Clamp01( _comboTimer / MathF.Max( 0.001f, ComboShowTime ) );
			var col = new Color( ComboColor.r, ComboColor.g, ComboColor.b, ComboColor.a * (0.50f + 0.50f * a) );

			string txt = $"x{_comboCount}";

			hud.DrawText( new TextRendering.Scope( txt, new Color( 0, 0, 0, 0.35f * a ), 18f * s ), new Vector2( x + w - (76f * s) + 1f, y - (18f * s) + 1f ) );
			hud.DrawText( new TextRendering.Scope( txt, col, 18f * s ), new Vector2( x + w - (76f * s), y - (18f * s) ) );
		}
	}

	private void DrawCoins( HudPainter hud, Vector2 offset )
	{
		if ( _playerStats == null ) return;

		float s = LeftHudScale;

		float killsX;
		float killsY;

		if ( AutoStackLeftPanels )
		{
			float gap = LeftStackGap * s;
			killsX = HPPosition.x + offset.x * 0.2f;
			killsY = (HPPosition.y + (HPPanelHeight * s) + gap + (LevelPanelHeight * s) + gap) + offset.y * 0.2f;
		}
		else
		{
			killsX = StatsKillsPosition.x + offset.x * 0.2f;
			killsY = StatsKillsPosition.y + offset.y * 0.2f;
		}

		float x = killsX;
		float y = killsY + (KillsPanelHeight * s) + (CoinsGap * s);

		float w = CoinsPanelWidth * s;
		float h = CoinsPanelHeight * s;

		DrawPanel( hud, x, y, w, h, s );

		hud.DrawText( new TextRendering.Scope( L( "#gamehud.coins.label", "🪙 МОНЕТ", "🪙 COINS" ), MutedText, SmallTextSize * s ), new Vector2( x + (12f * s), y + (10f * s) ) );
		hud.DrawText( new TextRendering.Scope( $"{_playerStats.Coins}", TextColor, BigTextSize * s ), new Vector2( x + w - (46f * s), y + (6f * s) ) );

		DrawMagnetWidget( hud, x, y, w, h, s );
	}

	private void DrawMagnetWidget( HudPainter hud, float coinsX, float coinsY, float coinsW, float coinsH, float scale )
	{
		if ( !MagnetWidgetEnabled ) return;

		var magnet = _magnetAbility;
		if ( magnet == null || !magnet.IsValid || !magnet.Unlocked ) return;

		float x = coinsX;
		float y = coinsY + coinsH + (MagnetWidgetGap * scale);
		float w = coinsW;
		float h = MagnetWidgetHeight * scale;

		DrawPanel( hud, x, y, w, h, scale );

		bool active = magnet.IsActive;
		Color fill = active ? MagnetWidgetFillActive : MagnetWidgetFillReady;
		float progress = magnet.GetProgress01();
		float seconds = magnet.GetDisplaySeconds();

		hud.DrawText( new TextRendering.Scope( "🧲", TextColor, 20f * scale ), new Rect( x + (8f * scale), y + (6f * scale), 28f * scale, 28f * scale ), TextFlag.Center );

		float barX = x + (38f * scale);
		float barY = y + (18f * scale);
		float barW = w - (86f * scale);
		float barH = 10f * scale;

		DrawRoundedRect( hud, barX, barY, barW, barH, MagnetWidgetBarBg, 6f * scale, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, barX, barY, barW * Clamp01( progress ), barH, fill, 6f * scale, 0f, new Color( 0, 0, 0, 0 ) );

		string timerText = $"{MathF.Ceiling( seconds )}{L( "#gamehud.common.seconds_suffix", "с", "s" )}";
		Color timerColor = active ? new Color( 0.70f, 1.00f, 0.78f, 0.98f ) : new Color( 0.78f, 0.90f, 1.00f, 0.96f );

		hud.DrawText( new TextRendering.Scope( timerText, timerColor, 16f * scale ), new Rect( x + w - (48f * scale), y + (10f * scale), 40f * scale, 20f * scale ), TextFlag.Right );
	}

	// =========================
	// RIGHT PANEL
	// =========================
	private void DrawAbilities( HudPainter hud, Vector2 offset )
	{
		var active = _abilityLevels.Where( kv => kv.Value > 0 ).ToList();
		if ( active.Count <= 0 ) return;

		float s = RightHudScale;

		float w = AbilitiesPanelWidth * s;

		float x = Screen.Width - w - (AbilitiesOffsetFromRightTop.x * s);
		float y = AbilitiesOffsetFromRightTop.y * s;

		x += offset.x * 0.15f;
		y += offset.y * 0.15f;

		float rowH = 22f * s;
		float headerH = 36f * s;
		float h = headerH + (10f * s) + active.Count * rowH + (12f * s);

		DrawPanel( hud, x, y, w, h, s );

		var title = new TextRendering.Scope( L( "#gamehud.abilities.title", "✨ УЛУЧШЕНИЯ", "✨ UPGRADES" ), TitleColor, TextSize * s );
		hud.DrawText( title, new Vector2( x + (12f * s), y + (10f * s) ) );

		for ( int i = 0; i < active.Count; i++ )
		{
			string text = GetAbilityDisplayText( active[i].Key, active[i].Value );
			var scope = new TextRendering.Scope( text, TextColor, SmallTextSize * s );
			float yy = y + headerH + i * rowH;
			hud.DrawText( scope, new Vector2( x + (14f * s), yy ) );
		}
	}


	private void DrawDemonWarning( HudPainter hud )
	{
		string text = GetDemonWarningText();
		if ( string.IsNullOrWhiteSpace( text ) )
			return;

		float s = CenterHudScale;

		float w = DemonWarningWidth * s;
		float h = DemonWarningHeight * s;

		float x = (Screen.Width * 0.5f) - (w * 0.5f);
		float y = GetCenterOverlayBottomY() + (DemonWarningTopGap * s);

		DrawRoundedRect( hud, x + (2f * s), y + (3f * s), w, h, DemonWarningShadow, DemonWarningRadius * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, x, y, w, h, DemonWarningBg, DemonWarningRadius * s, 1.6f * s, DemonWarningBorderColor );

		hud.DrawText(
			new TextRendering.Scope( text, DemonWarningTextColor, DemonWarningTextSize * s ),
			new Rect( x + (16f * s), y + (2f * s), w - (32f * s), h ),
			TextFlag.Center
		);
	}


	// =========================
	// CENTER MESSAGES
	// =========================
	private void DrawCenterMessages( HudPainter hud )
	{
		if ( _gameManager == null ) return;

		float alpha, timeLeft;
		Color col;

		if ( _gameManager.CurrentState == GameManager.GameState.Waiting )
		{
			alpha = 0.55f + 0.45f * MathF.Sin( Time.Now * 3f );
			col = new Color( WaitColor.r, WaitColor.g, WaitColor.b, alpha );
			DrawCenterBanner( hud, L( "#gamehud.center.waiting", "НАЖМИ F, ЧТОБЫ НАЧАТЬ БОЙ", "PRESS F TO START THE FIGHT" ), col );
		}
		else if ( _gameManager.CurrentState == GameManager.GameState.Preparing )
		{
			timeLeft = _gameManager.GetPrepareTimeLeft();
			DrawCenterBanner( hud, F( "#gamehud.center.preparing", "ПОДГОТОВКА: {0:F1} сек", "PREPARING: {0:F1} s", timeLeft ), PrepareColor );
		}
		else if ( _gameManager.CurrentState == GameManager.GameState.Fighting && _showFightMessage )
		{
			DrawCenterBanner( hud, L( "#gamehud.center.fight", "⚔️ БОЙ!!!", "⚔️ FIGHT!!!" ), FightColor );
		}
	}

	private void DrawCenterBanner( HudPainter hud, string text, Color textColor )
	{
		float s = CenterHudScale;

		float bw = CenterBannerWidth * s;
		float bh = CenterBannerHeight * s;

		float bx = (Screen.Width * 0.5f) - (bw * 0.5f);
		float by = CenterBannerTopOffset * s;

		DrawRoundedRect( hud, bx + (3f * s), by + (4f * s), bw, bh, CenterBannerShadow, CenterBannerRadius * s, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, bx, by, bw, bh, CenterBannerBg, CenterBannerRadius * s, CenterBannerBorder * s, CenterBannerBorderColor );

		hud.DrawRect( new Rect( bx + (10f * s), by + (12f * s), bw - (20f * s), 2f * s ), new Color( 1f, 1f, 1f, 0.06f ) );

		hud.DrawText(
			new TextRendering.Scope( text, textColor, CenterTextSize * s ),
			new Rect( bx, by + (2f * s), bw, bh ),
			TextFlag.Center
		);
	}

	// =========================
	// KILL POPUPS
	// =========================
	private void DrawKillPopups( HudPainter hud )
	{
		if ( !KillPopupsEnabled ) return;
		if ( _killPopups.Count == 0 ) return;

		for ( int i = 0; i < _killPopups.Count; i++ )
		{
			var p = _killPopups[i];
			float a = Clamp01( p.TimeLeft / MathF.Max( 0.001f, KillPopupLife ) );

			float scale = 1.0f + (0.20f * a);
			float size = p.Size * scale;

			var col = new Color( KillPopupColor.r, KillPopupColor.g, KillPopupColor.b, KillPopupColor.a * a );
			var shadow = new Color( KillPopupShadow.r, KillPopupShadow.g, KillPopupShadow.b, KillPopupShadow.a * a );

			var shadowScope = new TextRendering.Scope( p.Text, shadow, size );
			var textScope = new TextRendering.Scope( p.Text, col, size );

			hud.DrawText( shadowScope, new Vector2( p.Pos.x + 1f, p.Pos.y + 1f ) );
			hud.DrawText( textScope, p.Pos );
		}
	}

	// =========================
	// UI HELPERS
	// =========================
	private void DrawPanel( HudPainter hud, float x, float y, float w, float h, float scale )
	{
		DrawRoundedRect( hud, x + (ShadowOffset * scale), y + (ShadowOffset * scale), w, h, PanelShadow, CornerRadius * scale, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, x, y, w, h, PanelBg, CornerRadius * scale, BorderWidth * scale, BorderColor );
	}

	private void DrawRoundedRect( HudPainter hud, float x, float y, float w, float h, Color fill, float radius, float border, Color borderColor )
	{
		var r = new Rect( x, y, w, h );
		var corner = new Vector4( radius, radius, radius, radius );
		var bw = new Vector4( border, border, border, border );
		hud.DrawRect( r, fill, corner, bw, borderColor );
	}

	private static float Clamp01( float v )
	{
		if ( v < 0f ) return 0f;
		if ( v > 1f ) return 1f;
		return v;
	}
}
