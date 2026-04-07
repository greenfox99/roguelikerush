using Sandbox;
using System.Linq;

public sealed class SkillChest : Component
{
	[Property] public float InteractDistance { get; set; } = 90f;
	[Property] public string UseActionName { get; set; } = "Use";

	[Property] public SoundEvent OpenSound { get; set; }
	[Property] public GameObject OpenFxPrefab { get; set; }

	[Property] public ChestSpawnManager SpawnManager { get; set; }

	public bool IsOpened { get; private set; } = false;

	private Player _player;
	private SkillChestMenu _menu;

	protected override void OnStart()
	{
		_player = Scene.GetAllComponents<Player>().FirstOrDefault();
		_menu = Scene.GetAllComponents<SkillChestMenu>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		if ( IsOpened )
			return;

		if ( _player == null || !_player.IsValid )
			_player = Scene.GetAllComponents<Player>().FirstOrDefault();

		if ( _menu == null || !_menu.IsValid )
			_menu = Scene.GetAllComponents<SkillChestMenu>().FirstOrDefault();

		if ( _player == null || _menu == null )
			return;

		float dist = Vector3.DistanceBetween( Transform.Position, _player.Transform.Position );
		if ( dist > InteractDistance )
			return;

		if ( Input.Pressed( UseActionName ) )
		{
			_menu.OpenChest( this );
		}
	}

	public void MarkOpenedAndConsume()
	{
		if ( IsOpened )
			return;

		IsOpened = true;

		if ( OpenSound != null )
			Sound.Play( OpenSound, Transform.Position );

		if ( OpenFxPrefab != null )
			OpenFxPrefab.Clone( Transform.Position );

		SpawnManager?.NotifyChestOpened( this );
		GameObject.Destroy();
	}
}
