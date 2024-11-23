public static class CollisionDetection
{
	public static bool IntersectLine( Line self, Line other, out Vector2 hitNormal, out Vector2 hitPosition, out float distance )
	{
		hitNormal = Vector2.Zero;
		distance = 0f;
		hitPosition = Vector2.Zero;

		float x1 = self.Start.x;
		float y1 = self.Start.y;
		float x2 = self.End.x;
		float y2 = self.End.y;
		float x3 = other.Start.x;
		float y3 = other.Start.y;
		float x4 = other.End.x;
		float y4 = other.End.y;

		// You'd better believe I copied and pasted all this.
		float uA = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
		float uB = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

		var isIntersecting = uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1;

		// The lines don't intersect. Bail! 
		if ( !isIntersecting )
			return false;

		// Find the point at which the two lines intersect.
		float iX = x1 + (uA * (x2 - x1));
		float iY = y1 + (uA * (y2 - y1));
		hitPosition = new Vector2( iX, iY );
		distance = self.Start.Distance( hitPosition );

		// Find the two normals of the other line.
		float dx = x4 - x3;
		float dy = y4 - y3;
		var n1 = new Vector2( -dy, dx ).Normal;
		var n2 = new Vector2( dy, -dx ).Normal;

		// Figure out which normal is the one we can reflect off of.
		var selfDir = (self.End - self.Start).Normal;
		var normalDot = selfDir.Dot( n1 );
		hitNormal = normalDot <= 0 ? n1 : n2;

		return true;
	}
}
