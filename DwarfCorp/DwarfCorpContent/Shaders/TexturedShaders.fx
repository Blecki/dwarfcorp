
#define MAX_LIGHTS 16
#define LIGHT_COLOR float4(0, 0, 1, 0)

float3 xLightPositions[MAX_LIGHTS];


//------- Constants --------
float4x4 xView;
float4x4 xLightView;
float4x4 xLightProj;
float4x4 xReflectionView;

float xWaterOpacity;

float xWaterMinOpacity;

float4x4 xProjection;
float4x4 xWorld;
int xEnableLighting;
int xEnableShadows;
int xEnableWind;

Texture xWaterBumpMap;

float xWaveLength;
float xWaveHeight;

float3 xCamPos;

float xTime;
float xWindForce;
float3 xWindDirection;
float xTimeOfDay;
int xEnableFog;

float xFogStart;
float xFogEnd;
float3 xFogColor;
float4 xRippleColor;
float4 xFlatColor;
float2 pixelSize;
float4 xID;

//------- Technique: Clipping Plane Fix --------

int Clipping;
int GhostMode;
int SelfIllumination;
float4 ClipPlane0;

/*
float4 GetNoise(float4 pos)
{
	pos.w = 0;
	float mag = dot(pos, pos) * 0.00001;
	float sm = sin(mag + 0.1);
	float cm = cos(mag);
	return float4(cm * 0.2 + sm * 0.1, sm * 0.5 + cm * 0.1, sm * 0.2 + cm * 0.1, 0);
}
*/

float4 GetWind(float4 pos, float2 uv, float4 bounds)
{
	pos.w = 0;
	float windPower = 0.5 + sin(pos.x / 10.0f + pos.y / 10.0f + xTime * (1.2f + xWindForce / 10.0f));
	windPower = windPower*0.08f;
	float windTemp = (1.0 - (uv.y - bounds.y) / bounds.w);
	return float4(xWindDirection * windPower * windTemp, 0);
}


float4 GetFlagWind(float4 pos, float2 uv, float4 bounds)
{
	pos.w = 0;
	float3 waver = cross(xWindDirection, float3(0, 1, 0)) + xWindDirection * 0.25;
	float windPower = 0.5 + sin(uv.x * 10 + uv.y * 10 + xTime * (12.0 + xWindForce * 1000));
	windPower *= 0.05;
	float windTemp = ((uv.x - bounds.x) / bounds.z);
	return float4(waver * windPower * windTemp - float3(0, 0.5, 0) * windTemp * (saturate(1.0 -  xWindForce * 1000))
		+ xWindDirection * windTemp * saturate(xWindForce * 1000), 0);
}


//------- Technique: Clipping Plane Fix --------


//------- Texture Samplers --------
Texture xTexture;
Texture xIllumination;

sampler IllumSampler = sampler_state { texture = <xIllumination> ;  magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = clamp; AddressV = clamp;};

sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = POINT; minfilter = LINEAR; mipfilter = Linear; AddressU = clamp; AddressV = clamp; };

sampler ColorscaleSampler = sampler_state { texture = <xTexture>; magfilter = POINT; minfilter = LINEAR; mipfilter = Linear; AddressU = clamp; AddressV = clamp; };

sampler WrappedTextureSampler = sampler_state { texture = <xTexture>; magfilter = POINT; minfilter = LINEAR; mipfilter = Linear; AddressU = wrap; AddressV = wrap; };

Texture xReflectionMap;
float xWaterReflective;

Texture xSunGradient;
Texture xAmbientGradient;
Texture xTorchGradient;

Texture xShoreGradient;
Texture xLightmap;
// Light ramp tint
float4 xTint;
// Multiplicative output color.
float4 xColorTint;

