using Eidren.Core.Services;
using Eidren.Interaction;
using UnityEngine;

namespace Eidren.Composition
{
	public static class DeathBagSpawner
	{
		public const string PrefabResourcePath = "Prefabs/DeathBag";

		public static StorageContainer TrySpawn(GameSession session, string activeSceneName)
		{
			if (session == null || string.IsNullOrWhiteSpace(activeSceneName) || !session.DeathBag.LiesIn(activeSceneName) || !session.HasDeathBagContents())
			{
				return null;
			}
			StorageContainer storageContainer = Resources.Load<StorageContainer>("Prefabs/DeathBag");
			if (storageContainer == null)
			{
				Debug.LogError("Eidren: death bag prefab is missing at Resources/Prefabs/DeathBag. Run Eidren/Storage/Build V0.1.");
				return null;
			}
			StorageContainer storageContainer2 = Object.Instantiate(storageContainer, session.DeathBag.Position, Quaternion.identity);
			storageContainer2.name = "DeathBag";
			return storageContainer2;
		}
	}
}
