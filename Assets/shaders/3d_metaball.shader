FEATURES
{
    #include "common/features.hlsl"
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		// Add your vertex manipulation functions here
		o.vPositionPs = float4( i.vPositionOs.xyz, 1 );
		return FinalizeVertex( o );
	}
}

PS
{
    #include "common/pixel.hlsl"
	RenderState( SrgbWriteEnable0, true );
	RenderState( ColorWriteEnable0, RGBA );
	RenderState( FillMode, SOLID );
	RenderState( CullMode, NONE );
	RenderState( DepthWriteEnable, true );
	RenderState( DepthEnable, false );

	const int MAX_MARCHING_STEPS = 255;
	const float MIN_DIST = 0.0;
	const float MAX_DIST = 2000.0;
	const float EPSILON = 0.0001;

	float IntersectSDF( float distA, float distB )
	{
		return max( distA, distB );
	}

	float SmoothMin( float a, float b, float k )
	{
		float h = max( k - abs( a - b ), 0 ) / k;
		return min( a, b ) - h*h*h*k*1/6.0;
	}

	float TestSDF( float3 p )
	{
		return p.y;
	}

	float SphereSDF( float3 p, float s )
	{
		return length(p) - s;
	}

	float SceneSDF( float3 samplePoint )
	{
		// float testDist = TestSDF( samplePoint );
		// float sphereDist = SphereSDF( samplePoint + float3( 10, 0, 0 ), 1 );
		// return IntersectSDF( testDist, sphereDist );
		// return TestSDF( samplePoint );
		float sphereA = SphereSDF( samplePoint - float3( 10, 2, -0.1 + sin( g_flTime * 0.7 ) * 6), 3);
		float sphereB = SphereSDF( samplePoint - float3( 10, sin( g_flTime ) * 7, 0.2 ), 3 );
		float sphereC = SphereSDF( samplePoint - float3( 10, 2 + sin( g_flTime ) * 4, 1 + sin( g_flTime * 0.2 ) * 5 ), 3 );
		float smooth1 = SmoothMin( sphereA, sphereB, 5 );
		return SmoothMin( smooth1, sphereC, 5 );
	}

	float Raymarch( float3 eye, float3 dir, float start, float end )
	{
		// return SceneSDF( eye + dir );
		float depth = 1;
		for ( int i = 0; i < 255; i++ )
		{
			float dist = SceneSDF( eye + depth * dir );
			if ( dist <= 0.0001 )
			{
				return depth;
			}
			
			depth += dist;
			if ( depth > 1000 )
			{
				return end;
			}
		}
		return end;
	}

	float3 RayDirection( float fieldOfView, float2 size, float2 fragCoord )
	{
		float2 uv = fragCoord - size / 2.0;
		float x = size.y / tan(radians(fieldOfView / 2.0));
		return normalize( float3(x, uv.x, uv.y ) );
	}

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 eye = g_vCameraPositionWs;
		float3 dir = RayDirection( 90, g_vViewportSize.xy, i.vPositionSs.xy );
		dir = normalize( g_vCameraDirWs + dir );
		float dist = Raymarch( eye, dir, MIN_DIST, MAX_DIST );
		if ( dist > 0 )
		{
			return float4( 1, 0, 0, 1 );
		}
		else 
		{
			discard;
			return float4( 0, 0, 0, 0 );
		}
	}
}
