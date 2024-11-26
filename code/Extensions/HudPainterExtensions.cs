using Sandbox.Rendering;
using System;

public static class HudPainterExtensions
{
	public static void DrawArrow( this HudPainter hud, Vector2 from, Vector2 to, float size, Color color )
	{
		hud.DrawLine( from, to, 1f, color );
		var arrowHeadLength = size * 0.5f;
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
}
