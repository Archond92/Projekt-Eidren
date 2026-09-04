using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Presentation
{
	public sealed class BuildGridOverlayView : MonoBehaviour
	{
		public const string ResourceFolder = "Art/BuildGrid";

		private const float AreaHeight = 0.02f;

		private const float ZoneHeight = 0.03f;

		private const float BoundaryHeight = 0.04f;

		private const float MarkHeight = 0.05f;

		private const float NodeHeight = 0.06f;

		private static readonly Quaternion AlongX = Quaternion.identity;

		private static readonly Quaternion AlongZ = Quaternion.Euler(0f, 90f, 0f);

		private GameObject _area;

		private Material _areaMaterial;

		private BuildGridMarkPool _zones;

		private BuildGridMarkPool _boundary;

		private BuildGridMarkPool _cells;

		private BuildGridMarkPool _edges;

		private BuildGridMarkPool _nodes;

		private Material _focus;

		private Material _blocked;

		private Material _conflict;

		private void Awake()
		{
			_area = UnityEngine.Object.Instantiate(Prefab("BLD_Grid_Area"), base.transform, worldPositionStays: false);
			_area.name = "GridArea";
			_areaMaterial = InstanceMaterialOf(_area);
			_zones = new BuildGridMarkPool(Prefab("BLD_Grid_Zone"), base.transform);
			_boundary = new BuildGridMarkPool(Prefab("BLD_Grid_Boundary"), base.transform);
			_cells = new BuildGridMarkPool(Prefab("BLD_Grid_Cell"), base.transform);
			_edges = new BuildGridMarkPool(Prefab("BLD_Grid_Edge"), base.transform);
			_nodes = new BuildGridMarkPool(Prefab("BLD_Grid_Node"), base.transform);
			_focus = LoadMaterial("BLD_Grid_Focus");
			_blocked = LoadMaterial("BLD_Grid_Blocked");
			_conflict = LoadMaterial("BLD_Grid_Conflict");
			Hide();
		}

		public void ShowArea(Vector3 centre, Vector2 size, IReadOnlyList<Rect> blockedZones)
		{
			base.gameObject.SetActive(value: true);
			_area.SetActive(value: true);
			Place(_area.transform, new Vector3(centre.x, centre.y + 0.02f, centre.z), AlongX, new Vector3(size.x, 1f, size.y));
			_areaMaterial.mainTextureScale = size;
			ShowBoundary(centre, size);
			ShowZones(centre.y, blockedZones);
		}

		public void BeginMarks()
		{
			_cells.Begin();
			_edges.Begin();
			_nodes.Begin();
		}

		public void AddCell(Vector3 centre, BuildGridMarkStyle style)
		{
			BuildGridMark buildGridMark = _cells.Take();
			Place(buildGridMark.transform, Raised(centre, 0.05f), AlongX);
			buildGridMark.SetMaterial(MaterialFor(style));
		}

		public void AddEdge(Vector3 centre, bool alongZ, BuildGridMarkStyle style)
		{
			BuildGridMark buildGridMark = _edges.Take();
			Place(buildGridMark.transform, Raised(centre, 0.05f), alongZ ? AlongZ : AlongX);
			buildGridMark.SetMaterial(MaterialFor(style));
		}

		public void AddNode(Vector3 centre)
		{
			BuildGridMark buildGridMark = _nodes.Take();
			Place(buildGridMark.transform, Raised(centre, 0.06f), AlongX);
			buildGridMark.SetMaterial(_focus);
		}

		public void EndMarks()
		{
			_cells.End();
			_edges.End();
			_nodes.End();
		}

		public void ClearMarks()
		{
			_cells.HideAll();
			_edges.HideAll();
			_nodes.HideAll();
		}

		public void Hide()
		{
			ClearMarks();
			_zones.HideAll();
			_boundary.HideAll();
			if (_area != null)
			{
				_area.SetActive(value: false);
			}
			base.gameObject.SetActive(value: false);
		}

		private void ShowBoundary(Vector3 centre, Vector2 size)
		{
			_boundary.Begin();
			float y = centre.y + 0.04f;
			float num = size.x * 0.5f;
			float num2 = size.y * 0.5f;
			Bar(new Vector3(centre.x, y, centre.z + num2), AlongX, size.x);
			Bar(new Vector3(centre.x, y, centre.z - num2), AlongX, size.x);
			Bar(new Vector3(centre.x + num, y, centre.z), AlongZ, size.y);
			Bar(new Vector3(centre.x - num, y, centre.z), AlongZ, size.y);
			_boundary.End();
		}

		private void Bar(Vector3 centre, Quaternion rotation, float length)
		{
			Vector3 authoredScale = _boundary.AuthoredScale;
			Place(_boundary.Take().transform, centre, rotation, new Vector3(length, authoredScale.y, authoredScale.z));
		}

		private void ShowZones(float groundY, IReadOnlyList<Rect> zones)
		{
			_zones.Begin();
			for (int i = 0; i < (zones?.Count ?? 0); i++)
			{
				Rect rect = zones[i];
				Place(_zones.Take().transform, new Vector3(rect.center.x, groundY + 0.03f, rect.center.y), AlongX, new Vector3(rect.width, 1f, rect.height));
			}
			_zones.End();
		}

		private Material MaterialFor(BuildGridMarkStyle style)
		{
			if (1 == 0)
			{
			}
			Material result = style switch
			{
				BuildGridMarkStyle.Blocked => _blocked, 
				BuildGridMarkStyle.Conflict => _conflict, 
				_ => _focus, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static Vector3 Raised(Vector3 centre, float height)
		{
			return new Vector3(centre.x, centre.y + height, centre.z);
		}

		private static void Place(Transform target, Vector3 position, Quaternion rotation)
		{
			target.SetPositionAndRotation(position, rotation);
		}

		private static void Place(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			target.SetPositionAndRotation(position, rotation);
			target.localScale = scale;
		}

		private static Material InstanceMaterialOf(GameObject area)
		{
			MeshRenderer componentInChildren = area.GetComponentInChildren<MeshRenderer>(includeInactive: true);
			if (componentInChildren == null)
			{
				throw new InvalidOperationException("BLD_Grid_Area hat keinen Renderer.");
			}
			return componentInChildren.material;
		}

		private static GameObject Prefab(string assetName)
		{
			GameObject gameObject = Resources.Load<GameObject>("Art/BuildGrid/" + assetName);
			if (!(gameObject != null))
			{
				throw new InvalidOperationException("Rasterprefab fehlt: Art/BuildGrid/" + assetName);
			}
			return gameObject;
		}

		private static Material LoadMaterial(string assetName)
		{
			Material material = Resources.Load<Material>("Art/BuildGrid/" + assetName);
			if (!(material != null))
			{
				throw new InvalidOperationException("Rastermaterial fehlt: Art/BuildGrid/" + assetName);
			}
			return material;
		}

		private void OnDestroy()
		{
			if (_areaMaterial != null)
			{
				UnityEngine.Object.Destroy(_areaMaterial);
			}
		}
	}
}
