using static MetaballRenderer;

public partial class LavaLamp : PanelComponent
{
	private struct MetaballColorExtData 
	{
		public float HueOffset { get; set; }
		public float SaturationOffset { get; set; }
		public float ValueOffset { get; set; }

		public readonly ColorHsv Apply( ColorHsv color )
		{
			return color
				.WithHue( color.Hue + HueOffset )
				.WithSaturation( color.Saturation + SaturationOffset )
				.WithValue( color.Value + ValueOffset );
		}
	}

	[Property] public LavaWorld World { get; set; }

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

	[Property, Range( 0f, 1f ), Group( "Shader")] public float CutoffThreshold { get; set; } = 0.5f;
	[Property, Range( 0f, 1f ), Group( "Shader" )] public float CutoffSharpness { get; set; } = 0.5f;
	[Property, Range( 0f, 1f ), Group( "Shader" )] public float InnerBlend { get; set; } = 0.5f;

	public IEnumerable<Metaball2D> Metaballs => World?.Metaballs;

	private MetaballRenderer Renderer { get; set; }

	private Dictionary<Metaball2D, MetaballColorExtData> _colorData = new();

	protected override void OnTreeFirstBuilt()
	{
		World ??= GetComponent<LavaWorld>();
		UpdateColor();
	}

	protected override void OnUpdate()
	{
		if ( !World.IsValid() || !Renderer.IsValid() )
			return;

		UpdateInput();
		UpdateColor();
		UpdateAttributes();
		UpdateCursor();
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

		var speed = metaball.Velocity.Length.LerpInverse( LavaMinSpeed, LavaMaxSpeed );
		var color = Color.Lerp( LavaColor, FastLavaColor, speed ).ToHsv();
		if ( _colorData.TryGetValue( metaball, out MetaballColorExtData colorData ) )
		{
			metaball.BallColor = colorData.Apply( color );
			return;
		}
		var variance = RandomizeHsv();
		metaball.BallColor = variance.Apply( color );
		_colorData[metaball] = variance;
	}

	public Metaball2D SpawnMetaball( Vector2 panelPos, Color color, float size = 0.15f )
	{
		if ( !World.IsValid() || !Renderer.IsValid() )
			return null;

		return World.AddMetaball( Renderer.ScreenToPanelUV( panelPos ), color, size );
	}

	private MetaballColorExtData RandomizeHsv()
	{
		var hue = (Game.Random.Float( -360f, 360f ) * HueVariance).Clamp( 0f, 360f );
		var saturation = (Game.Random.Float( -1f, 1f ) * SaturationVariance).Clamp( 0f, 1f );
		var value = (Game.Random.Float( -1f, 1f ) * ValueVariance).Clamp( 0f, 1f );
		return new MetaballColorExtData()
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

		if ( Input.Down( "attack1" ) )
		{
			var mouseUv = Renderer.ScreenToPanelUV( Renderer.MousePosition );
			World.AttractToPoint( mouseUv, 1f );
		}
		if ( Input.Pressed( "attack2" ) )
		{
			SpawnMetaball( Renderer.MousePosition, LavaColor );
		}
	}
}
