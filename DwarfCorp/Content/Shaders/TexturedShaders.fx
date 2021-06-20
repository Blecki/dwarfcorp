
#define MAX_LIGHTS 64
#define LIGHT_COLOR float4(0, 0, 1, 0)

float3 xLightPositions[MAX_LIGHTS];
int ActiveLights;

//------- Constants --------
float4x4 xView;
float4x4 xLightView;
float4x4 xLightProj;
float4x4 xReflectionView;

float xCaveView;

float xWaterOpacity;

float xWaterMinOpacity;

float4x4 xProjection;
float4x4 xWorld;
int xEnableLighting;
int xEnableShadows;
int xEnableWind;

Texture2D xWaterBumpMap;

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
Texture2D xTexture;
Texture2D xIllumination;

sampler IllumSampler = sampler_state { texture = <xIllumination> ;  magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = clamp; AddressV = clamp;};

sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = POINT; minfilter = LINEAR; mipfilter = Linear; AddressU = clamp; AddressV = clamp; };

sampler ColorscaleSampler = sampler_state { texture = <xTexture>; magfilter = POINT; minfilter = LINEAR; mipfilter = Linear; AddressU = clamp; AddressV = clamp; };

sampler WrappedTextureSampler = sampler_state { texture = <xTexture>; magfilter = POINT; minfilter = LINEAR; mipfilter = Linear; AddressU = wrap; AddressV = wrap; };

Texture2D xReflectionMap;
float xWaterReflective;

Texture2D xSunGradient;
Texture2D xAmbientGradient;
Texture2D xTorchGradient;

Texture2D xShoreGradient;
Texture2D xLightmap;
// Light ramp tint
float4 xLightRamp;
// Multiplicative output color.
float4 xVertexColorMultiplier;

sampler LightmapSampler = sampler_state { texture = <xLightmap>; magfilter = POINT; minfilter = LINEAR; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

sampler SunSampler = sampler_state { texture = <xSunGradient>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };

sampler AmbientSampler = sampler_state { texture = <xAmbientGradient>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };

sampler TorchSampler = sampler_state { texture = <xTorchGradient>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

sampler ShoreSampler = sampler_state { texture = <xShoreGradient>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = wrap; AddressV = clamp; };

sampler ReflectionSampler = sampler_state { texture = <xReflectionMap> ; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = clamp; AddressV = clamp;};

sampler WaterBumpMapSampler = sampler_state { texture = <xWaterBumpMap> ; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = wrap; AddressV = wrap;};

Texture2D xShadowMap;
sampler ShadowMapSampler = sampler_state { texture = <xShadowMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };


///////////// Technique untextured
	struct UTVertexToPixel
	{
	float4 Position     : POSITION0;
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
		Output.Color = inColor * xVertexColorMultiplier;

		Output.ClipDistance = Clipping * dot(mul(xWorld,inPos), ClipPlane0); //MSS - Water Refactor added

		return Output;
	}

    UTVertexToPixel UTexturedVS_Pulse( float4 inPos_ : POSITION,  float3 dir : NORMAL, float4 inColor : COLOR0)
	{
		UTVertexToPixel Output = (UTVertexToPixel)0;
		float3 backward = -float3(xView[0][2], xView[1][2], xView[2][2]);
		float3 offset = normalize(cross(dir, backward)) * inPos_.w;
		float4 inPos = float4(inPos_.xyz + offset * 0.5, 1);
		float4x4 preViewProjection = mul (xView, xProjection);
		float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);

		Output.Position = mul(inPos, preWorldViewProjection);
		Output.Color = inColor * xVertexColorMultiplier + float4(.2, .2, .2, 0) * pow(sin(xTime * 2 + 0.1 * inPos.x + 0.2 * inPos.z), 2);

		Output.ClipDistance = Clipping * dot(mul(xWorld,inPos), ClipPlane0); //MSS - Water Refactor added

		return Output;
	}

	UTPixelToFrame UTexturedPS(UTVertexToPixel PSIn)
	{	
	    UTPixelToFrame Output = (UTPixelToFrame)0;
		Output.Color = PSIn.Color;
		if (PSIn.ClipDistance.w < 0.0f)
 		{
 			Output.Color *= clamp(-1.0f / (PSIn.ClipDistance.w * 0.75f) * 0.25f, 0, 1.0f);
 
 			clip(GhostMode * (Output.Color.a) - 0.1f);
 		}
		return Output;
	}


	technique Untextured
	{
		pass Pass0
		{   
			VertexShader = compile vs_4_0 UTexturedVS();
			PixelShader = compile ps_4_0 UTexturedPS();
		}
	}

	technique Untextured_Pulse
	{
		pass Pass0
		{   
			VertexShader = compile vs_4_0 UTexturedVS_Pulse();
			PixelShader = compile ps_4_0 UTexturedPS();
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
		VertexShader = compile vs_4_0 ShadowMapVSNonInstance();
		PixelShader = compile ps_4_0 ShadowMapPixelShader();
	}
};

technique ShadowInstanced
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 ShadowMapVSInstance();
		PixelShader = compile ps_4_0 ShadowMapPixelShader();
	}
};


