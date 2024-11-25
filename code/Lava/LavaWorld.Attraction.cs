public partial class LavaWorld : Component
{
	[Property, Group( "Attraction" )]
	public Vector3 GravityForce { get; set; } = Vector3.Left;

	[Property, Range( 0f, 5f ), Group( "Attraction" )]
	public float LavaAttractionScale { get; set; } = 1f;

	[Property, Range( 0f, 1f ), Group( "Attraction" )]
	public float LavaAttractionRange { get; set; } = 0.05f;

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
		if ( LavaAttractionScale == 0f )
			return;

		foreach ( var ball in Metaballs )
		{
			AttractToPoint( ball.Position, LavaAttractionScale, minDistance: LavaAttractionRange );
		}
	}

	public void AttractToPoint( Vector3 attractPos, float force = 1f, float minDistance = 0.002f, float worldScale = 0.05f )
	{
		if ( attractPos.IsNaN || Metaballs is null )
			return;

		foreach ( var ball in Metaballs )
		{
			var sqrDistance = ball.Position.DistanceSquared( attractPos );
			if ( sqrDistance < minDistance )
				return;

			var mass = ball.Radius * 20f;
			sqrDistance *= worldScale;
			var intensity = 1f / sqrDistance;
			// More massive balls are affected less strongly.
			intensity *= (1f / mass);
			intensity *= force;
			var direction = (attractPos - ball.Position).Normal;
			var targetVelocity = direction * intensity;
			targetVelocity = targetVelocity.Clamp( -MaxVelocity, MaxVelocity );
			ball.Velocity = ball.Velocity.LerpTo( targetVelocity, Time.Delta * 0.01f );
		}
	}
}
