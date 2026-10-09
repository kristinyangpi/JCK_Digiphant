Shader "StudentWork/Navy Galaxy Floor" {
 Properties { _TwinkleSpeed("Star twinkle speed",Float)=1 }
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
   struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;};
   CBUFFER_START(UnityPerMaterial)
   float _TwinkleSpeed;
   CBUFFER_END
   float _GalaxyTime;
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.positionWS=TransformObjectToWorld(a.positionOS.xyz);return o;}
   float3 stars(float2 p,float scale,float seed){
    float2 grid=p*scale,cell=floor(grid);float rnd=hash(cell+seed);
    float2 center=float2(hash(cell+seed+19),hash(cell+seed+57))*.72+.14;
    float2 delta=frac(grid)-center;float d=length(delta);
    float r=max(lerp(.018,.045,hash(cell+seed+91)),length(fwidth(grid))*.35);
    float core=exp(-d*d/(r*r)),halo=exp(-d*d/(r*r*7))*.15;
    float sparkle=(exp(-abs(delta.x)*110)*exp(-abs(delta.y)*15)+exp(-abs(delta.y)*110)*exp(-abs(delta.x)*15))*step(.98,rnd)*.7;
    float twinkle=.45+.55*(.5+.5*sin(_GalaxyTime*_TwinkleSpeed*(.7+hash(cell+seed+7)*1.2)+rnd*20));
    float3 color=lerp(float3(.65,.8,1),float3(1,.8,.96),hash(cell+seed+5));
    return color*(core+halo+sparkle)*twinkle*step(.68,rnd);
   }
   half4 frag(V i):SV_Target{
    float cloud=noise(i.positionWS.xz*.13)*noise(i.positionWS.xz*.065+13);
    float3 navy=float3(.003,.008,.027)+cloud*float3(.006,.003,.012);
    Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));navy*=lerp(.65,1,light.shadowAttenuation);
    float3 sparkle=stars(i.positionWS.xz,.9,0)+stars(i.positionWS.xz,1.7,123)*.4;
    return half4(navy+sparkle,1);
   }
   ENDHLSL
  }
 }
}
