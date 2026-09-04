using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// F31-006: Katalog- und Persistenznachweise der Tutorial-Questkette.
	/// Kette und EP-Summe sind im Entwurf und in der Bibel (M13.4)
	/// festgeschrieben; jede Ziel-Id muss im jeweiligen Katalog existieren.
	/// </summary>
	public sealed class Fixrunde031QuestKatalogTests
	{
		private const string AssetPath = "Assets/_Game/Resources/Data/Progression/QuestChain_V01.asset";

		private static QuestChainDefinition Kette()
		{
			QuestChainDefinition definition = AssetDatabase.LoadAssetAtPath<QuestChainDefinition>(AssetPath);
			Assert.That(definition, Is.Not.Null, "Questkette fehlt — Eidren/Data/Build Quest Chain V0.1 ausführen.");
			return definition;
		}

		[Test]
		public void Katalog_TraegtSiebzehnSchritteMitNeunhundertvierzigEp()
		{
			QuestChainDefinition kette = Kette();
			Assert.That(kette.Id, Is.EqualTo("quest.tutorial.first_eidra"));
			Assert.That(kette.Steps.Count, Is.EqualTo(17));
			int summe = 0;
			foreach (QuestStepEntry schritt in kette.Steps)
			{
				summe += schritt.ExperienceReward;
			}
			Assert.That(summe, Is.EqualTo(940), "Die EP-Summe der Kette ist in Entwurf und Bibel festgeschrieben.");
			Assert.That(kette.Steps[16].Condition, Is.EqualTo(QuestConditionKind.EidraCaptured),
				"Die Kette endet mit dem ersten Eidra-Fang.");
		}

		[Test]
		public void Katalog_JedeZielIdExistiertImSpiel()
		{
			HashSet<string> rezepte = SammleIds("t:CraftingRecipeDefinition", "Assets/_Game/Data/Crafting");
			HashSet<string> gebaeude = SammleIds("t:BuildingCostDefinition", "Assets/_Game/Data/Buildings");
			HashSet<string> knoten = SammleIds("t:ResourceNodeDefinition", "Assets/_Game/Data");
			foreach (QuestStepEntry schritt in Kette().Steps)
			{
				switch (schritt.Condition)
				{
					case QuestConditionKind.RecipeCrafted:
						Assert.That(rezepte, Does.Contain(schritt.TargetId), $"Rezept '{schritt.TargetId}' (Schritt '{schritt.Id}') fehlt.");
						break;
					case QuestConditionKind.BuildingConstructed:
						Assert.That(gebaeude, Does.Contain(schritt.TargetId), $"Gebäude '{schritt.TargetId}' (Schritt '{schritt.Id}') fehlt.");
						break;
					case QuestConditionKind.ResourceNodeCompleted:
						Assert.That(knoten, Does.Contain(schritt.TargetId), $"Knoten '{schritt.TargetId}' (Schritt '{schritt.Id}') fehlt.");
						break;
				}
			}
		}

		[Test]
		public void RecordQuestStepCompleted_VergibtDieEp()
		{
			ProgressionCurveDefinition kurve = AssetDatabase.LoadAssetAtPath<ProgressionCurveDefinition>(
				"Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
			Assert.That(kurve, Is.Not.Null);
			PlayerProgressionService dienst = new PlayerProgressionService(kurve);
			int vorher = dienst.State.Experience;
			int vergeben = dienst.RecordQuestStepCompleted(150);
			Assert.That(vergeben, Is.EqualTo(150));
			Assert.That(dienst.State.Experience - vorher, Is.EqualTo(150).Or.LessThan(150),
				"EP fließen in den Fortschritt (Levelaufstiege zehren den Zähler).");
			Assert.That(() => dienst.RecordQuestStepCompleted(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void Spielstand_QueststandUeberstehtDieRundreise()
		{
			QuestChainRuntimeState stand = new QuestChainRuntimeState
			{
				ChainId = "quest.tutorial.first_eidra",
				StepIndex = 7,
				StepProgress = 1,
				Completed = false
			};
			SaveQuestData gespeichert = SaveQuestMapper.ToSave(stand);
			Assert.That(gespeichert.chainId, Is.EqualTo(stand.ChainId));
			Assert.That(gespeichert.stepIndex, Is.EqualTo(7));
			Assert.That(gespeichert.stepProgress, Is.EqualTo(1));
			QuestChainRuntimeState geladen = SaveQuestMapper.ToRuntime(gespeichert);
			Assert.That(geladen, Is.Not.Null);
			Assert.That(geladen.ChainId, Is.EqualTo(stand.ChainId));
			Assert.That(geladen.StepIndex, Is.EqualTo(7));
			Assert.That(geladen.StepProgress, Is.EqualTo(1));
			Assert.That(geladen.Completed, Is.False);
		}

		[Test]
		public void Spielstand_VierzehnMigriertAufFuenfzehnMitLeeremQueststand()
		{
			SaveGameData alt = new SaveGameData
			{
				saveVersion = 14,
				quest = null
			};
			Assert.That(SaveGameMigration.TryMigrate(alt, out SaveGameData migriert, out string fehler), Is.True, fehler);
			Assert.That(migriert.saveVersion, Is.EqualTo(15));
			Assert.That(migriert.quest, Is.Not.Null, "Die Migration legt den leeren Queststand an.");
			Assert.That(migriert.quest.chainId, Is.Empty, "Der Stand wird erst beim Laden aus dem Bestand abgeleitet.");
			SaveGameData zukunft = new SaveGameData
			{
				saveVersion = 16
			};
			Assert.That(SaveGameMigration.TryMigrate(zukunft, out _, out _), Is.False, "Zukünftige Stände bleiben abgelehnt.");
		}

		[Test]
		public void Progression_FeuertDieQuestEreignisse()
		{
			ProgressionCurveDefinition kurve = AssetDatabase.LoadAssetAtPath<ProgressionCurveDefinition>(
				"Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
			PlayerProgressionService dienst = new PlayerProgressionService(kurve);
			List<string> rezepte = new List<string>();
			List<string> gebaeude = new List<string>();
			List<string> knoten = new List<string>();
			int gegner = 0;
			dienst.QuestRecipeCrafted += id => rezepte.Add(id);
			dienst.QuestBuildingConstructed += id => gebaeude.Add(id);
			dienst.QuestResourceNodeCompleted += id => knoten.Add(id);
			dienst.QuestEnemyDefeated += () => gegner++;
			dienst.RecordRecipeCrafted("craft_axe");
			dienst.RecordBuildingConstructed("building.workbench");
			dienst.RecordResourceNodeCompleted(0, "resource.fiber_plant");
			dienst.RecordResourceNodeCompleted(0);
			dienst.RecordEnemyDefeated(15);
			Assert.That(rezepte, Is.EqualTo(new List<string> { "craft_axe" }));
			Assert.That(gebaeude, Is.EqualTo(new List<string> { "building.workbench" }));
			Assert.That(knoten, Is.EqualTo(new List<string> { "resource.fiber_plant" }),
				"Ohne Knoten-Id kein Quest-Ereignis — die Kette zählt nur benannte Knoten.");
			Assert.That(gegner, Is.EqualTo(1));
		}

		[Test]
		public void Roster_MeldetDenFang()
		{
			ProgressionCurveDefinition kurve = AssetDatabase.LoadAssetAtPath<ProgressionCurveDefinition>(
				"Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
			TechnologyTreeDefinition baum = AssetDatabase.LoadAssetAtPath<TechnologyTreeDefinition>(
				"Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
			TechnologyUnlockService freischaltung = new TechnologyUnlockService(baum, new PlayerProgressionService(kurve));
			EidraRosterService roster = new EidraRosterService(new EidraRoster(), freischaltung);
			int gemeldet = 0;
			roster.Captured += () => gemeldet++;
			Assert.That(roster.TryCapture("terrock", out _, out string fehler), Is.True, fehler);
			Assert.That(gemeldet, Is.EqualTo(1));
			roster.TryCapture(null, out _, out _);
			Assert.That(gemeldet, Is.EqualTo(1), "Fehlversuche melden keinen Fang.");
		}

		private static HashSet<string> SammleIds(string filter, string ordner)
		{
			HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
			foreach (string guid in AssetDatabase.FindAssets(filter, new[] { ordner }))
			{
				string pfad = AssetDatabase.GUIDToAssetPath(guid);
				UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pfad);
				SerializedObject serialized = new SerializedObject(asset);
				SerializedProperty id = serialized.FindProperty("id");
				if (id != null && !string.IsNullOrWhiteSpace(id.stringValue))
				{
					ids.Add(id.stringValue);
				}
			}
			return ids;
		}
	}
}
