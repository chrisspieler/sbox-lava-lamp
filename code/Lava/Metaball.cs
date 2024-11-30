using Sandbox.Utility;

public class Metaball
{
	[ConVar( "metaball_vis" )]
	public static int MetaballVisMode { get; set; } = 0;

	[ConVar( "metaball_world_debug" )]
	public static bool WorldDebug { get; set; } = false;

	public Metaball( LavaWorld world ) 
	{
		World = world;
	}

	public Metaball( LavaWorld world, Vector3 position, Color color, float radius )
	{
		World = world;
		Position = position;
		InitialColor = color;
		Radius = radius;
	}

	public LavaWorld World { get; init; }
	public Vector3 Position { get; set; }
	public Color InitialColor { get; set; }
	public Color CalculatedColor { get; set; }
	public float Radius { get; set; }
	public Vector3 Velocity { get; set; }
	public float Temperature { get; set; }

	public const int MAX_BALLS = 256;
	public static Material Material2D => Material.FromShader( "shaders/2d_metaball.shader" );

	internal RenderData GetRenderData()
	{
		var color = MetaballVisMode switch
		{
			1 => GetVelocityVisColor(),
			2 => GetHeatVisColor(),
			_ => CalculatedColor
		};
		return new RenderData( Position, color, Radius);
	}

	private Color GetVelocityVisColor()
	{
		var t = Velocity.Length.LerpInverse( 0f, 1f );
		t = Easing.EaseIn( t );
		var r = t * 0.4f;
		// Perhaps green will be used later for something?
		var g = 0f;
		var b = (1f - t) * 0.7f;
		return new Color( r, g, b, 0.5f );
	}

	private Color GetHeatVisColor()
	{
		var t = Temperature.LerpInverse( 0f, World.MaxTemperature );
		t = Easing.EaseIn( t );
		var coldColor = Color.Blue;
		var hotColor = Color.Red;
		return Color.Lerp( coldColor, hotColor, t );
	}

	internal readonly struct RenderData
	{
		public RenderData() { }
		public RenderData( Vector3 position, Color color, float radius )
		{
			Position = new Vector4()
			{
				x = position.x,
				y = position.y,
				z = position.z,
				w = radius
			};
			Color = color;
		}

		public Vector4 Position { get; init; }
		public Vector4 Color { get; init; }
		public float Radius => Position.w;

		public readonly RenderData WithColor( Color color )
			=> this with { Color = color };

		public readonly RenderData WithPosition( Vector3 position )
			=> this with { Position = Position with { x = position.x, y = position.y, z = position.z } };

		public readonly RenderData WithRadius( float radius )
			=> this with { Position = Position with { w = radius } };
	}
}
