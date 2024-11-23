HEADER
{
	DevShader = true;
	Version = 1;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
MODES
{
	Default();
	VrForward();
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
FEATURES
{
	#include "ui/features.hlsl"
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
COMMON
{
	#include "ui/common.hlsl"
}
  
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
VS
{
	#include "ui/vertex.hlsl"
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
PS
{
	#include "ui/pixel.hlsl"

	// Setting this to 512 crashes my editor for some reason.
	#define MAX_BALLS 256

	int BallCount < Attribute( "BallCount" ); >;

	class Metaball 
	{
		float2 Position;
		float Radius;
		float _pad;
		float4 Color;

		float SDF( float2 uv )
		{
			return length(uv - Position) - Radius;
		}
	};

	cbuffer BallBuffer 
	{
		Metaball Balls[MAX_BALLS];
	};

	float InnerBlend < Attribute( "InnerBlend"); Default( 3 ); >;
	float CutoffThreshold < Attribute( "CutoffThreshold" ); Default( 0.06 ); >;
	float CutoffSharpness < Attribute( "CutoffSharpness" ); Default( 4 ); >;

	float GetCutoff( float shapeInfluence, float threshold, float sharpness )
	{
		float feather = shapeInfluence / threshold;
		feather = pow( feather, sharpness );
		return smoothstep( 0, 1, feather );
	}

	float4 renderMetaBalls(float2 uv ) {
		
		float shapeInfluence = 0;
		float4 colorInfluence = 0;
		
		for( int i = 0; i < BallCount.x; i++ )
		{
			Metaball ball = Balls[i];
			float sdf = ball.SDF( uv );
			float influence;
			influence = pow( ball.Radius / length( uv - ball.Position ), InnerBlend );
			// distance to outside of circle
			shapeInfluence += influence;
			colorInfluence += ball.Color * influence;
		}
		float threshold = CutoffThreshold / BallCount;
		float4 finalColor = colorInfluence / shapeInfluence;
		float cutoff = GetCutoff( shapeInfluence, threshold, CutoffSharpness );
		finalColor *= saturate( cutoff );
		return finalColor;
	}

	RenderState( SrgbWriteEnable0, true );
	RenderState( ColorWriteEnable0, RGBA );
	RenderState( FillMode, SOLID );
	RenderState( CullMode, NONE );

	// No depth
	RenderState( DepthWriteEnable, false );
	
	#define SUBPIXEL_AA_MAGIC 0.5

	PS_OUTPUT MainPs( PS_INPUT i )
	{
		PS_OUTPUT o;
		UI_CommonProcessing_Pre( i );
		
		float aspect = g_vViewportSize.x / g_vViewportSize.y;
		float2 uv = 2 * ( i.vTexCoord.xy - 0.5 );
		uv.x *= aspect;
		o.vColor = renderMetaBalls( uv );
		return UI_CommonProcessing_Post( i, o );
	}
}