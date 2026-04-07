using Sandbox;
using Sandbox.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using YourGame.UI;

public sealed class LevelUpMenu : Component
{
	[Property] public PlayerHealth PlayerHealth { get; set; }
	[Property] public PlayerStamina PlayerStaminaRef { get; set; }
	[Property] public PlayerStaff PlayerStaff { get; set; }
	[Property] public PlayerLuck PlayerLuckRef { get; set; }

	// ✅ Новый компонент с GameManager (можно назначить в инспекторе, или он сам найдёт в сцене)
	[Property] public AbilityRarityConfig RarityConfig { get; set; }

	// ===== ВКЛ/ВЫКЛ ФИЧ =====
	[Property] public bool EnableIntroAnimation { get; set; } = true;
	[Property] public bool EnableRarity { get; set; } = true;
	[Property] public bool EnableSelectAnimation { get; set; } = true;
	[Property] public bool EnableSelectSound { get; set; } = true;

	// ⚠️ Виньетка больше не нужна (именно она давала "чёрные полосы")
	// Оставил настройку на будущее, но больше не используется в DrawMenu().
	[Property] public bool EnableVignette { get; set; } = false;
	[Property] public float VignetteStrength { get; set; } = 0.35f;

	// ===== BLUR BACKGROUND =====
	[Property] public bool EnableBackgroundBlur { get; set; } = true;

	// Сила блюра (0..1). Подбери по вкусу.
	[Property, Range( 0f, 1f )] public float BackgroundBlurSize { get; set; } = 0.23f;

	private Blur _blur;
	private float _oldBlurSize;
	private bool _oldBlurEnabled;
	private bool _blurWasPresent;
	private bool _oldPostProcessing;
	private CameraComponent _blurCamera;

	[Property] public SoundEvent SelectSound { get; set; }
	[Property] public string SelectSoundName { get; set; } = "";

	// ===== ВИД / НАСТРОЙКИ =====
	[Property] public float OverlayAlpha { get; set; } = 0.62f;

	[Property] public float PanelWidth { get; set; } = 980f;
	[Property] public float PanelHeight { get; set; } = 520f;
	[Property] public float PanelRadius { get; set; } = 18f;
	[Property] public float PanelBorder { get; set; } = 2f;

	[Property] public float ShadowOffsetX { get; set; } = 4f;
	[Property] public float ShadowOffsetY { get; set; } = 6f;

	[Property] public Color PanelBg { get; set; } = new Color( 0.08f, 0.10f, 0.13f, 0.92f );
	[Property] public Color PanelBorderColor { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property] public Color PanelShadow { get; set; } = new Color( 0f, 0f, 0f, 0.35f );

	[Property] public Color TitleColor { get; set; } = new Color( 1.00f, 0.92f, 0.20f, 1.0f );
	[Property] public Color SubtitleColor { get; set; } = new Color( 1f, 1f, 1f, 0.70f );

	// Карточки
	[Property] public float CardWidth { get; set; } = 260f;
	[Property] public float CardHeight { get; set; } = 280f;
	[Property] public float CardRadius { get; set; } = 16f;
	[Property] public float CardBorder { get; set; } = 2f;
	[Property] public float CardGap { get; set; } = 24f;

	[Property] public Color CardBg { get; set; } = new Color( 0.10f, 0.12f, 0.16f, 0.92f );
	[Property] public Color CardBgTop { get; set; } = new Color( 1f, 1f, 1f, 0.05f );
	[Property] public Color CardBorderColor { get; set; } = new Color( 1f, 1f, 1f, 0.08f );

	[Property] public Color KeyBadgeBg { get; set; } = new Color( 0f, 0f, 0f, 0.35f );
	[Property] public Color KeyBadgeBorder { get; set; } = new Color( 1f, 1f, 1f, 0.10f );
	[Property] public Color KeyBadgeText { get; set; } = new Color( 0.70f, 0.95f, 1f, 0.95f );

	[Property] public Color AbilityNameColor { get; set; } = new Color( 1f, 1f, 1f, 0.95f );
	[Property] public Color AbilityDescColor { get; set; } = new Color( 1f, 1f, 1f, 0.70f );

	[Property] public Color IconCircleBg { get; set; } = new Color( 1f, 1f, 1f, 0.06f );
	[Property] public Color IconCircleBorder { get; set; } = new Color( 1f, 1f, 1f, 0.08f );

	[Property] public float TitleSize { get; set; } = 46f;
	[Property] public float SubtitleSize { get; set; } = 18f;
	[Property] public float AbilityNameSize { get; set; } = 22f;
	[Property] public float AbilityDescSize { get; set; } = 16f;
	[Property] public float IconSize { get; set; } = 64f;

	[Property] public float HintSize { get; set; } = 18f;
	[Property] public Color HintColor { get; set; } = new Color( 1f, 1f, 1f, 0.65f );

	// ===== РЕДКОСТЬ =====
	[Property] public Color CommonColor { get; set; } = new Color( 1f, 1f, 1f, 0.18f );
	[Property] public Color RareColor { get; set; } = new Color( 0.35f, 0.85f, 1.00f, 0.85f );
	[Property] public Color EpicColor { get; set; } = new Color( 0.85f, 0.40f, 1.00f, 0.90f );