//------- Technique: Textured --------
struct TVertexToPixel
{
    float4 Position : SV_POSITION;
	float4 LightRamp         : COLOR0;
	float4 WorldPosition : TEXCOORD0;
	float2 TextureCoords : TEXCOORD1;
	float4 ClipDistance : TEXCOORD2;
	float Fog            : TEXCOORD3;	
	float4 TextureBounds : TEXCOORD4;
	float4 VertexColor     : COLOR1;
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


float4 Lighting(uniform int max_lights, float4 worldPosition, float4 color)
{
	for (int i = 0; i < max_lights; i++)
	{
		float3 dpos = worldPosition.xyz - xLightPositions[i];
		float dist = dot(dpos, dpos) + 0.001f;
		color = saturate(color + xEnableLighting * LIGHT_COLOR / dist);
	}
	color = saturate(color + (1.0 - xEnableLighting) * LIGHT_COLOR / 999.0f);
	return color;
}

float4 Lighting1Light(float4 worldPosition, float4 color)
{
	float3 dpos = worldPosition.xyz - xLightPositions[0];
	float dist = dot(dpos, dpos) + 0.001f;
	color = saturate(color + xEnableLighting * LIGHT_COLOR / dist + (1.0 - xEnableLighting) * LIGHT_COLOR / 999.0f);
	return color;
}

TVertexToPixel TexturedVS_Flag(float4 inPos : POSITION, 
							float2 inTexCoords : TEXCOORD0, 
							float4 inLightRamp : COLOR0, 
							float4 inTexSource : TEXCOORD1, 
							float3 vertColor : COLOR1,
							uniform int max_lights)
{
	TVertexToPixel Output = (TVertexToPixel)0;

	float4 worldPosition = mul(inPos, xWorld);

	worldPosition += GetFlagWind(worldPosition, inTexCoords, inTexSource);

	Output.WorldPosition = worldPosition;

	float4 viewPosition = mul(worldPosition, xView);
	Output.Position = mul(viewPosition, xProjection);

	Output.TextureCoords = inTexCoords;
	Output.ClipDistance = dot(worldPosition, ClipPlane0);
	Output.LightRamp = inLightRamp * xLightRamp;
    Output.LightRamp.a = xLightRamp.a;
	Output.VertexColor.rgb = vertColor * xVertexColorMultiplier.rgb;
    Output.LightRamp.a *= xVertexColorMultiplier.a;
    Output.LightRamp = Lighting(max_lights, worldPosition, Output.LightRamp);

	Output.Fog = saturate((Output.Position.z - xFogStart) / (xFogEnd - xFogStart));

	Output.TextureBounds = inTexSource;
	return Output;
}

TVertexToPixel TexturedVS(float4 inPos : POSITION,  
						   float2 inTexCoords: TEXCOORD0, 
						   float4 inLightRamp : COLOR0, 
						   float4 inTexSource : TEXCOORD1, 
						   float4x4 world : BLENDWEIGHT, 
						   float4 lightRampMultiplier : COLOR1, 
						   float4 tint,
						   uniform int max_lights)
{
    TVertexToPixel Output = (TVertexToPixel)0;

	float4 worldPosition = mul(inPos, world);

	worldPosition += xEnableWind * GetWind(worldPosition, inTexCoords, inTexSource);

	Output.WorldPosition = worldPosition;
    
	float4 viewPosition = mul(worldPosition, xView);
    Output.Position = mul(viewPosition, xProjection);
    
	Output.TextureCoords = inTexCoords;
    Output.ClipDistance = dot(worldPosition, ClipPlane0);
    Output.LightRamp = inLightRamp * lightRampMultiplier;
    Output.LightRamp.a = lightRampMultiplier.a;
	Output.VertexColor = tint * xVertexColorMultiplier;
    Output.LightRamp.a *= xVertexColorMultiplier.a;
    Output.LightRamp = Lighting(max_lights, worldPosition, Output.LightRamp);
	Output.Fog = saturate((Output.Position.z - xFogStart) / (xFogEnd - xFogStart));
	
	Output.TextureBounds = inTexSource;
    return Output;
}

TVertexToPixel TexturedVS_1Light(float4 inPos : POSITION,
	float2 inTexCoords : TEXCOORD0,
	float4 inLightRamp : COLOR0,
	float4 inTexSource : TEXCOORD1,
	float4x4 world : BLENDWEIGHT,
	float4 lightTint : COLOR1,
	float4 tint)
{
	TVertexToPixel Output = (TVertexToPixel)0;

	float4 worldPosition = mul(inPos, world);

	worldPosition += xEnableWind * GetWind(worldPosition, inTexCoords, inTexSource);

	Output.WorldPosition = worldPosition;

	float4 viewPosition = mul(worldPosition, xView);
    Output.Position = mul(viewPosition, xProjection);

	Output.TextureCoords = inTexCoords;
	Output.ClipDistance = dot(worldPosition, ClipPlane0);
    Output.LightRamp = inLightRamp * lightTint;
    Output.LightRamp.a = lightTint.a;
	Output.VertexColor = tint * xVertexColorMultiplier;
    Output.LightRamp.a *= xVertexColorMultiplier.a;
    Output.LightRamp = Lighting1Light(worldPosition, Output.LightRamp);
	Output.Fog = saturate((Output.Position.z - xFogStart) / (xFogEnd - xFogStart));

	Output.TextureBounds = inTexSource;
	return Output;
}


TVertexToPixel TexturedVS_To_Lightmap(float4 inPos : POSITION, 
							          float2 inTexCoords : TEXCOORD0, 
								      float2 inLightmapCoords : TEXCOORD2,
								      float4 inColor : COLOR0, 
								      float4 inTexSource : TEXCOORD1, 
								      float4 lightTint : COLOR1,
									  uniform int max_lights)
{
	TVertexToPixel Output = (TVertexToPixel)0;
	float4 worldPosition = mul(inPos, xWorld);
	Output.WorldPosition = worldPosition;
	Output.Position = float4(2 * inLightmapCoords.x - 1.0 - pixelSize.x * 0.5, -(2 * inLightmapCoords.y - 1.0 - pixelSize.y * 0.5), 0.0, 1);
	Output.TextureCoords = inTexCoords;
    Output.LightRamp = inColor * lightTint;
    Output.LightRamp.a = lightTint.a;
	Output.VertexColor = xLightRamp;

	for (int i = 0; i < max_lights; i++)
	{
		float dx = worldPosition.x - xLightPositions[i].x;
		float dy = worldPosition.y - xLightPositions[i].y;
		float dz = worldPosition.z - xLightPositions[i].z;
		float dist = pow(dx, 2) + pow(dy, 2) + pow(dz, 2) + 0.001f;
        Output.LightRamp = saturate(Output.LightRamp + xEnableLighting * LIGHT_COLOR / dist);
    }

    Output.LightRamp = saturate(Output.LightRamp + (1.0 - xEnableLighting) * LIGHT_COLOR / 999.0f);
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

	if (PSIn.ClipDistance.w < 0.0f)
	{
 		Output.Color *= -1.0f / (PSIn.ClipDistance.w * 0.75f) * 0.25f;
 		clip(GhostMode * (Output.Color.a) - 0.1f);
 	}
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

    Output.Color = tex2D(SunSampler, float2(PSIn.LightRamp.r, (xTimeOfDay)));
    Output.Color.a *= PSIn.LightRamp.a;
    Output.Color.rgb += tex2D(TorchSampler, float2(PSIn.LightRamp.b, 0.5f)).rgb;
    Output.Color.rgb *= tex2D(AmbientSampler, float2(PSIn.LightRamp.g, 0.5f)).rgb;
	
	saturate(Output.Color.rgb);
	float2 textureCoords = ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds);
	float4 texColor = tex2D(TextureSampler, textureCoords);
	float4 illumColor = tex2D(IllumSampler, textureCoords);

