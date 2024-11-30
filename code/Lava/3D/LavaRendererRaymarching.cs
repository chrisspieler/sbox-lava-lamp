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

		UpdateAttributes();

		Graphics.Blit( Material );
	}

	private void UpdateAttributes()
	{
		var metaballData = World.Metaballs
			.Select( mb => mb.GetRenderData() )
			.ToList();

		Graphics.Attributes.SetData( "BallBuffer", metaballData );
		Graphics.Attributes.Set( "BallCount", metaballData.Count );
	}
}
