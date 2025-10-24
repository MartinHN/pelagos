Shader "pelagos/VoronoiRay"
{
    Properties
    {
         _Color("Color", Color) = (1, 1, 1, 1)
         _CellDensity("CellDensity", float) = 100
         _intensityOffset("intensityOffset", float) = 0
        _speed("speed", float) = 1
        
        
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float _CellDensity;
            float _intensityOffset;
            float _speed;
 inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}

void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            float2 lattice = float2(x,y);
            float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;
            }
        }
    }
}

float2 cart2polar(float2 uv){
float phi=atan(uv.y/uv.x);
float r=length(uv);
return float2(phi,r);
}


          
            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                // float3 entryPoint = _WorldSpaceCameraPos;
                 float3 viewDir = worldPos - _WorldSpaceCameraPos;
                // float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float t = _Time[0]*_speed;
                //float n2=voronoi2d(IN.texcoord * 10000.0 +float2(t,t));
                float n2;
                float cell;
    
                Unity_Voronoi_float(cart2polar(rayDir.xz).x+_WorldSpaceCameraPos.xy/100 +float2(t,0),60,_CellDensity,n2,cell);
                float transmittance =n2*IN.texcoord.y+_intensityOffset;
                transmittance= transmittance*transmittance;
                                transmittance= transmittance*transmittance;
                //transmittance=0.0f;
                return col+ _Color*saturate(transmittance);
            }
            ENDHLSL
        }
    }
}