using Sandbox;
using Sandbox.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using YourGame.UI;

public enum ShopUpgradeCategory
{
	Health,
	Staff,
	Stamina,
	Utility
}

[Serializable]
public class ShopUpgradeDefinition
{
	[Property] public string Id { get; set; } = "unknown";
	[Property] public string Name { get; set; } = "???";
	[Property] public string Description { get; set; } = "";
	[Property] public string Icon { get; set; } = "⭐";

	[Property] public AbilityRarity Rarity { get; set; } = AbilityRarity.Common;
	[Property] public ShopUpgradeCategory Category { get; set; } = ShopUpgradeCategory.Utility;

	[Property] public int Price { get; set; } = 25;
	[Property] public float PriceMultiplier { get; set; } = 1.25f;

	[Property] public int MaxStacks { get; set; } = 99;
	[Property] public bool CanBeStolen { get; set; } = true;
}

public static class PlayerStaffShopUpgradeFallbacks
{
	public static void AddBerserkAbility( this PlayerStaff staff ) { }
	public static void RemoveBerserkAbility( this PlayerStaff staff ) { }
	public static void AddRadiationSicknessLevel( this PlayerStaff staff ) { }
	public static void RemoveRadiationSicknessLevel( this PlayerStaff staff ) { }
	public static void AddGoldFeverLevel( this PlayerStaff staff ) { }
	public static void RemoveGoldFeverLevel( this PlayerStaff staff ) { }
}

public sealed class ShopUpgradeMenu : Component
{
	[Property] public PlayerStats PlayerStats { get; set; }
	[Property] public PlayerHealth PlayerHealth { get; set; }
	[Property] public PlayerStaff PlayerStaff { get; set; }
	[Property] public PlayerLuck PlayerLuck { get; set; }

	[Property] public float OverlayAlpha { get; set; } = 0.62f;

	[Property] public float PanelWidth { get; set; } = 1500f;
	[Property] public float PanelHeight { get; set; } = 860f;
	[Property] public float PanelRadius { get; set; } = 18f;

	[Property] public Color PanelBg { get; set; } = new Color( 0.08f, 0.10f, 0.14f, 0.95f );
	[Property] public Color PanelBorderColor { get; set; } = new Color( 1, 1, 1, 0.08f );
	[Property] public Color TitleColor { get; set; } = new Color( 1.00f, 0.92f, 0.20f, 1.0f );
	[Property] public Color SubColor { get; set; } = new Color( 1f, 1f, 1f, 0.70f );

	[Property] public float RowHeight { get; set; } = 82f;
	[Property] public float ScrollSpeed { get; set; } = 110f;

	[Property]
	public List<ShopUpgradeDefinition> Upgrades { get; set; } = new()
	{
		new ShopUpgradeDefinition{ Id="regen", Name="Регенерация", Description="1 HP раз в 4 секунды", Icon="❤️", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Health, Price=25, PriceMultiplier=1.20f, MaxStacks=99, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="maxhp", Name="Увеличение HP", Description="Макс. здоровье +20", Icon="💪", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Health, Price=35, PriceMultiplier=1.22f, MaxStacks=99, CanBeStolen=true },

		new ShopUpgradeDefinition{ Id="damage5", Name="Урон +5", Description="Урон посоха +5", Icon="⚡", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=60, PriceMultiplier=1.28f, MaxStacks=99, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="atkspd", Name="Скорость атаки", Description="Кулдаун -10% (стак)", Icon="⚔️", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=70, PriceMultiplier=1.30f, MaxStacks=20, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="multitarget", Name="Мульти-луч", Description="+1 цель", Icon="⚡⚡", Rarity=AbilityRarity.Epic, Category=ShopUpgradeCategory.Staff, Price=140, PriceMultiplier=1.45f, MaxStacks=10, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="range", Name="Дальность луча", Description="+5% дальность (стак)", Icon="🎯", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Staff, Price=30, PriceMultiplier=1.18f, MaxStacks=50, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="berserk", Name="Берсерк", Description="+К урону и скорости атаки при получении урона", Icon="😡", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=100, PriceMultiplier=1.50f, MaxStacks=1, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="radiation", Name="Лучевая болезнь", Description="Наносит длительный урон молнией", Icon="☣️", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Staff, Price=40, PriceMultiplier=1.60f, MaxStacks=6, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="goldfever", Name="Золотая лихорадка", Description="Больше золота за килы", Icon="🪙", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Utility, Price=10, PriceMultiplier=2.00f, MaxStacks=10, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="magnet", Name="Магнит", Description="Каждые 145 сек притягивает монеты и EXP со всей карты", Icon="🧲", Rarity=AbilityRarity.Epic, Category=ShopUpgradeCategory.Utility, Price=635, PriceMultiplier=1.00f, MaxStacks=1, CanBeStolen=true },

		new ShopUpgradeDefinition{ Id="runspeed", Name="Скорость бега", Description="+10% скорость (стак)", Icon="🏃", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Stamina, Price=80, PriceMultiplier=1.30f, MaxStacks=20, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="stam_regen", Name="Реген стамины", Description="+15% реген (стак)", Icon="🔋", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Stamina, Price=45, PriceMultiplier=1.20f, MaxStacks=30, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="stam_drain", Name="Экономия стамины", Description="-10% расход (стак)", Icon="🧃", Rarity=AbilityRarity.Epic, Category=ShopUpgradeCategory.Stamina, Price=120, PriceMultiplier=1.35f, MaxStacks=20, CanBeStolen=true },

		new ShopUpgradeDefinition{ Id="shield", Name="Щит", Description="Поглощает урон. +5 щита/лвл, реген 15с", Icon="🛡️", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Utility, Price=90, PriceMultiplier=1.30f, MaxStacks=20, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="luck", Name="Удача", Description="+5% шанс более редкой награды из сундуков", Icon="🍀", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Utility, Price=35, PriceMultiplier=2f, MaxStacks=10, CanBeStolen=true },
	};

