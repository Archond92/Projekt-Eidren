using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.IO;
using System;
using System.Linq;
using UnityEditor.Rendering;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class ActorSpriteAssetTests
{
	// Die 2D-Sprite-Sheets von Terrock und Noctarion wurden am 03.09.2026 als
	// Altlast entfernt (AltlastInventar: keine Referenzen). Die zugehoerige
	// Alpha-Pruefung TerrockMaps_AreCompleteAndAlphaAligned entfiel damit.

	[TestCase(0f, 1f, ActorFacing8.N)]
	[TestCase(1f, 1f, ActorFacing8.NE)]
	[TestCase(1f, 0f, ActorFacing8.E)]
	[TestCase(1f, -1f, ActorFacing8.SE)]
	[TestCase(0f, -1f, ActorFacing8.S)]
	[TestCase(-1f, -1f, ActorFacing8.SW)]
	[TestCase(-1f, 0f, ActorFacing8.W)]
	[TestCase(-1f, 1f, ActorFacing8.NW)]
	public void DirectionResolver_MapsEveryAuthoredSector(float x, float z, ActorFacing8 expected)
	{
		Assert.That<ActorFacing8>(EightDirectionResolver.Quantize(new Vector3(x, 0f, z)), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}

	[Test]
	public void DirectionResolver_UsesHysteresisAtSectorBoundary()
	{
		EightDirectionResolver eightDirectionResolver = new EightDirectionResolver();
		Assert.That<ActorFacing8>(eightDirectionResolver.Resolve(DirectionAt(0f)), (IResolveConstraint)(object)Is.EqualTo((object)ActorFacing8.N));
		Assert.That<ActorFacing8>(eightDirectionResolver.Resolve(DirectionAt(27f)), (IResolveConstraint)(object)Is.EqualTo((object)ActorFacing8.N), "A sub-margin boundary excursion must not flicker.", Array.Empty<object>());
		Assert.That<ActorFacing8>(eightDirectionResolver.Resolve(DirectionAt(30f)), (IResolveConstraint)(object)Is.EqualTo((object)ActorFacing8.NE));
		Assert.That<ActorFacing8>(eightDirectionResolver.Resolve(DirectionAt(20f)), (IResolveConstraint)(object)Is.EqualTo((object)ActorFacing8.NE), "Returning into the margin must keep the new view.", Array.Empty<object>());
		Assert.That<ActorFacing8>(eightDirectionResolver.Resolve(DirectionAt(14f)), (IResolveConstraint)(object)Is.EqualTo((object)ActorFacing8.N));
	}

	[Test]
	public void TerrockPrefab_UsesCanonicalMidpoly3DPresentation()
	{
		const string legacy = "Assets/_Game/Resources/Prefabs/Actors/2D/Terrock_2D.prefab";
		Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(legacy), Is.Null);
		Assert.That(File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + legacy), Is.True);
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/Prefabs/Actors/3D/Terrock_3D.prefab");
		Assert.That(gameObject, Is.Not.Null);
		Assert.That(gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true), Is.Not.Null);
		Assert.That(gameObject.GetComponentInChildren<LODGroup>(true).GetLODs(), Has.Length.EqualTo(3));
		Assert.That(gameObject.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null), Is.False);
	}

	[Test]
	public void ActorShaders_HaveNoCompilerErrors()
	{
		AssertShaderClean("Assets/_Game/Shaders/Actors/HandPaintedActorSprite.shader");
		AssertShaderClean("Assets/_Game/Shaders/Actors/ActorGroundShadow.shader");
	}

	private static Vector3 DirectionAt(float degrees)
	{
		float radians = degrees * ((float)Math.PI / 180f);
		return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
	}

	private static void AssertShaderClean(string path)
	{
		Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
		Assert.That<Shader>(shader, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		ShaderMessage[] shaderMessages = ShaderUtil.GetShaderMessages(shader);
		for (int i = 0; i < shaderMessages.Length; i++)
		{
			ShaderMessage message = shaderMessages[i];
			Assert.That<ShaderCompilerMessageSeverity>(message.severity, (IResolveConstraint)(object)Is.Not.EqualTo((object)ShaderCompilerMessageSeverity.Error), $"{path}:{message.line} {message.message}", Array.Empty<object>());
		}
	}
}
}
