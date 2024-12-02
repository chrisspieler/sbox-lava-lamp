FEATURES
{
    #include "common/features.hlsl"
}

MODES 
{
	VrForward();
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
	Depth( S_MODE_DEPTH );
}

COMMON
{
	#include "common/shared.hlsl"
	#include "shared/metaball.hlsl"

	int BallCount < Attribute( "BallCount" ); >;
	float BoundsMarginWs < Attribute( "BoundsMarginWs" ); Default( 0.25 ); Range( 0, 4 ); >;
	float3 WorldPosition < Attribute( "WorldPosition" ); >;
	float SimulationSize < Attribute( "SimulationSize" ); >;
	float ColorBlendScale < Attribute( "ColorBlendScale" ); Default( 2.5 ); >;
	float ShapeBlendScale < Attribute( "ShapeBlendScale" ); Default( 5 ); >;
	float3 LampOffset < Attribute( "LampOffset" ); Default3( 0, 0, 0 ); >;
	float3 LampBottomCenter < Attribute( "LampBottomCenter" ); Default3( 0, 0, -7.75 ); >;
	float3 LampTopCenter < Attribute( "LampTopCenter" ); Default3( 0, 0, 7.75 ); >;
	float LampBottomRadius < Attribute( "LampBottomRadius" );  Default( 3.75 ); >;
	float LampTopRadius < Attribute( "LampTopRadius" ); Default( 2.5 ); >;

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

	float IntersectSDF( float distA, float distB )
	{
		return max( distA, distB );
	}

	float SmoothMin( float a, float b, float k )
	{
		float h = max( k - abs( a - b ), 0 ) / k;
		return min( a, b ) - h*h*h*k*1/6.0;
	}

	// Distance function copied from: https://iquilezles.org/articles/distfunctions/
	float CappedConeSDF( float3 p, float3 a, float3 b, float ra, float rb )
	{
		float rba  = rb-ra;
		float baba = dot(b-a,b-a);
		float papa = dot(p-a,p-a);
		float paba = dot(p-a,b-a)/baba;
		float x = sqrt( papa - paba*paba*baba );
		float cax = max(0.0,x-((paba<0.5)?ra:rb));
		float cay = abs(paba-0.5)-0.5;
		float k = rba*rba + baba;
		float f = clamp( (rba*(x-ra)+paba*baba)/k, 0.0, 1.0 );
		float cbx = x-ra - f*rba;
		float cby = paba - f;
		float s = (cbx<0.0 && cay<0.0) ? -1.0 : 1.0;
		return s*sqrt( min(cax*cax + cay*cay*baba,
							cbx*cbx + cby*cby*baba) );
	}

	float SphereSDF( float3 samplePoint, Metaball ball )
	{
		float3 p = samplePoint - WorldPosition - ball.Position;
		float s = ball.Radius;
		return length(p) - s;
	}

	float3 SphereColor( Metaball ball, float sdBall )
	{
		float influence = saturate( pow( ball.Radius / sdBall, ColorBlendScale / ball.Radius ) );
		return ball.Color.rgb * influence;
	}

	RaymarchResult AllMetaballSDF( float3 samplePoint )
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
			sdScene = SmoothMin( sdScene, sdBall, ShapeBlendScale * ball.Radius );
		}
		return RaymarchResult::From( sdScene, saturate( albedo ) );
	}

	RaymarchResult SceneSDF( float3 samplePoint )
	{
		RaymarchResult sdMetaballs = AllMetaballSDF( samplePoint );
		float3 lampPos = samplePoint - WorldPosition - LampOffset;
		float sdLamp = CappedConeSDF( lampPos, LampBottomCenter, LampTopCenter, LampBottomRadius, LampTopRadius );
		float sdScene = IntersectSDF( sdMetaballs.Distance, sdLamp );
		sdMetaballs.Distance = sdScene;
		return sdMetaballs;
	}
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

	float4x4 Transform < Attribute("Transform"); >;

	float3 GetNearestMetaballWs( float3 vPositionWs )
	{
		float3 nearestPos = vPositionWs;
		float nearestDistance = 10000;

		for( int i = 0; i < BallCount; i++ )
		{
			Metaball ball = Balls[i];
			float3 position = WorldPosition + ball.Position;
			float distance = length( vPositionWs - position ) - ball.Radius;
			if ( distance < nearestDistance )
			{
				float3 dir = normalize( position - vPositionWs );
				nearestDistance = distance;
				nearestPos = vPositionWs + dir * distance;
			}
		}
		return nearestPos;
	}

	float3 ShrinkByDistance( float3 vStartPosWs, float3 distance )
	{
		float3 rayToCenter = normalize( WorldPosition - vStartPosWs.xyz );
		return vStartPosWs + rayToCenter * ( distance - BoundsMarginWs );
	}

	float3 ShrinkBySceneSDF( float3 vStartPosWs )
	{
		float3 lampPos = vStartPosWs - WorldPosition - LampOffset;
		// float sceneSDF = SceneSDF( vPositionWs ).Distance;
		float sceneSDF = CappedConeSDF( lampPos, LampBottomCenter, LampTopCenter, LampBottomRadius, LampTopRadius );
		return ShrinkByDistance( vStartPosWs, sceneSDF );
	}

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		o.vPositionWs = mul( Transform, float4( i.vPositionOs.xyz, 1 ) ).xyz;
		float3 vNearestMetaballWs = GetNearestMetaballWs( o.vPositionWs );
		float3 vLengthToMetaballWs = length( vNearestMetaballWs - o.vPositionWs );
		float3 vDirToMetaballWs = normalize( vNearestMetaballWs - o.vPositionWs );
		float3 vShrinkPositionWs = o.vPositionWs + vDirToMetaballWs * ( vLengthToMetaballWs - BoundsMarginWs );
		if ( length( vShrinkPositionWs - WorldPosition ) < length( o.vPositionWs - WorldPosition ) )
		{
			o.vPositionWs = vShrinkPositionWs;
		}
		// o.vPositionWs = ShrinkBySceneSDF( vStartPosWs );
		o.vPositionPs = Position3WsToPs( o.vPositionWs.xyz );
		return FinalizeVertex( o );
	}
}

