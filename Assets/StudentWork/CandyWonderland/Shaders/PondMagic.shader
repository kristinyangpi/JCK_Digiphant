Shader "StudentWork/CandyWonderland/Pond Magic" {
 Properties {_BaseColor("Tint",Color)=(1,1,1,1)}
 SubShader {Tags {"Queue"="Transparent+10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};half4 _BaseColor;
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.c=a.c*_BaseColor;o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target{float softness=smoothstep(0,.2,i.uv.y)*smoothstep(0,.2,1-i.uv.y);return half4(i.c.rgb*1.1,i.c.a*softness);}
 ENDHLSL}
 }
}