sampler LightmapSampler = sampler_state { texture = <xLightmap>; magfilter = POINT; minfilter = LINEAR; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

sampler SunSampler = sampler_state { texture = <xSunGradient>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };

sampler AmbientSampler = sampler_state { texture = <xAmbientGradient>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };

sampler TorchSampler = sampler_state { texture = <xTorchGradient>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

sampler ShoreSampler = sampler_state { texture = <xShoreGradient>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = wrap; AddressV = clamp; };

sampler ReflectionSampler = sampler_state { texture = <xReflectionMap> ; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = clamp; AddressV = clamp;};

sampler WaterBumpMapSampler = sampler_state { texture = <xWaterBumpMap> ; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = wrap; AddressV = wrap;};

Texture xShadowMap;
sampler ShadowMapSampler = sampler_state { texture = <xShadowMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };


///////////// Technique untextured
	struct UTVertexToPixel
	{
	float4 Position     : POSITION;
	float4 Color        : COLOR0;
	float4 ClipDistance     : TEXCOORD5;
	};

	struct UTPixelToFrame
	{
	float4 Color : COLOR0;
	};

	UTVertexToPixel UTexturedVS( float4 inPos_ : POSITION,  float4 inColor : COLOR0)
	{
		UTVertexToPixel Output = (UTVertexToPixel)0;
		float4 inPos = inPos_; // +GetNoise(inPos_);
		float4x4 preViewProjection = mul (xView, xProjection);
		float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);

		Output.Position = mul(inPos, preWorldViewProjection);
		Output.Color = inColor * xTint;

		Output.ClipDistance = Clipping * dot(mul(xWorld,inPos), ClipPlane0); //MSS - Water Refactor added

		return Output;
	}

	UTPixelToFrame UTexturedPS(UTVertexToPixel PSIn)
	{
		UTPixelToFrame Output = (UTPixelToFrame)0;

		Output.Color = PSIn.Color;

		clip(PSIn.ClipDistance);
	
		return Output;
	}


	technique Untextured
	{
		pass Pass0
		{   
			VertexShader = compile vs_3_0 UTexturedVS();
			PixelShader = compile ps_3_0 UTexturedPS();
		}
	}


float2 ClampTexture(float2 uv, float4 bounds)
{
	return float2(clamp(uv.x, bounds.x, bounds.z), clamp(uv.y, bounds.y, bounds.w));
}


///////////////
// Shadowmaps
struct SMapVertexToPixel
{
	float4 Position     : POSITION;
	float4 WorldPosition     : TEXCOORD0;
	float2 TextureCoords : TEXCOORD1;
};

struct SMapPixelToFrame
{
	float4 Color : COLOR0;
};

float4 GetPositionFromLight(float4 position)
{
	float4x4 WorldViewProjection = mul(xLightView, xLightProj);
	return mul(position, WorldViewProjection);
}

SMapVertexToPixel ShadowMapVS(float4 inPos : POSITION, float2 inTexCoords : TEXCOORD0, float4x4 world : BLENDWEIGHT)
{
	float4 worldPosition = mul(inPos, world);
	float4 viewPosition = mul(worldPosition, xView);
	SMapVertexToPixel Output = (SMapVertexToPixel)0;
	Output.Position = mul(viewPosition, xProjection);
	Output.WorldPosition = Output.Position;
	Output.TextureCoords = inTexCoords;
	return Output;
}

SMapPixelToFrame ShadowMapPixelShader(SMapVertexToPixel PSIn)
{
	SMapPixelToFrame Output = (SMapPixelToFrame)0;
	float4 texColor = tex2D(TextureSampler, PSIn.TextureCoords);
	clip((texColor.a - 0.5));
	Output.Color = 1.0f - (PSIn.WorldPosition.z / PSIn.WorldPosition.w);
	Output.Color.a = 1.0f;
	return Output;
}

SMapVertexToPixel ShadowMapVSNonInstance(float4 inPos : POSITION, float2 inTexCoords : TEXCOORD0)
{
	return ShadowMapVS(inPos, inTexCoords, xWorld);
}

SMapVertexToPixel ShadowMapVSInstance(float4 inPos : POSITION, float2 inTexCoords : TEXCOORD0, float4x4 transform : BLENDWEIGHT)
{
	return ShadowMapVS(inPos, inTexCoords, transpose(transform));
}

technique Shadow
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 ShadowMapVSNonInstance();
		PixelShader = compile ps_3_0 ShadowMapPixelShader();
	}
};

technique ShadowInstanced
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 ShadowMapVSInstance();
		PixelShader = compile ps_3_0 ShadowMapPixelShader();
	}
};


//------- Technique: Textured --------
struct TVertexToPixel
{
	float4 Position      : POSITION;
	float4 Color         : COLOR0;
	float4 WorldPosition : TEXCOORD0;
	float2 TextureCoords : TEXCOORD1;
	float4 ClipDistance : TEXCOORD2;
	float Fog            : TEXCOORD3;	
	float4 TextureBounds : TEXCOORD4;
	float3 ColorTint     : COLOR1;
};

