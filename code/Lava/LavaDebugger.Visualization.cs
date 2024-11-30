using Sandbox.Rendering;
using System;

public partial class LavaDebugger : Component
{
	public enum LavaVisMode
	{
		None		= 0,
		Velocity	= 1,
		Heat		= 2
	}

	[Property] public LavaVisMode VisMode
	{
		get
		{
			return Enum.IsDefined( typeof( LavaVisMode ), Metaball.MetaballVisMode )
				? (LavaVisMode)Metaball.MetaballVisMode
				: LavaVisMode.None;
		}
		set => Metaball.MetaballVisMode = (int)value;
	}

	protected override void OnPreRender()
	{
		if ( !Metaball.WorldDebug || !Renderer2D.IsValid() || !Renderer2D.TargetCamera.IsValid() )
			return;

		var hud = Renderer2D.TargetCamera.Hud;
		DebugDrawWorldInfo( hud );
		if ( VisMode == LavaVisMode.None )
			return;

		DebugDrawMetaballs( hud );
		if ( VisMode == LavaVisMode.Heat )
		{
			DrawHeatGrid( hud );
		}
	}

	private void DebugDrawWorldInfo( HudPainter hud )
	{
		_lastTextPosition = new Vector2( 15f, 0f );
		var screenSize = Screen.Size;
		var viewportAspect = Renderer2D.TargetCamera.GetScreenToViewportScale();
		var simAspect = Renderer2D.BoundsAspect;
		var viewportRect = Renderer2D.TargetCamera.GetCenteredViewportRect( simAspect );
		hud.DrawRect( viewportRect, Color.Transparent, Vector4.Zero, Vector4.One * 2, Color.Green.WithAlpha( 0.15f ) );
		PrintDebugText( $"sizeScreen: {screenSize}, aspectWorld: {simAspect}, aspectViewport: {viewportAspect}, positionViewport: {viewportRect.Position}, sizeViewport: {viewportRect.Size}" );
		var worldStart = -World.SimulationSize;
		var worldEnd = World.SimulationSize;
		PrintDebugText( $"World Size: {worldStart} to {worldEnd}" );
		var mousePos = Renderer2D.ScreenToPoint( Mouse.Position );
		var mouseUv = Renderer2D.ScreenToUV( Mouse.Position );
		PrintDebugText( $"Mouse Position: {mousePos}, Mouse UV: {mouseUv}" );
		var mouseCurrent = World.GetConvectionDirection( mousePos );
		PrintDebugText( $"Convection Direction: {mouseCurrent}" );
	}

	private Vector2 _lastTextPosition;

	private void PrintDebugText( string text, Vector2? screenPixel = null, TextFlag flags = TextFlag.LeftTop )
	{
		screenPixel ??= _lastTextPosition;
		Gizmo.Draw.ScreenText( text, screenPixel.Value, font: "Consolas", flags: flags );
		_lastTextPosition.y += Screen.Height / 100f * 4f;
	}

	private void DrawHeatGrid( HudPainter hud, float res = 32f )
	{
		
		res = MathF.Max( 16f, res );
		var screenRect = Renderer2D.TargetCamera.GetCenteredViewportRect( Renderer2D.BoundsAspect );
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
				hud.DrawRect( tileRect, color.WithAlpha( 0.05f ), default, Vector4.One, color.WithAlpha( 0.025f ) );
			}
		}
	}
	private float GetTileTemperature( Vector2 screenPos )
	{
		var tilePoint = Renderer2D.ScreenToPoint( screenPos );
		var heating = World.GetHeating( tilePoint );
		var cooling = World.GetCooling( tilePoint );
		return heating - cooling;
	}

	private Vector2 GetTileCurrent( Vector2 screenPos )
	{
		var tilePoint = Renderer2D.ScreenToPoint( screenPos );
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
			var screenPos = Renderer2D.PointToScreen( ball.Position );
			var screenRadius = ball.Radius * ( 1f / World.PointToWorldScale ) * Renderer2D.PointToScreenScale;
			var rect = new Rect( screenPos - screenRadius, screenRadius * 2f );
			var screenDiameter = screenRadius * 2f;
			hud.DrawRect( rect, Color.Transparent, new Vector4( screenDiameter, screenDiameter ), new Vector4( 1f, 1f ), Color.Green.WithAlpha( 0.1f ) );
		}
	}

	private void DebugDrawCurrent( HudPainter hud, Vector2 screenPos, Vector2 current, float res = 32f )
	{
		res = MathF.Max( 16f, res );

		var arrowLength = res * 0.5f;
		var from = screenPos;
		var to = from + current.Normal * arrowLength;
		var color = Color.White.WithAlpha( 0.1f );
		hud.DrawArrow( from, to, arrowLength, color );
	}
}
