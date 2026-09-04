using Eidren.Gameplay.Presentation;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Bringt das Abbau-Feedback des Kupfers zurueck. Der Stilumbau (E3b) hat
	/// CopperVein_Active.prefab ueber den T1ResourceVisualBuilder neu erzeugt;
	/// die alte Geometry_A14-Hierarchie trug eine CopperMiningVisualFeedback-
	/// Komponente, die dabei ersatzlos wegfiel (belegt im Kommentar von
	/// VisualAssetTests.CopperVeinUsesDetailedThreeDimensionalHeroGeometry).
	/// Der EditMode-Strukturtest wurde damals nachgezogen, der PlayMode-
	/// Verhaltenstest CopperMining_SynchronizesTwoVisibleImpactBursts nicht —
	/// er ist seitdem rot, und im Spiel fehlt jedes Schlag-Feedback am Kupfer.
	///
	/// Neuverdrahtung auf den Fabrik-Vertrag, ohne die Struktur anzutasten:
	/// die Wurzel wackelt (frueher: die OreCrown), die Erzsplitter
	/// (OreShard_00..04) sind Funkenpunkte und Flash-Ziele. Es kommen KEINE
	/// Renderer oder Collider ins Prefab — die Partikel entstehen zur
	/// Laufzeit; die Strukturtests (alle Renderer auf M_EidrenWorld_VertexLit,
	/// keine Collider) bleiben unberuehrt.
	///
	/// Idempotent: mehrfache Laeufe erzeugen denselben Endzustand.
	/// </summary>
	public static class CopperMiningFeedbackRebuilder
	{
		private const string PrefabPath =
			"Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Active.prefab";
		private const string StoneMaterialPath =
			"Assets/_Game/Art/World/Materials/M_CopperMining_Stone.mat";
		private const string SparkMaterialPath =
			"Assets/_Game/Art/World/Materials/M_CopperMining_Spark.mat";

		[MenuItem("Eidren/V0.2/Kupfer-Abbaufeedback wiederherstellen")]
		public static void Rebuild()
		{
			Material stein = EnsureUnlit(StoneMaterialPath, new Color(0.42f, 0.40f, 0.37f));
			Material funke = EnsureUnlit(SparkMaterialPath, new Color(1.00f, 0.58f, 0.16f));

			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				Transform[] splitter = root.GetComponentsInChildren<Transform>(true)
					.Where(t => t.name.StartsWith("OreShard_", StringComparison.Ordinal))
					.OrderBy(t => t.name, StringComparer.Ordinal)
					.ToArray();
				if (splitter.Length == 0)
				{
					// MIDPOLY-000: Das freigegebene V2-Modell fuehrt Kupfer und
					// Wirtsfels als getrennte Submeshes eines gemeinsamen Renderers.
					// Definierte ImpactPoints bewahren deshalb das Partikelfeedback,
					// ohne die Mid-Poly-Geometrie fuer einen Legacy-Namensvertrag zu
					// zerlegen. Alte Prefabs benutzen weiterhin OreShard_*.
					splitter = root.GetComponentsInChildren<Transform>(true)
						.Where(t => t.name.StartsWith("ImpactPoint_", StringComparison.Ordinal))
						.OrderBy(t => t.name, StringComparer.Ordinal)
						.ToArray();
				}
				if (splitter.Length == 0)
				{
					throw new InvalidOperationException("Keine Kupfer-Impactpunkte in " + PrefabPath + ".");
				}
				Renderer[] erzRenderer = root.GetComponentsInChildren<Renderer>(true)
					.Where(HasCopperMaterial)
					.ToArray();
				if (erzRenderer.Length == 0)
				{
					erzRenderer = splitter.Select(t => t.GetComponent<Renderer>()).Where(r => r != null).ToArray();
				}

				CopperMiningVisualFeedback feedback =
					root.GetComponent<CopperMiningVisualFeedback>();
				if (feedback == null)
				{
					feedback = root.AddComponent<CopperMiningVisualFeedback>();
				}
				feedback.Configure(root.transform, splitter, erzRenderer, stein, funke);

				PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			AssetDatabase.SaveAssets();
			Debug.Log("[K3D] Kupfer-Abbaufeedback wiederhergestellt: " + PrefabPath);
		}

		private static bool HasCopperMaterial(Renderer renderer)
		{
			return renderer != null && renderer.sharedMaterials.Any(material =>
				material != null && (material.name.IndexOf("Copper_Ore", StringComparison.OrdinalIgnoreCase) >= 0
					|| material.name.IndexOf("Kupfer", StringComparison.OrdinalIgnoreCase) >= 0));
		}

		private static Material EnsureUnlit(string pfad, Color farbe)
		{
			Shader shader = Shader.Find("Unlit/Color");
			if (shader == null)
			{
				throw new InvalidOperationException("Shader Unlit/Color fehlt.");
			}
			Material material = AssetDatabase.LoadAssetAtPath<Material>(pfad);
			if (material == null)
			{
				material = new Material(shader);
				AssetDatabase.CreateAsset(material, pfad);
			}
			material.shader = shader;
			material.color = farbe;
			return material;
		}
	}
}