struct SelectionBufferToPixel
{
	float4 Position : POSITION;
	float2 TextureCoords: TEXCOORD1;
	float4 ClipDistance : TEXCOORD2;
	float4 SelectionColor : COLOR0;
};

struct LightmapToPixel
{
	float4 Position          : POSITION;
	float4 ClipDistance     : TEXCOORD0;
	float Fog                : TEXCOORD1;
	float2 LightmapCoords    : TEXCOORD2;
	float4 LightmapBounds    : TEXCOORD3;
	float3 WorldPosition     : TEXCOORD4;
};

struct TPixelToFrame
{
	float4 Color : COLOR0;
};


SelectionBufferToPixel SelectionVS(float4 inPos: POSITION, float2 inTexCoords : TEXCOORD0, float4x4 world : BLENDWEIGHT, float4 selection : COLOR2)
{
	SelectionBufferToPixel Output = (SelectionBufferToPixel)0;

	float4 worldPosition = mul(inPos, world);

	float4 viewPosition = mul(worldPosition, xView);
	Output.Position = mul(viewPosition, xProjection);

	Output.TextureCoords = inTexCoords;
	Output.ClipDistance = dot(worldPosition, ClipPlane0);
	Output.SelectionColor = selection;
	return Output;
}

TVertexToPixel TexturedVS_Flag(float4 inPos : POSITION, 
							float2 inTexCoords : TEXCOORD0, 
							float4 inColor : COLOR0, 
							float4 inTexSource : TEXCOORD1, 
							float3 vertColor : COLOR1)
{
	TVertexToPixel Output = (TVertexToPixel)0;

	float4 worldPosition = mul(inPos, xWorld);

	worldPosition += GetFlagWind(worldPosition, inTexCoords, inTexSource);

	Output.WorldPosition = worldPosition;

	float4 viewPosition = mul(worldPosition, xView);
	Output.Position = mul(viewPosition, xProjection);

	Output.TextureCoords = inTexCoords;
	Output.ClipDistance = dot(worldPosition, ClipPlane0);
	Output.Color = inColor * xTint;
	Output.Color.a = xTint.a;
	Output.ColorTint.rgb = vertColor * xColorTint.rgb;
	Output.Color.a *= xColorTint.a;
	for (int i = 0; i < MAX_LIGHTS; i++)
	{
		float dx = worldPosition.x - xLightPositions[i].x;
		float dy = worldPosition.y - xLightPositions[i].y;
		float dz = worldPosition.z - xLightPositions[i].z;
		float dist = pow(dx, 2) + pow(dy, 2) + pow(dz, 2) + 0.001f;
		Output.Color = saturate(Output.Color + xEnableLighting * LIGHT_COLOR / dist);
	}

	Output.Color = saturate(Output.Color + (1.0 - xEnableLighting) * LIGHT_COLOR / 999.0f);

	Output.Fog = saturate((Output.Position.z - xFogStart) / (xFogEnd - xFogStart));

	Output.TextureBounds = inTexSource;
	return Output;
}

TVertexToPixel TexturedVS(float4 inPos : POSITION,  
						   float2 inTexCoords: TEXCOORD0, 
						   float4 inColor : COLOR0, 
						   float4 inTexSource : TEXCOORD1, 
						   float4x4 world : BLENDWEIGHT, 
						   float4 lightTint : COLOR1, 
						   float3 tint)
{
    TVertexToPixel Output = (TVertexToPixel)0;

	float4 worldPosition = mul(inPos, world);

	worldPosition += xEnableWind * GetWind(worldPosition, inTexCoords, inTexSource);

	Output.WorldPosition = worldPosition;
    
	float4 viewPosition = mul(worldPosition, xView);
    Output.Position = mul(viewPosition, xProjection);
    
	Output.TextureCoords = inTexCoords;
    Output.ClipDistance = dot(worldPosition, ClipPlane0);
	Output.Color = inColor * lightTint;
	Output.Color.a = lightTint.a;
	Output.ColorTint.rgb = tint.rgb * xColorTint.rgb;
	Output.Color.a *= xColorTint.a;
	// Dumb fake lighting for testing.
	/*
	float3 normal = normalize(inPos - float3(0.5, 0.5, 0.5));
	float3 lightPos = normalize(float3(-0.0, 0.5, -1));

	Output.Color.r *= clamp(dot(normal, lightPos), 0, 1);
	Output.Color.g = 0.8;
	*/
	for (int i = 0; i < MAX_LIGHTS; i++)
	{
		float dx = worldPosition.x - xLightPositions[i].x;
		float dy = worldPosition.y - xLightPositions[i].y;
		float dz = worldPosition.z - xLightPositions[i].z;
		float dist = pow(dx, 2) + pow(dy, 2) + pow(dz, 2) + 0.001f;
		Output.Color = saturate(Output.Color + xEnableLighting * LIGHT_COLOR / dist);
	}
	
	Output.Color = saturate(Output.Color + (1.0 - xEnableLighting) * LIGHT_COLOR / 999.0f);
	Output.Fog = saturate((Output.Position.z - xFogStart) / (xFogEnd - xFogStart));
	
	Output.TextureBounds = inTexSource;
    return Output;
}

