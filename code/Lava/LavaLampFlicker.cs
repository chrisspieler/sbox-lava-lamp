using System;

public class LavaLampFlicker : Component
{
	private record Averages( Vector2 LavaPosition, Vector2 LavaVelocity, Color LavaColor );

	[Property] public LavaWorld World { get; set; }
	[Property] public PointLight LampLight { get; set; }

	[Property, Group("Position")] 
	public float PositionScale { get; set; } = 10f;

	[Property, Group( "Velocity" )]
	public float VelocityMaxInput { get; set; } = 20f;

	[Property, Group( "Velocity" )]
	public float LowVelocityAttenuation { get; set; } = 2f;

	[Property, Group( "Velocity" )]
	public float HighVelocityAttenuation { get; set; } = 0.25f;

	[Property, Group( "Color" ), Range( -1f, 1f )]
	public float LightSaturationOffset { get; set; } = 0f;

	[Property, Group( "Color" ), Range( -1f, 1f )]
	public float LightValueOffset { get; set; } = 0f;


	protected override void OnStart()
	{
		World ??= GetComponent<LavaWorld>();
	}

	protected override void OnUpdate()
	{
		if ( !World.IsValid() )
			return;

		var balls = World.Metaballs.ToList();
		var averages = GetAverages( balls );
		SetPosition( averages.LavaPosition );
		SetVelocity( averages.LavaVelocity );
		SetColor( averages.LavaColor );
	}

	private Averages GetAverages( List<Metaball> balls )
	{
		Vector3 totalPosition = Vector3.Zero;
		Vector3 totalVelocity = Vector3.Zero;
		Vector4 totalColor = Color.White;
		for ( int i = 0; i < balls.Count; i++ )
		{
			var ball = balls[i];
			var color = ball.CalculatedColor.Clamp01();
			
			if ( i == 0 )
			{
				totalPosition = ball.Position;
				totalVelocity = ball.Velocity;
				totalColor = color;
				continue;
			}
			totalPosition += ball.Position;
			totalVelocity += ball.Velocity;
			totalColor += color;
		}
		var averagePos = totalPosition / balls.Count;
		var averageVel = totalVelocity / balls.Count;
		Color averageCol = totalColor / balls.Count;
		return new Averages( averagePos, averageVel, averageCol );
	}

	private void SetPosition( Vector2 averagePosition )
	{
		var localPos = new Vector3( 0f, -averagePosition.x, -averagePosition.y );
		localPos *= PositionScale;
		LocalPosition = LocalPosition.ExpDecayTo( localPos, 8f );
	}

	private void SetVelocity( Vector2 averageVelocity )
	{
		if ( !LampLight.IsValid() )
			return;

		float minLength = 0f;
		float maxLength = VelocityMaxInput;
		float minAttenuation = LowVelocityAttenuation;
		float maxAttenuation = HighVelocityAttenuation;
		var targetAttenuation = averageVelocity.Length.Remap( minLength, maxLength, minAttenuation, maxAttenuation );
		LampLight.Attenuation = LampLight.Attenuation.ExpDecayTo( targetAttenuation, 8f );
	}

	private void SetColor( Color averageColor )
	{
		if ( !LampLight.IsValid() )
			return;

		var hsv = averageColor.ToHsv();
		LampLight.LightColor = hsv with 
		{ 
			Saturation = hsv.Saturation + LightSaturationOffset,
			Value = hsv.Value + LightValueOffset
		};
	}
}
