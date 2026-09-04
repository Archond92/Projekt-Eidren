using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class BuildingPreviewStateTests
{
	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private PlayerProgressionService _progression;

	private TechnologyUnlockService _technology;

	private BuildingService _service;

	private IBuildingPlacementRule _rule;

	private static readonly string[] States = new string[3] { "Valid", "Conditional", "Invalid" };

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("BuildingPreviewStateTests");
		_content = _root.AddComponent<ContentDatabase>();
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
		ProgressionCurveDefinition curve = Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
		TechnologyTreeDefinition tree = Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
		_progression = new PlayerProgressionService(curve);
		_technology = new TechnologyUnlockService(tree, _progression);
		_service = new BuildingService(_content, _session, _technology, _progression);
		_rule = new BuildingPlacementRule(_content, new BuildingPlacementArea(new Rect(-10f, -10f, 20f, 20f), new Rect[1]
		{
			new Rect(-2f, -2f, 4f, 4f)
		}));
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void ValidAndAffordable_IsTheValidState()
	{
		UnlockAll();
		AddMaterials();
		BuildingPreview preview = _service.PreviewPlacement(Candidate("building.workbench", -5f, -5f), _rule);
		Assert.That<BuildingPreviewState>(preview.State, (IResolveConstraint)(object)Is.EqualTo((object)BuildingPreviewState.Valid));
		Assert.That<BuildingActionResult>(preview.Reason, (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
	}

	[Test]
	public void MissingMaterials_AreConditional_NotInvalid()
	{
		UnlockAll();
		BuildingPreview preview = _service.PreviewPlacement(Candidate("building.workbench", -5f, -5f), _rule);
		Assert.That<BuildingPreviewState>(preview.State, (IResolveConstraint)(object)Is.EqualTo((object)BuildingPreviewState.Conditional), "Der Platz stimmt — es fehlt nur etwas Beschaffbares.", Array.Empty<object>());
		Assert.That<BuildingActionResult>(preview.Reason, (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.MissingMaterials));
	}

	[Test]
	public void AMissingUnlock_IsConditional_NotInvalid()
	{
		AddMaterials();
		BuildingPreview preview = _service.PreviewPlacement(Candidate("building.workbench", -5f, -5f), _rule);
		Assert.That<BuildingPreviewState>(preview.State, (IResolveConstraint)(object)Is.EqualTo((object)BuildingPreviewState.Conditional));
		Assert.That<BuildingActionResult>(preview.Reason, (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.BuildingLocked));
	}

	[Test]
	public void ASpatialBlock_IsInvalid()
	{
		UnlockAll();
		AddMaterials();
		Assert.That<BuildingPreviewState>(_service.PreviewPlacement(Candidate("building.workbench", 40f, 40f), _rule).State, (IResolveConstraint)(object)Is.EqualTo((object)BuildingPreviewState.Invalid));
		Assert.That<BuildingPreviewState>(_service.PreviewPlacement(Candidate("building.workbench", 0f, 0f), _rule).State, (IResolveConstraint)(object)Is.EqualTo((object)BuildingPreviewState.Invalid));
	}

	[Test]
	public void EveryResultFallsIntoExactlyOneOfTheThreeStates()
	{
		foreach (BuildingActionResult result in Enum.GetValues(typeof(BuildingActionResult)))
		{
			BuildingPreviewState buildingPreviewState;
			switch (result)
			{
			case BuildingActionResult.Success:
				buildingPreviewState = BuildingPreviewState.Valid;
				break;
			case BuildingActionResult.BuildingLocked:
			case BuildingActionResult.MissingMaterials:
				buildingPreviewState = BuildingPreviewState.Conditional;
				break;
			default:
				buildingPreviewState = BuildingPreviewState.Invalid;
				break;
			}
			BuildingPreviewState expected = buildingPreviewState;
			Assert.That<BuildingPreviewState>(BuildingPreview.StateFor(result), (IResolveConstraint)(object)Is.EqualTo((object)expected), result.ToString(), Array.Empty<object>());
		}
	}

	[Test]
	public void OutsideTheBuildArea_BeatsAMissingUnlock()
	{
		AddMaterials();
		Assert.That<BuildingActionResult>(_service.PreviewPlacement(Candidate("building.workbench", 40f, 40f), _rule).Reason, (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.OutsideBuildArea), "Wer außerhalb der Baufläche steht, soll das lesen und nicht erst nach dem Freischalten erfahren, dass die Stelle nie ging.", Array.Empty<object>());
	}

	[Test]
	public void ALockedZone_BeatsMissingMaterials()
	{
		UnlockAll();
		Assert.That<BuildingActionResult>(_service.PreviewPlacement(Candidate("building.workbench", 0f, 0f), _rule).Reason, (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.BlockedZone));
	}

	[Test]
	public void TheMissingMaterialIsNamed()
	{
		UnlockAll();
		BuildingPreview preview = _service.PreviewPlacement(Candidate("building.workbench", -5f, -5f), _rule);
		Assert.That<string>(preview.MissingItemId, (IResolveConstraint)(object)Is.Not.Empty, "„Nicht genügend Holz\" sagt, was zu tun ist.", Array.Empty<object>());
		Assert.That<bool>(_content.TryGetItem(preview.MissingItemId, out var _), (IResolveConstraint)(object)Is.True, "Der genannte Gegenstand muss auch einer sein.", Array.Empty<object>());
	}

	[Test]
	public void TheNamedMaterial_IsTheOneTheCommitFailsOn()
	{
		UnlockAll();
		_session.PlayerInventory.Add("wood", 100);
		BuildingPlacementCandidate candidate = Candidate("building.workbench", -5f, -5f);
		BuildingPreview preview = _service.PreviewPlacement(in candidate, _rule);
		Assert.That<BuildingActionResult>(preview.Reason, (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.MissingMaterials));
		Assert.That<string>(preview.MissingItemId, (IResolveConstraint)(object)Is.EqualTo((object)"stone"));
		Assert.That<BuildingActionResult>(_service.TryPlace(in candidate, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.MissingMaterials), "Vorschau und Commit kommen aus derselben Prüfung.", Array.Empty<object>());
	}

	[Test]
	public void NoReasonIsCarriedWhenNothingIsMissing()
	{
		UnlockAll();
		AddMaterials();
		Assert.That<string>(_service.PreviewPlacement(Candidate("building.workbench", -5f, -5f), _rule).MissingItemId, (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void TheShownReasonNamesTheMissingMaterial()
	{
		UnlockAll();
		_session.PlayerInventory.Add("wood", 100);
		Assert.That<string>(BuildingActionText.For(_service.PreviewPlacement(Candidate("building.workbench", -5f, -5f), _rule), _content), (IResolveConstraint)(object)Is.EqualTo((object)"Nicht genügend Stein."));
	}

	[Test]
	public void TheShownReasonFallsBackWhenTheMaterialIsUnknown()
	{
		Assert.That<string>(BuildingActionText.For(new BuildingPreview(BuildingActionResult.MissingMaterials, "gibt_es_nicht"), _content), (IResolveConstraint)(object)Is.EqualTo((object)"Nicht genügend Rohstoffe."));
	}

	[Test]
	public void EverySpatialReasonHasItsOwnSentence()
	{
		BuildingActionResult[] obj = new BuildingActionResult[8]
		{
			BuildingActionResult.OutsideBuildArea,
			BuildingActionResult.BlockedZone,
			BuildingActionResult.Overlap,
			BuildingActionResult.EdgeOccupied,
			BuildingActionResult.FloorRequired,
			BuildingActionResult.NaturalGroundRequired,
			BuildingActionResult.MixedFoundation,
			BuildingActionResult.BuildingBusy
		};
		List<string> seen = new List<string>();
		BuildingActionResult[] array = obj;
		foreach (BuildingActionResult result in array)
		{
			string text = BuildingActionText.For(result);
			Assert.That<string>(text, (IResolveConstraint)(object)Is.Not.EqualTo((object)"Aktion nicht möglich."), $"{result} faellt auf den allgemeinen Satz zurueck.", Array.Empty<object>());
			foreach (string other in seen)
			{
				Assert.That<string>(text, (IResolveConstraint)(object)Is.Not.EqualTo((object)other), $"{result} teilt seinen Satz mit einem anderen " + "Grund und ist damit nicht unterscheidbar.", Array.Empty<object>());
			}
			seen.Add(text);
		}
	}

	[Test]
	public void EveryStateHasItsOwnBodyTint()
	{
		List<Color> colours = new List<Color>();
		string[] states = States;
		foreach (string state in states)
		{
			Material material = RequireMaterial("BLD_Preview_" + state);
			foreach (Color other in colours)
			{
				Assert.That<Color>(material.color, (IResolveConstraint)(object)Is.Not.EqualTo((object)other), "BLD_Preview_" + state, Array.Empty<object>());
			}
			colours.Add(material.color);
		}
	}

	[Test]
	public void EveryStateHasASecondSignalThatWorksWithoutColour()
	{
		List<Texture> shapes = new List<Texture>();
		string[] states = States;
		foreach (string state in states)
		{
			Texture shape = RequireMaterial("BLD_PreviewMark_" + state).GetTexture("_BaseMap");
			Assert.That<Texture>(shape, (IResolveConstraint)(object)Is.Not.Null, "BLD_PreviewMark_" + state + " hat keine Form — Farbe wäre das einzige Signal.", Array.Empty<object>());
			foreach (Texture other in shapes)
			{
				Assert.That<Texture>(shape, (IResolveConstraint)(object)Is.Not.SameAs((object)other), "BLD_PreviewMark_" + state + " teilt seine Form mit einem anderen Zustand und ist damit nicht unterscheidbar.", Array.Empty<object>());
			}
			shapes.Add(shape);
		}
	}

	[Test]
	public void ThePreviewMarkIsAuthoredAndNoRaycastTarget()
	{
		GameObject prefab = Resources.Load<GameObject>("Art/Buildings/BLD_Preview_Mark");
		Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null);
		BuildGridMark component = prefab.GetComponent<BuildGridMark>();
		Assert.That<BuildGridMark>(component, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<int>(component.Pieces.Length, (IResolveConstraint)(object)Is.GreaterThan((object)0));
		Assert.That<Collider>(prefab.GetComponentInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Null, "Die Vorschau steht dort, wo gebaut werden soll, und finge sonst genau den Klick ab, der sie bestätigen soll.", Array.Empty<object>());
		Assert.That<float>(Mathf.Abs(Vector3.Dot(component.Pieces[0].transform.rotation * Vector3.forward, Vector3.up)), (IResolveConstraint)(object)Is.GreaterThan((object)0.99f), "Die Bodenmarke liegt (M10.4).", Array.Empty<object>());
	}

	private static Material RequireMaterial(string assetName)
	{
		Material material = Resources.Load<Material>("Art/Buildings/" + assetName);
		Assert.That<Material>(material, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
		return material;
	}

	private static BuildingPlacementCandidate Candidate(string buildingId, float x, float z)
	{
		return new BuildingPlacementCandidate(buildingId, new Vector3(x, 0f, z), 0);
	}

	private void AddMaterials()
	{
		_session.PlayerInventory.Add("wood", 100);
		_session.PlayerInventory.Add("stone", 100);
		_session.PlayerInventory.Add("plant_fiber", 100);
		_session.PlayerInventory.Add("copper_bar", 10);
	}

	private void UnlockAll()
	{
		_progression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset").Nodes)
		{
			if (node.SortOrder <= 17 && _technology.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				Assert.That<TechnologyUnlockResult>(_technology.TryUnlock(node.Id), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success), node.Id, Array.Empty<object>());
			}
		}
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		return val;
	}
}
}
