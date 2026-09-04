using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Protokolliert die Subassets der importierten Wanderer.glb, damit sich
	/// Mesh-/Clip-Namen und der Legacy-Status ohne GUI pruefen lassen.
	/// </summary>
	public static class Wanderer3DImportDiagnose
	{
		public const string GlbPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb";

		[MenuItem("Eidren/V0.2/Wanderer3D Importdiagnose")]
		public static void Run()
		{
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath(GlbPath);
			Debug.Log($"[W3D] Subassets gesamt: {assets.Length}");
			foreach (Object asset in assets)
			{
				string extra = string.Empty;
				if (asset is AnimationClip clip)
				{
					extra = $" legacy={clip.legacy} laenge={clip.length:F2}s loop={clip.isLooping}";
				}
				Debug.Log($"[W3D] {asset.GetType().Name}: {asset.name}{extra}");
			}
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
			Debug.Log("[W3D] Wurzel: " + (root != null ? root.name : "FEHLT"));
			if (root != null)
			{
				Animation animation = root.GetComponentInChildren<Animation>(true);
				Debug.Log("[W3D] Animation-Komponente: " + (animation != null ? "vorhanden" : "FEHLT"));
			}
		}
	}
}
