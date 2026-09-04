using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class AreaArtVariantPrefabBuilder
{
	// Etappe 3b, Task 4 (BAKEKETTE-Befund, Abschnitt a): Wurzel und lokale Sequenz fuer die
	// Aeste der Baum-Gebietsidentitaet (Marsh/Quarry/EmberRuins). Vor dem T1-Umbau waren die
	// Aeste GameObject.CreatePrimitive(Cylinder) mit FirstMaterial(root) als Farbe -- nach dem
	// Umbau auf M_EidrenWorld_VertexLit liefert FirstMaterial deterministisch das geteilte
	// Vertexfarben-Material, das eingebaute Zylinder-Mesh traegt aber keinen Farbstream, also
	// wuerden die Aeste unbeabsichtigt hell/weisslich rendern. Ersetzt durch Fabrik-Geometrie
	// mit fester Rindenfarbe.
	private const string BranchMeshRoot = "Assets/_Game/Art/Zones/Meshes";

	private static readonly Color RindenFarbe = new Color(0.24f, 0.14f, 0.08f);

	private static int _astSequenz;

	// Muss vor jedem vollstaendigen Klon-Lauf (BuildTierOne/BuildAll) einmal aufgerufen
	// werden, damit die Ast_{NNN}-Nummerierung bei 0 beginnt und der Verwaisungsschutz in
	// PersistAst ueberzaehlige Assets aus einem frueheren Lauf mit mehr Aesten zuverlaessig
	// entfernt (mirror des Persist-Musters aus T1ResourceVisualBuilder).
	internal static void ResetBranchMeshSequence()
	{
		_astSequenz = 0;
	}

	internal static GameObject CloneVariant(AreaArtAssetBuilder.AreaSpec spec, string resourceId, string sourcePath, bool exhausted)
	{
		string key = spec.Key;
		if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
		{
			throw new InvalidOperationException("Missing variant source: " + sourcePath);
		}
		string state = (exhausted ? "Exhausted" : "Active");
		string safeResource = resourceId.Replace("resource.", string.Empty);
		string path = "Assets/_Game/Prefabs/Environment/AreaArtVariants/" + key + "_" + safeResource + "_" + state + ".prefab";
		GameObject root = PrefabUtility.LoadPrefabContents(sourcePath);
		root.name = key + "_" + safeResource + "_" + state;
		if (resourceId == "resource.tree")
		{
			AddTreeIdentity(root.transform, key, exhausted);
			AddRecognitionMarker(root.transform, exhausted);
		}
		else
		{
			AddLeadResourceIdentity(root.transform, key, resourceId, exhausted);
		}
		// G-004: Bodenerdung als Bauteil, nicht als Nachlauf.
		WorldContactShadowBuilder.Attach(root);
		GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
		PrefabUtility.UnloadPrefabContents(root);
		return saved;
	}

	private static void AddTreeIdentity(Transform root, string key, bool exhausted)
	{
		Transform old = root.Find("AreaIdentityGeometry");
		if (old != null)
		{
			UnityEngine.Object.DestroyImmediate(old.gameObject);
		}
		GameObject identity = new GameObject("AreaIdentityGeometry");
		identity.transform.SetParent(root, worldPositionStays: false);
		if (!exhausted)
		{
			switch (key)
			{
			case "Marsh":
				AddBranch(identity.transform, new Vector3(-0.2f, 3.7f, 0f), new Vector3(0.16f, 1.5f, 0.16f), new Vector3(0f, 0f, 58f), RindenFarbe);
				AddBranch(identity.transform, new Vector3(0.25f, 4.2f, 0.1f), new Vector3(0.13f, 1.25f, 0.13f), new Vector3(15f, 0f, -48f), RindenFarbe);
				break;
			case "Quarry":
				AddBranch(identity.transform, new Vector3(0f, 4.4f, 0f), new Vector3(0.28f, 1.4f, 0.28f), new Vector3(0f, 0f, -22f), RindenFarbe);
				break;
			case "EmberRuins":
				AddBranch(identity.transform, new Vector3(-0.15f, 4.5f, 0f), new Vector3(0.18f, 1.35f, 0.18f), new Vector3(0f, 0f, 32f), RindenFarbe);
				AddBranch(identity.transform, new Vector3(0.25f, 4.1f, 0f), new Vector3(0.14f, 1.2f, 0.14f), new Vector3(0f, 0f, -35f), RindenFarbe);
				break;
			}
		}
	}

	private static void AddLeadResourceIdentity(Transform root, string key, string resourceId, bool exhausted)
	{
		Transform old = root.Find("AreaIdentityGeometry");
		if (old != null)
		{
			UnityEngine.Object.DestroyImmediate(old.gameObject);
		}
		GameObject identity = new GameObject("AreaIdentityGeometry");
		identity.transform.SetParent(root, worldPositionStays: false);
		Color color = key switch
		{
			"Marsh" => new Color(0.24f, 0.42f, 0.31f), 
			"Quarry" => new Color(0.48f, 0.43f, 0.34f), 
			"EmberRuins" => new Color(0.58f, 0.22f, 0.09f), 
			_ => new Color(0.42f, 0.35f, 0.22f), 
		};
		string safeResource = resourceId.Replace("resource.", string.Empty);
		// F34-001: Aktiv und Erschoepft bekommen getrennte Materialien; vorher ueberschrieb der
		// Erschoepft-Lauf die Aktivfarbe im selben Asset (Faser-Schilf wurde abgedunkelt gerendert).
		Material material = EnsureColorMaterial(key.ToLowerInvariant() + "_" + safeResource + "_identity" + (exhausted ? "_exhausted" : string.Empty), exhausted ? (color * 0.55f) : color);
		if (resourceId == "resource.fiber_plant")
		{
			float height = (exhausted ? 0.12f : 0.34f);
			for (int i = -1; i <= 1; i++)
			{
				AddBox(identity.transform, "AreaReed", new Vector3((float)i * 0.11f, height * 0.52f, 0.02f), new Vector3(0.035f, height, 0.035f), new Vector3(0f, 0f, (float)i * 10f), material);
			}
		}
		else
		{
			float scale = (exhausted ? 0.13f : 0.34f);
			AddBox(identity.transform, "AreaShard", new Vector3((0f - scale) * 0.35f, scale * 0.52f, 0.02f), new Vector3(scale, scale, scale * 0.72f), new Vector3(8f, 24f, -10f), material);
			AddBox(identity.transform, "AreaShard", new Vector3(scale * 0.52f, scale * 0.35f, -0.03f), new Vector3(scale * 0.62f, scale * 0.65f, scale * 0.55f), new Vector3(-12f, -18f, 16f), material);
		}
	}

	private static void AddRecognitionMarker(Transform root, bool exhausted)
	{
		Transform old = root.Find("RecognitionMarker_AxeOchre");
		if (old != null)
		{
			UnityEngine.Object.DestroyImmediate(old.gameObject);
		}
		GameObject gameObject = new GameObject("RecognitionMarker_AxeOchre");
		gameObject.transform.SetParent(root, worldPositionStays: false);
		Material wood = EnsureColorMaterial("marker_axe_handle", new Color(0.28f, 0.13f, 0.05f));
		Material metal = EnsureColorMaterial("marker_axe_head", new Color(0.48f, 0.52f, 0.5f));
		Material cloth = EnsureColorMaterial("marker_ochre_cloth", new Color(0.72f, 0.42f, 0.08f));
		float factor = (exhausted ? 0.36f : 1f);
		Vector3 origin = (exhausted ? new Vector3(0.18f, 0.03f, 0f) : Vector3.zero);
		AddBox(gameObject.transform, "AxeHandle", origin + new Vector3(0.48f, 1.15f, -0.02f) * factor, new Vector3(0.07f, 0.85f, 0.07f) * factor, new Vector3(0f, 0f, -18f), wood);
		AddBox(gameObject.transform, "AxeHead", origin + new Vector3(0.36f, 1.56f, -0.02f) * factor, new Vector3(0.34f, 0.12f, 0.09f) * factor, new Vector3(0f, 0f, -18f), metal);
		AddBox(gameObject.transform, "OchreCloth", origin + new Vector3(0.58f, 1.02f, -0.04f) * factor, new Vector3(0.22f, 0.18f, 0.04f) * factor, Vector3.zero, cloth);
	}

	private static void AddBranch(Transform parent, Vector3 position, Vector3 scale, Vector3 rotation, Color rindenFarbe)
	{
		// Der eingebaute Zylinder hatte Basis-DURCHMESSER 1 (Halbbreite = 0.5*scale.x), aber
		// Basis-HOEHE 2 (Halbhoehe = 1*scale.y) -- nur die Y-Achse braucht die Verdopplung,
		// X/Z uebernehmen scale direkt (Fix nach Review-Befund Runde 1: fruehere Fassung
		// verdoppelte faelschlich alle drei Achsen und machte die Aeste doppelt so dick).
		Mesh mesh = PersistAst(EidrenMeshFactory.TaperedBox(new Vector3(scale.x, scale.y * 2f, scale.z), 0.7f, rindenFarbe));
		GameObject gameObject = new GameObject("SilhouetteBranch");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		// Der alte Zylinder-Primitive spannte sich um seinen Pivot herum (+-scale.y); die
		// Fabrik-Box sitzt mit der Basis auf y=0, daher der Versatz um -scale.y, um denselben
		// sichtbaren Bereich (Hoehenspanne) wie vorher zu behalten.
		gameObject.transform.localPosition = position - new Vector3(0f, scale.y, 0f);
		gameObject.transform.localEulerAngles = rotation;
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = EidrenWorldStyleAssets.EnsureWorldMaterial();
	}

	private static void AddBox(Transform parent, string name, Vector3 position, Vector3 scale, Vector3 rotation, Material material)
	{
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		gameObject.name = name;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = position;
		gameObject.transform.localScale = scale;
		gameObject.transform.localEulerAngles = rotation;
		gameObject.GetComponent<Renderer>().sharedMaterial = material;
		UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
	}

	// Persist mit Verwaisungsschutz (mirror T1ResourceVisualBuilder.Persist): Assets am
	// selben Sequenz-Index mit anderem Namen werden geloescht; ein vorhandenes Asset am
	// exakt gleichen Pfad wird per CopySerialized ueberschrieben, damit die GUID ueber
	// wiederholte BuildTierOne-Laeufe stabil bleibt.
	private static Mesh PersistAst(Mesh mesh)
	{
		if (!AssetDatabase.IsValidFolder(BranchMeshRoot))
		{
			AssetDatabase.CreateFolder("Assets/_Game/Art/Zones", "Meshes");
		}
		string pfad = string.Format("{0}/Ast_{1:000}_Ast.asset", BranchMeshRoot, _astSequenz);
		string muster = string.Format("Ast_{0:000}_", _astSequenz);
		// Unity erwartet bei nativen Einzelassets denselben Objektnamen wie im
		// Dateinamen. Der alte generische Fabrikname "TaperedBox" erzeugte bei
		// jedem Mid-Poly-Variantenlauf vermeidbare Importwarnungen.
		mesh.name = Path.GetFileNameWithoutExtension(pfad);
		_astSequenz++;
		foreach (string vorhanden in AssetDatabase.FindAssets("t:Mesh", new[] { BranchMeshRoot }))
		{
			string vorhandenerPfad = AssetDatabase.GUIDToAssetPath(vorhanden);
			if (Path.GetFileName(vorhandenerPfad).StartsWith(muster, StringComparison.Ordinal) && vorhandenerPfad != pfad)
			{
				AssetDatabase.DeleteAsset(vorhandenerPfad);
			}
		}
		Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(pfad);
		if (existing != null)
		{
			EditorUtility.CopySerialized(mesh, existing);
			UnityEngine.Object.DestroyImmediate(mesh);
			EditorUtility.SetDirty(existing);
			return existing;
		}
		AssetDatabase.CreateAsset(mesh, pfad);
		return mesh;
	}

	private static Material EnsureColorMaterial(string name, Color color)
	{
		string path = "Assets/_Game/Art/Zones/Materials/" + name + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material == null)
		{
			material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
			AssetDatabase.CreateAsset(material, path);
		}
		material.name = name;
		material.color = color;
		EditorUtility.SetDirty(material);
		return material;
	}
}
}