	Output.Color.rgba *= texColor;
	Output.Color.rgb *= PSIn.VertexColor.rgb;

	Output.Color.rgba = lerp(Output.Color.rgba, texColor, SelfIllumination * illumColor.r);
	return Output;
}


TVertexToPixel TexturedVSNonInstanced( float4 inPos : POSITION,  
									   float2 inTexCoords: TEXCOORD0, 
									   float4 lightRamp : COLOR0, 
									   float4 inTexSource : TEXCOORD1, 
									   float4 vertColor : COLOR1//,
									   //uniform int max_lights
									   )
{
    return TexturedVS(inPos, inTexCoords, lightRamp, inTexSource, xWorld, xLightRamp, vertColor, ActiveLights);// max_lights);
}

TVertexToPixel TexturedVSNonInstanced_1Light(float4 inPos : POSITION,
	float2 inTexCoords : TEXCOORD0,
	float4 inColor : COLOR0,
	float4 inTexSource : TEXCOORD1,
	float4 vertColor : COLOR1)
{
	return TexturedVS_1Light(inPos, inTexCoords, inColor, inTexSource, xWorld, xLightRamp, vertColor);
}

TVertexToPixel TexturedVSInstanced( float4 inPos : POSITION,  
								    float2 inTexCoords: TEXCOORD0, 
									float4 inLightRamp : COLOR3, 
									float4 inTexSource : TEXCOORD1, 
									float4 id : COLOR4, 
									float4x4 transform : BLENDWEIGHT, 
									float4 tint : COLOR5//,
									//uniform int max_lights
									)
{
    return TexturedVS(inPos, inTexCoords, inLightRamp, inTexSource, transpose(transform),
		              float4(1, 1, 1, tint.a), tint, ActiveLights);//max_lights);
}