	private bool _isOpen;
	public bool IsOpen => _isOpen;

	private int _selectedIndex = 0;
	private float _scroll = 0f;

	private readonly Dictionary<string, int> _owned = new();
	private Random _rng;

	private GameHUD _hud;

	protected override void OnStart()
	{
		GameLocalization.EnsureLoaded();
		_rng = new Random( (int)(Time.Now * 1000f) ^ GameObject.Id.GetHashCode() );

		PlayerStats ??= Components.Get<PlayerStats>() ?? Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
		PlayerHealth ??= Scene.GetAllComponents<PlayerHealth>().FirstOrDefault();
		PlayerStaff ??= Scene.GetAllComponents<PlayerStaff>().FirstOrDefault();
		PlayerLuck ??= Scene.GetAllComponents<PlayerLuck>().FirstOrDefault();

		_hud = Scene.GetAllComponents<GameHUD>().FirstOrDefault();

		SyncUpgradesFromDefaults();
		ApplyUpgradeLocalization();
		GameLocalization.Changed += ApplyUpgradeLocalization;
	}

	protected override void OnDestroy()
	{
		GameLocalization.Changed -= ApplyUpgradeLocalization;
		base.OnDestroy();
	}

	private void SyncUpgradesFromDefaults()
	{
		var defaults = CreateDefaultUpgrades();
		var existingById = new Dictionary<string, ShopUpgradeDefinition>( StringComparer.OrdinalIgnoreCase );

		if ( Upgrades != null )
		{
			foreach ( var upgrade in Upgrades )
			{
				if ( upgrade == null || string.IsNullOrWhiteSpace( upgrade.Id ) )
					continue;

				existingById[upgrade.Id] = upgrade;
			}
		}

		var result = new List<ShopUpgradeDefinition>( defaults.Count );

		foreach ( var def in defaults )
		{
			if ( existingById.TryGetValue( def.Id, out var existing ) )
			{
				existing.Name = def.Name;
				existing.Description = def.Description;
				existing.Icon = def.Icon;
				existing.Rarity = def.Rarity;
				existing.Category = def.Category;
				existing.Price = def.Price;
				existing.PriceMultiplier = def.PriceMultiplier;
				existing.MaxStacks = def.MaxStacks;
				existing.CanBeStolen = def.CanBeStolen;
				result.Add( existing );
			}
			else
			{
				result.Add( CloneUpgrade( def ) );
			}
		}

		Upgrades = result;
	}

	private static ShopUpgradeDefinition CloneUpgrade( ShopUpgradeDefinition def )
	{
		return new ShopUpgradeDefinition
		{
			Id = def.Id,
			Name = def.Name,
			Description = def.Description,
			Icon = def.Icon,
			Rarity = def.Rarity,
			Category = def.Category,
			Price = def.Price,
			PriceMultiplier = def.PriceMultiplier,
			MaxStacks = def.MaxStacks,
			CanBeStolen = def.CanBeStolen
		};
	}

