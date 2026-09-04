// G-002: Bodenmaterial mit Detail- und Makrolage.
// G-003: beleuchtet. Der Boden reagiert auf Richtungslicht, Schatten und das
// Trilight-Umgebungslicht der Zone; die Normale wird per Finite-Differenz aus
// der Koernung abgeleitet (EidrenGroundLighting.hlsl). Bei _NormalStrength 0
// und neutralem Licht entspricht das Ergebnis dem G-002-Stand.
//
// Drei Abtastebenen ueber eine UV-Menge:
//   Basis   - Tiling wie bisher (Zonengroesse / GroundTileWorldSize), Farbe und Charakter
//   Detail  - hoehere Frequenz, Mikrostruktur und Koernung
//   Makro   - niedrigere Frequenz, grossflaechige Variation gegen Kachelwiederholung
//
//   albedo = base * lerp(1, detail * 2, _DetailStrength) * (1 + (macro - 0.5) * 2 * _MacroStrength)
//   final  = albedo * (Sonne * N.L * Schatten + Umgebungslicht)
//
// Fehlt eine der beiden Zusatztexturen, ist die Ebene neutral: die Vorgabewerte
// sind "grey" (0.5), womit beide Terme exakt 1.0 ergeben. Der Shader ist damit
// auch auf Zonen anwendbar, die noch keine Detail- oder Makrotextur haben.
Shader "Eidren/Ground Detail"
{
    Properties
    {
        _BaseMap ("Base", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)

        // Die Vorgabewerte fuer die Kachelung sind bewusst keine ganzzahligen
        // Teiler von 1. Sonst laegen beide Zusatzlagen phasengleich auf der
        // Basiskachel und das 6-Einheiten-Raster bliebe sichtbar.
        //
        // _DetailScale 0,42 legt die Koernung in die Vergroesserung: 1024 Texel
        // auf 6/0,42 = 14,3 Welteinheiten sind 71,6 Texel je Einheit gegen rund
        // 80 Bildschirmpixel je Einheit. Ein Texel deckt damit etwa 1,1 Pixel
        // und das Mipmapping mittelt die Koernung nicht weg - genau daran sind
        // die Werte 2,7 und 1,05 gescheitert.
        _DetailMap ("Detail (Graustufen)", 2D) = "grey" {}
        _DetailScale ("Detail Kachelung", Float) = 0.42
        _DetailStrength ("Detail Staerke", Range(0,1)) = 0.6

        _MacroMap ("Makro (Graustufen)", 2D) = "grey" {}
        _MacroScale ("Makro Kachelung", Float) = 0.137
        _MacroStrength ("Makro Staerke", Range(0,1)) = 0.4

        // G-003: Staerke der aus der Koernung abgeleiteten Normale. 0 ist die
        // flache Ebene. Abgestimmt wird an den 1:1-Ausschnitten der Abnahme;
        // zu hohe Werte lassen den Boden nach Kies aussehen und brechen
        // G-002-Kriterium 6.
        _NormalStrength ("Relief Staerke", Range(0,2)) = 0.4

        [HideInInspector] _Surface ("__surface", Float) = 0
        [HideInInspector] _Cull ("__cull", Float) = 2
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _ZWrite ("__zw", Float) = 1
        [HideInInspector] _QueueOffset ("Queue offset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "Lit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            // G-003: dieselben Schattenvarianten wie Eidren/Actors/HandPaintedLitSprite,
            // damit Figuren- und Bodenschatten aus derselben Schattenkarte kommen.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "EidrenGroundLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  fogCoord   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);
            TEXTURE2D(_MacroMap);  SAMPLER(sampler_MacroMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _DetailMap_TexelSize;
                half4  _BaseColor;
                float  _DetailScale;
                half   _DetailStrength;
                float  _MacroScale;
                half   _MacroStrength;
                half   _NormalStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Detail- und Makro-UV leiten sich von der bereits gekachelten Basis-UV
                // ab, damit beide Ebenen der Zonengroesse folgen.
                float2 detailUv = input.uv * _DetailScale;
                half detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUv).r;
                half macro  = SAMPLE_TEXTURE2D(_MacroMap,  sampler_MacroMap,  input.uv * _MacroScale).r;

                half detailMul = lerp(1.0h, detail * 2.0h, _DetailStrength);
                half macroMul  = 1.0h + (macro - 0.5h) * 2.0h * _MacroStrength;

                half3 albedo = baseCol.rgb * detailMul * macroMul;

                // G-003: Relief aus der Koernung, Licht der Zone darauf.
                half3 normalWS = EidrenDeriveGroundNormal(
                    TEXTURE2D_ARGS(_DetailMap, sampler_DetailMap),
                    detailUv, _DetailMap_TexelSize.xy, _NormalStrength);
                half3 rgb = albedo * EidrenGroundLighting(input.positionWS, normalWS);

                rgb = MixFog(rgb, input.fogCoord);

                return half4(rgb, baseCol.a) * input.color;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
