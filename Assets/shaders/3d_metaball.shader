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
	RenderState( DepthWriteEnable, false );
	RenderState( DepthEnable, false );

	float IntersectSDF( float distA, float distB )
	{
		return max( distA, distB );
	}

	float SmoothMin( float a, float b, float k )
	{
		float h = max( k - abs( a - b ), 0 ) / k;
		return min( a, b ) - h*h*h*k*1/6.0;
	}

	float SphereSDF( float3 p, float s )
	{
		return length(p) - s;
	}

	float SceneSDF( float3 samplePoint )
	{
		float3 sphereALocalPos = float3( 24, 2, 8 + sin( g_flTime * 0.7 ) * 6);
		float sphereA = SphereSDF( samplePoint - sphereALocalPos, 3);

		float3 sphereBLocalPos = float3( 24, sin( g_flTime ) * 7, 8 );
		float sphereB = SphereSDF( samplePoint - sphereBLocalPos, 3 );

		float3 sphereCLocalPos = float3( 24, 2 + sin( g_flTime ) * 4, 8 + sin( g_flTime * 0.2 ) * 5 );
		float sphereC = SphereSDF( samplePoint - sphereCLocalPos, 3 );

		float smooth1 = SmoothMin( sphereA, sphereB, 5 );
		return SmoothMin( smooth1, sphereC, 5 );
	}

	float Raymarch( float3 eye, float3 dir, float start, float end )
	{
		float depth = 1;
		for ( int i = 0; i < 255; i++ )
		{
			float dist = SceneSDF( eye + depth * dir );
			if ( dist <= 0.0001 )
			{
				return depth;
			}
			
			depth += dist;
			if ( depth > 2000 )
			{
				return end;
			}
		}
		return end;
	}

	float3 RayDirection( PixelInput i )
	{
		float2 vUV = i.vPositionSs.xy / g_vViewportSize.xy;
		vUV -= 0.5;
		vUV *= 2;
		float3 vRayCs = mul( g_matWorldToProjection, g_vCameraDirWs );
		vRayCs += float3( vUV.x, -vUV.y, 0 );
		return mul( g_matProjectionToWorld, vRayCs );
	}

	float4 MainPs( PixelInput i ) : SV_Target0
	{	

		float3 dir = RayDirection( i );
		float3 eye = g_vCameraPositionWs;
		dir = normalize( g_vCameraDirWs + dir );
		float dist = Raymarch( eye, dir, 0, 2000 );
		float depth = Depth::GetLinear( i.vPositionSs );
		if ( depth > dist && dist < 2000 )
		{
			return float4( 1, 0, 0, 1 );
		}
		discard;
		return float4( 0, 0, 0, 0 );
	}
}
