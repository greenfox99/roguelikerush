using Sandbox;
using System;

public sealed class AbilityRarityConfig : Component
{
	// Базовые веса (не обязательно должны суммироваться в 1 — мы нормализуем)
	[Property, Range( 0f, 1f )] public float CommonWeight { get; set; } = 0.72f;
	[Property, Range( 0f, 1f )] public float RareWeight { get; set; } = 0.23f;
	[Property, Range( 0f, 1f )] public float EpicWeight { get; set; } = 0.05f;

	// Опционально: рост редкости с уровнем (очень удобная ручка)
	// Например: +0.003 к EpicWeight за уровень (после нормализации всё будет корректно)
	[Property] public bool ScaleWithLevel { get; set; } = false;
	[Property] public int LevelStart { get; set; } = 1;

	[Property] public float RareBonusPerLevel { get; set; } = 0.0f;
	[Property] public float EpicBonusPerLevel { get; set; } = 0.0f;

	// Жёсткие клампы на случай дикого баланса
	[Property, Range( 0f, 1f )] public float MinCommonChance { get; set; } = 0.05f;
	[Property, Range( 0f, 1f )] public float MaxEpicChance { get; set; } = 0.60f;

	public AbilityRarity RollRarity( Random rng, int playerLevel )
	{
		if ( rng == null )
			throw new ArgumentNullException( nameof( rng ) );

		float cw = MathF.Max( 0f, CommonWeight );
		float rw = MathF.Max( 0f, RareWeight );
		float ew = MathF.Max( 0f, EpicWeight );

		if ( ScaleWithLevel )
		{
			int lvl = Math.Max( 0, playerLevel - LevelStart );
			if ( lvl > 0 )
			{
				rw += RareBonusPerLevel * lvl;
				ew += EpicBonusPerLevel * lvl;
			}
		}

		NormalizeClamp( ref cw, ref rw, ref ew );

		float r = (float)rng.NextDouble();
		if ( r < ew ) return AbilityRarity.Epic;
		if ( r < ew + rw ) return AbilityRarity.Rare;
		return AbilityRarity.Common;
	}

	private void NormalizeClamp( ref float common, ref float rare, ref float epic )
	{
		// Если всё ноль — дефолт
		float sum = common + rare + epic;
		if ( sum <= 0.0001f )
		{
			common = 0.8f;
			rare = 0.18f;
			epic = 0.02f;
			sum = 1f;
		}

		common /= sum;
		rare /= sum;
		epic /= sum;

		// clamp’ы
		epic = Clamp01( epic );
		rare = Clamp01( rare );
		common = Clamp01( common );

		// min common
		if ( common < MinCommonChance )
		{
			float need = MinCommonChance - common;
			common = MinCommonChance;

			// урезаем rare/epic пропорционально
			float pool = rare + epic;
			if ( pool > 0.0001f )
			{
				float k = MathF.Max( 0f, (pool - need) ) / pool;
				rare *= k;
				epic *= k;
			}
			else
			{
				rare = 1f - common;
				epic = 0f;
			}
		}

		// max epic
		if ( epic > MaxEpicChance )
		{
			float excess = epic - MaxEpicChance;
			epic = MaxEpicChance;

			// добавляем в rare/common пропорционально текущему
			float pool = common + rare;
			if ( pool > 0.0001f )
			{
				common += excess * (common / pool);
				rare += excess * (rare / pool);
			}
			else
			{
				common = 1f - epic;
				rare = 0f;
			}
		}

		// финальная нормализация, чтобы сумма = 1
		float s2 = common + rare + epic;
		if ( s2 <= 0.0001f )
		{
			common = 0.8f;
			rare = 0.18f;
			epic = 0.02f;
			return;
		}

		common /= s2;
		rare /= s2;
		epic /= s2;
	}

	private static float Clamp01( float v )
	{
		if ( v < 0f ) return 0f;
		if ( v > 1f ) return 1f;
		return v;
	}
}
