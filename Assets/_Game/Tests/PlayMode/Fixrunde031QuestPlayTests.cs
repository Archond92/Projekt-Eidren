using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.UI;
using NUnit.Framework;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F31-006: Die Tutorial-Questkette läuft im Spiel — der erste Schritt
	/// steht im HUD, echte Spielereignisse rücken die Kette vor und
	/// vergeben EP.
	/// </summary>
	public sealed class Fixrunde031QuestPlayTests
	{
		[UnityTest]
		public IEnumerator Questkette_StartetImHudUndRuecktDurchEreignisseVor()
		{
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			yield return SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
			float zeit = 0f;
			QuestProgressService quest = null;
			while (quest == null && zeit < 20f)
			{
				zeit += Time.deltaTime;
				quest = EidrenServiceRoot.Instance?.QuestProgress;
				yield return null;
			}
			Assert.That(quest, Is.Not.Null, "Der Questdienst wurde nicht aufgebaut.");
			Assert.That(quest.IsCompleted, Is.False, "Ein Neuspiel beginnt am Kettenanfang.");
			Assert.That(quest.TryGetCurrentStep(out QuestStepData schritt), Is.True);
			Assert.That(schritt.Id, Is.EqualTo("fasern"));
			QuestHudPresenter hud = Object.FindFirstObjectByType<QuestHudPresenter>();
			Assert.That(hud, Is.Not.Null, "Das Aufgaben-Label fehlt im Kampf-HUD.");
			Text label = hud.GetComponent<Text>();
			yield return null;
			yield return null;
			Assert.That(label.enabled, Is.True);
			Assert.That(label.text, Does.Contain("Ernte 2 Faserpflanzen").And.Contain("(0/2)"));
			// Zwei Faserpflanzen über den echten Ereignisweg abschließen.
			PlayerProgressionService progression = EidrenServiceRoot.Instance.PlayerProgression;
			int epVorher = progression.State.Level * 100000 + progression.State.Experience;
			progression.RecordResourceNodeCompleted(0, "resource.fiber_plant");
			yield return null;
			yield return null;
			Assert.That(label.text, Does.Contain("(1/2)"), "Der Zähler folgt dem ersten Abschluss.");
			progression.RecordResourceNodeCompleted(0, "resource.fiber_plant");
			Assert.That(quest.CurrentStepIndex, Is.EqualTo(1), "Nach der zweiten Faserpflanze rückt die Kette vor.");
			Assert.That(quest.TryGetCurrentStep(out schritt), Is.True);
			Assert.That(schritt.Id, Is.EqualTo("werkbank"));
			int epNachher = progression.State.Level * 100000 + progression.State.Experience;
			Assert.That(epNachher, Is.GreaterThan(epVorher), "Schrittabschluss und Ernte vergeben EP.");
			yield return null;
			yield return null;
			Assert.That(label.text, Does.Contain("Baue eine Werkbank"), "Das HUD zeigt den nächsten Schritt.");
		}
	}
}