TVertexToPixel TexturedVS_To_Lightmap(float4 inPos : POSITION, 
							          float2 inTexCoords : TEXCOORD0, 
								      float2 inLightmapCoords : TEXCOORD2,
								      float4 inColor : COLOR0, 
								      float4 inTexSource : TEXCOORD1, 
								      float4 lightTint : COLOR1)
{
	TVertexToPixel Output = (TVertexToPixel)0;
	float4 worldPosition = mul(inPos, xWorld);
	Output.WorldPosition = worldPosition;
	Output.Position = float4(2 * inLightmapCoords.x - 1.0 - pixelSize.x * 0.5, -(2 * inLightmapCoords.y - 1.0 - pixelSize.y * 0.5), 0.0, 1);
	Output.TextureCoords = inTexCoords;
	Output.Color = inColor * lightTint;
	Output.Color.a = lightTint.a;
	Output.ColorTint.rgb = xTint.rgb;

	for (int i = 0; i < MAX_LIGHTS; i++)
	{
		float dx = worldPosition.x - xLightPositions[i].x;
		float dy = worldPosition.y - xLightPositions[i].y;
		float dz = worldPosition.z - xLightPositions[i].z;
		float dist = pow(dx, 2) + pow(dy, 2) + pow(dz, 2) + 0.001f;
		Output.Color = saturate(Output.Color + xEnableLighting * LIGHT_COLOR / dist);
	}

	Output.Color = saturate(Output.Color + (1.0 - xEnableLighting) * LIGHT_COLOR / 999.0f);
	Output.TextureBounds = inTexSource;
	return Output;
}

LightmapToPixel TexturedVS_From_Lightmap(
	float4 inPos : POSITION,
	float2 inLightmapCoords : TEXCOORD2,
	float4 inLightmapBounds : TEXCOORD3
	)
{
	LightmapToPixel Output = (LightmapToPixel)0;
	float4 worldPosition = mul(inPos, xWorld);
	float4 viewPosition = mul(worldPosition, xView);
	Output.Position = mul(viewPosition, xProjection);
	Output.ClipDistance = dot(worldPosition, ClipPlane0);

	Output.Fog = saturate((Output.Position.z - xFogStart) / (xFogEnd - xFogStart));
	Output.LightmapBounds = inLightmapBounds;
	Output.LightmapCoords = inLightmapCoords;
	return Output;
}

TPixelToFrame TexturedPS_From_Lightmap(LightmapToPixel PSIn)
{
	TPixelToFrame Output = (TPixelToFrame)0;

	float2 textureCoords = ClampTexture(PSIn.LightmapCoords, PSIn.LightmapBounds);
	Output.Color = tex2D(LightmapSampler, textureCoords);
	clip((Output.Color.a - 0.5));
	Output.Color.rgba = float4(lerp(Output.Color.rgb, xFogColor, PSIn.Fog) * Output.Color.a, Output.Color.a);

	clip(PSIn.ClipDistance.w);
	//Output = (TPixelToFrame)0;
	//Output.Color = float4(1, 0, 0, 1);
	return Output;
}

