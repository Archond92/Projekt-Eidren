using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Gameplay.Flow;
using Eidren.Interaction;
using Eidren.Player;
using Eidren.UI;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class AudioRuntimeBinder : MonoBehaviour
	{
		private readonly List<Action> _sceneUnbinders = new List<Action>();

		private readonly List<Action> _persistentUnbinders = new List<Action>();

		private readonly HashSet<int> _boundWorldItems = new HashSet<int>();

		private AudioService _audio;

		private SceneFlowService _sceneFlow;

		private PauseService _pause;

		private int _bindGeneration;

		private bool _initialized;

		public void Initialize(AudioService audio, SceneFlowService sceneFlow, PauseService pause)
		{
			UnbindPersistent();
			_audio = audio ?? throw new ArgumentNullException("audio");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_pause = pause ?? throw new ArgumentNullException("pause");
			_sceneFlow.SceneTransitionStarted += HandleSceneTransitionStarted;
			_pause.PauseChanged += HandlePauseChanged;
			SceneManager.sceneLoaded += HandleSceneLoaded;
			BindPersistentButtons();
			_initialized = true;
			RefreshBindings();
		}

		public void RefreshBindings()
		{
			if (_initialized)
			{
				int generation = ++_bindGeneration;
				if (Application.isPlaying)
				{
					StartCoroutine(BindAfterFrame(generation));
				}
				else
				{
					BindCurrentScene(SceneManager.GetActiveScene());
				}
			}
		}

		public void BindWorldItem(WorldItemController item)
		{
			if (item == null || !_boundWorldItems.Add(item.GetInstanceID()))
			{
				return;
			}
			Action handler = delegate
			{
				_audio.PlayOneShot("ui.item_pickup", item.transform.position);
			};
			item.PickupAudioRequested += handler;
			_sceneUnbinders.Add(delegate
			{
				if (item != null)
				{
					item.PickupAudioRequested -= handler;
				}
			});
		}

		public void BindCurrentScene(Scene scene)
		{
			if (_initialized && scene.IsValid() && scene.isLoaded)
			{
				UnbindScene();
				_audio.PlaySceneAudio(scene.name);
				PlayerPrefabBindings[] array = FindSceneComponents<PlayerPrefabBindings>(scene);
				foreach (PlayerPrefabBindings player in array)
				{
					BindPlayer(player);
				}
				WildlingController[] array2 = FindSceneComponents<WildlingController>(scene);
				foreach (WildlingController wildling in array2)
				{
					BindWildling(wildling);
				}
				BossController[] array3 = FindSceneComponents<BossController>(scene);
				foreach (BossController boss in array3)
				{
					BindBoss(boss);
				}
				ResourceNode[] array4 = FindSceneComponents<ResourceNode>(scene);
				foreach (ResourceNode resource in array4)
				{
					BindResource(resource);
				}
				WorldItemController[] array5 = FindSceneComponents<WorldItemController>(scene);
				foreach (WorldItemController item in array5)
				{
					BindWorldItem(item);
				}
				EidraForgeChestContainer[] array6 = FindSceneComponents<EidraForgeChestContainer>(scene);
				foreach (EidraForgeChestContainer chest in array6)
				{
					BindForgeChest(chest);
				}
				InventoryWindow[] array7 = FindSceneComponents<InventoryWindow>(scene);
				foreach (InventoryWindow window in array7)
				{
					BindMenuVisibility(window, unused: true);
				}
				CraftingWindow[] array8 = FindSceneComponents<CraftingWindow>(scene);
				foreach (CraftingWindow window2 in array8)
				{
					BindCrafting(window2);
				}
				StorageWindow[] array9 = FindSceneComponents<StorageWindow>(scene);
				foreach (StorageWindow window3 in array9)
				{
					BindMenuVisibility(window3, unused: true);
				}
				GameFlowController[] array10 = FindSceneComponents<GameFlowController>(scene);
				foreach (GameFlowController flow in array10)
				{
					BindGameFlow(flow);
				}
				Button[] array11 = FindSceneComponents<Button>(scene);
				foreach (Button button in array11)
				{
					BindButton(button, _sceneUnbinders);
				}
			}
		}

		private IEnumerator BindAfterFrame(int generation)
		{
			yield return null;
			if (generation == _bindGeneration)
			{
				BindCurrentScene(SceneManager.GetActiveScene());
			}
		}

		private void BindPlayer(PlayerPrefabBindings player)
		{
			if (player == null)
			{
				return;
			}
			PlayerMotor motor = player.Motor;
			PlayerCombatController combat = player.Combat;
			ConsumableController consumables = player.Consumables;
			Damageable health = player.Damageable;
			if (motor != null)
			{
				Action<Vector3> footstep = delegate(Vector3 position)
				{
					_audio.PlayOneShot("player.footstep", position);
				};
				Action dodge = delegate
				{
					_audio.PlayOneShot("player.dodge", motor.transform.position);
				};
				motor.Footstep += footstep;
				motor.DodgeStarted += dodge;
				_sceneUnbinders.Add(delegate
				{
					if (!(motor == null))
					{
						motor.Footstep -= footstep;
						motor.DodgeStarted -= dodge;
					}
				});
			}
			if (combat != null)
			{
				Action<float, int, WeaponFamily> attack = delegate(float duration, int combo, WeaponFamily family)
				{
					AudioService audio = _audio;
					if (1 == 0)
					{
					}
					string eventId = family switch
					{
						WeaponFamily.Daggers => "player.attack.daggers", 
						WeaponFamily.Spear => "player.attack.spear", 
						_ => "player.attack.hammer", 
					};
					if (1 == 0)
					{
					}
					audio.PlayOneShot(eventId, combat.transform.position);
				};
				Action<bool> hit = delegate
				{
					_audio.PlayOneShot("player.hit", combat.transform.position);
				};
				combat.AttackStarted += attack;
				combat.HitLanded += hit;
				_sceneUnbinders.Add(delegate
				{
					if (!(combat == null))
					{
						combat.AttackStarted -= attack;
						combat.HitLanded -= hit;
					}
				});
			}
			if (consumables != null)
			{
				Action<ItemUseActionType> used = delegate(ItemUseActionType action)
				{
					_audio.PlayOneShot((action == ItemUseActionType.Heal) ? "player.healing_potion" : "player.buff_food", consumables.transform.position);
				};
				consumables.ConsumableUsed += used;
				_sceneUnbinders.Add(delegate
				{
					if (consumables != null)
					{
						consumables.ConsumableUsed -= used;
					}
				});
			}
			if (!(health != null))
			{
				return;
			}
			Action<DamageInfo> damaged = delegate
			{
				_audio.PlayOneShot("player.damage", health.transform.position);
			};
			Action died = delegate
			{
				_audio.PlayOneShot("state.defeat");
			};
			health.Damaged += damaged;
			health.Died += died;
			_sceneUnbinders.Add(delegate
			{
				if (!(health == null))
				{
					health.Damaged -= damaged;
					health.Died -= died;
				}
			});
		}

		private void BindWildling(WildlingController wildling)
		{
			Action<WildlingFeedbackCue> feedback = delegate(WildlingFeedbackCue cue)
			{
				string eventId = ResolveEnemyAudioEvent(wildling, cue);
				_audio.PlayOneShot(eventId, wildling.transform.position);
			};
			wildling.FeedbackRequested += feedback;
			_sceneUnbinders.Add(delegate
			{
				if (wildling != null)
				{
					wildling.FeedbackRequested -= feedback;
				}
			});
		}

		private void BindForgeChest(EidraForgeChestContainer chest)
		{
			Action<EidraForgeChestContainer> activated = delegate(EidraForgeChestContainer value)
			{
				_audio.PlayOneShot(AudioEventIds.V02Container(ForgeContainerFamily(value.ContainerId), "open"), value.transform.position);
			};
			Action<StorageContainerChange> changed = delegate
			{
				bool flag = true;
				ItemStack[] array = chest.ExportSlots();
				foreach (ItemStack itemStack in array)
				{
					if (!itemStack.IsEmpty)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					_audio.PlayOneShot(AudioEventIds.V02Container(ForgeContainerFamily(chest.ContainerId), "empty"), chest.transform.position);
				}
			};
			chest.Activated += activated;
			chest.Changed += changed;
			_sceneUnbinders.Add(delegate
			{
				if (!(chest == null))
				{
					chest.Activated -= activated;
					chest.Changed -= changed;
				}
			});
		}

		private static string ForgeContainerFamily(string id)
		{
			if (id.StartsWith("forge.supply", StringComparison.Ordinal))
			{
				return "forge_supply";
			}
			if (id.StartsWith("forge.optional", StringComparison.Ordinal))
			{
				return "forge_optional";
			}
			if (id.StartsWith("forge.elite", StringComparison.Ordinal))
			{
				return "forge_elite";
			}
			if (id.StartsWith("forge.completion", StringComparison.Ordinal))
			{
				return "forge_completion";
			}
			if (id.EndsWith("reward.small", StringComparison.Ordinal))
			{
				return "reward_small";
			}
			if (id.EndsWith("reward.medium", StringComparison.Ordinal))
			{
				return "reward_medium";
			}
			if (id.EndsWith("reward.large", StringComparison.Ordinal))
			{
				return "reward_large";
			}
			return "recovery";
		}

		private static string ResolveEnemyAudioEvent(WildlingController enemy, WildlingFeedbackCue cue)
		{
			string text = ((enemy != null && enemy.Definition != null) ? enemy.Definition.Id : string.Empty);
			string result;
			if (string.Equals(text, "enemy.wildling", StringComparison.Ordinal))
			{
				if (1 == 0)
				{
				}
				result = cue switch
				{
					WildlingFeedbackCue.Discovered => "wildling.discover", 
					WildlingFeedbackCue.Attack => "wildling.attack", 
					WildlingFeedbackCue.Hit => "wildling.hit", 
					WildlingFeedbackCue.Stagger => "wildling.stagger", 
					_ => "wildling.death", 
				};
				if (1 == 0)
				{
				}
				return result;
			}
			if (1 == 0)
			{
			}
			result = cue switch
			{
				WildlingFeedbackCue.Discovered => "discover", 
				WildlingFeedbackCue.Attack => "attack", 
				WildlingFeedbackCue.Hit => "hit", 
				WildlingFeedbackCue.Stagger => "stagger", 
				_ => "death", 
			};
			if (1 == 0)
			{
			}
			string cue2 = result;
			return AudioEventIds.V02Enemy(text, cue2);
		}

		private void BindBoss(BossController boss)
		{
			Action<bool> battle = delegate(bool active)
			{
				if (active)
				{
					_audio.PlayOneShot("garon.discover", boss.transform.position);
				}
				_audio.SetBossMusicActive(active);
			};
			Action<GaronAttackType> attack = delegate(GaronAttackType type)
			{
				if (1 == 0)
				{
				}
				string text = type switch
				{
					GaronAttackType.Front => "garon.attack.front", 
					GaronAttackType.Charge => "garon.attack.charge", 
					_ => "garon.attack.spin", 
				};
				if (1 == 0)
				{
				}
				string eventId = text;
				_audio.PlayOneShot(eventId, boss.transform.position);
			};
			Action<DamageInfo, float, float> damaged = delegate
			{
				_audio.PlayOneShot("garon.hit", boss.transform.position);
			};
			Action<EnemyState, EnemyState> state = delegate(EnemyState previous, EnemyState current)
			{
				if (current == EnemyState.Staggered)
				{
					_audio.PlayOneShot("garon.stagger", boss.transform.position);
				}
			};
			Action died = delegate
			{
				_audio.PlayOneShot("garon.death", boss.transform.position);
				_audio.PlayOneShot("state.victory");
				_audio.SetBossMusicActive(active: false);
			};
			boss.BattleActivityChanged += battle;
			boss.AttackSelected += attack;
			boss.DamageResolved += damaged;
			boss.EnemyStateChanged += state;
			boss.Died += died;
			if (boss.BattleActive)
			{
				battle(obj: true);
			}
			_sceneUnbinders.Add(delegate
			{
				if (!(boss == null))
				{
					boss.BattleActivityChanged -= battle;
					boss.AttackSelected -= attack;
					boss.DamageResolved -= damaged;
					boss.EnemyStateChanged -= state;
					boss.Died -= died;
				}
			});
		}

		private void BindResource(ResourceNode resource)
		{
			Action<ResourceCollectionResult> collected = delegate(ResourceCollectionResult result)
			{
				string text = result.Item?.Id;
				if (1 == 0)
				{
				}
				string text2 = text switch
				{
					"wood" => "resource.wood", 
					"stone" => "resource.stone", 
					"plant_fiber" => "resource.plant_fiber", 
					"copper_ore" => "resource.copper_ore", 
					"hardwood" => AudioEventIds.V02Resource("hardwood", "hit"), 
					"swamp_hemp" => AudioEventIds.V02Resource("swamp_hemp", "hit"), 
					"granite" => AudioEventIds.V02Resource("granite", "hit"), 
					"iron_ore" => AudioEventIds.V02Resource("iron_ore", "hit"), 
					_ => string.Empty, 
				};
				if (1 == 0)
				{
				}
				string text3 = text2;
				if (!string.IsNullOrEmpty(text3))
				{
					_audio.PlayOneShot(text3, resource.transform.position);
				}
			};
			resource.Collected += collected;
			_sceneUnbinders.Add(delegate
			{
				if (resource != null)
				{
					resource.Collected -= collected;
				}
			});
		}

		private void BindCrafting(CraftingWindow window)
		{
			Action<bool> visibility = delegate(bool opened)
			{
				_audio.PlayOneShot(opened ? "ui.menu_open" : "ui.menu_close");
			};
			Action<CraftingResult> result = delegate(CraftingResult value)
			{
				_audio.PlayOneShot(value.Succeeded ? "ui.crafting_success" : "ui.crafting_failure");
			};
			window.VisibilityChanged += visibility;
			window.CraftResultPresented += result;
			_sceneUnbinders.Add(delegate
			{
				if (!(window == null))
				{
					window.VisibilityChanged -= visibility;
					window.CraftResultPresented -= result;
				}
			});
		}

		private void BindMenuVisibility(InventoryWindow window, bool unused)
		{
			Action<bool> visibility = delegate(bool opened)
			{
				_audio.PlayOneShot(opened ? "ui.menu_open" : "ui.menu_close");
			};
			window.VisibilityChanged += visibility;
			_sceneUnbinders.Add(delegate
			{
				if (window != null)
				{
					window.VisibilityChanged -= visibility;
				}
			});
		}

		private void BindMenuVisibility(StorageWindow window, bool unused)
		{
			Action<bool> visibility = delegate(bool opened)
			{
				_audio.PlayOneShot(opened ? "ui.menu_open" : "ui.menu_close");
			};
			window.VisibilityChanged += visibility;
			_sceneUnbinders.Add(delegate
			{
				if (window != null)
				{
					window.VisibilityChanged -= visibility;
				}
			});
		}

		private void BindGameFlow(GameFlowController flow)
		{
			Action<bool, float, int> ended = delegate(bool victory, float duration, int staggers)
			{
				_audio.PlayOneShot(victory ? "state.victory" : "state.defeat");
			};
			flow.GameEnded += ended;
			_sceneUnbinders.Add(delegate
			{
				if (flow != null)
				{
					flow.GameEnded -= ended;
				}
			});
		}

		private void BindPersistentButtons()
		{
			Button[] componentsInChildren = GetComponentsInChildren<Button>(includeInactive: true);
			foreach (Button button in componentsInChildren)
			{
				BindButton(button, _persistentUnbinders);
			}
		}

		private void BindButton(Button button, List<Action> unbinders)
		{
			if (button == null)
			{
				return;
			}
			UnityAction handler = delegate
			{
				_audio.PlayOneShot("ui.button");
			};
			button.onClick.AddListener(handler);
			unbinders.Add(delegate
			{
				if (button != null)
				{
					button.onClick.RemoveListener(handler);
				}
			});
		}

		private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			RefreshBindings();
		}

		private void HandleSceneTransitionStarted(string sceneKey)
		{
			_audio.PlayOneShot("state.scene_transition");
		}

		private void HandlePauseChanged(bool paused)
		{
			_audio.PlayOneShot(paused ? "ui.menu_open" : "ui.menu_close");
		}

		private static T[] FindSceneComponents<T>(Scene scene) where T : Component
		{
			List<T> list = new List<T>();
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				list.AddRange(gameObject.GetComponentsInChildren<T>(includeInactive: true));
			}
			return list.ToArray();
		}

		private void UnbindScene()
		{
			for (int num = _sceneUnbinders.Count - 1; num >= 0; num--)
			{
				_sceneUnbinders[num]?.Invoke();
			}
			_sceneUnbinders.Clear();
			_boundWorldItems.Clear();
		}

		private void UnbindPersistent()
		{
			UnbindScene();
			for (int num = _persistentUnbinders.Count - 1; num >= 0; num--)
			{
				_persistentUnbinders[num]?.Invoke();
			}
			_persistentUnbinders.Clear();
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionStarted -= HandleSceneTransitionStarted;
			}
			if (_pause != null)
			{
				_pause.PauseChanged -= HandlePauseChanged;
			}
			SceneManager.sceneLoaded -= HandleSceneLoaded;
		}

		private void OnDestroy()
		{
			UnbindPersistent();
		}
	}
}
