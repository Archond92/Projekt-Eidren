using Eidren.Data;
using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class InteractionIconTests
{
	private sealed class IconlessTarget : IInteractable
	{
		public string InteractionId => "test.iconless";

		public InteractionType Type { get; }

		public string DisplayText => "BENUTZEN";

		public Sprite Icon => null;

		public float InteractionRange => 2f;

		public Eidren.Interaction.InteractionMode Mode => Eidren.Interaction.InteractionMode.Instant;

		public float HoldDuration => 0f;

		public int Priority => 0;

		public Vector3 InteractionPosition => InteractionObject.transform.position;

		public GameObject InteractionObject { get; }

		public IconlessTarget(GameObject owner, InteractionType type = InteractionType.Station)
		{
			InteractionObject = owner;
			Type = type;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			blockedReason = string.Empty;
			return true;
		}

		public void BeginInteraction(in InteractionContext context)
		{
		}

		public void UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
		}

		public void CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
		}

		public void CompleteInteraction(in InteractionContext context)
		{
		}

		bool IInteractable.CanInteract(in InteractionContext context, out string blockedReason)
		{
			return CanInteract(in context, out blockedReason);
		}

		void IInteractable.BeginInteraction(in InteractionContext context)
		{
			BeginInteraction(in context);
		}

		void IInteractable.UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
			UpdateInteraction(in context, normalizedProgress);
		}

		void IInteractable.CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
			CancelInteraction(in context, reason);
		}

		void IInteractable.CompleteInteraction(in InteractionContext context)
		{
			CompleteInteraction(in context);
		}
	}

	private const string HandPath = "Assets/_Game/Resources/Art/UI/ui_interaction_hand.png";

	[TestCase("Tree", "Axe")]
	[TestCase("FiberPlant", "Scythe")]
	[TestCase("StoneDeposit", "Pickaxe")]
	[TestCase("CopperVein", "Pickaxe")]
	public void ResourceNodeShowsItsToolNotItsYield(string nodeName, string toolName)
	{
		GameObject instance = Instantiate("Assets/_Game/Prefabs/Resources/Nodes/" + nodeName + ".prefab");
		try
		{
			ResourceNode node = instance.GetComponentInChildren<ResourceNode>(includeInactive: true);
			Assert.That<ResourceNode>(node, (IResolveConstraint)(object)Is.Not.Null, nodeName, Array.Empty<object>());
			ItemDefinition tool = Item(toolName);
			Assert.That<Sprite>(node.Icon, (IResolveConstraint)(object)Is.SameAs((object)tool.InteractionIcon), nodeName + " zeigt nicht die HUD-Glyphe von " + toolName + ".", Array.Empty<object>());
			Assert.That<Sprite>(node.Icon, (IResolveConstraint)(object)Is.Not.SameAs((object)tool.Icon), nodeName + " zeigt weiterhin das detaillierte Itembild.", Array.Empty<object>());
			Assert.That<Sprite>(node.Icon, (IResolveConstraint)(object)Is.Not.SameAs((object)node.Definition.OutputItem.Icon), nodeName + " zeigt weiterhin seinen Ertrag.", Array.Empty<object>());
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(instance);
		}
	}

	[Test]
	public void HandPickedNodeCarriesNoToolIcon()
	{
		GameObject instance = Instantiate("Assets/_Game/Prefabs/Resources/Nodes/BerryBush.prefab");
		try
		{
			Assert.That<Sprite>(instance.GetComponentInChildren<ResourceNode>(includeInactive: true).Icon, (IResolveConstraint)(object)Is.Null);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(instance);
		}
	}

	[TestCase("Assets/_Game/Prefabs/Stations/StorageChest.prefab")]
	[TestCase("Assets/_Game/Prefabs/Stations/Workbench.prefab")]
	// Pfade auf die Level01-Fabrikstruktur nachgezogen (BLD_<Name>_L01) —
	// die alten flachen Prefabnamen existieren seit dem Gebaeude-Umbau nicht.
	[TestCase("Assets/_Game/Prefabs/Buildings/Level01/BLD_Smelter_L01.prefab")]
	[TestCase("Assets/_Game/Prefabs/Buildings/Level01/BLD_Sawmill_L01.prefab")]
	[TestCase("Assets/_Game/Prefabs/Buildings/Level01/BLD_CookingPot_L01.prefab")]
	[TestCase("Assets/_Game/Prefabs/Buildings/Level01/BLD_Ropewalk_L01.prefab")]
	[TestCase("Assets/_Game/Prefabs/Buildings/Level01/BLD_Stonecutter_L01.prefab")]
	[TestCase("Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab")]
	public void ContainerAndStationCarryNoOwnIcon(string prefabPath)
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null, prefabPath, Array.Empty<object>());
		IInteractable[] array = gameObject.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).OfType<IInteractable>().ToArray();
		Assert.That<IInteractable[]>(array, (IResolveConstraint)(object)Is.Not.Empty, prefabPath, Array.Empty<object>());
		IInteractable[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Assert.That<Sprite>(array2[i].Icon, (IResolveConstraint)(object)Is.Null, prefabPath + " bringt ein eigenes Symbol mit; laut F-003 gehört dorthin die Hand.", Array.Empty<object>());
		}
	}

	[Test]
	public void ButtonFallsBackToTheHandWithoutOwnIcon()
	{
		Sprite hand = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Resources/Art/UI/ui_interaction_hand.png");
		Assert.That<Sprite>(hand, (IResolveConstraint)(object)Is.Not.Null, "Assets/_Game/Resources/Art/UI/ui_interaction_hand.png", Array.Empty<object>());
		GameObject hud = Instantiate("Assets/_Game/Resources/UI/CombatHUD.prefab");
		GameObject target = new GameObject("IconlessTarget");
		try
		{
			CombatHudInteractionPresenter componentInChildren = hud.GetComponentInChildren<CombatHudInteractionPresenter>(includeInactive: true);
			Assert.That<CombatHudInteractionPresenter>(componentInChildren, (IResolveConstraint)(object)Is.Not.Null);
			InteractionSnapshot snapshot = new InteractionSnapshot(InteractionState.Available, new IconlessTarget(target), canInteract: true, 0f, string.Empty);
			Assert.That<Sprite>(snapshot.Icon, (IResolveConstraint)(object)Is.Null);
			Assert.That<Sprite>(componentInChildren.ResolveIcon(in snapshot), (IResolveConstraint)(object)Is.SameAs((object)hand), "Ohne eigenes Symbol bleibt die Fläche leer.", Array.Empty<object>());
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(target);
			UnityEngine.Object.DestroyImmediate(hud);
		}
	}

	[Test]
	public void TargetRingSupportsResourcesContainersAndStations()
	{
		GameObject player = new GameObject("TargetRingPlayer");
		GameObject targetOwner = new GameObject("TargetRingTarget");
		try
		{
			ResourceTargetIndicator indicator = player.AddComponent<ResourceTargetIndicator>();
			InteractionType[] array = new InteractionType[3]
			{
				InteractionType.Resource,
				InteractionType.Container,
				InteractionType.Station
			};
			for (int i = 0; i < array.Length; i++)
			{
				InteractionType type = array[i];
				IconlessTarget target = new IconlessTarget(targetOwner, type);
				indicator.SetTarget(target);
				Assert.That<IInteractable>(indicator.Target, (IResolveConstraint)(object)Is.SameAs((object)target), type.ToString(), Array.Empty<object>());
				Assert.That<bool>(indicator.IsVisible, (IResolveConstraint)(object)Is.True, type.ToString(), Array.Empty<object>());
			}
			indicator.SetTarget(new IconlessTarget(targetOwner, InteractionType.Npc));
			Assert.That<IInteractable>(indicator.Target, (IResolveConstraint)(object)Is.Null);
			Assert.That<bool>(indicator.IsVisible, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(targetOwner);
			UnityEngine.Object.DestroyImmediate(player);
		}
	}

	private static GameObject Instantiate(string prefabPath)
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null, prefabPath, Array.Empty<object>());
		return UnityEngine.Object.Instantiate(gameObject);
	}

	private static ItemDefinition Item(string assetName)
	{
		ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/" + assetName + ".asset");
		Assert.That<ItemDefinition>(itemDefinition, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
		return itemDefinition;
	}

	// Nachfix zu F31-017 (18.08.2026): Der gruene ZIELRING lag wie zuvor die
	// Bauvorschau unter der Oberkante gelegter Boeden (0,14) — er sass auf
	// Fusshoehe + 0,065. Der Ring muss dieselbe Freihoehe einhalten wie die
	// Vorschaumarke (0,2).
	[Test]
	public void Zielring_LiegtUeberGelegtenBoeden()
	{
		GameObject player = new GameObject("IndicatorRingOwner");
		GameObject targetOwner = GameObject.CreatePrimitive(PrimitiveType.Cube);
		try
		{
			targetOwner.transform.position = new Vector3(0f, 0.5f, 0f);
			ResourceTargetIndicator indicator = player.AddComponent<ResourceTargetIndicator>();
			indicator.SetTarget(new IconlessTarget(targetOwner));
			Assert.That<bool>(indicator.IsVisible, (IResolveConstraint)(object)Is.True);
			LineRenderer ring = player.GetComponentInChildren<LineRenderer>();
			Assert.That<LineRenderer>(ring, (IResolveConstraint)(object)Is.Not.Null);
			float tiefster = float.MaxValue;
			Vector3[] punkte = new Vector3[ring.positionCount];
			ring.GetPositions(punkte);
			foreach (Vector3 punkt in punkte)
			{
			tiefster = Mathf.Min(tiefster, punkt.y);
		}
		// Wuerfel-Fuss liegt auf y = 0; gelegte Boeden reichen bis 0,14.
		Assert.That<float>(tiefster, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)0.19f),
			"Der Zielring liegt unter der Oberkante gelegter Boeden (0,14) und ist dort unsichtbar.");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(targetOwner);
			UnityEngine.Object.DestroyImmediate(player);
		}
	}
}
}
