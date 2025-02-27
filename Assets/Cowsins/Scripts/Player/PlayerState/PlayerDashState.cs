using UnityEngine;
using System;
namespace cowsins
{
    public class PlayerDashState : PlayerBaseState
    {

        public PlayerDashState(PlayerStates currentContext, PlayerStateFactory playerStateFactory, Vector2 inp)
            : base(currentContext, playerStateFactory) { input = inp; }

        private PlayerMovement player;

        private Rigidbody rb;

        private float dashTimer;

        private Vector2 input;

        private EventHandler onDashNoInfinite;
        public override void EnterState()
        {
            player = _ctx.GetComponent<PlayerMovement>();
            rb = _ctx.GetComponent<Rigidbody>();
            onDashNoInfinite = player.RegainDash;
            dashTimer = player.dashDuration;
            player.dashing = true;
            rb.useGravity = true;

            player.events.OnStartDash.Invoke();

            Camera playerCam = player.GetComponent<WeaponController>().mainCamera.GetComponent<Camera>();

            playerCam.fieldOfView += player.fovToAddOnDash;

            if (!player.infiniteDashes)
            {
                player.currentDashes--;
                onDashNoInfinite?.Invoke(this, EventArgs.Empty);
            }

            //staminaLoss
            if (player.usesStamina) player.ReduceStamina(player.StaminaLossOnDash);

            // Gather Inputs on Enter State to avoid No reg issues
            input = InputManager.inputActions.GameControls.Movement.ReadValue<Vector2>();

        }

        public override void UpdateState()
        {

            player.events.OnDashing?.Invoke();

            Vector3 dir = GetProperDirection();
            rb.AddForce(dir * player.dashForce * Time.deltaTime, ForceMode.Impulse);

            CheckSwitchState();
        }
        public override void FixedUpdateState()
        {
        }

        public override void ExitState()
        {
            player.events.OnEndDash?.Invoke();
            player.dashing = false;
            rb.useGravity = true;
        }

        public override void CheckSwitchState()
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0 || !player.dashing) SwitchState(_factory.Default());

        }

        private Vector3 CameraBasedInput()
        {
            Vector3 forward = player.playerCam.transform.forward;
            Vector3 right = player.playerCam.transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 dir = forward * input.y + right * input.x;

            return dir;

        }

        private Vector3 GetProperDirection()
        {
            Vector3 direction = Vector3.zero;
            switch (player.dashMethod)
            {
                case PlayerMovement.DashMethod.ForwardAlways:
                    direction = player.orientation.forward;
                    break;
                case PlayerMovement.DashMethod.Free:
                    direction = player.playerCam.forward;
                    break;
                case PlayerMovement.DashMethod.InputBased:
                    direction = (input.x == 0 && input.y == 0) ? player.orientation.forward : CameraBasedInput();
                    break;
            }
            return direction;
        }
    }
}