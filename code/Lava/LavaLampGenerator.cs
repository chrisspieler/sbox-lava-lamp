public class LavaLampGenerator : Component
{
	[Property] public LavaWorld World { get; set; }

	[Property, Range( 0f, Metaball.MAX_BALLS, 1f ), Group( "Generator" )]
	public int InitialCount { get; set; } = 48;

	[Property, Range( 0f, 1f ), Group( "Generator")] 
	public float MinRadius { get; set; } = 0.45f;
	[Property, Range( 0f, 1f ), Group( "Generator" )]
	public float MaxRadius { get; set; } = 0.8f;
	protected override void OnStart()
	{
		World ??= GetComponent<LavaWorld>();
		GenerateMetaballs( InitialCount );
	}

	public void GenerateMetaballs( int count )
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
			GenerateMetaball();
		}
	}

	private void GenerateMetaball()
	{
		if ( !World.IsValid() )
			return;

		var size = World.SimulationSize * 0.5f;
		var y = Game.Random.Float( size.y * -0.95f, size.y * 0.95f );
		var z = Game.Random.Float( size.z * -0.8f, size.z * -1f );
		var position = new Vector3( 0, y, z );
		var radius = Game.Random.Float( MinRadius, MaxRadius );
		World.AddMetaball( position, World.LavaColor, radius );
	}
}