	[Property] public Color CommonGlow { get; set; } = new Color( 1f, 1f, 1f, 0.04f );
	[Property] public Color RareGlow { get; set; } = new Color( 0.35f, 0.85f, 1.00f, 0.10f );
	[Property] public Color EpicGlow { get; set; } = new Color( 0.85f, 0.40f, 1.00f, 0.12f );

	// ===== АНИМАЦИИ =====
	[Property] public float IntroTime { get; set; } = 0.22f;
	[Property] public float IntroScaleFrom { get; set; } = 0.94f;

	[Property] public float SelectAnimTime { get; set; } = 0.18f;
	[Property] public float SelectScaleTo { get; set; } = 1.06f;

	private bool _isMenuOpen = false;
	private List<AbilityOption> _currentOptions = new();
	private float _oldTimeScale = 1f;

	private int _lastLevelOpened = 0;

	private float _introStartReal = 0f;

	private bool _isSelecting = false;
	private int _selectedIndex = -1;
	private float _selectStartReal = 0f;

	private Random _rng;

	// ===== CHEST / EXTERNAL MENU =====
	private bool _externalPauseActive = false;

	// Блокирует ESC / pause menu, пока открыт LVLUP или внешнее меню награды
	public bool IsBlockingEscape => _isMenuOpen || _externalPauseActive;

	protected override void OnStart()
	{
		GameLocalization.EnsureLoaded();
		_rng = new Random( (int)(Time.Now * 1000f) ^ GameObject.Id.GetHashCode() );

		if ( RarityConfig == null )
			RarityConfig = Scene.GetAllComponents<AbilityRarityConfig>().FirstOrDefault();

		// ✅ Подхват стамины (чтобы PlayerStaminaRef не был null)
		if ( PlayerStaminaRef == null )
			PlayerStaminaRef = Scene.GetAllComponents<PlayerStamina>().FirstOrDefault();

		if ( PlayerLuckRef == null )
			PlayerLuckRef = Scene.GetAllComponents<PlayerLuck>().FirstOrDefault();

		var playerLevel = Components.Get<PlayerLevel>();
		if ( playerLevel == null )
			playerLevel = Scene.GetAllComponents<PlayerLevel>().FirstOrDefault();

		if ( playerLevel != null )
			playerLevel.OnLevelUp += OnLevelUp;
	}

	private void OnLevelUp( int newLevel )
	{
		if ( _externalPauseActive )
			return;

		_lastLevelOpened = newLevel;

		_currentOptions = GenerateRandomAbilities( 3, newLevel );
		_isMenuOpen = true;

		_isSelecting = false;
		_selectedIndex = -1;

		_introStartReal = RealTime.Now;

		_oldTimeScale = Scene.TimeScale;
		Scene.TimeScale = 0f;

		EnableBlur();
	}

	private void EnableBlur()
	{
		if ( !EnableBackgroundBlur ) return;
		if ( Scene?.Camera == null ) return;

		_blurCamera = Scene.Camera;

		// сохраняем состояние камеры
		_oldPostProcessing = _blurCamera.EnablePostProcessing;
		_blurCamera.EnablePostProcessing = true;

		// берём/создаём Blur на камере
		_blur = _blurCamera.Components.Get<Blur>();
		_blurWasPresent = _blur != null;

		if ( _blur == null )
			_blur = _blurCamera.Components.Create<Blur>( startEnabled: true );

		_oldBlurEnabled = _blur.Enabled;
		_oldBlurSize = _blur.Size;

		_blur.Enabled = true;
		_blur.Size = BackgroundBlurSize;
	}

	private void DisableBlur()
	{
		if ( !EnableBackgroundBlur ) return;
		if ( _blurCamera == null || !_blurCamera.IsValid ) return;

		_blurCamera.EnablePostProcessing = _oldPostProcessing;

		if ( _blur == null || !_blur.IsValid ) return;

		_blur.Size = _oldBlurSize;
		_blur.Enabled = _oldBlurEnabled;

		if ( !_blurWasPresent )
			_blur.Enabled = false;
	}

