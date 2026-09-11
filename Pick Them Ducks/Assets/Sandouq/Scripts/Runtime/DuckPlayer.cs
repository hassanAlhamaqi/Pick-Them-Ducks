using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandouq.Ducks
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DuckPlayer : MonoBehaviour
    {
        public Camera View;
        public Transform CarryTarget;
        DuckGame game;
        CharacterController motor;
        float pitch = 18, gravity;
        public void Initialize(DuckGame owner)
        {
            game = owner; motor = GetComponent<CharacterController>();
            motor.height = 1.8f; motor.radius = .3f; motor.center = Vector3.up * .9f;
            if(View != null && CarryTarget != null)return;
            var cameraObject = new GameObject("Player camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform, false); cameraObject.transform.localPosition = Vector3.up * 1.7f;
            View = cameraObject.GetComponent<Camera>(); View.tag = "MainCamera"; View.fieldOfView = 72; View.nearClipPlane = .06f; View.farClipPlane = 400;
            View.clearFlags = CameraClearFlags.SolidColor; View.backgroundColor = new Color(.65f, .84f, .91f);
            View.transform.localEulerAngles = new Vector3(pitch, 0, 0);
            CarryTarget = new GameObject("Carry target").transform; CarryTarget.SetParent(View.transform, false);
            CarryTarget.localPosition = new Vector3(.38f, -.42f, .7f);
        }
        void Update()
        {
            if (game == null || game.MenuOpen) return;
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard == null || mouse == null || Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 look = mouse.delta.ReadValue() * game.Settings.mouseSensitivity;
            transform.Rotate(0, look.x, 0); pitch = Mathf.Clamp(pitch - look.y, -75, 78);
            View.transform.localEulerAngles = new Vector3(pitch, 0, 0);
            Vector2 axes = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            axes = Vector2.ClampMagnitude(axes, 1);
            gravity = motor.isGrounded ? -2 : Mathf.Max(-30, gravity - 25 * Time.deltaTime);
            var before=transform.position;
            motor.Move((transform.TransformDirection(new Vector3(axes.x, 0, axes.y)) *
                (keyboard.leftShiftKey.isPressed ? game.Settings.sprintSpeed : game.Settings.walkSpeed) * (game.CarryingCasket ? .65f : 1f) + Vector3.up * gravity) * Time.deltaTime);
            if(game.Park!=null && (game.Park.InLake(transform.position) || Mathf.Abs(transform.position.x)>145 || transform.position.z < -18 || transform.position.z>280))Teleport(before);
        }
        public void Teleport(Vector3 position) { motor.enabled = false; transform.position = position; motor.enabled = true; gravity = 0; }
    }
}
