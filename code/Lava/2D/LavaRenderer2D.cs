public partial class LavaRenderer2D : Component
{
	[ConVar( "metaball_render_2d" )]
	public static bool RenderToScreen { get; set; } = true;

	[Property] public LavaWorld World { get; set; }
	[Property, Range( 0f, 1f ), Group( "Attributes" )] public float CutoffThreshold { get; set; } = 0.1f;
	[Property, Range( 0f, 1f ), Group( "Attributes" )] public float CutoffSharpness { get; set; } = 0.5f;
	[Property, Range( 0f, 1f ), Group( "Attributes" )] public float InnerBlend { get; set; } = 1f;

	private readonly Material _metaballMaterial = Metaball.Material2D;
	private SceneCustomObject _sceneObject;

	protected override void OnStart()
	{
		_sceneObject = new( Scene.SceneWorld )
		{
			RenderOverride = Render
		};
	}

	protected override void OnPreRender()
	{
		if ( Metaball.Debug )
		{
			_lastTextPosition = new Vector2( 30f, 50f );
			var screenSize = Screen.Size;
			PrintDebugText( $"Screen Size: {screenSize}" );
			var screenRect = new Rect( 0, 0, screenSize.x, screenSize.y );
			var worldStart = ScreenToShaderCoords( screenRect.TopLeft );
			var worldEnd = ScreenToShaderCoords( screenRect.BottomRight );
			PrintDebugText( $"World Size: {worldStart} to {worldEnd}" );
			var mousePos = ScreenToShaderCoords( Mouse.Position );
			PrintDebugText( $"Mouse Position: {mousePos}" );
		}
	}

	private Vector2 _lastTextPosition;

	private void PrintDebugText( string text, Vector2? screenPixel = null, TextFlag flags = TextFlag.LeftTop )
	{
		var camera = Scene.Camera;
		var debugOverlay = DebugOverlaySystem.Current;
		if ( debugOverlay is null || !camera.IsValid() )
			return;

		screenPixel ??= _lastTextPosition;

		var ray = camera.ScreenPixelToRay( screenPixel.Value );
		var worldPos = ray.Project( 100f );
		var size = 4f;
		debugOverlay.Text( worldPos, text, size: size, flags: flags, overlay: true );
		_lastTextPosition.y += Screen.Height / 100f * 4f;
	}

	public static Vector2 ScreenToShaderCoords( Vector2 screenPos )
	{
		var aspect = Screen.Width / Screen.Height;
		// var uv = screenPos / Box.Rect.Size - 0.5f;
		var uv = screenPos / Screen.Size - 0.5f;
		uv = 2 * uv;
		// Need to offset the UV when the panel is in the center of the screen.
		// TODO: Handle any panel position.
		// uv = 2 * (uv - 0.5f);
		uv.x *= aspect;
		return uv;
	}

	private void Render( SceneObject sceneObject )
	{
		var cantFindBalls = !World.IsValid() || World.MetaballCount < 1;
		var cantRender = !RenderToScreen || !_sceneObject.IsValid();
		if ( cantFindBalls || cantRender )
			return;

		UpdateAttributes();

		var screenRect = new Rect( Vector2.Zero, Screen.Size );
		Graphics.DrawQuad( screenRect, _metaballMaterial, Color.White );
	}

	private void UpdateAttributes()
	{
		var metaballData = World.Metaballs
			.Select( mb => mb.GetRenderData() )
			.ToList();

		var cutoffThreshold = 10f.LerpTo( 1000f, CutoffThreshold );
		var cutoffSharpness = 1f.LerpTo( 20f, CutoffSharpness );
		var innerBlend = 1f.LerpTo( 4f, InnerBlend );

		Graphics.Attributes.SetData( "BallBuffer", metaballData );
		Graphics.Attributes.Set( "BallCount", metaballData.Count );
		Graphics.Attributes.Set( "CutoffThreshold", cutoffThreshold );
		Graphics.Attributes.Set( "CutoffSharpness", cutoffSharpness );
		Graphics.Attributes.Set( "InnerBlend", innerBlend );
	}

	protected override void OnEnabled()
	{
		if ( !_sceneObject.IsValid() )
			return;

		_sceneObject.RenderingEnabled = true;
	}

	protected override void OnDisabled()
	{
		if ( !_sceneObject.IsValid() )
			return;

		_sceneObject.RenderingEnabled = false;
	}

	
}