	private List<AbilityOption> BuildAllAbilities()
	{
		const float CritMultCap = 4.0f;
		const float KillCdrPerKillCap = 0.0030f;

		return new List<AbilityOption>
		{
			new AbilityOption { Name="Регенерация", Description="1 HP раз в 4 секунды", Icon="❤️", BaseRarity=AbilityRarity.Rare,
				Action=() => { if (PlayerHealth!=null) PlayerHealth.HealthRegenAmount += 1f; } },

			new AbilityOption { Name="Увеличение HP", Description="Максимальное здоровье +20", Icon="💪", BaseRarity=AbilityRarity.Common,
				Action=() => { if (PlayerHealth!=null) PlayerHealth.MaxHealth += 20; } },

			new AbilityOption { Name="Щит", Description="Щит +5 (стак), реген. 15с", Icon="🛡️", BaseRarity=AbilityRarity.Rare,
				Action=() => { if (PlayerHealth!=null) PlayerHealth.AddShieldLevel(); } },

			new AbilityOption { Name="Урон +5", Description="Увеличивает урон на 5", Icon="⚡", BaseRarity=AbilityRarity.Epic,
				Action=() => { if (PlayerStaff!=null) PlayerStaff.Damage += 5; } },

			new AbilityOption { Name="Скорость атаки", Description="Уменьшает кулдаун на 10%", Icon="⚔️", BaseRarity=AbilityRarity.Rare,
				Action=() => { if (PlayerStaff!=null) PlayerStaff.AttackCooldown *= 0.9f; } },

			new AbilityOption { Name="Мульти-луч", Description="Бьет по нескольким целям", Icon="⚡⚡", BaseRarity=AbilityRarity.Epic,
				Action=() => { if (PlayerStaff!=null) PlayerStaff.MaxTargets += 1; } },

			new AbilityOption { Name="Рикошет", Description="Луч перескакивает на ещё 1 цель рядом", Icon="🪃", BaseRarity=AbilityRarity.Epic,
				Action=() => { if (PlayerStaff!=null) PlayerStaff.RicochetBounces += 1; } },

			new AbilityOption { Name="Дальность луча", Description="Увеличивает радиус атаки на 5%", Icon="🎯", BaseRarity=AbilityRarity.Common,
				Action=() => { if (PlayerStaff!=null) PlayerStaff.RangeMultiplier += 0.05f; } },

			new AbilityOption { Name="Критический разряд", Description="Шанс крита +5% (x2 урон)", Icon="✨", BaseRarity=AbilityRarity.Rare,
				Action=() =>
				{
					if ( PlayerStaff == null ) return;
					PlayerStaff.CritChance = Clamp01( PlayerStaff.CritChance + 0.05f );
					if ( PlayerStaff.CritMultiplier < 2f ) PlayerStaff.CritMultiplier = 2f;
				} },

			new AbilityOption { Name="Добивание", Description="+15% урона по врагам ниже 30% HP", Icon="🗡️", BaseRarity=AbilityRarity.Rare,
				Action=() =>
				{
					if ( PlayerStaff == null ) return;
					PlayerStaff.EnableExecute = true;
					PlayerStaff.ExecuteHpThreshold = 0.30f;
					PlayerStaff.ExecuteBonusMultiplier = 1.15f;
				} },

			new AbilityOption { Name="Поток убийств", Description="За килл посохом -0.1% КД (до -30%)", Icon="📉", BaseRarity=AbilityRarity.Epic,
				Action=() =>
				{
					if ( PlayerStaff == null ) return;
					PlayerStaff.EnableKillCooldownStacks = true;
					PlayerStaff.KillCooldownReductionPerKill = 0.001f;
					PlayerStaff.KillCooldownReductionMax = 0.30f;
				} },

			new AbilityOption { Name="Усиление крита", Description="Множитель крита +0.25 (до x4.0)", Icon="💥", BaseRarity=AbilityRarity.Epic,
				Action=() =>
				{
					if ( PlayerStaff == null ) return;
					if ( PlayerStaff.CritMultiplier < 2f ) PlayerStaff.CritMultiplier = 2f;
					PlayerStaff.CritMultiplier = Clamp( PlayerStaff.CritMultiplier + 0.25f, 2f, CritMultCap );
				} },

			new AbilityOption { Name="Калибровка потока", Description="+0.05% КД за килл (до 0.30%/килл)", Icon="🧠", BaseRarity=AbilityRarity.Rare,
				Action=() =>
				{
					if ( PlayerStaff == null ) return;

					PlayerStaff.EnableKillCooldownStacks = true;
					if ( PlayerStaff.KillCooldownReductionMax <= 0f ) PlayerStaff.KillCooldownReductionMax = 0.30f;
					if ( PlayerStaff.KillCooldownReductionPerKill <= 0f ) PlayerStaff.KillCooldownReductionPerKill = 0.001f;

					PlayerStaff.KillCooldownReductionPerKill = Clamp(
						PlayerStaff.KillCooldownReductionPerKill + 0.0005f,
						0.001f,
						KillCdrPerKillCap
					);
				} },

			// ===== НОВЫЕ АПГРЕЙДЫ (стакаются, % на всю игру) =====
			new AbilityOption { Name="Скорость бега", Description="Скорость +10% (стак)", Icon="🏃", BaseRarity=AbilityRarity.Rare,
				Action=() =>
				{
					if ( PlayerStaminaRef == null ) return;
					PlayerStaminaRef.AddMoveSpeedStack();
				} },

			new AbilityOption { Name="Реген стамины", Description="Реген стамины +15% (стак)", Icon="🔋", BaseRarity=AbilityRarity.Common,
				Action=() =>
				{
					if ( PlayerStaminaRef == null ) return;
					PlayerStaminaRef.AddStaminaRegenStack();
				} },

			new AbilityOption { Name="Экономия стамины", Description="Расход стамины -10% (стак)", Icon="🧃", BaseRarity=AbilityRarity.Epic,
				Action=() =>
				{
					if ( PlayerStaminaRef == null ) return;
					PlayerStaminaRef.AddStaminaDrainStack();
				} },

			new AbilityOption { Name="Удача", Description="Удача +2%", Icon="🍀", BaseRarity=AbilityRarity.Common,
				Action=() =>
				{
					if ( PlayerLuckRef == null ) return;
					PlayerLuckRef.AddLuck( 2f );
				} },

			new AbilityOption { Name="Большая удача", Description="Удача +8%", Icon="✨🍀", BaseRarity=AbilityRarity.Rare,
				Action=() =>
				{
					if ( PlayerLuckRef == null ) return;
					PlayerLuckRef.AddLuck( 8f );
				} },

			new AbilityOption { Name="Благословение удачи", Description="Удача +15%", Icon="🌟🍀", BaseRarity=AbilityRarity.Epic,
				Action=() =>
				{
					if ( PlayerLuckRef == null ) return;
					PlayerLuckRef.AddLuck( 15f );
				} },

			new AbilityOption { Name="Берсерк", Description="+К урону и скорости атаки при получении урона", Icon="😡", BaseRarity=AbilityRarity.Rare,
				Action=() =>
				{
					if ( PlayerStaff == null ) return;
					PlayerStaff.AddBerserkAbility();
				},
				CanAppear=() => PlayerStaff == null || PlayerStaff.BerserkAbilityLevel < 1 },

			new AbilityOption { Name="Лучевая болезнь", Description="Шанс нанести (отравляющий) урон", Icon="☣️", BaseRarity=AbilityRarity.Common,
				Action=() =>
				{
					if ( PlayerStaff == null ) return;
					PlayerStaff.AddRadiationSicknessLevel();
				},
				CanAppear=() => PlayerStaff == null || PlayerStaff.RadiationSicknessLevel < PlayerStaff.RadiationSicknessMaxLevel },

			new AbilityOption { Name="Золотая лихорадка", Description="Больше золота за килы", Icon="🪙", BaseRarity=AbilityRarity.Common,
				Action=() =>
				{
					if ( PlayerStaff == null ) return;
					PlayerStaff.AddGoldFeverLevel();
				},
				CanAppear=() => PlayerStaff == null || PlayerStaff.GoldFeverLevel < PlayerStaff.GoldFeverMaxLevel },

			new AbilityOption { Name="Магнит", Description="Каждые 145 сек притягивает монеты и EXP со всей карты", Icon="🧲", BaseRarity=AbilityRarity.Epic,
				Action=() =>
				{
					var magnet = MagnetAbilityController.GetOrCreate( this );
					magnet?.Unlock();
				},
				CanAppear=() => !MagnetAbilityController.IsUnlockedIn( Scene ) },
		};
	}