PS
{
	StaticCombo( S_MODE_DEPTH, 0..1, Sys(ALL) );
    #include "common/pixel.hlsl"

	int ShowBounds < Attribute( "ShowBounds" ); >;

	RenderState( SrgbWriteEnable0, true );
	RenderState( ColorWriteEnable0, RGBA );
	RenderState( DepthEnable, true );
	RenderState( DepthWriteEnable, S_MODE_DEPTH );


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
			float3 samplePoint = eye + depth * dir;
			RaymarchResult stepResult = SceneSDF( samplePoint );

			depth += stepResult.Distance;
			if ( stepResult.Distance <= 0.0001 )
			{
				stepResult.Distance = depth;
				stepResult.Normal = EstimateNormal( eye + depth * dir );
				return stepResult;
			}
			
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
		if ( ShowBounds > 0 )
			return 1;

		float3 dir = RayDirection( i );
		float3 eye = g_vCameraPositionWs;
		dir = normalize( g_vCameraDirWs + dir );
		RaymarchResult result = Raymarch( eye, dir, 0, 2000 );
		// Check if we're beyond the maximum distance we bother to raymarch.
		if ( result.Distance > 2000 )
		{
			discard;
			return 0;
		}
		#if S_MODE_DEPTH
			return 1;
		#endif
		Material m = Material::From( i );
		m.Albedo = result.Albedo;
		m.Normal = result.Normal;
		m.Emission = m.Albedo * 0.5;
		m.Roughness = 0.2;
		m.AmbientOcclusion = 1;
		return ShadingModelStandard::Shade( i, m );
	}
}
