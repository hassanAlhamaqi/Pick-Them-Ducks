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
        float pitch = 18, gravity,lookYaw;bool wasGrounded;Vector3 lastSafe;
        public bool Braking {get;private set;}
        public void Initialize(DuckGame owner)
        {
            game = owner;lastSafe=transform.position; motor = GetComponent<CharacterController>();
            if(View==null||CarryTarget==null){Debug.LogError("Assign the camera and carry target on the Player prefab.",this);enabled=false;}

        }
        void Update()
        {
            if (game == null || game.MenuOpen) return;
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard == null || mouse == null || Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 look = mouse.delta.ReadValue() * game.Settings.mouseSensitivity;
            ApplyLook(look);
            Vector2 axes = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            axes = Vector2.ClampMagnitude(axes, 1);
            Braking=!game.RidingCar&&keyboard.sKey.isPressed;
            if(!game.RidingCar&&keyboard.sKey.wasPressedThisFrame)game.Physics.FlushFront();
            bool driving=game.CanDrive;
            float speed;
            if(game.RidingCar){float signed=game.Car.TickDrive(game.DriveInput,Time.deltaTime);SteerCar((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),Time.deltaTime);axes=new Vector2(0,Mathf.Sign(signed));speed=Mathf.Abs(signed);driving=game.Car.IsMoving;}
            else{if(driving)axes=new Vector2(axes.x*.35f,1).normalized;speed=driving?game.Progress.DriveSpeed:(keyboard.leftShiftKey.isPressed?game.Settings.sprintSpeed:game.Settings.walkSpeed)*game.Progress.MovementMultiplier;}
            motor.radius=game.Progress.Tool==DuckTool.RollerCar?.95f:.3f;
            gravity = motor.isGrounded && gravity<=0 ? -2 : Mathf.Max(-30, gravity - 25 * Time.deltaTime);
            if(keyboard.spaceKey.wasPressedThisFrame)TryJump();
            var before=transform.position;
            motor.Move((transform.TransformDirection(new Vector3(axes.x, 0, axes.y)) *
                speed + Vector3.up * gravity) * Time.deltaTime);
            if(motor.isGrounded&&!wasGrounded&&gravity< -5)game.particles?.Play(game.particles.land,transform.position);wasGrounded=motor.isGrounded;
            if(driving)foreach(Transform part in (game.RidingCar?game.Car.visual:game.Stage.ToolModels[game.Progress.Data.currentTool]))if(part.name.Contains("Roller")||part.name.Contains("Wheel"))part.Rotate(0,(game.RidingCar?game.Car.CurrentSpeed:speed)*Time.deltaTime*90,0,Space.Self);
            if(game.Park!=null)
            {
                if(motor.isGrounded&&(!game.Park.InLake(transform.position)||game.Park.WalkableWater(transform.position)))lastSafe=transform.position;
                if(game.Park.InLake(transform.position)&&transform.position.y<game.Park.waterHeight+.05f)Teleport(lastSafe+Vector3.up*.1f);
                else if(Mathf.Abs(transform.position.x)>145||transform.position.z< -18||transform.position.z>280)Teleport(before);
            }
        }
        public void ApplyLook(Vector2 look){if(game.RidingCar)lookYaw=Mathf.Clamp(lookYaw+look.x,-120,120);else transform.Rotate(0,look.x,0);pitch=Mathf.Clamp(pitch-look.y,-75,78);View.transform.localEulerAngles=new Vector3(pitch,lookYaw,0);}
        public void SteerCar(float steer,float deltaTime){if(game.RidingCar&&game.Car.IsMoving)transform.Rotate(0,Mathf.Clamp(steer,-1,1)*game.Car.steeringSpeed*Mathf.Clamp01(Mathf.Abs(game.Car.CurrentSpeed)/5)*Mathf.Sign(game.Car.CurrentSpeed)*deltaTime,0);}
        public void ResetLook(){lookYaw=0;View.transform.localEulerAngles=new Vector3(pitch,0,0);}
        public bool TryJump(){if(game.RidingCar||motor==null||!motor.isGrounded||gravity>0||game.MenuOpen)return false;gravity=Mathf.Sqrt(2*25*1.35f);game.particles?.Play(game.particles.jump,transform.position);return true;}
        public void Teleport(Vector3 position) { motor.enabled = false; transform.position = position; motor.enabled = true; gravity = 0;wasGrounded=true;game?.ResetToolSweep(); }
    }
}
