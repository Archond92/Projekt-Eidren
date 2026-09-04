using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eidren.Editor
{
	/// <summary>
	/// Baut einen isolierten Windows-Player mit dem geprueften Max-Loadout-Seed.
	/// Der eigene Produktname trennt seinen Spielstand vom regulaeren Eidren.
	/// </summary>
	public static class MaxLoadoutWindowsBuilder
	{
		public const string OutputDirectory = "Releases/Eidren-Developer-MidPoly-windows-x64";
		public const string ExecutableName = "Eidren-Developer-MidPoly.exe";
		private const string SeedSource = "Assets/_Game/Editor/TesterSeed/TesterSeedSave.json";
		private const string RuntimeSeed = "Assets/_Game/Resources/Data/TesterSeedSave.json";

		[MenuItem("Eidren/Release/Build Windows Max-Loadout EXE")]
		public static void Build()
		{
			TesterSeedSaveBuilder.Build();
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string outputRoot = Path.GetFullPath(Path.Combine(projectRoot, OutputDirectory));
			if (!outputRoot.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Max-Loadout-Ausgabe liegt ausserhalb des Projekts.");

			// Eigener Ausgabeordner fuer diesen Stand: Der bisherige F33-Build
			// bleibt als Vergleich erhalten, waehrend im neuen Ordner garantiert
			// keine veralteten Player-Dateien liegen.
			if (Directory.Exists(outputRoot))
				Directory.Delete(outputRoot, true);
			Directory.CreateDirectory(outputRoot);

			Directory.CreateDirectory(Path.GetDirectoryName(RuntimeSeed));
			File.Copy(SeedSource, RuntimeSeed, true);
			AssetDatabase.ImportAsset(RuntimeSeed, ImportAssetOptions.ForceSynchronousImport);

			string oldProduct = PlayerSettings.productName;
			string oldVersion = PlayerSettings.bundleVersion;
			bool oldPlayerLog = PlayerSettings.usePlayerLog;
			bool oldDefaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
			GraphicsDeviceType[] oldGraphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
			ScriptingImplementation oldBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
			try
			{
				PlayerSettings.productName = "Eidren Developer MidPoly";
				PlayerSettings.bundleVersion = "0.3.4-dev-midpoly";
				PlayerSettings.usePlayerLog = true;
				PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
				PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
				PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

				string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
				if (scenes.Length == 0) throw new InvalidOperationException("Keine aktiven Szenen fuer den Max-Loadout-Build.");
				BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
				{
					scenes = scenes,
					locationPathName = Path.Combine(outputRoot, ExecutableName),
					target = BuildTarget.StandaloneWindows64,
					options = BuildOptions.Development
				});
				if (report.summary.result != BuildResult.Succeeded)
					throw new InvalidOperationException("Max-Loadout-Build fehlgeschlagen: " + report.summary.result);
				foreach (string developmentArtifact in Directory.GetDirectories(outputRoot, "*_BurstDebugInformation_DoNotShip"))
					Directory.Delete(developmentArtifact, true);
				Debug.Log("[MaxLoadout] Windows-EXE gebaut: " + Path.Combine(outputRoot, ExecutableName));
			}
			finally
			{
				PlayerSettings.productName = oldProduct;
				PlayerSettings.bundleVersion = oldVersion;
				PlayerSettings.usePlayerLog = oldPlayerLog;
				PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, oldDefaultGraphics);
				PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, oldGraphics);
				PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, oldBackend);
				AssetDatabase.DeleteAsset(RuntimeSeed);
				AssetDatabase.SaveAssets();
			}
		}
	}
}
