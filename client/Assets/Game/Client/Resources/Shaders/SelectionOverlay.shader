// Nét khung chọn ô (TerrainEditInput): unlit, vẽ đè lên địa hình (ZTest Always) để khung không chìm trong mặt mesh mịn.
// URP Unlit không có thuộc tính ZTest nên cần shader riêng. Nằm trong Resources để bản build có shader cho Shader.Find.
Shader "Game/SelectionOverlay"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _BaseColor * input.color;
            }
            ENDHLSL
        }
    }
}
