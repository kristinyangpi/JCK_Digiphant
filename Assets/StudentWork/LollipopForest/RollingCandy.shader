Shader "StudentWork/Rolling Striped Candy" {
 Properties {
  _BaseColor("Candy pink",Color)=(1,.2,.51,1)
  _CreamColor("Candy cream",Color)=(1,.94,.97,1)
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
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct V {float4 positionCS:SV_POSITION;float3 local:TEXCOORD0;float3 positionWS:TEXCOORD1;float3 normalWS:TEXCOORD2;};
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor,_CreamColor;float4 _BaseMap_ST;float _Cull,_Cutoff;
   CBUFFER_END
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.local=a.positionOS.xyz;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(a.normalOS);return o;}
   half4 frag(V i):SV_Target {
    float wave=sin(atan2(i.local.z,i.local.x)*6+i.local.y*16);float aa=max(fwidth(wave),.01);
    float stripe=smoothstep(-aa,aa,wave);float3 candy=lerp(_BaseColor.rgb,_CreamColor.rgb,stripe);
    float3 n=normalize(i.normalWS),v=normalize(GetWorldSpaceViewDir(i.positionWS));Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    float lit=saturate(dot(n,light.direction));float gloss=pow(saturate(dot(n,normalize(light.direction+v))),64)*.28;
    float rim=pow(1-saturate(dot(n,v)),3)*.1;
    return half4(candy*(.58+.42*lit*light.shadowAttenuation)+gloss+rim,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
