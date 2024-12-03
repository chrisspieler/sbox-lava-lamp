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
	[Property] public LavaLampGenerator Generator { get; set; }
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
	[Property, Range( 0f, 10f ), Group( "Interactivity" ), InputAction]
	public string AttractAction { get; set; } = "attack1";
	[Property, Group( "Interactivity" ), InputAction]
	public string SpawnAction { get; set; } = "attack2";
	public float SpawnRampUpTime { get; set; } = 1f;
	[Property, Group( "Interactivity" ), InputAction]
	public string ResetAction { get; set; } = "reload";
	[Property, Group( "Interactivity" ), InputAction]
	public string HelpAction { get; set; } = "help";

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
		if ( Input.Pressed( ResetAction ) )
		{
			ResetLavaWorld();
		}
		if ( Input.Pressed( HelpAction ) )
		{
			_shouldShowHelp = !_shouldShowHelp;
		}
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

	public void ResetLavaWorld()
	{
		if ( !World.IsValid() || !Generator.IsValid() )
			return;

		World.ClearWorld();
		Generator.GenerateMetaballs( Generator.InitialCount );
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

	private bool _shouldShowHelp = true;

	private void PaintHelpText( HudPainter hud )
	{
		if ( !_shouldShowHelp )
			return;

		var position = new Vector2( Screen.Size.x * 0.8f, Screen.Size.y * 0.05f );
		hud.DrawText( "HELP", position, Color.Yellow );
		position.y += Screen.Size.y * 0.025f;
		hud.DrawText( "-------------", position );
		position.y += Screen.Size.y * 0.025f;
		hud.DrawText( "Hold LMB to ATTRACT LAVA", position );
		position.y += Screen.Size.y * 0.025f;
		hud.DrawText( "Hold and release RMB to SPAWN LAVA", position );
		position.y += Screen.Size.y * 0.025f;
		hud.DrawText( "Press R key to RESET ALL LAVA", position, Color.Red );
		position.y += Screen.Size.y * 0.025f;
		hud.DrawText( "Press H key to hide all help text.", position, Color.Gray );
		position = new Vector2( Screen.Size.x / 2f, Screen.Size.y * 0.95f );
		hud.DrawText( "TIP: If you wait a while, the lava should begin rising on its own.", position, Color.White );
	}

	public Metaball SpawnMetaball( Vector3 simPos, Color color, float size = 0.5f )
	{
		if ( !World.IsValid() )
			return null;

		return World.AddMetaball( simPos, color, size );
	}
}