	private static List<ShopUpgradeDefinition> CreateDefaultUpgrades() => new()
	{
		new ShopUpgradeDefinition{ Id="regen", Name="Regeneration", Description="1 HP every 4 seconds", Icon="❤️", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Health, Price=200, PriceMultiplier=2f, MaxStacks=4, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="maxhp", Name="Max HP Up", Description="Max health +20", Icon="💪", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Health, Price=45, PriceMultiplier=2f, MaxStacks=5, CanBeStolen=true },

		new ShopUpgradeDefinition{ Id="damage5", Name="Damage +5", Description="Staff damage +5", Icon="⚡", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=300, PriceMultiplier=2f, MaxStacks=15, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="atkspd", Name="Attack Speed", Description="Cooldown -10% (stack)", Icon="⚔️", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=150, PriceMultiplier=2f, MaxStacks=10, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="multitarget", Name="Multi Beam", Description="+1 target", Icon="⚡⚡", Rarity=AbilityRarity.Epic, Category=ShopUpgradeCategory.Staff, Price=290, PriceMultiplier=2f, MaxStacks=10, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="range", Name="Beam Range", Description="+5% range (stack)", Icon="🎯", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Staff, Price=35, PriceMultiplier=1.6f, MaxStacks=10, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="ricochet", Name="Ricochet", Description="Beam bounces to 1 extra nearby target", Icon="🪃", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=300, PriceMultiplier=1.60f, MaxStacks=1, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="crit", Name="Critical Discharge", Description="Crit chance +5% (x2 damage)", Icon="✨", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=80, PriceMultiplier=1.65f, MaxStacks=10, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="execute", Name="Execution", Description="+15% damage to enemies below 30% HP", Icon="🗡️", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=85, PriceMultiplier=1.70f, MaxStacks=1, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="killflow", Name="Kill Flow", Description="Staff kills give -0.1% cooldown (up to -30%)", Icon="📉", Rarity=AbilityRarity.Epic, Category=ShopUpgradeCategory.Staff, Price=300, PriceMultiplier=1.00f, MaxStacks=1, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="flowcalibration", Name="Flow Calibration", Description="Kill-stack cooldown boost (one-time)", Icon="🧠", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=150, PriceMultiplier=1.00f, MaxStacks=1, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="critboost", Name="Critical Boost", Description="Crit multiplier +0.25 (up to x4.0)", Icon="💥", Rarity=AbilityRarity.Epic, Category=ShopUpgradeCategory.Staff, Price=250, PriceMultiplier=1.00f, MaxStacks=1, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="berserk", Name="Berserk", Description="+damage and attack speed when taking damage", Icon="😡", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Staff, Price=150, PriceMultiplier=1.50f, MaxStacks=1, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="radiation", Name="Radiation Sickness", Description="Deals lingering lightning damage", Icon="☣️", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Staff, Price=45, PriceMultiplier=1.70f, MaxStacks=6, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="goldfever", Name="Gold Fever", Description="More gold for kills", Icon="🪙", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Utility, Price=15, PriceMultiplier=1.75f, MaxStacks=10, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="magnet", Name="Magnet", Description="Every 145 sec pulls coins and EXP from the whole map", Icon="🧲", Rarity=AbilityRarity.Epic, Category=ShopUpgradeCategory.Utility, Price=600, PriceMultiplier=1.00f, MaxStacks=1, CanBeStolen=true },

		new ShopUpgradeDefinition{ Id="runspeed", Name="Run Speed", Description="+10% speed (stack)", Icon="🏃", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Stamina, Price=100, PriceMultiplier=1.8f, MaxStacks=6, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="stam_regen", Name="Stamina Regen", Description="+15% regen (stack)", Icon="🔋", Rarity=AbilityRarity.Common, Category=ShopUpgradeCategory.Stamina, Price=65, PriceMultiplier=1.7f, MaxStacks=5, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="stam_drain", Name="Stamina Efficiency", Description="-10% drain (stack)", Icon="🧃", Rarity=AbilityRarity.Epic, Category=ShopUpgradeCategory.Stamina, Price=60, PriceMultiplier=2f, MaxStacks=4, CanBeStolen=true },

		new ShopUpgradeDefinition{ Id="shield", Name="Shield", Description="Absorbs damage. +5 shield/level, 15s regen", Icon="🛡️", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Utility, Price=115, PriceMultiplier=1.80f, MaxStacks=6, CanBeStolen=true },
		new ShopUpgradeDefinition{ Id="luck", Name="Luck", Description="+5% chance for rarer chest rewards", Icon="🍀", Rarity=AbilityRarity.Rare, Category=ShopUpgradeCategory.Utility, Price=30, PriceMultiplier=1.5f, MaxStacks=10, CanBeStolen=true },
	};

	private void ApplyUpgradeLocalization()
	{
		if ( Upgrades == null )
			return;

		foreach ( var u in Upgrades )
		{
			if ( u == null || string.IsNullOrWhiteSpace( u.Id ) )
				continue;

			u.Name = GetUpgradeDisplayName( u );
			u.Description = GetUpgradeDisplayDescription( u );
		}
	}

	private void EnsureShieldUpgradeExists()
	{
		Upgrades ??= new();

		if ( Upgrades.Any( u => u.Id == "shield" ) )
			return;

		Upgrades.Add( new ShopUpgradeDefinition
		{
			Id = "shield",
			Name = "Щит",
			Description = "Поглощает урон. +5 щита/лвл, реген 15с",
			Icon = "🛡️",
			Rarity = AbilityRarity.Rare,
			Category = ShopUpgradeCategory.Utility,
			Price = 90,
			PriceMultiplier = 1.30f,
			MaxStacks = 20,
			CanBeStolen = true
		} );
	}

	private void EnsureLuckUpgradeExists()
	{
		Upgrades ??= new();

		if ( Upgrades.Any( u => u.Id == "luck" ) )
			return;

		Upgrades.Add( new ShopUpgradeDefinition
		{
			Id = "luck",
			Name = "Удача",
			Description = "+5% шанс более редкой награды из сундуков",
			Icon = "🍀",
			Rarity = AbilityRarity.Rare,
			Category = ShopUpgradeCategory.Utility,
			Price = 35,
			PriceMultiplier = 2f,
			MaxStacks = 10,
			CanBeStolen = true
		} );
	}

	private void EnsureSpecialUpgradeExists( string id, string name, string description, string icon, AbilityRarity rarity, ShopUpgradeCategory category, int price, float priceMultiplier, int maxStacks )
	{
		Upgrades ??= new();

		if ( Upgrades.Any( u => u.Id == id ) )
			return;

		Upgrades.Add( new ShopUpgradeDefinition
		{
			Id = id,
			Name = name,
			Description = description,
			Icon = icon,
			Rarity = rarity,
			Category = category,
			Price = price,
			PriceMultiplier = priceMultiplier,
			MaxStacks = maxStacks,
			CanBeStolen = true
		} );
	}

	public void Open()
	{
		_isOpen = true;
		_selectedIndex = Math.Clamp( _selectedIndex, 0, Math.Max( 0, Upgrades.Count - 1 ) );
	}

	public void Close()
	{
		_isOpen = false;
	}

	protected override void OnUpdate()
	{
		if ( !_isOpen ) return;

		Draw();
		HandleInput();
	}

