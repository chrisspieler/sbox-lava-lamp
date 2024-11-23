using Sandbox.Diagnostics;
using Sandbox.Polygons;
using Sandbox.Sdf;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

public partial class Lava3D : Component
{
	[Property] public LavaWorld LavaWorld { get; set; }
	[Property] public Sdf3DWorld SDFWorld { get; set; }
	[Property] public Sdf3DVolume SDFVolume { get; set; }

	public Vector3 LavaToWorld( Vector2 lavaPos )
	{
		// SDF size is world scale, so LavaToWorld must be found before LavaToLocal.
		var scale = SDFWorld.IsValid() ? SDFWorld.Size : WorldScale;
		var localPos = new Vector3( 0f, -lavaPos.x, -lavaPos.y ) * scale;
		// Awful hack to perpetuate another hack where the LavaWorld is scaled by aspect ratio.
		localPos.y *= Screen.Height / Screen.Width;
		localPos = ( localPos + SDFWorld.Size ) / 2f;
		var worldPos = WorldPosition + localPos;
		// Log.Info( $"lavaPos: {lavaPos}, scale: {scale}, localPos: {localPos}, worldPos: {worldPos}" );
		return worldPos;
	}

	public Vector3 LavaScaleToWorld( Vector2 lavaScale )
	{
		var scale = SDFWorld.IsValid() ? SDFWorld.Size : WorldScale;
		// Lava world is twice as large as SDF world.
		scale *= 0.5f;
		// We assume that a line or thin rectangle in 2D should have little depth in 3D.
		var xScale = MathF.Min( lavaScale.x, lavaScale.y );
		var worldScale = new Vector3( xScale, lavaScale.x, lavaScale.y ) * scale;
		// Log.Info( $"lavaScale: {lavaScale}, scale: {scale}, xScale: {xScale}, worldScale: {worldScale}" );
		return worldScale;
	}

	public Vector3 LavaToLocal( Vector2 lavaPos ) => WorldTransform.PointToLocal( LavaToWorld( lavaPos ) );

	protected override void OnStart()
	{
		LavaWorld ??= Components.GetInDescendantsOrSelf<LavaWorld>();
		SDFWorld ??= Components.GetInDescendantsOrSelf<Sdf3DWorld>();
	}

	private Task InitializationTask { get; set; } = null;

	protected override void OnUpdate()
	{
		if ( !SDFWorld.IsValid() || !LavaWorld.IsValid() )
			return;

		// If we have metaballs, haven't added them to the SDF world, and aren't in the process of doing so...
		if ( LavaWorld.MetaballCount != 0 && InitializationTask == null && SDFWorld.ModificationCount == 0 )
			InitializeSDFWorld();
	}

	private async void InitializeSDFWorld()
	{
		if ( !SDFVolume.IsValid() )
			return;

		var timer = FastTimer.StartNew();
		var tasks = CreateAllSDFShapes( SDFVolume );
		InitializationTask = Task.WhenAll( tasks );
		await InitializationTask;
		Log.Info( $"Initialized SDF world with {LavaWorld.MetaballCount} spheres in: {timer.ElapsedMilliSeconds:F2}ms" );
	}

	private IEnumerable<Task> CreateAllSDFShapes( Sdf3DVolume volume )
	{
		foreach ( var metaball in LavaWorld.Metaballs )
		{
			yield return CreateMetaballSDF( volume, metaball );
		}
	}

	private Task CreateMetaballSDF( Sdf3DVolume volume, Metaball2D metaball )
	{
		if ( metaball is null || volume is null )
			return Task.CompletedTask;

		var position = LavaToWorld( metaball.Position );
		var radius = LavaScaleToWorld( metaball.Radius );
		var sphere = new SphereSdf3D( position, radius.x );
		return SDFWorld.AddAsync( new SphereSdf3D( position, radius.x ), volume );
	}
}
