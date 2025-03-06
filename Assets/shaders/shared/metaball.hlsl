#define MAX_BALLS 256

class Metaball 
{
    float3 Position;
    float Radius;
    float4 Color;
};

StructuredBuffer<Metaball> Balls < Attribute( "Balls" ); >;