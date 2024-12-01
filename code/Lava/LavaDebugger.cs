public partial class LavaDebugger : Component
{
	private enum DebugMode
	{
		None,
		Sdf2D,
		Sdf3D
	}

	[Property] public LavaWorld World { get; set; }
	[Property] public LavaRenderer2D Renderer2D { get; set; }
	[Property] public LavaRendererRaymarching Renderer3D { get; set; }

	[Property] public bool ShowWorldInfo
	{
		get => Metaball.WorldDebug;
		set
		{
			Metaball.WorldDebug = value;
		}
	}

	[Property, Group( "Interactivity" )]
	public float AttractForce { get; set; } = 4f;
	[Property, Range( 0f, 10f ), Group( "Interactivity" )]
	public string AttractAction { get; set; } = "attack1";
	[Property, Group( "Interactivity" ), InputAction]
	public string SpawnAction { get; set; } = "attack2";

	private DebugMode Mode
	{
		get
		{
			// If the 2D render is up on screen, it has priority.
			if ( Renderer2D.IsValid() && Renderer2D.Active )
			{
				return DebugMode.Sdf2D;
			}
			else if ( Renderer3D.IsValid() && Renderer3D.Active )
			{
				return DebugMode.Sdf3D;
			}
			else
			{
				return DebugMode.None;
			}
		}
	}

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

		switch ( Mode )
		{
			case DebugMode.Sdf2D:
				UpdateInput2D();
				break;
			case DebugMode.Sdf3D:
				UpdateInput3D();
				break;
			default:
				break;
		}
	}

	private void UpdateInput2D()
	{
		var mousePos = Mouse.Position;
		if ( Input.Down( AttractAction ) )
		{
			var mousePoint = Renderer2D.ScreenToPoint( mousePos );
			World.AttractToPoint( mousePoint, AttractForce, minDistance: 0.25f );
		}
		if ( Input.Pressed( SpawnAction ) )
		{
			SpawnMetaball( Renderer2D.ScreenToPoint( mousePos ), World.LavaColor );
		}
	}

	private void UpdateInput3D()
	{
		var mousePos = Mouse.Position;
		if ( Input.Down( AttractAction ) )
		{
			var mousePoint = Renderer3D.ScreenToPoint( mousePos );
			World.AttractToPoint( mousePoint, AttractForce, minDistance: 0.25f );
		}
		if ( Input.Pressed( SpawnAction ) )
		{
			SpawnMetaball( Renderer3D.ScreenToPoint( mousePos ), World.LavaColor );
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

	public Metaball SpawnMetaball( Vector3 simPos, Color color, float size = 0.5f )
	{
		if ( !World.IsValid() )
			return null;

		return World.AddMetaball( simPos, color, size );
	}
}
