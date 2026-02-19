using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;
		[Tooltip("Whether the player can look around")]
		public bool canLook = true;
		[Tooltip("Whether the player can move around")]
		public bool canMove = true;
		[Tooltip("Whether the player can jump")]
		public bool canJump = true;
		[Tooltip("Target to force the player to look at")]
		public Transform ForceLookTarget;
		[Tooltip("Whether forced look is enabled")]
		public bool isForceLooking = false;
		[Tooltip("Speed for forced look rotation")]
		public float forceLookSpeed = 5.0f;
		[Tooltip("Yaw angle (degrees) before pitch starts moving toward the target")]
		public float forceLookPitchStartAngle = 60.0f;
		public CharacterController characterController;

		[Header("Footsteps")]
		[Tooltip("Audio source used to play footstep sounds")]
		public AudioSource FootstepSource;
		[Tooltip("Footstep clips for Concrete")]
		public AudioClip[] ConcreteFootsteps;
		[Tooltip("Footstep clips for Sand")]
		public AudioClip[] SandFootsteps;
		[Tooltip("Footstep clips for Rock")]
		public AudioClip[] RockFootsteps;
		[Tooltip("Minimum pitch for footstep sounds")]
		public float FootstepPitchMin = 0.9f;
		[Tooltip("Maximum pitch for footstep sounds")]
		public float FootstepPitchMax = 1.1f;
		[Tooltip("Seconds between footsteps while walking")]
		public float WalkStepInterval = 0.5f;
		[Tooltip("Seconds between footsteps while sprinting")]
		public float SprintStepInterval = 0.35f;
		[Tooltip("Minimum horizontal speed required to play footsteps")]
		public float FootstepSpeedThreshold = 0.1f;
		[Tooltip("Raycast length used to detect surface for footsteps")]
		public float FootstepRaycastLength = 2.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;
		private float _footstepTimer;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

	
#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
			if (FootstepSource == null)
			{
				FootstepSource = GetComponent<AudioSource>();
			}
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
		}

		private void Update()
		{
			JumpAndGravity();
			GroundedCheck();
			Move();
			UpdateFootsteps();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			if (isForceLooking && ForceLookTarget != null)
			{
				ForceLookAtTarget();
				return;
			}

			if (!canLook)
			{
				return;
			}

			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void ForceLookAtTarget()
		{
			Vector3 origin = CinemachineCameraTarget != null
				? CinemachineCameraTarget.transform.position
				: transform.position;
			Vector3 direction = ForceLookTarget.position - origin;
			if (direction.sqrMagnitude < 0.0001f)
			{
				return;
			}

			float currentYaw = transform.eulerAngles.y;
			float desiredYaw = currentYaw;
			Vector3 planarDirection = new Vector3(direction.x, 0.0f, direction.z);
			if (planarDirection.sqrMagnitude > 0.0001f)
			{
				Quaternion targetYaw = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
				desiredYaw = targetYaw.eulerAngles.y;
				transform.rotation = Quaternion.Slerp(transform.rotation, targetYaw, forceLookSpeed * Time.deltaTime);
			}

			Vector3 localDirection = transform.InverseTransformDirection(direction.normalized);
			float targetPitch = -Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;
			targetPitch = ClampAngle(targetPitch, BottomClamp, TopClamp);
			float yawDelta = Mathf.Abs(Mathf.DeltaAngle(currentYaw, desiredYaw));
			float pitchBlend = Mathf.InverseLerp(forceLookPitchStartAngle, 0.0f, yawDelta);
			_cinemachineTargetPitch = Mathf.LerpAngle(
				_cinemachineTargetPitch,
				targetPitch,
				forceLookSpeed * pitchBlend * Time.deltaTime);
			if (CinemachineCameraTarget != null)
			{
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
			}
		}

		private void Move()
		{
			if (!canMove)
			{
				// keep grounded gravity behavior while preventing horizontal motion
				_controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
				return;
			}

			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		private void UpdateFootsteps()
		{
			if (!canMove || !Grounded || FootstepSource == null)
			{
				_footstepTimer = 0.0f;
				return;
			}

			Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z);
			if (horizontalVelocity.magnitude < FootstepSpeedThreshold)
			{
				_footstepTimer = 0.0f;
				return;
			}

			float stepInterval = _input != null && _input.sprint ? SprintStepInterval : WalkStepInterval;
			_footstepTimer += Time.deltaTime;
			if (_footstepTimer >= stepInterval)
			{
				PlayFootstep();
				_footstepTimer = 0.0f;
			}
		}

		private void PlayFootstep()
		{
			AudioClip[] clips = GetFootstepClipsForSurface();
			if (clips == null || clips.Length == 0)
			{
				return;
			}

			AudioClip clip = clips[Random.Range(0, clips.Length)];
			if (clip == null)
			{
				return;
			}

			FootstepSource.pitch = Random.Range(FootstepPitchMin, FootstepPitchMax);
			FootstepSource.PlayOneShot(clip);
		}

		private AudioClip[] GetFootstepClipsForSurface()
		{
			Vector3 origin = transform.position + Vector3.up * 0.1f;
			if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, FootstepRaycastLength, GroundLayers, QueryTriggerInteraction.Ignore))
			{
				if (hit.collider.CompareTag("Sand"))
				{
					return SandFootsteps;
				}
				if (hit.collider.CompareTag("Rock"))
				{
					return RockFootsteps;
				}
				if (hit.collider.CompareTag("Concrete"))
				{
					return ConcreteFootsteps;
				}
			}

			return ConcreteFootsteps;
		}

		private void JumpAndGravity()
		{
			if (!canMove)
			{
				_input.jump = false;
			}
			if (!canJump)
			{
				_input.jump = false;
			}

			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}

		public void setForceLookTarget(Transform target)
		{
			ForceLookTarget = target;
		}

		public void enableForceLook(bool enable)
		{
			isForceLooking = enable;
		}

		public void toggleMovement(bool enable)
		{
			canMove = enable;
		}

		public void toggleLooking(bool enable)
		{
			canLook = enable;
		}

		public void teleportToPosition(Transform target)
		{
			characterController.enabled = false;
			transform.position = target.position;
			transform.rotation = target.rotation;
			characterController.enabled = true;
		}
	}
}