TVertexToPixel TexturedVSTiledInstanced(float4 inPos : POSITION,
	float2 inTexCoords : TEXCOORD0,
	float4 inLightRamp : COLOR3,
	float4 inTexSource : TEXCOORD1,
	float4 id : COLOR4,
	float4x4 transform : BLENDWEIGHT,
	float4 vertexColor : COLOR5,
	float4 tileTexSource : TEXCOORD5//,
	//uniform int max_lights
	)
{
	float2 newTexCoord = inTexCoords * float2(tileTexSource.z - tileTexSource.x, tileTexSource.w - tileTexSource.y);
	newTexCoord += float2(tileTexSource.x, tileTexSource.y);
    return TexturedVS(inPos, newTexCoord, inLightRamp, tileTexSource, transpose(transform),
		float4(1, 1, 1, vertexColor.a), vertexColor, ActiveLights);//, max_lights);
}

TVertexToPixel TexturedVSInstanced_1Light(float4 inPos : POSITION,
	float2 inTexCoords : TEXCOORD0,
	float4 lightRamp : COLOR3,
	float4 inTexSource : TEXCOORD1,
	float4 id : COLOR4,
	float4x4 transform : BLENDWEIGHT,
	float4 vertexColor : COLOR5)
{
    return TexturedVS_1Light(inPos, inTexCoords, lightRamp, inTexSource, transpose(transform),
		float4(1, 1, 1, vertexColor.a), vertexColor);
}

