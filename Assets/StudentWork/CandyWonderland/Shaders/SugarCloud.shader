Shader "StudentWork/CandyWonderland/Sugar Cloud" {
 Properties {_BaseColor("Pastel cloud",Color)=(1,.92,.95,1)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float3 world:TEXCOORD1;};float4 _BaseColor;
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.world=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);return o;}
 half4 frag(V i):SV_Target{float3 n=normalize(i.n);float facing=saturate(dot(n,normalize(GetWorldSpaceViewDir(i.world))));float light=saturate(n.y*.5+.5);float3 c=lerp(_BaseColor.rgb*float3(.85,.83,.97),_BaseColor.rgb,light);return half4(c,smoothstep(0,.55,facing)*.92);}
 ENDHLSL}
 }
}
