using Sandbox.Rendering;

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
	public float AttractForce { get; set; } = 10000f;
	[Property, Group( "Interactivity" )]
	public float AttractRampUpTime { get; set; } = 1f;
	[Property, Range( 0f, 10f ), Group( "Interactivity" )]
	public string AttractAction { get; set; } = "attack1";
	[Property, Group( "Interactivity" ), InputAction]
	public string SpawnAction { get; set; } = "attack2";
	public float SpawnRampUpTime { get; set; } = 1f;

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
		UpdateUI();
	}

	private void UpdateInput()
	{
		if ( !World.IsValid() )
			return;

		var mousePoint = GetMousePoint();
		UpdateAttract( mousePoint );
		UpdateSpawn( mousePoint );
	}

	private TimeSince _sinceFirstHeldAttract;

	private void UpdateAttract( Vector3 mousePoint)
	{
		if ( Input.Pressed( AttractAction ) )
			_sinceFirstHeldAttract = 0;

		if ( Input.Down( AttractAction ) )
		{
			var power = MathX.Remap( _sinceFirstHeldAttract, 0f, AttractRampUpTime, AttractForce * 0.3f, AttractForce );
			World.AttractToPoint( mousePoint, power, minDistance: 2f, massDamping: 0f );
		}
	}

	private TimeSince _sinceFirstHeldSpawn;

	private void UpdateSpawn( Vector3 mousePoint )
	{
		if ( Input.Pressed( SpawnAction ) )
			_sinceFirstHeldSpawn = 0;


		if ( Input.Released( SpawnAction ) )
		{
			var size = MathX.Remap( _sinceFirstHeldSpawn, 0f, SpawnRampUpTime, 0.3f, 1.2f );
			SpawnMetaball( mousePoint, World.LavaColor, size );
		}
	}

	private Vector3 GetMousePoint()
	{
		return Mode switch
		{
			DebugMode.Sdf2D => Renderer2D.ScreenToPoint( Mouse.Position ),
			DebugMode.Sdf3D => Renderer3D.ScreenToPoint( Mouse.Position ),
			_ => default
		};
	}

	private void UpdateUI()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		var hud = camera.Hud;
		PaintCursor( hud );
		PaintHelpText( hud );
	}

	private void PaintCursor( HudPainter hud )
	{
		hud.DrawCircle( Mouse.Position, 12f, Color.White );
		hud.DrawCircle( Mouse.Position, 10f, Color.Black );
	}

	private void PaintHelpText( HudPainter hud )
	{
		var position = new Vector2( Screen.Size.x * 0.8f, Screen.Size.y * 0.05f );
		hud.DrawText( "HELP", position );
		position.y += Screen.Size.y * 0.025f;
		hud.DrawText( "-------------", position );
		position.y += Screen.Size.y * 0.025f;
		hud.DrawText( "Hold LMB to ATTRACT LAVA", position );
		position.y += Screen.Size.y * 0.025f;
		hud.DrawText( "Hold and release RMB to SPAWN LAVA", position );
	}


	public Metaball SpawnMetaball( Vector3 simPos, Color color, float size = 0.5f )
	{
		if ( !World.IsValid() )
			return null;

		return World.AddMetaball( simPos, color, size );
	}
}