TPixelToFrame TexturedPS_To_Lightmap(TVertexToPixel PSIn)
{
	TPixelToFrame Output = (TPixelToFrame)0;
	float4 shadowPos = GetPositionFromLight(PSIn.WorldPosition);
		float2 shadowUV = 0.5 * shadowPos.xy / shadowPos.w + float2(0.5, 0.5);
		shadowUV.y = 1.0f - shadowUV.y;

	/*xEnableShadows
	if (shadowUV.x < 1.0 && shadowUV.y < 1.0 && shadowUV.x > 0 && shadowUV.y > 0)
	{
		float shadowdepth = tex2D(ShadowMapSampler, shadowUV).r;

		// Check our value against the depth value
		float ourdepth = 1 - (shadowPos.z / shadowPos.w) + 0.01;

		// Check the shadowdepth against the depth of this pixel
		// a fudge factor is added to account for floating-point error
		PSIn.Color.r *= saturate(exp(200 * (ourdepth - shadowdepth)) + 0.25f);
	}
	*/

	Output.Color = tex2D(SunSampler, float2(PSIn.Color.r, (xTimeOfDay)));
	Output.Color.a *= PSIn.Color.a;
	Output.Color.rgb += tex2D(TorchSampler, float2(PSIn.Color.b, 0.5f)).rgb;
	Output.Color.rgb *= tex2D(AmbientSampler, float2(PSIn.Color.g, 0.5f)).rgb;
	
	saturate(Output.Color.rgb);
	float2 textureCoords = ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds);
	float4 texColor = tex2D(TextureSampler, textureCoords);
	float4 illumColor = tex2D(IllumSampler, textureCoords);

	Output.Color.rgba *= texColor;
	Output.Color.rgb *= PSIn.ColorTint;

	Output.Color.rgba = lerp(Output.Color.rgba, texColor, SelfIllumination * illumColor.r);
	return Output;
}


TVertexToPixel TexturedVSNonInstanced( float4 inPos : POSITION,  float2 inTexCoords: TEXCOORD0, float4 inColor : COLOR0, float4 inTexSource : TEXCOORD1, float3 vertColor : COLOR1)
{
	return TexturedVS(inPos, inTexCoords, inColor, inTexSource, xWorld, xTint, vertColor);
}

TVertexToPixel TexturedVSInstanced( float4 inPos : POSITION,  
								    float2 inTexCoords: TEXCOORD0, 
									float4 inColor : COLOR0, 
									float4 inTexSource : TEXCOORD1, 
									float4 tint : COLOR1, 
									float4x4 transform : BLENDWEIGHT, 
									float4 instanceColor : COLOR2)
{
	return TexturedVS(inPos, inTexCoords, inColor, inTexSource, transpose(transform), 
		              instanceColor, float3(1, 1, 1));
}

SelectionBufferToPixel SelectionVSNonInstanced(float4 inPos : POSITION, float2 inTexCoords : TEXCOORD0)
{
	return  SelectionVS(inPos, inTexCoords, xWorld, xID);
}

SelectionBufferToPixel SelectionVSInstanced(float4 inPos : POSITION,
	float2 inTexCoords : TEXCOORD0,
	float4x4 transform : BLENDWEIGHT,
	float4 id : COLOR3)
{
	return SelectionVS(inPos, inTexCoords, transpose(transform) , id);
}


TPixelToFrame TexturedPS_Colorscale(TVertexToPixel PSIn)
{
	TPixelToFrame Output = (TPixelToFrame)0;

	Output.Color = tex2D(ColorscaleSampler, ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds));
	Output.Color.rgb *= tex2D(AmbientSampler, float2(PSIn.Color.g, 0.5f)).rgb;

	clip(PSIn.ClipDistance.w);
	return Output;
}

TPixelToFrame SelectionPS_Alphatest(SelectionBufferToPixel PSIn)
{
	TPixelToFrame Output = (TPixelToFrame)0;

	float2 textureCoords = PSIn.TextureCoords;
	float4 texColor = tex2D(TextureSampler, textureCoords);

	clip((texColor.a - 0.5));

	clip(PSIn.ClipDistance.w);
	Output.Color = PSIn.SelectionColor;
	return Output;
}

TPixelToFrame SilhouettePS(TVertexToPixel PSIn)
{
	TPixelToFrame Output = (TPixelToFrame)0;
	float2 textureCoords = ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds);
	float4 texColor = tex2D(TextureSampler, textureCoords);
	clip((texColor.a - 0.5));
	Output.Color.rgb = PSIn.ColorTint;
	Output.Color.a = 0.2;
	return Output;
}

