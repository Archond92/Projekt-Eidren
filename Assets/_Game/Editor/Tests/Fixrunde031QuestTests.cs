using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework;
using System.Collections.Generic;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// F31-006: Verhalten der Tutorial-Questkette (reine Logik). Die Kette
	/// rückt nur durch die passenden Ereignisse vor, vergibt je Schritt EP
	/// und leitet für Altbestände den Stand ohne EP ab.
	/// </summary>
	public sealed class Fixrunde031QuestTests
	{
		private static List<QuestStepData> Kette()
		{
			return new List<QuestStepData>
			{
				new QuestStepData("faser", "Ernte 2 Faserpflanzen", QuestConditionKind.ResourceNodeCompleted, "resource.fiber_plant", 2, 30),
				new QuestStepData("werkbank", "Baue eine Werkbank", QuestConditionKind.BuildingConstructed, "building.workbench", 1, 40),
				new QuestStepData("axt", "Fertige die Axt", QuestConditionKind.RecipeCrafted, "craft_axe", 1, 40),
				new QuestStepData("wildlinge", "Besiege 2 Wildlinge", QuestConditionKind.EnemyDefeated, null, 2, 60),
				new QuestStepData("fang", "Fange deinen ersten Eidra", QuestConditionKind.EidraCaptured, null, 1, 150)
			};
		}

		private static QuestProgressService Dienst(List<int> vergeben)
		{
			return new QuestProgressService("tutorial", Kette(), xp => vergeben.Add(xp));
		}

		[Test]
		public void ErsterSchritt_IstAktiv()
		{
			QuestProgressService dienst = Dienst(new List<int>());
			Assert.That(dienst.CurrentStepIndex, Is.EqualTo(0));
			Assert.That(dienst.IsCompleted, Is.False);
			Assert.That(dienst.TryGetCurrentStep(out QuestStepData schritt), Is.True);
			Assert.That(schritt.Id, Is.EqualTo("faser"));
			Assert.That(dienst.Steps.Count, Is.EqualTo(5));
		}

		[Test]
		public void FalschesEreignis_RuecktNichtVor()
		{
			List<int> vergeben = new List<int>();
			QuestProgressService dienst = Dienst(vergeben);
			dienst.NotifyRecipeCrafted("craft_axe");
			dienst.NotifyBuildingConstructed("building.workbench");
			dienst.NotifyEnemyDefeated();
			dienst.NotifyEidraCaptured();
			dienst.NotifyResourceNodeCompleted("resource.tree");
			Assert.That(dienst.CurrentStepIndex, Is.EqualTo(0));
			Assert.That(dienst.CurrentStepProgress, Is.EqualTo(0));
			Assert.That(vergeben, Is.Empty);
		}

		[Test]
		public void Zaehlerschritt_BrauchtDieZielanzahl()
		{
			List<int> vergeben = new List<int>();
			QuestProgressService dienst = Dienst(vergeben);
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			Assert.That(dienst.CurrentStepIndex, Is.EqualTo(0));
			Assert.That(dienst.CurrentStepProgress, Is.EqualTo(1));
			Assert.That(vergeben, Is.Empty);
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			Assert.That(dienst.CurrentStepIndex, Is.EqualTo(1));
			Assert.That(dienst.CurrentStepProgress, Is.EqualTo(0));
			Assert.That(vergeben, Is.EqualTo(new List<int> { 30 }));
		}

		[Test]
		public void SchrittAbschluss_VergibtEpUndFeuertEreignis()
		{
			List<int> vergeben = new List<int>();
			List<int> gefeuert = new List<int>();
			QuestProgressService dienst = Dienst(vergeben);
			dienst.StepChanged += index => gefeuert.Add(index);
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyBuildingConstructed("building.workbench");
			Assert.That(dienst.CurrentStepIndex, Is.EqualTo(2));
			Assert.That(vergeben, Is.EqualTo(new List<int> { 30, 40 }));
			Assert.That(gefeuert, Is.EqualTo(new List<int> { 1, 2 }));
		}

		[Test]
		public void Kettenabschluss_MeldetSichUndStopptWeitereVergabe()
		{
			List<int> vergeben = new List<int>();
			bool fertig = false;
			QuestProgressService dienst = Dienst(vergeben);
			dienst.ChainCompleted += () => fertig = true;
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyBuildingConstructed("building.workbench");
			dienst.NotifyRecipeCrafted("craft_axe");
			dienst.NotifyEnemyDefeated();
			dienst.NotifyEnemyDefeated();
			dienst.NotifyEidraCaptured();
			Assert.That(dienst.IsCompleted, Is.True);
			Assert.That(fertig, Is.True);
			Assert.That(dienst.TryGetCurrentStep(out _), Is.False);
			Assert.That(vergeben, Is.EqualTo(new List<int> { 30, 40, 40, 60, 150 }));
			dienst.NotifyEidraCaptured();
			dienst.NotifyRecipeCrafted("craft_axe");
			Assert.That(vergeben.Count, Is.EqualTo(5), "Nach dem Abschluss fließen keine EP mehr.");
		}

		[Test]
		public void Zustand_UeberstehtDieRundreise()
		{
			List<int> vergeben = new List<int>();
			QuestProgressService dienst = Dienst(vergeben);
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyBuildingConstructed("building.workbench");
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			QuestChainRuntimeState stand = dienst.CaptureState();
			Assert.That(stand, Is.Not.Null);
			Assert.That(stand.ChainId, Is.EqualTo("tutorial"));
			Assert.That(stand.StepIndex, Is.EqualTo(2));
			QuestProgressService wieder = Dienst(new List<int>());
			wieder.RestoreState(stand);
			Assert.That(wieder.CurrentStepIndex, Is.EqualTo(2));
			Assert.That(wieder.IsCompleted, Is.False);
			wieder.NotifyRecipeCrafted("craft_axe");
			Assert.That(wieder.CurrentStepIndex, Is.EqualTo(3));
		}

		[Test]
		public void Altbestand_HaktRezeptUndGebaeudeOhneEpAb()
		{
			List<int> vergeben = new List<int>();
			QuestProgressService dienst = Dienst(vergeben);
			dienst.AlignWithLegacyProgress(
				new[] { "craft_axe", "craft_hammer" },
				new[] { "building.workbench" },
				hasCapturedEidra: false);
			// Schritt 0 ist ein Zählschritt (nicht ableitbar) — die Kette
			// bleibt dort stehen; die erfüllten Folgeschritte räumen erst ab,
			// wenn die Kette sie erreicht.
			Assert.That(dienst.CurrentStepIndex, Is.EqualTo(0));
			Assert.That(vergeben, Is.Empty);
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			// Werkbank und Axt sind laut Altbestand erledigt: ohne EP
			// überspringen, direkt zum Wildling-Schritt.
			Assert.That(dienst.CurrentStepIndex, Is.EqualTo(3));
			Assert.That(vergeben, Is.EqualTo(new List<int> { 30 }));
		}

		[Test]
		public void Altbestand_MitEidra_GiltAlsAbgeschlossen()
		{
			List<int> vergeben = new List<int>();
			QuestProgressService dienst = Dienst(vergeben);
			dienst.AlignWithLegacyProgress(new string[0], new string[0], hasCapturedEidra: true);
			Assert.That(dienst.IsCompleted, Is.True);
			Assert.That(vergeben, Is.Empty);
		}

		[Test]
		public void Altbestand_OhneEidra_BleibtVorDemFangSchrittOffen()
		{
			// Auch wer alle Stationen längst gebaut und gefertigt hat, aber
			// nie fing, behält den Abschluss-Schritt — samt seiner EP.
			List<int> vergeben = new List<int>();
			QuestProgressService dienst = Dienst(vergeben);
			dienst.AlignWithLegacyProgress(
				new[] { "craft_axe", "craft_hammer", "craft_catch_device" },
				new[] { "building.workbench", "building.smelter" },
				hasCapturedEidra: false);
			Assert.That(dienst.IsCompleted, Is.False);
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyResourceNodeCompleted("resource.fiber_plant");
			dienst.NotifyEnemyDefeated();
			dienst.NotifyEnemyDefeated();
			Assert.That(dienst.CurrentStepIndex, Is.EqualTo(4), "Werkbank und Axt sind abgeleitet übersprungen.");
			Assert.That(vergeben, Is.EqualTo(new List<int> { 30, 60 }), "Nur echte neue Abschlüsse geben EP.");
		}
	}
}
