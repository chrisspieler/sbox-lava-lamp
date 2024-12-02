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

			return BBox.FromPositionAndSize( Vector3.Zero, World.SimulationSize * 1.5f );
		}
	}

	[Property] public Model BoundsModel { get; set; }

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
		_sceneObject.Flags.IsOpaque = true;
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
		var attributes = GetMetaballShaderAttributes();
		var tx = new Transform()
		{
			Position = WorldPosition,
			Rotation = WorldRotation,
			Scale = LocalBounds.Size
		};
		Graphics.DrawModel( BoundsModel, tx, attributes );
	}

	[Property, Range( 0, 10 )] public float ColorBlendScale { get; set; } = 2.5f;
	[Property, Range( 0, 20 )] public float ShapeBlendScale { get; set; } = 5f;
	[Property] public bool ShowBounds { get; set; } = false;
	[Property, Range( 0f, 4f )] public float BoundsMargin { get; set; } = 0.25f;

	[Property, Group( "Lamp" )] 
	public Vector3 LampOffset { get; set; } = Vector3.Zero;
	[Property, Group( "Lamp" )]
	public Vector3 LampBottomCenter { get; set; } = Vector3.Down * 7.75f;
	[Property, Group( "Lamp" )]
	public Vector3 LampTopCenter { get; set; } = Vector3.Up * 7.75f;
	[Property, Group( "Lamp" ), Range( 0.125f, 16f )]
	public float LampBottomRadius { get; set; } = 3.75f;
	[Property, Group( "Lamp" ), Range( 0.125f, 16f )]
	public float LampTopRadius { get; set; } = 2.5f;

	private RenderAttributes GetMetaballShaderAttributes()
	{
		var metaballData = World.Metaballs
			.Select( mb => mb.GetRenderData() )
			.ToList();

		var attributes = new RenderAttributes();
		var transform = Matrix.CreateScale( LocalBounds.Size, Vector3.Zero )
			* Matrix.CreateRotation( _sceneObject.Rotation )
			* Matrix.CreateTranslation( _sceneObject.Position );
		attributes.Set( "Transform", transform );
		attributes.Set( "ShowBounds", ShowBounds ? 1 : 0 );
		attributes.Set( "BoundsMarginWs", BoundsMargin );
		attributes.SetData( "BallBuffer", metaballData );
		attributes.Set( "BallCount", metaballData.Count );
		attributes.Set( "WorldPosition", WorldPosition );
		attributes.Set( "SimulationSize", World.SimulationSize );
		attributes.Set( "ColorBlendScale", ColorBlendScale );
		attributes.Set( "ShapeBlendScale", ShapeBlendScale );
		attributes.Set( "LampOffset", LampOffset );
		attributes.Set( "LampBottomCenter", LampBottomCenter );
		attributes.Set( "LampTopCenter", LampTopCenter );
		attributes.Set( "LampBottomRadius", LampBottomRadius );
		attributes.Set( "LampTopRadius", LampTopRadius );
		return attributes;
	}
}
