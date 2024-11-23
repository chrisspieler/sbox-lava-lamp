using Sandbox.UI;

public partial class MetaballRenderer : Panel
{
	public LavaWorld World { get; set; }
	public float CutoffThreshold { get; set; }
	public float CutoffSharpness { get; set; }
	public float InnerBlend { get; set; }

	private Material _metaballMaterial = Metaball2D.Material;

	public Vector2 ScreenToPanelUV( Vector2 screenPos )
	{
		var aspect = Screen.Width / Screen.Height;
		var uv = screenPos / Box.Rect.Size - 0.5f;
		uv = 2 * uv;
		// Need to offset the UV when the panel is in the center of the screen. Fix this?
		// uv = 2 * (uv - 0.5f);
		uv.x *= aspect;
		return uv;
	}

	public override void DrawBackground( ref RenderState state )
	{
		if ( !World.IsValid() || World.MetaballCount < 1 )
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
