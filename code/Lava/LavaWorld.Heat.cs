using Sandbox.Utility;

public partial class LavaWorld : Component
{
	[Property, FeatureEnabled( "Heat", Icon = "local_fire_department" )]
	public bool EnableHeat { get; set; } = true;

	[Property, Feature( "Heat" )]
	public Curve VerticalHeatingCurve { get; set; } = new Curve( new Curve.Frame( 0f, 1f ), new Curve.Frame( 1f, 0f ) );

	[Property, Feature( "Heat" )]
	public Curve VerticalCoolingCurve { get; set; } = new Curve( new Curve.Frame( 0f, 0f ), new Curve.Frame( 1f, 1f ) );


	[Property, Feature( "Heat" )]
	public float MaxTemperature { get; set; } = 6f;

	[Property, Feature( "Heat" )]
	public Vector3 HeatDirection { get; set; } = Vector3.Up;

	[Property, Feature( "Heat" )]
	public float ConvectionPower { get; set; } = 0.01f;

	[Property, Feature( "Heat" )]
	public Vector3 ConvectionNoiseScrolling { get; set; } = new Vector3( 10f, 7f );

	[Property, Feature( "Heat" )]
	public float ConvectionNoiseScale { get; set; } = 10f;

	private float _convectionNoiseSeed;

	private void ApplyHeat()
	{
		foreach ( var ball in Metaballs )
		{
			var position = ball.Position;
			var height = position.z.LerpInverse( -SimulationSize.z, SimulationSize.z );
			var heating = VerticalHeatingCurve.Evaluate( height );
			var cooling = VerticalCoolingCurve.Evaluate( height );
			ball.Temperature -= cooling * Time.Delta * ( 1f - ball.Radius );
			ball.Temperature += heating * Time.Delta * ( 1f - ball.Radius );
			ball.Temperature = ball.Temperature.Clamp( 0f, MaxTemperature );
			var heatDir = HeatDirection * ball.Temperature * Time.Delta;
			var noise = GetConvectionNoise( position );
			heatDir += noise * ConvectionPower * Time.Delta;
			ball.Velocity += heatDir;
		}
	}

	private Vector3 GetConvectionNoise( Vector3 position )
	{
		var noiseScale = 1f / ConvectionNoiseScale;
		var scroll = ConvectionNoiseScrolling * Time.Now;
		var xNoise = Noise.Perlin( scroll.x * noiseScale, noiseScale, _convectionNoiseSeed );
		var yNoise = Noise.Perlin( scroll.y * noiseScale, noiseScale, _convectionNoiseSeed );
		var dir = new Vector3( 0, -xNoise, yNoise );
		dir = dir * 2 - 1;
		return dir * 0.2f;
	}
}
