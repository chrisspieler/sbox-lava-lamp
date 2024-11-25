using Sandbox.Utility;

public class Metaball
{
	[ConVar( "metaball_debug" )]
	public static bool Debug { get; set; }

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
		var color = CalculatedColor;
		if ( Debug )
		{
			var t = Velocity.Length.LerpInverse( 0f, 0.5f );
			t = Easing.EaseIn( t );
			color = Color.Lerp( Color.Blue * 0.7f, Color.Red * 0.4f, t );
		}
		return new RenderData( Position, color, Radius );
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