TPixelToFrame TexturedPS_Alphatest(TVertexToPixel PSIn)
{
    TPixelToFrame Output = (TPixelToFrame)0;

	/*
	if (xEnableShadows)
	{
		float4 shadowPos = GetPositionFromLight(PSIn.WorldPosition);
		float2 shadowUV = 0.5 * shadowPos.xy / shadowPos.w + float2(0.5, 0.5);
		shadowUV.y = 1.0f - shadowUV.y;
		if (shadowUV.x < 1.0 && shadowUV.y < 1.0 && shadowUV.x > 0 && shadowUV.y > 0)
		{
			float shadowdepth = tex2D(ShadowMapSampler, shadowUV).r;

			// Check our value against the depth value
			float ourdepth = 1 - (shadowPos.z / shadowPos.w) + 0.05;

			// Check the shadowdepth against the depth of this pixel
			// a fudge factor is added to account for floating-point error
			PSIn.Color.r *= (exp(200 * (ourdepth - shadowdepth)) + 0.25f);
		}
	}
	*/
	Output.Color = tex2D(SunSampler, float2(PSIn.Color.r, (xTimeOfDay)));
	Output.Color.a *= PSIn.Color.a;
	Output.Color.rgb += tex2D(TorchSampler, float2(PSIn.Color.b,  0.5f)).rgb;
	Output.Color.rgb *= tex2D(AmbientSampler, float2(PSIn.Color.g, 0.5f)).rgb;
	
	//saturate(Output.Color.rgb);

	float2 textureCoords = ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds);
	float4 texColor = tex2D(TextureSampler, textureCoords);
	float4 illumColor = tex2D(IllumSampler, textureCoords);

	Output.Color.rgba *= texColor;
	Output.Color.rgb *= PSIn.ColorTint;

	Output.Color.rgba = lerp(Output.Color.rgba, texColor, SelfIllumination * illumColor.r);
	
	Output.Color.rgba = float4(lerp(Output.Color.rgb, xFogColor, PSIn.Fog) * Output.Color.a, Output.Color.a);

	clip((texColor.a - 0.5));

	clip(PSIn.ClipDistance.w);
    return Output;
}


TPixelToFrame TexturedPS(TVertexToPixel PSIn)
{
    TPixelToFrame Output = (TPixelToFrame)0;
	clip(PSIn.ClipDistance);  //MSS - Water Refactor added
    
	/*
	if (xEnableShadows)
	{
		float4 shadowPos = GetPositionFromLight(PSIn.WorldPosition);
			float2 shadowUV = 0.5 * shadowPos.xy / shadowPos.w + float2(0.5, 0.5);
			shadowUV.y = 1.0f - shadowUV.y;
		if (shadowUV.x < 1.0 && shadowUV.y < 1.0 && shadowUV.x > 0 && shadowUV.y > 0)
		{
			float shadowdepth = tex2D(ShadowMapSampler, shadowUV).r;

			// Check our value against the depth value
			float ourdepth = 1 - (shadowPos.z / shadowPos.w) + 0.05;

			// Check the shadowdepth against the depth of this pixel
			// a fudge factor is added to account for floating-point error
			PSIn.Color.r *= saturate(exp(100 * (ourdepth - shadowdepth)) + 0.25f);
		}
	}
	*/
	Output.Color = tex2D(SunSampler, float2(PSIn.Color.r, (xTimeOfDay)));
	Output.Color.rgb += tex2D(TorchSampler, float2(PSIn.Color.b, 0.5f)).rgb;
	saturate(Output.Color.rgb);

	Output.Color.rgb *=  tex2D(AmbientSampler, float2(PSIn.Color.g, 0.5f)).rgb;
	Output.Color.rgb *= PSIn.ColorTint;
	float4 texColor = tex2D(TextureSampler, ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds));
	float4 illumColor = tex2D(IllumSampler, ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds));

	Output.Color.rgba *= texColor;
	Output.Color.rgba = lerp(Output.Color.rgba, texColor, SelfIllumination * illumColor.r);
	
	Output.Color.rgba = float4(lerp(Output.Color.rgb, xFogColor, PSIn.Fog), Output.Color.a * PSIn.Color.a);
    return Output;
}

technique Selection
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 SelectionVSNonInstanced();
		PixelShader = compile ps_3_0 SelectionPS_Alphatest();
	}
};

technique Textured
{
    pass Pass0
    {   
        VertexShader = compile vs_3_0 TexturedVSNonInstanced();
        PixelShader  = compile ps_3_0 TexturedPS_Alphatest();
    }
}

