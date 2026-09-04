Shader "Eidren/World/VertexLitFade"
{
	// F31-001: Fade-Zwilling von Eidren/World/VertexLit. Der Basisshader ist
	// bewusst opak und kennt weder Blend noch einen Alphakanal — die
	// Sichtlinien-Ausblendung (ActorOcclusionTransparency) tauscht Materialien
	// mit dem Weltshader zur Laufzeit auf diesen Zwilling und steuert die
	// Deckkraft ueber _Color.a. Kein ShadowCaster-/DepthOnly-Pass: eine
	// ausgeblendete Flaeche soll weder Tiefe schreiben noch voll schatten.
	Properties
	{
		_Tint ("Tint", Color) = (1, 1, 1, 1)
		_Color ("Fade", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
				float3 normalWS : TEXCOORD1;
				half4 color : COLOR;
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _Tint;
				half4 _Color;
			CBUFFER_END

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.color = input.color;
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half3 albedo = input.color.rgb * _Tint.rgb;
				float3 normalWS = normalize(input.normalWS);
				float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				Light mainLight = GetMainLight(shadowCoord);
				half3 licht = mainLight.color * mainLight.shadowAttenuation
					* saturate(dot(normalWS, mainLight.direction));
				licht += SampleSH(normalWS);
				#if defined(_ADDITIONAL_LIGHTS)
				uint count = GetAdditionalLightsCount();
				#if USE_CLUSTER_LIGHT_LOOP
				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
				#endif
				LIGHT_LOOP_BEGIN(count)
					Light zusatz = GetAdditionalLight(lightIndex, input.positionWS);
					licht += zusatz.color * zusatz.distanceAttenuation
						* saturate(dot(normalWS, zusatz.direction));
				LIGHT_LOOP_END
				#endif
				return half4(albedo * licht, _Color.a);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
