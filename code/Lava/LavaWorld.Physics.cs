public partial class LavaWorld : Component
{
	[Property, Range( 0f, 1f ), Group( "Physics" )]
	public float WallRestitution { get; set; } = 0.1f;
	[Property, Range( 0f, 5f ), Group( "Physics" )] 
	public float GravityAttractionScale { get; set; } = 1f;

	[Property, Group( "Physics" )] 
	public Vector2 GravityDirection = Vector2.Up;

	[Property, Range( 0f, 5f ), Group( "Physics" )] 
	public float LavaAttractionScale { get; set; } = 1f;

	[Property, Range( 0f, 1f ), Group( "Physics" )] 
	public float LavaAttractionRange { get; set; } = 0.05f;

	protected override void OnUpdate()
	{
		ApplyDamping();
		AttractToGravity();
		AttractToLava();
		ApplyVelocity();
	}

	private void ApplyDamping()
	{
		foreach ( var ball in Metaballs )
		{
			// Apply damping.
			ball.Velocity = ball.Velocity.LerpTo( Vector2.Zero, Time.Delta * ball.Radius );
		}
	}

	private void ApplyVelocity()
	{
		foreach( var ball in Metaballs )
		{
			var velocity = ball.Velocity * Time.Delta;
			if ( velocity.Length < 0.0001f )
				continue;

			// Log.Info( $"ball velocity: {velocity}" );
			AdvanceBall( ball, velocity, 0 );
		}
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
		ball.Velocity *= 1f - WallRestitution;
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

	private void AttractToGravity()
	{
		foreach( var ball in Metaballs )
		{
			ball.Velocity += (Vector3)GravityDirection * GravityAttractionScale * 0.25f * Time.Delta;
		}
	}

	private void AttractToLava()
	{
		if ( LavaAttractionScale == 0f )
			return;

		foreach( var ball in Metaballs )
		{
			AttractToPoint( ball.Position, LavaAttractionScale, minDistance: LavaAttractionRange );
		}
	}

	public void AttractToPoint( Vector2 attractPos, float force = 1f, float minDistance = 0.002f, float worldScale = 0.05f )
	{
		if ( ((Vector3)attractPos).IsNaN || Metaballs is null )
			return;

		foreach ( var ball in Metaballs )
		{
			var currentPos = new Vector2( ball.Position.x, ball.Position.y );
			var sqrDistance = currentPos.DistanceSquared( attractPos );
			if ( sqrDistance < minDistance )
				return;

			var mass = ball.Radius * 20f;
			sqrDistance *= worldScale;
			var intensity = 1f / sqrDistance;
			// More massive balls are affected less strongly.
			intensity *= (1f / mass);
			intensity *= force;
			var direction = (attractPos - currentPos).Normal;
			var targetVelocity = direction * intensity;
			targetVelocity = targetVelocity.Clamp( -25f, 25f );
			ball.Velocity = ball.Velocity.LerpTo( targetVelocity, Time.Delta * 0.01f );
		}
	}
}
