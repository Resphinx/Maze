using UnityEngine;
namespace Resphinx.Maze
{
    public class MazePOV
    {
        public Transform feet, camera, character, head;
        public MazeOwner owner;
        public CameraPosition type;
        protected Transform root;
        protected float currentBounceAngle = 0;
        public float bounceAngleChange = Mathf.PI * 3.5f;
        public Vector2 Offset { get { return offset; } set { offset = value; } }
        protected Vector2 offset;
        public MazePOV(MazeOwner mazeOwner, string name)
        {
            this.owner = mazeOwner;
            root = owner.maze.root.transform;
            feet = new GameObject("feet-" + name + "-" + owner.mazeIndex).transform;
            feet.parent = root;
            camera = new GameObject("camera").transform;
            camera.parent = feet;
            character = new GameObject("character").transform;
            character.parent = feet;
            head = new GameObject("head").transform;
            head.parent = feet;
        }
        public virtual void Update(Vector3 position, Vector3 forward, float tilt = 0) { }
    }
    public class FirstPerson : MazePOV
    {
        public FirstPerson(MazeOwner owner) : base(owner, "fpov") {
            type = CameraPosition.FirstPerson;
        }
        public override void Update(Vector3 position, Vector3 forward, float tilt = 0)
        {
            feet.localPosition = position;
            Vector3 p = owner.walker.movementMode == MovementMode.Normal ? Mathf.Sin(currentBounceAngle) * 0.02f * Vector3.up : Vector3.zero;
            currentBounceAngle += Time.deltaTime * bounceAngleChange * (1 + (owner.walker.speedBoost - 1) / 2);
            character.localPosition = p;
            feet.LookAt(root.TransformPoint(feet.localPosition + forward), root.up);
            head.localPosition = p + owner.walker.elevation * Vector3.up;
            head.rotation = character.rotation;
            float angle = -tilt * 80; // 80 degrees
            head.Rotate(Vector3.right, angle);
            camera.SetPositionAndRotation(head.position, head.rotation);
        }
    }
    public class ThirdPersonRotate : MazePOV
    {
        public ThirdPersonRotate(MazeOwner owner, Vector2 offset) : base(owner, "rpov") { this.offset = offset; type = CameraPosition.ThirdPersonRotate;
        }
        public override void Update(Vector3 position, Vector3 forward, float tilt = 0)
        {
            feet.localPosition = position;
            Vector3 p = owner.walker.movementMode == MovementMode.Normal ? Mathf.Sin(currentBounceAngle) * 0.02f * Vector3.up : Vector3.zero;
            currentBounceAngle += Time.deltaTime * bounceAngleChange * (1 + (owner.walker.speedBoost - 1) / 2);
            character.localPosition = p;
            feet.LookAt(root.TransformPoint(feet.localPosition + forward), root.up);
            head.localPosition = p + owner.walker.elevation * Vector3.up;
            head.rotation = character.rotation;

            Vector3 x = feet.forward * offset.x;
            Vector3 y = root.up * offset.y;
            camera.position = feet.position + x + y;
            Vector3 f = feet.position - camera.position;
            f.Normalize();
            if (f.magnitude < 0.1f) camera.rotation = character.rotation;
            else if (f.y > 0.99f)
                camera.LookAt(feet.position, root.forward);
            else
                camera.LookAt(feet.position, root.up);
        }
    }
    public class ThirdPersonStatic : MazePOV
    {
        public ThirdPersonStatic(MazeOwner owner, Vector2 offset) : base(owner, "spov") { this.offset = offset; type = CameraPosition.ThirdPersonStatic;
        }
        public override void Update(Vector3 position, Vector3 forward, float tilt = 0)
        {
            feet.localPosition = position;
            Vector3 p = owner.walker.movementMode == MovementMode.Normal ? Mathf.Sin(currentBounceAngle) * 0.02f * Vector3.up : Vector3.zero;
            currentBounceAngle += Time.deltaTime * bounceAngleChange * (1 + (owner.walker.speedBoost - 1) / 2);
            character.localPosition = p;
            feet.LookAt(root.TransformPoint(feet.localPosition + forward), root.up);
            head.localPosition = p + owner.walker.elevation * Vector3.up;
            head.rotation = character.rotation;

            Vector3 x = root.forward * offset.x;
            Vector3 y = root.up * offset.y;
            camera.position = feet.position + x + y;
            Vector3 f = feet.position - camera.position;
            f.Normalize();
            if (f.magnitude < 0.1f) camera.rotation = character.rotation;
            else if (f.y > 0.99f)
                camera.LookAt(feet.position, root.forward);
            else
                camera.LookAt(feet.position, root.up);

        }
    }

}
