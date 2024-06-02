using UnityEngine;
namespace Resphinx.Maze
{
    /// <summary>
    /// The base class for defining the point-of-view of the camera when navigating the maze. There are three inheriting classes for basic POV types: <see cref="FirstPerson"/>, <see cref="ThirdPersonStatic"/> and <see cref="ThirdPersonRotate"/>. You can have multiple POVs for each maze.
    /// </summary>
    public class MazePOV
    {
        /// <summary>
        /// The transforn representing the feet of the character (elevation 0). 
        /// </summary>
        public Transform feet;
        /// <summary>
        /// The transform representing the camera (or POV).
        /// </summary>
        public Transform camera;
        /// <summary>
        /// The transform representing the character.
        /// </summary>
        public Transform character;
        /// <summary>
        /// The transform representing the character's head.
        /// </summary>
        public Transform head;
        /// <summary>
        /// The owner of the maze.
        /// </summary>
        public MazeOwner owner;
        /// <summary>
        /// The POV type.
        /// </summary>
        public CameraPosition type;
        /// <summary>
        /// The transform of the maze <see cref="MazeMap.root"/>.
        /// </summary>
        protected Transform root;
        /// <summary>
        /// This angle is used to calculate a sine wave for simulating a person's head height change during walking.
        /// </summary>
        protected float currentBounceAngle = 0;
        /// <summary>
        /// The speed of changing of <see cref="currentBounceAngle"/>.
        /// </summary>
        public float bounceAngleChange = Mathf.PI * 3.5f;
        /// <summary>
        /// A vector representing the distance between the c<see cref="camera"/> and the <see cref="feet"/>. This is used to calculate the camera's position.
        /// </summary>
        public Vector2 Offset { get { return offset; } set { offset = value; } }
        protected Vector2 offset;
        /// <summary>
        /// Not used.
        /// </summary>
        protected float nearPlane, fov, scale;
        /// <summary>
        /// The base constructor of the class. 
        /// </summary>
        /// <param name="mazeOwner">The <see cref="owner"/> maze</param>
        /// <param name="name">The name of the POV</param>
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
        /// <summary>
        /// Not used.
        /// </summary>
        public void SetCircle()
        {
            MazeManager.circle.transform.localScale = scale * Vector3.one;
            MazeManager.farNear.nearClipPlane = nearPlane;
            MazeManager.farNear.fieldOfView = fov;
            MazeManager.circle.transform.localPosition = 1.1f * Vector3.forward;
            MazeManager.circle.SetActive(type != CameraPosition.FirstPerson);
        }
        /// <summary>
        /// Calculates the position of the camera based on the feet and looking angle.
        /// </summary>
        /// <param name="position">The position of the character's feet (local to the maze <see cref="MazeMap.root"/> or <see cref="MazeOwner"/>'s game object</param>
        /// <param name="forward">The local forward vector (flattened or y = 0)</param>
        /// <param name="tilt">The tilting angle of the character's head</param>
        public virtual void Update(Vector3 position, Vector3 forward, float tilt = 0) { }
    }
    /// <summary>
    /// The default first person POV class.
    /// </summary>
    public class FirstPerson : MazePOV
    {
        /// <summary>
        /// See <see cref="MazePOV"/>'s constructor. The name parameter is automatically assigned (="fpov").
        /// </summary>
        /// <param name="owner">The maze's owner</param>
        public FirstPerson(MazeOwner owner) : base(owner, "fpov")
        {
            type = CameraPosition.FirstPerson;
        }
        /// <summary>
        /// See <see cref="MazePOV.Update(Vector3, Vector3, float)"/>
        /// </summary>
        /// <param name="position">See above</param>
        /// <param name="forward">See above</param>
        /// <param name="tilt">See above</param>
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
    /// <summary>
    /// The default third person POV class, where the camera follows the oriatation of the charater.
    /// </summary>
    public class ThirdPersonRotate : MazePOV
    {
        /// <summary>
        /// See <see cref="MazePOV"/>'s constructor. The name parameter is automatically assigned (="rpov").
        /// </summary>
        /// <param name="owner">The maze's owner</param>
        public ThirdPersonRotate(MazeOwner owner, Vector2 offset) : base(owner, "rpov")
        {
            this.offset = offset; type = CameraPosition.ThirdPersonRotate;
            nearPlane = owner.cameraDistance.magnitude - owner.height;
            float tan = owner.size / 2 / owner.cameraDistance.magnitude;
            fov = Mathf.Atan(tan);
            scale = tan * Camera.main.nearClipPlane * 1.1f * 2;
        }
        /// <summary>
        /// See <see cref="MazePOV.Update(Vector3, Vector3, float)"/>
        /// </summary>
        /// <param name="position">See above</param>
        /// <param name="forward">See above</param>
        /// <param name="tilt">See above</param>
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
    /// <summary>
    /// The default third person POV class, where the camera rotation remains fixed even if the character rotates.
    /// </summary>
    public class ThirdPersonStatic : MazePOV
    {
        /// <summary>
        /// See <see cref="MazePOV"/>'s constructor. The name parameter is automatically assigned (="spov").
        /// </summary>
        /// <param name="owner">The maze's owner</param>
        public ThirdPersonStatic(MazeOwner owner, Vector2 offset) : base(owner, "spov")
        {
            this.offset = offset; type = CameraPosition.ThirdPersonStatic;
            nearPlane = owner.cameraDistance.magnitude - owner.height;
            float tan = owner.size / 2 / owner.cameraDistance.magnitude;
            fov = Mathf.Atan(tan);
            scale = tan * Camera.main.nearClipPlane * 1.1f * 2;
        }
        /// <summary>
        /// See <see cref="MazePOV.Update(Vector3, Vector3, float)"/>
        /// </summary>
        /// <param name="position">See above</param>
        /// <param name="forward">See above</param>
        /// <param name="tilt">See above</param>
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
