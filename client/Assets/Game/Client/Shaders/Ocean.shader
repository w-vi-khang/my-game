// Nền đại dương (scene TerrainView, 0009): mặt phẳng lớn ngay dưới mặt nước, không phải khối nước thật.
// Gợn sóng bằng nhiễu theo toạ độ thế giới (pháp tuyến tính từ sai phân), màu sâu/nông theo góc nhìn, ánh mặt trời lấp lánh,
// nhận bóng của địa hình, sương mù để tan vào chân trời.
// Brief 0011: material Water.mat bật trong suốt (Blend SrcAlpha/OneMinusSrcAlpha, ZWrite Off, Cull Off) — mặc định vẫn đục như cũ.
// Task m1-bien-nhuom-mau-troi (brief 0034): mặt trên nhuộm dần sang màu chân trời theo buổi; sương chuyển mềm (SoftFogFactor).
// Brief 0082: pass "WaterDepthPrepass" (SRPDefaultUnlit) ghi độ sâu trước ForwardLit — nước trong suốt chỉ hiện mặt gần nhất; Ocean.mat (đục) tắt pass này.
// Brief 0084: mặt nước nhạt + trong hơn khi mỏng (độ dày từ alpha màu đỉnh do BlockMesher ghi; _VertexDepth = 0 cho Ocean.mat).
// Brief 0095: màu nông→sâu theo smoothstep độ dày; viền bờ mờ theo khoảng cách tới bờ (kênh R màu đỉnh); tham số từ water_m1.json "look" (WaterLook).
// Brief 0096: bọt bờ — dải theo khoảng cách tới bờ (R), tắt ở chỗ hẹp theo độ thoáng (G màu đỉnh); _FoamStrength mặc định 0.
// Brief 0097: lấp lánh — quầng mặt trời (pháp tuyến phẳng) + chấm lấp lánh theo lưới ô, mờ theo khoảng cách; _GlintHalo, _SparkleStrength mặc định 0.
// Brief 0099: vệt chảy — hướng chảy (TEXCOORD0.xy từ FlowDirs) + cờ mặt thác (TEXCOORD0.z); sọc trôi theo hướng (2 pha), thác trôi xuống; _FlowStreakStrength mặc định 0.
// Brief 0100: bọt dùng khoảng cách tới bờ đã làm mềm (soft-min, kênh B màu đỉnh) → đường bọt trơn ở bờ bậc thang; viền bờ vẫn theo R.
Shader "Game/Ocean"
{
    Properties
    {
        _DeepColor("Deep", Color) = (0.10, 0.45, 0.66, 1)
        _ShallowColor("Shallow", Color) = (0.32, 0.74, 0.80, 1)
        _RippleScale("Ripple scale", Float) = 0.45
        _RippleStrength("Ripple strength", Range(0, 1)) = 0.25
        _Glint("Sun glint", Range(0, 2)) = 0.6
        _Alpha("Alpha (chỉ material trong suốt)", Range(0, 1)) = 1
        _VertexDepth("Độ dày từ màu đỉnh (1 = nước trong map, brief 0084)", Range(0, 1)) = 0
        _DepthFull("Độ dày coi là đầy (ô)", Float) = 1
        _ThinAlpha("Hệ số alpha khi nước rất mỏng", Range(0, 1)) = 0.25
        _ShoreSoftWidth("Độ rộng viền bờ mờ (ô, brief 0095)", Float) = 0.35
        _FoamColor("Foam color (brief 0096)", Color) = (1, 1, 1, 1)
        _FoamWidth("Foam width (cells)", Float) = 2
        _FoamBands("Foam bands", Float) = 3
        _FoamLine("Foam line", Float) = 0.3
        _FoamSoft("Foam soft", Float) = 0.15
        _FoamSpeed("Foam speed", Float) = 0.25
        _FoamSwing("Foam swing", Float) = 0.3
        _FoamSwingSpeed("Foam swing speed", Float) = 0.8
        _FoamNoiseScale("Foam noise scale", Float) = 1.2
        _FoamOpenMin("Foam open min", Float) = 0.2
        _FoamStrength("Foam strength", Float) = 0
        _GlintPower("Glint power (brief 0097)", Float) = 160
        _GlintHalo("Sun halo", Float) = 0
        _GlintHaloPower("Sun halo power", Float) = 300
        _SparkleStrength("Sparkle strength", Float) = 0
        _SparkleScale("Sparkle cells per m", Float) = 1.5
        _SparkleDensity("Sparkle density", Float) = 0.35
        _SparkleSize("Sparkle radius (cell)", Float) = 0.15
        _SparkleSpeed("Sparkle speed", Float) = 3
        _SparkleFadeStart("Sparkle fade start (m)", Float) = 20
        _SparkleFadeEnd("Sparkle fade end (m)", Float) = 60
        _FlowStreakColor("Flow streak color (brief 0099)", Color) = (1, 1, 1, 1)
        _FlowStreakStrength("Flow streak strength", Float) = 0
        _FlowStreakScale("Flow streak scale (per m)", Float) = 1.5
        _FlowStreakLength("Flow streak length (ratio)", Float) = 5
        _FlowStreakSpeed("Flow streak speed (cells/s)", Float) = 1.2
        _FlowStreakCycle("Flow streak cycle (s)", Float) = 2
        _FlowStreakCut("Flow streak cut", Float) = 0.55
        _FlowStreakSoft("Flow streak soft", Float) = 0.15
        _FlowFallSpeed("Fall streak speed (cells/s)", Float) = 3
        [HideInInspector] _SrcBlend("Src blend", Float) = 1
        [HideInInspector] _DstBlend("Dst blend", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
        [HideInInspector] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _DeepColor, _ShallowColor;
            float _RippleScale;
            half _RippleStrength, _Glint, _Alpha;
            half _VertexDepth, _ThinAlpha, _ShoreSoftWidth;
            float _DepthFull;
            half4 _FoamColor;
            half _FoamWidth, _FoamBands, _FoamLine, _FoamSoft, _FoamSpeed, _FoamSwing, _FoamSwingSpeed, _FoamNoiseScale, _FoamOpenMin, _FoamStrength;
            float _GlintPower, _GlintHaloPower, _SparkleScale, _SparkleFadeStart, _SparkleFadeEnd;
            half _GlintHalo, _SparkleStrength, _SparkleDensity, _SparkleSize, _SparkleSpeed;
            half4 _FlowStreakColor;
            float _FlowStreakScale, _FlowStreakLength, _FlowStreakSpeed, _FlowStreakCycle, _FlowFallSpeed;
            half _FlowStreakStrength, _FlowStreakCut, _FlowStreakSoft;
        CBUFFER_END
        ENDHLSL

        // Brief 0082: depth prepass — nước trong suốt chỉ hiện mặt gần camera nhất (không chồng mặt sau/mặt trong theo thứ tự tam giác).
        Pass
        {
            Name "WaterDepthPrepass"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return GetVertexPositionInputs(positionOS.xyz).positionCS; // cùng phép biến đổi với ForwardLit để độ sâu trùng khít
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; half4 flow : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half depthT : TEXCOORD1;
                half shoreD : TEXCOORD2;
                half2 open : TEXCOORD3; // x = độ thoáng (G, brief 0096), y = khoảng cách tới bờ cho bọt (B, brief 0100)
                half3 flow : TEXCOORD4;
            };

            static const float WaterDepthEncodeMax = 4.0; // = BlockMesher.WaterDepthEncodeMax
            static const float WaterShoreEncodeMax = 4.0; // = BlockMesher.WaterShoreEncodeMax

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.depthT = lerp(1.0h, saturate(input.color.a * WaterDepthEncodeMax / _DepthFull), _VertexDepth);
                o.shoreD = lerp(WaterShoreEncodeMax, input.color.r * WaterShoreEncodeMax, _VertexDepth);
                o.open = half2(lerp(1.0h, input.color.g, _VertexDepth), lerp(WaterShoreEncodeMax, input.color.b * WaterShoreEncodeMax, _VertexDepth));
                o.flow = input.flow.xyz * _VertexDepth; // hướng chảy + cờ mặt thác (brief 0099); Ocean.mat (_VertexDepth 0) và mesh có UV thường → 0
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

            // Bọt bờ (brief 0096): dải song song bờ theo khoảng cách tới bờ, chạy vào bờ + cả dải trôi vào/ra; đứt quãng theo nhiễu; tắt ở chỗ hẹp (độ thoáng thấp).
            half ShoreFoam(float2 xz, half d, half open)
            {
                half env = 1.0h - smoothstep(_FoamWidth * 0.5h, _FoamWidth, d);
                env *= smoothstep(_FoamOpenMin, _FoamOpenMin + 0.15h, open);
                float t = _Time.y;
                float swing = sin(t * _FoamSwingSpeed) * _FoamSwing;
                half b = frac((d + swing) * _FoamBands / _FoamWidth + t * _FoamSpeed);
                half stripe = smoothstep(0.0h, _FoamSoft, b) * (1.0h - smoothstep(_FoamLine, _FoamLine + _FoamSoft, b));
                stripe *= smoothstep(0.35h, 0.65h, ValueNoise(xz * _FoamNoiseScale + t * 0.05));
                return saturate(stripe * env * _FoamStrength);
            }

            // Chấm lấp lánh (brief 0097): lưới ô theo toạ độ thế giới (_SparkleScale ô/m); mỗi ô có chấm với xác suất _SparkleDensity,
            // tâm ngẫu nhiên cách mép ô ≥ bán kính (không cần xét ô kề), nhấp nháy theo pha riêng; mờ dần theo khoảng cách để chỗ xa không nhiễu.
            half Sparkle(float2 xz, float dist)
            {
                float2 p = xz * _SparkleScale;
                int2 cell = (int2)floor(p);
                half on = step(HashInt(cell), _SparkleDensity);
                float2 ctr = float2(HashInt(cell + int2(37, 11)), HashInt(cell + int2(-19, 53))) * (1.0 - 2.0 * _SparkleSize) + _SparkleSize;
                half r = length(p - floor(p) - ctr) / _SparkleSize;
                half dotV = 1.0h - smoothstep(0.25h, 1.0h, r);
                half tw = saturate(sin(_Time.y * _SparkleSpeed + HashInt(cell + int2(71, -29)) * 6.2831853));
                tw *= tw;
                tw *= tw;
                half fade = 1.0h - smoothstep(_SparkleFadeStart, _SparkleFadeEnd, dist);
                return on * dotV * tw * fade * _SparkleStrength;
            }

            // Vệt chảy (brief 0099): mặt trên — sọc dài theo hướng chảy f (TEXCOORD0), trôi theo f bằng 2 pha lệch nửa chu kỳ (kiểu flow map) để chỗ hướng đổi không bị kéo méo;
            // sọc dọc x và sọc dọc z trộn theo |f.x|, |f.y| nên không phải xoay toạ độ; ao/nguồn f = 0 → không vệt. Mặt thác (fall) — sọc dọc trôi xuống.
            half StreakNoise(float2 u, half2 w)
            {
                float l = _FlowStreakLength;
                return w.x * ValueNoise(float2(u.x / l, u.y)) + w.y * ValueNoise(float2(u.x, u.y / l) + float2(17.3, 41.7));
            }

            half FlowStreak(float3 pws, half2 f, half fall)
            {
                half top = 0.0h, side = 0.0h;
                half mag = saturate(length(f));
                if (mag > 0.01h)
                {
                    half2 w = abs(f) / (abs(f.x) + abs(f.y));
                    float ph = _Time.y / _FlowStreakCycle;
                    float p0 = frac(ph), p1 = frac(ph + 0.5);
                    float travel = _FlowStreakSpeed * _FlowStreakCycle;
                    half n0 = StreakNoise((pws.xz - f * (p0 * travel)) * _FlowStreakScale, w);
                    half n1 = StreakNoise((pws.xz - f * (p1 * travel)) * _FlowStreakScale, w);
                    half nb = lerp(n0, n1, abs(1.0 - 2.0 * p0));
                    top = smoothstep(_FlowStreakCut, _FlowStreakCut + _FlowStreakSoft, nb) * mag;
                }
                if (fall > 0.01h)
                {
                    float2 uf = float2((pws.x + pws.z) * _FlowStreakScale, (pws.y + _Time.y * _FlowFallSpeed) * _FlowStreakScale / _FlowStreakLength);
                    side = smoothstep(_FlowStreakCut, _FlowStreakCut + _FlowStreakSoft, ValueNoise(uf)) * fall;
                }
                return saturate((top + side) * _FlowStreakStrength);
            }

            // Buổi trong ngày (brief 0023/0033): DayCycleView ghi biến global; _DayHorizon là màu chân trời dạng linear (= màu sương).
            // _DayCycleOn = 0 (scene không có DayCycleView) → không nhuộm.
            half _DayCycleOn;
            half4 _DayHorizon;
            // Biển nhuộm màu trời (task m1-bien-nhuom-mau-troi): theo khoảng cách (m) và theo góc nhìn xiên (fresnel).
            static const float SkyTintStart = 32.0, SkyTintEnd = 120.0;
            static const half SkyTintFresnel = 0.5;

            // Sương chuyển mềm (task m1-bien-nhuom-mau-troi): hệ số sương tuyến tính nắn bằng smoothstep → độ dốc về 0 ở fogStart và fogEnd,
            // không còn gãy ở mép sương. Giữ giống hệt hàm cùng tên trong Terrain.shader.
            half SoftFogFactor(float dist)
            {
                half f = ComputeFogFactorZ0ToFar(dist);
                #if defined(FOG_LINEAR_KEYWORD_DECLARED)
                if (FOG_LINEAR)
                    f = f * f * (3.0 - 2.0 * f);
                #endif
                return f;
            }

            half4 frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float2 uv = input.positionWS.xz * _RippleScale;
                const float e = 0.05;
                float h = Waves(uv);
                float dx = (Waves(uv + float2(e, 0)) - h) / e, dz = (Waves(uv + float2(0, e)) - h) / e;
                float3 n = normalize(float3(-dx * _RippleStrength, 1, -dz * _RippleStrength));
                float3 nLit = n; // chiếu sáng luôn theo pháp tuyến hướng lên: mặt dưới sáng như mặt trên (brief 0011 sửa 1 — từ dưới nhìn lên không tối)
                n = IS_FRONT_VFACE(face, n, -n); // mặt dưới (camera dưới nước, Cull Off): pháp tuyến quay xuống — chỉ dùng cho fresnel và lấp lánh

                float3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half fres = pow(1.0 - saturate(dot(v, n)), 3.0);
                half dT = smoothstep(0.0h, 1.0h, input.depthT); // chuyển màu êm (brief 0095)
                half3 baseCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, dT);

                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half shadow = light.shadowAttenuation * light.distanceAttenuation;
                float dist = distance(input.positionWS, GetCameraPositionWS());
                half ndl = saturate((dot(nLit, light.direction) + 0.3) / 1.3);
                half3 lightTerm = light.color * ndl * shadow * 0.7 + SampleSH(nLit);
                half3 diffuse = baseCol * lightTerm;
                float3 hv = normalize(light.direction + v);
                half spec = pow(saturate(dot(n, hv)), _GlintPower) * _Glint * shadow;
                // Quầng mặt trời (brief 0097): pháp tuyến phẳng → đốm tròn mềm, liền; spec gợn ở trên cho đốm gắt nhỏ.
                float3 nFlat = IS_FRONT_VFACE(face, float3(0, 1, 0), float3(0, -1, 0));
                half halo = pow(saturate(dot(nFlat, hv)), _GlintHaloPower) * _GlintHalo * shadow;
                half sparkle = Sparkle(input.positionWS.xz, dist) * shadow * IS_FRONT_VFACE(face, 1.0h, 0.0h);
                half glow = spec + halo + sparkle;
                half3 c = diffuse + light.color * glow;
                half streak = FlowStreak(input.positionWS, input.flow.xy, input.flow.z);
                c = lerp(c, _FlowStreakColor.rgb * lightTerm, streak);
                half foam = ShoreFoam(input.positionWS.xz, input.open.y, input.open.x);
                c = lerp(c, _FoamColor.rgb * lightTerm, foam);
                // Biển nhuộm màu trời: chỉ mặt trên; xa và nhìn xiên → về màu chân trời theo buổi, trước sương (task m1-bien-nhuom-mau-troi).
                half skyT = saturate(smoothstep(SkyTintStart, SkyTintEnd, dist) + fres * SkyTintFresnel);
                c = lerp(c, _DayHorizon.rgb, skyT * _DayCycleOn * IS_FRONT_VFACE(face, 1.0, 0.0));
                // Sương theo khoảng cách thật (brief 0010 QĐ 11): far clip phẳng + màn hình rộng → mép nhìn xa hơn bán kính nạp.
                half fogF = SoftFogFactor(dist);
                c = MixFog(c, fogF);
                // Nước trong (brief 0011): nhìn xiên đục hơn (fresnel); xa đục dần theo sương → chỗ xa/sâu tan vào màu sương như trên cạn.
                half fogVis = 1.0; // độ rõ: 1 gần, 0 xa — chỉ tính với sương tuyến tính (scene dùng sương tuyến tính)
                #if defined(FOG_LINEAR_KEYWORD_DECLARED)
                if (FOG_LINEAR)
                    fogVis = fogF;
                #endif
                half alpha = lerp(1.0, lerp(_Alpha, 1.0, fres), fogVis);
                alpha *= lerp(_ThinAlpha, 1.0h, dT);
                alpha = lerp(alpha, 1.0h, saturate(glow)); // đốm sáng không bị mất ở chỗ nước mỏng (brief 0097)
                alpha = lerp(alpha, 1.0h, streak);
                alpha = lerp(alpha, 1.0h, foam);
                alpha *= smoothstep(0.0h, _ShoreSoftWidth, input.shoreD); // viền bờ mờ dần (brief 0095)
                return half4(c, alpha);
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
