using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Editor;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class EidraForgeDungeonTests
{
	private sealed class ContentFixture : IDisposable
	{
		private readonly GameObject _root;

		public ContentDatabase Database { get; }

		public EidraForgeLootProfile Profile { get; }

		public PlayerInventory Inventory { get; }

		public ContentFixture()
		{
			EidraForgeLootAssetBuilder.Build();
			_root = new GameObject("ForgeDungeonContentFixture");
			ContentDatabase content = _root.AddComponent<ContentDatabase>();
			ItemDefinition[] items = (from value in AssetDatabase.FindAssets("t:ItemDefinition", new string[1] { "Assets/_Game/Data/Items" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemDefinition>)
				where value != null
				select value).ToArray();
			content.ConfigureItems(items);
			Database = content;
			Profile = AssetDatabase.LoadAssetAtPath<EidraForgeLootProfile>("Assets/_Game/Resources/Data/EidraForgeLoot_V02.asset");
			Assert.That<EidraForgeLootProfile>(Profile, (IResolveConstraint)(object)Is.Not.Null);
			Inventory = new PlayerInventory();
			Inventory.Configure(content);
		}

		public void Dispose()
		{
			UnityEngine.Object.DestroyImmediate(_root);
		}
	}

	[Test]
	public void Lifecycle_ResumesThenWaitsExactlyTwentyFourHours()
	{
		EidraForgeDungeonService eidraForgeDungeonService = new EidraForgeDungeonService();
		DateTime completed = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
		Assert.That<EidraForgeAccessState>(eidraForgeDungeonService.GetAccessState(completed), (IResolveConstraint)(object)Is.EqualTo((object)EidraForgeAccessState.FirstRunAvailable));
		Assert.That<bool>(eidraForgeDungeonService.TryBeginFirstRun(1234), (IResolveConstraint)(object)Is.True);
		Assert.That<EidraForgeAccessState>(eidraForgeDungeonService.GetAccessState(completed), (IResolveConstraint)(object)Is.EqualTo((object)EidraForgeAccessState.ResumeRun));
		Assert.That<bool>(eidraForgeDungeonService.TryBeginFirstRun(999), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(eidraForgeDungeonService.TryComplete(completed, out var first), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(first, (IResolveConstraint)(object)Is.True);
		Assert.That<EidraForgeAccessState>(eidraForgeDungeonService.GetAccessState(completed.AddHours(23.999)), (IResolveConstraint)(object)Is.EqualTo((object)EidraForgeAccessState.Cooldown));
		Assert.That<EidraForgeAccessState>(eidraForgeDungeonService.GetAccessState(completed.AddHours(24.0)), (IResolveConstraint)(object)Is.EqualTo((object)EidraForgeAccessState.ResetAvailable));
	}

	[Test]
	public void PaidReset_IsAtomicInventoryOnlyAndPreservesOneTimeFlags()
	{
		using ContentFixture fixture = new ContentFixture();
		PlayerInventory inventory = fixture.Inventory;
		inventory.Add("plank", 10);
		inventory.Add("stone_block", 8);
		inventory.Add("copper_bar", 4);
		DateTime completed;
		EidraForgeDungeonService eidraForgeDungeonService = CompletedService(out completed);
		eidraForgeDungeonService.MarkIgnivarCaptured();
		Assert.That<bool>(eidraForgeDungeonService.TryPaidReset(inventory, completed.AddHours(24.0), 2, playerInside: false, Array.Empty<ItemStack>(), out var failure), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(inventory.GetTotalAmount("plank"), (IResolveConstraint)(object)Is.EqualTo((object)10));
		inventory.Add("copper_bar", 1);
		Assert.That<bool>(eidraForgeDungeonService.TryPaidReset(inventory, completed.AddHours(24.0), 2, playerInside: true, Array.Empty<ItemStack>(), out failure), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(eidraForgeDungeonService.TryPaidReset(inventory, completed.AddHours(24.0), 2, playerInside: false, Array.Empty<ItemStack>(), out failure), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(inventory.GetTotalAmount("plank"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(inventory.GetTotalAmount("stone_block"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(inventory.GetTotalAmount("copper_bar"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<EidraForgeAccessState>(eidraForgeDungeonService.GetAccessState(completed.AddHours(24.0)), (IResolveConstraint)(object)Is.EqualTo((object)EidraForgeAccessState.ResumeRun));
		Assert.That<bool>(eidraForgeDungeonService.MarkIgnivarCaptured(), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void BossCycle_UsesThreeAttacksAndExactOpenWindows()
	{
		CoreGuardianCycle cycle = new CoreGuardianCycle();
		Assert.That<bool>(cycle.RecordClosedAttack(), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(cycle.RecordClosedAttack(), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(cycle.RecordClosedAttack(), (IResolveConstraint)(object)Is.True);
		Assert.That<CoreGuardianPhase>(cycle.Phase, (IResolveConstraint)(object)Is.EqualTo((object)CoreGuardianPhase.Overheating));
		Assert.That<bool>(cycle.FinishOverheat(), (IResolveConstraint)(object)Is.True);
		Assert.That<float>(cycle.RemainingSeconds, (IResolveConstraint)(object)Is.EqualTo((object)8f));
		for (int i = 0; i < 16; i++)
		{
			cycle.Tick(0.5f);
		}
		Assert.That<CoreGuardianPhase>(cycle.Phase, (IResolveConstraint)(object)Is.EqualTo((object)CoreGuardianPhase.Closed));
		Assert.That<bool>(cycle.AddStagger(400f), (IResolveConstraint)(object)Is.True);
		Assert.That<float>(cycle.RemainingSeconds, (IResolveConstraint)(object)Is.EqualTo((object)6f));
		Assert.That<float>(cycle.ProtectionWithMoltenBrand(active: true), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void BalanceAndIgnivarRules_MatchCanonicalTotals()
	{
		Assert.That<int>(EidraForgeBalance.TotalMarks(includeSealGuardian: true), (IResolveConstraint)(object)Is.EqualTo((object)45));
		Assert.That<int>(EidraForgeBalance.TotalMarks(includeSealGuardian: false), (IResolveConstraint)(object)Is.EqualTo((object)40));
		Assert.That<int>(EidraForgeBalance.TotalExperience(firstCompletion: true), (IResolveConstraint)(object)Is.EqualTo((object)1936));
		Assert.That<int>(EidraForgeBalance.TotalExperience(firstCompletion: false), (IResolveConstraint)(object)Is.EqualTo((object)1036));
		Assert.That<float>(IgnivarAbilityRules.ProtectionWithMoltenBrand(0.35f), (IResolveConstraint)(object)Is.EqualTo((object)0.2f).Within((object)0.0001f));
		EmberCircleDamageBudget budget = new EmberCircleDamageBudget();
		Assert.That<float>(Enumerable.Range(0, 8).Sum((int _) => budget.TryApplyTick("enemy.01")), (IResolveConstraint)(object)Is.EqualTo((object)30f));
	}

	[Test]
	public void Population_ProducesExactlyTwentyFourPhysicalMarkStacks()
	{
		using ContentFixture fixture = new ContentFixture();
		EidraForgeDungeonService eidraForgeDungeonService = new EidraForgeDungeonService();
		EidraForgeEnemyService enemies = new EidraForgeEnemyService(eidraForgeDungeonService);
		eidraForgeDungeonService.TryBeginFirstRun(201);
		ForgeEnemyState[] all = enemies.GetAll();
		Assert.That<ForgeEnemyState[]>(all, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)24));
		int experience = 0;
		int marks = 0;
		ForgeEnemyState[] array = all;
		foreach (ForgeEnemyState enemy in array)
		{
			Assert.That<bool>(enemies.TryRecordDefeat(enemy.SpawnId, Vector3.zero, fixture.Database, out var reward), (IResolveConstraint)(object)Is.True);
			experience += reward.Experience;
			marks += reward.MarkDrop.Quantity;
			Assert.That<bool>(enemies.TryRecordDefeat(enemy.SpawnId, Vector3.zero, fixture.Database, out var _), (IResolveConstraint)(object)Is.False);
		}
		Assert.That<int>(experience, (IResolveConstraint)(object)Is.EqualTo((object)1036));
		Assert.That<int>(marks, (IResolveConstraint)(object)Is.EqualTo((object)45));
		Assert.That<int>(enemies.GetAll().Count((ForgeEnemyState value) => value.MarkDropped), (IResolveConstraint)(object)Is.EqualTo((object)24));
	}

	[Test]
	public void VersionTwelveMigration_StartsFreeUnstartedRun()
	{
		Assert.That<bool>(SaveGameMigration.TryMigrate(new SaveGameData
		{
			saveVersion = 12,
			player = new SavePlayerData(),
			world = new SaveWorldData(),
			zones = Array.Empty<SaveZoneStateData>()
		}, out var result, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(result.saveVersion, (IResolveConstraint)(object)Is.EqualTo((object)15));
		Assert.That<int>(result.eidraForge.status, (IResolveConstraint)(object)Is.EqualTo((object)0));
		Assert.That<string>(result.eidraForge.runId, (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void RunChests_AreCanonicalSeededAndStable()
	{
		using ContentFixture fixture = new ContentFixture();
		EidraForgeDungeonService eidraForgeDungeonService = new EidraForgeDungeonService();
		EidraForgeChestService chests = new EidraForgeChestService(eidraForgeDungeonService);
		Assert.That<bool>(eidraForgeDungeonService.TryBeginFirstRun(918273), (IResolveConstraint)(object)Is.True);
		chests.EnsureRunChestsGenerated(fixture.Profile, fixture.Database);
		string first = ChestSignature(chests);
		chests.EnsureRunChestsGenerated(fixture.Profile, fixture.Database);
		Assert.That<string>(ChestSignature(chests), (IResolveConstraint)(object)Is.EqualTo((object)first));
		Assert.That<int>(first.Split('|', StringSplitOptions.None).Length, (IResolveConstraint)(object)Is.EqualTo((object)7));
		Assert.That<int>(chests.GetRunChest("forge.supply.01").Slots.Length, (IResolveConstraint)(object)Is.InRange((IComparable)2, (IComparable)3));
		Assert.That<int>(chests.GetRunChest("forge.completion.01").Slots.Length, (IResolveConstraint)(object)Is.InRange((IComparable)3, (IComparable)4));
	}

	[Test]
	public void RewardChest_PaymentIsAtomicAndContentBlocksRepurchase()
	{
		using ContentFixture fixture = new ContentFixture();
		EidraForgeDungeonService eidraForgeDungeonService = new EidraForgeDungeonService();
		EidraForgeChestService chests = new EidraForgeChestService(eidraForgeDungeonService);
		eidraForgeDungeonService.TryBeginFirstRun(77);
		fixture.Inventory.Add("smithing_mark", 30);
		Assert.That<bool>(chests.TryPurchaseRewardChest(ForgeRewardChestSize.Small, fixture.Inventory, fixture.Profile, fixture.Database, out var failure), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(fixture.Inventory.GetTotalAmount("smithing_mark"), (IResolveConstraint)(object)Is.EqualTo((object)15));
		Assert.That<bool>(chests.GetRewardChest(ForgeRewardChestSize.Small).Slots.Any((ItemStack value) => !value.IsEmpty), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(chests.TryPurchaseRewardChest(ForgeRewardChestSize.Small, fixture.Inventory, fixture.Profile, fixture.Database, out failure), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(fixture.Inventory.GetTotalAmount("smithing_mark"), (IResolveConstraint)(object)Is.EqualTo((object)15));
		Assert.That<bool>(chests.UpdateRewardChestContents(ForgeRewardChestSize.Small, Array.Empty<ItemStack>()), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(chests.TryPurchaseRewardChest(ForgeRewardChestSize.Small, fixture.Inventory, fixture.Profile, fixture.Database, out failure), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(fixture.Inventory.GetTotalAmount("smithing_mark"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(chests.GetRewardChest(ForgeRewardChestSize.Small).PurchaseIndex, (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void ForgeChests_RoundTripThroughSaveMapper()
	{
		using ContentFixture fixture = new ContentFixture();
		GameObject sourceRoot = new GameObject("ForgeSaveSource");
		GameObject targetRoot = new GameObject("ForgeSaveTarget");
		try
		{
			GameSession source = sourceRoot.AddComponent<GameSession>();
			source.ConfigureContentDatabase(fixture.Database);
			source.StartNewGame();
			source.EidraForge.TryBeginFirstRun(4411);
			source.EidraForgeChests.EnsureRunChestsGenerated(fixture.Profile, fixture.Database);
			source.PlayerInventory.Add("smithing_mark", 15);
			source.EidraForgeChests.TryPurchaseRewardChest(ForgeRewardChestSize.Small, source.PlayerInventory, fixture.Profile, fixture.Database, out var _);
			SaveGameMapper saveGameMapper = new SaveGameMapper(fixture.Database);
			SaveGameData save = saveGameMapper.Capture(source, "2026-08-04T00:00:00Z", "2026-08-04T00:01:00Z", "test");
			Assert.That<bool>(saveGameMapper.TryMapToRuntime(save, out var runtime, out var _, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
			GameSession gameSession = targetRoot.AddComponent<GameSession>();
			gameSession.ConfigureContentDatabase(fixture.Database);
			Assert.That<bool>(gameSession.TryRestoreRuntimeState(runtime, out error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
			Assert.That<string>(ChestSignature(gameSession.EidraForgeChests), (IResolveConstraint)(object)Is.EqualTo((object)ChestSignature(source.EidraForgeChests)));
			Assert.That<bool>(gameSession.EidraForgeChests.GetRewardChest(ForgeRewardChestSize.Small).Slots.Any((ItemStack value) => !value.IsEmpty), (IResolveConstraint)(object)Is.True);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(sourceRoot);
			UnityEngine.Object.DestroyImmediate(targetRoot);
		}
	}

	private static EidraForgeDungeonService CompletedService(out DateTime completed)
	{
		EidraForgeDungeonService eidraForgeDungeonService = new EidraForgeDungeonService();
		completed = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
		eidraForgeDungeonService.TryBeginFirstRun(1);
		eidraForgeDungeonService.TryComplete(completed, out var _);
		return eidraForgeDungeonService;
	}

	private static string ChestSignature(EidraForgeChestService chests)
	{
		string[] ids = new string[7] { "forge.supply.01", "forge.supply.02", "forge.supply.03", "forge.optional.01", "forge.optional.02", "forge.elite.01", "forge.completion.01" };
		return string.Join("|", ids.Select(delegate(string id)
		{
			ForgeContainerState runChest = chests.GetRunChest(id);
			return id + ":" + string.Join(",", runChest.Slots.Select((ItemStack slot) => $"{slot.ItemId}:{slot.Quantity}:{slot.Durability}"));
		}));
	}
}
}
