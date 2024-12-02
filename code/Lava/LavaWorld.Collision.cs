using System;
using System.Reflection.Metadata;

public partial class LavaWorld : Component
{
	internal record LavaCollider( Metaball Metaball, SphereCollider Collider, Rigidbody Rigidbody ) : IValid
	{
		public float Radius
		{
			get => Collider.Radius;
			set => Collider.Radius = value;
		}

		public bool IsValid => Rigidbody.IsValid();

		public void DestroyGameObject()
		{
			Rigidbody.DestroyGameObject();
		}
	}

	[Property, FeatureEnabled( "Collision", Icon = "sports_tennis" )]
	public bool EnableCollision { get; set; } = true;

	[Property, Range( -10f, 10f ), Feature( "Collision" )]
	public float WallBounce { get; set; } = 2f;

	[Property, Feature( "Collision" )]
	public GameObject LavaColliderContainer { get; set; }
	[Property, Feature( "Collision" )]
	public int LavaColliderCount => _activeLavaColliders.Count;
	internal IEnumerable<LavaCollider> LavaColliders => _activeLavaColliders.Values;
	private readonly Dictionary<Metaball, LavaCollider> _activeLavaColliders = new();

	private void InitializeColliders()
	{
		DestroyColliders();
		if ( !EnableCollision )
			return;

		foreach( var ball in Metaballs )
		{
			CreateBallCollider( ball );
		}
	}

	private void DestroyColliders()
	{
		// Just in case we changed containers or something...
		if ( LavaColliderContainer.IsValid() )
		{
			foreach( var child in LavaColliderContainer.Children )
			{
				child.Destroy();
			}
		}
		foreach( ( _, LavaCollider collider ) in _activeLavaColliders )
		{
			if ( collider.IsValid() )
			{
				collider.DestroyGameObject();
			}
		}
		_activeLavaColliders.Clear();
	}

	private void CreateBallCollider( Metaball ball )
	{
		if ( _activeLavaColliders.ContainsKey( ball ) )
			return;

		if ( !LavaColliderContainer.IsValid() )
		{
			LavaColliderContainer = new GameObject( GameObject, true, "Metaball Colliders" );
		}
		var ballGo = new GameObject( LavaColliderContainer, true, "Metaball Collider" )
		{
			LocalPosition = ball.Position
		};
		var collider = ballGo.AddComponent<SphereCollider>();
		collider.Radius = ball.Radius * 0.3f;
		var rigidbody = ballGo.AddComponent<Rigidbody>();
		var density = 1f;
		rigidbody.MassOverride = ball.Volume * density;
		rigidbody.Gravity = false;
		rigidbody.Locking = new PhysicsLock() { X = true, Pitch = true, Yaw = true, Roll = true };
		rigidbody.RigidbodyFlags = RigidbodyFlags.DisableCollisionSounds;
		_activeLavaColliders[ball] = new LavaCollider( ball, collider, rigidbody );
	}

	private void ApplyColliderPositions()
	{
		if ( !EnableCollision )
			return;

		foreach( ( Metaball ball, LavaCollider collider ) in _activeLavaColliders )
		{
			ball.Position = collider.Rigidbody.LocalPosition;
		}
	}

	private void KeepInBounds( Metaball metaball )
	{
		var size = SimulationSize * 0.5f;
		var bounds = LocalBounds * 1.1f;
		var isOutOfBounds = !bounds.Contains( metaball.Position );
		if ( !isOutOfBounds )
			return;

		metaball.Position = metaball.Position.Clamp( -size, size );
		if ( _activeLavaColliders.TryGetValue( metaball, out var collider ) )
		{
			if ( Metaball.WorldDebug )
			{
				var sphere = new Sphere( WorldTransform.PointToWorld( metaball.Position ), metaball.Radius * 2f );
				DebugOverlay.Sphere( sphere, color: Color.Red, duration: 1f, overlay: true );
			}
			collider.Rigidbody.LocalPosition = metaball.Position;
			collider.Rigidbody.Transform.ClearInterpolation();
		}
	}

	private void ApplyRigidbodyForces( Metaball ball )
	{
		if ( !_activeLavaColliders.ContainsKey( ball ) )
		{
			CreateBallCollider( ball );
		}
		var collider = _activeLavaColliders[ball];
		var rb = collider.Rigidbody;
		ball.Velocity = ball.Velocity.Clamp( -MaxVelocity, MaxVelocity );
		var targetPos = WorldTransform.PointToWorld( ball.Position + ball.Velocity );
		rb.SmoothMove( targetPos, 1f, Time.Delta );
	}
}
