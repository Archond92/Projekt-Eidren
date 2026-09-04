using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class V02ActorVisualBuilder
{
	private readonly struct Spec
	{
		public string Stem { get; }

		public string ActorId { get; }

		public float Height { get; }

		public bool Forge { get; }

		public Spec(string stem, string actorId, float height, bool forge)
		{
			Stem = stem;
			ActorId = actorId;
			Height = height;
			Forge = forge;
		}
	}

	private const string TextureRoot = "Assets/_Game/Resources/Art/Actors/V02";

	private const string WildlingVisual = "Assets/_Game/Prefabs/Actors/2D/Wildling_2D.prefab";

	[MenuItem("Eidren/Art/Build V0.2 Directional Actors")]
	public static void Build()
	{
		EnsureFolders();
		ImportTextures();
		WindowsReleaseTexturePolicy.Apply();
		foreach (Spec item in Specs())
		{
			BuildPrefab(item);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built eleven directional v0.2 actor prefabs.");
	}

	public static string PrefabPath(string stem, bool forge)
	{
		return (forge ? "Assets/_Game/Resources/Prefabs/Actors/Forge" : "Assets/_Game/Prefabs/Actors/2D/TierTwo") + "/" + stem + "_2D.prefab";
	}

	private static IEnumerable<Spec> Specs()
	{
		yield return new Spec("Riftling", "riftling", 1.7f, forge: false);
		yield return new Spec("RootCharger", "root_charger", 2.2f, forge: false);
		yield return new Spec("MoorThrower", "moor_thrower", 1.9f, forge: false);
		yield return new Spec("GraniteShell", "granite_shell", 2.4f, forge: false);
		yield return new Spec("RiftGuardian", "rift_guardian", 3f, forge: false);
		yield return new Spec("EmberEater", "ember_eater", 1.6f, forge: true);
		yield return new Spec("AshRunner", "ash_runner", 1.8f, forge: true);
		yield return new Spec("ForgeGuardian", "forge_guardian", 2.5f, forge: true);
		yield return new Spec("SealGuardian", "seal_guardian", 3f, forge: true);
		yield return new Spec("Ignivar", "ignivar", 1f, forge: true);
		yield return new Spec("CoreGuardian", "core_guardian", 4.5f, forge: true);
	}

	private static void BuildPrefab(Spec spec)
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Actors/2D/Wildling_2D.prefab");
		if (gameObject == null)
		{
			throw new FileNotFoundException("Assets/_Game/Prefabs/Actors/2D/Wildling_2D.prefab");
		}
		GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(gameObject);
		PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
		try
		{
			root.name = spec.Stem + "_2D";
			SpriteActorPresentation presentation = root.GetComponentInChildren<SpriteActorPresentation>(includeInactive: true);
			SpriteRenderer renderer = ((presentation != null) ? presentation.TargetRenderer : null);
			DynamicActorGroundShadow shadow = root.GetComponentInChildren<DynamicActorGroundShadow>(includeInactive: true);
			if (presentation == null || renderer == null || shadow == null)
			{
				throw new InvalidOperationException("The canonical actor template is incomplete.");
			}
			renderer.color = Color.white;
			presentation.Configure(renderer, shadow, spec.ActorId, "Art/Actors/V02/" + spec.ActorId, spec.Height, 256, 256, 220f, 0.055f);
			presentation.ConfigureStateStems("attack", "attack");
			presentation.ConfigurePrewarmStateStems("idle", "move", "telegraph", "attack", "hit", "stagger", "death", "appear");
			SpriteActorAnimator animator = root.GetComponentInChildren<SpriteActorAnimator>(includeInactive: true);
			if (animator == null)
			{
				animator = root.AddComponent<SpriteActorAnimator>();
			}
			animator.Configure(presentation, root.transform);
			animator.ConfigureEnemyStateStems("telegraph", "attack", "stagger", "move");
			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(spec.Stem, spec.Forge));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static void ImportTextures()
	{
		string[] files = Directory.GetFiles("Assets/_Game/Resources/Art/Actors/V02", "*.png", SearchOption.AllDirectories);
		for (int i = 0; i < files.Length; i++)
		{
			string path = files[i].Replace('\\', '/');
			TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (!(importer == null))
			{
				bool normal = path.EndsWith("_normal.png", StringComparison.OrdinalIgnoreCase);
				importer.textureType = (normal ? TextureImporterType.NormalMap : TextureImporterType.Default);
				importer.alphaSource = TextureImporterAlphaSource.FromInput;
				importer.alphaIsTransparency = !normal;
				importer.mipmapEnabled = false;
				importer.filterMode = FilterMode.Bilinear;
				importer.wrapMode = TextureWrapMode.Clamp;
				importer.maxTextureSize = 4096;
				importer.textureCompression = TextureImporterCompression.CompressedHQ;
				importer.SaveAndReimport();
			}
		}
	}

	private static void EnsureFolders()
	{
		Folder("Assets/_Game/Prefabs/Actors/2D", "TierTwo");
		Folder("Assets/_Game/Resources/Prefabs/Actors", "Forge");
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
