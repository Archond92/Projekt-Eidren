using Eidren.Data;
using System.Linq;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapCanvasView : MonoBehaviour
	{
		[SerializeField]
		private Image backdrop;

		[SerializeField]
		private Image mapVisual;

		[SerializeField]
		private Text mapTitle;

		[SerializeField]
		private WorldMapNodeView[] nodeViews = Array.Empty<WorldMapNodeView>();

		[SerializeField]
		private WorldMapConnectionView[] connections = Array.Empty<WorldMapConnectionView>();

		[SerializeField]
		private WorldMapInfoPanel infoPanel;

		[SerializeField]
		private WorldMapTravelButton travelButton;

		[SerializeField]
		private WorldMapTopStatusBar topStatusBar;

		[SerializeField]
		private WorldMapQuickActions quickActions;

		[SerializeField]
		private WorldMapSideMenu sideMenu;

		[SerializeField]
		private WorldMapTransitionOverlay transitionOverlay;

		[SerializeField]
		private WorldMapResponsiveLayout responsiveLayout;

		[SerializeField]
		private EventSystem eventSystem;

		public Image MapVisual => mapVisual;

		public WorldMapNodeView[] NodeViews => nodeViews;

		public WorldMapInfoPanel InfoPanel => infoPanel;

		public WorldMapTravelButton TravelButton => travelButton;

		public WorldMapTopStatusBar TopStatusBar => topStatusBar;

		public WorldMapQuickActions QuickActions => quickActions;

		public WorldMapSideMenu SideMenu => sideMenu;

		public WorldMapTransitionOverlay TransitionOverlay => transitionOverlay;

		public WorldMapResponsiveLayout ResponsiveLayout => responsiveLayout;

		public EventSystem EventSystem => eventSystem;

		public void ConfigureUi(Image background, Image regionalMap, Text title, WorldMapNodeView[] nodes, WorldMapConnectionView[] connectionViews, WorldMapInfoPanel panel, WorldMapTravelButton travel, WorldMapTopStatusBar statusBar, WorldMapQuickActions actions, WorldMapSideMenu menu, WorldMapTransitionOverlay transition, WorldMapResponsiveLayout responsive, EventSystem configuredEventSystem)
		{
			backdrop = background;
			mapVisual = regionalMap;
			mapTitle = title;
			nodeViews = nodes ?? Array.Empty<WorldMapNodeView>();
			connections = connectionViews ?? Array.Empty<WorldMapConnectionView>();
			infoPanel = panel;
			travelButton = travel;
			topStatusBar = statusBar;
			quickActions = actions;
			sideMenu = menu;
			transitionOverlay = transition;
			responsiveLayout = responsive;
			eventSystem = configuredEventSystem;
		}

		public void Apply(WorldMapDefinition definition)
		{
			ValidateReferences();
			WorldMapThemeData theme = definition.Theme;
			backdrop.color = theme.BackgroundDeep;
			mapVisual.sprite = definition.MapVisual;
			mapVisual.color = Color.white;
			mapTitle.text = definition.DisplayName;
			theme.ApplyTypography(mapTitle, WorldMapTypographyRole.MapTitle, theme.TextPrimary);
			infoPanel.ApplyTheme(theme);
			travelButton.ApplyTheme(theme);
			topStatusBar.ApplyTheme(theme);
			quickActions.ApplyTheme(theme);
			sideMenu.ApplyTheme(theme);
			transitionOverlay.ApplyTheme(theme);
			WorldMapConnectionView[] array = connections;
			foreach (WorldMapConnectionView worldMapConnectionView in array)
			{
				worldMapConnectionView.ApplyTheme(theme);
			}
			responsiveLayout.ApplyForAspect((Screen.height > 0) ? ((float)Screen.width / (float)Screen.height) : 1.7777778f);
		}

		public void ValidateReferences()
		{
			if (backdrop == null || mapVisual == null || mapTitle == null || nodeViews == null || nodeViews.Length != 8 || nodeViews.Any((WorldMapNodeView node) => node == null) || connections == null || connections.Length != 7 || connections.Any((WorldMapConnectionView connection) => connection == null) || infoPanel == null || travelButton == null || topStatusBar == null || quickActions == null || sideMenu == null || transitionOverlay == null || responsiveLayout == null || eventSystem == null)
			{
				throw new InvalidOperationException("WorldMapCanvasView '" + base.name + "' has missing serialized references.");
			}
			WorldMapNodeView[] array = nodeViews;
			foreach (WorldMapNodeView worldMapNodeView in array)
			{
				worldMapNodeView.ValidateReferences();
			}
			infoPanel.ValidateReferences();
			travelButton.ValidateReferences();
		}
	}
}
