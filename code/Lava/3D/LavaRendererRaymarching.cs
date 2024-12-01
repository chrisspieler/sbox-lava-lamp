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

	public Vector3 ScreenToPoint( Vector2 mousePos )
	{
		var camera = Scene?.Camera;
		if ( !camera.IsValid() )
			return default;

		var plane = new Plane( WorldPosition, WorldRotation.Forward );
		var ray = camera.ScreenPixelToRay( mousePos );
		var position = plane.Trace( ray, true ) ?? default;
		position = WorldTransform.PointToLocal( position );
		return position;
	}

	protected override void OnEnabled()
	{
		World ??= GetComponent<LavaWorld>();
		World ??= Scene.GetAllComponents<LavaWorld>().FirstOrDefault();
		if ( !World.IsValid() )
			return;

		
		_sceneObject ??= new SceneCustomObject( Scene.SceneWorld );
		_sceneObject.RenderOverride = Render;
		_sceneObject.Tags.Add( Tags );
		_sceneObject.LocalBounds = BBox.FromPositionAndSize( WorldPosition, World.SimulationSize * 1.5f );
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

	[Property, Range( 0, 10 )] public float ColorBlendScale { get; set; } = 2.5f;
	[Property, Range( 0, 20 )] public float ShapeBlendScale { get; set; } = 5f;


	private RenderAttributes UpdateAttributes()
	{
		var metaballData = World.Metaballs
			.Select( mb => mb.GetRenderData() )
			.ToList();

		var attributes = new RenderAttributes();
		attributes.SetData( "BallBuffer", metaballData );
		attributes.Set( "BallCount", metaballData.Count );
		attributes.Set( "WorldPosition", WorldPosition );
		attributes.Set( "SimulationSize", World.SimulationSize );
		attributes.Set( "ColorBlendScale", ColorBlendScale );
		attributes.Set( "ShapeBlendScale", ShapeBlendScale );
		return attributes;
	}
}
