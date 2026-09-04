using Eidren.Presentation;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class WorldChestVisualStateTests
	{
		[Test]
		public void VierZustaende_ErgebenVierUnterscheidbareSichtbarkeiten()
		{
			GameObject wurzel = new GameObject("Kiste");
			try
			{
				Transform deckel = new GameObject("LidPivot").transform;
				deckel.SetParent(wurzel.transform);
				GameObject zu = Kind(wurzel, "ClosedDetails");
				GameObject offen = Kind(wurzel, "OpenedDetails");
				GameObject leer = Kind(wurzel, "EmptiedDetails");
				GameObject lootVoll = Kind(wurzel, "LootFill_Full");
				GameObject lootTeil = Kind(wurzel, "LootFill_Partial");
				WorldChestVisual visual = wurzel.AddComponent<WorldChestVisual>();
				visual.Configure(deckel, zu, offen, leer, null, null, lootVoll, lootTeil);
				HashSet<string> kombinationen = new HashSet<string>();
				foreach (WorldChestVisualState status in new[]
				{
					WorldChestVisualState.Closed, WorldChestVisualState.Opened,
					WorldChestVisualState.PartiallyEmptied, WorldChestVisualState.Emptied
				})
				{
					visual.Apply(status);
					kombinationen.Add($"{zu.activeSelf}|{offen.activeSelf}|{leer.activeSelf}|{lootVoll.activeSelf}|{lootTeil.activeSelf}");
				}
				Assert.That(kombinationen.Count, Is.EqualTo(4), "jeder Zustand braucht eine eigene Sichtbarkeitskombination");
				visual.Apply(WorldChestVisualState.Opened);
				Assert.That(lootVoll.activeSelf, Is.True);
				Assert.That(lootTeil.activeSelf, Is.False);
				visual.Apply(WorldChestVisualState.PartiallyEmptied);
				Assert.That(lootVoll.activeSelf, Is.False);
				Assert.That(lootTeil.activeSelf, Is.True);
				visual.Apply(WorldChestVisualState.Emptied);
				Assert.That(lootVoll.activeSelf, Is.False);
				Assert.That(lootTeil.activeSelf, Is.False);
			}
			finally
			{
				Object.DestroyImmediate(wurzel);
			}
		}

		private static GameObject Kind(GameObject eltern, string name)
		{
			GameObject kind = new GameObject(name);
			kind.transform.SetParent(eltern.transform);
			return kind;
		}
	}
}
