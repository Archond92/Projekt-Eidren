using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class EidraForgeDungeonService
	{
		public const int CooldownHours = 24;

		public const string DungeonId = "dungeon.eidra_forge";

		public const string SceneKey = "EidraForge";

		private EidraForgeRunState _state = EidraForgeRunState.CreateUnstarted();

		private static readonly InventoryItemAmount[] ResetCosts = new InventoryItemAmount[3]
		{
			new InventoryItemAmount("plank", 10),
			new InventoryItemAmount("stone_block", 8),
			new InventoryItemAmount("copper_bar", 5)
		};

		public long ReadyUtcTicks => (_state.CompletedUtcTicks <= 0) ? 0 : Math.Min(DateTime.MaxValue.Ticks, _state.CompletedUtcTicks + TimeSpan.FromHours(24.0).Ticks);

		public bool IsIgnivarCaptured => _state.IgnivarCaptured;

		/// <summary>Ist der Formwall im Einbruch aufgebaut? Ueberdauert Laeufe.</summary>
		public bool IstFormwallGebaut => _state.FormwallGebaut;

		/// <summary>Steht der Ertrag dieses Laufs noch zur Abholung bereit?</summary>
		public bool IstFormwallErtragOffen => _state.FormwallErtragOffen;

		/* Kosten laut SCHMIEDE_ENTWURF.md Abschnitt 6. Die sechs Beschlaege sind
		   kein Balancehebel - der Wall zahlt sich ohnehin nach drei Laeufen aus -,
		   sondern eine Abwaegung: Kosten und Ertrag sind dieselbe Waehrung, und ein
		   Eisenteil kostet genau zwei. Wer den Wall baut, verzichtet auf drei
		   Ruestungsstuecke. */
		private static readonly InventoryItemAmount[] FormwallKosten = new InventoryItemAmount[4]
		{
			new InventoryItemAmount("stone_block", 24),
			new InventoryItemAmount("plank", 16),
			new InventoryItemAmount("copper_bar", 10),
			new InventoryItemAmount("smithing_fitting", 6)
		};

		private static readonly string[] NestWaechter = new string[4]
		{
			"forge.enemy.ash_runner.03",
			"forge.enemy.ash_runner.04",
			"forge.enemy.ash_runner.05",
			"forge.enemy.ash_runner.06"
		};

		public const int FormwallErtrag = 2;

		/// <summary>Baukosten, damit die Anzeige benennen kann, was noch fehlt.</summary>
		public static IReadOnlyList<InventoryItemAmount> FormwallKostenListe => FormwallKosten;

		/// <summary>
		/// Der Bauplatz ist gesperrt, solange die vier AshRunner im Schutt nisten.
		/// Damit ist der Einbruch eine Aufgabe und kein Automat: erst raeumen,
		/// dann bauen.
		/// </summary>
		public bool IstBauplatzFrei()
		{
			foreach (string spawnId in NestWaechter)
			{
				if (!IsEnemyDefeated(spawnId))
				{
					return false;
				}
			}
			return true;
		}

		public bool TryBaueFormwall(PlayerInventory inventory, out InventoryTransactionFailure failure)
		{
			failure = InventoryTransactionFailure.InvalidRequest;
			if (inventory == null || _state.Status != EidraForgeRunStatus.InProgress
				|| _state.FormwallGebaut || !IstBauplatzFrei())
			{
				return false;
			}
			if (!inventory.TryApplyTransaction(FormwallKosten, string.Empty, 0, out failure))
			{
				return false;
			}
			_state.FormwallGebaut = true;
			/* Im Bau-Lauf selbst gibt es noch nichts abzuholen: sonst kaemen die
			   investierten Beschlaege sofort zurueck und die Kosten waeren keine.
			   Der Ertrag beginnt mit dem naechsten Lauf. */
			_state.FormwallErtragOffen = false;
			return true;
		}

		/// <summary>
		/// Holt den Ertrag des fertigen Formwalls ab: zwei Schmiedebeschlaege,
		/// einmal pro Lauf. Zum Vergleich liefern die Truhen im Mittel 0,45 pro
		/// Lauf - der Ausbau macht aus Glueck Verlaesslichkeit.
		/// </summary>
		public bool TryErnteFormwall(PlayerInventory inventory)
		{
			if (inventory == null || _state.Status != EidraForgeRunStatus.InProgress
				|| !_state.FormwallGebaut || !_state.FormwallErtragOffen)
			{
				return false;
			}
			/* PlayerInventory.Add liefert den Rest, der nicht mehr hineinpasste -
			   bei vollstaendiger Aufnahme also 0. Passt der Ertrag nicht, bleibt
			   er am Wall stehen und geht nicht verloren. */
			if (inventory.Add("smithing_fitting", FormwallErtrag) != 0)
			{
				return false;
			}
			_state.FormwallErtragOffen = false;
			return true;
		}

		internal EidraForgeRunState MutableState => _state;

		public EidraForgeAccessState GetAccessState(DateTime utcNow)
		{
			if (utcNow.Kind != DateTimeKind.Utc)
			{
				throw new ArgumentException("Dungeon time must be UTC.", "utcNow");
			}
			if (_state.Status == EidraForgeRunStatus.NeverStarted)
			{
				return EidraForgeAccessState.FirstRunAvailable;
			}
			if (_state.Status == EidraForgeRunStatus.InProgress)
			{
				return EidraForgeAccessState.ResumeRun;
			}
			return (utcNow.Ticks < ReadyUtcTicks) ? EidraForgeAccessState.Cooldown : EidraForgeAccessState.ResetAvailable;
		}

		public ForgeDropState[] GetDrops()
		{
			return DungeonStateCopy.Drops(_state.Drops);
		}

		public ItemStack[] GetRecoverySlots()
		{
			return DungeonStateCopy.Stacks(_state.RecoverySlots);
		}

		public bool IsEnemyDefeated(string spawnId)
		{
			ForgeEnemyState[] array = _state.Enemies ?? Array.Empty<ForgeEnemyState>();
			foreach (ForgeEnemyState forgeEnemyState in array)
			{
				if (forgeEnemyState != null && forgeEnemyState.Defeated && string.Equals(forgeEnemyState.SpawnId, spawnId, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		public bool TryUpdateRecoverySlots(ItemStack[] remaining)
		{
			if (!DoesNotIncrease(_state.RecoverySlots, remaining))
			{
				return false;
			}
			_state.RecoverySlots = DungeonStateCopy.Stacks(remaining);
			return true;
		}

		public bool TryBeginFirstRun(int seed)
		{
			if (_state.Status != EidraForgeRunStatus.NeverStarted)
			{
				return false;
			}
			BeginRun(seed);
			return true;
		}

		public bool TryComplete(DateTime utcNow, out bool firstCompletion)
		{
			firstCompletion = false;
			if (_state.Status != EidraForgeRunStatus.InProgress || utcNow.Kind != DateTimeKind.Utc)
			{
				return false;
			}
			firstCompletion = !_state.FirstCompletionGranted;
			_state.FirstCompletionGranted = true;
			_state.Status = EidraForgeRunStatus.Completed;
			_state.CompletedUtcTicks = utcNow.Ticks;
			return true;
		}

		public bool TryPaidReset(PlayerInventory inventory, DateTime utcNow, int nextSeed, bool playerInside, ItemStack[] recoveryContents, out InventoryTransactionFailure failure)
		{
			failure = InventoryTransactionFailure.InvalidRequest;
			if (inventory == null || playerInside || GetAccessState(utcNow) != EidraForgeAccessState.ResetAvailable)
			{
				return false;
			}
			if (HasContents(_state.RecoverySlots) && HasContents(recoveryContents))
			{
				return false;
			}
			if (!inventory.TryApplyTransaction(ResetCosts, string.Empty, 0, out failure))
			{
				return false;
			}
			if (HasContents(recoveryContents))
			{
				_state.RecoverySlots = DungeonStateCopy.Stacks(recoveryContents);
			}
			BeginRun(nextSeed);
			return true;
		}

		public bool MarkIgnivarCaptured()
		{
			if (_state.IgnivarCaptured)
			{
				return false;
			}
			_state.IgnivarCaptured = true;
			_state.TeamSelectionPending = true;
			return true;
		}

		public bool ConsumeTeamSelection()
		{
			if (!_state.TeamSelectionPending)
			{
				return false;
			}
			_state.TeamSelectionPending = false;
			return true;
		}

		internal EidraForgeRunState Capture()
		{
			return _state.Copy();
		}

		internal void Restore(EidraForgeRunState state)
		{
			_state = state?.Copy() ?? EidraForgeRunState.CreateUnstarted();
			// Nach-Release-Fix (18.08.2026): Läufe aus älteren Fassungen
			// kennen neue Gegner nicht — ohne Reparatur spawnen sie in diesem
			// Lauf nie („der Wächter spawnt nicht").
			if (_state.Status == EidraForgeRunStatus.InProgress)
			{
				_state.Enemies = EidraForgePopulationRules.RepairRoster(_state.Enemies);
			}
		}

		internal void ResetForNewGame()
		{
			_state = EidraForgeRunState.CreateUnstarted();
		}

		private void BeginRun(int seed)
		{
			bool ignivarCaptured = _state.IgnivarCaptured;
			bool firstCompletionGranted = _state.FirstCompletionGranted;
			/* Der Ausbau ueberlebt den Lauf; der Ertrag steht zu jedem Laufbeginn
			   wieder bereit, sammelt sich aber nicht an. */
			bool formwallGebaut = _state.FormwallGebaut;
			ForgeRewardChestState[] rewardChests = _state.RewardChests;
			ItemStack[] recoverySlots = _state.RecoverySlots;
			_state = new EidraForgeRunState
			{
				Status = EidraForgeRunStatus.InProgress,
				RunId = $"eidra_forge.{Math.Abs(seed)}.{Guid.NewGuid():N}",
				LootSeed = seed,
				IgnivarCaptured = ignivarCaptured,
				FirstCompletionGranted = firstCompletionGranted,
				FormwallGebaut = formwallGebaut,
				FormwallErtragOffen = formwallGebaut,
				RepeatRun = firstCompletionGranted,
				RewardChests = DungeonStateCopy.Rewards(rewardChests),
				RecoverySlots = DungeonStateCopy.Stacks(recoverySlots),
				Chests = EidraForgeContainerRules.CreateEmptyRunChests(),
				Enemies = EidraForgePopulationRules.CreateEnemyStates()
			};
		}

		private static bool HasContents(ItemStack[] slots)
		{
			if (slots == null)
			{
				return false;
			}
			foreach (ItemStack itemStack in slots)
			{
				if (!itemStack.IsEmpty)
				{
					return true;
				}
			}
			return false;
		}

		private static bool DoesNotIncrease(ItemStack[] before, ItemStack[] after)
		{
			if (after == null)
			{
				return false;
			}
			List<ItemStack> list = new List<ItemStack>(before ?? Array.Empty<ItemStack>());
			for (int i = 0; i < after.Length; i++)
			{
				ItemStack candidate = after[i];
				if (!candidate.IsEmpty)
				{
					int num = list.FindIndex((ItemStack old) => string.Equals(old.ItemId, candidate.ItemId, StringComparison.Ordinal) && string.Equals(old.InstanceId, candidate.InstanceId, StringComparison.Ordinal) && old.Durability == candidate.Durability && old.Quantity >= candidate.Quantity);
					if (num < 0)
					{
						return false;
					}
					list.RemoveAt(num);
				}
			}
			return true;
		}
	}
}
