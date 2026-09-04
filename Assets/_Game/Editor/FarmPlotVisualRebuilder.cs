using Eidren.Interaction;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Baut die drei Zustands-Visuals (leer/bepflanzt/erntereif) des
	/// Farmbeets in BLD_FarmPlot_L01 neu auf. Beim Umzug der Gebaeude auf
	/// die BLD_*_L01-Prefabs gingen die Visual-Zuweisungen des
	/// FarmPlotController verloren (alle drei Felder standen auf null,
	/// der Traeger "Visual_A20" fehlte ganz) — HomeBaseContentTests haelt
	/// den Vertrag fest. Aufbau nach dem CopperMiningFeedbackRebuilder-
	/// Muster: Primitive ohne Collider, Unlit/Color-Materialien.
	/// </summary>
	public static class FarmPlotVisualRebuilder
	{
		private const string PrefabPfad = "Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab";

		private const string MaterialOrdner = "Assets/_Game/Materials/Buildings";

		[MenuItem("Eidren/Art/FarmPlot-Visuals neu bauen")]
		public static void Bauen()
		{
			Material erde = EnsureMaterial("M_FarmPlot_Soil", new Color(0.36f, 0.27f, 0.19f));
			Material spross = EnsureMaterial("M_FarmPlot_Sprout", new Color(0.31f, 0.54f, 0.24f));
			Material frucht = EnsureMaterial("M_FarmPlot_Crop", new Color(0.50f, 0.64f, 0.24f));
			GameObject wurzel = PrefabUtility.LoadPrefabContents(PrefabPfad);
			try
			{
				FarmPlotController controller = wurzel.GetComponentInChildren<FarmPlotController>(includeInactive: true);
				if (controller == null)
				{
					throw new System.InvalidOperationException("BLD_FarmPlot_L01 traegt keinen FarmPlotController.");
				}
				Transform alt = controller.transform.Find("Visual_A20");
				if (alt != null)
				{
					Object.DestroyImmediate(alt.gameObject);
				}
				GameObject traeger = new GameObject("Visual_A20");
				traeger.transform.SetParent(controller.transform, worldPositionStays: false);
				GameObject leer = ZustandsGruppe(traeger.transform, "VIS_Empty", erde, spross, frucht, sprossen: 0, reife: false);
				GameObject bepflanzt = ZustandsGruppe(traeger.transform, "VIS_Planted", erde, spross, frucht, sprossen: 5, reife: false);
				GameObject reif = ZustandsGruppe(traeger.transform, "VIS_Ready", erde, spross, frucht, sprossen: 5, reife: true);
				bepflanzt.SetActive(value: false);
				reif.SetActive(value: false);
				SerializedObject serialisiert = new SerializedObject(controller);
				serialisiert.FindProperty("emptyVisual").objectReferenceValue = leer;
				serialisiert.FindProperty("plantedVisual").objectReferenceValue = bepflanzt;
				serialisiert.FindProperty("readyVisual").objectReferenceValue = reif;
				serialisiert.ApplyModifiedPropertiesWithoutUndo();
				PrefabUtility.SaveAsPrefabAsset(wurzel, PrefabPfad);
				Debug.Log("[FARM] Visual_A20 mit drei Zustands-Visuals neu gebaut und verdrahtet.");
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(wurzel);
			}
		}

		private static GameObject ZustandsGruppe(Transform eltern, string name, Material erde, Material spross, Material frucht, int sprossen, bool reife)
		{
			GameObject gruppe = new GameObject(name);
			gruppe.transform.SetParent(eltern, worldPositionStays: false);
			// Beet: flacher Quader unter dem 2x2-Footprint, leicht eingerueckt.
			Primitive(gruppe.transform, "Beet", erde, new Vector3(0f, 0.03f, 0f), new Vector3(1.8f, 0.06f, 1.8f));
			Vector2[] raster = new Vector2[5]
			{
				new Vector2(-0.55f, -0.55f),
				new Vector2(0.55f, -0.55f),
				new Vector2(-0.55f, 0.55f),
				new Vector2(0.55f, 0.55f),
				new Vector2(0f, 0f)
			};
			for (int i = 0; i < sprossen; i++)
			{
				if (reife)
				{
					Primitive(gruppe.transform, $"Pflanze_{i}", frucht, new Vector3(raster[i].x, 0.26f, raster[i].y), new Vector3(0.16f, 0.4f, 0.16f));
				}
				else
				{
					Primitive(gruppe.transform, $"Spross_{i}", spross, new Vector3(raster[i].x, 0.13f, raster[i].y), new Vector3(0.08f, 0.14f, 0.08f));
				}
			}
			return gruppe;
		}

		private static void Primitive(Transform eltern, string name, Material material, Vector3 position, Vector3 groesse)
		{
			GameObject objekt = GameObject.CreatePrimitive(PrimitiveType.Cube);
			objekt.name = name;
			// Farm-Visuals duerfen den mechanischen Collider nicht veraendern
			// (HomeBaseContentTests) — der Primitive-Collider fliegt sofort raus.
			Object.DestroyImmediate(objekt.GetComponent<Collider>());
			objekt.transform.SetParent(eltern, worldPositionStays: false);
			objekt.transform.localPosition = position;
			objekt.transform.localScale = groesse;
			objekt.GetComponent<MeshRenderer>().sharedMaterial = material;
		}

		private static Material EnsureMaterial(string name, Color farbe)
		{
			if (!AssetDatabase.IsValidFolder("Assets/_Game/Materials"))
			{
				AssetDatabase.CreateFolder("Assets/_Game", "Materials");
			}
			if (!AssetDatabase.IsValidFolder(MaterialOrdner))
			{
				AssetDatabase.CreateFolder("Assets/_Game/Materials", "Buildings");
			}
			string pfad = MaterialOrdner + "/" + name + ".mat";
			Material material = AssetDatabase.LoadAssetAtPath<Material>(pfad);
			if (material == null)
			{
				material = new Material(Shader.Find("Unlit/Color"));
				AssetDatabase.CreateAsset(material, pfad);
			}
			material.color = farbe;
			EditorUtility.SetDirty(material);
			return material;
		}
	}
}
