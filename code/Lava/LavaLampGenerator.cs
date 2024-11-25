public class LavaLampGenerator : Component
{
	[Property] public LavaWorld World { get; set; }

	[Property, Range( 0f, Metaball.MAX_BALLS, 1f ), Group( "Generator" )]
	public int MetaballCount { get; set; } = 96;

	[Property, Range( 0f, 1f ), Group( "Generator")] 
	public float MinRadius { get; set; } = 0.05f;
	[Property, Range( 0f, 1f ), Group( "Generator" )]
	public float MaxRadius { get; set; } = 0.15f;
	protected override void OnStart()
	{
		World ??= GetComponent<LavaWorld>();
		InitializeMetaballs( MetaballCount );
	}
	private void InitializeMetaballs( int count )
	{
		if ( count <= 0 || !World.IsValid() )
			return;

		if ( World.MetaballCount + count > Metaball.MAX_BALLS )
		{
			Log.Info( $"Adding {count} balls would exceed limit of {Metaball.MAX_BALLS}" );
			return;
		}

		for ( int i = 0; i < count; i++ )
		{
			InitializeMetaball();
		}
	}

	public void InitializeMetaball()
	{
		if ( !World.IsValid() )
			return;

		var x = Game.Random.Float( -0.95f, 0.95f );
		var y = Game.Random.Float( 0.8f, 1f );
		var position = new Vector2( x, y );
		var radius = Game.Random.Float( MinRadius, MaxRadius );
		World.AddMetaball( position, World.LavaColor, radius );
	}
}
