using Sandbox.Utility;
using System;

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
	public float ConvectionNoiseScale 
	{
		get => _convectionNoiseScale * 1000f;
		set
		{
			_convectionNoiseScale = value * 0.001f;
			_noise = Noise.PerlinField( new Noise.Parameters( _convectionNoiseSeed, value ) );
		}
	}
	private float _convectionNoiseScale = 0.001f;

	private int _convectionNoiseSeed;
	private INoiseField _noise;

	private void ApplyHeat()
	{
		foreach ( var ball in Metaballs )
		{
			var heating = GetHeating( ball.Position );
			var cooling = GetCooling( ball.Position );
			var heatChangeAmount = Time.Delta * (1f / ball.Volume);
			ball.Temperature -= cooling * heatChangeAmount;
			ball.Temperature += heating * heatChangeAmount;
			ball.Temperature = ball.Temperature.Clamp( 0f, MaxTemperature );
			var heatForce = HeatDirection.Normal * ball.Temperature * Time.Delta;
			heatForce += GetConvectionDirection( ball.Position ) * GetConvectionForce( ball.Position );
			ball.Velocity += heatForce;
		}
	}

	public float GetHeating( Vector3 position )
	{
		var size = SimulationSize * 0.5f;
		var height = position.z.LerpInverse( -size.z, size.z );
		return VerticalHeatingCurve.Evaluate( height );
	}

	public float GetCooling( Vector3 position )
	{
		var size = SimulationSize * 0.5f;
		var height = position.z.LerpInverse( -size.z, size.z );
		return VerticalCoolingCurve.Evaluate( height );
	}

	public Vector3 GetConvection( Vector3 position )
	{
		return GetConvectionDirection( position ) * GetConvectionForce( position ) * 0.0001f;
	}

	private Vector2 CurlNoise( Vector2 p, float derivativeSample = 0.001f )
	{
		var x1 = _noise.Sample( p.x + derivativeSample, p.y );
		var x2 = _noise.Sample( p.x - derivativeSample, p.y );
		var y1 = _noise.Sample( p.x, p.y + derivativeSample );
		var y2 = _noise.Sample( p.x, p.y - derivativeSample );
		var xD = x2 - x1;
		var yD = y2 - y1;
		var angle = MathF.Atan2( yD, xD );
		return new Vector2( MathF.Cos( angle ), MathF.Sin( angle ) );
	}

	public Vector3 GetConvectionDirection( Vector3 position )
	{
		_noise ??= Noise.PerlinField( new( _convectionNoiseSeed, ConvectionNoiseScale ) );

		var scroll = position + ConvectionNoiseScrolling * Time.Now;
		var noise = CurlNoise( new Vector2( -scroll.y, -scroll.z ) );
		var dir = new Vector3( 0f, -noise.x, -noise.y );

		if ( dir.IsNaN )
			return Vector3.Zero;

		return dir.Normal;
	}

	public float GetConvectionForce( Vector3 position )
	{
		return ConvectionPower * GravityForce.Length * 0.01f;
	}
}