	private void HandleInput()
	{
		_scroll -= Input.MouseWheel.y * ScrollSpeed;

		if ( Input.Pressed( "attack1" ) )
		{
			Vector2 m = Mouse.Position;

			var layout = GetLayout();
			var listRect = layout.ListRect;
			var buyRect = layout.BuyRect;

			if ( RectContains( listRect, m ) )
			{
				int idx = GetIndexFromMouse( m, listRect );
				if ( idx >= 0 && idx < Upgrades.Count )
					_selectedIndex = idx;
			}

			if ( RectContains( buyRect, m ) )
				BuySelected();
		}

		if ( Input.Pressed( "Use" ) )
			Close();
	}

	private Rect GetPanelRect()
	{
		float widthByScreen = Screen.Width * 0.78f;
		float heightByScreen = Screen.Height * 0.80f;

		float maxWidth = Screen.Width * 0.94f;
		float maxHeight = Screen.Height * 0.90f;

		float width = (float)Math.Max( PanelWidth, widthByScreen );
		float height = (float)Math.Max( PanelHeight, heightByScreen );

		width = Clamp( width, 900f, maxWidth );
		height = Clamp( height, 620f, maxHeight );

		float x = (Screen.Width - width) * 0.5f;
		float y = (Screen.Height - height) * 0.5f;

		return new Rect( x, y, width, height );
	}

	private float GetUiScale()
	{
		var panel = GetPanelRect();
		float sx = panel.Width / 1500f;
		float sy = panel.Height / 860f;
		return Clamp( Math.Min( sx, sy ), 1f, 1.35f );
	}

	private float GetActualRowHeight()
	{
		float scaled = RowHeight * GetUiScale();
		return Math.Max( 72f, scaled );
	}

	private int GetIndexFromMouse( Vector2 mouse, Rect listRect )
	{
		float y = mouse.y - listRect.Top + _scroll;
		if ( y < 0 ) return -1;
		return (int)(y / GetActualRowHeight());
	}

	private void BuySelected()
	{
		if ( PlayerStats == null ) return;
		if ( _selectedIndex < 0 || _selectedIndex >= Upgrades.Count ) return;

		var def = Upgrades[_selectedIndex];

		int owned = GetOwnedForUi( def.Id );
		if ( def.MaxStacks > 0 && owned >= def.MaxStacks )
			return;

		int currentPrice = GetCurrentPrice( def, owned );

		if ( !PlayerStats.TrySpendCoins( currentPrice ) )
			return;

		ApplyUpgrade( def.Id );

		int newOwned = owned + 1;
		_owned[def.Id] = newOwned;

		_hud ??= Scene.GetAllComponents<GameHUD>().FirstOrDefault();
		_hud?.SetAbilityLevel( def.Name, newOwned );
	}

	private int GetOwned( string id )
	{
		if ( string.IsNullOrEmpty( id ) ) return 0;
		return _owned.TryGetValue( id, out var v ) ? v : 0;
	}

	private int GetOwnedForUi( string id )
	{
		int owned = GetOwned( id );

		if ( id == "magnet" && MagnetAbilityController.IsUnlockedIn( Scene ) )
			return Math.Max( owned, 1 );

		return owned;
	}

	private int GetCurrentPrice( ShopUpgradeDefinition def )
	{
		return GetCurrentPrice( def, GetOwned( def.Id ) );
	}

	private int GetCurrentPrice( ShopUpgradeDefinition def, int ownedCount )
	{
		if ( def == null )
			return 0;

		double basePrice = Math.Max( 1, def.Price );
		double multiplier = def.PriceMultiplier;

		if ( multiplier <= 0.0001 )
			multiplier = 1.0;

		double scaled = basePrice * Math.Pow( multiplier, Math.Max( 0, ownedCount ) );
		int result = (int)Math.Round( scaled );

		return Math.Max( 1, result );
	}

	private int GetNextPrice( ShopUpgradeDefinition def )
	{
		return GetCurrentPrice( def, GetOwned( def.Id ) + 1 );
	}

	public bool TryStealRandomUpgrade( out string stolenName )
	{
		stolenName = "";

		if ( Upgrades == null || Upgrades.Count == 0 )
			return false;

		var stealable = new List<ShopUpgradeDefinition>();

		foreach ( var u in Upgrades )
		{
			if ( !u.CanBeStolen ) continue;
			if ( GetOwned( u.Id ) <= 0 ) continue;

			stealable.Add( u );
		}

		if ( stealable.Count == 0 )
			return false;

		var pick = stealable[_rng.Next( 0, stealable.Count )];

		int newOwned = Math.Max( 0, GetOwned( pick.Id ) - 1 );
		_owned[pick.Id] = newOwned;

		UndoUpgrade( pick.Id );
		stolenName = pick.Name;

		_hud ??= Scene.GetAllComponents<GameHUD>().FirstOrDefault();
		_hud?.SetAbilityLevel( pick.Name, newOwned );

		return true;
	}

