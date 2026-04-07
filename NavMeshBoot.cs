using Sandbox;

public sealed class NavMeshBoot : Component
{
	protected override void OnStart()
	{
		Scene.NavMesh.SetDirty();
		Log.Info( "✅ NavMesh SetDirty() called" );
	}
}