	private List<AbilityOption> GenerateRandomAbilities( int count, int playerLevel )
	{
		var all = BuildAllAbilities()
			.Where( a => a?.CanAppear?.Invoke() ?? true )
			.ToList();

		if ( all.Count == 0 )
			return new List<AbilityOption>();

		// Если редкость выключена — ведём себя как раньше
		if ( !EnableRarity || RarityConfig == null )
		{
			var pickedSimple = all
				.OrderBy( x => MathF.Sin( (x.Name.GetHashCode() * 0.0001f) + (RealTime.Now * 0.7f) ) )
				.Take( count )
				.ToList();

			if ( !EnableRarity )
				for ( int i = 0; i < pickedSimple.Count; i++ ) pickedSimple[i].BaseRarity = AbilityRarity.Common;

			return pickedSimple;
		}

		// Пулы по редкости
		var commons = all.Where( a => a.BaseRarity == AbilityRarity.Common ).ToList();
		var rares = all.Where( a => a.BaseRarity == AbilityRarity.Rare ).ToList();
		var epics = all.Where( a => a.BaseRarity == AbilityRarity.Epic ).ToList();

		var result = new List<AbilityOption>( count );
		var usedNames = new HashSet<string>();

		for ( int i = 0; i < count; i++ )
		{
			var desired = RarityConfig.RollRarity( _rng, playerLevel );

			AbilityOption picked = TryPickFromPool( desired, commons, rares, epics, usedNames );
			if ( picked == null )
				break;

			result.Add( picked );
			usedNames.Add( picked.Name ?? "" );
		}

		// Если вдруг получилось меньше — добиваем любыми
		if ( result.Count < count )
		{
			foreach ( var a in all )
			{
				if ( result.Count >= count ) break;
				if ( usedNames.Contains( a.Name ?? "" ) ) continue;
				result.Add( a );
				usedNames.Add( a.Name ?? "" );
			}
		}

		return result;
	}

	private AbilityOption TryPickFromPool(
		AbilityRarity desired,
		List<AbilityOption> commons,
		List<AbilityOption> rares,
		List<AbilityOption> epics,
		HashSet<string> used )
	{
		Span<AbilityRarity> order = stackalloc AbilityRarity[3];

		if ( desired == AbilityRarity.Epic )
		{
			order[0] = AbilityRarity.Epic;
			order[1] = AbilityRarity.Rare;
			order[2] = AbilityRarity.Common;
		}
		else if ( desired == AbilityRarity.Rare )
		{
			order[0] = AbilityRarity.Rare;
			order[1] = AbilityRarity.Common;
			order[2] = AbilityRarity.Epic;
		}
		else
		{
			order[0] = AbilityRarity.Common;
			order[1] = AbilityRarity.Rare;
			order[2] = AbilityRarity.Epic;
		}

		for ( int k = 0; k < 3; k++ )
		{
			var pool = order[k] == AbilityRarity.Common ? commons :
					   order[k] == AbilityRarity.Rare ? rares : epics;

			var candidate = PickRandomNotUsed( pool, used );
			if ( candidate != null )
				return candidate;
		}

		return null;
	}

