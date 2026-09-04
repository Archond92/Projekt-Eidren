using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Enemies/Enemy Definition")]
	public sealed class EnemyDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[SerializeField]
		[Min(1f)]
		private float maximumHealth = 100f;

		[SerializeField]
		[Min(1f)]
		private float maximumStagger = 50f;

		[SerializeField]
		[Min(0.01f)]
		private float staggerDuration = 1f;

		[SerializeField]
		[Min(0f)]
		private float attackDamage = 10f;

		[SerializeField]
		[Range(0f, 0.95f)]
		private float protection;

		[SerializeField]
		private EnemyCombatStyle combatStyle;

		[SerializeField]
		[Min(0f)]
		private float secondaryAttackDamage;

		[SerializeField]
		[Min(0f)]
		private float postStaggerResistanceDuration;

		[SerializeField]
		[Range(0f, 1f)]
		private float postStaggerDamageMultiplier = 1f;

		[SerializeField]
		[Min(0.01f)]
		private float telegraphDuration = 0.5f;

		[SerializeField]
		[Min(0.01f)]
		private float attackWindowDuration = 0.18f;

		[SerializeField]
		[Min(0.01f)]
		private float recoveryDuration = 0.8f;

		[SerializeField]
		[Min(0f)]
		private float deathDisableDelay = 2.5f;

		[SerializeField]
		[Min(0f)]
		private int experienceReward;

		[SerializeField]
		private LootTableDefinition lootTable;

		[SerializeField]
		private EnemyNavigationData navigation;

		[SerializeField]
		private EidraData capturableEidra;

		[SerializeField]
		private GameObject prefab;

		public string Id => id ?? string.Empty;

		public string DisplayName => displayName ?? string.Empty;

		public float MaximumHealth => Mathf.Max(1f, maximumHealth);

		public float MaximumStagger => Mathf.Max(1f, maximumStagger);

		public float StaggerDuration => Mathf.Max(0.01f, staggerDuration);

		public float AttackDamage => Mathf.Max(0f, attackDamage);

		public float Protection => Mathf.Clamp(protection, 0f, 0.95f);

		public EnemyCombatStyle CombatStyle => combatStyle;

		public float SecondaryAttackDamage => Mathf.Max(0f, secondaryAttackDamage);

		public float PostStaggerResistanceDuration => Mathf.Max(0f, postStaggerResistanceDuration);

		public float PostStaggerDamageMultiplier => Mathf.Clamp01(postStaggerDamageMultiplier);

		public float TelegraphDuration => Mathf.Max(0.01f, telegraphDuration);

		public float AttackWindowDuration => Mathf.Max(0.01f, attackWindowDuration);

		public float RecoveryDuration => Mathf.Max(0.01f, recoveryDuration);

		public float DeathDisableDelay => Mathf.Max(0f, deathDisableDelay);

		public int ExperienceReward => Mathf.Max(0, experienceReward);

		public LootTableDefinition LootTable => lootTable;

		public EnemyNavigationData Navigation => navigation;

		public EidraData CapturableEidra => capturableEidra;

		public bool IsCapturableEidra => capturableEidra != null;

		public GameObject Prefab => prefab;

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(Id))
			{
				list.Add("Stable enemy ID is empty.");
			}
			if (string.IsNullOrWhiteSpace(DisplayName))
			{
				list.Add("Enemy '" + base.name + "' has no display name.");
			}
			if (maximumHealth <= 0f)
			{
				list.Add("Enemy '" + base.name + "' requires positive health.");
			}
			if (maximumStagger <= 0f)
			{
				list.Add("Enemy '" + base.name + "' requires positive stagger.");
			}
			if (attackDamage < 0f)
			{
				list.Add("Enemy '" + base.name + "' has negative attack damage.");
			}
			if (navigation.MoveSpeed <= 0f)
			{
				list.Add("Enemy '" + base.name + "' requires positive move speed.");
			}
			if (navigation.DetectionRange <= 0f)
			{
				list.Add("Enemy '" + base.name + "' requires a detection range.");
			}
			if (navigation.LeashRange <= 0f)
			{
				list.Add("Enemy '" + base.name + "' requires a leash range.");
			}
			if (navigation.AttackRange <= 0f)
			{
				list.Add("Enemy '" + base.name + "' requires an attack range.");
			}
			if (navigation.AttackRange >= navigation.DetectionRange)
			{
				list.Add("Enemy '" + base.name + "' attack range must be below detection range.");
			}
			if (IsCapturableEidra)
			{
				if (lootTable != null)
				{
					list.Add("Enemy '" + base.name + "' is a capturable eidra and must not carry a loot table — eidra never die.");
				}
				if (prefab == null)
				{
					list.Add("Enemy '" + base.name + "' is a capturable eidra and needs a prefab; the zone builds it from the seed, not from the scene asset.");
				}
				list.AddRange(capturableEidra.GetValidationErrors());
			}
			return list.ToArray();
		}
	}

	public static class EidraEnemyIds
	{
		public const string Terrock = "enemy.eidra.terrock";

		public const string Noctarion = "enemy.eidra.noctarion";

		public const string Ignivar = "enemy.eidra.ignivar";
	}

	public static class EnemyIds
	{
		public const string Wildling = "enemy.wildling";

		public const string Riftling = "enemy.riftling";

		public const string RootCharger = "enemy.root_charger";

		public const string MoorThrower = "enemy.moor_thrower";

		public const string GraniteShell = "enemy.granite_shell";

		public const string RiftGuardian = "enemy.rift_guardian";

		public const string EmberEater = "enemy.ember_eater";

		public const string AshRunner = "enemy.ash_runner";

		public const string ForgeGuardian = "enemy.forge_guardian";

		public const string SealGuardian = "enemy.seal_guardian";

		public const string CoreGuardian = "enemy.core_guardian";
	}
}
