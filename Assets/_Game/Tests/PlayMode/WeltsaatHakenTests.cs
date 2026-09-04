using Eidren.Core.Services;
using NUnit.Framework;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Beweist, dass <see cref="FesteWeltsaatAttribute"/> tatsaechlich vor
	/// jedem Test laeuft.
	///
	/// Der Haken ist die stillste Art von Code: Feuert er nicht, faellt kein
	/// Test um — die Laeufe sind dann nur wieder zufaellig, und das merkt man
	/// erst, wenn ein Fehlschlag sich nicht nachstellen laesst. Deshalb ein
	/// Test, der genau das prueft.
	/// </summary>
	public sealed class WeltsaatHakenTests
	{
		[Test]
		public void WeltsaatIstWaehrendJedesTestsFestgenagelt()
		{
			Assert.That(ZoneStateService.DefaultMasterSeed, Is.EqualTo(Testumgebung.Saat),
				"Der Assembly-Haken hat die Weltsaat nicht gesetzt — die Suite wuerfelt wieder pro Lauf.");
		}
	}
}
