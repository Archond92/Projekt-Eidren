using Eidren.Core.BuildGrid;
using Eidren.Core.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Composition
{
	public static class BuildingActionText
	{
		public static string For(in BuildingPreview preview, ContentDatabase content)
		{
			if (preview.Reason == BuildingActionResult.MissingMaterials && content != null && content.TryGetItem(preview.MissingItemId, out var value))
			{
				return "Nicht genügend " + value.DisplayName + ".";
			}
			return For(preview.Reason);
		}

		public static string For(BuildingActionResult result)
		{
			if (1 == 0)
			{
			}
			string result2 = result switch
			{
				BuildingActionResult.BuildingLocked => "Gebäude ist noch nicht freigeschaltet.", 
				BuildingActionResult.MissingMaterials => "Nicht genügend Rohstoffe.", 
				BuildingActionResult.OutsideBuildArea => "Außerhalb der Baufläche.", 
				BuildingActionResult.BlockedZone => "Spawn- oder Ausgangszone ist gesperrt.", 
				BuildingActionResult.Overlap => "Der Platz ist bereits belegt.", 
				BuildingActionResult.EdgeOccupied => "Kante bereits belegt.", 
				BuildingActionResult.FloorRequired => "Benötigt einen Boden.", 
				BuildingActionResult.NaturalGroundRequired => "Nur auf natürlichem Boden.", 
				BuildingActionResult.MixedFoundation => "Ganz auf Boden oder ganz auf Erde.", 
				BuildingActionResult.NotMovable => "Boden und Wände werden gebaut, nicht versetzt.", 
				BuildingActionResult.BuildingBusy => "Gebäude wird gerade benutzt.",
				BuildingActionResult.BuildingLimitReached => "Höchstzahl dieses Gebäudes erreicht.",
				BuildingActionResult.StorageNotEmpty => "Die Lagerkiste muss leer sein.", 
				BuildingActionResult.RefundInventoryFull => "Kein Platz für die Rückerstattung.", 
				_ => "Aktion nicht möglich.", 
			};
			if (1 == 0)
			{
			}
			return result2;
		}
	}

	internal static class HomeBaseBuildingPlacement
	{
		public static BuildingPlacementArea CreateArea(ZoneController zone)
		{
			Bounds bounds = zone.WalkableGround.bounds;
			Rect buildBounds = new Rect(bounds.min.x + 5f, bounds.min.z + 5f, Mathf.Max(1f, bounds.size.x - 10f), Mathf.Max(1f, bounds.size.z - 10f));
			List<Rect> list = new List<Rect>();
			MapExitDirection[] array = new MapExitDirection[5]
			{
				MapExitDirection.None,
				MapExitDirection.North,
				MapExitDirection.East,
				MapExitDirection.South,
				MapExitDirection.West
			};
			foreach (MapExitDirection entryDirection in array)
			{
				Transform spawnForEntry = zone.GetSpawnForEntry(entryDirection);
				if (!(spawnForEntry == null))
				{
					list.Add(new Rect(spawnForEntry.position.x - 2f, spawnForEntry.position.z - 2f, 4f, 4f));
				}
			}
			foreach (MapExitVolume exitVolume in zone.ExitVolumes)
			{
				Bounds triggerBounds = exitVolume.TriggerBounds;
				list.Add(new Rect(triggerBounds.min.x, triggerBounds.min.z, triggerBounds.size.x, triggerBounds.size.z));
			}
			return new BuildingPlacementArea(buildBounds, list, BuildGridOrigin.HomeBase);
		}
	}
}