	private AbilityOption PickRandomNotUsed( List<AbilityOption> pool, HashSet<string> used )
	{
		if ( pool == null || pool.Count == 0 ) return null;

		int available = 0;
		for ( int i = 0; i < pool.Count; i++ )
		{
			string n = pool[i].Name ?? "";
			if ( used.Contains( n ) ) continue;
			available++;
		}

		if ( available == 0 ) return null;

		int pick = _rng.Next( 0, available );
		for ( int i = 0; i < pool.Count; i++ )
		{
			var a = pool[i];
			string n = a.Name ?? "";
			if ( used.Contains( n ) ) continue;

			if ( pick == 0 )
				return a;

			pick--;
		}

		return null;
	}

	// ===== CHEST API =====

	public bool PauseForExternalMenu()
	{
		if ( _isMenuOpen || _externalPauseActive )
			return false;

		_externalPauseActive = true;
		_oldTimeScale = Scene.TimeScale;
		Scene.TimeScale = 0f;
		EnableBlur();
		return true;
	}

	public void ResumeFromExternalMenu()
	{
		if ( !_externalPauseActive )
			return;

		DisableBlur();
		Scene.TimeScale = _oldTimeScale;
		_externalPauseActive = false;
	}

	public void ApplyAbilityDirect( AbilityOption option )
	{
		if ( option == null )
			return;

		option.Action?.Invoke();

		var gameHUD = Scene.GetAllComponents<GameHUD>().FirstOrDefault();
		if ( gameHUD != null )
			gameHUD.AddAbility( option.Name );
	}

	public AbilityOption RollSingleAbilityForChest( ChestRarityConfig chestRarity, float luckPercent )
	{
		var all = BuildAllAbilities()
			.Where( a => a?.CanAppear?.Invoke() ?? true )
			.ToList();
		if ( all == null || all.Count == 0 )
			return null;

		AbilityRarity desired;

		if ( EnableRarity && chestRarity != null )
		{
			desired = chestRarity.RollRarity( _rng, luckPercent );
		}
		else if ( EnableRarity && RarityConfig != null )
		{
			desired = RarityConfig.RollRarity( _rng, 1 );
		}
		else
		{
			desired = AbilityRarity.Common;
		}

		var commons = all.Where( a => a.BaseRarity == AbilityRarity.Common ).ToList();
		var rares = all.Where( a => a.BaseRarity == AbilityRarity.Rare ).ToList();
		var epics = all.Where( a => a.BaseRarity == AbilityRarity.Epic ).ToList();

		var picked = TryPickFromPool(
			desired,
			commons,
			rares,
			epics,
			new HashSet<string>()
		);

		if ( picked != null )
			return picked;

		return all[_rng.Next( 0, all.Count )];
	}

	protected override void OnUpdate()
	{
		if ( !_isMenuOpen ) return;

		if ( _isSelecting )
		{
			DrawMenu();
			TryFinishSelection();
			return;
		}

		if ( Input.Pressed( "Slot1" ) ) BeginSelect( 0 );
		else if ( Input.Pressed( "Slot2" ) ) BeginSelect( 1 );
		else if ( Input.Pressed( "Slot3" ) ) BeginSelect( 2 );

		DrawMenu();
	}

	private void BeginSelect( int index )
	{
		if ( index < 0 || index >= _currentOptions.Count ) return;

		_selectedIndex = index;

		if ( EnableSelectSound ) PlaySelectSound();

		if ( EnableSelectAnimation )
		{
			_isSelecting = true;
			_selectStartReal = RealTime.Now;
		}
		else ApplySelectionNow();
	}

	private void PlaySelectSound()
	{
		if ( SelectSound != null ) { Sound.Play( SelectSound, 0f ); return; }
		if ( !string.IsNullOrEmpty( SelectSoundName ) ) { Sound.Play( SelectSoundName, 0f ); return; }
	}

	private void TryFinishSelection()
	{
		float t = (RealTime.Now - _selectStartReal) / MathF.Max( 0.001f, SelectAnimTime );
		if ( t >= 1f ) ApplySelectionNow();
	}

	private void ApplySelectionNow()
	{
		int index = _selectedIndex;
		if ( index < 0 || index >= _currentOptions.Count ) return;

		_currentOptions[index].Action?.Invoke();

		var gameHUD = Scene.GetAllComponents<GameHUD>().FirstOrDefault();
		if ( gameHUD != null )
			gameHUD.AddAbility( _currentOptions[index].Name );

		DisableBlur();

		Scene.TimeScale = _oldTimeScale;
		_isMenuOpen = false;

		_isSelecting = false;
		_selectedIndex = -1;
	}