TVertexToPixel TexturedVSTiledInstanced_1Light(float4 inPos : POSITION,
	float2 inTexCoords : TEXCOORD0,
	float4 lightRamp : COLOR3,
	float4 inTexSource : TEXCOORD1,
	float4 id : COLOR4,
	float4x4 transform : BLENDWEIGHT,
	float4 vertexColor : COLOR5,
	float4 tileTexSource : TEXCOORD5)
{
	float2 newTexCoord = inTexCoords * float2(tileTexSource.z - tileTexSource.x, tileTexSource.w - tileTexSource.y);
	newTexCoord += float2(tileTexSource.x, tileTexSource.y);
    return TexturedVS_1Light(inPos, newTexCoord, lightRamp, inTexSource, transpose(transform),
		float4(1, 1, 1, vertexColor.a), vertexColor);
}

SelectionBufferToPixel SelectionVSNonInstanced(float4 inPos : POSITION, float2 inTexCoords : TEXCOORD0)
{
	return  SelectionVS(inPos, inTexCoords, xWorld, xID);
}

SelectionBufferToPixel SelectionVSInstanced(float4 inPos : POSITION,
	float2 inTexCoords : TEXCOORD0,
	float4x4 transform : BLENDWEIGHT,
	float4 id : COLOR4)
{
	return SelectionVS(inPos, inTexCoords, transpose(transform) , id);
}

SelectionBufferToPixel SelectionVSTiledInstanced(float4 inPos : POSITION,
	float2 inTexCoords : TEXCOORD0,
	float4x4 transform : BLENDWEIGHT,
	float4 id : COLOR4,
	float4 tileTexSource : TEXCOORD5)
{
	float2 newTexCoord = inTexCoords * float2(tileTexSource.z - tileTexSource.x, tileTexSource.w - tileTexSource.y);
	newTexCoord += float2(tileTexSource.x, tileTexSource.y);
	return SelectionVS(inPos, newTexCoord, transpose(transform), id);
}


TPixelToFrame TexturedPS_Colorscale(TVertexToPixel PSIn)
{
	TPixelToFrame Output = (TPixelToFrame)0;

	Output.Color = tex2D(ColorscaleSampler, ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds));
    Output.Color.rgb *= tex2D(AmbientSampler, float2(PSIn.LightRamp.g, 0.5f)).rgb;
    Output.Color.rgb *= PSIn.VertexColor.rgb;
	clip(Output.Color.a - 0.5);
	clip(PSIn.ClipDistance.w);
	return Output;
}

TPixelToFrame SelectionPS_Alphatest(SelectionBufferToPixel PSIn)
{
	TPixelToFrame Output = (TPixelToFrame)0;

	float2 textureCoords = PSIn.TextureCoords;
	float4 texColor = tex2D(TextureSampler, textureCoords);

	clip((texColor.a - 0.5));

	clip(PSIn.ClipDistance.w + 1);
	Output.Color = PSIn.SelectionColor;
	return Output;
}

TPixelToFrame SilhouettePS(TVertexToPixel PSIn)
{
	TPixelToFrame Output = (TPixelToFrame)0;
	float2 textureCoords = ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds);
	float4 texColor = tex2D(TextureSampler, textureCoords);
	clip((texColor.a - 0.5));
	Output.Color.rgb = PSIn.VertexColor;
	Output.Color.a = 0.2;
	return Output;
}

