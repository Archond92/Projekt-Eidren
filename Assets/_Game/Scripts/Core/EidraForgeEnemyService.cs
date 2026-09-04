using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class EidraForgeEnemyService
	{
		private readonly EidraForgeDungeonService _dungeon;

		public EidraForgeEnemyService(EidraForgeDungeonService dungeon)
		{
			_dungeon = dungeon ?? throw new ArgumentNullException("dungeon");
		}

		public ForgeEnemyState[] GetAll()
		{
			return DungeonStateCopy.Enemies(_dungeon.MutableState.Enemies);
		}

		public bool Update(string spawnId, float health, float stagger)
		{
			ForgeEnemyState forgeEnemyState = Find(spawnId);
			if (forgeEnemyState == null || forgeEnemyState.Defeated)
			{
				return false;
			}
			forgeEnemyState.Health = Mathf.Max(0f, health);
			forgeEnemyState.Stagger = Mathf.Max(0f, stagger);
			return true;
		}

		public bool TryRecordDefeat(string spawnId, Vector3 dropPosition, ContentDatabase content, out EidraForgeEnemyReward reward)
		{
			reward = default(EidraForgeEnemyReward);
			ForgeEnemyState forgeEnemyState = Find(spawnId);
			if (forgeEnemyState == null || forgeEnemyState.Defeated || !EidraForgePopulationRules.TryGetReward(spawnId, out var experience, out var marks))
			{
				return false;
			}
			if (!(content != null))
			{
				throw new ArgumentNullException("content");
			}
			ItemDefinition item = content.GetItem("smithing_mark");
			ItemStack itemStack = ItemStack.Create(item, marks);
			forgeEnemyState.Health = 0f;
			forgeEnemyState.Defeated = true;
			forgeEnemyState.MarkDropped = true;
			List<ForgeDropState> list = new List<ForgeDropState>(_dungeon.MutableState.Drops ?? Array.Empty<ForgeDropState>())
			{
				new ForgeDropState
				{
					DropId = spawnId + ".smithing_mark",
					Stack = itemStack,
					Position = dropPosition
				}
			};
			_dungeon.MutableState.Drops = list.ToArray();
			reward = new EidraForgeEnemyReward(experience, itemStack);
			return true;
		}

		public bool RemoveDrop(string dropId)
		{
			ForgeDropState[] drops = _dungeon.MutableState.Drops;
			if (drops == null)
			{
				return false;
			}
			List<ForgeDropState> list = new List<ForgeDropState>(drops.Length);
			bool flag = false;
			ForgeDropState[] array = drops;
			foreach (ForgeDropState forgeDropState in array)
			{
				if (!flag && forgeDropState != null && string.Equals(forgeDropState.DropId, dropId, StringComparison.Ordinal))
				{
					flag = true;
				}
				else
				{
					list.Add(forgeDropState);
				}
			}
			if (flag)
			{
				_dungeon.MutableState.Drops = list.ToArray();
			}
			return flag;
		}

		private ForgeEnemyState Find(string spawnId)
		{
			ForgeEnemyState[] array = _dungeon.MutableState.Enemies ?? Array.Empty<ForgeEnemyState>();
			foreach (ForgeEnemyState forgeEnemyState in array)
			{
				if (forgeEnemyState != null && string.Equals(forgeEnemyState.SpawnId, spawnId, StringComparison.Ordinal))
				{
					return forgeEnemyState;
				}
			}
			return null;
		}
	}
}
