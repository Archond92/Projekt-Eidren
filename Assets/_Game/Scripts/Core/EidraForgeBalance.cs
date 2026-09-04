using System;

namespace Eidren.Core.Services
{
	public static class EidraForgeBalance
	{
		private static readonly ForgeEnemyBalance[] Entries = new ForgeEnemyBalance[5]
		{
			new ForgeEnemyBalance("enemy.ember_eater", 12, 95f, 40f, 12f, 0f, 8, 1),
			new ForgeEnemyBalance("enemy.ash_runner", 6, 180f, 70f, 22f, 0f, 20, 2),
			new ForgeEnemyBalance("enemy.forge_guardian", 4, 360f, 170f, 30f, 0.2f, 45, 2),
			new ForgeEnemyBalance("enemy.seal_guardian", 1, 750f, 240f, 30f, 0.15f, 140, 5),
			new ForgeEnemyBalance("enemy.core_guardian", 1, 3000f, 400f, 34f, 0.35f, 500, 8)
		};

		public static ForgeEnemyBalance Get(string enemyId)
		{
			ForgeEnemyBalance[] entries = Entries;
			for (int i = 0; i < entries.Length; i++)
			{
				ForgeEnemyBalance result = entries[i];
				if (string.Equals(result.Id, enemyId, StringComparison.Ordinal))
				{
					return result;
				}
			}
			throw new ArgumentException("Unknown forge enemy ID.", "enemyId");
		}

		public static int TotalMarks(bool includeSealGuardian)
		{
			if (!includeSealGuardian)
			{
				return 40;
			}
			return 45;
		}

		public static int TotalExperience(bool firstCompletion)
		{
			if (!firstCompletion)
			{
				return 1036;
			}
			return 1936;
		}
	}

	public readonly struct ForgeEnemyBalance
	{
		public string Id { get; }

		public int Count { get; }

		public float Health { get; }

		public float Stagger { get; }

		public float Damage { get; }

		public float Protection { get; }

		public int Experience { get; }

		public int Marks { get; }

		public ForgeEnemyBalance(string id, int count, float health, float stagger, float damage, float protection, int experience, int marks)
		{
			Id = id;
			Count = count;
			Health = health;
			Stagger = stagger;
			Damage = damage;
			Protection = protection;
			Experience = experience;
			Marks = marks;
		}
	}
}
