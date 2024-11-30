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
	#include "shared/metaball.hlsl"

	int BallCount < Attribute( "BallCount" ); >;

	class RaymarchResult
	{
		float Distance;
		float3 Albedo;
		float3 Normal;

		static RaymarchResult From( float distance, float3 albedo )
		{
			RaymarchResult result;
			result.Distance = distance;
			result.Albedo = albedo;
			return result;
		}
	};

	RenderState( SrgbWriteEnable0, true );
	RenderState( ColorWriteEnable0, RGBA );
	RenderState( FillMode, SOLID );
	RenderState( CullMode, NONE );
	RenderState( DepthWriteEnable, false );
	RenderState( DepthEnable, false );

	float3 WorldPosition < Attribute( "WorldPosition" ); >;
	float SimulationSize < Attribute( "SimulationSize" ); >;
	float ColorBlendScale < Attribute( "ColorBlendScale" ); Default( 2.5 ); >;
	float ShapeBlendScale < Attribute( "ShapeBlendScale" ); Default( 5 ); >;

	float IntersectSDF( float distA, float distB )
	{
		return max( distA, distB );
	}

	float SmoothMin( float a, float b, float k )
	{
		float h = max( k - abs( a - b ), 0 ) / k;
		return min( a, b ) - h*h*h*k*1/6.0;
	}

	float SphereSDF( float3 samplePoint, Metaball ball )
	{
		float3 p = samplePoint - WorldPosition - ball.Position * 0.5;
		float s = ball.Radius * 0.25;
		return length(p) - s;
	}

	float3 SphereColor( Metaball ball, float sdBall )
	{
		float influence = pow( ball.Radius / sdBall, ColorBlendScale );
		return ball.Color.rgb * influence;
	}

	RaymarchResult SceneSDF( float3 samplePoint )
	{
		if ( BallCount < 0 )
			return RaymarchResult::From( 99999, float3( 0, 0, 0 ) );
		
		Metaball ball = Balls[0];
		float sdScene = SphereSDF( samplePoint, ball );
		float3 albedo = SphereColor( ball, sdScene );
		
		for ( int i = 1; i < BallCount; i++ )
		{
			Metaball ball = Balls[i];
			float sdBall = SphereSDF( samplePoint, ball );
			albedo += SphereColor( ball, sdBall );
			sdScene = SmoothMin( sdScene, sdBall, ShapeBlendScale );
		}
		return RaymarchResult::From( sdScene, saturate( albedo ) );
	}

	float3 EstimateNormal( float3 p )
	{
		return normalize( float3 (
			SceneSDF( float3( p.x + 0.0001, p.y, p.z ) ).Distance - SceneSDF( float3( p.x - 0.0001, p.y, p.z ) ).Distance,
			SceneSDF( float3( p.x, p.y + 0.0001, p.z ) ).Distance - SceneSDF( float3( p.x, p.y - 0.0001, p.z ) ).Distance,
			SceneSDF( float3( p.x, p.y, p.z + 0.0001 ) ).Distance - SceneSDF( float3( p.x, p.y, p.z - 0.0001 ) ).Distance
		));
	}

	RaymarchResult Raymarch( float3 eye, float3 dir, float start, float end )
	{
		float depth = start;
		for ( int i = 0; i < 255; i++ )
		{
			RaymarchResult stepResult = SceneSDF( eye + depth * dir );
			if ( stepResult.Distance <= 0.0001 )
			{
				stepResult.Normal = EstimateNormal( eye + depth * dir );
				return stepResult;
			}
			
			depth += stepResult.Distance;
			if ( depth > end )
			{
				return RaymarchResult::From( depth, float3( 0, 0, 0 ) );
			}
		}
		return RaymarchResult::From( end, float3( 0, 0, 0 ) );
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
		RaymarchResult result = Raymarch( eye, dir, 0, 2000 );
		float depth = Depth::GetLinear( i.vPositionSs );
		if ( result.Distance > depth || result.Distance > 2000 )
		{
			discard;
			return float4( 0, 0, 0, 0 );
		}
		Material m = Material::From( i );
		m.Albedo = result.Albedo;
		m.Normal = result.Normal;
		m.Emission = m.Albedo * 0.5;
		m.Roughness = 0.2;
		m.AmbientOcclusion = 1;
		return ShadingModelStandard::Shade( i, m );
	}
}
