// G-001: Wiederhergestellt aus dem Dekompilat-Stub.
//
// Der von AssetRipper erzeugte Platzhalter ("//DummyShaderTextExporter") gab im
// Fragment-Programm die Konstante float4(1,1,1,1) zurueck und deklarierte weder
// einen Sampler fuer _BaseMap noch einen Blend-Zustand. Jede der 15 Flaechen mit
// diesem Shader wurde dadurch als deckend weisse Platte gezeichnet und verdeckte
// den darunterliegenden Boden.
//
// Diese Fassung fuehrt keine neue Gestaltung ein. Sie stellt genau das Verhalten
// her, das die vorhandenen Materialdaten bereits beschreiben:
//   _Surface 1, _SrcBlend 5 (SrcAlpha), _DstBlend 10 (OneMinusSrcAlpha),
//   _ZWrite 0, _Cull 2 (Back), _CustomRenderQueue 3000, _SURFACE_TYPE_TRANSPARENT.
// Die endgueltige Gestaltung der Bodenuebergaenge gehoert zu G-002.
Shader "Eidren/Area Art Blend"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)

        // G-002: dieselben Zusatzlagen wie Eidren/Ground Detail, mit denselben
        // Vorgabewerten. Der Flicken traegt die UV-Kachelung im Mesh
        // (AreaArtGroundBuilder.BuildFeatheredPatch: uv = u * width / 6), der
        // Basisboden ueber mainTextureScale = Groesse / 6. Beide ergeben eine
        // UV-Einheit je 6 Welteinheiten, weshalb dieselben Kachelfaktoren auf
        // beiden Flaechen dieselbe Weltfrequenz erzeugen.
        _DetailMap ("Detail (Graustufen)", 2D) = "grey" {}
        _DetailScale ("Detail Kachelung", Float) = 0.42
        _DetailStrength ("Detail Staerke", Range(0,1)) = 0.6

        _MacroMap ("Makro (Graustufen)", 2D) = "grey" {}
        _MacroScale ("Makro Kachelung", Float) = 0.137
        _MacroStrength ("Makro Staerke", Range(0,1)) = 0.4

        // G-003: identischer Wert wie auf Eidren/Ground Detail, sonst kippt
        // das Relief an der Flickenkante.
        _NormalStrength ("Relief Staerke", Range(0,2)) = 0.4

        [HideInInspector] _Surface ("__surface", Float) = 1
        [HideInInspector] _Blend ("__mode", Float) = 0
        [HideInInspector] _Cull ("__cull", Float) = 2
        [HideInInspector] _BlendOp ("__blendop", Float) = 0
        [HideInInspector] _SrcBlend ("__src", Float) = 5
        [HideInInspector] _DstBlend ("__dst", Float) = 10
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 10
        [HideInInspector] _ZWrite ("__zw", Float) = 0
        [HideInInspector] _QueueOffset ("Queue offset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "Lit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            // G-003: dieselben Schattenvarianten wie Eidren/Ground Detail. Die
            // Flicken liegen auf dem Basisboden; faellt ein Schatten auf die
            // Kante, muessen beide Flaechen ihn gleich zeichnen.
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

                // G-002: Die Vertexfarbe traegt die Randausblendung des Flickens.
                // AreaArtGroundBuilder.BuildFeatheredPatch schreibt sie in das Mesh;
                // ohne diese Multiplikation wird sie verworfen und der Flicken deckt
                // den Basisboden vollstaendig zu (siehe G002_ENTWURF.md, Abschnitt 1.2).
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor * input.color;

                // G-002: Koernung und Grossflaechenvariation wie auf dem Basisboden.
                // Ohne sie blieben die Flicken die einzigen strukturlosen Flaechen im
                // Bild - und da die Erdflaechen ausschliesslich Flicken sind, wurde
                // Abnahmekriterium 1 genau dort verfehlt.
                //
                // Nur rgb wird moduliert. Alpha traegt die Randausblendung; eine
                // Koernung darauf wuerde den weichen Rand aufrauhen.
                float2 detailUv = input.uv * _DetailScale;
                half detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUv).r;
                half macro  = SAMPLE_TEXTURE2D(_MacroMap,  sampler_MacroMap,  input.uv * _MacroScale).r;
                color.rgb *= lerp(1.0h, detail * 2.0h, _DetailStrength)
                           * (1.0h + (macro - 0.5h) * 2.0h * _MacroStrength);

                // G-003: identische Lichtrechnung wie Eidren/Ground Detail, auf
                // denselben UVs - das Relief ist an der Flickenkante deckungsgleich
                // und es entsteht keine Helligkeitsnaht (EidrenGroundLighting.hlsl).
                half3 normalWS = EidrenDeriveGroundNormal(
                    TEXTURE2D_ARGS(_DetailMap, sampler_DetailMap),
                    detailUv, _DetailMap_TexelSize.xy, _NormalStrength);
                color.rgb *= EidrenGroundLighting(input.positionWS, normalWS);

                color.rgb = MixFog(color.rgb, input.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
