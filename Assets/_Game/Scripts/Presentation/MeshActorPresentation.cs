using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// 3D-Gegenstueck zu SpriteActorPresentation: spielt die Legacy-Clips der
	/// Wanderer.glb ueber eine Animation-Komponente, dreht das Modell fluessig
	/// zur Blick-/Bewegungsrichtung und stellt Zustaende ohne eigenen Clip
	/// (Treffer, Taumeln, Tod, Erscheinen, Ausweichen, Ernten) prozedural dar.
	/// Schaltet ausserdem die Ruestungs-Slotmeshes und das Waffenmesh.
	/// Kern (dieses File): Felder, Lebenszyklus, IActorPresentation,
	/// ILocomotionPresentation, IArmorPresentation und die Clip-Namensregel.
	/// Die Zustaende stehen in MeshActorPresentation.Zustaende.cs, alles zu
	/// Waffen, Werkzeugen und leeren Haenden in MeshActorPresentation.Haende.cs.
	/// </summary>
	public sealed partial class MeshActorPresentation : MonoBehaviour, IActorPresentation, ILocomotionPresentation, IAuthoredStatePresentation, IArmorPresentation
	{
		public const string StanceSpear = "Speer";
		public const string StanceDaggers = "Dolche";
		public const string StanceHammer = "Hammer";
		public const string StanceBare = "Ohne";
		// Werkzeug-Traeger der Wanderer.glb (W-001): eigene Ruhe_/Gehen_/Laufen_-
		// Varianten und je ein Abbau_-Clip.
		public const string StanceAxe = "Axt";
		public const string StancePickaxe = "Spitzhacke";
		public const string StanceScythe = "Sense";

		// Reihenfolge entspricht den SetArmorTiers-Parametern: Kopf, Brust, Haende, Beine.
		private static readonly string[] SlotPrefixes = { "Helm_", "Harnisch_", "Haende_", "Beine_" };
		private static readonly string[] TierSuffixes = { null, "Stoff", "Kupfer", "Eisen" };
		private static readonly string[] WeaponMeshNames = { "Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer" };
		// PHASE-2: itemgenaue Optiken. Die generischen Familienknoten bleiben als
		// Rueckfall fuer Legacy-Modelle und isolierte Presentation-Tests erhalten.
		private static readonly string[] WeaponVariantMeshNames =
		{
			"Waffe_Hammer_Base", "Waffe_Hammer_Kupfer", "Waffe_Hammer_Eisen", "Waffe_Hammer_Sealbreaker",
			"Waffe_Dolche_Base", "Waffe_Dolche_Kupfer", "Waffe_Dolche_Eisen", "Waffe_Dolche_AshFangs",
			"Waffe_Speer_Kupfer", "Waffe_Speer_Eisen", "Waffe_Speer_EmberThorn"
		};
		// Werkzeuge haengen im selben Rig, sind aber keine Kampfwaffen: sie werden
		// nur waehrend des Abbaus gezielt eingeblendet (W-001) und muessen bei
		// jeder Waffenumschaltung mit aus (W-003).
		private static readonly string[] ToolMeshNames = { "Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense" };
		private static readonly string[] ToolVariantMeshNames =
		{
			"Waffe_Axt_Base", "Waffe_Axt_Kupfer", "Waffe_Axt_Eisen",
			"Waffe_Spitzhacke_Base", "Waffe_Spitzhacke_Kupfer", "Waffe_Spitzhacke_Eisen",
			"Waffe_Sense_Base", "Waffe_Sense_Kupfer", "Waffe_Sense_Eisen"
		};
		private static readonly string[] RendererCategorySuffixes =
		{
			"_OrganicFace", "_Cloth", "_LeatherWood", "_CopperMetal", "_IronMetal"
		};
		private static readonly int TintId = Shader.PropertyToID("_Tint");

		[SerializeField]
		private Animation animationPlayer;

		[SerializeField]
		private Transform modelRoot;

		[SerializeField]
		private float worldHeight = 1.8f;

		[SerializeField]
		private float turnDegreesPerSecond = 720f;

		[SerializeField]
		private float crossfadeSeconds = 0.12f;

		private Renderer[] _renderers = Array.Empty<Renderer>();
		private readonly Dictionary<string, Transform> _namedNodes = new Dictionary<string, Transform>();
		private MaterialPropertyBlock _properties;
		private Color _tint = Color.white;
		// _stance ist die wirksame Traeger-Stance (Waffe oder Werkzeug-Override),
		// _weaponStance die ausgeruestete Waffe, zu der nach dem Abbau
		// zurueckgekehrt wird.
		private string _stance = StanceBare;
		private string _weaponStance = StanceBare;
		private string _weaponItemId;
		private string _harvestTool;
		private string _harvestToolItemId;
		private bool _weaponVisible;
		private string _currentClip;
		private float _targetYaw = 180f;
		private float _currentYaw = 180f;
		private ProceduralPose _procedural;
		private float _proceduralStartedAt;
		private float _proceduralDuration;
		private Vector3 _modelBasePosition;
		private Vector3 _modelBaseScale = Vector3.one;

		public ActorFacing8 Facing { get; private set; } = ActorFacing8.S;

		public ActorVisualState VisualState { get; private set; } = ActorVisualState.Idle;

		public float WorldHeight => worldHeight;

		public void Configure(Animation configuredAnimation, Transform configuredModelRoot, float configuredWorldHeight = 1.8f)
		{
			animationPlayer = configuredAnimation;
			modelRoot = configuredModelRoot;
			worldHeight = configuredWorldHeight;
			CacheHierarchy();
		}

		private void Awake()
		{
			CacheHierarchy();
		}

		private void OnEnable()
		{
			ActorPresentationRegistry.Register(this);
		}

		private void OnDisable()
		{
			ActorPresentationRegistry.Unregister(this);
		}

		private void CacheHierarchy()
		{
			Transform root = (modelRoot != null) ? modelRoot : base.transform;
			_renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
			_namedNodes.Clear();
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (!_namedNodes.ContainsKey(child.name))
				{
					_namedNodes.Add(child.name, child);
				}
			}
			if (_properties == null)
			{
				_properties = new MaterialPropertyBlock();
			}
			if (modelRoot != null)
			{
				_modelBasePosition = modelRoot.localPosition;
				_modelBaseScale = modelRoot.localScale;
			}
		}

		// ------------------------------------------------------------------
		// IActorPresentation
		// ------------------------------------------------------------------

		public void SetFacing(ActorFacing8 facing)
		{
			Facing = facing;
			_targetYaw = FacingToYaw(facing);
		}

		public void SetVisualState(ActorVisualState state, float normalizedTime = 0f)
		{
			VisualState = state;
			switch (state)
			{
			case ActorVisualState.Idle:
				PlayLoop("Ruhe_" + _stance);
				break;
			case ActorVisualState.Move:
				PlayLoop("Laufen_" + _stance);
				break;
			case ActorVisualState.Ability1:
			case ActorVisualState.Ability2:
				PlayTimed("Angriff_" + _stance, 0f);
				break;
			case ActorVisualState.Hit:
				StartProcedural(ProceduralPose.Hit, 0.2f);
				break;
			case ActorVisualState.Stagger:
				StartProcedural(ProceduralPose.Stagger, 0.5f);
				break;
			case ActorVisualState.Death:
				PlayLoop("Ruhe_" + _stance);
				StartProcedural(ProceduralPose.Death, 1.2f);
				break;
			case ActorVisualState.Appear:
				// Wiederbeleben/Erscheinen loest die Todes-Sperre wieder auf - noetig fuer Revive und Gegner-Pooling.
				_procedural = ProceduralPose.None;
				StartProcedural(ProceduralPose.Appear, 0.6f);
				break;
			}
		}

		public void SetTint(Color color)
		{
			_tint = color;
			ApplyTint(_tint);
		}

		// ------------------------------------------------------------------
		// ILocomotionPresentation
		// ------------------------------------------------------------------

		public void SetLocomotion(Vector3 worldDirection, bool moving)
		{
			worldDirection.y = 0f;
			if (moving && worldDirection.sqrMagnitude > 0.0001f)
			{
				// Kontinuierliche Drehung aus der echten Bewegungsrichtung;
				// SetFacing liefert nur die 45-Grad-Quantisierung als Rueckfall.
				_targetYaw = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
			}
		}

		// ------------------------------------------------------------------
		// IArmorPresentation
		// ------------------------------------------------------------------

		public void SetAssetVariant(string actorId)
		{
			// Atlas-Konzept der Sprite-Darstellung; fuer das Mesh bedeutungslos.
		}

		public void SetArmorParts(bool head, bool chest, bool hands, bool legs)
		{
			SetArmorTiers(head ? 1 : 0, chest ? 1 : 0, hands ? 1 : 0, legs ? 1 : 0);
		}

		public void SetArmorTiers(int head, int chest, int hands, int legs)
		{
			ApplySlot(0, head);
			ApplySlot(1, chest);
			ApplySlot(2, hands);
			ApplySlot(3, legs);
		}

		public static string ResolveClipName(string stem, string stance)
		{
			if (string.IsNullOrEmpty(stem))
			{
				return null;
			}
			if (stem == "idle")
			{
				return "Ruhe_" + stance;
			}
			if (stem == "move")
			{
				return "Laufen_" + stance;
			}
			if (stem.StartsWith("hammer", StringComparison.Ordinal))
			{
				return "Angriff_Hammer";
			}
			if (stem.StartsWith("dagger", StringComparison.Ordinal))
			{
				return "Angriff_Dolche";
			}
			if (stem.StartsWith("spear", StringComparison.Ordinal))
			{
				return "Angriff_Speer";
			}
			if (stem == "harvest")
			{
				// Abbau-Clips existieren nur fuer die Werkzeug-Traeger; ohne
				// Werkzeug bleibt der prozedurale Rueckfall zustaendig (W-001).
				if (stance == StanceAxe || stance == StancePickaxe || stance == StanceScythe)
				{
					return "Abbau_" + stance;
				}
				return null;
			}
			if (stem == "open")
			{
				// W-009: Kisten oeffnen — ein traegerunabhaengiger Clip.
				return "Oeffnen";
			}
			return null;
		}

		public static string ArmorMeshName(int slotIndex, int tier)
		{
			if (slotIndex < 0 || slotIndex >= SlotPrefixes.Length || tier < 1 || tier > 3)
			{
				return null;
			}
			return SlotPrefixes[slotIndex] + TierSuffixes[tier];
		}

		public static float FacingToYaw(ActorFacing8 facing)
		{
			return (int)facing * 45f;
		}

		private void ApplySlot(int slotIndex, int tier)
		{
			// Haare sind ohne Kopfbedeckung sichtbar, unter Kapuze oder Metallhelm
			// jedoch verborgen. So ragen sie nicht durch die Mid-Poly-Ruestung.
			if (slotIndex == 0)
			{
				SetNodeActive("Haare", tier == 0);
			}
			string active = ArmorMeshName(slotIndex, Mathf.Clamp(tier, 0, 3));
			for (int index = 1; index < TierSuffixes.Length; index++)
			{
				string meshName = SlotPrefixes[slotIndex] + TierSuffixes[index];
				SetNodeActive(meshName, meshName == active);
			}
		}

		private void SetNodeActive(string nodeName, bool active)
		{
			// EditMode-Instanzen und einzelne Prefab-Workflows rufen Awake nicht
			// garantiert auf. Die Umschaltung muss auch dann den realen Modellbaum
			// finden; im Player ist der Cache zu diesem Zeitpunkt bereits gefuellt.
			if (_namedNodes.Count == 0)
			{
				CacheHierarchy();
			}
			if (_namedNodes.TryGetValue(nodeName, out Transform node) && node != null && node.gameObject.activeSelf != active)
			{
				node.gameObject.SetActive(active);
			}
			// MIDPOLY-000: Im produktiven Wanderer haengen die geskinnten LOD-Meshes
			// fuer einen sicheren glTF-Roundtrip direkt am Armature-Knoten. Der
			// semantische Schaltknoten bleibt separat erhalten; die zugehoerigen
			// Renderer tragen Namen wie LOD0_Helm_Kupfer_CopperMetal.
			Transform root = modelRoot != null ? modelRoot : transform;
			foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
			{
				if (RendererBelongsToModule(renderer.name, nodeName)
					&& renderer.gameObject.activeSelf != active)
				{
					renderer.gameObject.SetActive(active);
				}
			}
		}

		private static bool RendererBelongsToModule(string rendererName, string moduleName)
		{
			if (string.IsNullOrEmpty(rendererName) || !rendererName.StartsWith("LOD", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			int separator = rendererName.IndexOf('_');
			if (separator < 0)
			{
				return false;
			}
			string payload = rendererName.Substring(separator + 1);
			foreach (string suffix in RendererCategorySuffixes)
			{
				string expected = moduleName + suffix;
				if (payload.StartsWith(expected, StringComparison.Ordinal))
				{
					string remainder = payload.Substring(expected.Length);
					return remainder.Length == 0 || remainder.StartsWith("_Mesh", StringComparison.Ordinal)
						|| remainder.StartsWith(".", StringComparison.Ordinal);
				}
			}
			// ReferenceUpgradeComplete verwendet pro Modul nummerierte
			// Materialteile: LOD0_Harnisch_Kupfer_03_... . Nur eine direkt
			// folgende Ziffer gilt als Treffer, damit generische Familienknoten
			// nicht die konkreten Waffenvarianten mitschalten.
			string numberedPrefix = moduleName + "_";
			return payload.StartsWith(numberedPrefix, StringComparison.Ordinal)
				&& payload.Length > numberedPrefix.Length
				&& char.IsDigit(payload[numberedPrefix.Length]);
		}
	}
}
