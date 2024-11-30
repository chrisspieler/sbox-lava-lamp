using Sandbox.Diagnostics;
using Sandbox.Sdf;
using System.Threading.Tasks;

public partial class LavaRendererLibsdf : Component
{
	[Property] public LavaWorld LavaWorld { get; set; }
	[Property] public Sdf3DWorld SDFWorld { get; set; }
	[Property] public Sdf3DVolume SDFVolume { get; set; }
	[Property, Range( 1f, 16f )] public float SDFScale { get; set; } = 8f;

	private Dictionary<Metaball, SphereSdf3D> _metaballSdf = new();
	[Property] public string SdfLastUpdateTime => $"{_sdfLastUpdateTime:F2}ms";
	private float _sdfLastUpdateTime;

	[Property] public string SdfInitializeTime => $"{_sdfInitializeTime:F2}ms";
	private float _sdfInitializeTime;

	protected override void OnStart()
	{
		LavaWorld ??= Components.GetInDescendantsOrSelf<LavaWorld>();
		SDFWorld ??= Components.GetInDescendantsOrSelf<Sdf3DWorld>();
	}

	private Task InitializationTask { get; set; } = null;
	private Task UpdateTask { get; set; } = null;

	protected override void OnUpdate()
	{
		if ( !SDFWorld.IsValid() || !LavaWorld.IsValid() )
			return;

		// If we have metaballs, haven't added them to the SDF world, and aren't in the process of doing so...
		if ( LavaWorld.MetaballCount != 0 && InitializationTask == null && SDFWorld.ModificationCount == 0 )
			InitializeSDFWorld();

		UpdateTransform();

		if ( UpdateTask is null )
		{
			UpdateMetaballs();
		}
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.Color = Color.Blue;
		Gizmo.Draw.LineBBox( LocalBounds );
		Gizmo.Draw.Color = Color.Green;
		foreach( var metaball in _metaballSdf )
		{
			var metaballPosition = LavaToLocal( metaball.Key.Position );
			Gizmo.Draw.LineSphere( new Sphere( metaballPosition, 4f ) );
		}
	}

	private void UpdateTransform()
	{
		SDFWorld.Size = LavaWorld.SimulationSize * SDFScale;
		LocalScale = 1f / ( SDFScale );
		WorldRotation = LavaWorld.WorldRotation;
		var offset = LavaWorld.WorldTransform.PointToWorld( -LavaWorld.SimulationSize * 0.5f );
		WorldPosition = offset;
	}

	public BBox LocalBounds
	{
		get
		{
			if ( !SDFWorld.IsValid() || !LavaWorld.IsValid() )
				return BBox.FromPositionAndSize( 32f, 64f );

			var position = SDFScale * LavaWorld.SimulationSize * 0.5f;
			var size = SDFScale * LavaWorld.SimulationSize;
			return BBox.FromPositionAndSize( position, size );
		}
	}

	public Vector3 LavaToLocal( Vector3 lavaPos )
	{
		lavaPos *= SDFScale * 0.5f;
		lavaPos += LavaWorld.SimulationSize * SDFScale * 0.5f;
		return lavaPos;
	}

	private async void InitializeSDFWorld()
	{
		if ( !SDFVolume.IsValid() )
			return;

		var timer = FastTimer.StartNew();
		SDFWorld.Opacity = 0.99f;
		var tasks = CreateAllSDFShapes( SDFVolume );
		InitializationTask = Task.WhenAll( tasks );
		await InitializationTask;
		_sdfInitializeTime = (float)timer.ElapsedMilliSeconds;
	}

	private IEnumerable<Task> CreateAllSDFShapes( Sdf3DVolume volume )
	{
		foreach ( var metaball in LavaWorld.Metaballs )
		{
			yield return CreateMetaballSDF( volume, metaball );
		}
	}

	private Task CreateMetaballSDF( Sdf3DVolume volume, Metaball metaball )
	{
		if ( metaball is null || volume is null )
			return Task.CompletedTask;

		var position = LavaToLocal( metaball.Position );
		var radius = metaball.Radius;
		var sphere = new SphereSdf3D( position, radius );
		_metaballSdf[metaball] = sphere;
		return SDFWorld.AddAsync( sphere, volume );
	}

	private async void UpdateMetaballs()
	{
		/* To update a metaball, we remove a sphere from the SDFWorld in the shape of the previously
		 * cached position and radius of the metaball. Then, we add a new sphere to the SDFWorld using 
		 * the current position and radius of the LavaWorld metaball. 
		 * 
		 * This results in two operations: Add and Subtract.
		 * 
		 * Using this method, 48 metaballs (that's 96 operations total) would update in about 16ms.
		 *																
		 * If instead of using a Subtract operation, we clear the SDF world and apply only the Add operations,
		 * updates take longer - about 32ms. So in this case, it's faster to double up on operations.
		 */

		var timer = FastTimer.StartNew();
		var modInfo = GetMetaballModifications( SDFVolume, _metaballSdf );
		UpdateTask = SDFWorld.SetModificationsAsync( modInfo.Mods );
		await UpdateTask;
		_metaballSdf = modInfo.NewSpheres;
		UpdateTask = null;
		_sdfLastUpdateTime = (float)timer.ElapsedMilliSeconds;
	}

	private record MetaballModInfo( IEnumerable<Modification<Sdf3DVolume, ISdf3D>> Mods, Dictionary<Metaball, SphereSdf3D> NewSpheres);

	private MetaballModInfo GetMetaballModifications( Sdf3DVolume volume, Dictionary<Metaball, SphereSdf3D> oldSpheres )
	{
		var subtractions = new List<Modification<Sdf3DVolume, ISdf3D>>();
		var additions = new List<Modification<Sdf3DVolume, ISdf3D>>();
		var newSpheres = new Dictionary<Metaball, SphereSdf3D>();
		foreach( ( Metaball metaball, SphereSdf3D sphere ) in oldSpheres )
		{
			subtractions.Add( new( sphere, volume, Operator.Subtract ) );
			var position = LavaToLocal( metaball.Position );
			var radius = metaball.Radius * ( 1f / LocalScale );
			var newSphere = new SphereSdf3D( position, radius.x );
			additions.Add( new( newSphere, volume, Operator.Add ) );
			newSpheres[metaball] = newSphere;
		}
		var mods = new List<Modification<Sdf3DVolume, ISdf3D>>();
		mods.AddRange( subtractions );
		mods.AddRange( additions );
		return new MetaballModInfo( mods, newSpheres );
	}
}
