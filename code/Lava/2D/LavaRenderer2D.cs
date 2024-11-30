using System;

public partial class LavaRenderer2D : Component
{
	[ConVar( "metaball_render_2d" )]
	public static bool RenderToScreen { get; set; } = true;

	[Property] public LavaWorld World { get; set; }
	[Property] public CameraComponent TargetCamera { get; set; }
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
		TargetCamera ??= Scene.Camera;
	}

	private void Render( SceneObject sceneObject )
	{
		var cantFindBalls = !World.IsValid() || World.MetaballCount < 1;
		var cantRender = !RenderToScreen || !_sceneObject.IsValid() || !TargetCamera.IsValid();
		if ( cantFindBalls || cantRender )
			return;

		UpdateAttributes();

		var screenRect = TargetCamera.GetCenteredViewportRect( BoundsAspect );
		Graphics.Viewport = screenRect;
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
		Graphics.Attributes.Set( "SimulationSize", World.SimulationSize );
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
