using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Presentation
{
	public sealed class BuildingRoofView : MonoBehaviour
	{
		private sealed class Room
		{
			private readonly GameObject _prefab;

			private readonly List<Transform> _tiles = new List<Transform>();

			private readonly List<Vector3> _cells = new List<Vector3>();

			private readonly Transform _root;

			private readonly Material _material;

			private readonly Color _colour;

			private float _alpha = 1f;

			public Room(GameObject prefab, Material authored, Transform parent, int index)
			{
				_prefab = prefab;
				GameObject gameObject = new GameObject($"Roof_{index:00}");
				_root = gameObject.transform;
				_root.SetParent(parent, worldPositionStays: false);
				_material = new Material(authored)
				{
					name = $"{authored.name} (Raum {index:00})",
					hideFlags = HideFlags.DontSave
				};
				_colour = _material.GetColor("_BaseColor");
			}

			public void Cover(IReadOnlyList<Vector3> cellCentres, float height)
			{
				_cells.Clear();
				_root.gameObject.SetActive(value: true);
				for (int i = 0; i < cellCentres.Count; i++)
				{
					Vector3 item = cellCentres[i];
					_cells.Add(item);
					Tile(i).position = new Vector3(item.x, item.y + height, item.z);
				}
				for (int j = cellCentres.Count; j < _tiles.Count; j++)
				{
					_tiles[j].gameObject.SetActive(value: false);
				}
			}

			public void Clear()
			{
				_cells.Clear();
				_root.gameObject.SetActive(value: false);
			}

			public bool Contains(Vector3 position)
			{
				foreach (Vector3 cell in _cells)
				{
					if (Mathf.Abs(position.x - cell.x) <= 0.5f && Mathf.Abs(position.z - cell.z) <= 0.5f)
					{
						return true;
					}
				}
				return false;
			}

			public void FadeTowards(float target, float step)
			{
				if (!Mathf.Approximately(_alpha, target))
				{
					_alpha = Mathf.MoveTowards(_alpha, target, step);
					Color colour = _colour;
					colour.a = _colour.a * _alpha;
					_material.SetColor("_BaseColor", colour);
					_material.color = colour;
					_root.gameObject.SetActive(_cells.Count > 0 && _alpha > 0.001f);
				}
			}

			public void Dispose()
			{
				if (_material != null)
				{
					UnityEngine.Object.Destroy(_material);
				}
			}

			private Transform Tile(int index)
			{
				if (index == _tiles.Count)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(_prefab, _root, worldPositionStays: false);
					gameObject.name = $"{_prefab.name}_{index:000}";
					MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
					foreach (MeshRenderer meshRenderer in componentsInChildren)
					{
						meshRenderer.sharedMaterial = _material;
					}
					_tiles.Add(gameObject.transform);
				}
				Transform transform = _tiles[index];
				transform.gameObject.SetActive(value: true);
				return transform;
			}
		}

		public const string ResourceFolder = "Art/Buildings";

		private const string TileAsset = "BLD_Roof_Tile";

		private const string MaterialAsset = "BLD_Roof";

		private const string BaseColour = "_BaseColor";

		private const float HalfCell = 0.5f;

		private const float FadeSpeed = 4.5f;

		private readonly List<Room> _rooms = new List<Room>();

		private GameObject _tilePrefab;

		private Material _authored;

		private Transform _occupant;

		private float _height;

		private int _used;

		public int RoomCount => _used;

		private void Awake()
		{
			_tilePrefab = Load<GameObject>("BLD_Roof_Tile");
			_authored = Load<Material>("BLD_Roof");
		}

		public void Configure(Transform occupant, float height)
		{
			if (!(occupant != null))
			{
				throw new ArgumentNullException("occupant");
			}
			_occupant = occupant;
			if (height <= 0f)
			{
				throw new ArgumentOutOfRangeException("height", height, "Ein Dach liegt über dem Boden.");
			}
			_height = height;
		}

		public void BeginRooms()
		{
			_used = 0;
		}

		public void AddRoom(IReadOnlyList<Vector3> cellCentres)
		{
			if (cellCentres != null && cellCentres.Count != 0)
			{
				if (_used == _rooms.Count)
				{
					_rooms.Add(new Room(_tilePrefab, _authored, base.transform, _used));
				}
				_rooms[_used++].Cover(cellCentres, _height);
			}
		}

		public void EndRooms()
		{
			for (int i = _used; i < _rooms.Count; i++)
			{
				_rooms[i].Clear();
			}
		}

		private void LateUpdate()
		{
			if (!(_occupant == null))
			{
				Vector3 position = _occupant.position;
				float step = 4.5f * Time.unscaledDeltaTime;
				for (int i = 0; i < _used; i++)
				{
					Room room = _rooms[i];
					room.FadeTowards(room.Contains(position) ? 0f : 1f, step);
				}
			}
		}

		private void OnDestroy()
		{
			foreach (Room room in _rooms)
			{
				room.Dispose();
			}
			_rooms.Clear();
		}

		private static T Load<T>(string assetName) where T : UnityEngine.Object
		{
			T val = Resources.Load<T>("Art/Buildings/" + assetName);
			if (!(val != null))
			{
				throw new InvalidOperationException("Dachasset fehlt: Art/Buildings/" + assetName);
			}
			return val;
		}
	}
}
