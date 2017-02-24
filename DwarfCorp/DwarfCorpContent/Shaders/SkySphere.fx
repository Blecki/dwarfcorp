//reuse//code//Vertex//
uniform extern float4x4 ViewMatrix;
uniform extern float4x4 ProjectionMatrix;
float4x4 xRot;
float xTransparency;
float xTint;

void SkyboxVertexShader( float3 pos : POSITION0,
                         out float4 SkyPos : POSITION0,
                         out float3 SkyCoord : TEXCOORD0 )
{
    // Calculate rotation. Using a float3 result, so translation is ignored
    float3 rotatedPosition = mul(mul(pos, xRot), ViewMatrix);           
    // Calculate projection, moving all vertices to the far clip plane 
    // (w and z both 1.0)
    SkyPos = mul(float4(rotatedPosition, 1), ProjectionMatrix).xyww;    

    SkyCoord = pos;

};
//reuse//code//Vertex//
//reuse//code//Pixel//
texture SkyboxTexture;
texture TintTexture;

sampler TintS = sampler_state
{
    Texture = <TintTexture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    MipFilter = LINEAR;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

sampler SkyboxS = sampler_state
{
    Texture = <SkyboxTexture>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = WRAP;
    AddressV = WRAP;
};


float4 SkyboxPixelShader( float3 SkyCoord : TEXCOORD0 ) : COLOR
{
    // grab the pixel color value from the skybox cube map
	float4 toReturn = texCUBE(SkyboxS, SkyCoord * 2) * tex2D(TintS, float2(xTint, 0.5f));
	toReturn.a = xTransparency;
    return toReturn;
};
//reuse//code//Pixel//
//reuse//code//Technique//
technique SkyboxTechnique
{
    pass P0
    {
        vertexShader = compile vs_2_0 SkyboxVertexShader();
        pixelShader = compile ps_2_0 SkyboxPixelShader();

        // We're drawing the inside of a model
        CullMode = None;  
        // We don't want it to obscure objects with a Z < 1
        ZWriteEnable = false; 
		SrcBlend = SrcAlpha;
		DestBlend =  InvSrcAlpha;

    }
}

technique SunMoonTechnique
{
    pass P0
    {
        vertexShader = compile vs_2_0 SkyboxVertexShader();
        pixelShader = compile ps_2_0 SkyboxPixelShader();

        // We're drawing the inside of a model
        CullMode = None;  
        // We don't want it to obscure objects with a Z < 1
        ZWriteEnable = false; 
		SrcBlend = SrcAlpha;
		DestBlend =  One;

    }
}
//reuse//code//Technique//