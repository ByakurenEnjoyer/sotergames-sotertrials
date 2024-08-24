using Sandbox;

public sealed class TTD : Component, Component.ITriggerListener, Component.ICollisionListener
{
	[Property] public CharacterController Player { get; set; }
	[Property] public BoxCollider collider {get; set;}
	[Property] public List<GameObject> SpawnPoints { get; set; }

	protected override void OnUpdate()
	{

	}

  	void ITriggerListener.OnTriggerEnter( Collider other )
    {
	   if (other.Components.TryGet(out PlayerController pc))
	   MoveToSpawn(pc.GameObject);
	   
	}
	[Broadcast]
	void MoveToSpawn(GameObject toMove)
	{
		toMove.Transform.Position = SpawnPoints[0].Transform.Position;
	}

	public void OnTriggerExit( Collider other )
    {
       Log.Info(other);
	}
}
