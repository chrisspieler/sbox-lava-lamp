using System;

public partial class LavaRenderer2D : Component
{
	public Vector2 BoundsAspect
	{
		get
		{
			if ( !World.IsValid() )
				return Vector2.One;

			var size = new Vector2( World.SimulationSize.y, World.SimulationSize.z );
			if ( size.x > size.y )
			{
				return new( 1f, size.y / size.x );
			}
			else
			{
				return new( size.x / size.y, 1f );
			}
		}
	}

	public float PointToScreenScale
	{
		get
		{
			float min = MathF.Min( World.SimulationSize.x, World.SimulationSize.y );
			if ( min == 0 )
				return 0.001f;

			var viewportSize = Screen.Size;
			if ( TargetCamera.IsValid() )
			{
				viewportSize = TargetCamera.GetViewportSize();
			}
			return MathF.Min( viewportSize.x, viewportSize.y ) / 2f;
		}
	}

	public Vector2 ScreenToUV( Vector2 screenPos )
	{
		if ( !TargetCamera.IsValid() )
			return Vector2.Zero;

		var viewport = TargetCamera.GetCenteredViewportRect( BoundsAspect );
		return ( screenPos - viewport.Position ) / viewport.Size;
	}

	public Vector3 ScreenToPoint( Vector2 screenPos )
	{
		if ( !World.IsValid() )
			return Vector3.Zero;

		Vector2 uv = ScreenToUV( screenPos );
		return World.UVToPoint( uv );
	}

	public Vector2 PointToScreen( Vector3 simPosition )
	{
		if ( !World.IsValid() )
			return Vector2.Zero;

		var uv = World.PointToUV( simPosition );
		return UVToScreen( uv );
	}

	public Vector2 UVToScreen( Vector2 uv )
	{
		if ( !TargetCamera.IsValid() )
			return Vector2.Zero;

		var viewport = TargetCamera.GetCenteredViewportRect( BoundsAspect );
		return viewport.Position + viewport.Size * uv;
	}
}
