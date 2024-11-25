using Sandbox.Utility;
using System;

public partial class LavaWorld : Component
{
	[Property, Feature( "Color" )]
	public Color LavaColor { get; set; } = Color.Orange;

	[Property, Feature( "Color" ), ShowIf( nameof(EnableHeat), true )]
	public Color FastLavaColor { get; set; } = Color.Yellow;

	[Property, Feature( "Color" ), Range( 0f, 1f )]
	public float HueVariance { get; set; } = 0.02f;

	[Property, Feature( "Color" ), Range( 0f, 1f )]
	public float SaturationVariance { get; set; } = 0.05f;

	[Property, Feature( "Color" ), Range( 0f, 1f )]
	public float ValueVariance { get; set; } = 0.05f;

	private void UpdateColor()
	{
		foreach ( var metaball in Metaballs )
		{
			UpdateColor( metaball );
		}
	}

	private void UpdateColor( Metaball metaball )
	{
		if ( metaball is null )
			return;

		metaball.CalculatedColor = CalculateColor( metaball );
		return;
	}

	private Color CalculateColor( Metaball metaball )
	{
		var heat = 0f;
		if ( EnableHeat )
		{
			heat = metaball.Temperature.LerpInverse( 0, MaxTemperature );
			heat = Easing.SineEaseIn( heat );
		}
		return Color.Lerp( metaball.InitialColor, FastLavaColor, heat );
	}

	private ColorHsv RandomizeHsv( Color input )
	{
		var h = input.AdjustHue( (Game.Random.Float( -360f, 360f ) * HueVariance ).Clamp( 0f, 360f ) );
		var s = h.Saturate( (Game.Random.Float( -1f, 1f ) * SaturationVariance ).Clamp( 0f, 1f ) );
		return s.Lighten( (Game.Random.Float( -1f, 1f ) * ValueVariance ).Clamp( 0f, 1f ) );
	}
}
