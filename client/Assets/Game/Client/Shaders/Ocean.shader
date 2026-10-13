// Nền đại dương (scene TerrainView, 0009): mặt phẳng lớn ngay dưới mặt nước, không phải khối nước thật.
// Gợn sóng bằng nhiễu theo toạ độ thế giới (pháp tuyến tính từ sai phân), màu sâu/nông theo góc nhìn, ánh mặt trời lấp lánh,
// nhận bóng của địa hình, sương mù để tan vào chân trời.
Shader "Game/Ocean"
{
    Properties
    {
        _DeepColor("Deep", Color) = (0.10, 0.45, 0.66, 1)
        _ShallowColor("Shallow", Color) = (0.32, 0.74, 0.80, 1)
        _RippleScale("Ripple scale", Float) = 0.45
        _RippleStrength("Ripple strength", Range(0, 1)) = 0.25
        _Glint("Sun glint", Range(0, 2)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _DeepColor, _ShallowColor;
            float _RippleScale;
            half _RippleStrength, _Glint;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
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

            float Waves(float2 p)
            {
                float t = _Time.y;
                return ValueNoise(p + t * float2(0.06, 0.03)) + 0.5 * ValueNoise(p * 2.3 - t * float2(0.05, 0.08));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.positionWS.xz * _RippleScale;
                const float e = 0.05;
                float h = Waves(uv);
                float dx = (Waves(uv + float2(e, 0)) - h) / e, dz = (Waves(uv + float2(0, e)) - h) / e;
                float3 n = normalize(float3(-dx * _RippleStrength, 1, -dz * _RippleStrength));

                float3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half fres = pow(1.0 - saturate(dot(v, n)), 3.0);
                half3 baseCol = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(0.25 + fres));

                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half shadow = light.shadowAttenuation * light.distanceAttenuation;
                half ndl = saturate((dot(n, light.direction) + 0.3) / 1.3);
                half3 diffuse = baseCol * (light.color * ndl * shadow * 0.7 + SampleSH(n));
                half spec = pow(saturate(dot(n, normalize(light.direction + v))), 160.0) * _Glint * shadow;
                half3 c = diffuse + light.color * spec;
                c = MixFog(c, input.fogFactor);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
