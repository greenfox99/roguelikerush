using Sandbox;
using Sandbox.UI;
using System;
using System.Linq;
using YourGame.UI;

public sealed class SkillChestMenu : PanelComponent
{
	[Property, ImageAssetPath] public string ClosedChestImage { get; set; }
	[Property, ImageAssetPath] public string OpenChestImage { get; set; }

	[Property] public bool BlockEscape { get; set; } = true;
	[Property] public bool ForceMouseVisible { get; set; } = true;

	[Property, Group( "Animation" )] public float RollDuration { get; set; } = 2.6f;
	[Property, Group( "Animation" )] public float MinPreviewStep { get; set; } = 0.05f;
	[Property, Group( "Animation" )] public float MaxPreviewStep { get; set; } = 0.22f;

	[Property, Group( "Sell Price" )] public int MinSellPrice { get; set; } = 10;
	[Property, Group( "Sell Price" )] public int MaxSellPrice { get; set; } = 200;
	[Property, Group( "Sell Price" )] public float MaxLuckForBestPrice { get; set; } = 90f;

	[Property, Group( "Audio" )] public SoundEvent ChestOpenSound { get; set; }
	[Property, Group( "Audio" )] public SoundEvent ChestSellSound { get; set; }
	[Property, Group( "Audio" )] public SoundEvent EpicRewardSound { get; set; }

	[Property, Group( "Epic Shake" )] public float EpicShakeDuration { get; set; } = 8f;
	[Property, Group( "Epic Shake" )] public float EpicShakePositionAmplitude { get; set; } = 7f;
	[Property, Group( "Epic Shake" )] public float EpicShakeRotationAmplitude { get; set; } = 3.0f;
	[Property, Group( "Epic Shake" )] public float EpicShakeFrequency { get; set; } = 18f;

	[Property, Group( "Colors" )] public Color CommonColor { get; set; } = new Color( 1f, 1f, 1f, 0.85f );
	[Property, Group( "Colors" )] public Color RareColor { get; set; } = new Color( 0.35f, 0.85f, 1f, 0.95f );
	[Property, Group( "Colors" )] public Color EpicColor { get; set; } = new Color( 0.85f, 0.40f, 1f, 1f );

	private Panel _overlay;
	private Panel _window;
	private Panel _chestImage;
	private Panel _rewardCard;

	private Panel _openButton;
	private Panel _sellButton;

	private Label _title;
	private Label _luck;
	private Label _chance;
	private Label _sellPriceLabel;

	private Label _rewardIcon;
	private Label _rewardName;
	private Label _rewardDesc;

	private Label _openButtonLabel;
	private Label _sellButtonLabel;

	private SkillChest _activeChest;
	private LevelUpMenu _levelUpMenu;
	private ChestRarityConfig _chestRarity;
	private PlayerLuck _playerLuck;
	private PlayerStats _playerStats;
	private ChestCameraShake _cameraShake;

	private AbilityOption _previewReward;
	private AbilityOption _finalReward;

	private bool _visible;
	private bool _rolling;
	private float _rollStartReal;
	private float _nextPreviewReal;

	protected override void OnTreeFirstBuilt()
	{
		base.OnTreeFirstBuilt();
		GameLocalization.EnsureLoaded();
		BuildUi();
		SetVisible( false );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !_visible )
			return;

		if ( BlockEscape && Input.EscapePressed )
			Input.EscapePressed = false;

		if ( ForceMouseVisible )
			Mouse.Visibility = MouseVisibility.Visible;

