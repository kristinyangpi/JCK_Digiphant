Shader "StudentWork/CandyWonderland/Pastel Sky" {
 Properties { _Top("Upper sky",Color)=(.52,.72,.94,1) _Horizon("Peach horizon",Color)=(1,.76,.8,1) _Bottom("Cloud ocean",Color)=(.82,.73,.92,1) }
 SubShader { Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 vertex:POSITION;}; struct V {float4 pos:SV_POSITION;float3 ray:TEXCOORD0;};
 float4 _Top,_Horizon,_Bottom;
 V vert(A v){V o;o.pos=TransformObjectToHClip(v.vertex.xyz);o.ray=v.vertex.xyz;return o;}
 half4 frag(V i):SV_Target{float3 d=normalize(i.ray);float3 c=lerp(_Horizon.rgb,_Top.rgb,smoothstep(0,.8,d.y));c=lerp(c,_Bottom.rgb,smoothstep(0,.65,-d.y));float sun=pow(saturate(dot(d,normalize(float3(-.4,.32,.7)))),80);c+=float3(1,.71,.35)*sun*.32;return half4(c,1);}
 ENDHLSL }
 }
}