	private void ApplyUpgrade( string id )
	{
		switch ( id )
		{
			case "regen":
				if ( PlayerHealth != null ) PlayerHealth.HealthRegenAmount += 1f;
				break;

			case "maxhp":
				if ( PlayerHealth != null ) PlayerHealth.MaxHealth += 20;
				break;

			case "damage5":
				if ( PlayerStaff != null ) PlayerStaff.Damage += 5;
				break;

			case "atkspd":
				if ( PlayerStaff != null ) PlayerStaff.AttackCooldown *= 0.9f;
				break;

			case "multitarget":
				if ( PlayerStaff != null ) PlayerStaff.MaxTargets += 1;
				break;

			case "range":
				if ( PlayerStaff != null ) PlayerStaff.RangeMultiplier += 0.05f;
				break;

			case "berserk":
				if ( PlayerStaff != null ) PlayerStaff.AddBerserkAbility();
				break;

			case "radiation":
				if ( PlayerStaff != null ) PlayerStaff.AddRadiationSicknessLevel();
				break;

			case "goldfever":
				if ( PlayerStaff != null ) PlayerStaff.AddGoldFeverLevel();
				break;

			case "runspeed":
				if ( PlayerHealth != null ) PlayerHealth.RunSpeedMultiplier *= 1.10f;
				break;

			case "stam_regen":
				if ( PlayerHealth != null ) PlayerHealth.StaminaRegenRate *= 1.15f;
				break;

			case "stam_drain":
				if ( PlayerHealth != null ) PlayerHealth.StaminaDrainRate *= 0.90f;
				break;

			case "shield":
				if ( PlayerHealth != null ) PlayerHealth.AddShieldLevel();
				break;

			case "luck":
				if ( PlayerLuck != null ) PlayerLuck.AddLuck( 5f );
				break;

			case "magnet":
				{
					var magnet = MagnetAbilityController.GetOrCreate( this );
					magnet?.Unlock();
				}
				break;
		}
	}

	private void UndoUpgrade( string id )
	{
		switch ( id )
		{
			case "regen":
				if ( PlayerHealth != null ) PlayerHealth.HealthRegenAmount -= 1f;
				break;

			case "maxhp":
				if ( PlayerHealth != null ) PlayerHealth.MaxHealth -= 20;
				break;

			case "damage5":
				if ( PlayerStaff != null ) PlayerStaff.Damage -= 5;
				break;

			case "atkspd":
				if ( PlayerStaff != null ) PlayerStaff.AttackCooldown /= 0.9f;
				break;

			case "multitarget":
				if ( PlayerStaff != null ) PlayerStaff.MaxTargets = Math.Max( 1, PlayerStaff.MaxTargets - 1 );
				break;

			case "range":
				if ( PlayerStaff != null ) PlayerStaff.RangeMultiplier -= 0.05f;
				break;

			case "berserk":
				if ( PlayerStaff != null ) PlayerStaff.RemoveBerserkAbility();
				break;

			case "radiation":
				if ( PlayerStaff != null ) PlayerStaff.RemoveRadiationSicknessLevel();
				break;

			case "goldfever":
				if ( PlayerStaff != null ) PlayerStaff.RemoveGoldFeverLevel();
				break;

			case "runspeed":
				if ( PlayerHealth != null ) PlayerHealth.RunSpeedMultiplier /= 1.10f;
				break;

			case "stam_regen":
				if ( PlayerHealth != null ) PlayerHealth.StaminaRegenRate /= 1.15f;
				break;

			case "stam_drain":
				if ( PlayerHealth != null ) PlayerHealth.StaminaDrainRate /= 0.90f;
				break;

			case "shield":
				if ( PlayerHealth != null ) PlayerHealth.RemoveShieldLevel();
				break;

			case "luck":
				if ( PlayerLuck != null ) PlayerLuck.AddLuck( -5f );
				break;

			case "magnet":
				{
					var magnet = MagnetAbilityController.FindIn( Scene );
					magnet?.RemoveUnlock();
				}
				break;
		}
	}

	private void Draw()
	{
		if ( Scene.Camera == null ) return;
		var hud = Scene.Camera.Hud;

		hud.DrawRect( new Rect( 0, 0, Screen.Width, Screen.Height ), new Color( 0, 0, 0, OverlayAlpha ) );

		var panel = GetPanelRect();
		float ui = GetUiScale();

		DrawRoundedRect( hud, panel.Left, panel.Top, panel.Width, panel.Height, PanelBg, PanelRadius * ui, 2f, PanelBorderColor );
		DrawCenteredText( hud, T( "shopupgrades.title", "УЛУЧШЕНИЯ", "UPGRADES" ), new Rect( panel.Left, panel.Top + 18f * ui, panel.Width, 54f * ui ), TitleColor, 42f * ui );

		int coins = PlayerStats?.Coins ?? 0;
		float luck = PlayerLuck?.LuckPercent ?? 0f;

		DrawCenteredText( hud, string.Format( T( "shopupgrades.coins", "🪙 Монет: {0}", "🪙 Coins: {0}" ), coins ), new Rect( panel.Left, panel.Top + 70f * ui, panel.Width, 24f * ui ), SubColor, 18f * ui );
		DrawCenteredText( hud, string.Format( T( "shopupgrades.luck", "🍀 Удача: {0:0.#}%", "🍀 Luck: {0:0.#}%" ), luck ), new Rect( panel.Left, panel.Top + 94f * ui, panel.Width, 24f * ui ), new Color( 0.75f, 1f, 0.55f, 0.85f ), 18f * ui );
		DrawCenteredText( hud, T( "shopupgrades.back_hint", "ESC / E — назад", "ESC / E — back" ), new Rect( panel.Left, panel.Bottom - 40f * ui, panel.Width, 24f * ui ), new Color( 1, 1, 1, 0.55f ), 18f * ui );

		var layout = GetLayout();
		DrawList( hud, layout.ListRect );
		DrawDetails( hud, layout.DetailsRect, layout.BuyRect );
	}

