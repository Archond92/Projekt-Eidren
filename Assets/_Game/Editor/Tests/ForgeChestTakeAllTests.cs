using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Die Verlieskisten der Eidra-Schmiede
	/// boten kein „Alles nehmen" an — die Freigaberegel kannte nur
	/// Weltkisten und Gegner-Beutebehälter. Verlieskisten sind wie
	/// Weltkisten einmalige Fundbehälter, kein dauerhaftes Heimatlager.
	/// </summary>
	public sealed class ForgeChestTakeAllTests
	{
		[Test]
		public void Verlieskisten_ErlaubenAllesNehmen()
		{
			GameObject owner = new GameObject("ForgeChest_TakeAllProbe");
			try
			{
				EidraForgeChestContainer kiste = owner.AddComponent<EidraForgeChestContainer>();
				Assert.That(StorageWindow.AllowsTakeAll(kiste), Is.True,
					"Verlieskisten sind Fundbehälter — Alles-nehmen gehört dazu.");
			}
			finally
			{
				Object.DestroyImmediate(owner);
			}
		}
	}
}
