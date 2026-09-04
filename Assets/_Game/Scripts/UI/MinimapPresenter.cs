using Eidren.Core.Services;
using Eidren.Player;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.UI
{
	/// <summary>
	/// Zeichnet den Kartenausschnitt um die Figur (N04-001): Ressourcen, Gegner,
	/// Bosse und entdeckte Kisten. Die Marker kommen aus der
	/// <c>MinimapRegistry</c> — kein <c>Find*</c>, keine Erzeugung im Takt (§8).
	/// Aktualisiert wird getaktet, nicht pro Bild.
	/// </summary>
	public sealed class MinimapPresenter : MonoBehaviour
	{
		[SerializeField]
		private RectTransform field;

		[SerializeField]
		private RectTransform markerRoot;

		[SerializeField]
		private RectTransform edgeRoot;

		[SerializeField]
		private Image markerTemplate;

		[SerializeField]
		private Sprite dotSymbol;

		[SerializeField]
		private Sprite bossSymbol;

		[SerializeField]
		private Sprite chestSymbol;

		[SerializeField]
		private Sprite lineSymbol;

		[SerializeField]
		private float worldRadius = 22f;

		[SerializeField]
		private float mapYaw = 45f;

		[SerializeField]
		private float mapRadius = 110f;

		[SerializeField]
		private float refreshInterval = 0.1f;

		[SerializeField]
		private MinimapStyle style = MinimapStyle.Default;

		[SerializeField]
		private MinimapResourceColor[] resourceColors = Array.Empty<MinimapResourceColor>();

		private PlayerMotor _motor;

		private Camera _view;

		private MinimapMarkerPool _pool;

		private MinimapMarkerPool _edgePool;

		// Das "Ende der Karte" — kommt aus der Zonengeometrie
		// (ZoneBoundarySettings.CreateMapEdgeBoundary), nicht aus der
		// Bewegungsgrenze: Die ist in allen echten Zonen Unbounded.
		private PlayerMovementBoundary _mapEdge = PlayerMovementBoundary.Unbounded();

		// Wandkontur des Verlieses (N04: Aufbau im Sichtradius). Ausserhalb
		// des Verlieses leer — dann zeichnet der Takt nichts Zusaetzliches.
		private IReadOnlyList<MinimapWallSegment> _wallSegments;

		/// <summary>Farbe der Wandkontur — öffentlich für den Testnachweis.</summary>
		public static readonly Color WallColor = new Color(0.93f, 0.9f, 0.82f, 0.95f);

		private const float WallThickness = 2f;

		private float _nextRefresh;

		public void ConfigureReferences(RectTransform mapField, RectTransform markers, RectTransform edges, Image template, Sprite dot, Sprite boss, Sprite chest, Sprite line)
		{
			field = mapField;
			markerRoot = markers;
			edgeRoot = edges;
			markerTemplate = template;
			dotSymbol = dot;
			bossSymbol = boss;
			chestSymbol = chest;
			lineSymbol = line;
			_pool = null;
			_edgePool = null;
		}

		public void ConfigureStyle(MinimapStyle configuredStyle, MinimapResourceColor[] configuredResourceColors)
		{
			style = configuredStyle;
			resourceColors = configuredResourceColors ?? Array.Empty<MinimapResourceColor>();
		}

		public void ConfigureRange(float configuredWorldRadius, float configuredYaw, float configuredMapRadius)
		{
			worldRadius = Mathf.Max(1f, configuredWorldRadius);
			mapYaw = configuredYaw;
			mapRadius = Mathf.Max(1f, configuredMapRadius);
		}

		public void SetMapEdge(PlayerMovementBoundary edge)
		{
			_mapEdge = edge;
		}

		/// <summary>
		/// Wandkontur fuer die Karte (Verlies). Kommt vom Szenenaufbau —
		/// die UI-Schicht fragt kein NavMesh ab. Null oder leer schaltet
		/// die Kontur ab (Aussenzonen).
		/// </summary>
		public void SetWallOutline(IReadOnlyList<MinimapWallSegment> segments)
		{
			_wallSegments = segments;
		}

		public void Bind(PlayerMotor motor, Camera view)
		{
			_motor = motor;
			_view = view;
			_nextRefresh = 0f;
			EnsurePool();
		}

		/// <summary>
		/// Zeichnet die Karte einmal neu. Öffentlich, weil EditMode-Tests kein
		/// <c>LateUpdate</c> ausführen — und weil ein Zonenwechsel eine sofortige
		/// Auffrischung braucht.
		/// </summary>
		public void Refresh()
		{
			EnsurePool();
			if (_motor == null || _pool == null)
			{
				return;
			}
			Vector3 center = _motor.transform.position;
			_pool.Begin();
			IReadOnlyList<IMinimapMarker> markers = MinimapRegistry.Markers;
			// Indexschleife statt foreach: über die Schnittstelle wuerde der
			// Enumerator je Takt eine Allokation kosten.
			for (int i = 0; i < markers.Count; i++)
			{
				IMinimapMarker marker = markers[i];
				if (marker == null)
				{
					continue;
				}
				Vector3 world = marker.MinimapPosition;
				if (!marker.ShowsOnMinimap)
				{
					Discover(marker, world);
					if (!marker.ShowsOnMinimap)
					{
						continue;
					}
				}
				if (MinimapProjection.TryProject(world, center, worldRadius, mapRadius, mapYaw, out var point))
				{
					Draw(marker, point);
				}
			}
			_pool.End();
			DrawBoundary(center);
		}

		private void LateUpdate()
		{
			if (_motor != null && Time.unscaledTime >= _nextRefresh)
			{
				_nextRefresh = Time.unscaledTime + Mathf.Max(0.02f, refreshInterval);
				Refresh();
			}
		}

		/// <summary>
		/// Kisten erscheinen erst, wenn sie einmal im Kamerabild lagen. Geprüft
		/// wird hier, weil nur die Karte die Kamera kennt; gemerkt wird es am
		/// Objekt.
		/// </summary>
		private void Discover(IMinimapMarker marker, Vector3 world)
		{
			if (_view == null || !(marker is IMinimapDiscoverable discoverable))
			{
				return;
			}
			Vector3 viewport = _view.WorldToViewportPoint(world);
			if (viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f)
			{
				discoverable.MarkSeenOnMinimap();
			}
		}

		private void Draw(IMinimapMarker marker, Vector2 point)
		{
			Image image = _pool.Take();
			Sprite sprite = dotSymbol;
			Color color = style.resourceColor;
			float size = style.resourceSize;
			switch (marker.MinimapKind)
			{
				case MinimapMarkerKind.Boss:
					sprite = bossSymbol;
					color = style.bossColor;
					size = style.bossSize;
					break;
				case MinimapMarkerKind.Chest:
					sprite = chestSymbol;
					color = style.chestColor;
					size = style.chestSize;
					break;
				case MinimapMarkerKind.Enemy:
					color = style.enemyColor;
					size = style.enemySize;
					break;
				default:
					color = ResourceColor(marker.MinimapPaletteId);
					break;
			}
			image.sprite = sprite;
			image.color = color;
			RectTransform rect = image.rectTransform;
			rect.sizeDelta = new Vector2(size, size);
			rect.anchoredPosition = point;
		}

		/// <summary>
		/// Das Ende der Karte als Linie. Die Grenze kommt aus dem Motor, damit sie
		/// nach einem Zonenwechsel ohne Neubindung stimmt.
		/// </summary>
		private void DrawBoundary(Vector3 center)
		{
			if (_edgePool == null)
			{
				return;
			}
			_edgePool.Begin();
			PlayerMovementBoundary boundary = _mapEdge;
			if (boundary.HasHardBoundary)
			{
				if (boundary.Shape == MovementBoundaryShape.Rectangle)
				{
					Vector3 c = boundary.Center;
					float halfX = boundary.Size.x * 0.5f;
					float halfZ = boundary.Size.y * 0.5f;
					Vector3 sw = new Vector3(c.x - halfX, c.y, c.z - halfZ);
					Vector3 se = new Vector3(c.x + halfX, c.y, c.z - halfZ);
					Vector3 ne = new Vector3(c.x + halfX, c.y, c.z + halfZ);
					Vector3 nw = new Vector3(c.x - halfX, c.y, c.z + halfZ);
					DrawEdge(center, sw, se);
					DrawEdge(center, se, ne);
					DrawEdge(center, ne, nw);
					DrawEdge(center, nw, sw);
				}
				else
				{
					// Der Kreis wird in Sehnen zerlegt; bei 22 m Ausschnitt und
					// zonengrossen Radien ist immer nur ein kurzer Bogen im Bild.
					const int steps = 48;
					Vector3 previous = CirclePoint(boundary, steps, 0);
					for (int i = 1; i <= steps; i++)
					{
						Vector3 next = CirclePoint(boundary, steps, i);
						DrawEdge(center, previous, next);
						previous = next;
					}
				}
			}
			// Wandkontur des Verlieses — DrawEdge clippt selbst auf den
			// Sichtradius, gezeichnet wird nur der Ausschnitt um die Figur.
			if (_wallSegments != null)
			{
				for (int i = 0; i < _wallSegments.Count; i++)
				{
					DrawEdge(center, _wallSegments[i].From, _wallSegments[i].To, WallColor, WallThickness);
				}
			}
			_edgePool.End();
		}

		private static Vector3 CirclePoint(PlayerMovementBoundary boundary, int steps, int index)
		{
			float angle = (float)index / (float)steps * (Mathf.PI * 2f);
			return new Vector3(boundary.Center.x + Mathf.Cos(angle) * boundary.Radius, boundary.Center.y, boundary.Center.z + Mathf.Sin(angle) * boundary.Radius);
		}

		private void DrawEdge(Vector3 center, Vector3 from, Vector3 to)
		{
			DrawEdge(center, from, to, style.edgeColor, style.edgeThickness);
		}

		private void DrawEdge(Vector3 center, Vector3 from, Vector3 to, Color color, float thickness)
		{
			if (MinimapProjection.DistanceToSegment(center, from, to) > worldRadius)
			{
				return;
			}
			Vector2 a = MinimapProjection.Project(from, center, worldRadius, mapRadius, mapYaw);
			Vector2 b = MinimapProjection.Project(to, center, worldRadius, mapRadius, mapYaw);
			Vector2 delta = b - a;
			float length = delta.magnitude;
			if (length < 0.01f)
			{
				return;
			}
			Image image = _edgePool.Take();
			image.sprite = lineSymbol;
			image.color = color;
			RectTransform rect = image.rectTransform;
			rect.sizeDelta = new Vector2(length, Mathf.Max(1f, thickness));
			rect.anchoredPosition = a + delta * 0.5f;
			rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
		}

		private Color ResourceColor(string paletteId)
		{
			if (!string.IsNullOrEmpty(paletteId) && resourceColors != null)
			{
				for (int i = 0; i < resourceColors.Length; i++)
				{
					if (string.Equals(resourceColors[i].paletteId, paletteId, StringComparison.Ordinal))
					{
						return resourceColors[i].color;
					}
				}
			}
			return style.resourceColor;
		}

		private void EnsurePool()
		{
			if (_pool == null && markerTemplate != null && markerRoot != null)
			{
				_pool = new MinimapMarkerPool(markerTemplate, markerRoot);
			}
			if (_edgePool == null && markerTemplate != null && edgeRoot != null)
			{
				_edgePool = new MinimapMarkerPool(markerTemplate, edgeRoot);
			}
		}
	}
}
