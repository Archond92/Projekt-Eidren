using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
public sealed class CombatFeedbackPoolingPlayModeTests
{
	[UnityTest]
	public IEnumerator FloatingText_IsResetAndReused()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Testumgebung.LeereWeltBereitstellen();
		// Andere Tests koennen (z. B. bei einem Fehlschlag mitten im Test) einen noch nicht
		// abgelaufenen FloatingCombatText hinterlassen. FindActive darf diesen Fund nicht mit
		// der hier neu erzeugten Instanz verwechseln, sonst haengt dieser Test von der
		// Ausfuehrungsreihenfolge anderer Klassen ab. Daher: bereits aktive Instanzen vorher merken.
		// Zusaetzlich zur Merkliste: noch AKTIVE Alt-Texte erst ablaufen
		// lassen. Der Pool ist ein LIFO-Stack — laeuft ein fremder Text
		// WAEHREND der 0,45-s-Wartezeit unten ab, liegt er beim zweiten
		// Spawn OBEN auf dem Stack und Acquire liefert ihn statt der
		// eigenen Instanz (FindActive in Zeile ~50 findet ihn dann auch):
		// deshalb flackerte dieser Test nur im Suitenlauf. Mit leerem
		// Aktivbestand ist die LIFO-Reihenfolge deterministisch.
		for (int frame = 0; frame < 360 && ActiveInstanceIds<FloatingCombatText>().Count > 0; frame++)
		{
			yield return null;
		}
		HashSet<int> preExisting = ActiveInstanceIds<FloatingCombatText>();
		CombatFeedback.SpawnStatusText(Vector3.zero, "TEST", Color.red, 0.01f);
		FloatingCombatText first = FindNewlyActive<FloatingCombatText>(preExisting);
		Assert.That<FloatingCombatText>(first, (IResolveConstraint)(object)Is.Not.Null);
		int firstId = first.gameObject.GetInstanceID();
		yield return new WaitForSeconds(0.45f);
		Assert.That<bool>(first.gameObject.activeSelf, (IResolveConstraint)(object)Is.False);
		Assert.That<string>(first.GetComponent<TextMesh>().text, (IResolveConstraint)(object)Is.Empty);
		CombatFeedback.SpawnStatusText(Vector3.one, "WIEDER", Color.green, 0.01f);
		FloatingCombatText floatingCombatText = FindActive<FloatingCombatText>();
		Assert.That<FloatingCombatText>(floatingCombatText, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<int>(floatingCombatText.gameObject.GetInstanceID(), (IResolveConstraint)(object)Is.EqualTo((object)firstId));
		yield return new WaitForSeconds(0.45f);
	}

	[UnityTest]
	public IEnumerator Aura_ManualReleaseIsResetAndReused()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Testumgebung.LeereWeltBereitstellen();
		GameObject gameObject = new GameObject("Aura_Target");
		GameObject gameObject2 = CombatFeedback.SpawnAura(gameObject.transform, Color.cyan, 5f, "TEST", 2f, 3f);
		int firstId = gameObject2.GetInstanceID();
		CombatFeedback.Release(gameObject2);
		Assert.That<bool>(gameObject2.activeSelf, (IResolveConstraint)(object)Is.False);
		Assert.That<string>(gameObject2.GetComponentInChildren<TextMesh>(includeInactive: true).text, (IResolveConstraint)(object)Is.Empty);
		GameObject gameObject3 = CombatFeedback.SpawnAura(gameObject.transform, Color.yellow, 5f, "WIEDER", 1f, 2f);
		Assert.That<int>(gameObject3.GetInstanceID(), (IResolveConstraint)(object)Is.EqualTo((object)firstId));
		CombatFeedback.Release(gameObject3);
		Object.Destroy(gameObject);
		yield return null;
	}

	private static T FindActive<T>() where T : Component
	{
		T[] array = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (T value in array)
		{
			if (value.gameObject.activeSelf)
			{
				return value;
			}
		}
		return null;
	}

	private static HashSet<int> ActiveInstanceIds<T>() where T : Component
	{
		HashSet<int> ids = new HashSet<int>();
		T[] array = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (T value in array)
		{
			if (value.gameObject.activeSelf)
			{
				ids.Add(value.gameObject.GetInstanceID());
			}
		}
		return ids;
	}

	private static T FindNewlyActive<T>(HashSet<int> preExisting) where T : Component
	{
		T[] array = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (T value in array)
		{
			if (value.gameObject.activeSelf && !preExisting.Contains(value.gameObject.GetInstanceID()))
			{
				return value;
			}
		}
		return null;
	}
}
}
