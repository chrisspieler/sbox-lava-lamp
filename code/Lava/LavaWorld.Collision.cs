public partial class LavaWorld : Component
{
	[Property, FeatureEnabled( "Collision", Icon = "sports_tennis" )]
	public bool EnableCollision { get; set; } = true;

	[Property, Range( -10f, 10f ), Feature( "Collision" )]
	public float WallBounce { get; set; } = 2f;

	private void AdvanceBall( Metaball ball, Vector2 wishTranslation, int depth )
	{
		if ( depth > 3 )
		{
			// Log.Info( $"Max collision depth reached! Position: {ball.Position}, Wish translation: {wishTranslation}");
			ball.Velocity = Vector3.Zero;
			return;
		}
		// HACK: we're using old 2D coordinates for the physics until using real physics with a PhysicsWorld or something.
		var from = new Vector2( -ball.Position.y, -ball.Position.z );
		var to = from + wishTranslation;
		var wishLine = new Line( from, to );
		if ( !IntersectWall( wishLine, ball.Radius, out Vector2 normal, out Vector2 hitPosition, out float distance ) )
		{
			ball.Position += new Vector3( 0f, -wishTranslation.x, -wishTranslation.y );
			return;
		}

		// HACK: Continue using 2D coordinates
		var velocity2d = new Vector2( -ball.Velocity.y, -ball.Velocity.z );
		var reflection2d = Vector3.Reflect( velocity2d, normal );
		var reflection3d = new Vector3( 0f, -reflection2d.x, -reflection2d.y );
		var bounce = reflection3d * WallBounce;
		ball.Velocity += bounce;
		// Log.Info( $"Intersect wall! BallPos - {ball.Position}, WishLine - from:{wishLine.Start} to:{wishLine.End}, Wish Translation - {wishTranslation}, Normal - {normal}, Hit Position: {hitPosition}, Distance: {distance}" );
		var wishRay = new Ray( from, Vector3.Direction( from, to ) );
		var nextLength = wishTranslation.Length - distance;
		var nextDir = Vector3.Reflect( wishRay.Forward, normal );
		var nextFrom = wishRay.Project( distance );
		var nextWishTranslation = nextFrom + nextDir * nextLength;
		// Log.Info( $"Next dir: {nextDir}, next from: {nextFrom}, next wish translation: {nextWishTranslation}" );
		AdvanceBall( ball, nextWishTranslation, depth + 1 );
	}

	private void KeepInBounds( Metaball metaball )
	{
		var size = SimulationSize.WithX( 0f ) * 0.5f;
		metaball.Position = metaball.Position.Clamp( -size, size );
	}

	private Vector2 PhysicsBounds => new Vector2( -SimulationSize.y, -SimulationSize.z ) * 0.5f;
	private Line LeftEdge => new( new Vector2( -PhysicsBounds.x, -PhysicsBounds.y ), new Vector2( -PhysicsBounds.x, PhysicsBounds.y ) );
	private Line BottomEdge => new( new Vector2( -PhysicsBounds.x, PhysicsBounds.y ), new Vector2( PhysicsBounds.x, PhysicsBounds.y ) );
	private Line RightEdge => new( new Vector2( PhysicsBounds.x, PhysicsBounds.y ), new Vector2( PhysicsBounds.x, -PhysicsBounds.y ) );
	private Line TopEdge => new( new Vector2( PhysicsBounds.x, -PhysicsBounds.y ), new Vector2( -PhysicsBounds.x, -PhysicsBounds.y ) );

	private bool IntersectWall( Line self, float radius, out Vector2 hitNormal, out Vector2 hitPosition, out float distance )
	{
		if ( CollisionDetection.IntersectLine( self, LeftEdge, out hitNormal, out hitPosition, out distance ) )
			return true;

		if ( CollisionDetection.IntersectLine( self, BottomEdge, out hitNormal, out hitPosition, out distance ) )
			return true;

		if ( CollisionDetection.IntersectLine( self, RightEdge, out hitNormal, out hitPosition, out distance ) )
			return true;

		if ( CollisionDetection.IntersectLine( self, TopEdge, out hitNormal, out hitPosition, out distance ) )
			return true;

		return false;
	}
}
