public partial class LavaWorld : Component
{
	[Property, FeatureEnabled( "Collision", Icon = "sports_tennis" )]
	public bool EnableCollision { get; set; } = true;

	[Property, Range( -10f, 10f ), Feature( "Collision" )]
	public float WallBounce { get; set; } = 2f;

	protected override void OnUpdate()
	{
		ApplyDamping();
		AttractToGravity();
		AttractToLava();
		ApplyVelocity();
	}



	private void AdvanceBall( Metaball ball, Vector3 wishTranslation, int depth )
	{
		if ( depth > 3 )
		{
			// Log.Info( $"Max collision depth reached! Position: {ball.Position}, Wish translation: {wishTranslation}");
			ball.Velocity = Vector2.Zero;
			return;
		}
		var from = ball.Position;
		var to = ball.Position + wishTranslation;
		var wishLine = new Line( from, to );
		if ( !IntersectWall( wishLine, ball.Radius, out Vector2 normal, out Vector2 hitPosition, out float distance ) )
		{
			ball.Position += wishTranslation;
			return;
		}

		ball.Velocity = Vector3.Reflect( ball.Velocity, normal );
		var bounce = ball.Velocity * WallBounce;
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

	private readonly Line _leftEdge = new( new Vector2( -1f, -1f ), new Vector2( -1f, 1f ) );
	private readonly Line _bottomEdge = new( new Vector2( -1f, 1f ), new Vector2( 1f, 1f ) );
	private readonly Line _rightEdge = new( new Vector2( 1f, 1f ), new Vector2( 1f, -1f ) );
	private readonly Line _topEdge = new( new Vector2( 1f, -1f ), new Vector2( -1f, -1f ) );

	private bool IntersectWall( Line self, float radius, out Vector2 hitNormal, out Vector2 hitPosition, out float distance )
	{
		if ( CollisionDetection.IntersectLine( self, _leftEdge, out hitNormal, out hitPosition, out distance ) )
			return true;

		if ( CollisionDetection.IntersectLine( self, _bottomEdge, out hitNormal, out hitPosition, out distance ) )
			return true;

		if ( CollisionDetection.IntersectLine( self, _rightEdge, out hitNormal, out hitPosition, out distance ) )
			return true;

		if ( CollisionDetection.IntersectLine( self, _topEdge, out hitNormal, out hitPosition, out distance ) )
			return true;

		return false;
	}
}
