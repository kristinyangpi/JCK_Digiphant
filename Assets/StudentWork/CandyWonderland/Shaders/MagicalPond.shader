Shader "StudentWork/CandyWonderland/Magical Pond" {
 Properties {_BaseColor("Water",Color)=(.25,.83,.9,.7)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};float4 _BaseColor;
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target{float2 uv=(i.uv-.5)*2;float r=length(uv);float waves=sin(r*42-_Time.y*2+sin(uv.x*12+_Time.y)*.5)*.5+.5;float gleam=pow(sin(uv.x*35+uv.y*41+_Time.y*1.8)*.5+.5,24);float edge=1-smoothstep(.89,1,r);return half4(lerp(_BaseColor.rgb,half3(.91,1,1),waves*.18+gleam*.28),_BaseColor.a*edge);}
 ENDHLSL}
 }
}
