﻿using Managers;
using Managers.Timers;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Entities {
	[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
	public class Player : MonoSingleton<Player> {

		#region PlayerControl
		[System.Serializable]
		private struct PlayerControl {
			[SerializeField, SearchableEnum]
			private KeyCode up;

			[SerializeField, SearchableEnum]
			private KeyCode down;

			[SerializeField, SearchableEnum]
			private KeyCode left;

			[SerializeField, SearchableEnum]
			private KeyCode right;

			[SerializeField, SearchableEnum]
			private KeyCode bomb;

			public KeyCode Up { get => up; }
			public KeyCode Down { get => down; }
			public KeyCode Left { get => left; }
			public KeyCode Right { get => right; }
			public KeyCode Bomb { get => bomb; }
		}
		#endregion

		#region PlayerAnimation
		[System.Serializable]
		private struct PlayerAnimation {
			[SerializeField, MustBeAssigned]
			private AnimationClip defaultIdle;

			[SerializeField, MustBeAssigned]
			private AnimationClip idleFront;

			[SerializeField, MustBeAssigned]
			private AnimationClip idleBack;

			[SerializeField, MustBeAssigned]
			private AnimationClip defaultMove;

			[SerializeField, MustBeAssigned]
			private AnimationClip frontMove;

			[SerializeField, MustBeAssigned]
			private AnimationClip moveBack;

			public AnimationClip DefaultIdle { get => defaultIdle; }
			public AnimationClip IdleFront { get => idleFront; }
			public AnimationClip IdleBack { get => idleBack; }
			public AnimationClip DefaultMove { get => defaultMove; }
			public AnimationClip FrontMove { get => frontMove; }
			public AnimationClip MoveBack { get => moveBack; }
		}
		#endregion

		#region Direction_Indicator

		[System.Serializable]
		private struct DirectionIndicator {
			[SerializeField, MustBeAssigned]
			private GameObject indicator;

			[SerializeField]
			private Vector2 direction;

			public Vector2 Direction { get => direction; }
			public GameObject Indicator { get => indicator; }
		}

		#endregion

		[SerializeField, AutoProperty]
		private Rigidbody2D rb;

		[SerializeField, AutoProperty]
		private SpriteRenderer spriteRenderer;

		[Separator("Controls")]
		[SerializeField, Tooltip("Controls for the player"), MustBeAssigned]
		private PlayerControl controls;

		[SerializeField, Tooltip("Reload bar for the player"), MustBeAssigned]
		private ReloadBar reloadBar;

		[Separator("Speed")]
		[SerializeField, Tooltip("Move speed of the player"), PositiveValueOnly]
		private float moveSpeed;

		[SerializeField, Tooltip("How much value to add to speed when upgraded"), PositiveValueOnly]
		private float upgradeSpeedValue;

		[Separator("Bomb")]
		[SerializeField, Tooltip("Prefab for the bomb"), PositiveValueOnly]
		private Bomb bombPrefab;

		[SerializeField, Tooltip("Cooldown before the next bomb throw"), PositiveValueOnly]
		private float bombCooldown;

		[Separator("Animation")]
		[SerializeField, Tooltip("Animations for the player"), MustBeAssigned]
		private PlayerAnimation animations;

		[SerializeField, AutoProperty]
		private Animator animator;

		private Vector2 currentDirection;

		private bool bombAvailable;

		protected override void OnAwake() {
			if(rb == null) {
				rb = GetComponent<Rigidbody2D>();
			}

			if(animator == null) {
				animator = GetComponent<Animator>();
			}

			if(spriteRenderer == null) {
				spriteRenderer = GetComponent<SpriteRenderer>();
			}

			bombAvailable = true;

			currentDirection = Vector2.right;
			reloadBar.SetProperties(moveSpeed * 2f, bombCooldown);
		}

		private void Update() {
			if(GameManager.Instance.GameOver) { return; }

			Vector2 input = UpdateMoveDirection();

			//SetDirectionIndicators(input);
			currentDirection = input == Vector2.zero ? currentDirection : input;
			UpdateAnimationByInputDirection(input);

			UpdateBombTrigger();

			#region Local_Function

			void UpdateBombTrigger() {
				if((Input.GetKeyDown(controls.Bomb) || Input.GetKeyDown(KeyCode.Joystick1Button0)) && bombAvailable) {
					ThrowBomb();
				}
			}

			Vector2 UpdateMoveDirection() {
				float xInput = Input.GetAxis("Horizontal");
				float yInput = Input.GetAxis("Vertical");

				Vector2 inputDirection = new Vector2(xInput, yInput);
				//Debug.Log(inputDirection);

				if(inputDirection == Vector2.zero) {
					if(Input.GetKey(controls.Up)) {
						inputDirection += Vector2.up;
					}
					if(Input.GetKey(controls.Down)) {
						inputDirection += Vector2.down;
					}
					if(Input.GetKey(controls.Left)) {
						inputDirection += Vector2.left;
					}
					if(Input.GetKey(controls.Right)) {
						inputDirection += Vector2.right;
					}
				}

				rb.velocity = moveSpeed * inputDirection.normalized;

				return inputDirection;
			}

			void UpdateAnimationByInputDirection(Vector2 direction) {

				if(direction == Vector2.zero) {
					// Not moving; Check idle direction.
					if(currentDirection.y < -0.1f && currentDirection.x == 0) {
						animator.Play(animations.IdleFront.name);
					} else if(currentDirection.y >= 0.1f) {
						animator.Play(animations.IdleBack.name);
					} else {
						animator.Play(animations.DefaultIdle.name);
					}
				} else {
					if(direction.y < -0.1f && direction.x == 0) {
						animator.Play(animations.FrontMove.name);
					} else if(direction.y >= 0.1f) {
						animator.Play(animations.MoveBack.name);
					} else {
						animator.Play(animations.DefaultMove.name);
					}
				}

				SetFacingDirection(direction);
			}

			void SetFacingDirection(Vector2 direction) {
				Vector2 facing = transform.localScale;

				float directionX = 0;

				if(direction.x == 0) {
					directionX = facing.x;
				} else {
					directionX = direction.x;
				}

				if(directionX >= 0) {
					directionX = 1;
				} else {
					directionX = -1;
				}

				facing.x = directionX;

				transform.localScale = facing;
			}

			#endregion
		}

		private void ThrowBomb() {
			reloadBar.TriggerReload();
			bombAvailable = false;

			Bomb newBomb = Instantiate(bombPrefab);
			newBomb.transform.position = transform.position;
			newBomb.Throw(currentDirection);

			SoundManager.Instance.PlayAudioByType(AudioType.Bomb_Throw);
			CallbackTimerManager.Instance.AddTimer(bombCooldown, RefreshBomb);
		}

		private void RefreshBomb() {
			bombAvailable = true;
		}

		private void OnCollisionEnter2D(Collision2D other) {
			if(other.gameObject.CompareTag("Enemy")) {
				//Debug.Log("Hit eneymy");

				TriggerHit();
			}
		}

		private void TriggerHit() {
			EffectManager.Instance.CreateStarRing(transform.position);
			EffectManager.Instance.CreateScreenShake();

			EnemyManager.Instance.KillAllEnemies();
			GameManager.Instance.DecreaseHealth();

			SoundManager.Instance.PlayAudioByType(AudioType.Player_Hit);

			StartCoroutine(HitBlink(5, .1f));

			#region Local_Function

			IEnumerator HitBlink(int blinkNum, float blinkInterval) {
				for(int i = 0; i < blinkNum * 2; i++) {
					spriteRenderer.enabled = !spriteRenderer.enabled;
					yield return new WaitForSeconds(blinkInterval);
				}
			}

			#endregion
		}

		internal void Upgrade() {
			moveSpeed += upgradeSpeedValue;

			// Slightly decrease bomb cooldown
			bombCooldown -= (bombCooldown * 0.025f);

			reloadBar.SetProperties(moveSpeed * 2f, bombCooldown);
		}

		internal void Kill() {
			Destroy(reloadBar.gameObject);
			Destroy(gameObject);
		}
	}
}
