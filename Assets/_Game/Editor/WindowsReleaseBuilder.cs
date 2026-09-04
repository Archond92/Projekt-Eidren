using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Eidren.Editor
{
public static class WindowsReleaseBuilder
{
	[Serializable]
	private sealed class ReleaseManifest
	{
		public string version;

		public string unityVersion;

		public string commit;

		public bool workingTreeDirty;

		public string builtAtUtc;

		public string platform;

		public string artifact;

		public long artifactBytes;

		public string sha256;

		public ReleaseFile[] files;
	}

	[Serializable]
	private sealed class ReleaseFile
	{
		public string path;

		public long bytes;
	}

	public const string Version = "0.1.0";

	public const string ArtifactName = "Eidren-v0.1.0-windows-x64";

	public const string DevelopmentVersion = "0.3.4";

	public const string DevelopmentArtifactName = "Eidren-v0.3.4-windows-x64";

	private const string ReleaseRoot = "Releases";

	private const string StagingRoot = "Releases/Staging";

	[MenuItem("Eidren/Release/Build Windows v0.1.0")]
	public static void Build()
	{
		Build("0.1.0", "Eidren-v0.1.0-windows-x64", development: false);
	}

	[MenuItem("Eidren/Release/Build Windows Development Snapshot")]
	public static void BuildDevelopment()
	{
		Build(DevelopmentVersion, DevelopmentArtifactName, development: true);
	}

	private static void Build(string version, string artifactName, bool development)
	{
		string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
		string stagingDirectory = Path.Combine(projectRoot, "Releases/Staging", artifactName);
		string text = Path.Combine(projectRoot, "Releases");
		string zipPath = Path.Combine(text, artifactName + ".zip");
		string manifestPath = Path.Combine(text, artifactName + ".manifest.json");
		string checksumPath = Path.Combine(text, artifactName + ".sha256");
		ValidateOutputPath(projectRoot, stagingDirectory);
		RecreateDirectory(stagingDirectory);
		Directory.CreateDirectory(text);
		DeleteIfPresent(zipPath);
		DeleteIfPresent(manifestPath);
		DeleteIfPresent(checksumPath);
		WindowsReleaseTexturePolicy.Apply();
		string[] scenes = ReleaseScenes();
		ValidateScenes(scenes);
		BuildReport report = BuildPlayerWithReleaseSettings(scenes, Path.Combine(stagingDirectory, "Eidren.exe"), version, development);
		if (report.summary.result != BuildResult.Succeeded)
		{
			throw new InvalidOperationException($"Eidren release build failed: {report.summary.result}");
		}
		RemoveDevelopmentArtifacts(stagingDirectory);
		ZipFile.CreateFromDirectory(stagingDirectory, zipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
		string checksum = Sha256(zipPath);
		File.WriteAllText(checksumPath, checksum + "  " + Path.GetFileName(zipPath) + Environment.NewLine);
		WriteManifest(manifestPath, stagingDirectory, zipPath, checksum, projectRoot, version);
		UnityEngine.Debug.Log("Eidren " + version + " release created: " + zipPath + " " + $"({new FileInfo(zipPath).Length:N0} bytes, SHA-256 " + checksum + ").");
	}

	private static BuildReport BuildPlayerWithReleaseSettings(string[] scenes, string executablePath, string version, bool development)
	{
		NamedBuildTarget namedTarget = NamedBuildTarget.Standalone;
		bool oldUsePlayerLog = PlayerSettings.usePlayerLog;
		bool oldDefaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
		GraphicsDeviceType[] oldGraphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
		ScriptingImplementation oldBackend = PlayerSettings.GetScriptingBackend(namedTarget);
		ManagedStrippingLevel oldStripping = PlayerSettings.GetManagedStrippingLevel(namedTarget);
		string oldVersion = PlayerSettings.bundleVersion;
		try
		{
			PlayerSettings.companyName = "Eidren";
			PlayerSettings.productName = "Eidren";
			PlayerSettings.bundleVersion = version;
			PlayerSettings.usePlayerLog = false;
			PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, automatic: false);
			PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new GraphicsDeviceType[1] { GraphicsDeviceType.Direct3D11 });
			PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.Mono2x);
			PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.Medium);
			PlayerSettings.stripEngineCode = true;
			AssetDatabase.SaveAssets();
			if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
			{
				throw new InvalidOperationException("Windows x64 build support is not available.");
			}
			return BuildPipeline.BuildPlayer(new BuildPlayerOptions
			{
				scenes = scenes,
				locationPathName = executablePath,
				target = BuildTarget.StandaloneWindows64,
				options = (BuildOptions)(0x80000 | (development ? 1 : 0))
			});
		}
		finally
		{
			PlayerSettings.usePlayerLog = oldUsePlayerLog;
			PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, oldDefaultGraphics);
			PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, oldGraphics);
			PlayerSettings.SetScriptingBackend(namedTarget, oldBackend);
			PlayerSettings.SetManagedStrippingLevel(namedTarget, oldStripping);
			PlayerSettings.bundleVersion = oldVersion;
			AssetDatabase.SaveAssets();
		}
	}

	private static string[] ReleaseScenes()
	{
		return (from scene in EditorBuildSettings.scenes
			where scene.enabled
			select scene.path into path
			where path.IndexOf("Test", StringComparison.OrdinalIgnoreCase) < 0 && path.IndexOf("Capture", StringComparison.OrdinalIgnoreCase) < 0 && path.IndexOf("Review", StringComparison.OrdinalIgnoreCase) < 0 && path.IndexOf("Sandbox", StringComparison.OrdinalIgnoreCase) < 0
			select path).ToArray();
	}

	private static void ValidateScenes(string[] scenes)
	{
		if (scenes.Length < 2 || scenes[0] != "Assets/_Game/Scenes/Bootstrap.unity" || scenes[1] != "Assets/_Game/Scenes/MainMenu.unity")
		{
			throw new InvalidOperationException("Release scene order must start with Bootstrap and MainMenu.");
		}
		foreach (string scene in scenes)
		{
			if (!File.Exists(scene))
			{
				throw new FileNotFoundException("Release scene is missing.", scene);
			}
		}
	}

	private static void RemoveDevelopmentArtifacts(string directory)
	{
		string unusedD3D12Directory = Path.Combine(directory, "D3D12");
		if (Directory.Exists(unusedD3D12Directory))
		{
			Directory.Delete(unusedD3D12Directory, recursive: true);
		}
		string[] unwantedExtensions = new string[2] { ".pdb", ".log" };
		string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
		foreach (string file in files)
		{
			string extension = Path.GetExtension(file);
			if (unwantedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) || Path.GetFileName(file).Contains("-results", StringComparison.OrdinalIgnoreCase))
			{
				File.Delete(file);
			}
		}
		foreach (string item in from path in Directory.GetDirectories(directory, "*DoNotShip*", SearchOption.AllDirectories)
			orderby path.Length descending
			select path)
		{
			Directory.Delete(item, recursive: true);
		}
	}

	private static void WriteManifest(string manifestPath, string stagingDirectory, string zipPath, string checksum, string projectRoot, string version)
	{
		ReleaseFile[] files = (from path in Directory.GetFiles(stagingDirectory, "*", SearchOption.AllDirectories).OrderBy((string path) => path, StringComparer.OrdinalIgnoreCase)
			select new ReleaseFile
			{
				path = Path.GetRelativePath(stagingDirectory, path).Replace('\\', '/'),
				bytes = new FileInfo(path).Length
			}).ToArray();
		ReleaseManifest manifest = new ReleaseManifest
		{
			version = version,
			unityVersion = Application.unityVersion,
			commit = GitCommit(projectRoot),
			workingTreeDirty = GitWorkingTreeDirty(projectRoot),
			builtAtUtc = DateTime.UtcNow.ToString("O"),
			platform = "Windows x64",
			artifact = Path.GetFileName(zipPath),
			artifactBytes = new FileInfo(zipPath).Length,
			sha256 = checksum,
			files = files
		};
		File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, prettyPrint: true) + Environment.NewLine);
	}

	private static string GitCommit(string projectRoot)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "git",
				Arguments = "rev-parse --verify HEAD",
				WorkingDirectory = projectRoot,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			});
			string value = process?.StandardOutput.ReadToEnd().Trim();
			process?.WaitForExit();
			return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
		}
		catch (Exception)
		{
			return "unknown";
		}
	}

	private static bool GitWorkingTreeDirty(string projectRoot)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "git",
				Arguments = "status --porcelain --untracked-files=normal",
				WorkingDirectory = projectRoot,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			});
			string value = process?.StandardOutput.ReadToEnd();
			process?.WaitForExit();
			return !string.IsNullOrWhiteSpace(value);
		}
		catch (Exception)
		{
			return true;
		}
	}

	private static string Sha256(string path)
	{
		using FileStream stream = File.OpenRead(path);
		using SHA256 hash = SHA256.Create();
		return string.Concat(from value in hash.ComputeHash(stream)
			select value.ToString("x2"));
	}

	private static void ValidateOutputPath(string projectRoot, string stagingDirectory)
	{
		string allowed = Path.GetFullPath(Path.Combine(projectRoot, "Releases")) + Path.DirectorySeparatorChar;
		if (!(Path.GetFullPath(stagingDirectory) + Path.DirectorySeparatorChar).StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Release staging path escaped the project Releases folder.");
		}
	}

	private static void RecreateDirectory(string path)
	{
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
		Directory.CreateDirectory(path);
	}

	private static void DeleteIfPresent(string path)
	{
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}
}
}
