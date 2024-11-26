using Sandbox.Rendering;
using System;

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

	public Vector2 PointToScreenScale => (1f / GetScreenAspect()) * World.GetSimulationAspect() * Screen.Size;

	public Rect GetCenteredScreenRect()
	{
		var simAspect = World.GetSimulationAspect();
		var screenAspect = GetScreenAspect();
		var rectSize = Screen.Size * (1f / screenAspect) * simAspect;
		var xMargin = (Screen.Size.x - rectSize.x) * 0.5f;
		var yMargin = (Screen.Size.y - rectSize.y) * 0.5f;
		var offset = new Vector2( xMargin, yMargin );
		return new Rect( offset, rectSize );
	}

	private Vector2 GetScreenAspect()
	{
		var yScale = 1f;
		var xScale = 1f;
		if ( Screen.Height > Screen.Width )
		{
			yScale = Screen.Width / Screen.Height;
		}
		else
		{
			xScale = Screen.Width / Screen.Height;
		}
		return new Vector2( xScale, yScale );
	}

	public Vector2 ScreenToUV( Vector2 screenPos )
	{
		var screenNormal = screenPos / Screen.Size;
		var screenScales = GetScreenAspect();
		// Scale around center of screen by aspect ratio.
		var uv = screenNormal - 0.5f;
		uv *= screenScales;
		uv += 0.5f;
		return uv;
	}

	public Vector2 PointToScreen( Vector3 simPosition )
	{
		var uv = World.PointToUV( simPosition );
		var screenAspect = 1f / GetScreenAspect();
		var simAspect = World.GetSimulationAspect();
		// Scale around center of screen by inverse of aspect ratio.
		uv -= 0.5f;
		uv *= screenAspect * simAspect;
		uv += 0.5f;
		return uv * Screen.Size;
	}

	public Vector3 ScreenToPoint( Vector2 screenPos )
	{
		// For now, the on-screen representation of a simulation is centered in middle of the screen,
		// and stretched uniformly so that its largest axis snugly fits the sides of the screen.

		var screenNormal = screenPos / Screen.Size;
		var screenScales = GetScreenAspect();
		// Scale around center of screen by aspect ratio.
		screenNormal -= 0.5f;
		screenNormal *= screenScales;
		screenNormal += 0.5f;
		return World.UVToPoint( screenNormal );
	}

	protected override void OnPreRender()
	{
		var camera = Scene.Camera;
		if ( !Metaball.Debug || !camera.IsValid() )
			return;

		var hud = camera.Hud;
		DrawHeatGrid( hud );
		DebugDrawMetaballs( hud );
		_lastTextPosition = new Vector2( 15f, 0f );
		var screenSize = Screen.Size;
		var renderRect = GetCenteredScreenRect();
		hud.DrawRect( renderRect, Color.Transparent, Vector4.Zero, Vector4.One * 2, Color.Green.WithAlpha( 0.15f ) );
		PrintDebugText( $"Screen Size: {screenSize}, Render Rect: {renderRect.Size}" );
		var worldStart = -World.SimulationSize;
		var worldEnd = World.SimulationSize;
		PrintDebugText( $"World Size: {worldStart} to {worldEnd}" );
		var mousePos = ScreenToPoint( Mouse.Position );
		var mouseUv = ScreenToUV( Mouse.Position );
		PrintDebugText( $"Mouse Position: {mousePos}, Mouse UV: {mouseUv}" );
		var mouseCurrent = World.GetConvectionDirection( mousePos );
		PrintDebugText( $"Convection Direction: {mouseCurrent}" );
	}

	private void DrawHeatGrid( HudPainter hud, float res = 32f )
	{
		res = MathF.Max( 16f, res );
		var screenRect = GetCenteredScreenRect();
		var xMin = screenRect.Position.x;
		var xMax = xMin + screenRect.Size.x;
		var yMin = screenRect.Position.y;
		var yMax = yMin + screenRect.Size.y;
		for ( float y = yMin; y < yMax; y += res )
		{
			for ( float x = xMin; x < xMax; x += res )
			{
				var screenPos = new Vector2( x, y );
				var temp = GetTileTemperature( screenPos + res * 0.5f );
				var color = GetTemperatureDebugColor( temp );
				var current = GetTileCurrent( screenPos );
				DebugDrawCurrent( hud, screenPos + res * 0.5f, current, res );
				var xSize = MathF.Min( res, xMax - x );
				var ySize = MathF.Min( res, yMax - y );
				var tileSize = new Vector2( xSize, ySize );
				var tileRect = new Rect( screenPos, tileSize );
				hud.DrawRect( tileRect, color.WithAlpha( 0.05f ), default, Vector4.One, color.WithAlpha( 0.025f) );
			}
		}
	}

	private float GetTileTemperature( Vector2 screenPos )
	{
		var tilePoint = ScreenToPoint( screenPos );
		var heating = World.GetHeating( tilePoint );
		var cooling = World.GetCooling( tilePoint );
		return heating - cooling;
	}

	private Vector2 GetTileCurrent( Vector2 screenPos )
	{
		var tilePoint = ScreenToPoint( screenPos );
		var current = World.GetConvectionDirection( tilePoint );
		return new Vector2( -current.y, -current.z );
	}

	private Color GetTemperatureDebugColor( float temperature )
	{
		var maxCooling = -World.VerticalCoolingCurve.ValueRange.y;
		var maxHeating = World.VerticalHeatingCurve.ValueRange.y;
		var frac = temperature.LerpInverse( maxCooling, maxHeating );
		return Color.Lerp( Color.Blue, Color.Red, frac );
	}

	private void DebugDrawMetaballs( HudPainter hud )
	{
		for ( int i = 0; i < World.MetaballCount; i++ )
		{
			var ball = World[i];
			var screenPos = PointToScreen( ball.Position );
			var screenRadius = ball.Radius * PointToScreenScale.x;
			var rect = new Rect( screenPos - screenRadius, screenRadius * 2f );
			var screenDiameter = screenRadius * 2f;
			hud.DrawRect( rect, Color.Transparent, new Vector4( screenDiameter, screenDiameter ), new Vector4( 1f, 1f ), Color.Green.WithAlpha( 0.1f ) );
			var text = new TextRendering.Scope( $"{i}", Color.White, 12, "Consolas" );
			hud.DrawText( text, rect );
		}
	}

	private void DebugDrawCurrent( HudPainter hud, Vector2 screenPos, Vector2 current, float res = 32f )
	{
		res = MathF.Max( 16f, res );
		var arrowLength = res * 0.5f;
		var from = screenPos;
		var to = from + current.Normal * arrowLength;
		var color = Color.White.WithAlpha( 0.1f );
		hud.DrawLine( from, to, 1f, color );
		var arrowHeadLength = arrowLength * 0.5f;
		var arrowHeadAngle = MathF.PI / 4;
		var lineAngle = MathF.Atan2( to.y - from.y, to.x - from.x );
		var arrowHead1 = new Vector2
		{
			x = to.x - arrowHeadLength * MathF.Cos( lineAngle - arrowHeadAngle ),
			y = to.y - arrowHeadLength * MathF.Sin( lineAngle - arrowHeadAngle )
		};
		hud.DrawLine( to, arrowHead1, 1f, color );
		var arrowHead2 = new Vector2
		{
			x = to.x - arrowHeadLength * MathF.Cos( lineAngle + arrowHeadAngle ),
			y = to.y - arrowHeadLength * MathF.Sin( lineAngle + arrowHeadAngle )
		};
		hud.DrawLine( to, arrowHead2, 1f, color );
	}

	private Vector2 _lastTextPosition;

	private void PrintDebugText( string text, Vector2? screenPixel = null, TextFlag flags = TextFlag.LeftTop )
	{
		screenPixel ??= _lastTextPosition;
		Gizmo.Draw.ScreenText( text, screenPixel.Value, font: "Consolas", flags: flags );
		_lastTextPosition.y += Screen.Height / 100f * 4f;
	}

	private void Render( SceneObject sceneObject )
	{
		var cantFindBalls = !World.IsValid() || World.MetaballCount < 1;
		var cantRender = !RenderToScreen || !_sceneObject.IsValid();
		if ( cantFindBalls || cantRender )
			return;

		UpdateAttributes();

		var screenRect = GetCenteredScreenRect();
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