	// ====== UI Drawing ======
	private void DrawMenu()
	{
		if ( Scene.Camera == null ) return;

		HudPainter hud = Scene.Camera.Hud;

		float introAlpha = 1f;
		float introScale = 1f;

		if ( EnableIntroAnimation )
		{
			float t = (RealTime.Now - _introStartReal) / MathF.Max( 0.001f, IntroTime );
			t = Clamp01( t );
			float ease = 1f - MathF.Pow( 1f - t, 3f );

			introAlpha = ease;
			introScale = Lerp( IntroScaleFrom, 1f, ease );
		}

		float overlayA = OverlayAlpha * introAlpha;
		hud.DrawRect( new Rect( 0, 0, Screen.Width, Screen.Height ), new Color( 0f, 0f, 0f, overlayA ) );

		float centerX = Screen.Width * 0.5f;
		float centerY = Screen.Height * 0.48f;

		float w = PanelWidth * introScale;
		float h = PanelHeight * introScale;

		float px = centerX - w * 0.5f;
		float py = centerY - h * 0.5f;

		DrawRoundedRect( hud, px + ShadowOffsetX, py + ShadowOffsetY, w, h, PanelShadow, PanelRadius, 0f, new Color( 0, 0, 0, 0 ) );
		DrawRoundedRect( hud, px, py, w, h, new Color( PanelBg.r, PanelBg.g, PanelBg.b, PanelBg.a * introAlpha ), PanelRadius, PanelBorder, PanelBorderColor );

		DrawCenteredText( hud, T( "levelup.title", "ВЫБЕРИ СПОСОБНОСТЬ", "CHOOSE AN ABILITY" ), new Rect( px, py + 26f, w, 60f ), new Color( TitleColor.r, TitleColor.g, TitleColor.b, TitleColor.a * introAlpha ), TitleSize );
		DrawCenteredText( hud, string.Format( T( "levelup.level", "Уровень {0}", "Level {0}" ), _lastLevelOpened ), new Rect( px, py + 70f, w, 26f ), new Color( SubtitleColor.r, SubtitleColor.g, SubtitleColor.b, SubtitleColor.a * introAlpha ), SubtitleSize );

		float cardsTop = py + 130f;
		float cardW = CardWidth * introScale;
		float cardH = CardHeight * introScale;
		float gap = CardGap * introScale;

		float totalCardsWidth = (cardW * 3f) + (gap * 2f);
		float startX = centerX - totalCardsWidth * 0.5f;

		for ( int i = 0; i < _currentOptions.Count && i < 3; i++ )
		{
			float cx = startX + i * (cardW + gap);
			float cy = cardsTop;

			float dim = 1f;
			float selScale = 1f;
			float flash = 0f;

			if ( _isSelecting && EnableSelectAnimation )
			{
				float t = (RealTime.Now - _selectStartReal) / MathF.Max( 0.001f, SelectAnimTime );
				t = Clamp01( t );
				float ease = 1f - MathF.Pow( 1f - t, 3f );

				if ( i == _selectedIndex ) { selScale = Lerp( 1f, SelectScaleTo, ease ); flash = 1f - ease; }
				else dim = Lerp( 1f, 0.45f, ease );
			}

			DrawAbilityCard( hud, i, _currentOptions[i], cx, cy, cardW, cardH, introAlpha * dim, selScale, flash );
		}

		DrawCenteredText(
			hud,
			T( "levelup.hint", "Нажми 1 / 2 / 3 для выбора", "Press 1 / 2 / 3 to choose" ),
			new Rect( px, py + h - 56f, w, 24f ),
			new Color( HintColor.r, HintColor.g, HintColor.b, HintColor.a * introAlpha ),
			HintSize
		);
	}

	private void DrawAbilityCard( HudPainter hud, int index, AbilityOption opt, float x, float y, float w, float h, float alpha, float scale, float flash )
	{
		float cx = x + w * 0.5f;
		float cy = y + h * 0.5f;

		float sw = w * scale;
		float sh = h * scale;

		float sx = cx - sw * 0.5f;
		float sy = cy - sh * 0.5f;

		Color borderCol;
		Color glowCol;
		GetRarityColors( opt.BaseRarity, out borderCol, out glowCol );

		borderCol = new Color( borderCol.r, borderCol.g, borderCol.b, borderCol.a * alpha );
		glowCol = new Color( glowCol.r, glowCol.g, glowCol.b, glowCol.a * alpha );

		DrawRoundedRect( hud, sx + 3f, sy + 5f, sw, sh, new Color( 0f, 0f, 0f, 0.30f * alpha ), CardRadius, 0f, new Color( 0, 0, 0, 0 ) );

		if ( EnableRarity )
			DrawRoundedRect( hud, sx - 2f, sy - 2f, sw + 4f, sh + 4f, glowCol, CardRadius + 2f, 2f, borderCol );

		DrawRoundedRect( hud, sx, sy, sw, sh, new Color( CardBg.r, CardBg.g, CardBg.b, CardBg.a * alpha ), CardRadius, CardBorder, new Color( CardBorderColor.r, CardBorderColor.g, CardBorderColor.b, CardBorderColor.a * alpha ) );

		hud.DrawRect( new Rect( sx + 14f, sy + 16f, sw - 28f, 2f ), new Color( CardBgTop.r, CardBgTop.g, CardBgTop.b, CardBgTop.a * alpha ) );

		if ( flash > 0f )
		{
			float fa = Clamp01( flash ) * 0.18f * alpha;
			DrawRoundedRect( hud, sx, sy, sw, sh, new Color( 1f, 1f, 1f, fa ), CardRadius, 0f, new Color( 0, 0, 0, 0 ) );
		}

		float badgeW = 44f;
		float badgeH = 34f;
		float bx = sx + 14f;
		float by = sy + 14f;

		DrawRoundedRect( hud, bx, by, badgeW, badgeH, new Color( KeyBadgeBg.r, KeyBadgeBg.g, KeyBadgeBg.b, KeyBadgeBg.a * alpha ), 10f, 2f, new Color( KeyBadgeBorder.r, KeyBadgeBorder.g, KeyBadgeBorder.b, KeyBadgeBorder.a * alpha ) );
		DrawCenteredText( hud, $"{index + 1}", new Rect( bx, by, badgeW, badgeH ), new Color( KeyBadgeText.r, KeyBadgeText.g, KeyBadgeText.b, KeyBadgeText.a * alpha ), 20f );

		if ( EnableRarity )
		{
			string rar = GetLocalizedRarityText( opt.BaseRarity );
			DrawCenteredText( hud, rar, new Rect( sx + sw - 100f, sy + 18f, 90f, 20f ), new Color( borderCol.r, borderCol.g, borderCol.b, 0.85f * alpha ), 12f );
		}

		float circleSize = 92f;
		float cix = sx + (sw * 0.5f) - (circleSize * 0.5f);
		float ciy = sy + 56f;

		DrawRoundedRect(
			hud, cix, ciy, circleSize, circleSize,
			new Color( IconCircleBg.r, IconCircleBg.g, IconCircleBg.b, IconCircleBg.a * alpha ),
			circleSize * 0.5f, 2f,
			new Color( IconCircleBorder.r, IconCircleBorder.g, IconCircleBorder.b, IconCircleBorder.a * alpha )
		);

		DrawCenteredText( hud, opt.Icon ?? "?", new Rect( cix, ciy + 6f, circleSize, circleSize ), new Color( TitleColor.r, TitleColor.g, TitleColor.b, TitleColor.a * alpha ), IconSize );

		DrawCenteredText( hud, GetLocalizedAbilityName( opt ), new Rect( sx + 14f, sy + 160f, sw - 28f, 30f ), new Color( AbilityNameColor.r, AbilityNameColor.g, AbilityNameColor.b, AbilityNameColor.a * alpha ), AbilityNameSize );
		DrawCenteredText( hud, GetLocalizedAbilityDescription( opt ), new Rect( sx + 18f, sy + 198f, sw - 36f, 60f ), new Color( AbilityDescColor.r, AbilityDescColor.g, AbilityDescColor.b, AbilityDescColor.a * alpha ), AbilityDescSize );
	}

