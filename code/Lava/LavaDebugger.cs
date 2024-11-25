public class LavaDebugger : Component
{
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

	public IEnumerable<Metaball> Metaballs => World?.Metaballs;

	protected override void OnStart()
	{
		World ??= GetComponent<LavaWorld>();
	}

	protected override void OnUpdate()
	{
		if ( !World.IsValid() )
			return;

		UpdateInput();
		UpdateCursor();
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
			SpawnMetaball( mousePos, World.LavaColor );
		}
	}

	private void UpdateCursor()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		camera.Hud.DrawCircle( Mouse.Position, 12f, Color.White );
		camera.Hud.DrawCircle( Mouse.Position, 10f, Color.Black );
	}

	public Metaball SpawnMetaball( Vector2 screenPos, Color color, float size = 0.15f )
	{
		if ( !World.IsValid() )
			return null;

		return World.AddMetaball( LavaRenderer2D.ScreenToShaderCoords( screenPos ), color, size );
	}
}
