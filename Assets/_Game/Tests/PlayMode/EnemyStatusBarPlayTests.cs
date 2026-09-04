using Eidren.AI;
using Eidren.UI;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Die Gegner-Statusleiste bindet beim
	/// Aktivieren den Anzeigenamen der Definition — der EditMode kann das
	/// nicht prüfen, weil OnEnable dort nicht läuft.
	/// </summary>
	public sealed class EnemyStatusBarPlayTests
	{
		[UnityTest]
		public IEnumerator Bind_SetztDenAnzeigenamen()
		{
			// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
			yield return Testumgebung.LeereWeltBereitstellen();
			GameObject prefab = Resources.Load<GameObject>("Prefabs/Enemies/Wildling");
			if (prefab == null)
			{
				prefab = UnityEditorHilfe();
			}
			Assert.That(prefab, Is.Not.Null, "Wildling-Prefab nicht gefunden.");
			GameObject instance = Object.Instantiate(prefab, new Vector3(0f, 500f, 0f), Quaternion.identity);
			try
			{
				yield return null;
				WildlingStatusBars bars = instance.GetComponentInChildren<WildlingStatusBars>(true);
				Assert.That(bars, Is.Not.Null);
				WildlingController controller = instance.GetComponent<WildlingController>();
				Assert.That(bars.NameLabel, Is.Not.Null);
				Assert.That(bars.NameLabel.text, Is.EqualTo(controller.Definition.DisplayName),
					"Das Namenslabel zeigt nicht den Anzeigenamen der Definition.");
			}
			finally
			{
				Object.Destroy(instance);
			}
		}

		private static GameObject UnityEditorHilfe()
		{
#if UNITY_EDITOR
			return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Wildling.prefab");
#else
			return null;
#endif
		}
	}
}
