public static class CameraExtensions
{
	public static Vector2 ViewportUVToScreen( this CameraComponent camera, Vector2 uv )
	{
		var viewport = GetViewportRect( camera );
		return viewport.Position + viewport.Size * uv;
	}

	public static Vector2 ViewportPixelToScreen( this CameraComponent camera, Vector2 viewportPixel )
	{
		var viewport = GetViewportRect( camera );
		return viewport.Position + viewportPixel;
	}

	public static Vector2 ScreenToViewportPixel( this CameraComponent camera, Vector2 screenPos )
	{
		var viewport = GetViewportRect( camera );
		return screenPos - viewport.Position;
	}

	public static Vector2 ScreenToViewportNormal( this CameraComponent camera, Vector2 screenPos )
	{
		var viewport = GetViewportRect( camera );
		var viewportPixel = screenPos - viewport.Position;
		return viewportPixel / viewport.Size;
	}

	public static Rect GetCenteredViewportRect( this CameraComponent camera, Vector2 rectScale )
	{
		var viewportRect = GetViewportRect( camera );
		rectScale *= viewportRect.Size * 0.75f;
		return viewportRect.Align( rectScale, TextFlag.Center );
	}

	/// <summary>
	/// Get the position of the camera viewport in pixels.
	/// </summary>
	public static Vector2 GetViewportPosition( this CameraComponent camera )
	{
		return Screen.Size * new Vector2( camera.Viewport.x, camera.Viewport.y );
	}

	/// <summary>
	/// Get the size of the camera viewport in pixels.
	/// </summary>
	public static Vector2 GetViewportSize( this CameraComponent camera )
	{
		var xSize = MathX.Clamp( camera.Viewport.z - camera.Viewport.x, 0f, 1f );
		var ySize = MathX.Clamp( camera.Viewport.w - camera.Viewport.y, 0f, 1f );
		return new Vector2( xSize, ySize ) * Screen.Size;
	}

	public static Rect GetViewportRect( this CameraComponent camera )
	{
		var size = GetViewportSize( camera );
		var offset = GetViewportPosition( camera );
		// var remainder = Screen.Size 
		return new Rect( offset, size );
	}

	/// <summary>
	/// Returns a Vector2 which when scaled by Screen.Size should result in the size of the camera viewport.
	/// <br/><br/>
	/// If the viewport square, both x and y will be 1. 
	/// <br/>
	/// If the viewport is wider than it is tall, x will be 1, and y will be the inverse aspect ratio of the viewport. 
	/// <br/>
	/// If the viewport is taller than it is wide, y will be 1, and x will be the inverse aspect ratio of the viewport.
	/// </summary>
	public static Vector2 GetScreenToViewportScale( this CameraComponent camera )
	{
		var size = GetViewportSize( camera );
		var viewportAspect = size.x / size.y;
		return viewportAspect > 1f
			? new Vector2( 1f, 1f / viewportAspect )
			: new Vector2( 1f / viewportAspect, 1f );
	}
}