	private (Rect ListRect, Rect DetailsRect, Rect BuyRect) GetLayout()
	{
		var panel = GetPanelRect();
		float ui = GetUiScale();

		float leftPad = 28f * ui;
		float rightPad = 28f * ui;
		float topOffset = 126f * ui;
		float bottomPad = 56f * ui;
		float gap = 24f * ui;

		float contentHeight = panel.Height - topOffset - bottomPad;
		float listWidth = panel.Width * 0.56f;

		var list = new Rect( panel.Left + leftPad, panel.Top + topOffset, listWidth, contentHeight );
		var details = new Rect( list.Right + gap, panel.Top + topOffset, panel.Right - rightPad - (list.Right + gap), contentHeight );
		var buy = new Rect( details.Left + 42f * ui, details.Bottom - 76f * ui, details.Width - 84f * ui, 54f * ui );

		return (list, details, buy);
	}

	private void DrawList( HudPainter hud, Rect listRect )
	{
		float ui = GetUiScale();
		float rowHeight = GetActualRowHeight();

		DrawRoundedRect( hud, listRect.Left, listRect.Top, listRect.Width, listRect.Height, new Color( 1, 1, 1, 0.03f ), 14f * ui, 2f, new Color( 1, 1, 1, 0.08f ) );

		float contentH = Upgrades.Count * rowHeight;
		float maxScroll = Math.Max( 0, contentH - listRect.Height );
		_scroll = Clamp( _scroll, 0, maxScroll );

		Vector2 m = Mouse.Position;

		for ( int i = 0; i < Upgrades.Count; i++ )
		{
			float y = listRect.Top + i * rowHeight - _scroll;
			if ( y + rowHeight < listRect.Top ) continue;
			if ( y > listRect.Bottom ) break;

			var row = new Rect( listRect.Left + 10f * ui, y + 6f * ui, listRect.Width - 20f * ui, rowHeight - 12f * ui );

			bool hover = RectContains( row, m );
			bool selected = i == _selectedIndex;

			Color bg = selected ? new Color( 0.22f, 0.30f, 0.40f, 0.75f )
				: hover ? new Color( 0.16f, 0.20f, 0.28f, 0.65f )
				: new Color( 0.10f, 0.12f, 0.16f, 0.55f );

			DrawRoundedRect( hud, row.Left, row.Top, row.Width, row.Height, bg, 12f * ui, 2f, new Color( 1, 1, 1, 0.08f ) );

			var u = Upgrades[i];
			int owned = GetOwnedForUi( u.Id );
			int currentPrice = GetCurrentPrice( u, owned );
			bool canAfford = (PlayerStats?.Coins ?? 0) >= currentPrice;

			DrawCenteredText( hud, u.Icon, new Rect( row.Left + 8f * ui, row.Top + 6f * ui, 54f * ui, row.Height - 12f * ui ), new Color( 1, 1, 1, 0.95f ), 30f * ui );
			DrawTextLeft( hud, GetUpgradeDisplayName( u ), new Rect( row.Left + 68f * ui, row.Top + 10f * ui, row.Width - 150f * ui, 24f * ui ), new Color( 1, 1, 1, 0.95f ), 20f * ui );
			DrawTextLeft( hud, GetUpgradeDisplayDescription( u ), new Rect( row.Left + 68f * ui, row.Top + 38f * ui, row.Width - 150f * ui, 20f * ui ), new Color( 1, 1, 1, 0.55f ), 15f * ui );

			string right = $"{currentPrice}🪙";
			if ( owned > 0 ) right += $"   x{owned}";

			DrawTextRight( hud, right, new Rect( row.Left, row.Top + 20f * ui, row.Width - 16f * ui, 24f * ui ), canAfford ? new Color( 1, 1, 1, 0.75f ) : new Color( 1, 0.35f, 0.35f, 0.9f ), 18f * ui );
		}
	}

