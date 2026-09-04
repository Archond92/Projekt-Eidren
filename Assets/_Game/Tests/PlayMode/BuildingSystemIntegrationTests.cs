using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.Presentation;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class BuildingSystemIntegrationTests
{
	private sealed class CombatThreat : EnemyControllerBase
	{
		public void Engage(Transform target, Damageable targetHealth)
		{
			InitializeEnemy("test.threat", 100f, 50f, 0.1f, target, targetHealth, new EnemyNavigationData
			{
				MoveSpeed = 3f,
				Acceleration = 12f,
				AngularSpeed = 360f,
				AttackRange = 2f,
				DetectionRange = 12f,
				LeashRange = 60f,
				LeashFollowDistance = 10f,
				PatrolRadius = 0f,
				PatrolWait = 0.05f,
				AlertDuration = 0.02f,
				ReturnTolerance = 0.5f,
				NavMeshSampleDistance = 4f,
				AgentRadius = 0.5f,
				AgentHeight = 2f
			});
			ActivateCombat();
		}

		protected override IEnumerator ExecuteAttack()
		{
			yield break;
		}
	}

	[UnityTest]
	public IEnumerator BuildCraftAndDemolish_UsesTheRealHomeBaseChain()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		BuildingPlacementController controller = player.BuildingPlacement;
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		Assert.That<WorkbenchController>(UnityEngine.Object.FindFirstObjectByType<WorkbenchController>(), (IResolveConstraint)(object)Is.Null, "A new HomeBase must not contain a gifted workbench.", Array.Empty<object>());
		services.GameSession.PlayerInventory.Add("wood", 3);
		services.GameSession.PlayerInventory.Add("stone", 2);
		services.GameSession.PlayerInventory.Add("plant_fiber", 2);
		input.PressCraftingToggle();
		yield return null;
		Assert.That<bool>(player.CraftingWindow.IsOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<CraftingStationType>(player.CraftingWindow.CurrentStation, (IResolveConstraint)(object)Is.EqualTo((object)CraftingStationType.None));
		player.CraftingWindow.CraftButton.onClick.Invoke();
		yield return null;
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("axe"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		player.CraftingWindow.Close();
		yield return null;
		services.PlayerProgression.RecordEnemyDefeated(175);
		Assert.That<TechnologyUnlockResult>(services.TechnologyUnlocks.TryUnlock("technology.01.workbench"), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success));
		// Seit dem M8.2-Technologiebaum sind die elf Level-1-Gebaeude ueber
		// die Nodes sortOrder 4-16 verteilt — nach NUR der Werkbank-Tech
		// zeigt das Menue produktkorrekt EIN Gebaeude (gemessen: 1 statt der
		// alten 11). Ein Versuch, den craft_hammer-Node dabei auszusparen
		// (fuer die alte Ausgegraut-Pruefung weiter unten), scheiterte
		// messbar: der Node ist Kettenglied, ohne ihn oeffnen sich nur 2 der
		// 11 Gebaeude. Deshalb Vollfreischaltung wie im Nachbartest; die
		// Ausgegraut-Pruefung wechselt unten die Richtung.
		UnlockEverything(services);
		services.GameSession.PlayerInventory.Add("wood", 8);
		services.GameSession.PlayerInventory.Add("stone", 6);
		input.PressBuildingToggle();
		yield return null;
		Assert.That<bool>(player.BuildingMenu.IsOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<IEnumerable<string>>(player.BuildingMenu.VisibleBuildings.Select((BuildingCostDefinition value) => value.Id), (IResolveConstraint)(object)Does.Contain("building.workbench"));
		Assert.That<int>(player.BuildingMenu.VisibleBuildings.Count, (IResolveConstraint)(object)Is.EqualTo((object)11));
		controller.BeginPlacement("building.workbench");
		Assert.That<bool>(player.BuildingMenu.IsPlacementMode, (IResolveConstraint)(object)Is.True);
		GameObject gameObject = GameObject.Find("BuildingGhost");
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(gameObject.activeInHierarchy, (IResolveConstraint)(object)Is.True);
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
		Assert.That<Renderer[]>(componentsInChildren, (IResolveConstraint)(object)Is.Not.Empty);
		Assert.That<bool>(componentsInChildren.All((Renderer renderer) => renderer.enabled && renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name == "Universal Render Pipeline/Unlit"), (IResolveConstraint)(object)Is.True);
		controller.RotateCandidate();
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		yield return null;
		WorkbenchController workbench = UnityEngine.Object.FindFirstObjectByType<WorkbenchController>();
		Assert.That<WorkbenchController>(workbench, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(services.GameSession.Buildings.GetAll()[0].QuarterTurns, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(player.BuildingMenu.IsPlacementMode, (IResolveConstraint)(object)Is.False);
		input.PressBuildingToggle();
		yield return null;
		player.CraftingWindow.Open(workbench);
		yield return null;
		string[] array = player.CraftingWindow.VisibleRecipes.Select((CraftingRecipeDefinition recipe) => recipe.Id).ToArray();
		Assert.That<string[]>(array, (IResolveConstraint)(object)Does.Contain("craft_axe"));
		Assert.That<string[]>(array, (IResolveConstraint)(object)Does.Contain("craft_hammer"));
		Assert.That<string[]>(array, (IResolveConstraint)(object)Does.Not.Contain("craft_copper_bar"));
		// RICHTUNGSWECHSEL (13.08.2026): Frueher prueften wir hier, dass das
		// Hammer-Rezept mangels Technologie AUSGEGRAUT ist (Opacity < 0,5).
		// Unter dem M8.2-Baum ist sein Node Kettenglied der Gebaeude-Techs —
		// gesperrt lassen und zugleich den vollen 11er-Katalog sehen ist
		// unmoeglich geworden. Die Deckkraft-Mechanik wird jetzt in der
		// Gegenrichtung geprueft (freigeschaltetes Rezept voll deckend);
		// eine echte Ausgegraut-Probe braeuchte ein Werkbank-Rezept aus
		// einem Node JENSEITS der Freischaltgrenze — als offener Punkt der
		// Testhygiene-Runde notiert.
		int hammerIndex = Array.IndexOf(array, "craft_hammer");
		Assert.That<float>(player.CraftingWindow.RecipeViews[hammerIndex].Opacity, (IResolveConstraint)(object)Is.GreaterThan((object)0.5f));
		player.CraftingWindow.Close();
		yield return null;
		input.PressBuildingToggle();
		yield return null;
		Assert.That<BuildingActionResult>(controller.DemolishNearest(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		yield return null;
		Assert.That<WorkbenchController>(UnityEngine.Object.FindFirstObjectByType<WorkbenchController>(), (IResolveConstraint)(object)Is.Null);
		Assert.That<BuildingInstanceState[]>(services.GameSession.Buildings.GetAll(), (IResolveConstraint)(object)Is.Empty);
		// 8/6 statt 4/3: Unter der Vollfreischaltung oben ist auch die
		// Rueckgewinnungs-Technologie aktiv — der Abriss erstattet die VOLLEN
		// Werkbank-Kosten (8 Holz, 6 Stein) statt der halben des
		// Basiszustands (gemessen: wood 8 statt erwartet 4). Die Kette
		// selbst — Abriss erstattet Material, Lager stimmt aufs Stueck —
		// bleibt exakt geprueft.
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)8));
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.EqualTo((object)6));
		player.BuildingMenu.Close();
	}

	[UnityTest]
	public IEnumerator BuildingSurvivesSceneChangeWithTransformAndLevel()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings playerPrefabBindings = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		UnlockEverything(services);
		services.GameSession.PlayerInventory.Add("wood", 12);
		services.GameSession.PlayerInventory.Add("stone", 6);
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		playerPrefabBindings.BuildingPlacement.BeginPlacement("building.floor");
		playerPrefabBindings.BuildingPlacement.MoveCandidate(Vector2.left * 4f);
		Assert.That<BuildingActionResult>(playerPrefabBindings.BuildingPlacement.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		BuildingInstanceView placedFloor = UnityEngine.Object.FindObjectsByType<BuildingInstanceView>(FindObjectsSortMode.None)
			.Single((BuildingInstanceView view) => view.BuildingId == "building.floor");
		LODGroup floorLod = placedFloor.GetComponent<LODGroup>();
		Assert.That<LODGroup>(floorLod, (IResolveConstraint)(object)Is.Not.Null);
		LOD[] floorLods = floorLod.GetLODs();
		Camera gameplayCamera = Camera.main;
		Assert.That<Camera>(gameplayCamera, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<float>(floorLods[floorLods.Length - 1].screenRelativeTransitionHeight,
			(IResolveConstraint)(object)Is.LessThan(floorLod.size / (2f * gameplayCamera.orthographicSize)),
			"Der gebaute Boden darf bei der echten Spielkamera nicht vom letzten LOD ausgeblendet werden (F33-001).", Array.Empty<object>());
		RenderTexture previousTarget = gameplayCamera.targetTexture;
		RenderTexture visibilityProbe = RenderTexture.GetTemporary(320, 180, 16);
		try
		{
			gameplayCamera.targetTexture = visibilityProbe;
			gameplayCamera.Render();
		}
		finally
		{
			gameplayCamera.targetTexture = previousTarget;
			RenderTexture.ReleaseTemporary(visibilityProbe);
		}
		Assert.That<bool>(floorLods.SelectMany((LOD lod) => lod.renderers).Any((Renderer renderer) => renderer.isVisible),
			(IResolveConstraint)(object)Is.True,
			"Der bestaetigte Boden muss nach dem Ende der Vorschau tatsaechlich gerendert werden (F33-001).", Array.Empty<object>());
		Assert.That<bool>(playerPrefabBindings.BuildingPlacement.IsPlacementActive, (IResolveConstraint)(object)Is.True, "Boden bleibt als aktiver Bauplan gewaehlt.", Array.Empty<object>());
		Assert.That<bool>(playerPrefabBindings.BuildingMenu.IsPlacementMode, (IResolveConstraint)(object)Is.True);
		playerPrefabBindings.BuildingPlacement.BeginPlacement("building.workbench");
		playerPrefabBindings.BuildingPlacement.MoveCandidate(Vector2.right * 4f);
		playerPrefabBindings.BuildingPlacement.RotateCandidate();
		Assert.That<BuildingActionResult>(playerPrefabBindings.BuildingPlacement.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		BuildingInstanceState state = services.GameSession.Buildings.GetAll().Single((BuildingInstanceState value) => value.BuildingId == "building.workbench");
		yield return SceneManager.LoadSceneAsync("WorldMap", LoadSceneMode.Single);
		yield return SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		yield return WaitForPlayer();
		BuildingInstanceView buildingInstanceView = UnityEngine.Object.FindObjectsByType<BuildingInstanceView>(FindObjectsSortMode.None).Single((BuildingInstanceView view) => view.BuildingId == "building.workbench");
		Assert.That<BuildingInstanceView>(buildingInstanceView, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<string>(buildingInstanceView.InstanceId, (IResolveConstraint)(object)Is.EqualTo((object)state.InstanceId));
		Assert.That<string>(buildingInstanceView.BuildingId, (IResolveConstraint)(object)Is.EqualTo((object)state.BuildingId));
		Assert.That<int>(buildingInstanceView.Level, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<Vector3>(buildingInstanceView.transform.position, (IResolveConstraint)(object)Is.EqualTo((object)state.Position));
		Assert.That<float>(buildingInstanceView.transform.eulerAngles.y, (IResolveConstraint)(object)Is.EqualTo((object)90f).Within((object)0.1f));
	}

	[UnityTest]
	public IEnumerator WallRunDerivesItsShapesAndTheDoorStaysWalkable()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		UnlockEverything(services);
		services.GameSession.PlayerInventory.Add("wood", 60);
		services.GameSession.PlayerInventory.Add("plant_fiber", 20);
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		yield return null;
		player.BuildingPlacement.BeginPlacement("building.wall");
		for (int index = 0; index < 3; index++)
		{
			if (index > 0)
			{
				player.BuildingPlacement.MoveCandidate(Vector2.right);
			}
			Assert.That<BuildingActionResult>(player.BuildingPlacement.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), $"Wand {index}", Array.Empty<object>());
		}
		yield return null;
		BuildingInstanceView[] walls = EdgeViews("building.wall");
		Assert.That<int>(walls.Length, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<bool>(PieceActive(walls[0], "Cap_MinusX"), (IResolveConstraint)(object)Is.True, "Das westliche Ende der Reihe steht frei.", Array.Empty<object>());
		Assert.That<bool>(PieceActive(walls[0], "Cap_PlusX"), (IResolveConstraint)(object)Is.False, "Nach Osten laeuft die Mauer weiter.", Array.Empty<object>());
		Assert.That<bool>(PieceActive(walls[1], "Cap_MinusX"), (IResolveConstraint)(object)Is.False, "Die mittlere Wand ist beidseitig eingebaut.", Array.Empty<object>());
		Assert.That<bool>(PieceActive(walls[1], "Cap_PlusX"), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(PieceActive(walls[1], "Joint_MinusX"), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(PieceActive(walls[1], "Joint_PlusX"), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(PieceActive(walls[2], "Cap_PlusX"), (IResolveConstraint)(object)Is.True, "Das oestliche Ende der Reihe steht frei.", Array.Empty<object>());
		string replacedId = walls[1].InstanceId;
		player.BuildingPlacement.BeginPlacement("building.door");
		player.BuildingPlacement.MoveCandidate(Vector2.right);
		Assert.That<BuildingActionResult>(player.BuildingPlacement.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		yield return null;
		Assert.That<bool>(services.GameSession.Buildings.GetAll().Any((BuildingInstanceState state) => state.InstanceId == replacedId), (IResolveConstraint)(object)Is.False, "Die ersetzte Wand ist aus dem Bestand verschwunden.", Array.Empty<object>());
		Assert.That<int>(EdgeViews("building.wall").Length, (IResolveConstraint)(object)Is.EqualTo((object)2), "Und ihr Objekt auch aus der Szene — sonst blockierte ihr Hindernis die frische Tuer.", Array.Empty<object>());
		BuildingInstanceView[] array = EdgeViews("building.door");
		Assert.That<int>(array.Length, (IResolveConstraint)(object)Is.EqualTo((object)1));
		NavMeshObstacle obstacle = array[0].GetComponentInChildren<NavMeshObstacle>(includeInactive: true);
		Assert.That<bool>(obstacle == null || !obstacle.enabled, (IResolveConstraint)(object)Is.True, "Die Tuer bleibt begehbar (Abschnitt 14).", Array.Empty<object>());
		BuildingInstanceView[] array2 = EdgeViews("building.wall");
		Assert.That<bool>(PieceActive(array2[0], "Cap_PlusX"), (IResolveConstraint)(object)Is.False, "Nach Osten steht jetzt die Tuer.", Array.Empty<object>());
		Assert.That<bool>(PieceActive(array2[1], "Cap_MinusX"), (IResolveConstraint)(object)Is.False);
	}

	[UnityTest]
	public IEnumerator TheGridIsVisibleOnlyInBuildMode()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot instance = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		UnlockEverything(instance);
		instance.GameSession.PlayerInventory.Add("wood", 60);
		instance.GameSession.PlayerInventory.Add("stone", 30);
		instance.GameSession.PlayerInventory.Add("plant_fiber", 20);
		BuildGridOverlayView overlay = UnityEngine.Object.FindFirstObjectByType<BuildGridOverlayView>(FindObjectsInactive.Include);
		Assert.That<BuildGridOverlayView>(overlay, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(overlay.gameObject.activeInHierarchy, (IResolveConstraint)(object)Is.False, "Ausserhalb des Baumodus bleibt das Raster unsichtbar.", Array.Empty<object>());
		Assert.That<Collider>(overlay.GetComponentInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Null, "Das Raster darf kein Raycast-Ziel erzeugen.", Array.Empty<object>());
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		yield return null;
		Assert.That<bool>(overlay.gameObject.activeInHierarchy, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(ActiveMarks(overlay, "BLD_Grid_Boundary_"), (IResolveConstraint)(object)Is.EqualTo((object)4), "Die Baugrenze umfasst die Flaeche auf allen vier Seiten.", Array.Empty<object>());
		Assert.That<int>(ActiveMarks(overlay, "BLD_Grid_Cell_"), (IResolveConstraint)(object)Is.Zero, "Ohne Bauplan gibt es keinen Kandidaten und kein Fenster.", Array.Empty<object>());
		player.BuildingPlacement.BeginPlacement("building.floor");
		yield return null;
		Assert.That<int>(ActiveMarks(overlay, "BLD_Grid_Cell_"), (IResolveConstraint)(object)Is.GreaterThan((object)0), "Bei Bodenwahl werden Zellen betont.", Array.Empty<object>());
		Assert.That<int>(ActiveMarks(overlay, "BLD_Grid_Edge_"), (IResolveConstraint)(object)Is.Zero);
		player.BuildingPlacement.BeginPlacement("building.wall");
		yield return null;
		Assert.That<int>(ActiveMarks(overlay, "BLD_Grid_Edge_"), (IResolveConstraint)(object)Is.GreaterThan((object)0), "Bei Wandwahl werden Kanten betont.", Array.Empty<object>());
		Assert.That<int>(ActiveMarks(overlay, "BLD_Grid_Node_"), (IResolveConstraint)(object)Is.GreaterThan((object)0), "... und ihre Eckpunkte.", Array.Empty<object>());
		Assert.That<int>(ActiveMarks(overlay, "BLD_Grid_Cell_"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<Collider>(overlay.GetComponentInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Null);
		GameObject gameObject = GameObject.Find("BuildingGhost");
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(gameObject.GetComponentsInChildren<Collider>(includeInactive: true).All((Collider collider) => !collider.enabled), (IResolveConstraint)(object)Is.True, "Der Ghost darf kein Raycast-Ziel erzeugen.", Array.Empty<object>());
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		yield return null;
		Assert.That<bool>(overlay.gameObject.activeInHierarchy, (IResolveConstraint)(object)Is.False, "Mit dem Baumodus verschwindet auch das Raster.", Array.Empty<object>());
	}

	[UnityTest]
	public IEnumerator ThePreviewShowsAllThreeStatesWithTwoSignals()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		UnlockEverything(services);
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		yield return null;
		player.BuildingPlacement.BeginPlacement("building.workbench");
		yield return null;
		AssertPreviewState("Conditional");
		services.GameSession.PlayerInventory.Add("wood", 40);
		services.GameSession.PlayerInventory.Add("stone", 40);
		player.BuildingPlacement.RotateCandidate();
		yield return null;
		AssertPreviewState("Valid");
		for (int step = 0; step < 60; step++)
		{
			player.BuildingPlacement.MoveCandidate(Vector2.right);
		}
		yield return null;
		AssertPreviewState("Invalid");
		Assert.That<BuildingActionResult>(player.BuildingPlacement.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.OutsideBuildArea), "Vorschau und Commit kommen aus derselben Regel.", Array.Empty<object>());
		player.BuildingMenu.Close();
	}

	[UnityTest]
	public IEnumerator ThePlacementBarServesTouchAndSwitchesHintsInPlace()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		UnlockEverything(services);
		services.GameSession.PlayerInventory.Add("wood", 40);
		services.GameSession.PlayerInventory.Add("stone", 40);
		input.PressBuildingToggle();
		yield return null;
		BuildingPlacementBar bar = player.BuildingMenu.PlacementBar;
		Assert.That<BuildingPlacementBar>(bar, (IResolveConstraint)(object)Is.Not.Null, "Die Leiste ist autoriert.", Array.Empty<object>());
		Assert.That<bool>(bar.IsVisible, (IResolveConstraint)(object)Is.False, "Ohne laufende Platzierung gibt es nichts zu bestätigen.", Array.Empty<object>());
		player.BuildingPlacement.BeginPlacement("building.workbench");
		yield return null;
		Assert.That<bool>(bar.IsVisible, (IResolveConstraint)(object)Is.True, "Der Katalog weicht zurück, die Leiste tritt an seine Stelle.", Array.Empty<object>());
		int keyboardId = bar.KeyboardHints.GetInstanceID();
		int gamepadId = bar.GamepadHints.GetInstanceID();
		input.SetDisplayFamily(InputDisplayFamily.KeyboardMouse);
		Assert.That<bool>(bar.KeyboardHints.activeSelf, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(bar.GamepadHints.activeSelf, (IResolveConstraint)(object)Is.False);
		input.SetDisplayFamily(InputDisplayFamily.Gamepad);
		Assert.That<bool>(bar.KeyboardHints.activeSelf, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(bar.GamepadHints.activeSelf, (IResolveConstraint)(object)Is.True);
		input.SetDisplayFamily(InputDisplayFamily.Touch);
		Assert.That<bool>(bar.KeyboardHints.activeSelf, (IResolveConstraint)(object)Is.False, "Im Touchmodus stehen keine Hardwarehinweise (M12.1).", Array.Empty<object>());
		Assert.That<bool>(bar.GamepadHints.activeSelf, (IResolveConstraint)(object)Is.False);
		Assert.That<int>(bar.KeyboardHints.GetInstanceID(), (IResolveConstraint)(object)Is.EqualTo((object)keyboardId), "Ein Familienwechsel erzeugt keine UI neu (M12.1).", Array.Empty<object>());
		Assert.That<int>(bar.GamepadHints.GetInstanceID(), (IResolveConstraint)(object)Is.EqualTo((object)gamepadId));
		int before = player.BuildingPlacement.Candidate.QuarterTurns;
		bar.RotateButton.onClick.Invoke();
		yield return null;
		Assert.That<int>(player.BuildingPlacement.Candidate.QuarterTurns, (IResolveConstraint)(object)Is.EqualTo((object)((before + 1) % 4)));
		bar.ConfirmButton.onClick.Invoke();
		yield return null;
		Assert.That<BuildingInstanceState[]>(services.GameSession.Buildings.GetAll(), (IResolveConstraint)(object)Is.Not.Empty, "Die Touchfläche baut.", Array.Empty<object>());
		player.BuildingPlacement.BeginPlacement("building.workbench");
		yield return null;
		bar.CancelButton.onClick.Invoke();
		yield return null;
		Assert.That<bool>(player.BuildingPlacement.IsPlacementActive, (IResolveConstraint)(object)Is.False, "Und bricht ab.", Array.Empty<object>());
		player.BuildingMenu.Close();
	}

	private static void AssertPreviewState(string state)
	{
		GameObject gameObject = GameObject.Find("BuildingGhost");
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null);
		BuildGridMark componentInChildren = gameObject.GetComponentInChildren<BuildGridMark>(includeInactive: true);
		Assert.That<BuildGridMark>(componentInChildren, (IResolveConstraint)(object)Is.Not.Null, "Bodenmarke fehlt", Array.Empty<object>());
		Assert.That<string>(componentInChildren.Pieces[0].sharedMaterial.name, (IResolveConstraint)(object)Is.EqualTo((object)("BLD_PreviewMark_" + state)), "zweites Signal", Array.Empty<object>());
		Assert.That<bool>(gameObject.GetComponentsInChildren<Renderer>(includeInactive: true).Any((Renderer renderer) => renderer.sharedMaterial != null && renderer.sharedMaterial.name == "BLD_Preview_" + state), (IResolveConstraint)(object)Is.True, "Toenung des Koerpers", Array.Empty<object>());
	}

	[UnityTest]
	public IEnumerator BuildModeLocksThePlayerAndYieldsToCombat()
	{
		yield return LoadFreshHomeBase();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		input.PressBuildingToggle();
		yield return null;
		Assert.That<bool>(player.BuildingMenu.IsOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.False, "Bewegung, Kampf, Sammeln und Interaktion sind gesperrt.", Array.Empty<object>());
		Assert.That<bool>(player.GetComponentsInChildren<Renderer>(includeInactive: true).Any((Renderer renderer) => renderer.enabled), (IResolveConstraint)(object)Is.True, "Der Spieler bleibt im Baumodus sichtbar.", Array.Empty<object>());
		Assert.That<float>(Time.timeScale, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.0001f), "Die Welt wird nicht global pausiert; Produktion laeuft weiter.", Array.Empty<object>());
		CombatThreat threat = SpawnThreat(zone, player);
		yield return WaitUntil(() => !player.BuildingMenu.IsOpen, 3f, "Der Baumodus blieb im Kampf offen.");
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.True, "Wer aus dem Baumodus faellt, muss sich wehren koennen.", Array.Empty<object>());
		input.PressBuildingToggle();
		yield return null;
		Assert.That<bool>(player.BuildingMenu.IsOpen, (IResolveConstraint)(object)Is.False, "Im Kampf laesst sich der Baumodus nicht oeffnen.", Array.Empty<object>());
		UnityEngine.Object.Destroy(threat.gameObject);
		yield return null;
		input.PressBuildingToggle();
		yield return null;
		Assert.That<bool>(player.BuildingMenu.IsOpen, (IResolveConstraint)(object)Is.True, "Nach dem Kampf steht der Baumodus wieder offen.", Array.Empty<object>());
		player.BuildingMenu.Close();
	}

	[UnityTest]
	public IEnumerator AClosedRoomDerivesARoofAndAnOpenOneDoesNot()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		BuildingPlacementController controller = player.BuildingPlacement;
		UnlockEverything(services);
		services.GameSession.PlayerInventory.Add("wood", 60);
		services.GameSession.PlayerInventory.Add("stone", 30);
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		yield return null;
		BuildingRoofView roofs = UnityEngine.Object.FindFirstObjectByType<BuildingRoofView>(FindObjectsInactive.Include);
		Assert.That<BuildingRoofView>(roofs, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Collider>(roofs.GetComponentInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Null, "Ein Dach darf kein Raycast-Ziel erzeugen (Abschnitt 9).", Array.Empty<object>());
		controller.BeginPlacement("building.floor");
		Vector3 cell = controller.Candidate.Position;
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		yield return null;
		Assert.That<int>(roofs.RoomCount, (IResolveConstraint)(object)Is.Zero, "Eine offene Bodenflaeche bekommt kein Dach.", Array.Empty<object>());
		controller.BeginPlacement("building.wall");
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Nordkante", Array.Empty<object>());
		controller.RotateCandidate();
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Ostkante", Array.Empty<object>());
		controller.MoveCandidate(Vector2.down);
		controller.RotateCandidate();
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Suedkante", Array.Empty<object>());
		controller.MoveCandidate(Vector2.left);
		controller.MoveCandidate(Vector2.up);
		controller.RotateCandidate();
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Westkante", Array.Empty<object>());
		yield return null;
		Assert.That<int>(roofs.RoomCount, (IResolveConstraint)(object)Is.EqualTo((object)1), "Der geschlossene Raum bekommt sein Dach.", Array.Empty<object>());
		// Der Dekompilierer hat den Lambda verloren und nur die Methodengruppe
		// Enumerable.Count hinterlassen. Aus dem Cast auf ActualValueDelegate<int>
		// und dem Testaufbau (ein Boden, vier Waende) rekonstruiert.
		Assert.That<int>((ActualValueDelegate<int>)(() => services.GameSession.Buildings.GetAll().Count()), (IResolveConstraint)(object)Is.EqualTo((object)5), "Ein Boden und vier Waende — das Dach ist abgeleitet und keine gespeicherte Gebaeudeinstanz (Abschnitt 15).", Array.Empty<object>());
		MovePlayer(player, cell + new Vector3(0f, 0f, 1.5f));
		Assert.That<BuildingActionResult>(controller.DemolishNearest(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		yield return null;
		Assert.That<int>(roofs.RoomCount, (IResolveConstraint)(object)Is.Zero, "Ein offener Rand traegt kein Dach mehr.", Array.Empty<object>());
		player.BuildingMenu.Close();
	}

	[UnityTest]
	public IEnumerator MoveSelectionPrefersTheBuildingOverTheFloorBeneathIt()
	{
		// F34-004/F34-005: Der Spieler steht auf dem Bodenfeld, die Werkbank
		// steht darauf. Frueher gewann der Boden die reine Distanzsuche.
		// BeginPlacement setzt den Kandidaten drei Meter VOR den Spieler, daher
		// wird der Spieler vor jeder Platzierung entsprechend versetzt.
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		BuildingPlacementController controller = player.BuildingPlacement;
		UnlockEverything(services);
		services.GameSession.PlayerInventory.Add("wood", 120);
		services.GameSession.PlayerInventory.Add("stone", 60);
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		yield return null;
		controller.BeginPlacement("building.floor");
		Vector3 cell = controller.Candidate.Position;
		Vector3 approach = cell - Vector3.forward * 3f;
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Boden", Array.Empty<object>());
		yield return null;
		MovePlayer(player, cell);
		controller.BeginMoveNearest();
		Assert.That<bool>(controller.IsPlacementActive, (IResolveConstraint)(object)Is.True, "Ein freies Bodenfeld bleibt waehlbar.", Array.Empty<object>());
		Assert.That<string>(controller.Candidate.BuildingId, (IResolveConstraint)(object)Is.EqualTo((object)"building.floor"));
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Boden an Ort und Stelle bestaetigt", Array.Empty<object>());
		yield return null;
		MovePlayer(player, approach);
		controller.BeginPlacement("building.workbench");
		Assert.That<Vector3>(controller.Candidate.Position, (IResolveConstraint)(object)Is.EqualTo((object)cell), "Werkbank-Kandidat liegt auf der Bodenzelle", Array.Empty<object>());
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Werkbank auf dem Boden", Array.Empty<object>());
		yield return null;
		MovePlayer(player, cell);
		controller.BeginMoveNearest();
		Assert.That<bool>(controller.IsPlacementActive, (IResolveConstraint)(object)Is.True, "Das Gebaeude auf dem Boden ist waehlbar.", Array.Empty<object>());
		Assert.That<string>(controller.Candidate.BuildingId, (IResolveConstraint)(object)Is.EqualTo((object)"building.workbench"), "Das Gebaeude hat Vorrang vor dem Bodenfeld unter den Fuessen.", Array.Empty<object>());
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Werkbank an Ort und Stelle bestaetigt", Array.Empty<object>());
		yield return null;
		// Vom Rand aus: die Werkbank (3,5 m) ist Kandidat, der belegte Boden nicht.
		MovePlayer(player, cell + Vector3.forward * 3.5f);
		Assert.That<BuildingActionResult>(controller.DemolishNearest(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Die Werkbank wird abgerissen.", Array.Empty<object>());
		yield return null;
		Assert.That<bool>(services.GameSession.Buildings.GetAll().Any(state => state.BuildingId == "building.workbench"), (IResolveConstraint)(object)Is.False, "Die Werkbank ist weg.", Array.Empty<object>());
		Assert.That<bool>(services.GameSession.Buildings.GetAll().Any(state => state.BuildingId == "building.floor"), (IResolveConstraint)(object)Is.True, "Der Boden bleibt.", Array.Empty<object>());
		MovePlayer(player, approach);
		controller.BeginPlacement("building.workbench");
		Assert.That<BuildingActionResult>(controller.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Werkbank erneut auf dem Boden", Array.Empty<object>());
		yield return null;
		// Auf der Zelle selbst: gleiche Distanz, der Rang entscheidet.
		MovePlayer(player, cell);
		int before = services.GameSession.Buildings.GetAll().Length;
		Assert.That<BuildingActionResult>(controller.DemolishNearest(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		yield return null;
		Assert.That<int>(services.GameSession.Buildings.GetAll().Length, (IResolveConstraint)(object)Is.EqualTo((object)(before - 1)));
		Assert.That<bool>(services.GameSession.Buildings.GetAll().Any(state => state.BuildingId == "building.floor"), (IResolveConstraint)(object)Is.True, "Abreissen trifft das Gebaeude, nie den belegten Boden darunter.", Array.Empty<object>());
		player.BuildingMenu.Close();
	}

	private static int ActiveMarks(BuildGridOverlayView overlay, string prefix)
	{
		int count = 0;
		foreach (Transform child in overlay.transform)
		{
			if (child.gameObject.activeInHierarchy && child.name.StartsWith(prefix, StringComparison.Ordinal))
			{
				count++;
			}
		}
		return count;
	}

	private static BuildingInstanceView[] EdgeViews(string buildingId)
	{
		return (from view in UnityEngine.Object.FindObjectsByType<BuildingInstanceView>(FindObjectsSortMode.None)
			where view.BuildingId == buildingId
			orderby view.transform.position.x
			select view).ToArray();
	}

	private static bool PieceActive(BuildingInstanceView view, string pieceName)
	{
		Transform transform = view.transform.Find(pieceName);
		Assert.That<Transform>(transform, (IResolveConstraint)(object)Is.Not.Null, view.name + "/" + pieceName, Array.Empty<object>());
		return transform.gameObject.activeSelf;
	}

	private static void UnlockEverything(EidrenServiceRoot services)
	{
		services.PlayerProgression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in services.TechnologyTree.Nodes)
		{
			if (node.SortOrder <= 17 && services.TechnologyUnlocks.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				Assert.That<TechnologyUnlockResult>(services.TechnologyUnlocks.TryUnlock(node.Id), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success));
			}
		}
	}

	private static IEnumerator LoadFreshHomeBase()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		yield return SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		yield return WaitForPlayer();
		EidrenServiceRoot instance = EidrenServiceRoot.Instance;
		instance.GameSession.StartNewGame();
		instance.PlayerProgression.ResetForNewGame();
		instance.TechnologyUnlocks.ResetForNewGame();
	}

	private static IEnumerator WaitForPlayer()
	{
		float deadline = Time.realtimeSinceStartup + 12f;
		while (Time.realtimeSinceStartup < deadline)
		{
			PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
			if (player != null && player.BuildingPlacement != null && player.BuildingMenu != null)
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail("HomeBase building composition did not initialize.");
	}

	private static void MovePlayer(PlayerPrefabBindings player, Vector3 position)
	{
		CharacterController characterController = player.CharacterController;
		characterController.enabled = false;
		player.transform.position = position;
		characterController.enabled = true;
	}

	private static IEnumerator WaitUntil(Func<bool> predicate, float timeout, string failure)
	{
		float deadline = Time.realtimeSinceStartup + timeout;
		while (Time.realtimeSinceStartup < deadline)
		{
			if (predicate())
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail(failure);
	}

	private static CombatThreat SpawnThreat(ZoneController zone, PlayerPrefabBindings player)
	{
		GameObject gameObject = new GameObject("CombatThreat");
		gameObject.transform.SetParent(zone.PopulationRoot, worldPositionStays: false);
		gameObject.transform.position = player.transform.position;
		CombatThreat combatThreat = gameObject.AddComponent<CombatThreat>();
		combatThreat.Engage(player.transform, player.Damageable);
		return combatThreat;
	}
}
}
