using Eidren.Presentation;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class ForgeContainerVisualBuilder
{
	private readonly struct Spec
	{
		public string Name { get; }

		public Vector3 Size { get; }

		public Color Body { get; }

		public Color Accent { get; }

		public int Bands { get; }

		public int Crest { get; }

		public bool Locker { get; }

		public Spec(string name, Vector3 size, Color body, Color accent, int bands, int crest, bool locker = false)
		{
			Name = name;
			Size = size;
			Body = body;
			Accent = accent;
			Bands = bands;
			Crest = crest;
			Locker = locker;
		}
	}

	private const string MeshRoot = "Assets/_Game/Art/Containers/Forge/Meshes";

	private static int _meshSequence;

	[MenuItem("Eidren/Art/Build Forge 3D Containers")]
	public static void Build()
	{
		EnsureFolders();
		_meshSequence = 0;
		foreach (Spec item in Specs())
		{
			// Freigegebene Phase-4-Prefabs duerfen durch den historischen
			// Low-Poly-Builder nicht wieder ueberschrieben werden.
			if (MidpolyForgeContainerMigration.IsApprovedContainer(item.Name))
				continue;
			BuildPrefab(item);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built eight unique forge container families.");
	}

	private static IEnumerable<Spec> Specs()
	{
		Color wood = new Color(0.2f, 0.105f, 0.055f);
		Color iron = new Color(0.105f, 0.115f, 0.13f);
		Color ember = new Color(0.88f, 0.31f, 0.075f);
		yield return new Spec("SupplyChest", new Vector3(1.18f, 0.8f, 0.78f), wood, new Color(0.43f, 0.29f, 0.17f), 2, 0);
		yield return new Spec("OptionalChest", new Vector3(1.25f, 0.8f, 0.82f), new Color(0.15f, 0.11f, 0.08f), new Color(0.63f, 0.34f, 0.12f), 3, 1);
		yield return new Spec("EliteChest", new Vector3(1.42f, 1f, 0.92f), iron, new Color(0.53f, 0.23f, 0.09f), 4, 2);
		yield return new Spec("CompletionChest", new Vector3(1.62f, 1.2f, 1.02f), iron, ember, 5, 3);
		yield return new Spec("SmallRewardChest", new Vector3(0.95f, 0.7f, 0.68f), new Color(0.19f, 0.12f, 0.07f), new Color(0.55f, 0.31f, 0.12f), 1, 1);
		yield return new Spec("MediumRewardChest", new Vector3(1.2f, 0.9f, 0.82f), new Color(0.15f, 0.11f, 0.08f), new Color(0.69f, 0.35f, 0.1f), 2, 2);
		yield return new Spec("LargeRewardChest", new Vector3(1.55f, 1.2f, 1.02f), iron, new Color(0.92f, 0.39f, 0.08f), 4, 3);
		yield return new Spec("RecoveryContainer", new Vector3(1.45f, 0.8f, 0.82f), new Color(0.16f, 0.18f, 0.18f), new Color(0.2f, 0.55f, 0.43f), 3, 4, locker: true);
	}

	private static Mesh Persist(Mesh mesh)
	{
		string path = string.Format("{0}/Forge_{1:000}_{2}.asset", MeshRoot, _meshSequence++, mesh.name);
		Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
		if (existing != null)
		{
			EditorUtility.CopySerialized(mesh, existing);
			Object.DestroyImmediate(mesh);
			EditorUtility.SetDirty(existing);
			return existing;
		}
		AssetDatabase.CreateAsset(mesh, path);
		return mesh;
	}

	private static void BuildPrefab(Spec spec)
	{
		Color plank = Color.Lerp(spec.Body, Color.white, 0.12f);
		Color metall = Color.Lerp(spec.Accent, Color.black, 0.35f);
		Color innen = new Color(0.018f, 0.015f, 0.012f);
		Color futter = Color.Lerp(spec.Body, Color.white, 0.28f);
		Color gold = new Color(0.84f, 0.66f, 0.29f);
		Material material = EidrenWorldStyleAssets.EnsureWorldMaterial();
		GameObject root = new GameObject(spec.Name + "_3D");

		/* Korpus: Sockel + zwei Plankenlagen (Namen neu, Signatur bleibt ueber Bounds + Filterzahl) */
		MeshObject("Sockel", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 1.06f, spec.Size.y * 0.1f, spec.Size.z * 1.1f), 1f, spec.Accent)), material, root.transform);
		GameObject planke1 = MeshObject("Planke_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x, spec.Size.y * 0.28f, spec.Size.z), 1f, spec.Body)), material, root.transform);
		planke1.transform.localPosition = Vector3.up * (spec.Size.y * 0.1f);
		GameObject planke2 = MeshObject("Planke_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x, spec.Size.y * 0.25f, spec.Size.z), 0.98f, plank)), material, root.transform);
		planke2.transform.localPosition = Vector3.up * (spec.Size.y * 0.38f);

		/* Deckel: Bogen-Loft am bestehenden LidPivot-Ort */
		GameObject lidPivot = Child("LidPivot", root.transform);
		lidPivot.transform.localPosition = new Vector3(0f, spec.Size.y * 0.61f, spec.Size.z * 0.45f);
		EidrenMeshFactory.LoftProfile[] bogen =
		{
			new EidrenMeshFactory.LoftProfile(0f, spec.Size.x * 0.98f, spec.Size.z * 0.94f),
			new EidrenMeshFactory.LoftProfile(spec.Size.y * 0.14f, spec.Size.x * 0.9f, spec.Size.z * 0.86f),
			new EidrenMeshFactory.LoftProfile(spec.Size.y * 0.26f, spec.Size.x * 0.72f, spec.Size.z * 0.68f),
			new EidrenMeshFactory.LoftProfile(spec.Size.y * 0.34f, spec.Size.x * 0.48f, spec.Size.z * 0.46f)
		};
		GameObject lid = MeshObject("ForgedLid", Persist(EidrenMeshFactory.Loft(bogen, EidrenMeshFactory.LoftShape.Rect, spec.Body, capBottom: false, capTop: true)), material, lidPivot.transform);
		lid.transform.localPosition = new Vector3(0f, 0f, (0f - spec.Size.z) * 0.45f);

		GameObject closed = Child("ClosedDetails", root.transform);
		GameObject opened = Child("OpenedDetails", root.transform);
		GameObject emptied = Child("EmptiedDetails", root.transform);

		/* Schauseite -z: Schnalle unter ClosedDetails, Baender an der Wurzel (immer sichtbar) */
		GameObject clasp = MeshObject("IdentityClasp", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.16f, spec.Size.y * 0.3f, spec.Size.z * 0.1f), 0.8f, metall)), material, closed.transform);
		clasp.transform.localPosition = new Vector3(0f, spec.Size.y * 0.4f, (0f - spec.Size.z) * 0.5f);
		for (int index = 0; index < spec.Bands; index++)
		{
			float x = ((spec.Bands == 1) ? 0f : Mathf.Lerp((0f - spec.Size.x) * 0.38f, spec.Size.x * 0.38f, (float)index / (float)(spec.Bands - 1)));
			GameObject band = MeshObject($"ForgedBand_{index + 1}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.07f, spec.Size.y * 0.98f, spec.Size.z * 1.06f), 0.98f, metall)), material, root.transform);
			band.transform.localPosition = new Vector3(x, 0f, 0f);
		}

		/* Kamm: Spec.Crest Pyramidenzaehne auf der Deckelfront; Locker (Recovery) traegt sie aufrecht */
		for (int index = 0; index < spec.Crest; index++)
		{
			GameObject zahn = MeshObject($"Crest_{index + 1}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.12f + (float)index * 0.018f, 0.2f + (float)index * 0.035f, 0.1f), 0.1f, spec.Accent)), material, closed.transform);
			zahn.transform.localPosition = new Vector3(((float)index - (float)(spec.Crest - 1) * 0.5f) * 0.19f, spec.Size.y * 0.82f, (0f - spec.Size.z) * 0.28f);
			zahn.transform.localRotation = Quaternion.Euler(spec.Locker ? 0f : (-10f), 0f, 0f);
		}

		/* Offen/Leer + Loot-Fuellstand wie bei den Weltkisten */
		GameObject interior = MeshObject("DarkInterior", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.72f, 0.035f, spec.Size.z * 0.62f), 0.97f, innen)), material, opened.transform);
		interior.transform.localPosition = Vector3.up * (spec.Size.y * 0.63f);
		GameObject lining = MeshObject("EmptyInterior", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.72f, 0.025f, spec.Size.z * 0.62f), 0.98f, futter)), material, emptied.transform);
		lining.transform.localPosition = Vector3.up * (spec.Size.y * 0.635f);
		GameObject lootFull = Child("LootFill_Full", root.transform);
		MeshObject("Gold_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.4f, spec.Size.y * 0.16f, spec.Size.z * 0.36f), 0.55f, gold)), material, lootFull.transform).transform.localPosition = new Vector3((0f - spec.Size.x) * 0.07f, spec.Size.y * 0.64f, 0f);
		MeshObject("Gold_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.24f, spec.Size.y * 0.1f, spec.Size.z * 0.22f), 0.5f, gold)), material, lootFull.transform).transform.localPosition = new Vector3(spec.Size.x * 0.15f, spec.Size.y * 0.64f, (0f - spec.Size.z) * 0.08f);
		GameObject lootPartial = Child("LootFill_Partial", root.transform);
		MeshObject("GoldRest_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.18f, spec.Size.y * 0.06f, spec.Size.z * 0.16f), 0.55f, gold)), material, lootPartial.transform).transform.localPosition = new Vector3((0f - spec.Size.x) * 0.2f, spec.Size.y * 0.635f, spec.Size.z * 0.12f);

		BoxCollider boxCollider = root.AddComponent<BoxCollider>();
		boxCollider.size = spec.Size;
		boxCollider.center = Vector3.up * spec.Size.y * 0.5f;
		WorldChestVisual worldChestVisual = root.AddComponent<WorldChestVisual>();
		worldChestVisual.Configure(lidPivot.transform, closed, opened, emptied, null, null, lootFull, lootPartial);
		worldChestVisual.Apply(WorldChestVisualState.Closed);
		int dreiecke = 0;
		foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			dreiecke += filter.sharedMesh.triangles.Length / 3;
		}
		Debug.Log($"[StilE2] {spec.Name}: {dreiecke} Dreiecke");
		// G-004: Bodenerdung als Bauteil, nicht als Nachlauf.
		WorldContactShadowBuilder.Attach(root);
		PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Containers/Forge/" + spec.Name + "_2D.prefab");
		Object.DestroyImmediate(root);
	}

	private static GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent)
	{
		GameObject gameObject = Child(name, parent);
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
		return gameObject;
	}

	private static GameObject Child(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return gameObject;
	}

	private static void EnsureFolders()
	{
		Folder("Assets/_Game/Art/Containers/Forge", "Materials");
		Folder("Assets/_Game/Art/Containers/Forge", "Meshes");
		Folder("Assets/_Game/Prefabs", "Containers");
		Folder("Assets/_Game/Prefabs/Containers", "Forge");
	}

	private static void Folder(string parent, string child)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + child))
		{
			AssetDatabase.CreateFolder(parent, child);
		}
	}
}
}
