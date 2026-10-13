// Bầu trời có mây (skybox, scene TerrainView): màu chuyển từ chân trời lên đỉnh, mây tạo bằng nhiễu fbm chiếu lên mặt
// phẳng trời và trôi chậm. Không cần texture. Màu chân trời trùng màu sương mù để biển tan vào trời (0009).
Shader "Game/SkyClouds"
{
    Properties
    {
        _ZenithColor("Zenith", Color) = (0.36, 0.62, 0.92, 1)
        _HorizonColor("Horizon", Color) = (0.78, 0.88, 0.97, 1)
        _CloudColor("Cloud", Color) = (1, 1, 1, 1)
        _CloudShadeColor("Cloud shade", Color) = (0.80, 0.85, 0.93, 1)
        _CloudCover("Cloud cover", Range(0, 1)) = 0.52
        _CloudSoftness("Cloud softness", Range(0.01, 0.5)) = 0.18
        _CloudScale("Cloud scale", Float) = 1.1
        _CloudSpeed("Cloud speed", Float) = 0.004
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor, _HorizonColor, _CloudColor, _CloudShadeColor;
                half _CloudCover, _CloudSoftness;
                float _CloudScale, _CloudSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.dir = input.positionOS.xyz;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), u.x),
                            lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float s = 0, a = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    s += a * ValueNoise(p);
                    p = p * 2.03 + 17.1;
                    a *= 0.5;
                }
                return s;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dir);
                float up = saturate(d.y);
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(up, 0.55));
                // Mây: chiếu hướng nhìn lên mặt phẳng trời (gần chân trời mây dày, nhỏ lại theo phối cảnh).
                float2 uv = d.xz / (d.y + 0.12) * _CloudScale + _Time.y * _CloudSpeed * float2(1, 0.4);
                float n = Fbm(uv);
                float cloud = smoothstep(_CloudCover, _CloudCover + _CloudSoftness, n) * smoothstep(0.0, 0.18, d.y);
                // Mặt dưới mây tối hơn ở chỗ mây dày.
                half3 cloudCol = lerp(_CloudColor.rgb, _CloudShadeColor.rgb, saturate((n - _CloudCover) * 2.5));
                half3 c = lerp(sky, cloudCol, cloud * 0.92);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