// For drawing voxel icons
TPixelToFrame TexturedPS_Icon(TVertexToPixel PSIn)
{
    TPixelToFrame Output = (TPixelToFrame)0;
	float2 textureCoords = ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds);
	float4 texColor = tex2D(TextureSampler, textureCoords);
	clip((texColor.a - 0.5));
	Output.Color.rgba = texColor;
	float3 light1_dir = -normalize(float3(1.5, -1, 1));
	float3 light2_dir = -normalize(float3(-2, -1, -0.5));
	float3 normal = normalize(PSIn.WorldPosition.xyz);
	float3 diffuse1 = normalize(float3(0.5, 0.5, 0.4));
	float3 diffuse2 = normalize(float3(0.1, 0.1, 0.3)) * 0.5;
	diffuse1 *= clamp(dot(normal, light1_dir), 0, 1);
	diffuse2 *= clamp(dot(normal, light2_dir), 0, 1);
	float4 illumColor = tex2D(IllumSampler, textureCoords);
	Output.Color.rgb *= (diffuse1 + diffuse2 + float3(0.4, 0.42, 0.45));
	Output.Color.rgba = lerp(Output.Color.rgba, texColor, SelfIllumination * illumColor.r);	
    Output.Color.rgb *= xVertexColorMultiplier;
    return Output;
}


float4x4 stippleMatrix =
{
    1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
    13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
    4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
    16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
};


int xTextureWidth;
int xTextureHeight;
int xScreenWidth;
int xScreenHeight;


// ps_2_0 version of the main pixel shader... don't ask why this is needed.
TPixelToFrame TexturedPS2(TVertexToPixel PSIn)
{
    TPixelToFrame Output = (TPixelToFrame) 0;
    float2 textureCoords = ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds);
    float4 texColor = tex2D(TextureSampler, textureCoords);

    clip((texColor.a - 0.5));

    Output.Color = tex2D(SunSampler, float2(PSIn.LightRamp.r, (xTimeOfDay)));
    Output.Color.a *= PSIn.LightRamp.a;
        
    float alpha = smoothstep(0.0f, 1.0f, (1.0 - PSIn.LightRamp.r) * 1.25);
    Output.Color.a = lerp(Output.Color.a, alpha, xCaveView);
    Output.Color.rgb += tex2D(TorchSampler, float2(PSIn.LightRamp.b, 0.5f)).rgb;
    Output.Color.rgb *= tex2D(AmbientSampler, float2(PSIn.LightRamp.g, 0.5f)).rgb;

    float4 illumColor = tex2D(IllumSampler, textureCoords);

    Output.Color.rgba *= texColor;
    Output.Color.rgb *= PSIn.VertexColor;

    Output.Color.rgba = lerp(Output.Color.rgba, texColor, SelfIllumination * illumColor.r);
	
    Output.Color.rgba = float4(lerp(Output.Color.rgb, xFogColor, PSIn.Fog) * Output.Color.a, Output.Color.a);
    if (PSIn.ClipDistance.w - 0.5f * xCaveView < 0.0f)
    {
        Output.Color *= clamp(-1.0f / (PSIn.ClipDistance.w * 0.75f) * 0.25f, 0, 1);
 
        clip(GhostMode * (Output.Color.a) - 0.1f);
    }
    if (xCaveView * PSIn.LightRamp.r > 0.5)
    {
        Output.Color.rgb = float3(Output.Color.r, Output.Color.r, Output.Color.r);
    }
    return Output;
}


