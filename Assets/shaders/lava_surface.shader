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
		return FinalizeVertex( o );
	}
}

PS
{
    #include "common/pixel.hlsl"

	float3 LavaColorCool < Attribute( "LavaColorCool" ); UiType( Color ); Default3( 1, 0.1, 0 ); >;
	float3 LavaColorHot < Attribute( "LavaColorHot" ); UiType( Color ); Default3( 1, 0.3, 0 ); >;
	float LavaHotHeight < Attribute( "LavaHotHeight" ); Range( 0, 100 ); UiType( Slider ); Default( 16 ); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 vPositionWs = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		Material m = Material::From( i );
		float height = saturate( vPositionWs.z / LavaHotHeight );
		m.Albedo = lerp( LavaColorHot, LavaColorCool, height );
		m.Emission = m.Albedo * ( 1 - height );
		//m.Albedo = float4( i.vCentroidNormalWs, 1 );
		return ShadingModelStandard::Shade( i, m );
		m.Emission = m.Albedo.rgb * 0.1;
		// return float4( m.Albedo, 1 );
		/* m.Metalness = 1.0f; // Forces the object to be metalic */
		return ShadingModelStandard::Shade( i, m );
	}
}
