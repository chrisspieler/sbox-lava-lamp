public partial class LavaWorld : Component
{
	[Property, Group( "Velocity")]
	public Vector3 MaxVelocity { get; set; } = new Vector3( 0f, 100f, 100f );
	[Property, Range( 0f, 2f ), Group( "Velocity" )]
	public float DampingStrength { get; set; } = 1f;

	private void ApplyDamping()
	{
		if ( DampingStrength == 0f )
			return;

		foreach ( var ball in Metaballs )
		{
			ball.Velocity = ball.Velocity.LerpTo( Vector2.Zero, DampingStrength * 0.1f * ball.Radius );
		}
	}

	private void ApplyVelocity()
	{
		foreach ( var ball in Metaballs )
		{
			var velocity = ball.Velocity * Time.Delta;
			if ( velocity.Length < 0.0001f )
				continue;

			KeepInBounds( ball );

			// Log.Info( $"ball velocity: {velocity}" );
			if ( EnableCollision )
			{
				AdvanceBall( ball, velocity );
			}
			else
			{
				ball.Position += velocity;
			}
		}
	}
}
