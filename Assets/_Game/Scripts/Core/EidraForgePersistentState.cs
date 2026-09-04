using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	internal static class DungeonStateCopy
	{
		internal static ItemStack[] Stacks(ItemStack[] source)
		{
			return Copy(source, (ItemStack value) => value.Copy());
		}

		internal static ForgeEnemyState[] Enemies(ForgeEnemyState[] source)
		{
			return Copy(source, (ForgeEnemyState value) => value.Copy());
		}

		internal static ForgeContainerState[] Chests(ForgeContainerState[] source)
		{
			return Copy(source, (ForgeContainerState value) => value.Copy());
		}

		internal static ForgeDropState[] Drops(ForgeDropState[] source)
		{
			return Copy(source, (ForgeDropState value) => value.Copy());
		}

		internal static ForgeRewardChestState[] Rewards(ForgeRewardChestState[] source)
		{
			return Copy(source, (ForgeRewardChestState value) => value.Copy());
		}

		private static T[] Copy<T>(T[] source, Func<T, T> copy)
		{
			if (source == null)
			{
				return Array.Empty<T>();
			}
			T[] array = new T[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = ((source[i] == null) ? default(T) : copy(source[i]));
			}
			return array;
		}
	}

	public sealed class ForgeContainerState
	{
		public string InstanceId = string.Empty;

		public int Family;

		public bool Opened;

		public ItemStack[] Slots = Array.Empty<ItemStack>();

		public ForgeContainerState Copy()
		{
			return new ForgeContainerState
			{
				InstanceId = (InstanceId ?? string.Empty),
				Family = Family,
				Opened = Opened,
				Slots = DungeonStateCopy.Stacks(Slots)
			};
		}
	}

	public sealed class ForgeDropState
	{
		public string DropId = string.Empty;

		public ItemStack Stack = ItemStack.Empty;

		public Vector3 Position;

		public ForgeDropState Copy()
		{
			return new ForgeDropState
			{
				DropId = (DropId ?? string.Empty),
				Stack = Stack.Copy(),
				Position = Position
			};
		}
	}

	public sealed class ForgeEnemyState
	{
		public string SpawnId = string.Empty;

		public float Health;

		public float Stagger;

		public bool Defeated;

		public bool MarkDropped;

		public ForgeEnemyState Copy()
		{
			return (ForgeEnemyState)MemberwiseClone();
		}
	}

	public sealed class ForgeRewardChestState
	{
		public int Size;

		public int PurchaseIndex;

		public ItemStack[] Slots = Array.Empty<ItemStack>();

		public ForgeRewardChestState Copy()
		{
			return new ForgeRewardChestState
			{
				Size = Size,
				PurchaseIndex = PurchaseIndex,
				Slots = DungeonStateCopy.Stacks(Slots)
			};
		}
	}
}
