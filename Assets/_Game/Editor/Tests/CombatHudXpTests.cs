using Eidren.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	public sealed class CombatHudXpTests
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

		[Test]
		public void XpAnzeige_SitztImSpielerstatusUndNutztKeinPortrait()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(root, Is.Not.Null, "HUD-Prefab fehlt: " + PrefabPath);
			CombatHudStatusPresenter status = root.GetComponentInChildren<CombatHudStatusPresenter>(includeInactive: true);
			Assert.That(status, Is.Not.Null);
			Assert.That(status.ExperienceFill, Is.Not.Null, "ExperienceFill-Referenz fehlt");
			Assert.That(status.LevelValue, Is.Not.Null, "LevelValue-Referenz fehlt");

			Transform playerStatus = FindDeep(root.transform, "PlayerStatus");
			Assert.That(playerStatus, Is.Not.Null, "PlayerStatus-Block fehlt");
			Assert.That(status.ExperienceFill.transform.IsChildOf(playerStatus), Is.True,
				"Die XP-Leiste gehört in den Spielerstatus links oben, nicht als separates Panel");
			Assert.That(status.LevelValue.transform.IsChildOf(playerStatus), Is.True,
				"Der Levelwert gehört in den Spielerstatus links oben, nicht als separates Panel");

			Transform portrait = FindDeep(playerStatus, "Portrait");
			Assert.That(portrait, Is.Not.Null, "Portrait fehlt im Spielerstatus");
			Sprite portraitSprite = portrait.GetComponent<Image>().sprite;
			Assert.That(portraitSprite, Is.Not.Null, "Portrait ohne Sprite");
			int portraitUses = 0;
			foreach (Image image in root.GetComponentsInChildren<Image>(includeInactive: true))
			{
				if (image.sprite == portraitSprite)
				{
					portraitUses++;
				}
			}
			Assert.That(portraitUses, Is.EqualTo(1),
				"Das Charakterbild darf im HUD nur einmal verwendet werden (Portrait), nicht als XP-Panel-Grafik");

			Assert.That(FindDeep(root.transform, "ExperiencePanel"), Is.Null,
				"Das separate XP-Panel rechts oben (hinter dem Rucksack-Button) muss entfallen");

			Assert.That(status.ExperienceFill.type, Is.EqualTo(Image.Type.Filled), "XP-Füllung muss ein Füllbalken sein");
			Assert.That(status.ExperienceFill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal), "XP-Füllung muss horizontal laufen");

			AssertActiveUpTo(status.ExperienceFill.transform, root.transform);
			AssertActiveUpTo(status.LevelValue.transform, root.transform);
		}

		private static void AssertActiveUpTo(Transform leaf, Transform root)
		{
			for (Transform current = leaf; current != null && current != root; current = current.parent)
			{
				Assert.That(current.gameObject.activeSelf, Is.True, "XP-Anzeige muss aktiv sein, inaktiv: " + current.name);
			}
		}

		private static Transform FindDeep(Transform root, string childName)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child.name == childName)
				{
					return child;
				}
			}
			return null;
		}
	}
}
