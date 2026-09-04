using System;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapResponsiveLayout : MonoBehaviour
	{
		[SerializeField]
		private RectTransform mapViewport;

		[SerializeField]
		private RectTransform infoPanel;

		[SerializeField]
		private RectTransform sideMenu;

		[SerializeField]
		private RectTransform quickActions;

		[SerializeField]
		private RectTransform travelButton;

		private Vector2Int _lastScreenSize;

		public WorldMapInfoPanelPlacement CurrentPlacement { get; private set; }

		public void ConfigureUi(RectTransform map, RectTransform info, RectTransform menu, RectTransform quick)
		{
			mapViewport = map;
			infoPanel = info;
			sideMenu = menu;
			quickActions = quick;
			ResolveTravelButton();
		}

		public void ApplyForAspect(float aspect)
		{
			if (mapViewport == null || infoPanel == null || sideMenu == null || quickActions == null)
			{
				throw new InvalidOperationException("Responsive layout '" + base.name + "' has missing references.");
			}
			WorldMapLayoutSpec worldMapLayoutSpec = Calculate(aspect);
			CurrentPlacement = worldMapLayoutSpec.Placement;
			Apply(mapViewport, worldMapLayoutSpec.MapAnchors);
			Apply(infoPanel, worldMapLayoutSpec.InfoAnchors);
			Apply(sideMenu, worldMapLayoutSpec.SideMenuAnchors);
			Apply(quickActions, worldMapLayoutSpec.QuickActionsAnchors);
			ApplyTravelButton(worldMapLayoutSpec.Placement);
		}

		public static WorldMapLayoutSpec Calculate(float aspect)
		{
			if (aspect >= 1.85f)
			{
				return new WorldMapLayoutSpec(WorldMapInfoPanelPlacement.Side, new Rect(0.015f, 0.085f, 0.61f, 0.81f), new Rect(0.635f, 0.085f, 0.225f, 0.81f), new Rect(0.87f, 0.085f, 0.115f, 0.81f), new Rect(0.025f, 0.02f, 0.6f, 0.055f));
			}
			return new WorldMapLayoutSpec(WorldMapInfoPanelPlacement.Bottom, new Rect(0.02f, 0.34f, 0.78f, 0.555f), new Rect(0.02f, 0.075f, 0.78f, 0.25f), new Rect(0.815f, 0.075f, 0.17f, 0.82f), new Rect(0.02f, 0.015f, 0.78f, 0.05f));
		}

		private void Awake()
		{
			ApplyCurrent();
		}

		private void Update()
		{
			if (_lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
			{
				ApplyCurrent();
			}
		}

		private void ApplyCurrent()
		{
			_lastScreenSize = new Vector2Int(Screen.width, Screen.height);
			float aspect = ((Screen.height > 0) ? ((float)Screen.width / (float)Screen.height) : 1.7777778f);
			ApplyForAspect(aspect);
		}

		private void ResolveTravelButton()
		{
			if (!(travelButton != null) && !(infoPanel == null))
			{
				WorldMapTravelButton componentInChildren = infoPanel.GetComponentInChildren<WorldMapTravelButton>(includeInactive: true);
				if (componentInChildren != null)
				{
					travelButton = componentInChildren.GetComponent<RectTransform>();
				}
			}
		}

		private void ApplyTravelButton(WorldMapInfoPanelPlacement placement)
		{
			ResolveTravelButton();
			if (!(travelButton == null))
			{
				if (placement == WorldMapInfoPanelPlacement.Bottom)
				{
					travelButton.anchorMin = new Vector2(0.7f, 0.04f);
					travelButton.anchorMax = new Vector2(0.97f, 0.28f);
				}
				else
				{
					travelButton.anchorMin = new Vector2(0.05f, 0.01f);
					travelButton.anchorMax = new Vector2(0.95f, 0.065f);
				}
				travelButton.offsetMin = Vector2.zero;
				travelButton.offsetMax = Vector2.zero;
			}
		}

		private static void Apply(RectTransform target, Rect anchors)
		{
			target.anchorMin = new Vector2(anchors.xMin, anchors.yMin);
			target.anchorMax = new Vector2(anchors.xMax, anchors.yMax);
			target.offsetMin = Vector2.zero;
			target.offsetMax = Vector2.zero;
		}
	}

	public readonly struct WorldMapLayoutSpec
	{
		public WorldMapInfoPanelPlacement Placement { get; }

		public Rect MapAnchors { get; }

		public Rect InfoAnchors { get; }

		public Rect SideMenuAnchors { get; }

		public Rect QuickActionsAnchors { get; }

		public WorldMapLayoutSpec(WorldMapInfoPanelPlacement placement, Rect mapAnchors, Rect infoAnchors, Rect sideMenuAnchors, Rect quickActionsAnchors)
		{
			Placement = placement;
			MapAnchors = mapAnchors;
			InfoAnchors = infoAnchors;
			SideMenuAnchors = sideMenuAnchors;
			QuickActionsAnchors = quickActionsAnchors;
		}
	}
}
