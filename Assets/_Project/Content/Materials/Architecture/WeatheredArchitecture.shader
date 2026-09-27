Shader "HorseParking/WeatheredArchitecture"
{
 Properties {
 _BaseMap("Original palette",2D)="white"{} _Wood("Wood",2D)="white"{} _Stone("Stone",2D)="white"{} _Scale("World texture scale",Float)=1.4
 _PaintColor("Painted siding",Color)=(0.82,0.76,0.60,1)
 _TimberColor("Sun bleached timber",Color)=(0.62,0.49,0.34,1)
 _RoofColor("Weathered roof",Color)=(0.27,0.34,0.36,1)
 _StoneColor("Foundation",Color)=(0.58,0.59,0.56,1)
 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass {
 Name "ForwardLit"
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 TEXTURE2D(_Wood); SAMPLER(sampler_Wood);
 TEXTURE2D(_Stone); SAMPLER(sampler_Stone);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST; float _Scale;
 half4 _PaintColor, _TimberColor, _RoofColor, _StoneColor;
 CBUFFER_END
 struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
 struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float fog:TEXCOORD3; };
 V vert(A i) { V o; VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz); o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=TransformObjectToWorldNormal(i.normalOS); o.uv=TRANSFORM_TEX(i.uv,_BaseMap); o.fog=ComputeFogFactor(p.positionCS.z); return o; }
 half4 frag(V i):SV_Target {
 float3 n=normalize(i.normalWS); float3 w=pow(abs(n),4); w/=max(dot(w,1),0.001); float3 p=i.positionWS*_Scale;
 half3 wood=SAMPLE_TEXTURE2D(_Wood,sampler_Wood,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_Wood,sampler_Wood,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_Wood,sampler_Wood,p.xy).rgb*w.z;
 half3 stone=SAMPLE_TEXTURE2D(_Stone,sampler_Stone,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_Stone,sampler_Stone,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_Stone,sampler_Stone,p.xy).rgb*w.z;
 half3 palette=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
 float blue=saturate((palette.b-palette.r)*6);
 float grey=1-saturate((max(palette.r,max(palette.g,palette.b))-min(palette.r,min(palette.g,palette.b)))*7);
 float luminance=dot(palette,half3(0.2126,0.7152,0.0722));
 float woodDetail=clamp(dot(wood,half3(0.2126,0.7152,0.0722))*1.3+0.7,0.72,1.12);
 float stoneDetail=clamp(dot(stone,half3(0.2126,0.7152,0.0722))*1.6+0.65,0.65,1.12);
 float siding=grey*smoothstep(0.18,0.5,luminance);
 float foundation=grey*(1-smoothstep(0.12,0.35,luminance));
 half3 albedo=lerp(_TimberColor.rgb,_PaintColor.rgb,siding)*woodDetail;
 albedo=lerp(albedo,_StoneColor.rgb*stoneDetail,foundation);
 float roofRibs=0.92+0.08*abs(sin((i.positionWS.x+i.positionWS.z)*24));
 albedo=lerp(albedo,_RoofColor.rgb*roofRibs*woodDetail,blue);
 Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
 half3 color=albedo*(max(SampleSH(n),half3(0.24,0.25,0.26))+light.color*saturate(dot(n,light.direction))*light.shadowAttenuation);
 return half4(MixFog(color,i.fog),1);
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
