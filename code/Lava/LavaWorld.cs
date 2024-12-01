using System;

public partial class LavaWorld : Component, Component.IHasBounds
{
	/// <summary>
	/// Defines the bounds of the simulation space. 
	/// <br/><br/>
	/// The origin of the simulation space is at the exact center of a bounding box of size SimulationSize.
	/// In worldspace, the origin will be the postion of the GameObject that holds this component.
	/// </summary>
	[Property] public Vector3 SimulationSize 
	{
		get => _simulationSize * WorldScale;
		set
		{
			value = value.Clamp( 1f, 1024f );
			_simulationSize = value;
			foreach ( var metaball in Metaballs )
			{
				metaball.Position = metaball.Position.Clamp( -SimulationSize * 0.95f, SimulationSize * 0.95f );
			}
		}
	}
	private Vector3 _simulationSize = new( 16, 16, 16 );
	[Property] public float PointToWorldScale
	{
		get
		{
			var xyMin = MathF.Min( SimulationSize.x, SimulationSize.y );
			return MathF.Min( xyMin, SimulationSize.z );
		}
	}
	[Property] public BBox WorldBounds => LocalBounds.Transform( WorldTransform );
	public BBox LocalBounds => BBox.FromPositionAndSize( Vector3.Zero, SimulationSize );

	public IEnumerable<Metaball> Metaballs => _metaballs;
	private List<Metaball> _metaballs = new();
	[Property] public int MetaballCount => _metaballs.Count;

	public Metaball this[int i ]
	{
		get
		{
			return _metaballs[i];
		}
	}

	protected override void OnStart()
	{
		_convectionNoiseSeed = Game.Random.Int( 0, 5000 );
	}

	protected override void OnEnabled()
	{
		InitializeColliders();
	}

	protected override void OnDisabled()
	{
		DestroyColliders();
	}

	protected override void OnUpdate()
	{
		ApplyColliderPositions();
		ApplyHeat();
		ApplyDamping();
		AttractToGravity();
		AttractToLava();
		UpdateVelocity();
		UpdateColor();
	}

	protected override void OnFixedUpdate()
	{
		FixedUpdateVelocity();
	}

	/// <summary>
	/// Convert a point in simulation space to an unclamped point in UV space with a top-left origin.
	/// </summary>
	public Vector2 PointToUV( Vector3 simPosition )
	{
		var normalPos = simPosition / ( SimulationSize * 0.5f );
		var uv = new Vector2( -normalPos.y, -normalPos.z );
		uv /= 2f;
		uv += 0.5f;
		return uv;
	}

	public Vector3 UVToPoint( Vector2 uv )
	{
		// Remap from 0 to 1 -> -1 to 1
		uv -= 0.5f;
		uv *= 2;
		var worldPos = new Vector3( 0f, -uv.x, -uv.y );
		worldPos *= SimulationSize;
		return worldPos;
	}

	public Metaball AddMetaball( Vector3 position, Color color, float radius )
	{
		_metaballs ??= new List<Metaball>();
		if ( _metaballs.Count >= Metaball.MAX_BALLS )
		{
			Log.Info( $"Unable to add metaball to {GameObject.Name}: metaball limit reached." );
			return null;
		}

		color = RandomizeHsv( color );
		var metaball = new Metaball( this, position, color, radius )
		{
			Temperature = Game.Random.Float( 0, MaxTemperature * 0.2f )
		};
		_metaballs.Add( metaball );
		CreateBallCollider( metaball );
		return metaball;
	}
}
