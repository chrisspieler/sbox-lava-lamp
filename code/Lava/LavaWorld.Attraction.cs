public partial class LavaWorld : Component
{
	[Property, Group( "Attraction" )]
	public Vector3 GravityForce { get; set; } = Vector3.Down * 20f;

	[Property, Range( 0f, 50f ), Group( "Attraction" )]
	public float LavaAttractionForce { get; set; } = 20f;

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
		if ( LavaAttractionForce.AlmostEqual( 0f ) )
			return;

		foreach ( var ball in Metaballs )
		{
			AttractToPoint( ball.Position, LavaAttractionForce, minDistance: LavaAttractionMinRange );
		}
	}

	public void AttractToPoint( Vector3 attractPos, float force = 1f, float minDistance = 0.5f, float massDamping = 0f )
	{
		if ( attractPos.IsNaN || Metaballs is null )
			return;

		foreach ( var ball in Metaballs )
		{
			var sqrDistance = ball.Position.DistanceSquared( attractPos );
			if ( sqrDistance < minDistance )
				return;

			massDamping = massDamping.Clamp( 0f, 1f );

			var mass = ball.Mass;
			var intensity = force * Time.Delta;
			// More massive balls are affected less strongly.
			intensity *= ( 1f / mass ).LerpTo( 0f, massDamping );
			intensity *= 1f / sqrDistance;
			var direction = (attractPos - ball.Position).Normal;
			var targetVelocity = direction * intensity;
			ball.Velocity += targetVelocity * Time.Delta;
		}
	}
}