	private void DrawDetails( HudPainter hud, Rect rect, Rect buyRect )
	{
		float ui = GetUiScale();

		DrawRoundedRect( hud, rect.Left, rect.Top, rect.Width, rect.Height, new Color( 1, 1, 1, 0.03f ), 14f * ui, 2f, new Color( 1, 1, 1, 0.08f ) );

		if ( _selectedIndex < 0 || _selectedIndex >= Upgrades.Count )
			return;

		var u = Upgrades[_selectedIndex];
		int owned = GetOwnedForUi( u.Id );
		int currentPrice = GetCurrentPrice( u, owned );
		int nextPrice = GetNextPrice( u );

		DrawCenteredText( hud, $"{u.Icon}  {GetUpgradeDisplayName( u )}", new Rect( rect.Left + 10f * ui, rect.Top + 28f * ui, rect.Width - 20f * ui, 40f * ui ), new Color( 1, 1, 1, 0.95f ), 34f * ui );
		DrawCenteredText( hud, GetUpgradeDisplayDescription( u ), new Rect( rect.Left + 28f * ui, rect.Top + 96f * ui, rect.Width - 56f * ui, 72f * ui ), new Color( 1, 1, 1, 0.70f ), 20f * ui );
		DrawCenteredText( hud, string.Format( T( "shopupgrades.rarity", "Редкость: {0}", "Rarity: {0}" ), GetLocalizedRarity( u.Rarity ) ), new Rect( rect.Left + 28f * ui, rect.Top + 190f * ui, rect.Width - 56f * ui, 28f * ui ), new Color( 1, 1, 1, 0.55f ), 18f * ui );
		DrawCenteredText( hud, string.Format( T( "shopupgrades.current_price", "Текущая цена: {0}🪙", "Current price: {0}🪙" ), currentPrice ), new Rect( rect.Left + 28f * ui, rect.Top + 224f * ui, rect.Width - 56f * ui, 28f * ui ), new Color( 1, 1, 1, 0.55f ), 18f * ui );
		DrawCenteredText( hud, string.Format( T( "shopupgrades.price_multiplier", "Множитель цены: x{0:0.##}", "Price multiplier: x{0:0.##}" ), u.PriceMultiplier ), new Rect( rect.Left + 28f * ui, rect.Top + 258f * ui, rect.Width - 56f * ui, 28f * ui ), new Color( 1, 1, 1, 0.55f ), 18f * ui );

		if ( u.MaxStacks > 0 )
			DrawCenteredText( hud, string.Format( T( "shopupgrades.purchased_max", "Куплено: {0}/{1}", "Purchased: {0}/{1}" ), owned, u.MaxStacks ), new Rect( rect.Left + 28f * ui, rect.Top + 292f * ui, rect.Width - 56f * ui, 28f * ui ), new Color( 1, 1, 1, 0.55f ), 18f * ui );
		else
			DrawCenteredText( hud, string.Format( T( "shopupgrades.purchased", "Куплено: {0}", "Purchased: {0}" ), owned ), new Rect( rect.Left + 28f * ui, rect.Top + 292f * ui, rect.Width - 56f * ui, 28f * ui ), new Color( 1, 1, 1, 0.55f ), 18f * ui );

		if ( u.MaxStacks <= 0 || owned < u.MaxStacks )
			DrawCenteredText( hud, string.Format( T( "shopupgrades.next_price", "Следующая цена: {0}🪙", "Next price: {0}🪙" ), nextPrice ), new Rect( rect.Left + 28f * ui, rect.Top + 326f * ui, rect.Width - 56f * ui, 28f * ui ), new Color( 1f, 0.92f, 0.55f, 0.80f ), 18f * ui );

		bool canAfford = (PlayerStats?.Coins ?? 0) >= currentPrice;
		bool canBuy = u.MaxStacks <= 0 || owned < u.MaxStacks;

		Color buyBg = canAfford && canBuy ? new Color( 0.20f, 0.70f, 0.35f, 0.80f ) : new Color( 0.35f, 0.35f, 0.35f, 0.45f );
		DrawRoundedRect( hud, buyRect.Left, buyRect.Top, buyRect.Width, buyRect.Height, buyBg, 14f * ui, 2f, new Color( 1, 1, 1, 0.10f ) );

		string buyText = canBuy
			? string.Format( T( "shopupgrades.buy_for", "КУПИТЬ ЗА {0}🪙", "BUY FOR {0}🪙" ), currentPrice )
			: T( "shopupgrades.max", "МАКС.", "MAX" );
		DrawCenteredText( hud, buyText, buyRect, new Color( 1, 1, 1, 0.95f ), 24f * ui );
	}


	private string T( string key, string russianFallback, string englishFallback )
	{
		GameLocalization.EnsureLoaded();
		return GameLocalization.T( key, GameLocalization.IsLanguage( "ru" ) ? russianFallback : englishFallback );
	}

	private string GetLocalizedRarity( AbilityRarity rarity )
	{
		return rarity switch
		{
			AbilityRarity.Rare => T( "shopupgrades.rarity.rare", "Редкая", "Rare" ),
			AbilityRarity.Epic => T( "shopupgrades.rarity.epic", "Эпическая", "Epic" ),
			_ => T( "shopupgrades.rarity.common", "Обычная", "Common" )
		};
	}

	private string GetUpgradeDisplayName( ShopUpgradeDefinition def )
	{
		if ( def == null ) return string.Empty;

		return def.Id switch
		{
			"regen" => T( "shopupgrades.upgrade.regen.name", "Регенерация", "Regeneration" ),
			"maxhp" => T( "shopupgrades.upgrade.maxhp.name", "Увеличение HP", "Max HP Up" ),
			"damage5" => T( "shopupgrades.upgrade.damage5.name", "Урон +5", "Damage +5" ),
			"atkspd" => T( "shopupgrades.upgrade.atkspd.name", "Скорость атаки", "Attack Speed" ),
			"multitarget" => T( "shopupgrades.upgrade.multitarget.name", "Мульти-луч", "Multi Beam" ),
			"range" => T( "shopupgrades.upgrade.range.name", "Дальность луча", "Beam Range" ),
			"ricochet" => T( "shopupgrades.upgrade.ricochet.name", "Рикошет", "Ricochet" ),
			"crit" => T( "shopupgrades.upgrade.crit.name", "Критический разряд", "Critical Discharge" ),
			"execute" => T( "shopupgrades.upgrade.execute.name", "Добивание", "Execution" ),
			"killflow" => T( "shopupgrades.upgrade.killflow.name", "Поток убийств", "Kill Flow" ),
			"flowcalibration" => T( "shopupgrades.upgrade.flowcalibration.name", "Калибровка потока", "Flow Calibration" ),
			"critboost" => T( "shopupgrades.upgrade.critboost.name", "Усиление крита", "Critical Boost" ),
			"berserk" => T( "shopupgrades.upgrade.berserk.name", "Берсерк", "Berserk" ),
			"radiation" => T( "shopupgrades.upgrade.radiation.name", "Лучевая болезнь", "Radiation Sickness" ),
			"goldfever" => T( "shopupgrades.upgrade.goldfever.name", "Золотая лихорадка", "Gold Fever" ),
			"magnet" => T( "shopupgrades.upgrade.magnet.name", "Магнит", "Magnet" ),
			"runspeed" => T( "shopupgrades.upgrade.runspeed.name", "Скорость бега", "Run Speed" ),
			"stam_regen" => T( "shopupgrades.upgrade.stam_regen.name", "Реген стамины", "Stamina Regen" ),
			"stam_drain" => T( "shopupgrades.upgrade.stam_drain.name", "Экономия стамины", "Stamina Efficiency" ),
			"shield" => T( "shopupgrades.upgrade.shield.name", "Щит", "Shield" ),
			"luck" => T( "shopupgrades.upgrade.luck.name", "Удача", "Luck" ),
			_ => def.Name ?? string.Empty
		};
	}

