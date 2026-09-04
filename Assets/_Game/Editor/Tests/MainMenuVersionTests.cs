using Eidren.Editor;
using NUnit.Framework;
using System.IO;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// W-006: Der Startbildschirm traegt die aktuelle Version, nicht den
	/// Untertitel einer alten. Geprueft wird gegen die Version, die der
	/// Release-Builder tatsaechlich ausliefert — die feste Erwartung "V0.2"
	/// musste sonst bei jedem Versionssprung von Hand nachgezogen werden und
	/// war beim Sprung auf 0.3 prompt falsch (§6: eine Wahrheitsquelle).
	/// </summary>
	public sealed class MainMenuVersionTests
	{
		private const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";

		private const string BuilderPath = "Assets/_Game/Editor/EidrenSceneStructureBuilder.cs";

		/// <summary>"0.3.0-dev" wird zu "V0.3" — Haupt- und Nebenversion, so wie
		/// das Label im Startbildschirm aufgebaut ist.</summary>
		private static string ErwartetesLabel()
		{
			string[] teile = WindowsReleaseBuilder.DevelopmentVersion.Split('.');
			return "V" + teile[0] + "." + teile[1];
		}

		[Test]
		public void MainMenuSzene_TraegtDieAusgelieferteVersion()
		{
			string sceneText = File.ReadAllText(ScenePath);
			Assert.That(sceneText, Does.Not.Contain("V0.1  ·  AUFBRUCH"),
				"Der Startbildschirm zeigt noch den v0.1-Untertitel");
			Assert.That(sceneText, Does.Contain(ErwartetesLabel()),
				"Der Startbildschirm traegt nicht die Version, die der Release-Builder ausliefert ("
					+ WindowsReleaseBuilder.DevelopmentVersion + ")");
		}

		[Test]
		public void SzenenBuilder_TraegtDasselbeLabelWieDieSzene()
		{
			// Der Builder ist die Quelle des Labels bei kuenftigen Neubauten.
			// Laeuft er auseinander, erzeugt der naechste Szenenneubau
			// stillschweigend eine falsche Version.
			string builderSource = File.ReadAllText(BuilderPath);
			Assert.That(builderSource, Does.Not.Contain("V0.1  ·  AUFBRUCH"));
			Assert.That(builderSource, Does.Contain(ErwartetesLabel()),
				"Ein Neubau der Szene wuerde ein anderes Versionslabel erzeugen als die Szene heute traegt");
		}
	}
}
