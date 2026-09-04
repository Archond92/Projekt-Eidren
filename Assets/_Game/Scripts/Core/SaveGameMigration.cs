using Eidren.Core.BuildGrid;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public static class SaveGameMigration
	{
		public const int VersionWithDurability = 2;

		public const int VersionWithEquipment = 3;

		public const int VersionWithZoneStates = 4;

		public const int VersionWithProgression = 5;

		public const int VersionWithTechnologyTree = 6;

		public const int VersionWithBuildings = 7;

		public const int VersionWithProduction = 8;

		public const int VersionWithEidraRoster = 9;

		public const int VersionWithBuildGrid = 10;

		public const int VersionWithDurableItemInstances = 11;

		public const int VersionWithPersistentWorldChests = 12;

		public const int VersionWithEidraForgeDungeon = 13;


		private static void MigrateSecondEidraSlot(SaveProgressionData progression)
		{
			if (progression?.unlockedTechnologyNodeIds == null)
			{
				return;
			}
			HashSet<string> nodes = new HashSet<string>(progression.unlockedTechnologyNodeIds, StringComparer.Ordinal);
			if (nodes.Remove("technology.12.second_eidra_slot"))
			{
				progression.availableTechnologyPoints = Math.Max(0, progression.availableTechnologyPoints + 1);
				progression.spentTechnologyPoints = Math.Max(0, progression.spentTechnologyPoints - 1);
			}
			string[] sorted = new string[nodes.Count];
			nodes.CopyTo(sorted);
			Array.Sort(sorted, StringComparer.Ordinal);
			progression.unlockedTechnologyNodeIds = sorted;
		}

		private static void MigrateBuildingsToGrid(SaveBuildingData[] buildings)
		{
			if (buildings == null)
			{
				return;
			}
			foreach (SaveBuildingData saveBuildingData in buildings)
			{
				if (saveBuildingData != null)
				{
					BuildGridOrigin homeBase = BuildGridOrigin.HomeBase;
					GridCoordinate gridCoordinate = homeBase.ToCell(saveBuildingData.positionX, saveBuildingData.positionZ);
					saveBuildingData.cellX = gridCoordinate.X;
					saveBuildingData.cellZ = gridCoordinate.Z;
					if (string.Equals(saveBuildingData.buildingId, "building.wall", StringComparison.Ordinal) || string.Equals(saveBuildingData.buildingId, "building.door", StringComparison.Ordinal))
					{
						saveBuildingData.placementKind = 2;
						saveBuildingData.edgeOrientation = (int)BuildGridOrigin.OrientationFor(saveBuildingData.quarterTurns);
					}
					else
					{
						saveBuildingData.placementKind = (string.Equals(saveBuildingData.buildingId, "building.floor", StringComparison.Ordinal) ? 1 : 0);
					}
				}
			}
		}

		public static bool TryMigrate(SaveGameData source, out SaveGameData result, out string error)
		{
			result = source;
			if (source == null)
			{
				error = "Save data is missing.";
				return false;
			}
			if (source.saveVersion > 15)
			{
				error = $"Save version {source.saveVersion} is newer than " + "supported.";
				return false;
			}
			while (result.saveVersion < 15)
			{
				if (!TryApplyNextStep(result, out error))
				{
					return false;
				}
			}
			error = string.Empty;
			return true;
		}

		private static bool TryApplyNextStep(SaveGameData data, out string error)
		{
			switch (data.saveVersion)
			{
			case 1:
				data.saveVersion = 2;
				error = string.Empty;
				return true;
			case 2:
				data.player.equipment = Array.Empty<SaveEquipmentEntry>();
				data.deathBag = new SaveDeathBagData();
				data.saveVersion = 3;
				error = string.Empty;
				return true;
			case 3:
				data.zones = Array.Empty<SaveZoneStateData>();
				data.saveVersion = 4;
				error = string.Empty;
				return true;
			case 4:
				data.progression = new SaveProgressionData();
				data.saveVersion = 5;
				error = string.Empty;
				return true;
			case 5:
			{
				SaveGameData saveGameData = data;
				if (saveGameData.progression == null)
				{
					saveGameData.progression = new SaveProgressionData();
				}
				data.progression.unlockedTechnologyNodeIds = Array.Empty<string>();
				data.saveVersion = 6;
				error = string.Empty;
				return true;
			}
			case 6:
				data.buildings = Array.Empty<SaveBuildingData>();
				data.saveVersion = 7;
				error = string.Empty;
				return true;
			case 7:
			{
				SaveGameData saveGameData = data;
				if (saveGameData.buildings == null)
				{
					saveGameData.buildings = Array.Empty<SaveBuildingData>();
				}
				data.saveVersion = 8;
				error = string.Empty;
				return true;
			}
			case 8:
			{
				SaveGameData saveGameData = data;
				if (saveGameData.player == null)
				{
					saveGameData.player = new SavePlayerData();
				}
				data.player.eidraRoster = MigrateSingleEidra(data.player.activeEidraId);
				data.player.activeEidraInstanceIds = ((data.player.eidraRoster.Length != 1) ? Array.Empty<string>() : new string[1] { data.player.eidraRoster[0].instanceId });
				data.saveVersion = 9;
				error = string.Empty;
				return true;
			}
			case 9:
				MigrateBuildingsToGrid(data.buildings);
				data.saveVersion = 10;
				error = string.Empty;
				return true;
			case 10:
				V02SaveMigration.Apply(data);
				data.saveVersion = 11;
				error = string.Empty;
				return true;
			case 11:
				if (data.zones != null)
				{
					SaveZoneStateData[] zones = data.zones;
					foreach (SaveZoneStateData saveZoneStateData in zones)
					{
						if (saveZoneStateData != null)
						{
							saveZoneStateData.worldChests = Array.Empty<SaveWorldChestData>();
						}
					}
				}
				data.saveVersion = 12;
				error = string.Empty;
				return true;
			case 12:
				data.eidraForge = new SaveEidraForgeData();
				data.saveVersion = 13;
				error = string.Empty;
				return true;
			case 13:
				// F31-011: Der zweite Eidra-Platz haengt jetzt am Fanggeraet-
				// Knoten; der separate Knoten entfaellt mit Punktrueckgabe.
				MigrateSecondEidraSlot(data.progression);
				data.saveVersion = 14;
				error = string.Empty;
				return true;
			case 14:
				// F31-006: Tutorial-Questkette. Der Stand bleibt hier leer —
				// beim Laden leitet der Dienst ihn aus den Erstlisten und dem
				// Eidra-Bestand ab (EP nur für echte, neue Abschluesse).
				data.quest = new SaveQuestData();
				data.saveVersion = 15;
				error = string.Empty;
				return true;
			default:
				error = "No migration exists from save version " + $"{data.saveVersion}.";
				return false;
			}
		}

		private static SaveEidraInstanceData[] MigrateSingleEidra(string activeEidraId)
		{
			if (string.IsNullOrWhiteSpace(activeEidraId))
			{
				return Array.Empty<SaveEidraInstanceData>();
			}
			return new SaveEidraInstanceData[1]
			{
				new SaveEidraInstanceData
				{
					instanceId = EidraInstanceState.BuildInstanceId(activeEidraId, 1),
					eidraId = activeEidraId
				}
			};
		}
	}
}
