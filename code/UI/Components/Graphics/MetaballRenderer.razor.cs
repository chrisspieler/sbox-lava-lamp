using Sandbox.UI;

public partial class MetaballRenderer : Panel
{
	[ConVar( "metaball_render_2d" )]
	public static bool Render2D { get; set; } = false;

	public LavaWorld World { get; set; }
	public float CutoffThreshold { get; set; }
	public float CutoffSharpness { get; set; }
	public float InnerBlend { get; set; }

	private Material _metaballMaterial = Metaball2D.Material;

	public override void Tick()
	{
		if ( Metaball2D.Debug )
		{
			_lastTextPosition = new Vector2( 30f, 50f );
			var screenSize = Screen.Size;
			PrintDebugText( $"Screen Size: {screenSize}" );
			var screenRect = new Rect( 0, 0, screenSize.x, screenSize.y );
			var worldStart = ScreenToPanelUV( screenRect.TopLeft );
			var worldEnd = ScreenToPanelUV( screenRect.BottomRight );
			PrintDebugText( $"World Size: {worldStart} to {worldEnd}" );
			var mousePos = ScreenToPanelUV( MousePosition );
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

	public Vector2 ScreenToPanelUV( Vector2 screenPos )
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

	public override void DrawBackground( ref RenderState state )
	{
		if ( !World.IsValid() || World.MetaballCount < 1 )
			return;

		if ( !Render2D )
			return;

		var metaballData = World.Metaballs
			.Select( mb => mb.GetRenderData() )
			.ToList();

		Graphics.Attributes.SetData( "BallBuffer", metaballData );
		Graphics.Attributes.Set( "BallCount", metaballData.Count );
		Graphics.Attributes.Set( "CutoffThreshold", CutoffThreshold );
		Graphics.Attributes.Set( "CutoffSharpness", CutoffSharpness );
		Graphics.Attributes.Set( "InnerBlend", InnerBlend );
		Graphics.DrawQuad( Box.Rect, _metaballMaterial, Color.White );
	}
}