technique Textured_Flag
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 TexturedVS_Flag();
		PixelShader = compile ps_2_0 TexturedPS_Alphatest();
	}
}

technique Silhouette
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 TexturedVSNonInstanced();
		PixelShader = compile ps_3_0 SilhouettePS();
	}
}

technique Textured_From_Lightmap
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 TexturedVS_From_Lightmap();
		PixelShader = compile ps_3_0 TexturedPS_From_Lightmap();
	}
}

technique Lightmap
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 TexturedVS_To_Lightmap();
		PixelShader = compile ps_3_0 TexturedPS_To_Lightmap();
	}
}

technique Textured_colorscale
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 TexturedVSNonInstanced();
		PixelShader = compile ps_3_0 TexturedPS_Colorscale();
	}
}

technique Instanced
{
    pass Pass0
    {   
        VertexShader = compile vs_3_0 TexturedVSInstanced();
		PixelShader = compile ps_3_0 TexturedPS_Alphatest();
    }
}

technique Instanced_SelectionBuffer
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 SelectionVSInstanced();
		PixelShader = compile ps_3_0 SelectionPS_Alphatest();
	}
}

//------- Technique: Water --------
struct WVertexToPixel
{
     float4 Position                 : POSITION;
	 float2 TextureSamplingPos        : TEXCOORD5;
     float4 ReflectionMapSamplingPos    : TEXCOORD1;
     float2 BumpMapSamplingPos        : TEXCOORD2;
     float4 RefractionMapSamplingPos : TEXCOORD3;
     float4 Position3D                : TEXCOORD4;
	 float2 UnMovedTextureSamplingPos : TEXCOORD6;
	 float4 Color : COLOR0;
	 float Fog : TEXCOORD7;
	 float ClipDistance : COLOR1;

};

struct WPixelToFrame
{
     float4 Color : COLOR0;
};

WVertexToPixel WaterVS(float4 inPos_ : POSITION, float2 inTex: TEXCOORD0, float4 inColor : COLOR0)
{    
	float4 inPos = inPos_;// +GetNoise(inPos_);
	WVertexToPixel Output = (WVertexToPixel)0;

	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
	float4x4 preReflectionViewProjection = mul (xReflectionView, xProjection);
	float4x4 preWorldReflectionViewProjection = mul (xWorld, preReflectionViewProjection);

	Output.Position = mul(inPos, preWorldViewProjection);

	Output.ReflectionMapSamplingPos = mul(inPos, preWorldReflectionViewProjection);
	Output.RefractionMapSamplingPos = mul(inPos, preWorldViewProjection);

	Output.Position3D = mul(inPos, xWorld);
	Output.BumpMapSamplingPos = inTex/xWaveLength;

	float st = sin(xTime * 0.2 + inTex.y * 0.1);
	float2 moveVector = xWindDirection.xz * xWindForce * xTime * 100;
	moveVector.x += st;
	moveVector.y += 1.0 - st;
	Output.BumpMapSamplingPos = moveVector + inTex;   
	Output.TextureSamplingPos = moveVector + inTex;
	Output.UnMovedTextureSamplingPos = inTex;
	Output.Color = inColor;
	Output.Fog = saturate((Output.Position.z - xFogStart) / (xFogEnd - xFogStart));
	Output.ClipDistance = Clipping * dot(Output.Position3D, ClipPlane0);

	return Output;
}

