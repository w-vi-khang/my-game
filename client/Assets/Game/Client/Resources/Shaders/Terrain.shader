// Địa hình khối (decision 0011): màu lấy từ màu đỉnh do BlockMesher ghi theo vật liệu (RGB sRGB, đã nhân AO theo đỉnh);
// byte alpha = mã hoa văn (BlockMesher.Pattern*) → hoa văn tạo bằng code theo toạ độ thế giới trên mặt chính (bước 5.4,
// không cần texture). Khuếch tán bọc (wrap) cho chuyển sáng tối mềm, không specular/phản chiếu → nhám, không bóng.
// Ánh sáng mặt trời có bóng + ánh sáng môi trường (SH) + sương mù.
// Pass theo mẫu SimpleLit / ShadowCasterPass / DepthNormalsPass của URP 17.4. Nằm trong Resources để bản build có shader cho Shader.Find.
Shader "Game/Terrain"
{
    Properties
    {
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _AmbientStrength("Ambient", Range(0, 2)) = 1
        _Wrap("Wrap lighting", Range(0, 1)) = 0.35
        _PatternStrength("Pattern strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _AmbientStrength;
            half _Wrap;
            half _PatternStrength;
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                half4 col = input.color;
            #if !defined(UNITY_COLORSPACE_GAMMA)
                col.rgb = SRGBToLinear(col.rgb); // màu đỉnh ghi theo sRGB
            #endif
                o.color = half4(col.rgb * _BaseColor.rgb, col.a);
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

            /// Chấm tròn rải ngẫu nhiên: mỗi ô lưới có chấm với xác suất density, bán kính radius (đơn vị ô lưới).
            float Dots(float2 p, float density, float radius)
            {
                float2 i = floor(p), f = frac(p);
                float m = 0;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float h = Hash21(i + g);
                    if (h > density)
                        continue;
                    float2 o = float2(Hash21(i + g + 17.1), Hash21(i + g + 31.7)) * 0.6 + 0.2;
                    float d = length(g + o - f);
                    m = max(m, 1.0 - smoothstep(radius * 0.6, radius, d));
                }
                return m;
            }

            /// Hoa văn theo mã (BlockMesher.Pattern*): trả hệ số nhân màu.
            half3 Pattern(int id, float2 uv)
            {
                half k = 1.0;
                if (id == 1) // cỏ: loang lớn + loang nhỏ + đốm sáng thưa
                    k = lerp(0.9, 1.08, ValueNoise(uv * 1.3)) * lerp(0.96, 1.04, ValueNoise(uv * 5.0))
                      * lerp(1.0, 1.22, Dots(uv * 7.0, 0.15, 0.09));
                else if (id == 2) // đất: loang nhẹ + chấm tối (như vách đất khối cỏ tham khảo trong 0011)
                    k = lerp(0.93, 1.05, ValueNoise(uv * 1.7)) * lerp(1.0, 0.74, Dots(uv * 5.5, 0.35, 0.1));
                else if (id == 3) // đá: vết loang
                    k = lerp(0.84, 1.07, ValueNoise(uv * 2.5)) * lerp(0.95, 1.03, ValueNoise(uv * 9.0));
                else if (id == 4) // cát: hạt mịn + đốm sáng
                    k = lerp(0.94, 1.04, ValueNoise(uv * 9.0)) * lerp(1.0, 1.08, Dots(uv * 9.0, 0.2, 0.12));
                return k;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                // Toạ độ trên mặt chính theo pháp tuyến (mặt khối thẳng trục → không cần triplanar).
                float3 an = abs(n);
                float2 uv = an.y >= an.x && an.y >= an.z ? input.positionWS.xz
                          : an.x >= an.z ? input.positionWS.zy : input.positionWS.xy;
                int id = (int)round(input.color.a * 255.0);
                half3 albedo = input.color.rgb * lerp(1.0, Pattern(id, uv), _PatternStrength);

                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate((dot(n, light.direction) + _Wrap) / (1.0 + _Wrap));
                half3 direct = light.color * (ndl * light.shadowAttenuation * light.distanceAttenuation);
                half3 ambient = SampleSH(n) * _AmbientStrength;
                half3 c = albedo * (direct + ambient);
                c = MixFog(c, input.fogFactor);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                return ApplyShadowClamping(positionCS);
            }

            half4 frag() : SV_Target { return 0; }
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

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
            #if defined(_GBUFFER_NORMALS_OCT)
                float3 normalWS = normalize(input.normalWS);
                float2 oct = saturate(PackNormalOctQuadEncode(normalWS) * 0.5 + 0.5);
                return half4(PackFloat2To888(oct), 0.0);
            #else
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            #endif
            }
            ENDHLSL
        }
    }
}
