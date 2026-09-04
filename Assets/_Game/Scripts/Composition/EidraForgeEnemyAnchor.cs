using Eidren.Data;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class EidraForgeEnemyAnchor : MonoBehaviour
	{
		[SerializeField]
		private string spawnId = string.Empty;

		[SerializeField]
		private GameObject enemyPrefab;

		[SerializeField]
		private EnemyDefinition enemyDefinition;

		[SerializeField]
		private CoreGuardianData coreGuardianData;

		public string SpawnId => spawnId ?? string.Empty;

		public GameObject EnemyPrefab => enemyPrefab;

		public EnemyDefinition EnemyDefinition => enemyDefinition;

		public CoreGuardianData CoreGuardianData => coreGuardianData;

		public void Configure(string configuredSpawnId, GameObject configuredPrefab, EnemyDefinition configuredDefinition = null, CoreGuardianData configuredCoreData = null)
		{
			spawnId = configuredSpawnId ?? string.Empty;
			enemyPrefab = configuredPrefab;
			enemyDefinition = configuredDefinition;
			coreGuardianData = configuredCoreData;
		}
	}
}
