using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// Interaktiver Review-Viewer fuer den produktiven Low-Poly-Wanderer.
	/// Die Itemvarianten verwenden bewusst die sechs damaligen Familienmeshes.
	/// </summary>
	public sealed class WandererLowPolyViewerController : MonoBehaviour
	{
		private readonly struct HandItem
		{
			public readonly string Label;
			public readonly string ItemId;
			public readonly string Stance;
			public readonly bool IsTool;
			public readonly string FamilyMesh;

			public HandItem(string label, string itemId, string stance, bool isTool, string familyMesh)
			{
				Label = label;
				ItemId = itemId;
				Stance = stance;
				IsTool = isTool;
				FamilyMesh = familyMesh;
			}
		}

		public static readonly string[] ExpectedClipNames =
		{
			"Abbau_Axt", "Abbau_Sense", "Abbau_Spitzhacke", "Angriff_Dolche", "Angriff_Hammer", "Angriff_Speer",
			"Gehen_Axt", "Gehen_Dolche", "Gehen_Hammer", "Gehen_Sense", "Gehen_Speer", "Gehen_Spitzhacke",
			"Laufen_Axt", "Laufen_Dolche", "Laufen_Hammer", "Laufen_Sense", "Laufen_Speer", "Laufen_Spitzhacke",
			"Oeffnen", "Ruhe_Axt", "Ruhe_Dolche", "Ruhe_Hammer", "Ruhe_Sense", "Ruhe_Speer", "Ruhe_Spitzhacke"
		};

		private static readonly string[] ArmorTiers = { "Aus", "Stoff", "Kupfer", "Eisen" };
		private static readonly HandItem[] HandItems =
		{
			new HandItem("Leere Hände", null, MeshActorPresentation.StanceDaggers, false, "–"),
			new HandItem("Hammer", "hammer", MeshActorPresentation.StanceHammer, false, "Waffe_Hammer"),
			new HandItem("Kupferhammer", "copper_hammer", MeshActorPresentation.StanceHammer, false, "Waffe_Hammer"),
			new HandItem("Eisenhammer", "iron_hammer", MeshActorPresentation.StanceHammer, false, "Waffe_Hammer"),
			new HandItem("Siegelbrecher", "sealbreaker", MeshActorPresentation.StanceHammer, false, "Waffe_Hammer"),
			new HandItem("Dolche", "daggers", MeshActorPresentation.StanceDaggers, false, "Waffe_Dolche"),
			new HandItem("Kupferdolche", "copper_daggers", MeshActorPresentation.StanceDaggers, false, "Waffe_Dolche"),
			new HandItem("Eisendolche", "iron_daggers", MeshActorPresentation.StanceDaggers, false, "Waffe_Dolche"),
			new HandItem("Aschenfänge", "ash_fangs", MeshActorPresentation.StanceDaggers, false, "Waffe_Dolche"),
			new HandItem("Kupferspeer", "copper_spear", MeshActorPresentation.StanceSpear, false, "Waffe_Speer"),
			new HandItem("Eisenspeer", "iron_spear", MeshActorPresentation.StanceSpear, false, "Waffe_Speer"),
			new HandItem("Glutdorn", "ember_thorn", MeshActorPresentation.StanceSpear, false, "Waffe_Speer"),
			new HandItem("Axt", "axe", MeshActorPresentation.StanceAxe, true, "Waffe_Axt"),
			new HandItem("Kupferaxt", "copper_axe", MeshActorPresentation.StanceAxe, true, "Waffe_Axt"),
			new HandItem("Eisenaxt", "iron_axe", MeshActorPresentation.StanceAxe, true, "Waffe_Axt"),
			new HandItem("Spitzhacke", "pickaxe", MeshActorPresentation.StancePickaxe, true, "Waffe_Spitzhacke"),
			new HandItem("Kupferspitzhacke", "copper_pickaxe", MeshActorPresentation.StancePickaxe, true, "Waffe_Spitzhacke"),
			new HandItem("Eisenspitzhacke", "iron_pickaxe", MeshActorPresentation.StancePickaxe, true, "Waffe_Spitzhacke"),
			new HandItem("Sense", "scythe", MeshActorPresentation.StanceScythe, true, "Waffe_Sense"),
			new HandItem("Kupfersense", "copper_scythe", MeshActorPresentation.StanceScythe, true, "Waffe_Sense"),
			new HandItem("Eisensense", "iron_scythe", MeshActorPresentation.StanceScythe, true, "Waffe_Sense")
		};

		[SerializeField] private Transform displayPivot;
		[SerializeField] private MeshActorPresentation presentation;
		[SerializeField] private Animation animationPlayer;
		[SerializeField] private Camera viewerCamera;

		private readonly int[] _armor = new int[4];
		private string[] _clips = Array.Empty<string>();
		private int _clipIndex;
		private int _handItemIndex;
		private float _animationSpeed = 1f;
		private float _yaw = 180f;
		private float _pitch = 10f;
		private float _distance = 4.6f;
		private bool _playing = true;
		private bool _autoOrbit;
		private bool _initialized;
		private Vector2 _scroll;

		public static int ArmorPieceCount => 12;
		public static int HandItemVariantCount => HandItems.Length - 1;
		public static int HandSelectionCount => HandItems.Length;
		public int ImportedClipCount => _clips.Length;

		public void Configure(Transform pivot, MeshActorPresentation meshPresentation, Animation clips, Camera camera)
		{
			displayPivot = pivot;
			presentation = meshPresentation;
			animationPlayer = clips;
			viewerCamera = camera;
		}

		private void Start()
		{
			Initialize();
		}

		private void Initialize()
		{
			if (_initialized) return;
			if (presentation == null) presentation = GetComponentInChildren<MeshActorPresentation>(true);
			if (animationPlayer == null) animationPlayer = GetComponentInChildren<Animation>(true);
			if (viewerCamera == null) viewerCamera = Camera.main;
			if (displayPivot == null && presentation != null) displayPivot = presentation.transform;
			_clips = animationPlayer == null
				? Array.Empty<string>()
				: animationPlayer.Cast<AnimationState>().Select(state => state.name)
					.Where(name => !string.IsNullOrEmpty(name)).Distinct().OrderBy(name => name).ToArray();
			_clipIndex = Mathf.Max(0, Array.IndexOf(_clips, "Ruhe_Dolche"));
			ApplyArmor();
			ApplyHandItem();
			PlaySelectedClip();
			ApplyCamera();
			_initialized = true;
		}

		private void Update()
		{
			if (!_initialized) Initialize();
			if (_autoOrbit) _yaw += 18f * Time.unscaledDeltaTime;
			AnimationState state = CurrentState();
			if (state != null) state.speed = _playing ? _animationSpeed : 0f;
		}

		private void LateUpdate()
		{
			ApplyCamera();
		}

		private void OnGUI()
		{
			HandleOrbitInput(Event.current);
			float panelWidth = PanelWidth();
			GUI.Box(new Rect(16f, 16f, panelWidth, Screen.height - 32f), GUIContent.none);
			GUILayout.BeginArea(new Rect(30f, 26f, panelWidth - 28f, Screen.height - 52f));
			_scroll = GUILayout.BeginScrollView(_scroll);
			GUILayout.Label("WANDERER · LOW-POLY VIEWER", HeaderStyle());
			GUILayout.Label("Linke Maustaste: drehen · Mausrad: zoomen", SmallStyle());

			GUILayout.Space(12f);
			GUILayout.Label("RÜSTUNG", SectionStyle());
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Aus")) SetArmorPreset(0);
			if (GUILayout.Button("Stoff")) SetArmorPreset(1);
			if (GUILayout.Button("Kupfer")) SetArmorPreset(2);
			if (GUILayout.Button("Eisen")) SetArmorPreset(3);
			GUILayout.EndHorizontal();
			DrawArmorSlot("Kopf", 0);
			DrawArmorSlot("Brust", 1);
			DrawArmorSlot("Hände", 2);
			DrawArmorSlot("Beine", 3);

			GUILayout.Space(12f);
			GUILayout.Label("WERKZEUGE UND WAFFEN", SectionStyle());
			DrawSelectionStepper(HandItems[_handItemIndex].Label, ChangeHandItem);
			GUILayout.Label("Low-Poly-Familienmodell: " + HandItems[_handItemIndex].FamilyMesh, SmallStyle());

			GUILayout.Space(12f);
			GUILayout.Label("ANIMATIONEN · " + _clips.Length, SectionStyle());
			DrawSelectionStepper(_clips.Length == 0 ? "Keine Clips" : _clips[_clipIndex], ChangeClip);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button(_playing ? "Pause" : "Abspielen")) _playing = !_playing;
			if (GUILayout.Button("Neustart")) PlaySelectedClip();
			if (GUILayout.Button("Item ↔ Clip")) MatchHandsToClip();
			GUILayout.EndHorizontal();
			GUILayout.Label("Geschwindigkeit " + _animationSpeed.ToString("0.00") + "×", SmallStyle());
			_animationSpeed = GUILayout.HorizontalSlider(_animationSpeed, 0.1f, 2f);
			AnimationState state = CurrentState();
			if (state != null)
			{
				float progress = Mathf.Repeat(state.normalizedTime, 1f);
				GUILayout.HorizontalSlider(progress, 0f, 1f);
				GUILayout.Label((progress * state.length).ToString("0.00") + " / " + state.length.ToString("0.00") + " s", SmallStyle());
			}

			GUILayout.Space(12f);
			GUILayout.Label("ANSICHT", SectionStyle());
			_autoOrbit = GUILayout.Toggle(_autoOrbit, "Automatisch drehen");
			if (GUILayout.Button("Kamera zurücksetzen"))
			{
				_yaw = 180f;
				_pitch = 10f;
				_distance = 4.6f;
			}
			GUILayout.Space(8f);
			GUILayout.Label("12 Rüstungsteile · 20 Handitemvarianten · 25 Clips", SmallStyle());
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawArmorSlot(string label, int slot)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, GUILayout.Width(Mathf.Clamp(PanelWidth() * 0.22f, 52f, 90f)));
			if (GUILayout.Button("‹", GUILayout.Width(30f))) ChangeArmor(slot, -1);
			GUILayout.Label(ArmorTiers[_armor[slot]], CenterStyle(), GUILayout.MinWidth(54f));
			if (GUILayout.Button("›", GUILayout.Width(30f))) ChangeArmor(slot, 1);
			GUILayout.EndHorizontal();
		}

		private static void DrawSelectionStepper(string label, Action<int> change)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("‹", GUILayout.Width(32f))) change(-1);
			GUILayout.Label(label, CenterStyle(), GUILayout.MinWidth(80f));
			if (GUILayout.Button("›", GUILayout.Width(32f))) change(1);
			GUILayout.EndHorizontal();
		}

		private void ChangeArmor(int slot, int direction)
		{
			_armor[slot] = Wrap(_armor[slot] + direction, ArmorTiers.Length);
			ApplyArmor();
		}

		private void SetArmorPreset(int tier)
		{
			for (int index = 0; index < _armor.Length; index++) _armor[index] = tier;
			ApplyArmor();
		}

		private void ApplyArmor()
		{
			presentation?.SetArmorTiers(_armor[0], _armor[1], _armor[2], _armor[3]);
		}

		private void ChangeHandItem(int direction)
		{
			_handItemIndex = Wrap(_handItemIndex + direction, HandItems.Length);
			ApplyHandItem();
		}

		private void ApplyHandItem()
		{
			if (presentation == null) return;
			HandItem item = HandItems[_handItemIndex];
			if (string.IsNullOrEmpty(item.ItemId)) presentation.SetBareHands();
			else if (item.IsTool) presentation.SetHarvestToolItem(item.ItemId, item.Stance);
			else presentation.SetWeaponItem(item.ItemId, item.Stance, true);
		}

		private void ChangeClip(int direction)
		{
			if (_clips.Length == 0) return;
			_clipIndex = Wrap(_clipIndex + direction, _clips.Length);
			PlaySelectedClip();
		}

		private void PlaySelectedClip()
		{
			if (animationPlayer == null || _clips.Length == 0) return;
			AnimationState state = animationPlayer[_clips[_clipIndex]];
			if (state == null) return;
			state.wrapMode = WrapMode.Loop;
			state.speed = _playing ? _animationSpeed : 0f;
			state.normalizedTime = 0f;
			animationPlayer.Play(state.name);
		}

		private void MatchHandsToClip()
		{
			if (_clips.Length == 0) return;
			string clip = _clips[_clipIndex];
			string stance = clip.Substring(clip.IndexOf('_') + 1);
			int match = Array.FindIndex(HandItems, item => item.Stance == stance && item.ItemId != null);
			if (match >= 0)
			{
				_handItemIndex = match;
				ApplyHandItem();
			}
			else if (clip == "Oeffnen")
			{
				_handItemIndex = 0;
				ApplyHandItem();
			}
		}

		private AnimationState CurrentState()
		{
			return animationPlayer != null && _clips.Length > 0 ? animationPlayer[_clips[_clipIndex]] : null;
		}

		private void HandleOrbitInput(Event input)
		{
			if (input == null || input.mousePosition.x <= PanelWidth() + 30f) return;
			if (input.type == EventType.MouseDrag && input.button == 0)
			{
				_yaw += input.delta.x * 0.45f;
				_pitch = Mathf.Clamp(_pitch - input.delta.y * 0.35f, -15f, 55f);
				input.Use();
			}
			else if (input.type == EventType.ScrollWheel)
			{
				_distance = Mathf.Clamp(_distance + input.delta.y * 0.18f, 2.2f, 7f);
				input.Use();
			}
		}

		private void ApplyCamera()
		{
			if (viewerCamera == null) return;
			Vector3 target = (displayPivot != null ? displayPivot.position : transform.position) + Vector3.up * 0.95f;
			Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
			float halfViewWidth = _distance * Mathf.Tan(viewerCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * viewerCamera.aspect;
			float normalizedPanelWidth = Mathf.Clamp01((PanelWidth() + 16f) / Mathf.Max(1f, Screen.width));
			Vector3 framingOffset = orbit * Vector3.left * (halfViewWidth * normalizedPanelWidth);
			viewerCamera.transform.SetPositionAndRotation(target - orbit * Vector3.forward * _distance + framingOffset, orbit);
		}

		private static float PanelWidth() => Mathf.Clamp(Screen.width * 0.38f, 240f, 420f);
		private static int Wrap(int value, int count) => (value % count + count) % count;
		private static GUIStyle HeaderStyle() => new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true };
		private static GUIStyle SectionStyle() => new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
		private static GUIStyle SmallStyle() => new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
		private static GUIStyle CenterStyle() => new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
	}
}
