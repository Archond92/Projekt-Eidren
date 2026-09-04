using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Stellt die gemeinsamen Stil-Assets der Weltobjekte bereit: den
	/// Vertexfarben-Shader und das eine geteilte Material. Dazu die
	/// unbeleuchtete Glut-Variante fuer selbstleuchtende Flaechen.
	/// </summary>
	public static class EidrenWorldStyleAssets
	{
		public const string ShaderName = "Eidren/World/VertexLit";

		public const string MaterialPath = "Assets/_Game/Art/World/Materials/M_EidrenWorld_VertexLit.mat";

		public const string GlowShaderName = "Eidren/World/VertexGlow";

		public const string GlowMaterialPath = "Assets/_Game/Art/World/Materials/M_EidrenWorld_VertexGlow.mat";

		public static Material EnsureWorldMaterial()
		{
			return Ensure(MaterialPath, ShaderName, "M_EidrenWorld_VertexLit");
		}

		/// <summary>
		/// Material fuer Glutflaechen: gibt die Vertexfarbe unbeleuchtet aus, damit
		/// Glut im fast schwarzen Verlies nicht abgedunkelt wird.
		/// </summary>
		public static Material EnsureGlowMaterial()
		{
			return Ensure(GlowMaterialPath, GlowShaderName, "M_EidrenWorld_VertexGlow");
		}

		private static Material Ensure(string pfad, string shaderName, string name)
		{
			Material material = AssetDatabase.LoadAssetAtPath<Material>(pfad);
			if (material == null)
			{
				string ordner = "Assets/_Game/Art/World/Materials";
				if (!AssetDatabase.IsValidFolder(ordner))
				{
					AssetDatabase.CreateFolder("Assets/_Game/Art/World", "Materials");
				}
				material = new Material(Shader.Find(shaderName)) { name = name };
				AssetDatabase.CreateAsset(material, pfad);
			}
			return material;
		}
	}
}
