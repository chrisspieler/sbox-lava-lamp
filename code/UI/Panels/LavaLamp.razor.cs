using Sandbox.Services;
using Sandbox.Utility;
using System;

public partial class LavaLamp : PanelComponent
{
	private struct MetaballExtData 
	{
		public MetaballExtData()
		{

		}

		public float HueOffset { get; set; }
		public float SaturationOffset { get; set; }
		public float ValueOffset { get; set; }
		public float Temperature { get; set; }

		public readonly ColorHsv Apply( ColorHsv color )
		{
			return color
				.WithHue( color.Hue + HueOffset )
				.WithSaturation( color.Saturation + SaturationOffset )
				.WithValue( color.Value + ValueOffset );
		}
	}

	[Property] public LavaWorld World { get; set; }

	[Property] public bool Debug
	{
		get => Metaball2D.Debug;
		set
		{
			Metaball2D.Debug = value;
		}
	}

	[Property, Group( "Interactivity" )]
	public float AttractForce { get; set; } = 4f;
	[Property, Group( "Interactivity" ), InputAction]
	public string AttractAction { get; set; } = "attack1";
	[Property, Group( "Interactivity" ), InputAction]
	public string SpawnAction { get; set; } = "attack2";

	[Property, Group( "Color")] 
	public Color LavaColor { get; set; } = Color.Orange;

	[Property, Group( "Color" ), Range( 0f, 1f )] 
	public float HueVariance { get; set; } = 0.05f;

	[Property, Group( "Color" ), Range( 0f, 1f )] 
	public float SaturationVariance { get; set; } = 0.05f;

	[Property, Group( "Color" ), Range( 0f, 1f )] 
	public float ValueVariance { get; set; } = 0.05f;

	[Property, Group( "Color" )]
	public Color FastLavaColor { get; set; } = Color.Yellow;

	[Property, Group( "Color" )]
	public float LavaMaxSpeed { get; set; } = 1f;

	[Property, Group( "Color" )]
	public float LavaMinSpeed { get; set; } = 0f;

	[Property, Group( "Heat" )]
	public float MinHeatY { get; set; } = 0.6f;

	[Property, Group( "Heat" )]
	public float MaxHeatY { get; set; } = 1f;

	[Property, Group( "Heat" )]
	public float HeatPerSecond { get; set; } = 0.01f;

	[Property, Group( "Heat" )]
	public float MinTemperature { get; set; } = 0f;

	[Property, Group( "Heat" )]
	public float MaxTemperature { get; set; } = 5f;

	[Property, Group( "Heat" )]
	public float CoolingPerSecond { get; set; } = 1f;

	[Property, Group( "Heat" )]
	public Vector2 HeatDirection { get; set; } = Vector2.Down;

	[Property, Group( "Heat" )]
	public float ConvectionPower { get; set; } = 1f;

	[Property, Group( "Heat" )] 
	public Vector3 ConvectionNoiseScrolling { get; set; } = new Vector3( 2f, 3f );

	[Property, Group( "Heat" )] 
	public float ConvectionNoiseScale { get; set; } = 10f;


	[Property, Range( 0f, 1f ), Group( "Shader")] public float CutoffThreshold { get; set; } = 0.5f;
	[Property, Range( 0f, 1f ), Group( "Shader" )] public float CutoffSharpness { get; set; } = 0.5f;
	[Property, Range( 0f, 1f ), Group( "Shader" )] public float InnerBlend { get; set; } = 0.5f;

	public IEnumerable<Metaball2D> Metaballs => World?.Metaballs;

	private MetaballRenderer Renderer { get; set; }

	private readonly Dictionary<Metaball2D, MetaballExtData> _metaballData = new();

	private float _convectionNoiseSeed;

	protected override void OnTreeFirstBuilt()
	{
		World ??= GetComponent<LavaWorld>();

		_convectionNoiseSeed = Game.Random.Float( 0f, 5000f );
		UpdateColor();
		InitializeMetaballData();
	}

	protected override void OnUpdate()
	{
		if ( !World.IsValid() || !Renderer.IsValid() )
			return;

		UpdateInput();
		UpdateColor();
		UpdateAttributes();
		UpdateCursor();
		UpdateHeat();
	}

	private void UpdateColor()
	{
		if ( !World.IsValid() )
			return;

		foreach( var metaball in World.Metaballs )
		{
			UpdateColor( metaball );
		}
	}

	private void UpdateColor( Metaball2D metaball )
	{
		if ( metaball is null )
			return;

		if ( !_metaballData.TryGetValue( metaball, out MetaballExtData data ) )
			return;

		var baseColor = GetLavaBaseColor( metaball.Velocity );
		metaball.BallColor = data.Apply( baseColor );
		return;
	}

