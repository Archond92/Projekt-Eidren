using Eidren.Editor;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using UnityEditor;

namespace Eidren.Tests.EditMode
{
public sealed class WindowsReleaseTests
{
	private const string ActorTextureRoot = "Assets/_Game/Resources/Art/Actors";

	[Test]
	public void ReleaseIdentity_IsPinnedToVersionZeroPointOne()
	{
		Assert.That<string>("0.1.0", (IResolveConstraint)(object)Is.EqualTo((object)"0.1.0"));
		Assert.That<string>("Eidren-v0.1.0-windows-x64", (IResolveConstraint)(object)Is.EqualTo((object)"Eidren-v0.1.0-windows-x64"));
	}

	[Test]
	public void ReleaseScenes_StartWithBootFlow_AndExcludePrototypeContent()
	{
		MethodInfo method = typeof(WindowsReleaseBuilder).GetMethod("ReleaseScenes", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.That<MethodInfo>(method, (IResolveConstraint)(object)Is.Not.Null);
		string[] scenes = (string[])method.Invoke(null, null);
		Assert.That<string[]>(scenes, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).GreaterThanOrEqualTo((object)2));
		Assert.That<IEnumerable<string>>(scenes.Take(2), (IResolveConstraint)(object)Is.EqualTo((object)new string[2] { "Assets/_Game/Scenes/Bootstrap.unity", "Assets/_Game/Scenes/MainMenu.unity" }));
		Assert.That<string[]>(scenes, (IResolveConstraint)(object)Has.None.Matches<string>((Predicate<string>)((string path) => path.Contains("Prototype", StringComparison.OrdinalIgnoreCase) || path.Contains("Test", StringComparison.OrdinalIgnoreCase) || path.Contains("Capture", StringComparison.OrdinalIgnoreCase) || path.Contains("Review", StringComparison.OrdinalIgnoreCase) || path.Contains("Sandbox", StringComparison.OrdinalIgnoreCase))));
	}

	[Test]
	public void ActorTextures_UseLeanStandaloneReleasePolicy()
	{
		string[] array = (from text in AssetDatabase.FindAssets("t:Texture2D", new string[1] { "Assets/_Game/Resources/Art/Actors" }).Select(AssetDatabase.GUIDToAssetPath)
			where text.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
			select text).ToArray();
		Assert.That<string[]>(array, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).GreaterThan((object)0));
		string[] array2 = array;
		foreach (string path in array2)
		{
			TextureImporter obj = AssetImporter.GetAtPath(path) as TextureImporter;
			Assert.That<TextureImporter>(obj, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
			Assert.That<bool>(obj.mipmapEnabled, (IResolveConstraint)(object)Is.False, path, Array.Empty<object>());
			TextureImporterPlatformSettings platformTextureSettings = obj.GetPlatformTextureSettings("Standalone");
			Assert.That<bool>(platformTextureSettings.overridden, (IResolveConstraint)(object)Is.True, path, Array.Empty<object>());
			Assert.That<int>(platformTextureSettings.maxTextureSize, (IResolveConstraint)(object)Is.EqualTo((object)4096), path, Array.Empty<object>());
			Assert.That<int>(platformTextureSettings.compressionQuality, (IResolveConstraint)(object)Is.EqualTo((object)80), path, Array.Empty<object>());
			string fileName = Path.GetFileNameWithoutExtension(path);
			TextureImporterFormat expectedFormat = (fileName.EndsWith("_normal", StringComparison.OrdinalIgnoreCase) ? TextureImporterFormat.BC5 : (fileName.EndsWith("_emission", StringComparison.OrdinalIgnoreCase) ? TextureImporterFormat.DXT1Crunched : TextureImporterFormat.DXT5Crunched));
			Assert.That<TextureImporterFormat>(platformTextureSettings.format, (IResolveConstraint)(object)Is.EqualTo((object)expectedFormat), path, Array.Empty<object>());
		}
	}
}
}
