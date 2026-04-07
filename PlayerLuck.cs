using Sandbox;

public sealed class PlayerLuck : Component
{
	[Property] public float LuckPercent { get; set; } = 0f;
	[Property] public float MaxLuckPercent { get; set; } = 95f;

	public void AddLuck( float amount )
	{
		LuckPercent += amount;
		if ( LuckPercent < 0f ) LuckPercent = 0f;
		if ( LuckPercent > MaxLuckPercent ) LuckPercent = MaxLuckPercent;
	}
}