	private Color GetLavaBaseColor( Vector2 velocity )
	{
		var speed = velocity.Length.LerpInverse( LavaMinSpeed, LavaMaxSpeed );
		return Color.Lerp( LavaColor, FastLavaColor, speed );
	}

	private void InitializeMetaballData()
	{
		if ( !World.IsValid() )
			return;

		foreach( var metaball in World.Metaballs )
		{
			InitializeMetaballData( metaball );
		}
	}

	private void InitializeMetaballData( Metaball2D metaball )
	{
		var baseColor = GetLavaBaseColor( metaball.Velocity );
		var data = RandomizeHsv();
		metaball.BallColor = data.Apply( baseColor );
		data.Temperature = Game.Random.Float( MinTemperature, MaxTemperature );
		_metaballData[metaball] = data;
	}

	private void UpdateHeat()
	{
		if ( !World.IsValid() )
			return;

		foreach( var ball in World.Metaballs )
		{
			if ( !_metaballData.TryGetValue( ball, out MetaballExtData data ) )
				continue;

			var position = ball.Position;
			if ( position.y < MinHeatY || position.y > MaxHeatY )
			{
				data.Temperature -= CoolingPerSecond * Time.Delta;
				data.Temperature = data.Temperature.Clamp( 0f, MaxTemperature );
				_metaballData[ball] = data;
				continue;
			}
			else
			{
				var heatZone = position.y.LerpInverse( MinHeatY, MaxHeatY );
				heatZone = MathF.Max( 0.5f, heatZone );
				var heatAmount = 0f.LerpTo( HeatPerSecond, heatZone );
				heatAmount *= Time.Delta;
				data.Temperature += heatAmount;
				data.Temperature = data.Temperature.Clamp( 0f, MaxTemperature );
			}
			var noise = GetConvectionNoise( position );
			var heatDir = HeatDirection + noise;
			heatDir *= data.Temperature * ConvectionPower;
			heatDir *= Time.Delta;
			ball.Velocity += heatDir;
		}
	}

	private Vector2 GetConvectionNoise( Vector2 position )
	{
		var noiseScale = 1f / ConvectionNoiseScale;
		var scroll = ConvectionNoiseScrolling * Time.Now;
		var xNoise = Noise.Perlin( scroll.x * noiseScale, noiseScale, _convectionNoiseSeed );
		var yNoise = Noise.Perlin( scroll.y * noiseScale, noiseScale, _convectionNoiseSeed );
		var dir = new Vector2( xNoise, yNoise );
		dir = dir * 2 - 1;
		return dir * 0.2f;
	}

	public Metaball2D SpawnMetaball( Vector2 panelPos, Color color, float size = 0.15f )
	{
		if ( !World.IsValid() || !Renderer.IsValid() )
			return null;

		return World.AddMetaball( Renderer.ScreenToPanelUV( panelPos ), color, size );
	}

	private MetaballExtData RandomizeHsv()
	{
		var hue = (Game.Random.Float( -360f, 360f ) * HueVariance).Clamp( 0f, 360f );
		var saturation = (Game.Random.Float( -1f, 1f ) * SaturationVariance).Clamp( 0f, 1f );
		var value = (Game.Random.Float( -1f, 1f ) * ValueVariance).Clamp( 0f, 1f );
		return new MetaballExtData()
		{
			HueOffset = hue,
			SaturationOffset = saturation,
			ValueOffset = value
		};
	}

	private void UpdateAttributes()
	{
		if ( Renderer is null )
			return;

		Renderer.CutoffThreshold = 10f.LerpTo( 1000f, CutoffThreshold );
		Renderer.CutoffSharpness = 1f.LerpTo( 20f, CutoffSharpness );
		Renderer.InnerBlend = 1f.LerpTo( 4f, InnerBlend );
	}

	private void UpdateCursor()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		camera.Hud.DrawCircle( Mouse.Position, 12f, Color.White );
		camera.Hud.DrawCircle( Mouse.Position, 10f, Color.Black );
	}

	private void UpdateInput()
	{
		if ( !Renderer.IsValid() || !World.IsValid() )
			return;

		if ( Input.Down( AttractAction ) )
		{
			var mouseUv = Renderer.ScreenToPanelUV( Renderer.MousePosition );
			World.AttractToPoint( mouseUv, AttractForce );
		}
		if ( Input.Pressed( SpawnAction ) )
		{
			SpawnMetaball( Renderer.MousePosition, LavaColor );
		}
	}
}
