public partial class LavaWorld : Component
{
	[Property, Group( "Attraction" )]
	public Vector3 GravityForce { get; set; } = Vector3.Down * 20f;

	[Property, Range( 0f, 500f ), Group( "Attraction" )]
	public float LavaAttractionForce { get; set; } = 100f;

	[Property, Range( 0f, 5f ), Group( "Attraction" )]
	public float LavaAttractionMinRange { get; set; } = 0.5f;

	private void AttractToGravity()
	{
		if ( GravityForce.Length == 0f )
			return;

		foreach ( var ball in Metaballs )
		{
			ball.Velocity += GravityForce * Time.Delta;
		}
	}

	private void AttractToLava()
	{
		if ( LavaAttractionForce == 0f )
			return;

		foreach ( var ball in Metaballs )
		{
			AttractToPoint( ball.Position, LavaAttractionForce, minDistance: LavaAttractionMinRange );
		}
	}

	public void AttractToPoint( Vector3 attractPos, float force = 1f, float minDistance = 0.25f )
	{
		if ( attractPos.IsNaN || Metaballs is null )
			return;

		foreach ( var ball in Metaballs )
		{
			var sqrDistance = ball.Position.DistanceSquared( attractPos );
			if ( sqrDistance < minDistance )
				return;

			var density = 1f;
			var mass = ball.Volume * density;
			// More massive balls are affected less strongly.
			var intensity = ( 1f / mass );
			intensity *= force;
			intensity *= 1f / sqrDistance;
			var direction = (attractPos - ball.Position).Normal;
			var targetVelocity = direction * intensity;
			targetVelocity = targetVelocity.Clamp( -MaxVelocity, MaxVelocity );
			ball.Velocity = ball.Velocity.LerpTo( targetVelocity, Time.Delta * 0.01f );
		}
	}
}
