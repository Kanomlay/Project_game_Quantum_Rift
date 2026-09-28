// สไปรต์มอนสเตอร์พร้อมเอฟเฟกต์ (ตั้งค่ารายตัวผ่าน MaterialPropertyBlock โดย MonsterFx)
// ต่อยอดจาก Sprite-Unlit-Default ของ URP 17: พลิกซ้ายขวา (flipX) สี/ความใสของ SpriteRenderer ทำงานเหมือนเดิม
// หน้าตาเท่ากับ Sprite-Lit-Default ตอนฉากไม่มีแสง 2D (GameScene ไม่มีแสง)
// - _FlashColor / _FlashAmount : ย้อมทั้งตัวเป็นสีทึบ (โดนตี = ขาว, ง้างโจมตี = ส้มอ่อน)
// - _Squash (xy = ยืด/ยุบรอบจุด pivot ของภาพ, zw = เลื่อนภาพ) : หายใจ เด้งตอนเดิน ยุบตอนโดนตี/ลงพื้น
//   ขยับแค่ภาพ ไม่แตะ transform จึงไม่กระทบ collider และระบบที่ย่อ/ขยายตัวตอนเกิด
// - _Dissolve : สลายทีละเม็ดพิกเซลของภาพตอนตาย ขอบที่กำลังจะหายเรืองสี _DissolveColor
Shader "QuantumRift/SpriteFX"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
        _Squash ("Squash (xy) Offset (zw)", Vector) = (1,1,0,0)
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _DissolveColor ("Dissolve Edge", Color) = (1,1,1,1)
        [HideInInspector] _PixelGrid ("Texture Size (xy)", Vector) = (64,64,0,0)

        // Legacy properties ให้ SpriteRenderer ตั้งค่าได้เหมือน shader สไปรต์ปกติ
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex FxVertex
            #pragma fragment FxFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FlashColor;
                half _FlashAmount;
                float4 _Squash;
                half _Dissolve;
                half4 _DissolveColor;
                float4 _PixelGrid; // ขนาดไฟล์ภาพ (MonsterFx ตั้งให้) ไม่ใช้ _MainTex_TexelSize เพราะ 2D SRP Batcher ไม่รองรับ
            CBUFFER_END

            Varyings FxVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                input.positionOS.xy = input.positionOS.xy * _Squash.xy + _Squash.zw;

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            float PixelNoise(float2 uv)
            {
                float2 texel = floor(uv * _PixelGrid.xy); // หนึ่งค่าต่อหนึ่งเม็ดพิกเซลของภาพ
                return frac(sin(dot(texel, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 FxFragment(Varyings input) : SV_Target
            {
                half4 color = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                color.rgb = lerp(color.rgb, _FlashColor.rgb, _FlashAmount);

                if (_Dissolve > 0.0h)
                {
                    float noise = PixelNoise(input.uv);
                    clip(noise - _Dissolve);
                    color.rgb = lerp(color.rgb, _DissolveColor.rgb, step(noise, _Dissolve + 0.1));
                }
                return color;
            }
            ENDHLSL
        }
    }
}