TPixelToFrame TexturedPS(TVertexToPixel PSIn)
{
        TPixelToFrame Output = (TPixelToFrame) 0;
        float2 textureCoords = ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds);
        float4 texColor = tex2D(TextureSampler, textureCoords);

        clip((texColor.a - 0.5));

        Output.Color = tex2D(SunSampler, float2(PSIn.LightRamp.r, (xTimeOfDay)));
        Output.Color.a *= PSIn.LightRamp.a;
        
        float alpha = smoothstep(0.0f, 1.0f, (1.0 - PSIn.LightRamp.r) * 1.25);
        float screenWidth = lerp(0, 1.0 - saturate(length(PSIn.Position.xy - float2(xScreenWidth * 0.5, xScreenHeight * 0.5)) / 700), xCaveView);
        Output.Color.a = lerp(Output.Color.a, alpha, screenWidth);
        Output.Color.rgb += tex2D(TorchSampler, float2(PSIn.LightRamp.b, 0.5f)).rgb;
        Output.Color.rgb *= tex2D(AmbientSampler, float2(PSIn.LightRamp.g, 0.5f)).rgb;

        float4 illumColor = tex2D(IllumSampler, textureCoords);

        Output.Color.rgba *= texColor;
        Output.Color.rgba *= PSIn.VertexColor.rgba;

        Output.Color.rgba = lerp(Output.Color.rgba, texColor, SelfIllumination * illumColor.r);
	
        Output.Color.rgba = float4(lerp(Output.Color.rgb, xFogColor, PSIn.Fog) * Output.Color.a, Output.Color.a);
        if (PSIn.ClipDistance.w - 0.5f * xCaveView < 0.0f)
        {
            Output.Color *= clamp(-1.0f / (PSIn.ClipDistance.w * 0.75f) * 0.25f, 0, 1);
 
            clip(GhostMode * (Output.Color.a) - 0.1f);
        }
        if (xCaveView * PSIn.LightRamp.r > 0.5)
        {
            Output.Color.rgb = float3(Output.Color.r, Output.Color.r, Output.Color.r);
        }
        return Output;
    }

    TPixelToFrame TexturedPS_Alphatest(TVertexToPixel PSIn)
    {
        TPixelToFrame Output = TexturedPS(PSIn);
        clip(Output.Color.a - stippleMatrix[PSIn.Position.x % 4][PSIn.Position.y % 4]);
        Output.Color.a = 1.0;
        return Output;
    }

    int transparencytable[4] =
    {
        0, 255,
255, 0
    };


    TPixelToFrame TexturedPS_Alphatest_Stipple(TVertexToPixel PSIn)
    {
        TPixelToFrame Output = TexturedPS_Alphatest(PSIn);
        int x = (int) (fmod(PSIn.Position.x, 2));
        int y = (int) (fmod(PSIn.Position.y, 2));
        clip(100 - transparencytable[x + y * 2]);
        return Output;
    }


    TPixelToFrame TexturedPS_Color(TVertexToPixel PSIn)
    {
        TPixelToFrame Output = (TPixelToFrame) 0;

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
        Output.Color = tex2D(SunSampler, float2(PSIn.LightRamp.r, (xTimeOfDay)));
        Output.Color.rgb += tex2D(TorchSampler, float2(PSIn.LightRamp.b, 0.5f)).rgb;
        saturate(Output.Color.rgb);

        Output.Color.rgb *= tex2D(AmbientSampler, float2(PSIn.LightRamp.g, 0.5f)).rgb;
        Output.Color.rgb *= PSIn.VertexColor;
        float4 texColor = tex2D(TextureSampler, ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds));
        float4 illumColor = tex2D(IllumSampler, ClampTexture(PSIn.TextureCoords, PSIn.TextureBounds));

        Output.Color.rgba *= texColor;
        Output.Color.rgba = lerp(Output.Color.rgba, texColor, SelfIllumination * illumColor.r);
	
        Output.Color.rgba = float4(lerp(Output.Color.rgb, xFogColor, PSIn.Fog), Output.Color.a * PSIn.LightRamp.a);

        if (PSIn.ClipDistance.w < 0.0f)
        {
            Output.Color *= clamp(-1.0f / (PSIn.ClipDistance.w * 0.75f) * 0.25f, 0, 1);
 
            clip(GhostMode * (Output.Color.a) - 0.1f);
        }

        return Output;
    }

technique Selection
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 SelectionVSNonInstanced();
		PixelShader = compile ps_4_0 SelectionPS_Alphatest();
	}
};

technique Textured
{
    pass Pass0
    {   
        VertexShader = compile vs_4_0 TexturedVSNonInstanced();//MAX_LIGHTS);
        PixelShader  = compile ps_4_0 TexturedPS_Alphatest();
    }
}

