Shader "Eidren/World/VertexGlow"
{
	// Unbeleuchtete Schwestervariante von Eidren/World/VertexLit fuer selbstleuchtende
	// Flaechen: Feueroeffnung der Esse, Glutadern, Kohlenpfannen, erstarrter Auslauf.
	// Gibt die Vertexfarbe unveraendert aus. Noetig, weil VertexLit die Albedo mit dem
	// Licht multipliziert - im Verlies mit fast schwarzem Umgebungslicht saeuft jede
	// Glutfarbe sonst ab. Das Projekt rendert in Gamma ohne Nachbearbeitung, ein
	// Emissionskanal steht nicht zur Verfuegung.
	// Meshes fuer dieses Material mit kontaktAo:false bauen - die eingebackene
	// Kontaktabdunklung wuerde die Glut sonst an den Raendern ausbleichen lassen.
	Properties
	{
		_Tint ("Tint", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

		Pass
		{
			Name "ForwardUnlit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

			CBUFFER_START(UnityPerMaterial)
				half4 _Tint;
			CBUFFER_END

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.color = input.color;
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return half4(input.color.rgb * _Tint.rgb, 1.0h);
			}
			ENDHLSL
		}

		// Bewusst ohne ShadowCaster: eine selbstleuchtende Flaeche soll keinen Schatten
		// werfen. Die Glutadern liegen flach im Boden, ein Schattenwurf waere dort falsch.
		UsePass "Universal Render Pipeline/Lit/DepthOnly"
	}
	FallBack Off
}
