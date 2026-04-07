using Sandbox;
using System;

public sealed class ChestRarityConfig : Component
{
	[Property, Group( "Base Chances" )] public float CommonChance { get; set; } = 75f;
	[Property, Group( "Base Chances" )] public float RareChance { get; set; } = 20f;
	[Property, Group( "Base Chances" )] public float EpicChance { get; set; } = 5f;

	[Property, Group( "Luck Scaling" )] public float RarePerLuckPercent { get; set; } = 0.25f;
	[Property, Group( "Luck Scaling" )] public float EpicPerLuckPercent { get; set; } = 0.10f;

	public AbilityRarity RollRarity( Random rng, float luckPercent )
	{
		float common = CommonChance;
		float rare = RareChance;
		float epic = EpicChance;

		float luck = MathF.Max( 0f, luckPercent );

		rare += luck * RarePerLuckPercent;
		epic += luck * EpicPerLuckPercent;

		// забираем вес у common
		common -= luck * (RarePerLuckPercent + EpicPerLuckPercent);

		if ( common < 0f ) common = 0f;
		if ( rare < 0f ) rare = 0f;
		if ( epic < 0f ) epic = 0f;

		float total = common + rare + epic;
		if ( total <= 0.001f )
			return AbilityRarity.Common;

		common /= total;
		rare /= total;
		epic /= total;

		float roll = (float)rng.NextDouble();

		if ( roll < epic )
			return AbilityRarity.Epic;

		if ( roll < epic + rare )
			return AbilityRarity.Rare;

		return AbilityRarity.Common;
	}

	public string GetChanceText( float luckPercent )
	{
		float common = CommonChance;
		float rare = RareChance;
		float epic = EpicChance;

		float luck = MathF.Max( 0f, luckPercent );

		rare += luck * RarePerLuckPercent;
		epic += luck * EpicPerLuckPercent;
		common -= luck * (RarePerLuckPercent + EpicPerLuckPercent);

		if ( common < 0f ) common = 0f;
		if ( rare < 0f ) rare = 0f;
		if ( epic < 0f ) epic = 0f;

		float total = common + rare + epic;
		if ( total <= 0.001f )
			total = 1f;

		common = (common / total) * 100f;
		rare = (rare / total) * 100f;
		epic = (epic / total) * 100f;

		return $"Обычный: {common:0.#}%   Редкий: {rare:0.#}%   Эпический: {epic:0.#}%";
	}
}
