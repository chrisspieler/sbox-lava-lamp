public class LavaLampGenerator : Component
{
	[Property] public LavaWorld World { get; set; }

	[Property, Range( 0f, Metaball2D.MAX_BALLS, 1f ), Group( "Generator" )]
	public int MetaballCount { get; set; } = 96;

	protected override void OnStart()
	{
		World ??= GetComponent<LavaWorld>();
		AddRandomMetaballs( MetaballCount );
	}
	private void AddRandomMetaballs( int count )
	{
		if ( count <= 0 || !World.IsValid() )
			return;

		if ( World.MetaballCount + count > Metaball2D.MAX_BALLS )
		{
			Log.Info( $"Adding {count} balls would exceed limit of {Metaball2D.MAX_BALLS}" );
			return;
		}

		for ( int i = 0; i < count; i++ )
		{
			AddRandomMetaball();
		}
	}
	public void AddRandomMetaball()
	{
		if ( !World.IsValid() )
			return;

		var position = Vector2.Random * 0.9f;
		var hsv = Color.Red.ToHsv();
		var color = hsv.WithHue( hsv.Hue + Game.Random.Float( 0f, 360f ) );
		color = color.WithAlpha( Game.Random.Float( 0.2f, 0.9f ) );
		var radius = Game.Random.Float( 0.05f, 0.15f );
		World.AddMetaball( position, color, radius );
	}
}