	private void GetRarityColors( AbilityRarity rarity, out Color border, out Color glow )
	{
		if ( !EnableRarity ) { border = CommonColor; glow = CommonGlow; return; }

		switch ( rarity )
		{
			case AbilityRarity.Rare: border = RareColor; glow = RareGlow; break;
			case AbilityRarity.Epic: border = EpicColor; glow = EpicGlow; break;
			default: border = CommonColor; glow = CommonGlow; break;
		}
	}


	private string T( string key, string russianFallback, string englishFallback )
	{
		GameLocalization.EnsureLoaded();
		return GameLocalization.T( key, GameLocalization.IsLanguage( "ru" ) ? russianFallback : englishFallback );
	}

	private string GetLocalizedRarityText( AbilityRarity rarity )
	{
		return rarity switch
		{
			AbilityRarity.Rare => T( "levelup.rarity.rare", "РЕДК.", "RARE" ),
			AbilityRarity.Epic => T( "levelup.rarity.epic", "ЭПИК", "EPIC" ),
			_ => T( "levelup.rarity.common", "ОБЫЧ.", "COMMON" )
		};
	}

	private string GetLocalizedAbilityName( AbilityOption opt )
	{
		string name = opt?.Name ?? string.Empty;

		return name switch
		{
			"Регенерация" => T( "levelup.ability.regen.name", "Регенерация", "Regeneration" ),
			"Увеличение HP" => T( "levelup.ability.maxhp.name", "Увеличение HP", "Max HP Up" ),
			"Щит" => T( "levelup.ability.shield.name", "Щит", "Shield" ),
			"Урон +5" => T( "levelup.ability.damage5.name", "Урон +5", "Damage +5" ),
			"Скорость атаки" => T( "levelup.ability.atkspd.name", "Скорость атаки", "Attack Speed" ),
			"Мульти-луч" => T( "levelup.ability.multitarget.name", "Мульти-луч", "Multi Beam" ),
			"Рикошет" => T( "levelup.ability.ricochet.name", "Рикошет", "Ricochet" ),
			"Дальность луча" => T( "levelup.ability.range.name", "Дальность луча", "Beam Range" ),
			"Критический разряд" => T( "levelup.ability.crit.name", "Критический разряд", "Critical Discharge" ),
			"Добивание" => T( "levelup.ability.execute.name", "Добивание", "Execution" ),
			"Поток убийств" => T( "levelup.ability.killflow.name", "Поток убийств", "Kill Flow" ),
			"Усиление крита" => T( "levelup.ability.critboost.name", "Усиление крита", "Critical Boost" ),
			"Калибровка потока" => T( "levelup.ability.flowcalibration.name", "Калибровка потока", "Flow Calibration" ),
			"Скорость бега" => T( "levelup.ability.runspeed.name", "Скорость бега", "Run Speed" ),
			"Реген стамины" => T( "levelup.ability.stam_regen.name", "Реген стамины", "Stamina Regen" ),
			"Экономия стамины" => T( "levelup.ability.stam_drain.name", "Экономия стамины", "Stamina Efficiency" ),
			"Удача" => T( "levelup.ability.luck.name", "Удача", "Luck" ),
			"Большая удача" => T( "levelup.ability.bigluck.name", "Большая удача", "Great Luck" ),
			"Благословение удачи" => T( "levelup.ability.luckblessing.name", "Благословение удачи", "Luck Blessing" ),
			"Берсерк" => T( "levelup.ability.berserk.name", "Берсерк", "Berserk" ),
			"Лучевая болезнь" => T( "levelup.ability.radiation.name", "Лучевая болезнь", "Radiation Sickness" ),
			"Золотая лихорадка" => T( "levelup.ability.goldfever.name", "Золотая лихорадка", "Gold Fever" ),
			"Магнит" => T( "levelup.ability.magnet.name", "Магнит", "Magnet" ),
			_ => name
		};
	}

