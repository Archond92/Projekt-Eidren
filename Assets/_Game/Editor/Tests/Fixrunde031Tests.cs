using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Input;
using Eidren.Player;
using Eidren.Presentation;
using Eidren.UI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Nachweise der Fixrunde v0.3.1 (Paket 1). Je Test ist der zugehörige
	/// Eintrag der FIXSAMMLUNG_V0.3.1.md vermerkt.
	/// </summary>
	public sealed class Fixrunde031Tests
	{
		// F31-019: Der Brot-Buff soll 2 Minuten halten (Nutzerentscheid vom
		// 17.08.2026); Dauer steht doppelt — im Wert und im Beschreibungstext.
		[Test]
		public void Brot_BuffDauertZweiMinuten()
		{
			ItemDefinition brot = LoadAsset<ItemDefinition>("Assets/_Game/Data/Items/BuffFood.asset");
			Assert.That(brot.UseConfiguration.Duration, Is.EqualTo(120f),
				"Brot-Buff muss 120 Sekunden halten.");
			Assert.That(brot.Description, Does.Contain("2 Minuten"),
				"Die Beschreibung muss die Dauer nennen, die tatsächlich gilt.");
		}

		// F31-016a: Die Schild-Fähigkeit (Steinhaut, Reichweite 0) wirkt auf die
		// eigene Figur und darf nicht an der Kampfziel-Prüfung scheitern.
		[Test]
		public void Steinhaut_ScheitertNichtAnDerZielpruefung()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			GameObject player = new GameObject("F31016_Player");
			GameObject cameraObject = new GameObject("F31016_Camera");
			try
			{
				player.AddComponent<CharacterController>();
				Damageable health = player.AddComponent<Damageable>();
				health.Initialize(100f);
				PlayerInputReader input = player.AddComponent<PlayerInputReader>();
				PlayerMotor motor = player.AddComponent<PlayerMotor>();
				Camera camera = cameraObject.AddComponent<Camera>();
				motor.Initialize(input, camera, health, Vector3.zero, 3f);
				EidraData terrock = LoadAsset<EidraData>("Assets/_Game/Data/Eidren/Terrock.asset");
				Assert.That(terrock.Skill2, Is.Not.Null, "Terrock braucht Steinhaut als Skill2.");
				Assert.That(terrock.Skill2.ExecutionType, Is.EqualTo(AbilityExecutionType.Shield),
					"Vorbedingung: Skill2 von Terrock ist der Schild.");
				Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
				EidraTeamController team = player.AddComponent<EidraTeamController>();
				// Kein Boss angebunden — exakt die Lage in jeder normalen Zone.
				team.Initialize(input, motor, health, null, terrock, null, material, material);
				string failure = null;
				team.AbilityFailed += (index, reason) =>
				{
					if (index == 1)
					{
						failure = reason;
					}
				};
				input.PressSkill(1);
				Assert.That(failure, Is.Not.EqualTo("KEIN GÜLTIGES ZIEL"),
					"Der Schild braucht kein Kampfziel und darf nicht an der Zielprüfung scheitern.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(player);
				UnityEngine.Object.DestroyImmediate(cameraObject);
			}
		}

		private static readonly (string Slot, string Stoff, string Kupfer, string Eisen)[] RuestungsSlots =
		{
			("Kopf", "WandererHood", "CopperHelmet", "IronHelmet"),
			("Brust", "WandererCoat", "CopperChest", "IronChest"),
			("Haende", "WandererBracers", "CopperGloves", "IronGloves"),
			("Beine", "WandererLegs", "CopperLegs", "IronLegs")
		};

		// F31-002: Alle zwölf Rüstungsteile brauchen eigene Icons — heute teilen
		// sich alle drei Materialstufen die vier Wanderer-Platzhalter.
		[Test]
		public void Ruestung_TeiltKeinIconUeberMaterialstufen()
		{
			var gesehen = new Dictionary<Sprite, string>();
			foreach (var slot in RuestungsSlots)
			{
				foreach (string assetName in new[] { slot.Stoff, slot.Kupfer, slot.Eisen })
				{
					ItemDefinition item = LoadAsset<ItemDefinition>("Assets/_Game/Data/Items/" + assetName + ".asset");
					Assert.That(item.Icon, Is.Not.Null, assetName + " hat kein Icon.");
					if (gesehen.TryGetValue(item.Icon, out string bereits))
					{
						Assert.Fail(assetName + " teilt sein Icon mit " + bereits + " ('"
							+ AssetDatabase.GetAssetPath(item.Icon) + "').");
					}
					gesehen.Add(item.Icon, assetName);
				}
			}
		}

		// F31-002: Die Stoffrüstung nutzt die fertigen Einzelteil-Icons
		// (ITEM_Wanderer*), nicht die ITEM_TMP_Wanderer*-Platzhalter
		// (Ganzfigur, Turnaround-Blatt, UI-Leiste).
		[Test]
		public void Stoffruestung_NutztDieFertigenEinzelteilIcons()
		{
			foreach (var slot in RuestungsSlots)
			{
				ItemDefinition item = LoadAsset<ItemDefinition>("Assets/_Game/Data/Items/" + slot.Stoff + ".asset");
				Assert.That(item.Icon, Is.Not.Null, slot.Stoff + " hat kein Icon.");
				string dateiName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(item.Icon));
				Assert.That(dateiName, Is.EqualTo("ITEM_" + slot.Stoff),
					slot.Stoff + " muss das fertige Icon ITEM_" + slot.Stoff + " tragen, nicht '" + dateiName + "'.");
			}
		}

		// F31-002: Die vier Stoffrezepte zeigen in der Werkbank das Icon ihres
		// Ergebnisses — nicht die fest verdrahteten ITEM_TMP_-Platzhalter.
		[Test]
		public void Stoffrezepte_TragenKeinWandererPlatzhalterIcon()
		{
			foreach (var slot in RuestungsSlots)
			{
				string rezeptPfad = "Assets/_Game/Data/Crafting/Recipe_" + slot.Stoff + ".asset";
				CraftingRecipeDefinition rezept = LoadAsset<CraftingRecipeDefinition>(rezeptPfad);
				Assert.That(rezept.Icon, Is.Not.Null, "Recipe_" + slot.Stoff + " hat kein Icon.");
				string dateiName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(rezept.Icon));
				Assert.That(dateiName, Does.Not.StartWith("ITEM_TMP_Wanderer"),
					"Recipe_" + slot.Stoff + " zeigt noch den Platzhalter '" + dateiName + "'.");
			}
		}

		// F31-010: Der Acker ist datenseitig auf zwei gleichzeitige
		// Instanzen begrenzt.
		[Test]
		public void AckerDefinition_TraegtDasLimitZwei()
		{
			BuildingCostDefinition acker = LoadAsset<BuildingCostDefinition>("Assets/_Game/Data/Buildings/FarmPlot.asset");
			Assert.That(acker.MaximumCount, Is.EqualTo(2),
				"Der Acker muss auf zwei gleichzeitige Instanzen begrenzt sein.");
		}

		// F31-010: Die dritte Acker-Platzierung wird abgelehnt; nach einem
		// Abriss ist der Platz wieder frei.
		[Test]
		public void Acker_HoechstensZweiGleichzeitig()
		{
			GameObject root = new GameObject("F31010_Root");
			try
			{
				ContentDatabase content = root.AddComponent<ContentDatabase>();
				GameSession session = root.AddComponent<GameSession>();
				session.Initialize();
				session.ConfigureContentDatabase(content);
				session.StartNewGame();
				ProgressionCurveDefinition curve = LoadAsset<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
				TechnologyTreeDefinition tree = LoadAsset<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
				PlayerProgressionService progression = new PlayerProgressionService(curve);
				TechnologyUnlockService technology = new TechnologyUnlockService(tree, progression);
				BuildingService service = new BuildingService(content, session, technology, progression);
				IBuildingPlacementRule rule = new BuildingPlacementRule(content, new BuildingPlacementArea(new Rect(-12f, -12f, 24f, 24f), Array.Empty<Rect>()));
				progression.RecordEnemyDefeated(100000);
				foreach (TechnologyNodeDefinition node in tree.Nodes)
				{
					if (node.SortOrder <= 17 && technology.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
					{
						technology.TryUnlock(node.Id);
					}
				}
				session.PlayerInventory.Add("wood", 100);
				session.PlayerInventory.Add("plant_fiber", 100);
				BuildingPlacementCandidate erste = new BuildingPlacementCandidate("building.farm_plot", new Vector3(-8f, 0f, -8f), 0);
				BuildingPlacementCandidate zweite = new BuildingPlacementCandidate("building.farm_plot", new Vector3(-2f, 0f, -2f), 0);
				BuildingPlacementCandidate dritte = new BuildingPlacementCandidate("building.farm_plot", new Vector3(5f, 0f, 5f), 0);
				Assert.That(service.TryPlace(in erste, rule, out _), Is.EqualTo(BuildingActionResult.Success), "erster Acker");
				Assert.That(service.TryPlace(in zweite, rule, out BuildingInstanceState zweiter), Is.EqualTo(BuildingActionResult.Success), "zweiter Acker");
				Assert.That(service.TryPlace(in dritte, rule, out _), Is.EqualTo(BuildingActionResult.BuildingLimitReached),
					"Der dritte Acker muss an der Mengengrenze scheitern.");
				Assert.That(service.TryDemolish(zweiter.InstanceId, out _), Is.EqualTo(BuildingActionResult.Success), "Abriss");
				Assert.That(service.TryPlace(in dritte, rule, out _), Is.EqualTo(BuildingActionResult.Success),
					"Nach dem Abriss muss wieder ein Acker gebaut werden können.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		// F31-017: Bodenteile reichen bis y = 0,14 — die Vorschaumarke muss
		// darüber liegen, sonst verdeckt ein gelegter Boden die Umrandung.
		[Test]
		public void Bauvorschau_MarkeLiegtUeberDerBodenoberkante()
		{
			Assert.That(BuildingGhostView.MarkLocalPosition.y, Is.GreaterThanOrEqualTo(0.16f),
				"Die Vorschaumarke liegt unter der Bodenoberkante (0,14) und wird von gelegten Böden verdeckt.");
		}

		// F31-007: Die vier stillgelegten Knoten und der separate Slot-Knoten
		// sind aus dem Baum entfernt; kein Knoten trägt mehr "retired.never".
		[Test]
		public void Technologiebaum_OhneStillgelegteUndOhneSlotKnoten()
		{
			TechnologyTreeDefinition tree = LoadAsset<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
			string[] entfernt =
			{
				"technology.12.second_eidra_slot", "technology.19.t1_weapons",
				"technology.21.cloth_coat", "technology.22.cloth_bracers", "technology.23.cloth_shoes"
			};
			foreach (string id in entfernt)
			{
				Assert.That(tree.TryGetNode(id, out _), Is.False, id + " muss entfernt sein.");
			}
			Assert.That(tree.Nodes.Count, Is.EqualTo(28), "Baum muss 28 Knoten haben.");
			foreach (TechnologyNodeDefinition node in tree.Nodes)
			{
				Assert.That(node.RequiredProgressFlag, Is.Not.EqualTo("retired.never"),
					node.Id + " darf nicht stillgelegt sein.");
			}
		}

		// F31-011: Das Fanggerät setzt Sägewerk und Seilerei voraus (seine
		// Zutaten), trägt den zweiten Eidra-Platz selbst und steht in der
		// Knotenliste hinter beiden Voraussetzungen (Freischalt-Reihenfolge).
		[Test]
		public void Fanggeraet_KommtNachSeinenProduktionsgebaeuden()
		{
			TechnologyTreeDefinition tree = LoadAsset<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
			Assert.That(tree.TryGetNode("technology.11.catch_device", out TechnologyNodeDefinition catchDevice), Is.True);
			Assert.That(catchDevice.PrerequisiteNodeIds,
				Is.EquivalentTo(new[] { "technology.13.sawmill", "technology.14.ropewalk" }),
				"Fanggerät braucht Brett (Sägewerk) und Seil (Seilerei).");
			Assert.That(catchDevice.FeatureIds, Does.Contain("feature.eidra_slot_2"),
				"Der zweite Eidra-Platz kommt mit dem Fanggerät.");
			int indexCatch = IndexOf(tree, "technology.11.catch_device");
			Assert.That(indexCatch, Is.GreaterThan(IndexOf(tree, "technology.13.sawmill")));
			Assert.That(indexCatch, Is.GreaterThan(IndexOf(tree, "technology.14.ropewalk")));
		}

		// F31-011: Alte Spielstände mit dem separaten Slot-Knoten laden weiter;
		// der Knoten verschwindet, der Punkt wird erstattet, das Feature bleibt
		// über den Fanggerät-Knoten erhalten.
		[Test]
		public void MigrationVierzehn_EntferntDenSlotKnotenMitRueckerstattung()
		{
			SaveGameData legacy = new SaveGameData
			{
				saveVersion = 13,
				progression = new SaveProgressionData
				{
					availableTechnologyPoints = 1,
					spentTechnologyPoints = 5,
					unlockedTechnologyNodeIds = new[]
					{
						"technology.01.workbench", "technology.11.catch_device",
						"technology.12.second_eidra_slot"
					}
				}
			};
			Assert.That(SaveGameMigration.TryMigrate(legacy, out SaveGameData migrated, out string error), Is.True, error);
			// Seit F31-006 endet die Kette bei 15 (Queststand).
			Assert.That(migrated.saveVersion, Is.EqualTo(15));
			Assert.That(migrated.progression.unlockedTechnologyNodeIds,
				Is.EquivalentTo(new[] { "technology.01.workbench", "technology.11.catch_device" }));
			Assert.That(migrated.progression.availableTechnologyPoints, Is.EqualTo(2), "Punkt erstattet.");
			Assert.That(migrated.progression.spentTechnologyPoints, Is.EqualTo(4));
		}

		// F31-012: Der Katalog braucht einen maskierten Scrollbereich — ohne
		// ihn wächst die Kartenzeile (11 × 200 px + Abstände) über den
		// Bildschirmrand und ein Teil der Gebäude ist unerreichbar.
		[Test]
		public void Baumenue_KatalogLiegtInMaskiertemScrollbereich()
		{
			GameObject prefab = LoadAsset<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab");
			UnityEngine.UI.ScrollRect scroll = prefab.GetComponentInChildren<UnityEngine.UI.ScrollRect>(includeInactive: true);
			Assert.That(scroll, Is.Not.Null, "Katalog hat keinen ScrollRect.");
			Assert.That(scroll.horizontal, Is.True, "Katalog muss horizontal scrollen.");
			Assert.That(scroll.viewport, Is.Not.Null, "ScrollRect ohne Viewport.");
			Assert.That(scroll.viewport.GetComponent<UnityEngine.UI.RectMask2D>(), Is.Not.Null,
				"Der Viewport braucht eine Maske, sonst läuft die Zeile weiter über den Rand.");
			Assert.That(scroll.content, Is.Not.Null, "ScrollRect ohne Content.");
			BuildingMenuItemView[] views = prefab.GetComponentsInChildren<BuildingMenuItemView>(includeInactive: true);
			Assert.That(views, Has.Length.EqualTo(11));
			foreach (BuildingMenuItemView view in views)
			{
				Assert.That(view.transform.IsChildOf(scroll.content), Is.True,
					view.name + " liegt nicht im scrollbaren Content.");
			}
		}

		// F31-013: Die Karte trennt Name und Kosten vertikal — beide über die
		// volle Kartenbreite, Kosten unten, Name darüber. Die alte Karte legte
		// beide nebeneinander (Name bekam ~77 px und brach mitten im Wort um).
		[Test]
		public void Baumenue_KartenTrennenNameUndKostenVertikal()
		{
			GameObject prefab = LoadAsset<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab");
			foreach (BuildingMenuItemView view in prefab.GetComponentsInChildren<BuildingMenuItemView>(includeInactive: true))
			{
				RectTransform label = FindRect(view.transform, "Label");
				RectTransform cost = FindRect(view.transform, "CostLabel");
				Assert.That(label.anchorMin.x, Is.LessThanOrEqualTo(0.05f), view.name + ": Name muss die volle Breite nutzen.");
				Assert.That(label.anchorMax.x, Is.GreaterThanOrEqualTo(0.95f), view.name + ": Name muss die volle Breite nutzen.");
				Assert.That(cost.anchorMin.x, Is.LessThanOrEqualTo(0.05f), view.name + ": Kosten müssen die volle Breite nutzen.");
				Assert.That(cost.anchorMax.y, Is.LessThanOrEqualTo(0.5f), view.name + ": Kosten gehören in die untere Kartenhälfte.");
				Assert.That(label.anchorMin.y, Is.GreaterThanOrEqualTo(0.5f), view.name + ": Name gehört in die obere Kartenhälfte.");
				Text costText = cost.GetComponent<Text>();
				Assert.That(costText.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap),
					view.name + ": Kosten dürfen nicht über den Kartenrand hinauslaufen.");
			}
		}

		// F31-013: Alle elf Gebäude zeigen ihr eigenes Gebäude-Render — nicht
		// das Brett-Icon (Lagerkiste) oder den Hammer (Werkbank).
		[Test]
		public void Baumenue_GebaeudeIconsZeigenDasGebaeude()
		{
			var gesehen = new Dictionary<Sprite, string>();
			foreach (string asset in new[] { "Workbench", "StorageChest", "Smelter", "Sawmill", "Ropewalk", "Stonecutter", "CookingPot", "FarmPlot", "Wall", "Floor", "Door" })
			{
				BuildingCostDefinition building = LoadAsset<BuildingCostDefinition>("Assets/_Game/Data/Buildings/" + asset + ".asset");
				Assert.That(building.Icon, Is.Not.Null, asset + " hat kein Icon.");
				string pfad = AssetDatabase.GetAssetPath(building.Icon);
				Assert.That(Path.GetFileName(pfad), Does.StartWith("BLD_"),
					asset + " zeigt '" + Path.GetFileName(pfad) + "' statt eines Gebäude-Renders.");
				if (gesehen.TryGetValue(building.Icon, out string bereits))
				{
					Assert.Fail(asset + " teilt sein Icon mit " + bereits + ".");
				}
				gesehen.Add(building.Icon, asset);
			}
		}

		// F31-013: Der Kategorie-Kopf ist Teil der Karte (oberer Streifen) und
		// überlagert weder Icon noch Namen.
		[Test]
		public void Baumenue_KategorieKopfUeberlagertKeineKartenteile()
		{
			GameObject prefab = LoadAsset<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab");
			foreach (BuildingMenuItemView view in prefab.GetComponentsInChildren<BuildingMenuItemView>(includeInactive: true))
			{
				RectTransform header = FindRect(view.transform, "CategoryHeader");
				RectTransform icon = FindRect(view.transform, "BuildingIcon");
				float headerBottom = -header.sizeDelta.y;
				Assert.That(icon.anchoredPosition.y - icon.sizeDelta.y * (1f - icon.pivot.y), Is.LessThanOrEqualTo(headerBottom + 0.01f),
					view.name + ": Das Icon muss vollständig unter dem Kategorie-Kopf beginnen.");
				Assert.That(icon.anchorMin.y, Is.EqualTo(1f).Within(0.001f),
					view.name + ": Icon hängt nicht an der Kartenoberkante — Anordnung unklar.");
			}
		}

		// F31-014: Gegenprobe zur Meldung „Kupfererz wird nicht verbraucht" —
		// die Entnahme nimmt zuerst aus dem Rucksack, erst dann aus dem Lager.
		// Mit leerem Rucksack MUSS der Lagerbestand sinken; tut er das, war
		// die Beobachtung eine Folge der Entnahme-Reihenfolge, kein Fehler.
		[Test]
		public void Schmelzen_ZiehtErzAusDemLagerWennDerRucksackLeerIst()
		{
			GameObject root = new GameObject("F31014_Root");
			try
			{
				ContentDatabase content = root.AddComponent<ContentDatabase>();
				GameSession session = root.AddComponent<GameSession>();
				session.Initialize();
				session.ConfigureContentDatabase(content);
				session.StartNewGame();
				ProgressionCurveDefinition curve = LoadAsset<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
				TechnologyTreeDefinition tree = LoadAsset<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
				PlayerProgressionService progression = new PlayerProgressionService(curve);
				TechnologyUnlockService technology = new TechnologyUnlockService(tree, progression);
				progression.RecordEnemyDefeated(100000);
				foreach (TechnologyNodeDefinition node in tree.Nodes)
				{
					if (node.SortOrder <= 17 && technology.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
					{
						technology.TryUnlock(node.Id);
					}
				}
				session.Buildings.TryAdd(new BuildingInstanceState("f31014.chest", "building.storage_chest", Vector3.zero, 0, 1));
				session.SetStorageState(new StorageContainerState("f31014.chest", new[] { new ItemStack("copper_ore", 3) }));
				Assert.That(session.PlayerInventory.GetTotalAmount("copper_ore"), Is.Zero, "Vorbedingung: Rucksack leer.");
				HomeBaseMaterialStock stock = new HomeBaseMaterialStock(content, session);
				CraftingService crafting = new CraftingService(content, session.PlayerInventory, new WeaponProgressionState(), technology, null, progression, stock);
				Assert.That(crafting.TryCraft("craft_copper_bar", CraftingStationType.Smelter).Succeeded, Is.True,
					"Schmelzen mit Erz nur im Lager muss gelingen.");
				Assert.That(session.TryGetStorageState("f31014.chest", out StorageContainerState danach), Is.True);
				int imLager = 0;
				foreach (ItemStack slot in danach.Slots)
				{
					if (slot.ItemId == "copper_ore")
					{
						imLager += slot.Quantity;
					}
				}
				Assert.That(imLager, Is.Zero, "Die drei Erze müssen aus dem Lager abgezogen sein.");
				Assert.That(session.PlayerInventory.GetTotalAmount("copper_bar"), Is.EqualTo(1),
					"Der Barren landet im Rucksack.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		// F31-015: Der Modell-Gierwinkel ist ein WELT-Winkel, sitzt aber als
		// LOKALE Rotation unter der vom Gegner-Controller gedrehten Wurzel.
		// Ohne Kompensation addieren sich beide — nach Osten schaut das Modell
		// 90° daneben, nach Süden exakt entgegengesetzt zum Telegraphen.
		[Test]
		public void KreaturenModell_KompensiertDieWurzeldrehung()
		{
			Assert.That(CreatureMeshPresentation.ModelLocalYaw(90f, 90f), Is.EqualTo(0f).Within(0.001f),
				"Wurzel schaut nach Osten, Modell soll nach Osten schauen — lokal 0°.");
			Assert.That(CreatureMeshPresentation.ModelLocalYaw(180f, 180f), Is.EqualTo(0f).Within(0.001f),
				"Wurzel nach Süden, Modell nach Süden — lokal 0°, nicht 180° (Rücken zum Spieler).");
			Assert.That(Mathf.DeltaAngle(CreatureMeshPresentation.ModelLocalYaw(0f, 90f), -90f), Is.EqualTo(0f).Within(0.001f),
				"Achtel-Quantisierung: Weltziel Norden bei Ost-Wurzel ist lokal −90°.");
			Assert.That(CreatureMeshPresentation.ModelLocalYaw(45f, 0f), Is.EqualTo(45f).Within(0.001f),
				"Unrotierte Wurzel (Spielerfall): Weltwinkel bleibt unverändert.");
		}

		// F31-018: Jedes Rüstungsteil gibt so viele Lebenspunkte, wie es
		// Prozent Schutz gibt (Schutz × 100) — keine zweite Zahlenreihe.
		[Test]
		public void Ruestung_LebensBonusFolgtDemSchutz()
		{
			Assert.That(ProtectionRules.ArmorHealthBonus(new[] { 0.06f, 0.12f, 0.03f, 0.09f }),
				Is.EqualTo(30f).Within(0.001f), "Volle Eisenrüstung: +30 Leben.");
			Assert.That(ProtectionRules.ArmorHealthBonus(new[] { 0.02f, 0.04f, 0.01f, 0.03f }),
				Is.EqualTo(10f).Within(0.001f), "Volle Stoffrüstung: +10 Leben.");
			Assert.That(ProtectionRules.ArmorHealthBonus(Array.Empty<float>()), Is.Zero, "Ohne Rüstung kein Bonus.");
		}

		// F31-018: Der Bonus hebt nur das Maximum — Anlegen heilt nicht,
		// Ablegen/Bruch klemmt den aktuellen Wert auf das neue Maximum.
		[Test]
		public void Damageable_MaxLebenBonusHeiltNichtUndKlemmtBeimAblegen()
		{
			GameObject root = new GameObject("F31018_Damageable");
			try
			{
				Damageable damageable = root.AddComponent<Damageable>();
				damageable.Initialize(100f);
				damageable.SetMaxHealthBonus(30f);
				Assert.That(damageable.MaxHealth, Is.EqualTo(130f), "Bonus hebt das Maximum.");
				Assert.That(damageable.CurrentHealth, Is.EqualTo(100f), "Anlegen heilt nicht.");
				Assert.That(damageable.Heal(20f), Is.True);
				Assert.That(damageable.CurrentHealth, Is.EqualTo(120f));
				damageable.SetMaxHealthBonus(0f);
				Assert.That(damageable.MaxHealth, Is.EqualTo(100f), "Ablegen senkt das Maximum.");
				Assert.That(damageable.CurrentHealth, Is.EqualTo(100f), "Aktueller Wert wird geklemmt — nie 120/100.");
				damageable.SetMaxHealthBonus(10f);
				Assert.That(damageable.CurrentHealth, Is.EqualTo(100f), "Erneutes Anlegen heilt nicht.");
				Assert.That(damageable.MaxHealth, Is.EqualTo(110f));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		// F31-008: Das Tür-Prefab trägt Blatt-Angel, Spieler-Sensor und die
		// Türlogik; der Rahmen bleibt an der Wurzel stehen.
		[Test]
		public void Tuer_HatBlattAngelSensorUndVerdrahtung()
		{
			GameObject prefab = LoadAsset<GameObject>("Assets/_Game/Prefabs/Buildings/Level01/BLD_Door_L01.prefab");
			BuildingDoorView view = prefab.GetComponentInChildren<BuildingDoorView>(includeInactive: true);
			Assert.That(view, Is.Not.Null, "Tür hat keine BuildingDoorView.");
			Assert.That(view.Blade, Is.Not.Null, "Türblatt ist nicht verdrahtet.");
			Assert.That(view.Blade.name, Is.EqualTo("DoorBlade"));
			foreach (string teil in new[] { "DoorPlank_0", "DoorPlank_4", "Griff", "Scharnierband_0" })
			{
				Assert.That(view.Blade.Find(teil), Is.Not.Null, teil + " hängt nicht am Blatt.");
			}
			Transform rahmen = null;
			foreach (Transform child in prefab.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child.name == "RahmenLinks")
				{
					rahmen = child;
				}
			}
			Assert.That(rahmen, Is.Not.Null, "RahmenLinks fehlt.");
			Assert.That(rahmen.IsChildOf(view.Blade), Is.False, "Der Rahmen darf nicht mitschwingen.");
			Assert.That(view.BlockingCollider, Is.Not.Null, "Sperr-Collider ist nicht verdrahtet.");
			Assert.That(view.BlockingCollider.isTrigger, Is.False, "Der Sperr-Collider ist kein Trigger.");
			SphereCollider sensor = view.GetComponent<SphereCollider>();
			Assert.That(sensor, Is.Not.Null, "Türsensor fehlt.");
			Assert.That(sensor.isTrigger, Is.True, "Der Sensor muss ein Trigger sein.");
			Assert.That(sensor.radius, Is.GreaterThan(1.5f), "Sensor zu klein — Tür öffnete erst im Anschlag.");
		}

		// F31-003: Die abgebaute Kupferader muss sich in der Silhouette klar
		// vom aktiven Zustand unterscheiden — wie beim Stein: flacher Rest
		// statt des vollen Wirtsfels-Clusters, dem nur das Erz fehlt.
		[Test]
		public void Kupferader_AbgebautIstDeutlichFlacherAlsAktiv()
		{
			float aktiv = VisualHoehe("CopperVein_Active");
			float abgebaut = VisualHoehe("CopperVein_Exhausted");
			Assert.That(abgebaut, Is.LessThan(aktiv * 0.6f),
				$"Abgebaut ({abgebaut:0.00}) muss deutlich flacher sein als aktiv ({aktiv:0.00}) — sonst liest es niemand als abgebaut.");
			int teileAktiv = TeileZahl("CopperVein_Active");
			int teileAbgebaut = TeileZahl("CopperVein_Exhausted");
			Assert.That(teileAbgebaut, Is.LessThan(teileAktiv),
				"Der abgebaute Zustand braucht sichtbar weniger Felsteile.");
		}

		// F31-003 (Nachschärfung): Der abgebaute Rest darf keine Erzfarbe
		// mehr tragen — beim Stein sind beide Verbraucht-Töne grau, beim
		// Kupfer leuchtete jeder dritte Fels weiter orange.
		[Test]
		public void Kupferader_AbgebautTraegtKeineErzfarbe()
		{
			Assert.That(HatErzfarbe("CopperVein_Active"), Is.True,
				"Vorbedingung: der aktive Zustand trägt Erzfarbe.");
			Assert.That(HatErzfarbe("CopperVein_Exhausted"), Is.False,
				"Der abgebaute Rest muss farblich Wirtsfels sein — das Erz ist weg.");
		}

		private static bool HatErzfarbe(string name)
		{
			GameObject prefab = LoadAsset<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/" + name + ".prefab");
			foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(includeInactive: true))
			{
				if (renderer.sharedMaterials.Any(material => material != null && material.name == "RES_Copper_Ore")) return true;
			}
			foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
			{
				if (filter.sharedMesh == null || filter.name == "ContactShadow")
				{
					continue;
				}
				foreach (Color color in filter.sharedMesh.colors)
				{
					if (color.r > 0.6f && color.r - color.b > 0.3f)
					{
						return true;
					}
				}
			}
			return false;
		}

		// F31-001: Ein 2,4 m hoher Baum verdeckt die Figur unabhängig von
		// seiner Breite — die alte Schwelle verlangte zusätzlich 1,6 m Spanne
		// und ließ jeden Baum (1,2 m) durchfallen.
		[Test]
		public void Sichtverdeckung_HoheSchlankeVerdeckerZaehlen()
		{
			Assert.That(ActorOcclusionTransparency.IsLargeOccluder(new Bounds(Vector3.zero, new Vector3(1.2f, 2.4f, 1.2f))),
				Is.True, "Baum-Maße (2,4 hoch, 1,2 breit) müssen als Verdecker gelten.");
			Assert.That(ActorOcclusionTransparency.IsLargeOccluder(new Bounds(Vector3.zero, new Vector3(0.8f, 0.9f, 0.8f))),
				Is.False, "Kleinzeug bleibt vom Fade verschont.");
		}

		// F31-001: Materialien am opaken Weltshader müssen beim Fade auf den
		// Transparenz-Zwilling wechseln — der Basisshader kennt weder Blend
		// noch Alphakanal, Alpha-Setzen allein bleibt dort unsichtbar.
		[Test]
		public void Sichtverdeckung_WeltmaterialWechseltAufFadeZwilling()
		{
			Shader welt = Shader.Find("Eidren/World/VertexLit");
			Assert.That(welt, Is.Not.Null);
			Material quelle = new Material(welt);
			Material fade = ActorOcclusionTransparency.CreateFadeMaterial(quelle);
			try
			{
				Assert.That(fade.shader.name, Is.EqualTo("Eidren/World/VertexLitFade"),
					"Weltshader-Material muss auf den Fade-Zwilling wechseln.");
				Assert.That(fade.HasProperty("_Color"), Is.True,
					"Der Zwilling braucht den _Color-Kanal, über den der Fade das Alpha setzt.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(fade);
				UnityEngine.Object.DestroyImmediate(quelle);
			}
		}

		private static float VisualHoehe(string name)
		{
			GameObject prefab = LoadAsset<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/" + name + ".prefab");
			float top = float.MinValue;
			foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
			{
				if (filter.sharedMesh == null || filter.name == "ContactShadow")
				{
					continue;
				}
				Bounds bounds = filter.sharedMesh.bounds;
				float weltTop = filter.transform.position.y + (bounds.center.y + bounds.extents.y) * filter.transform.lossyScale.y;
				top = Mathf.Max(top, weltTop);
			}
			return top;
		}

		private static int TeileZahl(string name)
		{
			GameObject prefab = LoadAsset<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/" + name + ".prefab");
			int count = 0;
			foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
			{
				if (filter.sharedMesh != null && filter.name != "ContactShadow")
				{
					count++;
				}
			}
			return count;
		}

		private static RectTransform FindRect(Transform root, string name)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child.name == name && child != root)
				{
					return (RectTransform)child;
				}
			}
			Assert.Fail("'" + name + "' fehlt unter " + root.name);
			return null;
		}

		private static int IndexOf(TechnologyTreeDefinition tree, string nodeId)
		{
			for (int index = 0; index < tree.Nodes.Count; index++)
			{
				if (tree.Nodes[index].Id == nodeId)
				{
					return index;
				}
			}
			return -1;
		}

		private static T LoadAsset<T>(string path) where T : UnityEngine.Object
		{
			T asset = AssetDatabase.LoadAssetAtPath<T>(path);
			Assert.That(asset, Is.Not.Null, "Asset fehlt: " + path);
			return asset;
		}
	}
}
