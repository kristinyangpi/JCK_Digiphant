Shader "StudentWork/Animated Rainbow Road" {
 Properties {
  _ColorSpeed("Color cycle speed",Float)=0.055
  _Brightness("Rainbow brightness",Range(0,2))=1.15
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float2 localXZ:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial)
   float _ColorSpeed,_Brightness;
   CBUFFER_END
   float _FlowTime;
   float _RoundCornerCount;
   float4 _RoundCenters[24],_RoundVertices[24],_RoundNext[24];
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.localXZ=a.positionOS.xz;return o;}
   half4 frag(V i):SV_Target{
    float h=frac(i.uv.x*.83+i.uv.y*.018+_FlowTime*_ColorSpeed);
    float3 rgb=saturate(abs(frac(h+float3(0,2.0/3,1.0/3))*6-3)-1);
    rgb=lerp(rgb,float3(1,1,1),.11);
    float stripe=pow(saturate(.5+.5*sin((i.uv.x*7+_FlowTime*.11)*6.283)),14)*.11;
    float sheen=.045*sin(i.uv.y*2-_FlowTime*.7);
    float alpha=smoothstep(0,.028,min(i.uv.x,1-i.uv.x)*4.2);
    for(int c=0;c<(int)_RoundCornerCount;c++){
     float2 d=i.localXZ-_RoundVertices[c].xy;
     if(length(d)<_RoundCenters[c].w*2+_RoundCenters[c].z && dot(d,_RoundVertices[c].zw)<_RoundCenters[c].w && dot(d,_RoundNext[c].xy)<_RoundCenters[c].w){
      float inside=_RoundCenters[c].z-length(i.localXZ-_RoundCenters[c].xy);
      alpha*=smoothstep(-.015,.025,inside);
     }
    }
    return half4((rgb+stripe+sheen)*_Brightness,alpha);
   }
   ENDHLSL
  }
 }
}
