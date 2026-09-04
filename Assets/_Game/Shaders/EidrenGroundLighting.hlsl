// G-003: gemeinsame Beleuchtung fuer Eidren/Ground Detail und
// Eidren/Area Art Blend.
//
// Eine Datei fuer beide Shader, aus demselben Grund, aus dem G-002 beiden
// dieselben Kachelfaktoren gegeben hat: Basisboden und Blend-Flicken liegen
// im Bild direkt nebeneinander und muessen dieselbe Lichtantwort zeigen,
// sonst zeichnet sich jeder Flickenrand als Helligkeitsnaht ab. Da beide
// Flaechen eine UV-Einheit je 6 Welteinheiten tragen, tastet derselbe Code
// an derselben Weltposition denselben Detailtexel ab - das Relief ist an
// der Nahtstelle deckungsgleich.
//
// Annahmen, unter denen diese Datei gilt:
// - Die Flaeche ist horizontal (Bodenebene, Weltnormale nach +Y). Fuer
//   andere Flaechen ist die Normalenherleitung unten falsch.
// - Das Projekt rechnet im Gamma-Farbraum mit LDR-Grading. Die Summe aus
//   Sonne und Umgebungslicht ist darauf abgestimmt, auf ebener Flaeche nahe
//   1,0 zu liegen, damit der in G-002 abgenommene Boden seine Helligkeit
//   behaelt (Kalibrierung: ZoneLightingBuilder, Abnahme G-003).
//
// Kein Glanzanteil: die Welt ist gemalt, Formmodellierung entsteht allein
// aus Diffuslicht und Umgebungsanteil.

#ifndef EIDREN_GROUND_LIGHTING_INCLUDED
#define EIDREN_GROUND_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Leitet die Weltnormale per Finite-Differenz aus der Detail-Graustufenlage
// ab. Die Hoehe ist die Helligkeit der Koernung; eine eigene Hoehentextur
// gibt es bewusst nicht - das Relief soll exakt die Struktur beleuchten,
// die G-002 als Koernung aufgebracht hat, nicht eine zweite daneben.
//
// Die Makrolage bleibt aussen vor: ihre Wiederholung von 43,8 Welteinheiten
// wuerde grossflaechige Scheinhuegel auf spielerisch flachem Boden erzeugen.
// Sie ist Albedo-Variation und bleibt es.
//
// detailUv:   bereits mit _DetailScale multiplizierte UV
// texelSize:  _DetailMap_TexelSize.xy
// strength:   _NormalStrength; 0 ergibt exakt die flache Normale (0,1,0)
half3 EidrenDeriveGroundNormal(TEXTURE2D_PARAM(detailMap, samplerDetail),
                               float2 detailUv, float2 texelSize, half strength)
{
    half hC = SAMPLE_TEXTURE2D(detailMap, samplerDetail, detailUv).r;
    half hX = SAMPLE_TEXTURE2D(detailMap, samplerDetail, detailUv + float2(texelSize.x, 0)).r;
    half hY = SAMPLE_TEXTURE2D(detailMap, samplerDetail, detailUv + float2(0, texelSize.y)).r;

    // Hoehenfeld auf einer Y-hoch-Ebene: N = normalize(-dh/dx, 1, -dh/dz).
    // Ob UV.x im Weltraum nach +X oder -X laeuft, ist hier gleichgueltig:
    // die Koernung ist Rauschen ohne Oben und Unten, und weil Basisboden und
    // Flicken denselben Code auf denselben UVs ausfuehren, stimmen beide
    // Flaechen in jedem Fall ueberein.
    return normalize(half3(-(hX - hC) * strength, 1.0h, -(hY - hC) * strength));
}

// Diffuslicht plus Umgebungslicht. Gibt den Faktor zurueck, mit dem die
// Albedo multipliziert wird.
half3 EidrenGroundLighting(float3 positionWS, half3 normalWS)
{
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    Light mainLight = GetMainLight(shadowCoord);

    half ndl = saturate(dot(normalWS, mainLight.direction));
    half3 sun = mainLight.color * (ndl * mainLight.shadowAttenuation);

    // Trilight-Umgebungslicht der Zone, als SH abgetastet. Haengt an der
    // abgeleiteten Normale, damit auch Schattenseiten der Koernung nicht
    // schwarz fallen, sondern in die Boden-Ambientfarbe kippen.
    half3 ambient = SampleSH(normalWS);

    return sun + ambient;
}

#endif
