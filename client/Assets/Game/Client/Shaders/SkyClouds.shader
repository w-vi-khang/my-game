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

            // Băm toạ độ ô bằng số nguyên: GPU điện thoại (Mali) được sắp xếp lại phép tính số thực, nên băm bằng frac(p × hằng)
            // làm cùng một đỉnh lưới ra hai giá trị ở hai ô kề → lộ đường lưới. Phép nguyên chính xác trên mọi GPU.
            float HashInt(int2 c)
            {
                uint2 q = asuint(c) * uint2(1597334673u, 3812015801u);
                uint n = (q.x ^ q.y) * 1597334673u;
                n ^= n >> 16;
                n *= 2246822519u;
                n ^= n >> 13;
                return n * (1.0 / 4294967295.0);
            }

            // Ô có hash (dùng cho chấm/đốm): giữ tên Hash21 nhưng nhận toạ độ ô đã floor (số nguyên dạng float) + độ lệch hằng.
            float Hash21(float2 p)
            {
                return HashInt((int2)floor(p));
            }

            float ValueNoise(float2 p)
            {
                float2 fl = floor(p), f = p - fl;
                int2 i = (int2)fl;
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(HashInt(i), HashInt(i + int2(1, 0)), u.x),
                            lerp(HashInt(i + int2(0, 1)), HashInt(i + int2(1, 1)), u.x), u.y);
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
