// G-000 Nachtrag: Wiederhergestellt aus dem Dekompilat-Platzhalter.
//
// Der von AssetRipper erzeugte Platzhalter ("//DummyShaderTextExporter") gab im
// Fragmentprogramm nur _MainTex * _Color zurueck. Von den fuenfzehn deklarierten
// Eigenschaften verwendete er zwei. Jede Figur im Spiel wurde dadurch
// unbeleuchtet gezeichnet, ohne Normalmap, ohne Emission, ohne Ruestungsebene
// und ohne Umriss.
//
// Der Originalquelltext ist nicht wiederherstellbar - Shader liegen im Build nur
// als uebersetzter Code vor. Diese Fassung bedient den nachweisbaren Vertrag:
// die Eigenschaften aus dem Platzhalter und die Werte, die
// SpriteActorPresentation.ApplyMaterialProperties() per MaterialPropertyBlock
// setzt (_NormalMap, _EmissionMap, _ArmorTex, _ArmorNormalMap,
// _ArmorEmissionMap, _ArmorParts, _ArmorTiers, _SpriteUvRect, _Color,
// _OutlinePixels, _AlphaClip, _EmissionStrength).
//
// NICHT belegt und daher bewusst nicht erfunden: die Bedeutung von _ArmorTiers.
// Der Wert wird deklariert und uebergeben, aber nicht ausgewertet - welche
// Stufenabstufung das Original daraus ableitete, ist aus dem Build nicht
// erkennbar. G-006 und G-007 muessen das festlegen.
//
// _ArmorParts wird ausgewertet, aber nur als Schalter: sind alle vier Teile
// null, bleibt die Ruestungsebene aus. Eine teilweise Ueberblendung einzelner
// Koerperpartien braeuchte eine Partiemaske, die es im Projekt nicht gibt.

Shader "Eidren/Actors/HandPaintedLitSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Albedo", 2D) = "white" {}
        [NoScaleOffset] _NormalMap ("Normal", 2D) = "bump" {}
        [NoScaleOffset] _EmissionMap ("Emission", 2D) = "black" {}
        [NoScaleOffset] _ArmorTex ("Armor Albedo", 2D) = "black" {}
        [NoScaleOffset] _ArmorNormalMap ("Armor Normal", 2D) = "bump" {}
        [NoScaleOffset] _ArmorEmissionMap ("Armor Emission", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline", Color) = (0.055,0.075,0.085,0.9)
        _OutlinePixels ("Outline Pixels", Range(0, 3)) = 1.25
        _AlphaClip ("Alpha Clip", Range(0, 0.5)) = 0.06
        _LightStrength ("Light Strength", Range(0, 2)) = 1
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.62
        _EmissionStrength ("Emission Strength", Range(0, 3)) = 1.15
        _ArmorParts ("Armor Parts", Vector) = (0,0,0,0)
        _ArmorTiers ("Armor Tiers", Vector) = (0,0,0,0)
        _SpriteUvRect ("Sprite UV Rect", Vector) = (0,0,1,1)
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
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS : TEXCOORD1;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);          SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);        SAMPLER(sampler_NormalMap);
            TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_ArmorTex);         SAMPLER(sampler_ArmorTex);
            TEXTURE2D(_ArmorNormalMap);   SAMPLER(sampler_ArmorNormalMap);
            TEXTURE2D(_ArmorEmissionMap); SAMPLER(sampler_ArmorEmissionMap);

            // Bewusst ausserhalb von UnityPerMaterial: alle Werte kommen ueber
            // MaterialPropertyBlock, ein CBUFFER-Eintrag wuerde vom SRP-Batcher
            // ueberschrieben.
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _OutlineColor;
            float4 _ArmorParts;
            float4 _ArmorTiers;
            float4 _SpriteUvRect;
            float  _OutlinePixels;
            float  _AlphaClip;
            float  _LightStrength;
            float  _ShadowStrength;
            float  _EmissionStrength;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                output.color      = input.color;
                return output;
            }

            // Alphawert an einer Nachbarposition, begrenzt auf das Rechteck des
            // aktuellen Einzelbildes. Ohne diese Begrenzung wuerde der Umriss
            // ueber die Kante hinaus in das nachbarliegende Bild des Atlas
            // greifen.
            float AlphaAt(float2 uv)
            {
                float2 min_ = _SpriteUvRect.xy;
                float2 max_ = _SpriteUvRect.xy + _SpriteUvRect.zw;
                if (any(uv < min_) || any(uv > max_))
                {
                    return 0.0;
                }
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;

                // Ruestungsebene: die Wanderer-Variante desselben Spritesheets,
                // bildgleich zur Basis. Sie wird ueber ihren eigenen Alphakanal
                // aufgelegt, sobald mindestens ein Teil ausgeruestet ist.
                float armorOn = saturate(_ArmorParts.x + _ArmorParts.y + _ArmorParts.z + _ArmorParts.w);
                if (armorOn > 0.0)
                {
                    half4 armorAlbedo = SAMPLE_TEXTURE2D(_ArmorTex, sampler_ArmorTex, input.uv);
                    half  cover = armorAlbedo.a * armorOn;
                    albedo.rgb = lerp(albedo.rgb, armorAlbedo.rgb, cover);
                    albedo.a   = max(albedo.a, cover);

                    half3 armorNormal = UnpackNormal(SAMPLE_TEXTURE2D(_ArmorNormalMap, sampler_ArmorNormalMap, input.uv));
                    normalTS = normalize(lerp(normalTS, armorNormal, cover));

                    half3 armorEmission = SAMPLE_TEXTURE2D(_ArmorEmissionMap, sampler_ArmorEmissionMap, input.uv).rgb;
                    emission = lerp(emission, armorEmission, cover);
                }

                albedo *= _Color * input.color;

                // Umriss: liegt der eigene Alphawert unter der Schwelle, aber ein
                // Nachbar darueber, wird die Umrissfarbe gezeichnet.
                float2 step_ = _MainTex_TexelSize.xy * _OutlinePixels;
                float neighbour =
                    max(max(AlphaAt(input.uv + float2( step_.x, 0)),
                            AlphaAt(input.uv + float2(-step_.x, 0))),
                        max(AlphaAt(input.uv + float2(0,  step_.y)),
                            AlphaAt(input.uv + float2(0, -step_.y))));

                if (albedo.a < _AlphaClip)
                {
                    if (neighbour < _AlphaClip || _OutlinePixels <= 0.0)
                    {
                        clip(-1);
                    }
                    return half4(_OutlineColor.rgb, _OutlineColor.a * input.color.a);
                }

                // Beleuchtung: Das Sprite ist eine zur Kamera gedrehte Flaeche.
                // Die Tangentenbasis wird deshalb aus den Objektachsen gebildet,
                // damit die Normalmap wie auf einer bemalten Karte wirkt.
                float3 tangentWS   = normalize(TransformObjectToWorldDir(float3(1, 0, 0)));
                float3 bitangentWS = normalize(TransformObjectToWorldDir(float3(0, 1, 0)));
                float3 faceWS      = normalize(TransformObjectToWorldDir(float3(0, 0, -1)));
                float3 normalWS    = normalize(normalTS.x * tangentWS + normalTS.y * bitangentWS + normalTS.z * faceWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float shade = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                half3 lighting = mainLight.color * (ndotl * shade * _LightStrength);
                half3 ambient  = SampleSH(normalWS);

                half3 color = albedo.rgb * (ambient + lighting) + emission * _EmissionStrength;
                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
