using System.Threading.Tasks;
using Shooter.Shared;
using Godot;
using System;

namespace Shooter.Client.Simulation.Components
{
    public enum CameraMode
    {
        FPS,
        TPS,
        Follow,
        Server,
        Dugeon
    }
    public partial class PlayerCameraComponent : Camera3D, IComponent<PlayerSimulation>
    {
        public PlayerSimulation MainComponent { get; set; }
        public const float tpsDamping = 1;
        public float Sensitivity = 2f;
        private float rotX = 0.0f;
        private float rotY = 0.0f;
        private Vector3 cameraOffset = new Vector3(0, 0.5f, 0.1f);
        private float tpsCameraDistance = 4f;
        public bool enableInput = true;

        public CameraMode cameraMode = CameraMode.FPS;

        public override void _EnterTree()
        {
            base._EnterTree();

            var rotation = this.Transform.basis.GetEuler();

            this.rotX = rotation.x;
            this.rotY = rotation.y;
        }

        public override void _Process(float delta)
        {
            var body = this.MainComponent.Components.Get<PlayerBodyComponent>();
            if (body != null)
            {
                if (this.cameraMode == CameraMode.TPS)
                {
                    var targetNode = body.Transform;
                    var transform = this.Transform;
                    transform.origin.x = targetNode.origin.x + tpsCameraDistance * Mathf.Cos(rotY * -1);
                    transform.origin.z = targetNode.origin.z + tpsCameraDistance * Mathf.Sin(rotY * -1);
                    this.Transform = transform;

                    this.Transform = this.Transform.LookingAt(body.Transform.origin, Vector3.Up);
                }
                else if (this.cameraMode == CameraMode.FPS)
                {
                    var transform = body.Transform;

                    transform.origin = transform.origin + cameraOffset;
                    transform.origin.y = transform.origin.y + body.getCrouchingHeight();
                    transform.basis = new Basis(new Vector3(rotX, rotY, 0));

                    this.Transform = transform;
                }
                else if (this.cameraMode == CameraMode.Server)
                {
                    var transform = body.Transform;
                    this.Transform = transform;
                }
            }
        }

        public Quaternion getViewRotation()
        {
            return this.Transform.basis.GetRotationQuaternion();
        }
        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (@event is InputEventMouseMotion && enableInput)
            {
                // Handle cursor lock state
                if (Input.GetMouseMode() == Input.MouseMode.Captured)
                {
                    var ev = @event as InputEventMouseMotion;
                    rotX -= ev.Relative.y * (Sensitivity / 1000);
                    rotY -= ev.Relative.x * (Sensitivity / 1000);
                    rotX = Mathf.Clamp(rotX, Mathf.Deg2Rad(-90), Mathf.Deg2Rad(90));
                }
            }

            @event.Dispose();
        }
    }
}