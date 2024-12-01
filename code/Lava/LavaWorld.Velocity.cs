public partial class LavaWorld : Component
{
	[Property, Group( "Velocity")]
	public Vector3 MaxVelocity { get; set; } = new Vector3( 0f, 16f, 16f );
	[Property, Range( 0f, 5f ), Group( "Velocity" )]
	public float DampingStrength { get; set; } = 1f;

	private void ApplyDamping()
	{
		if ( DampingStrength == 0f )
			return;

		foreach ( var ball in Metaballs )
		{
			ball.Velocity += -ball.Velocity * DampingStrength * Time.Delta;
		}
	}

	private void UpdateVelocity()
	{
		if ( EnableCollision )
			return;

		foreach( var ball in Metaballs )
		{
			var velocity = ball.Velocity * Time.Delta;
			if ( velocity.Length < 0.0001f )
				continue;

			ball.Position += velocity;
			KeepInBounds( ball );
		}
	}

	private void FixedUpdateVelocity()
	{
		if ( !EnableCollision )
			return;

		foreach( var ball in Metaballs )
		{
			ApplyRigidbodyForces( ball );
			KeepInBounds( ball );
		}
	}
}