		if ( _rolling )
			UpdateRollAnimation();
	}

	protected override void OnDisabled()
	{
		ForceHideMenuState();
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		ForceHideMenuState();
		base.OnDestroy();
	}

	public void OpenChest( SkillChest chest )
	{
		if ( chest == null || !chest.IsValid || chest.IsOpened )
			return;

		_levelUpMenu ??= Scene.GetAllComponents<LevelUpMenu>().FirstOrDefault();
		_chestRarity ??= Scene.GetAllComponents<ChestRarityConfig>().FirstOrDefault();
		_playerLuck ??= Scene.GetAllComponents<PlayerLuck>().FirstOrDefault();
		_playerStats ??= Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		_cameraShake ??= Scene.GetAllComponents<ChestCameraShake>().FirstOrDefault();

		if ( _levelUpMenu == null )
		{
			Log.Warning( T( "skillchest.warn.levelup_missing", "SkillChestMenu: LevelUpMenu не найден в сцене.", "SkillChestMenu: LevelUpMenu not found in scene." ) );
			return;
		}

		if ( !_levelUpMenu.PauseForExternalMenu() )
			return;

		StopEpicShakeImmediate();

		_activeChest = chest;
		_previewReward = null;
		_finalReward = null;
		_rolling = false;

		float luckPercent = _playerLuck?.LuckPercent ?? 0f;
		int sellPrice = GetSellPrice( luckPercent );

		_title.Text = T( "skillchest.title", "СУНДУК НАВЫКА", "SKILL CHEST" );
		_luck.Text = string.Format( T( "skillchest.luck", "Удача: {0:0.#}%", "Luck: {0:0.#}%" ), luckPercent );

		string chanceText = _chestRarity != null
			? _chestRarity.GetChanceText( luckPercent )
			: T( "skillchest.chance_not_configured", "Шансы не настроены", "Chances are not configured" );
		_chance.Text = LocalizeMaybeToken( chanceText );

		_sellPriceLabel.Text = string.Format( T( "skillchest.sell_price", "Продать за: {0} 🪙", "Sell for: {0} 🪙" ), sellPrice );

		_rewardIcon.Text = "❔";
		_rewardName.Text = T( "skillchest.unknown_reward.name", "Неизвестная награда", "Unknown reward" );
		_rewardDesc.Text = T( "skillchest.unknown_reward.desc", "Открой сундук, чтобы получить случайное улучшение, или продай его за монеты", "Open the chest to get a random upgrade, or sell it for coins" );

		_openButtonLabel.Text = T( "skillchest.button.open", "ОТКРЫТЬ", "OPEN" );
		_sellButtonLabel.Text = T( "skillchest.button.sell", "ПРОДАТЬ", "SELL" );

		SetOpenButtonEnabled( true );
		SetSellButtonEnabled( true );

		if ( !string.IsNullOrWhiteSpace( ClosedChestImage ) )
			_chestImage.Style.SetBackgroundImage( ClosedChestImage );
		else
			_chestImage.Style.Set( "background-image", "none" );

		ApplyRewardCardStyle( AbilityRarity.Common );
		SetVisible( true );
	}

	private void BuildUi()
	{
		Panel.Style.Set( "width", "100%" );
		Panel.Style.Set( "height", "100%" );
		Panel.Style.Set( "display", "none" );
		Panel.Style.Set( "pointer-events", "none" );
		Panel.Style.Dirty();

		_overlay = new Panel { Parent = Panel };
		_overlay.Style.Set( "position", "absolute" );
		_overlay.Style.Set( "left", "0" );
		_overlay.Style.Set( "top", "0" );
		_overlay.Style.Set( "width", "100%" );
		_overlay.Style.Set( "height", "100%" );
		_overlay.Style.Set( "display", "none" );
		_overlay.Style.Set( "background-color", "rgba(0,0,0,0.72)" );
		_overlay.Style.Set( "pointer-events", "none" );
		_overlay.Style.Dirty();

		_window = new Panel { Parent = _overlay };
		_window.Style.Set( "position", "absolute" );
		_window.Style.Set( "left", "50%" );
		_window.Style.Set( "top", "50%" );
		_window.Style.Set( "width", "900px" );
		_window.Style.Set( "height", "620px" );
		_window.Style.Set( "transform", "translate(-50%, -50%)" );
		_window.Style.Set( "background-color", "rgba(18,20,28,0.96)" );
		_window.Style.Set( "border-radius", "24px" );
		_window.Style.Set( "border-width", "2px" );
		_window.Style.Set( "border-color", "rgba(255,255,255,0.08)" );
		_window.Style.Set( "pointer-events", "all" );
		_window.Style.Dirty();

		_title = new Label { Parent = _window, Text = T( "skillchest.title", "СУНДУК НАВЫКА", "SKILL CHEST" ) };
		_title.Style.Set( "position", "absolute" );
		_title.Style.Set( "left", "0" );
		_title.Style.Set( "top", "28px" );
		_title.Style.Set( "width", "100%" );
		_title.Style.Set( "text-align", "center" );
		_title.Style.Set( "font-size", "34px" );
		_title.Style.Set( "font-weight", "800" );
		_title.Style.Set( "font-color", "rgba(255,230,110,1)" );
		_title.Style.Dirty();

		_luck = new Label { Parent = _window, Text = T( "skillchest.luck_default", "Удача: 0%", "Luck: 0%" ) };
		_luck.Style.Set( "position", "absolute" );
		_luck.Style.Set( "left", "0" );
		_luck.Style.Set( "top", "78px" );
		_luck.Style.Set( "width", "100%" );
		_luck.Style.Set( "text-align", "center" );
		_luck.Style.Set( "font-size", "20px" );
		_luck.Style.Set( "font-color", "rgba(255,255,255,0.95)" );
		_luck.Style.Dirty();

		_chance = new Label { Parent = _window, Text = "" };
		_chance.Style.Set( "position", "absolute" );
		_chance.Style.Set( "left", "0" );
		_chance.Style.Set( "top", "108px" );
		_chance.Style.Set( "width", "100%" );
		_chance.Style.Set( "text-align", "center" );
		_chance.Style.Set( "font-size", "16px" );
		_chance.Style.Set( "font-color", "rgba(255,255,255,0.68)" );
		_chance.Style.Dirty();

		_sellPriceLabel = new Label { Parent = _window, Text = T( "skillchest.sell_price_default", "Продать за: 10 🪙", "Sell for: 10 🪙" ) };
		_sellPriceLabel.Style.Set( "position", "absolute" );
		_sellPriceLabel.Style.Set( "left", "0" );
		_sellPriceLabel.Style.Set( "top", "136px" );
		_sellPriceLabel.Style.Set( "width", "100%" );
		_sellPriceLabel.Style.Set( "text-align", "center" );
		_sellPriceLabel.Style.Set( "font-size", "20px" );
		_sellPriceLabel.Style.Set( "font-weight", "700" );
		_sellPriceLabel.Style.Set( "font-color", "rgba(255,210,80,1)" );
		_sellPriceLabel.Style.Dirty();

		_chestImage = new Panel { Parent = _window };
		_chestImage.Style.Set( "position", "absolute" );
		_chestImage.Style.Set( "left", "60px" );
		_chestImage.Style.Set( "top", "190px" );
		_chestImage.Style.Set( "width", "260px" );
		_chestImage.Style.Set( "height", "260px" );
		_chestImage.Style.Set( "background-repeat", "no-repeat" );
		_chestImage.Style.Set( "background-size", "100% 100%" );
		_chestImage.Style.Set( "background-position", "0 0" );
		_chestImage.Style.Set( "pointer-events", "none" );
		_chestImage.Style.Dirty();

		_rewardCard = new Panel { Parent = _window };
		_rewardCard.Style.Set( "position", "absolute" );
		_rewardCard.Style.Set( "right", "60px" );
		_rewardCard.Style.Set( "top", "190px" );
		_rewardCard.Style.Set( "width", "500px" );
		_rewardCard.Style.Set( "height", "260px" );
		_rewardCard.Style.Set( "background-color", "rgba(28,32,44,0.98)" );
		_rewardCard.Style.Set( "border-radius", "22px" );
		_rewardCard.Style.Set( "border-width", "3px" );
		_rewardCard.Style.Set( "border-color", "rgba(255,255,255,0.2)" );
		_rewardCard.Style.Set( "pointer-events", "none" );
		_rewardCard.Style.Dirty();

		_rewardIcon = new Label { Parent = _rewardCard, Text = "❔" };
		_rewardIcon.Style.Set( "position", "absolute" );
		_rewardIcon.Style.Set( "left", "0" );
		_rewardIcon.Style.Set( "top", "26px" );
		_rewardIcon.Style.Set( "width", "100%" );
		_rewardIcon.Style.Set( "text-align", "center" );
		_rewardIcon.Style.Set( "font-size", "72px" );
		_rewardIcon.Style.Set( "pointer-events", "none" );
		_rewardIcon.Style.Dirty();

		_rewardName = new Label { Parent = _rewardCard, Text = T( "skillchest.unknown_reward.name", "Неизвестная награда", "Unknown reward" ) };
		_rewardName.Style.Set( "position", "absolute" );
		_rewardName.Style.Set( "left", "20px" );
		_rewardName.Style.Set( "top", "128px" );
		_rewardName.Style.Set( "width", "460px" );
		_rewardName.Style.Set( "text-align", "center" );
		_rewardName.Style.Set( "font-size", "26px" );
		_rewardName.Style.Set( "font-weight", "700" );
		_rewardName.Style.Set( "font-color", "rgba(255,255,255,0.98)" );
		_rewardName.Style.Set( "pointer-events", "none" );
		_rewardName.Style.Dirty();

		_rewardDesc = new Label { Parent = _rewardCard, Text = "" };
		_rewardDesc.Style.Set( "position", "absolute" );
		_rewardDesc.Style.Set( "left", "24px" );
		_rewardDesc.Style.Set( "top", "170px" );
		_rewardDesc.Style.Set( "width", "452px" );
		_rewardDesc.Style.Set( "text-align", "center" );
		_rewardDesc.Style.Set( "font-size", "18px" );
		_rewardDesc.Style.Set( "font-color", "rgba(255,255,255,0.72)" );
		_rewardDesc.Style.Set( "pointer-events", "none" );
		_rewardDesc.Style.Dirty();

		_openButton = new Panel { Parent = _window };
		_openButton.Style.Set( "position", "absolute" );
		_openButton.Style.Set( "left", "220px" );
		_openButton.Style.Set( "bottom", "42px" );
		_openButton.Style.Set( "width", "220px" );
		_openButton.Style.Set( "height", "62px" );
		_openButton.Style.Set( "background-color", "rgba(35,140,70,0.95)" );
		_openButton.Style.Set( "border-radius", "14px" );
		_openButton.Style.Set( "border-width", "2px" );
		_openButton.Style.Set( "border-color", "rgba(140,255,170,0.6)" );
		_openButton.Style.Set( "pointer-events", "all" );
		_openButton.Style.Dirty();
		_openButton.AddEventListener( "onclick", OnOpenButtonClicked );

		_openButtonLabel = new Label { Parent = _openButton, Text = T( "skillchest.button.open", "ОТКРЫТЬ", "OPEN" ) };
		_openButtonLabel.Style.Set( "width", "100%" );
		_openButtonLabel.Style.Set( "height", "100%" );
		_openButtonLabel.Style.Set( "text-align", "center" );
		_openButtonLabel.Style.Set( "font-size", "22px" );
		_openButtonLabel.Style.Set( "font-weight", "800" );
		_openButtonLabel.Style.Set( "font-color", "rgba(255,255,255,1)" );
		_openButtonLabel.Style.Set( "padding-top", "16px" );
		_openButtonLabel.Style.Set( "pointer-events", "none" );
		_openButtonLabel.Style.Dirty();

		_sellButton = new Panel { Parent = _window };
		_sellButton.Style.Set( "position", "absolute" );
		_sellButton.Style.Set( "right", "220px" );
		_sellButton.Style.Set( "bottom", "42px" );
		_sellButton.Style.Set( "width", "220px" );
		_sellButton.Style.Set( "height", "62px" );
		_sellButton.Style.Set( "background-color", "rgba(160,110,30,0.96)" );
		_sellButton.Style.Set( "border-radius", "14px" );
		_sellButton.Style.Set( "border-width", "2px" );
		_sellButton.Style.Set( "border-color", "rgba(255,215,120,0.7)" );
		_sellButton.Style.Set( "pointer-events", "all" );
		_sellButton.Style.Dirty();
		_sellButton.AddEventListener( "onclick", OnSellButtonClicked );

		_sellButtonLabel = new Label { Parent = _sellButton, Text = T( "skillchest.button.sell", "ПРОДАТЬ", "SELL" ) };
		_sellButtonLabel.Style.Set( "width", "100%" );
		_sellButtonLabel.Style.Set( "height", "100%" );
		_sellButtonLabel.Style.Set( "text-align", "center" );
		_sellButtonLabel.Style.Set( "font-size", "22px" );
		_sellButtonLabel.Style.Set( "font-weight", "800" );
		_sellButtonLabel.Style.Set( "font-color", "rgba(255,255,255,1)" );
		_sellButtonLabel.Style.Set( "padding-top", "16px" );
		_sellButtonLabel.Style.Set( "pointer-events", "none" );
		_sellButtonLabel.Style.Dirty();
	}


	private string T( string key, string russianFallback, string englishFallback )
	{
		GameLocalization.EnsureLoaded();
		return GameLocalization.T( key, GameLocalization.IsLanguage( "ru" ) ? russianFallback : englishFallback );
	}

	private string LocalizeMaybeToken( string text )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return string.Empty;

		if ( text.StartsWith( "#", StringComparison.Ordinal ) )
			return GameLocalization.T( text, text );

		return text;
	}

	private string GetRewardKey( AbilityOption reward )
	{
		if ( reward == null )
			return string.Empty;

		string name = reward.Name ?? string.Empty;
		string icon = reward.Icon ?? string.Empty;

		if ( name == "Регенерация" || name == "Regeneration" || icon == "❤️" ) return "regen";
		if ( name == "Увеличение HP" || name == "Max HP Up" || icon == "💪" ) return "maxhp";
		if ( name == "Щит" || name == "Shield" || icon == "🛡️" ) return "shield";
		if ( name == "Урон +5" || name == "Damage +5" || icon == "⚡" ) return "damage5";
		if ( name == "Скорость атаки" || name == "Attack Speed" || icon == "⚔️" ) return "atkspd";
		if ( name == "Мульти-луч" || name == "Multi Beam" || icon == "⚡⚡" ) return "multitarget";
		if ( name == "Рикошет" || name == "Ricochet" || icon == "🪃" ) return "ricochet";
		if ( name == "Дальность луча" || name == "Beam Range" || icon == "🎯" ) return "range";
		if ( name == "Критический разряд" || name == "Critical Discharge" || icon == "✨" ) return "crit";
		if ( name == "Добивание" || name == "Execution" || icon == "🗡️" ) return "execute";
		if ( name == "Поток убийств" || name == "Kill Flow" || icon == "📉" ) return "killflow";
		if ( name == "Усиление крита" || name == "Critical Boost" || icon == "💥" ) return "critboost";
		if ( name == "Калибровка потока" || name == "Flow Calibration" || icon == "🧠" ) return "flowcalibration";
		if ( name == "Скорость бега" || name == "Run Speed" || icon == "🏃" ) return "runspeed";
		if ( name == "Реген стамины" || name == "Stamina Regen" || icon == "🔋" ) return "stam_regen";
		if ( name == "Экономия стамины" || name == "Stamina Efficiency" || icon == "🧃" ) return "stam_drain";
		if ( name == "Удача" || name == "Luck" || icon == "🍀" ) return "luck";
		if ( name == "Большая удача" || name == "Great Luck" || icon == "✨🍀" ) return "bigluck";
		if ( name == "Благословение удачи" || name == "Luck Blessing" || icon == "🌟🍀" ) return "luckblessing";
		if ( name == "Берсерк" || name == "Berserk" || icon == "😡" ) return "berserk";
		if ( name == "Лучевая болезнь" || name == "Radiation Sickness" || icon == "☣️" ) return "radiation";
		if ( name == "Золотая лихорадка" || name == "Gold Fever" || icon == "🪙" ) return "goldfever";
		if ( name == "Магнит" || name == "Magnet" || icon == "🧲" ) return "magnet";

		return string.Empty;
	}

	private string GetRewardDisplayName( AbilityOption reward )
	{
		if ( reward == null )
			return T( "skillchest.unknown_reward.name", "Неизвестная награда", "Unknown reward" );

		string key = GetRewardKey( reward );

		return key switch
		{
			"regen" => T( "skillchest.reward.regen.name", "Регенерация", "Regeneration" ),
			"maxhp" => T( "skillchest.reward.maxhp.name", "Увеличение HP", "Max HP Up" ),
			"shield" => T( "skillchest.reward.shield.name", "Щит", "Shield" ),
			"damage5" => T( "skillchest.reward.damage5.name", "Урон +5", "Damage +5" ),
			"atkspd" => T( "skillchest.reward.atkspd.name", "Скорость атаки", "Attack Speed" ),
			"multitarget" => T( "skillchest.reward.multitarget.name", "Мульти-луч", "Multi Beam" ),
			"ricochet" => T( "skillchest.reward.ricochet.name", "Рикошет", "Ricochet" ),
			"range" => T( "skillchest.reward.range.name", "Дальность луча", "Beam Range" ),
			"crit" => T( "skillchest.reward.crit.name", "Критический разряд", "Critical Discharge" ),
			"execute" => T( "skillchest.reward.execute.name", "Добивание", "Execution" ),
			"killflow" => T( "skillchest.reward.killflow.name", "Поток убийств", "Kill Flow" ),
			"critboost" => T( "skillchest.reward.critboost.name", "Усиление крита", "Critical Boost" ),
			"flowcalibration" => T( "skillchest.reward.flowcalibration.name", "Калибровка потока", "Flow Calibration" ),
			"runspeed" => T( "skillchest.reward.runspeed.name", "Скорость бега", "Run Speed" ),
			"stam_regen" => T( "skillchest.reward.stam_regen.name", "Реген стамины", "Stamina Regen" ),
			"stam_drain" => T( "skillchest.reward.stam_drain.name", "Экономия стамины", "Stamina Efficiency" ),
			"luck" => T( "skillchest.reward.luck.name", "Удача", "Luck" ),
			"bigluck" => T( "skillchest.reward.bigluck.name", "Большая удача", "Great Luck" ),
			"luckblessing" => T( "skillchest.reward.luckblessing.name", "Благословение удачи", "Luck Blessing" ),
			"berserk" => T( "skillchest.reward.berserk.name", "Берсерк", "Berserk" ),
			"radiation" => T( "skillchest.reward.radiation.name", "Лучевая болезнь", "Radiation Sickness" ),
			"goldfever" => T( "skillchest.reward.goldfever.name", "Золотая лихорадка", "Gold Fever" ),
			"magnet" => T( "skillchest.reward.magnet.name", "Магнит", "Magnet" ),
			_ => reward.Name ?? "???"
		};
	}

	private string GetRewardDisplayDescription( AbilityOption reward )
	{
		if ( reward == null )
			return T( "skillchest.unknown_reward.desc", "Открой сундук, чтобы получить случайное улучшение, или продай его за монеты", "Open the chest to get a random upgrade, or sell it for coins" );

		string key = GetRewardKey( reward );

		return key switch
		{
			"regen" => T( "skillchest.reward.regen.desc", "1 HP раз в 4 секунды", "1 HP every 4 seconds" ),
			"maxhp" => T( "skillchest.reward.maxhp.desc", "Максимальное здоровье +20", "Maximum health +20" ),
			"shield" => T( "skillchest.reward.shield.desc", "Щит +5 (стак), реген. 15с", "Shield +5 (stack), 15s regen" ),
			"damage5" => T( "skillchest.reward.damage5.desc", "Увеличивает урон на 5", "Increases damage by 5" ),
			"atkspd" => T( "skillchest.reward.atkspd.desc", "Уменьшает кулдаун на 10%", "Reduces cooldown by 10%" ),
			"multitarget" => T( "skillchest.reward.multitarget.desc", "Бьет по нескольким целям", "Hits multiple targets" ),
			"ricochet" => T( "skillchest.reward.ricochet.desc", "Луч перескакивает на ещё 1 цель рядом", "Beam bounces to 1 extra nearby target" ),
			"range" => T( "skillchest.reward.range.desc", "Увеличивает радиус атаки на 5%", "Increases attack range by 5%" ),
			"crit" => T( "skillchest.reward.crit.desc", "Шанс крита +5% (x2 урон)", "Crit chance +5% (x2 damage)" ),
			"execute" => T( "skillchest.reward.execute.desc", "+15% урона по врагам ниже 30% HP", "+15% damage to enemies below 30% HP" ),
			"killflow" => T( "skillchest.reward.killflow.desc", "За килл посохом -0.1% КД (до -30%)", "Staff kills give -0.1% cooldown (up to -30%)" ),
			"critboost" => T( "skillchest.reward.critboost.desc", "Множитель крита +0.25 (до x4.0)", "Crit multiplier +0.25 (up to x4.0)" ),
			"flowcalibration" => T( "skillchest.reward.flowcalibration.desc", "+0.05% КД за килл (до 0.30%/килл)", "+0.05% cooldown per kill (up to 0.30%/kill)" ),
			"runspeed" => T( "skillchest.reward.runspeed.desc", "Скорость +10% (стак)", "Move speed +10% (stack)" ),
			"stam_regen" => T( "skillchest.reward.stam_regen.desc", "Реген стамины +15% (стак)", "Stamina regen +15% (stack)" ),
			"stam_drain" => T( "skillchest.reward.stam_drain.desc", "Расход стамины -10% (стак)", "Stamina drain -10% (stack)" ),
			"luck" => T( "skillchest.reward.luck.desc", "Удача +2%", "Luck +2%" ),
			"bigluck" => T( "skillchest.reward.bigluck.desc", "Удача +8%", "Luck +8%" ),
			"luckblessing" => T( "skillchest.reward.luckblessing.desc", "Удача +15%", "Luck +15%" ),
			"berserk" => T( "skillchest.reward.berserk.desc", "+К урону и скорости атаки при получении урона", "+damage and attack speed when taking damage" ),
			"radiation" => T( "skillchest.reward.radiation.desc", "Шанс нанести (отравляющий) урон", "Chance to apply lingering damage" ),
			"goldfever" => T( "skillchest.reward.goldfever.desc", "Больше золота за килы", "More gold for kills" ),
			"magnet" => T( "skillchest.reward.magnet.desc", "Каждые 145 сек притягивает монеты и EXP со всей карты", "Every 145 sec pulls coins and EXP from the whole map" ),
			_ => reward.Description ?? string.Empty
		};
	}

	private void OnOpenButtonClicked()
	{
		if ( !_visible || _rolling )
			return;

		if ( _finalReward == null )
		{
			BeginRoll();
			return;
		}

		TakeReward();
	}

	private void OnSellButtonClicked()
	{
		if ( !_visible || _rolling )
			return;

		SellChest();
	}

	private void BeginRoll()
	{
		if ( _levelUpMenu == null )
			return;

		PlaySoundOnChest( ChestOpenSound );

		_rolling = true;
		_rollStartReal = RealTime.Now;
		_nextPreviewReal = RealTime.Now;

		_openButtonLabel.Text = T( "skillchest.button.opening", "ОТКРЫВАЕТСЯ...", "OPENING..." );
		SetOpenButtonEnabled( false );
		SetSellButtonEnabled( false );
	}

	private void UpdateRollAnimation()
	{
		float elapsed = RealTime.Now - _rollStartReal;
		float t = elapsed / MathF.Max( 0.001f, RollDuration );
		if ( t > 1f ) t = 1f;

		if ( RealTime.Now >= _nextPreviewReal )
		{
			float luckPercent = _playerLuck?.LuckPercent ?? 0f;
			_previewReward = _levelUpMenu.RollSingleAbilityForChest( _chestRarity, luckPercent );

			if ( _previewReward != null )
				UpdateRewardVisual( _previewReward );

			float eased = t * t;
			float step = Lerp( MinPreviewStep, MaxPreviewStep, eased );
			_nextPreviewReal = RealTime.Now + step;
		}

		float pulseA = 0.18f + MathF.Sin( RealTime.Now * 18f ) * 0.08f;
		_rewardCard.Style.Set( "background-color", $"rgba(40,45,62,{0.88f + pulseA:0.###})" );
		_rewardCard.Style.Dirty();

		if ( t >= 1f )
			FinishRoll();
	}

	private void FinishRoll()
	{
		_rolling = false;

		float luckPercent = _playerLuck?.LuckPercent ?? 0f;
		_finalReward = _levelUpMenu.RollSingleAbilityForChest( _chestRarity, luckPercent );

		if ( _finalReward != null )
			UpdateRewardVisual( _finalReward );

		if ( !string.IsNullOrWhiteSpace( OpenChestImage ) )
			_chestImage.Style.SetBackgroundImage( OpenChestImage );

		if ( _finalReward != null && _finalReward.BaseRarity == AbilityRarity.Epic )
			PlayEpicRewardFx();

		_title.Text = T( "skillchest.reward_received", "НАГРАДА ПОЛУЧЕНА!", "REWARD RECEIVED!" );
		_openButtonLabel.Text = T( "skillchest.button.take", "ЗАБРАТЬ", "TAKE" );
		SetOpenButtonEnabled( true );

		SetSellButtonEnabled( false );
		_sellButton.Style.Set( "opacity", "0.35" );
		_sellButton.Style.Dirty();
	}

	private void TakeReward()
	{
		if ( _finalReward == null || _activeChest == null )
			return;

		StopEpicShakeImmediate();

		_levelUpMenu.ApplyAbilityDirect( _finalReward );
		_levelUpMenu.ResumeFromExternalMenu();

		var chest = _activeChest;
		_activeChest = null;

		SetVisible( false );
		chest.MarkOpenedAndConsume();
	}

	private void SellChest()
	{
		if ( _activeChest == null )
			return;

		StopEpicShakeImmediate();

		float luckPercent = _playerLuck?.LuckPercent ?? 0f;
		int sellPrice = GetSellPrice( luckPercent );

		PlaySoundOnChest( ChestSellSound );

		_playerStats ??= Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		_playerStats?.AddCoins( sellPrice );

		_levelUpMenu?.ResumeFromExternalMenu();

		var chest = _activeChest;
		_activeChest = null;

		SetVisible( false );

		if ( chest.SpawnManager != null )
			chest.SpawnManager.NotifyChestOpened( chest );

		if ( chest.GameObject != null && chest.GameObject.IsValid )
			chest.GameObject.Destroy();
	}

	private int GetSellPrice( float luckPercent )
	{
		if ( MaxLuckForBestPrice <= 0f )
			return MaxSellPrice;

		float t = luckPercent / MaxLuckForBestPrice;
		if ( t < 0f ) t = 0f;
		if ( t > 1f ) t = 1f;

		float price = Lerp( MinSellPrice, MaxSellPrice, t );
		return (int)MathF.Round( price );
	}

	private void UpdateRewardVisual( AbilityOption reward )
	{
		_rewardIcon.Text = string.IsNullOrWhiteSpace( reward.Icon ) ? "?" : reward.Icon;
		_rewardName.Text = GetRewardDisplayName( reward );
		_rewardDesc.Text = GetRewardDisplayDescription( reward );

		ApplyRewardCardStyle( reward.BaseRarity );
	}

	private void ApplyRewardCardStyle( AbilityRarity rarity )
	{
		Color c = rarity == AbilityRarity.Epic ? EpicColor :
				  rarity == AbilityRarity.Rare ? RareColor :
				  CommonColor;

		_rewardCard.Style.Set( "border-color", $"rgba({c.r * 255f:0},{c.g * 255f:0},{c.b * 255f:0},0.95)" );
		_rewardCard.Style.Dirty();

		_rewardName.Style.Set( "font-color", $"rgba({c.r * 255f:0},{c.g * 255f:0},{c.b * 255f:0},1)" );
		_rewardName.Style.Dirty();
	}

	private void SetOpenButtonEnabled( bool enabled )
	{
		_openButton.Style.Set( "pointer-events", enabled ? "all" : "none" );
		_openButton.Style.Set( "opacity", enabled ? "1" : "0.55" );
		_openButton.Style.Dirty();
	}

	private void SetSellButtonEnabled( bool enabled )
	{
		_sellButton.Style.Set( "pointer-events", enabled ? "all" : "none" );
		_sellButton.Style.Set( "opacity", enabled ? "1" : "0.55" );
		_sellButton.Style.Dirty();
	}

	private void SetVisible( bool visible )
	{
		_visible = visible;

		Panel.Style.Set( "display", visible ? "flex" : "none" );
		Panel.Style.Set( "pointer-events", visible ? "all" : "none" );
		Panel.Style.Dirty();

		if ( _overlay != null )
		{
			_overlay.Style.Set( "display", visible ? "flex" : "none" );
			_overlay.Style.Set( "pointer-events", visible ? "all" : "none" );
			_overlay.Style.Dirty();
		}

		if ( ForceMouseVisible )
			Mouse.Visibility = visible ? MouseVisibility.Visible : MouseVisibility.Hidden;

		if ( !visible )
		{
			StopEpicShakeImmediate();
			_rolling = false;
			_previewReward = null;
			_finalReward = null;
		}
	}

	private void ForceHideMenuState()
	{
		StopEpicShakeImmediate();
		_levelUpMenu?.ResumeFromExternalMenu();

		_visible = false;
		_rolling = false;
		_previewReward = null;
		_finalReward = null;
		_activeChest = null;

		if ( Panel != null )
		{
			Panel.Style.Set( "display", "none" );
			Panel.Style.Set( "pointer-events", "none" );
			Panel.Style.Dirty();
		}

		if ( _overlay != null )
		{
			_overlay.Style.Set( "display", "none" );
			_overlay.Style.Set( "pointer-events", "none" );
			_overlay.Style.Dirty();
		}

		if ( ForceMouseVisible )
			Mouse.Visibility = MouseVisibility.Hidden;
	}

	private void StopEpicShakeImmediate()
	{
		_cameraShake ??= Scene?.GetAllComponents<ChestCameraShake>().FirstOrDefault();
		_cameraShake?.Stop();
	}

	private void PlaySoundOnChest( SoundEvent sound )
	{
		if ( sound == null || _activeChest == null || !_activeChest.IsValid )
			return;

		_activeChest.GameObject.PlaySound( sound, Vector3.Zero );
	}

	private void PlayEpicRewardFx()
	{
		if ( EpicRewardSound != null )
			Sound.Play( EpicRewardSound, 0f );

		_cameraShake ??= Scene.GetAllComponents<ChestCameraShake>().FirstOrDefault();
		_cameraShake?.Play(
			EpicShakeDuration,
			EpicShakePositionAmplitude,
			EpicShakeRotationAmplitude,
			EpicShakeFrequency
		);
	}

	private static float Lerp( float a, float b, float t )
	{
		if ( t < 0f ) t = 0f;
		if ( t > 1f ) t = 1f;
		return a + (b - a) * t;
	}
}