	private string GetLocalizedAbilityDescription( AbilityOption opt )
	{
		string name = opt?.Name ?? string.Empty;

		return name switch
		{
			"Регенерация" => T( "levelup.ability.regen.desc", "1 HP раз в 4 секунды", "1 HP every 4 seconds" ),
			"Увеличение HP" => T( "levelup.ability.maxhp.desc", "Максимальное здоровье +20", "Maximum health +20" ),
			"Щит" => T( "levelup.ability.shield.desc", "Щит +5 (стак), реген. 15с", "Shield +5 (stack), 15s regen" ),
			"Урон +5" => T( "levelup.ability.damage5.desc", "Увеличивает урон на 5", "Increases damage by 5" ),
			"Скорость атаки" => T( "levelup.ability.atkspd.desc", "Уменьшает кулдаун на 10%", "Reduces cooldown by 10%" ),
			"Мульти-луч" => T( "levelup.ability.multitarget.desc", "Бьет по нескольким целям", "Hits multiple targets" ),
			"Рикошет" => T( "levelup.ability.ricochet.desc", "Луч перескакивает на ещё 1 цель рядом", "Beam bounces to 1 extra nearby target" ),
			"Дальность луча" => T( "levelup.ability.range.desc", "Увеличивает радиус атаки на 5%", "Increases attack range by 5%" ),
			"Критический разряд" => T( "levelup.ability.crit.desc", "Шанс крита +5% (x2 урон)", "Crit chance +5% (x2 damage)" ),
			"Добивание" => T( "levelup.ability.execute.desc", "+15% урона по врагам ниже 30% HP", "+15% damage to enemies below 30% HP" ),
			"Поток убийств" => T( "levelup.ability.killflow.desc", "За килл посохом -0.1% КД (до -30%)", "Staff kills give -0.1% cooldown (up to -30%)" ),
			"Усиление крита" => T( "levelup.ability.critboost.desc", "Множитель крита +0.25 (до x4.0)", "Crit multiplier +0.25 (up to x4.0)" ),
			"Калибровка потока" => T( "levelup.ability.flowcalibration.desc", "+0.05% КД за килл (до 0.30%/килл)", "+0.05% cooldown per kill (up to 0.30%/kill)" ),
			"Скорость бега" => T( "levelup.ability.runspeed.desc", "Скорость +10% (стак)", "Move speed +10% (stack)" ),
			"Реген стамины" => T( "levelup.ability.stam_regen.desc", "Реген стамины +15% (стак)", "Stamina regen +15% (stack)" ),
			"Экономия стамины" => T( "levelup.ability.stam_drain.desc", "Расход стамины -10% (стак)", "Stamina drain -10% (stack)" ),
			"Удача" => T( "levelup.ability.luck.desc", "Удача +2%", "Luck +2%" ),
			"Большая удача" => T( "levelup.ability.bigluck.desc", "Удача +8%", "Luck +8%" ),
			"Благословение удачи" => T( "levelup.ability.luckblessing.desc", "Удача +15%", "Luck +15%" ),
			"Берсерк" => T( "levelup.ability.berserk.desc", "+К урону и скорости атаки при получении урона", "+damage and attack speed when taking damage" ),
			"Лучевая болезнь" => T( "levelup.ability.radiation.desc", "Шанс нанести (отравляющий) урон", "Chance to apply lingering damage" ),
			"Золотая лихорадка" => T( "levelup.ability.goldfever.desc", "Больше золота за килы", "More gold for kills" ),
			"Магнит" => T( "levelup.ability.magnet.desc", "Каждые 145 сек притягивает монеты и EXP со всей карты", "Every 145 sec pulls coins and EXP from the whole map" ),
			_ => opt?.Description ?? string.Empty
		};
	}


	private void DrawRoundedRect( HudPainter hud, float x, float y, float w, float h, Color fill, float radius, float border, Color borderColor )
	{
		var r = new Rect( x, y, w, h );
		var corner = new Vector4( radius, radius, radius, radius );
		var bw = new Vector4( border, border, border, border );
		hud.DrawRect( r, fill, corner, bw, borderColor );
	}

	private void DrawCenteredText( HudPainter hud, string text, Rect rect, Color color, float size )
	{
		var scope = new TextRendering.Scope( text ?? "", color, size );
		hud.DrawText( scope, rect, TextFlag.Center );
	}

	private static float Clamp01( float v )
	{
		if ( v < 0f ) return 0f;
		if ( v > 1f ) return 1f;
		return v;
	}

	private static float Clamp( float v, float min, float max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}

	private static float Lerp( float a, float b, float t )
	{
		t = Clamp01( t );
		return a + (b - a) * t;
	}
}

public enum AbilityRarity
{
	Common = 0,
	Rare = 1,
	Epic = 2
}

public class AbilityOption
{
	public string Name { get; set; }
	public string Description { get; set; }
	public string Icon { get; set; }
	public Action Action { get; set; }
	public Func<bool> CanAppear { get; set; }
	public AbilityRarity BaseRarity { get; set; } = AbilityRarity.Common;
}
