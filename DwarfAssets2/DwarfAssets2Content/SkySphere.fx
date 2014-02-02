float4x4 World;
float4x4 View;
float4x4 Projection;

float3 CameraPosition;

Texture SkyBoxTexture; 
samplerCUBE SkyBoxSampler = sampler_state 
{ 
   texture = <SkyBoxTexture>; 
   magfilter = LINEAR; 
   minfilter = LINEAR; 
   mipfilter = LINEAR; 
   AddressU = Mirror; 
   AddressV = Mirror; 
};

float xTint;

Texture TintTexture; 
sampler TintSampler = sampler_state 
{ 
   texture = <TintTexture>; 
   magfilter = LINEAR; 
   minfilter = LINEAR; 
   mipfilter = LINEAR; 
   AddressU = Mirror; 
   AddressV = Mirror; 
};



struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 TextureCoordinate : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    float4 VertexPosition = worldPosition;
    output.TextureCoordinate = VertexPosition - CameraPosition;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    return float4(normalize(input.TextureCoordinate), 1) + texCUBE(SkyBoxSampler, normalize(input.TextureCoordinate));
}

float4 TintShaderFunction(VertexShaderOutput input) : COLOR0
{
	return tex2D(TintSampler, float2(xTint, 1.0 - (normalize(input.TextureCoordinate).y + 0.05) ));
}

technique Skybox
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

technique SkyBoxFake
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 TintShaderFunction();
    }
}