technique Textured_Stipple
{
    pass Pass0
    {   
        VertexShader = compile vs_4_0 TexturedVSNonInstanced();//1);
        PixelShader  = compile ps_4_0 TexturedPS_Alphatest_Stipple();
    }
}

technique Icon
{
    pass Pass0
    {   
        VertexShader = compile vs_4_0 TexturedVSNonInstanced();//MAX_LIGHTS);
        PixelShader  = compile ps_4_0 TexturedPS_Icon();
    }
}

technique Textured_1_Light
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced_1Light();
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}
/*
technique Textured_2_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(2);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_3_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(3);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_4_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(4);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_5_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(5);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_6_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(6);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_7_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(7);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_8_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(8);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_9_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(9);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_10_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(10);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_11_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(11);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_12_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(12);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_13_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(13);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_14_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(14);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_15_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(15);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Textured_16_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced(16);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}*/

technique Textured_Flag
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVS_Flag();//MAX_LIGHTS);
		PixelShader = compile ps_4_0 TexturedPS2();
	}
}

technique Silhouette
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced();//MAX_LIGHTS);
		PixelShader = compile ps_4_0 SilhouettePS();
	}
}

technique Textured_From_Lightmap
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVS_From_Lightmap();
		PixelShader = compile ps_4_0 TexturedPS_From_Lightmap();
	}
}

technique Lightmap
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVS_To_Lightmap();//MAX_LIGHTS);
		PixelShader = compile ps_4_0 TexturedPS_To_Lightmap();
	}
}

technique Textured_colorscale
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSNonInstanced();//MAX_LIGHTS);
		PixelShader = compile ps_4_0 TexturedPS_Colorscale();
	}
}

technique Instanced
{
    pass Pass0
    {   
		VertexShader = compile vs_4_0 TexturedVSInstanced();//MAX_LIGHTS);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
    }
}

technique Instanced_1_Light
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced_1Light();
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

/*

technique Instanced_2_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(2);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_3_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(3);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_4_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(4);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_5_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(5);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_6_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(6);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}


technique Instanced_7_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(7);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}


technique Instanced_8_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(8);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_9_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(9);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_10_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(10);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_11_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(11);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_12_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(12);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_13_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(13);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique Instanced_14_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(14);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}


technique Instanced_15_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(15);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}


technique Instanced_16_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSInstanced(16);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}*/

technique TiledInstanced
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced();//MAX_LIGHTS);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstancedSilhouette
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced();//MAX_LIGHTS);
		PixelShader = compile ps_4_0 SilhouettePS();
	}
}

technique TiledInstanced_1_Light
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced_1Light();
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}
/*
technique TiledInstanced_2_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(2);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_3_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(3);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_4_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(4);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_5_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(5);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_6_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(6);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}


technique TiledInstanced_7_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(7);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}


technique TiledInstanced_8_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(8);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_9_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(9);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_10_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(10);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_11_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(11);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_12_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(12);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_13_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(13);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}

technique TiledInstanced_14_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(14);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}


technique TiledInstanced_15_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(15);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}


technique TiledInstanced_16_Lights
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVSTiledInstanced(16);
		PixelShader = compile ps_4_0 TexturedPS_Alphatest();
	}
}*/


technique Instanced_SelectionBuffer
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 SelectionVSInstanced();
		PixelShader = compile ps_4_0 SelectionPS_Alphatest();
	}
}

technique TiledInstanced_SelectionBuffer
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 SelectionVSTiledInstanced();
		PixelShader = compile ps_4_0 SelectionPS_Alphatest();
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
		 VertexShader = compile vs_4_0 WaterVS();
		 PixelShader = compile ps_4_0 WaterPS();

     }
}

technique WaterFlat
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 WaterVS_Flat();
		PixelShader = compile ps_4_0 WaterPS_Flat();

	}
}

technique WaterTextured
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 WaterVS_Textured();
		PixelShader = compile ps_4_0 WaterPS_Textured();

	}
}