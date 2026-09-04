using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class WorldChestInteractionModeTests
	{
		[Test]
		public void GeschlosseneKiste_NutztTimedMitPositiverDauer()
		{
			GameObject chestObject = new GameObject("ChestModeTest_Closed");
			try
			{
				WorldChestContainer chest = chestObject.AddComponent<WorldChestContainer>();
				Assert.That(chest.Status, Is.EqualTo(WorldChestStatus.Closed));
				Assert.That(chest.Mode, Is.EqualTo(Eidren.Interaction.InteractionMode.Timed), "Geschlossene Kisten müssen wie der Ressourcenabbau nach einmaligem Auslösen selbstständig weiterlaufen");
				Assert.That(chest.HoldDuration, Is.GreaterThan(0f), "Der zeitgebundene Öffnungsvorgang braucht eine positive Dauer");
			}
			finally
			{
				Object.DestroyImmediate(chestObject);
			}
		}

		[Test]
		public void GeoeffneteKiste_BleibtInstant()
		{
			GameObject chestObject = new GameObject("ChestModeTest_Opened");
			try
			{
				WorldChestContainer chest = chestObject.AddComponent<WorldChestContainer>();
				ContentDatabase database = chestObject.AddComponent<ContentDatabase>();
				GameSession session = chestObject.AddComponent<GameSession>();
				chest.Initialize(database, session, new WorldChestState("chest-mode-test", "spawn-mode-test", WorldChestFamily.Common, new ItemStack[24], opened: true));
				Assert.That(chest.Mode, Is.EqualTo(Eidren.Interaction.InteractionMode.Instant), "Bereits geöffnete Kisten brauchen keinen zeitgebundenen Vorgang");
				Assert.That(chest.HoldDuration, Is.EqualTo(0f));
			}
			finally
			{
				Object.DestroyImmediate(chestObject);
			}
		}
	}
}
