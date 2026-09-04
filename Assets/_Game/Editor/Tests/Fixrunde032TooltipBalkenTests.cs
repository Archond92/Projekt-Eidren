using NUnit.Framework;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// F32-009 (20.08.2026, mit Bild gemeldet): Der schwarze Balken des
	/// Tooltips war zu kurz für seinen Text — „Abklingzeit 12 s" stand
	/// rechts außerhalb. Ursache: Der Text war auf Überlaufen gestellt und
	/// der Balken auf feste Höhe. Er muss umbrechen und mitwachsen.
	/// </summary>
	public sealed class Fixrunde032TooltipBalkenTests
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

		[Test]
		public void Tooltip_BrichtUmUndWaechstMit()
		{
			GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(hud, Is.Not.Null, "Kampf-HUD-Prefab fehlt.");
			Transform panel = Finde(hud.transform, "TooltipPanel");
			Assert.That(panel, Is.Not.Null,
				"Kein Tooltip-Feld im HUD — 'Eidren/V0.4/N04-003 Tooltip ins Kampf-HUD einbauen' laufen lassen.");

			Text text = panel.GetComponentInChildren<Text>(includeInactive: true);
			Assert.That(text, Is.Not.Null, "Das Tooltip-Feld hat keinen Text.");
			Assert.That(text.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap),
				"Der Text muss umbrechen — sonst läuft er aus dem Balken heraus.");

			ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
			Assert.That(fitter, Is.Not.Null,
				"Der Balken braucht einen ContentSizeFitter, sonst bleibt er einzeilig hoch.");
			Assert.That(fitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize),
				"Die Höhe muss dem Inhalt folgen.");
			Assert.That(panel.GetComponent<VerticalLayoutGroup>(), Is.Not.Null,
				"Ohne Layoutgruppe kennt der Fitter die gewünschte Höhe nicht.");
		}

		private static Transform Finde(Transform root, string name)
		{
			foreach (Transform kind in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (kind.name == name)
				{
					return kind;
				}
			}
			return null;
		}
	}
}
