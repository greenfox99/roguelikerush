using Sandbox;
using System;
using System.Collections.Generic;

public sealed class PlayerLevel : Component
{
	[Property] public int CurrentLevel { get; private set; } = 1;
	[Property] public int CurrentExp { get; private set; } = 0;

	public event Action<int> OnLevelUp;
	public event Action<string> OnAbilitySelected;

	[Property] public List<LevelRequirement> LevelRequirements { get; set; } = new();

	protected override void OnStart()
	{
		// ПРИНУДИТЕЛЬНО заполняем список правильными значениями
		LevelRequirements = new List<LevelRequirement>
		{
			new LevelRequirement { Level = 1, ExpNeeded = 0 },
			new LevelRequirement { Level = 2, ExpNeeded = 15 },
			new LevelRequirement { Level = 3, ExpNeeded = 30 },
			new LevelRequirement { Level = 4, ExpNeeded = 50 },
			new LevelRequirement { Level = 5, ExpNeeded = 75 },
			new LevelRequirement { Level = 6, ExpNeeded = 105 },
			new LevelRequirement { Level = 7, ExpNeeded = 140 },
			new LevelRequirement { Level = 8, ExpNeeded = 180 },
			new LevelRequirement { Level = 9, ExpNeeded = 225 },
			new LevelRequirement { Level = 10, ExpNeeded = 275 },
			new LevelRequirement { Level = 11, ExpNeeded = 330 },
			new LevelRequirement { Level = 12, ExpNeeded = 390 },
			new LevelRequirement { Level = 13, ExpNeeded = 455 },
			new LevelRequirement { Level = 14, ExpNeeded = 525 },
			new LevelRequirement { Level = 15, ExpNeeded = 600 },
			new LevelRequirement { Level = 16, ExpNeeded = 680 },
			new LevelRequirement { Level = 17, ExpNeeded = 765 },
			new LevelRequirement { Level = 18, ExpNeeded = 855 },
			new LevelRequirement { Level = 19, ExpNeeded = 950 },
			new LevelRequirement { Level = 20, ExpNeeded = 1050 },
			new LevelRequirement { Level = 21, ExpNeeded = 1155 },
			new LevelRequirement { Level = 22, ExpNeeded = 1265 },
			new LevelRequirement { Level = 23, ExpNeeded = 1380 },
			new LevelRequirement { Level = 24, ExpNeeded = 1500 },
			new LevelRequirement { Level = 25, ExpNeeded = 1625 },
			new LevelRequirement { Level = 26, ExpNeeded = 1755 },
			new LevelRequirement { Level = 27, ExpNeeded = 1890 },
			new LevelRequirement { Level = 28, ExpNeeded = 2030 },
			new LevelRequirement { Level = 29, ExpNeeded = 2175 },
			new LevelRequirement { Level = 30, ExpNeeded = 2325 }
		};

		CurrentLevel = 1;
		CurrentExp = 0;

		Log.Info( $"=== ДИАГНОСТИКА УРОВНЕЙ ===" );
		Log.Info( $"Всего элементов в LevelRequirements: {LevelRequirements.Count}" );

		for ( int i = 0; i < LevelRequirements.Count; i++ )
		{
			Log.Info( $"  [{i}] Level: {LevelRequirements[i].Level}, ExpNeeded: {LevelRequirements[i].ExpNeeded}" );
		}

		Log.Info( $"==========================" );
		Log.Info( $"⭐ Игрок стартует с уровнем {CurrentLevel}, нужно опыта до 2: {GetExpNeededForLevel( 2 )}" );
	}

	public void AddExp( int amount )
	{
		CurrentExp += amount;
		Log.Info( $"✨ +{amount} опыта! Всего: {CurrentExp}" );

		CheckLevelUp();
	}

	private void CheckLevelUp()
	{
		bool leveledUp = false;

		while ( CurrentLevel < LevelRequirements.Count )
		{
			int expNeeded = GetExpNeededForLevel( CurrentLevel + 1 );

			if ( CurrentExp >= expNeeded )
			{
				CurrentLevel++;
				leveledUp = true;
				Log.Info( $"⭐ УРОВЕНЬ {CurrentLevel} ДОСТИГНУТ!" );
			}
			else
			{
				break;
			}
		}

		if ( leveledUp )
		{
			OnLevelUp?.Invoke( CurrentLevel );
		}
	}

	public int GetExpNeededForLevel( int level )
	{
		foreach ( var req in LevelRequirements )
		{
			if ( req.Level == level )
				return req.ExpNeeded;
		}
		return 0;
	}

	public int GetExpNeededForNextLevel()
	{
		return GetExpNeededForLevel( CurrentLevel + 1 );
	}

	public float GetLevelProgress()
	{
		int currentLevelExp = GetExpNeededForLevel( CurrentLevel );
		int nextLevelExp = GetExpNeededForLevel( CurrentLevel + 1 );

		if ( nextLevelExp == 0 ) return 1f;

		return (float)(CurrentExp - currentLevelExp) / (nextLevelExp - currentLevelExp);
	}

	public void NotifyAbilitySelected( string abilityName )
	{
		OnAbilitySelected?.Invoke( abilityName );
	}
}

[System.Serializable]
public class LevelRequirement
{
	public int Level;
	public int ExpNeeded;
}
