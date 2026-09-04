using Eidren.AI;
using Eidren.Composition;
using Eidren.Data;
using NUnit.Framework;
using UnityEditor;

namespace Eidren.Tests.EditMode
{
	public sealed class EidraDepartureRewardTests
	{
		private const string TerrockPath = "Assets/_Game/Data/Enemies/Enemy_Terrock.asset";
		private const string NoctarionPath = "Assets/_Game/Data/Enemies/Enemy_Noctarion.asset";

		[TestCase(TerrockPath)]
		[TestCase(NoctarionPath)]
		public void FangbareWeltEidra_HabenPositiveKampfbelohnung(string assetPath)
		{
			EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(assetPath);
			Assert.That(definition, Is.Not.Null, "Gegnerdefinition fehlt: " + assetPath);
			Assert.That(definition.ExperienceReward, Is.GreaterThan(0),
				"Ein im Kampf besiegbares Welt-Eidra braucht eine positive XP-Belohnung");
		}

		[Test]
		public void Fluchtbelohnung_GibtEsNurBeimFledUebergang()
		{
			EnemyDefinition terrock = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(TerrockPath);
			Assert.That(terrock, Is.Not.Null);
			Assert.That(ZoneEidraPopulator.ResolveDepartureReward(terrock, EidraDepartureReason.Fled),
				Is.EqualTo(terrock.ExperienceReward), "Flucht durch Spielerkampf gilt als besiegt und vergibt die Belohnung");
			Assert.That(ZoneEidraPopulator.ResolveDepartureReward(terrock, EidraDepartureReason.Captured),
				Is.Zero, "Ein erfolgreicher Fang darf nicht dieselbe Kampfbelohnung erhalten");
			Assert.That(ZoneEidraPopulator.ResolveDepartureReward(null, EidraDepartureReason.Fled),
				Is.Zero, "Ohne Definition gibt es keine Belohnung");
		}
	}
}
