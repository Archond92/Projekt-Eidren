// G-000 Nachtrag: Wiederhergestellt aus dem Dekompilat-Platzhalter.
//
// Der von AssetRipper erzeugte Platzhalter ("//DummyShaderTextExporter") war mit
// RenderType "Opaque" ausgezeichnet, schrieb in den Tiefenpuffer und gab
// _MainTex * _Color ohne jede Mischung zurueck. Der Bodenschatten wurde dadurch
// als deckende Platte gezeichnet statt als weiche getoente Flaeche.
//
// Der Originalquelltext ist nicht wiederherstellbar - Shader liegen im Build nur
// als uebersetzter Code vor. Diese Fassung erfuellt den nachweisbaren Vertrag:
// die beiden deklarierten Eigenschaften, der Vorgabewert des Farbtons
// (0.11, 0.15, 0.18, 0.38 - dunkles Blaugrau bei 38 % Deckung) und der Name
// "GroundShadow". Ueber die endgueltige Gestalt des Bodenschattens entscheidet
// G-004.

Shader "Eidren/Actors/GroundShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Shadow Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.11, 0.15, 0.18, 0.38)
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Lighting Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Bewusst ausserhalb von UnityPerMaterial: die Werte kommen ueber
            // MaterialPropertyBlock (SpriteActorPresentation), und ein
            // CBUFFER-Eintrag wuerde vom SRP-Batcher ueberschrieben.
            float4 _MainTex_ST;
            float4 _Color;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                output.color      = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 shadow = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Die Schattentextur traegt die Form in ihrem Alphakanal; die
                // Farbe stammt vollstaendig aus dem Farbton, damit der Schatten
                // sich nicht von der Figurentextur einfaerben laesst.
                half4 result;
                result.rgb = _Color.rgb;
                result.a   = shadow.a * _Color.a * input.color.a;
                return result;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