WPixelToFrame WaterPS(WVertexToPixel PSIn)
{
	WPixelToFrame Output = (WPixelToFrame)0;
	clip(PSIn.ClipDistance);
	Output.Color = PSIn.Color;

	float4 bumpColor = tex2D(WaterBumpMapSampler, PSIn.BumpMapSamplingPos);
	float2 perturbation = xWaveHeight * (bumpColor.rg - 0.5f) * 2.0f;

	float2 ProjectedTexCoords;
	ProjectedTexCoords.x = PSIn.ReflectionMapSamplingPos.x / PSIn.ReflectionMapSamplingPos.w / 2.0f + 0.5f;
	ProjectedTexCoords.y = -PSIn.ReflectionMapSamplingPos.y / PSIn.ReflectionMapSamplingPos.w / 2.0f + 0.5f;
	float2 perturbatedTexCoords = ProjectedTexCoords + perturbation;
	float4 reflectiveColor = tex2D(ReflectionSampler, perturbatedTexCoords);

	float3 eyeVector = normalize(xCamPos.xyz - PSIn.Position3D.xyz);

	float3 normalVector = float3(0, 1, 0);

	float fresnelTerm = abs(dot(eyeVector, normalVector));
	float4 dullColor = tex2D(WrappedTextureSampler, PSIn.TextureSamplingPos);
	Output.Color = dullColor;
	Output.Color = lerp(reflectiveColor, dullColor, fresnelTerm);
	Output.Color = lerp(dullColor, Output.Color, xWaterReflective);

	Output.Color.rgba = float4(lerp(Output.Color.rgb, xFogColor, PSIn.Fog) * Output.Color.a, Output.Color.a);

	float st = -xTime * 0.1 + lerp(perturbation.r / xWaveHeight, PSIn.Color.r, 0.8);
	Output.Color.rgb += (xRippleColor * (PSIn.Color.r * 1.5) * tex2D(ShoreSampler, st)).rgb;
	Output.Color.a = lerp(xWaterMinOpacity, xWaterOpacity, 1.0 - fresnelTerm);
	return Output;
}



WVertexToPixel WaterVS_Flat(float4 inPos : POSITION, float2 inTex : TEXCOORD0, float4 inColor : COLOR0)
{
	WVertexToPixel Output = (WVertexToPixel)0;

	float4x4 preViewProjection = mul(xView, xProjection);
	float4 pos3d = mul(inPos, xWorld);
	Output.Position = mul(pos3d, preViewProjection);
	Output.Color = xFlatColor;

	Output.ClipDistance = Clipping * dot(mul(xWorld, inPos), ClipPlane0);

	return Output;
}

WPixelToFrame WaterPS_Textured(WVertexToPixel PSIn)
{
	WPixelToFrame Output = (WPixelToFrame)0;
	clip(PSIn.ClipDistance);

	Output.Color = tex2D(WrappedTextureSampler, PSIn.TextureSamplingPos);

	float r = PSIn.Color.r;

	Output.Color.rgba = float4(lerp(Output.Color.rgb, xFogColor, PSIn.Fog) * Output.Color.a, Output.Color.a);
	float3 eyeVector = normalize(xCamPos.xyz - PSIn.Position3D.xyz);

	float3 normalVector = float3(0, 1, 0);

	float fresnelTerm = abs(dot(eyeVector, normalVector));
	float st = xTime * 0.1 + r;
	Output.Color.rgb += (xRippleColor * (r * 1.5) * tex2D(ShoreSampler, st)).rgb;
	//Output.Color.rgb = float3(fresnelTerm, 0, 0);
	Output.Color.a = lerp(xWaterMinOpacity, xWaterOpacity, 1.0 - fresnelTerm);
	return Output;
}

WVertexToPixel WaterVS_Textured(float4 inPos_ : POSITION, float2 inTex : TEXCOORD0, float4 inColor : COLOR0)
{
	float4 inPos = inPos_;  //+GetNoise(inPos_);
	WVertexToPixel Output = (WVertexToPixel)0;

	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	Output.Position = mul(inPos, preWorldViewProjection);
	Output.Position3D = mul(inPos, xWorld);

	float st = sin(xTime * 0.2 + inTex.y * 0.1);
	float2 moveVector = xWindDirection.xz * xWindForce * xTime * 1000;
	moveVector.x += st;
	moveVector.y += 1.0 - st;
	Output.TextureSamplingPos = moveVector + inTex;
	Output.Color = inColor;
	Output.Fog = saturate((Output.Position.z - xFogStart) / (xFogEnd - xFogStart));
	Output.ClipDistance = Clipping * dot(Output.Position3D, ClipPlane0);

	return Output;
}

WPixelToFrame WaterPS_Flat(WVertexToPixel PSIn)
{
	WPixelToFrame Output = (WPixelToFrame)0;
	Output.Color = PSIn.Color;
	return Output;
}

technique Water
{
     pass Pass0
     {
		 VertexShader = compile vs_3_0 WaterVS();
		 PixelShader = compile ps_3_0 WaterPS();

     }
}

technique WaterFlat
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 WaterVS_Flat();
		PixelShader = compile ps_3_0 WaterPS_Flat();

	}
}

technique WaterTextured
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 WaterVS_Textured();
		PixelShader = compile ps_3_0 WaterPS_Textured();

	}
}