	private string GetUpgradeDisplayDescription( ShopUpgradeDefinition def )
	{
		if ( def == null ) return string.Empty;

		return def.Id switch
		{
			"regen" => T( "shopupgrades.upgrade.regen.desc", "1 HP раз в 4 секунды", "1 HP every 4 seconds" ),
			"maxhp" => T( "shopupgrades.upgrade.maxhp.desc", "Макс. здоровье +20", "Max health +20" ),
			"damage5" => T( "shopupgrades.upgrade.damage5.desc", "Урон посоха +5", "Staff damage +5" ),
			"atkspd" => T( "shopupgrades.upgrade.atkspd.desc", "Кулдаун -10% (стак)", "Cooldown -10% (stack)" ),
			"multitarget" => T( "shopupgrades.upgrade.multitarget.desc", "+1 цель", "+1 target" ),
			"range" => T( "shopupgrades.upgrade.range.desc", "+5% дальность (стак)", "+5% range (stack)" ),
			"ricochet" => T( "shopupgrades.upgrade.ricochet.desc", "Луч перескакивает ещё на 1 цель", "Beam bounces to 1 extra nearby target" ),
			"crit" => T( "shopupgrades.upgrade.crit.desc", "Шанс крита +5% (x2 урон)", "Crit chance +5% (x2 damage)" ),
			"execute" => T( "shopupgrades.upgrade.execute.desc", "+15% урона по врагам ниже 30% HP", "+15% damage to enemies below 30% HP" ),
			"killflow" => T( "shopupgrades.upgrade.killflow.desc", "За килл посохом -0.1% КД (до -30%)", "Staff kills give -0.1% cooldown (up to -30%)" ),
			"flowcalibration" => T( "shopupgrades.upgrade.flowcalibration.desc", "Усиление килл-стака КД (один раз)", "Kill-stack cooldown boost (one-time)" ),
			"critboost" => T( "shopupgrades.upgrade.critboost.desc", "Множитель крита +0.25 (до x4.0)", "Crit multiplier +0.25 (up to x4.0)" ),
			"berserk" => T( "shopupgrades.upgrade.berserk.desc", "+К урону и скорости атаки при получении урона", "+damage and attack speed when taking damage" ),
			"radiation" => T( "shopupgrades.upgrade.radiation.desc", "Наносит длительный урон молнией", "Deals lingering lightning damage" ),
			"goldfever" => T( "shopupgrades.upgrade.goldfever.desc", "Больше золота за килы", "More gold for kills" ),
			"magnet" => T( "shopupgrades.upgrade.magnet.desc", "Каждые 145 сек притягивает монеты и EXP со всей карты", "Every 145 sec pulls coins and EXP from the whole map" ),
			"runspeed" => T( "shopupgrades.upgrade.runspeed.desc", "+10% скорость (стак)", "+10% speed (stack)" ),
			"stam_regen" => T( "shopupgrades.upgrade.stam_regen.desc", "+15% реген (стак)", "+15% regen (stack)" ),
			"stam_drain" => T( "shopupgrades.upgrade.stam_drain.desc", "-10% расход (стак)", "-10% drain (stack)" ),
			"shield" => T( "shopupgrades.upgrade.shield.desc", "Поглощает урон. +5 щита/лвл, реген 15с", "Absorbs damage. +5 shield/level, 15s regen" ),
			"luck" => T( "shopupgrades.upgrade.luck.desc", "+5% шанс более редкой награды из сундуков", "+5% chance for rarer chest rewards" ),
			_ => def.Description ?? string.Empty
		};
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

	private static void DrawCenteredText( HudPainter hud, string text, Rect rect, Color color, float size )
	{
		var scope = new TextRendering.Scope( text ?? "", color, size );
		hud.DrawText( scope, rect, TextFlag.Center );
	}

	private static void DrawTextLeft( HudPainter hud, string text, Rect rect, Color color, float size )
	{
		var scope = new TextRendering.Scope( text ?? "", color, size );
		hud.DrawText( scope, rect, TextFlag.Left );
	}

	private static void DrawTextRight( HudPainter hud, string text, Rect rect, Color color, float size )
	{
		var scope = new TextRendering.Scope( text ?? "", color, size );
		hud.DrawText( scope, rect, TextFlag.Right );
	}

	private static float Clamp( float v, float min, float max )
	{
		if ( v < min ) return min;
		if ( v > max ) return max;
		return v;
	}
}
