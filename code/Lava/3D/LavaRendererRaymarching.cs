public class LavaRendererRaymarching : Component, Component.IHasBounds
{
	[Property] public LavaWorld World { get; set; }

	public BBox LocalBounds
	{
		get
		{
			if ( !World.IsValid() )
			{
				return BBox.FromPositionAndSize( 0f, 1f );
			}

			return BBox.FromPositionAndSize( Vector3.Zero, World.SimulationSize );
		}
	}
	private readonly Material Material = Metaball.Material3D;
	private SceneCustomObject _sceneObject;

	protected override void OnEnabled()
	{
		World ??= GetComponent<LavaWorld>();
		World ??= Scene.GetAllComponents<LavaWorld>().FirstOrDefault();
		if ( !World.IsValid() )
			return;

		
		_sceneObject ??= new SceneCustomObject( Scene.SceneWorld );
		_sceneObject.RenderOverride = Render;
		_sceneObject.Tags.Add( Tags );
	}

	protected override void OnUpdate()
	{
		if ( !_sceneObject.IsValid() )
			return;

		_sceneObject.Transform = WorldTransform;
	}

	protected override void OnDisabled()
	{
		_sceneObject?.Delete();
		_sceneObject = null;
	}

	private void Render( SceneObject sceneObject )
	{
		if ( !World.IsValid() )
			return;

		sceneObject.Transform = Transform.World;
		var attributes = UpdateAttributes();

		Graphics.Blit( Material, attributes );
	}

	private RenderAttributes UpdateAttributes()
	{
		var metaballData = World.Metaballs
			.Select( mb => mb.GetRenderData() )
			.ToList();

		var attributes = new RenderAttributes();
		attributes.SetData( "BallBuffer", metaballData );
		attributes.Set( "BallCount", metaballData.Count );
		return attributes;
	}
}
