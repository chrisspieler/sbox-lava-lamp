using Sandbox.Utility;
using System;

public class LavaLamp : Component
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
		get => Metaball.Debug;
		set
		{
			Metaball.Debug = value;
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

	public IEnumerable<Metaball> Metaballs => World?.Metaballs;

	private readonly Dictionary<Metaball, MetaballExtData> _metaballData = new();

	private float _convectionNoiseSeed;

	protected override void OnStart()
	{
		World ??= GetComponent<LavaWorld>();

		_convectionNoiseSeed = Game.Random.Float( 0f, 5000f );
		UpdateColor();
		InitializeMetaballData();
	}

	protected override void OnUpdate()
	{
		if ( !World.IsValid() )
			return;

		UpdateInput();
		UpdateColor();
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

	private void UpdateColor( Metaball metaball )
	{
		if ( metaball is null )
			return;

		if ( !_metaballData.TryGetValue( metaball, out MetaballExtData data ) )
		{
			InitializeMetaballData( metaball );
			return;
		}

		var baseColor = GetLavaBaseColor( metaball.Velocity );
		metaball.BallColor = data.Apply( baseColor );
		return;
	}

	private Color GetLavaBaseColor( Vector3 velocity )
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

	private void InitializeMetaballData( Metaball metaball )
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
			ball.Velocity += (Vector3)heatDir;
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

	public Metaball SpawnMetaball( Vector2 screenPos, Color color, float size = 0.15f )
	{
		if ( !World.IsValid() )
			return null;

		return World.AddMetaball( LavaRenderer2D.ScreenToShaderCoords( screenPos ), color, size );
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
		if ( !World.IsValid() )
			return;

		var mousePos = Mouse.Position;
		if ( Input.Down( AttractAction ) )
		{
			var mouseUv = LavaRenderer2D.ScreenToShaderCoords( mousePos );
			World.AttractToPoint( mouseUv, AttractForce );
		}
		if ( Input.Pressed( SpawnAction ) )
		{
			SpawnMetaball( mousePos, LavaColor );
		}
	}
}
