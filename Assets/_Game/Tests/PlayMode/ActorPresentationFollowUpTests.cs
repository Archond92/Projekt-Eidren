using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class ActorPresentationFollowUpTests
{
	// 03.09.2026: Bis dahin diente die 2D-Sprite-Presentation mit den
	// Terrock-Sheets als Pruefvehikel. Die Sheets wurden als Altlast entfernt
	// (Terrock ist Mid-Poly-3D); beide Tests laufen jetzt mit der
	// MeshActorPresentation, die im Spiel tatsaechlich benutzt wird.

	[UnityTest]
	public IEnumerator MovingActor_FacesAndStepsInTravelDirection()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Eidren.Tests.PlayMode.Testumgebung.LeereWeltBereitstellen();
		Camera camera = CreateCamera();
		GameObject root = new GameObject("MovingActorTest");
		root.SetActive(value: false);
		MeshActorPresentation presentation = AddPresentation(root.transform);
		root.AddComponent<SpriteActorAnimator>().Configure(presentation, root.transform);
		root.SetActive(value: true);
		yield return null;
		root.transform.position += Vector3.right * 0.35f;
		yield return null;
		Assert.That<ActorFacing8>(presentation.Facing, (IResolveConstraint)(object)Is.EqualTo((object)ActorFacing8.E));
		Assert.That<ActorVisualState>(presentation.VisualState, (IResolveConstraint)(object)Is.EqualTo((object)ActorVisualState.Move));
		UnityEngine.Object.Destroy(root);
		UnityEngine.Object.Destroy(camera.gameObject);
		yield return null;
	}

	[UnityTest]
	public IEnumerator LargeOccluder_FadesAndRestoresAroundActor()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Eidren.Tests.PlayMode.Testumgebung.LeereWeltBereitstellen();
		Camera camera = CreateCamera();
		camera.transform.position = new Vector3(0f, 2f, -6f);
		camera.transform.LookAt(Vector3.up * 0.5f);
		GameObject actor = new GameObject("OcclusionActorTest");
		AddPresentation(actor.transform);
		GameObject occluder = GameObject.CreatePrimitive(PrimitiveType.Cube);
		occluder.name = "LargeOccluderTest";
		occluder.transform.position = new Vector3(0f, 1f, -3f);
		occluder.transform.localScale = new Vector3(2f, 2f, 1f);
		MeshRenderer renderer = occluder.GetComponent<MeshRenderer>();
		Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
		Assert.That<Shader>(shader, (IResolveConstraint)(object)Is.Not.Null);
		Material original = (renderer.sharedMaterial = new Material(shader));
		if (UnityEngine.Object.FindFirstObjectByType<ActorOcclusionTransparency>() == null)
		{
			new GameObject("OcclusionServiceTest").AddComponent<ActorOcclusionTransparency>();
		}
		yield return new WaitForSecondsRealtime(0.35f);
		Material faded = renderer.sharedMaterial;
		Assert.That<Material>(faded, (IResolveConstraint)(object)Is.Not.SameAs((object)original));
		Color obj = (faded.HasProperty("_BaseColor") ? faded.GetColor("_BaseColor") : faded.color);
		Assert.That<float>(obj.a, (IResolveConstraint)(object)Is.LessThan((object)0.7f));
		occluder.transform.position += Vector3.right * 10f;
		yield return new WaitForSecondsRealtime(0.35f);
		Assert.That<Material>(renderer.sharedMaterial, (IResolveConstraint)(object)Is.SameAs((object)original));
		UnityEngine.Object.Destroy(actor);
		UnityEngine.Object.Destroy(occluder);
		UnityEngine.Object.Destroy(original);
		UnityEngine.Object.Destroy(camera.gameObject);
		yield return null;
	}

	private static Camera CreateCamera()
	{
		Camera camera = new GameObject("ActorFollowUpTestCamera")
		{
			tag = "MainCamera"
		}.AddComponent<Camera>();
		camera.transform.position = new Vector3(0f, 4f, -7f);
		camera.transform.rotation = Quaternion.Euler(24f, 0f, 0f);
		return camera;
	}

	private static MeshActorPresentation AddPresentation(Transform parent)
	{
		GameObject gameObject = new GameObject("ActorVisualTest");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		GameObject modelRoot = new GameObject("ModelRoot");
		modelRoot.transform.SetParent(gameObject.transform, worldPositionStays: false);
		MeshActorPresentation presentation = gameObject.AddComponent<MeshActorPresentation>();
		presentation.Configure(null, modelRoot.transform, 1f);
		return presentation;
	}
}
}
