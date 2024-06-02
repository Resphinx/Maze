using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Resphinx.Maze
{
    /// <summary>
    /// The options for the dash's status.
    /// </summary>
    public enum DashStatus { None, Dashing, Reached }
    /// <summary>
    /// The three basic camera modes. See <see cref="MazePOV"/>.
    /// </summary>
    public enum CameraPosition { FirstPerson, ThirdPersonRotate, ThirdPersonStatic }
    /// <summary>
    /// This class controls the movement of characters inside a maze
    /// </summary>
    public class MazeWalker
    {
        /// <summary>
        /// The active POV
        /// </summary>
        public MazePOV view;
        /// <summary>
        /// The maze owning this walker
        /// </summary>
        public MazeMap maze;
        /// <summary>
        /// Whether dashing is possible (the default is true, but you can change it depending on the game context).
        /// </summary>
        public bool canDash = true;
        /// <summary>
        /// The position of the character on the cell's base level.
        /// </summary>
        public Vector3 feet;
        /// <summary>
        /// The movement speed (per seconds).
        /// </summary>
        public float moveSpeed = 2f;
        /// <summary>
        /// The duration of the dash. This only affects dashes initilized after changing it (See <see cref="MazeDasher.Init(Vector3, MazeCell, float)"/>
        /// </summary>
        public float dashTime = 0.3f;
        /// <summary>
        /// The turning speed in degree/s.
        /// </summary>
        public float turnSpeed = 150;
        /// <summary>
        /// Speed boost (multiplied by <see cref="moveSpeed"/>
        /// </summary>
        public float speedBoost = 1;
        /// <summary>
        /// The dashing manager of this maze. It is only constructed once. For each dash use <see cref="MazeDasher.Init(Vector3, MazeCell, float)"/>.
        /// </summary>
        MazeDasher dasher = new MazeDasher();
        /// <summary>
        /// The previous active cell 
        /// </summary>
        public MazeCell lastCell;
        /// <summary>
        /// The current active cell (where character stands, or should if the maze was active)
        /// </summary>
        public MazeCell currentCell;
        /// <summary>
        /// The camera's elevation (only used for first-person POVs).
        /// </summary>
        public float elevation = 1.2f;
        /// <summary>
        /// Whether the position of the mouse (its Y) affects the tilting of the camera.
        /// </summary>
        public bool mouseTilt = true;
        /// <summary>
        /// The direction of the next dash.
        /// </summary>
        public DashDirection dashDirection = Maze.DashDirection.Look;
        /// <summary>
        /// The active movement mode
        /// </summary>
        public MovementMode movementMode = MovementMode.Normal;
        /// <summary>
        /// A child object used to help calculating rotation of objects (it's rotation is subject to that of <see cref="rotateParent"/>). See <see cref="Turn(Vector3, float)"/>.
        /// </summary>
        public static Transform rotateChild;
        /// <summary>
        /// The parent object of <see cref="rotateChild"/>. See <see cref="Turn(Vector3, float)"/>.
        /// </summary>
        public static Transform rotateParent;
        Vector3 lastForward = Vector3.forward;
        float lastTilt = 0;
        /// <summary>
        /// This initializes the rotation of <see cref="rotateChild"/> and <see cref="rotateParent"/>.
        /// </summary>
        public static void InitializeRotation()
        {
            rotateParent = new GameObject("rotation pivot").transform;
            rotateChild = new GameObject("rotated direction").transform;
            rotateChild.transform.parent = rotateParent;
            rotateChild.localPosition = Vector3.forward;
        }
        /// <summary>
        /// Initial setting the maze map and POV in <see cref="MazeOwner"/>.
        /// </summary>
        /// <param name="map">The parent maze</param>
        /// <param name="pov">The initial POV</param>
        public void SetView(MazeMap map, MazePOV pov)
        {
            view = pov;
            maze = map;
        }
        /// <summary>
        /// Changes the active POV.
        /// </summary>
        /// <param name="pov"></param>
        public void ChangeView(MazePOV pov)
        {
            view = pov;
            view.SetCircle();
            view.Update(feet, lastForward, lastTilt);
        }
        /// <summary>
        /// Rotates a point around the up vector.
        /// </summary>
        /// <param name="last">The point's last coordinates</param>
        /// <param name="delta">The angle in degrees</param>
        /// <returns>The new position after rotation</returns>
        public static Vector3 Turn(Vector3 last, float delta)
        {
            rotateChild.position = last;
            rotateParent.Rotate(Vector3.up, delta, Space.World);
            return rotateChild.position;
        }
        /// <summary>
        /// Updates the position and rotation of the character, as well as of the camera.
        /// </summary>
        /// <param name="moveDirection">Moving direction (-1:back, 0:still, and +1: forward)</param>
        /// <param name="turnDirection">Turnin direction (-1:left, 0:none, and +1:right)</param>
        /// <param name="mouseY">The relative Y position of the mouse to the screen size and center (see <see cref="GetTilt(float)"/>)</param>
        public void Update(int moveDirection, int turnDirection, float mouseY = 0)
        {
            Vector3 moveVector;

            float lt = lastTilt;
            lastTilt = GetTilt(mouseY);
            bool changed = lt != lastTilt;

            switch (movementMode)
            {
                case MovementMode.Normal:
                    if (turnDirection != 0)
                    {
                        lastForward = Turn(lastForward, turnDirection * Time.deltaTime * turnSpeed);
                        changed = true;
                    }
                    if (moveDirection != 0)
                    {
                        moveVector = moveDirection * lastForward;
                        MazeCell nextCell = MoveDirection(moveVector, currentCell, out Vector3 next);
                        if (nextCell != currentCell)
                        {
                            lastCell = currentCell;
                            currentCell = nextCell;
                            lastCell.LeaveCell();
                            currentCell.EnterCell(true);
                        }
                        feet = currentCell.AddLocalElevation(next);
                        changed = true;
                    }
                    break;
                case MovementMode.Dash:
                    if (dasher.dashing == DashStatus.Dashing)
                    {
                        bool dashFinalized = dasher.TryReach(Time.deltaTime);
                        feet = dasher.position;
                        if (dashFinalized)
                        {
                            dasher.dashing = DashStatus.None;
                            movementMode = MovementMode.Normal;
                            if (dasher.destination.z != currentCell.z)
                            {
                                maze.vision.levels[lastCell.z].Show(lastCell, false);
                                maze.vision.levels[dasher.destination.z].Show(dasher.destination, true);
                            }
                            lastCell = currentCell;
                            lastCell.LeaveCell();
                            currentCell = dasher.destination;
                            currentCell.EnterCell(false);
                            feet = currentCell.AddLocalElevation(currentCell.position);
                        }
                        changed = true;
                    }
                    break;
            }
            if (changed) view.Update(feet, lastForward, lastTilt);

        }
        /// <summary>
        /// Camera's tilting angle based on the mouse position.
        /// </summary>
        /// <param name="y">Mouse's Y coordinates, that is calculate by its absolute Y: (Y - Screen.height/2) / (Screen.height/2) </param>
        /// <returns>Returns a value between -1 and 1. If the mouse cursor is within the 20% of screen height around its center, it returns 0, otherwise it returns the relative amount by which the cursor has approached the upper (+1) or lower (-1) edges of the screen.</returns>
        float GetTilt(float y)
        {
            if (y == 0) return 0;
            float ya = Mathf.Abs(y);
            if (ya < 0.2f) y = 0;
            else
            {
                y = (Mathf.Sign(y) * (ya - 0.2f)) * 1.25f;
                ya = Mathf.Abs(y);
                if (ya > 1) y = Mathf.Sign(y);
            }
            return Mathf.Sin(y * Mathf.PI / 3);
        }
        /// <summary>
        /// Sets the active cell on the maze and position the character, updates the visisbility and POV.
        /// </summary>
        /// <param name="x">X of the cell</param>
        /// <param name="y">Y of the cell</param>
        /// <param name="z">Z of the cell</param>
        public void SetCurrentCell(int x, int y, int z)
        {
            currentCell = maze.cells[x, y, z];
            lastCell = currentCell;
            feet = currentCell.AddLocalElevation(currentCell.position);
            lastForward = Vector3.forward;
            lastTilt = 0;
            view.Update(feet, lastForward, lastTilt);
            //     Debug.Log("set cell: " + camera.position.y);
            maze.vision.levels[z].Apply(currentCell, maze.owner.currentVisionOffset);
        }
        /// <summary>
        /// Checks horizontal dashing to an adjacent cell
        /// </summary>
        /// <param name="u">The character's flat forward vector (y = 0)</param>
        /// <param name="cell">The current cell</param>
        /// <param name="md">The destination cell (if possible) in the vector's direction or null</param>
        /// <returns>True if dashing is possible</returns>
        bool DashDirection(Vector3 u, MazeCell cell, out MazeCell md)
        {
            Vector2 au = new Vector2(Mathf.Abs(u.x), Mathf.Abs(u.z));
            int a = -1, b = 0;
            int x = 0, y = 0;
            if (au.x < 0.35f)
            {
                a = 1;
                b = u.z > 0 ? 1 : 0;
                y = u.z > 0 ? 1 : -1;
            }
            else if (au.y < 0.35f)
            {
                a = 0;
                b = u.x > 0 ? 1 : 0;
                x = u.x > 0 ? 1 : -1;
            }
            md = null;
            if (a >= 0)
                if (maze.InRange(cell.x + x, cell.y + y))
                    if (maze.cells[cell.x + x, cell.y + y, cell.z] != null)
                        if (maze.cells[cell.x + x, cell.y + y, cell.z].situation != BundleSituation.Void)
                            if (cell.connection[a, b] != Connection.Unpassable)
                                md = maze.cells[cell.x + x, cell.y + y, cell.z];

            return md != null;
        }
        /// <summary>
        /// Checks vertical dashing to a new cell
        /// </summary>
        /// <param name="cell">The current cell</param>
        /// <param name="md">The destination cell</param>
        /// <returns>True if dashing is possible</returns>
        bool DashDirection(MazeCell cell, out MazeCell md)
        {
            int level = cell.z + (dashDirection == Maze.DashDirection.Up ? 1 : -1);
            md = null;
            if (level >= 0 && level < maze.levels)
                if (maze.cells[cell.x, cell.y, level] != null)
                    if (maze.cells[cell.x, cell.y, level].situation != BundleSituation.Void && maze.cells[cell.x, cell.y, level].situation != BundleSituation.Hanging)
                        md = maze.cells[cell.x, cell.y, level];
            return md != null;
        }
        /// <summary>
        /// Finds the next position of the character when walking. This method calls <see cref="MazeCell.NextPosition(Vector3, Vector3, float, float)"/>.
        /// </summary>
        /// <param name="dir">The walking direction</param>
        /// <param name="cell">The current cell</param>
        /// <param name="next">The next position</param>
        /// <returns></returns>
        MazeCell MoveDirection(Vector3 dir, MazeCell cell, out Vector3 next)
        {
            MazeCell r;
            float dt = Time.deltaTime;
            float speed = speedBoost * moveSpeed;
            next = cell.NextPosition(feet, dir, speed, dt);
            if ((r = cell.CanGo(next)) != null)
                return r;
            next = feet;
            return cell;
        }
        /// <summary>
        /// Calculates and initializes dashing (this method calls <see cref="DashDirection(MazeCell, out MazeCell)"/> or <see cref="DashDirection(Vector3, MazeCell, out MazeCell)"/> depending on the <c>dir</c>.
        /// </summary>
        /// <param name="dir">The direction of the dash</param>
        public void ActivateDash(DashDirection dir)
        {
            if (movementMode != MovementMode.Dash && dasher.dashing == DashStatus.None && canDash)
            {
                movementMode = MovementMode.Dash;
                dashDirection = dir;
                Vector3 v = Vector3.zero;
                bool horizontal = true;
                switch (dashDirection)
                {
                    case Maze.DashDirection.Look:
                        v = lastForward;
                        break;
                    case Maze.DashDirection.Right: v = Vector3.forward; break;
                    case Maze.DashDirection.Forward: v = Vector3.right; break;
                    case Maze.DashDirection.Left: v = Vector3.left; break;
                    case Maze.DashDirection.Back: v = Vector3.back; break;
                    case Maze.DashDirection.Down: horizontal = false; break;
                    case Maze.DashDirection.Up: horizontal = false; break;
                }
                movementMode = MovementMode.Normal;
                if (horizontal)
                {
                    if (DashDirection(v, currentCell, out MazeCell m))
                    {
                        dasher.Init(feet, m, dashTime);
                        maze.vision.levels[m.z].Show(m, true);
                        movementMode = MovementMode.Dash;
                    }
                }
                else if (DashDirection(currentCell, out MazeCell m))
                {
                    dasher.Init(feet, m, dashTime);
                    maze.vision.levels[m.z].Show(m, true);
                    movementMode = MovementMode.Dash;
                }

            }

        }
    }
}

