public static class ColorExtensions
{
	public static Color Clamp01( this Color color )
	{
		return color with
		{
			r = color.r.Clamp( 0f, 1f ),
			g = color.g.Clamp( 0f, 1f ),
			b = color.b.Clamp( 0f, 1f ),
			a = color.a.Clamp( 0f, 1f )
		};
	}
}
