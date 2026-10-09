Shader "StudentWork/Pastel Cotton Candy" {
 Properties {
  _BaseColor("Pastel candy color",Color)=(1,.7,.85,1)
  _BaseMap("Base",2D)="white"{}
  _Cull("Cull",Float)=2
  _Cutoff("Cutoff",Float)=.5
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;float4 _BaseMap_ST;float _Cull,_Cutoff;
   CBUFFER_END
   float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
   float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);}
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(a.normalOS);return o;}
   half4 frag(V i):SV_Target{
    float3 n=normalize(i.normalWS);Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    float diffuse=saturate(dot(n,light.direction));float rim=pow(1-saturate(dot(n,normalize(GetWorldSpaceViewDir(i.positionWS)))),2);
    float fibers=noise(i.positionWS*35)*.6+noise(i.positionWS*73)*.4;
    float3 candy=lerp(_BaseColor.rgb,1,rim*.28+fibers*.1);
    candy*=.68+.32*diffuse*lerp(.65,1,light.shadowAttenuation);
    return half4(candy+(fibers-.5)*.045,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
