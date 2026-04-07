using Sandbox;

public sealed class ShopTrigger : Component, Component.ITriggerListener
{
	public bool PlayerInside { get; private set; }

	public void OnTriggerEnter( GameObject other )
	{
		// проверяем что вошёл именно игрок
		if ( other.Components.Get<Player>() == null )
			return;

		PlayerInside = true;
		Log.Info( $"[SHOP TRIGGER] ENTER: {other.Name}" );
	}

	public void OnTriggerExit( GameObject other )
	{
		if ( other.Components.Get<Player>() == null )
			return;

		PlayerInside = false;
		Log.Info( $"[SHOP TRIGGER] EXIT: {other.Name}" );
	}
}
