Shader "StudentWork/CandyWonderland/Silken Waterfall" {
 Properties {_BaseColor("Water tint",Color)=(.55,.92,1,.46) }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};float4 _BaseColor;
 V vert(A v){V o;o.p=TransformObjectToHClip(v.p.xyz);o.uv=v.uv;return o;}
 half4 frag(V i):SV_Target{float waves=sin(i.uv.x*91+sin(i.uv.y*11-_Time.y*2)*2)*.5+.5;float foam=pow(sin(i.uv.y*60+_Time.y*7+i.uv.x*18)*.5+.5,12);float edge=smoothstep(0,.05,i.uv.x)*smoothstep(0,.05,1-i.uv.x);return half4(lerp(_BaseColor.rgb,float3(.95,1,1),waves*.3+foam*.4),edge*(_BaseColor.a+foam*.25)*smoothstep(0,.13,1-i.uv.y));}
 ENDHLSL}
 }
}
