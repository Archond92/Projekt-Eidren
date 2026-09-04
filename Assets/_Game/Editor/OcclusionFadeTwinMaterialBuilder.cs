using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Verankert den Fade-Zwilling der
	/// Sichtlinien-Ausblendung als Resources-Material, damit Unity den
	/// Shader nicht aus dem Player-Build strippt.
	/// </summary>
	public static class OcclusionFadeTwinMaterialBuilder
	{
		private const string AssetPath = "Assets/_Game/Resources/Materials/OcclusionFadeTwin.mat";

		[MenuItem("Eidren/Data/Build Occlusion Fade Twin Material")]
		public static void Build()
		{
			Shader shader = Shader.Find("Eidren/World/VertexLitFade");
			if (shader == null)
			{
				throw new System.InvalidOperationException("Eidren/World/VertexLitFade fehlt.");
			}
			Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetPath);
			if (material == null)
			{
				material = new Material(shader) { name = "OcclusionFadeTwin" };
				AssetDatabase.CreateAsset(material, AssetPath);
			}
			else
			{
				material.shader = shader;
				EditorUtility.SetDirty(material);
			}
			// Doppelt verankert: Auch in die Always-Included-Shaderliste der
			// Grafikeinstellungen — das Resources-Material allein hat den
			// Shader nicht beweisbar ins Bundle gebracht.
			var graphics = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset");
			var serialized = new SerializedObject(graphics);
			SerializedProperty always = serialized.FindProperty("m_AlwaysIncludedShaders");
			bool vorhanden = false;
			for (int i = 0; i < always.arraySize; i++)
			{
				if (always.GetArrayElementAtIndex(i).objectReferenceValue == shader)
				{
					vorhanden = true;
					break;
				}
			}
			if (!vorhanden)
			{
				always.InsertArrayElementAtIndex(always.arraySize);
				always.GetArrayElementAtIndex(always.arraySize - 1).objectReferenceValue = shader;
				serialized.ApplyModifiedPropertiesWithoutUndo();
			}
			AssetDatabase.SaveAssets();
			Debug.Log("Eidren: occlusion fade twin anchored in Resources and AlwaysIncludedShaders.");
		}
	}
}
