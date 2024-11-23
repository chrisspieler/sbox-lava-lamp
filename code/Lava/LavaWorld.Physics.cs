public partial class LavaWorld : Component
{
	[Property, Range( 0f, 5f )] public float LavaAttractionScale { get; set; } = 1f;
	[Property, Range( 0f, 1f )] public float LavaAttractionRange { get; set; } = 0.05f;

	protected override void OnUpdate()
	{
		AttractToLava();
		ApplyVelocity();
	}

	private void ApplyVelocity()
	{
		foreach ( var ball in Metaballs )
		{
			// Apply damping.
			ball.Velocity = ball.Velocity.LerpTo( Vector2.Zero, Time.Delta * ball.Radius );
			ball.Position = ball.Position + ball.Velocity * Time.Delta;
		}
	}

	private void AttractToLava()
	{

		foreach( var ball in Metaballs )
		{
			AttractToPoint( ball.Position, 1f, minDistance: LavaAttractionRange );
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
