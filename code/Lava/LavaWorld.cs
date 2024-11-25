﻿public partial class LavaWorld : Component
{
	public IEnumerable<Metaball> Metaballs => _metaballs;
	private List<Metaball> _metaballs = new();
	public int MetaballCount => _metaballs.Count;

	public Vector2 ScreenToWorld( Vector2 screenPos )
	{
		var aspect = Screen.Width / Screen.Height;
		var worldPos = 2 * (screenPos / Screen.Size - 0.5f);
		worldPos.x *= aspect;
		return worldPos;
	}

	public Metaball AddMetaball( Vector2 position, Color color, float radius )
	{
		_metaballs ??= new List<Metaball>();
		if ( _metaballs.Count >= Metaball.MAX_BALLS )
		{
			Log.Info( $"Unable to add metaball to {GameObject.Name}: metaball limit reached." );
			return null;
		}

		var metaball = new Metaball( this, position, color, radius );
		_metaballs.Add( metaball );
		return metaball;
	}
}