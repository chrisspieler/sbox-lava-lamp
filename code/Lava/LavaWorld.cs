public partial class LavaWorld : Component
{
	public IEnumerable<Metaball2D> Metaballs => _metaballs;
	private List<Metaball2D> _metaballs = new();
	public int MetaballCount => _metaballs.Count;

	public Metaball2D AddMetaball( Vector2 position, Color color, float radius )
	{
		_metaballs ??= new List<Metaball2D>();
		if ( _metaballs.Count >= Metaball2D.MAX_BALLS )
		{
			Log.Info( $"Unable to add metaball to {GameObject.Name}: metaball limit reached." );
			return null;
		}

		var metaball = new Metaball2D( this, position, color, radius );
		_metaballs.Add( metaball );
		return metaball;
	}
}
