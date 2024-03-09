using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Resphinx.Maze
{
    public enum MazeStatus { Normal, Dash, Pause, UI }
    public enum UIStatus { Home, Play }
    public enum SeekStatus { None, Heart, Tear }
    public enum DashStatus { None, Dashing, Reached }
    public enum CameraPosition { FirstPerson, ThirdPersonRotate, ThirdPersonStatic }
    public class MazeWalker
    {
        public MazePOV view;
        public MazeMap maze;
        public bool canDash = true;
        public Vector3 feet;
        public float moveSpeed = 2f;
        public float dashTime = 0.3f;
        public float turnSpeed = 150;
        public float speedBoost = 1;
        MazeDasher dasher = new MazeDasher();
        public MazeCell lastCell, currentCell;
        public float elevation = 1.5f;
        public Color navigationColor = Color.white;
        public bool mouseTilt = true;
        public DashMode dashMode = DashMode.Look;
        public MovementMode movementMode = MovementMode.Normal;

        public static Transform rotateChild, rotateParent;
        Vector3 lastForward = Vector3.forward;
        float lastTilt = 0;

        public static void InitializeRotation()
        {
            rotateParent = new GameObject("rotation pivot").transform;
            rotateChild = new GameObject("rotated direction").transform;
            rotateChild.transform.parent = rotateParent;
            rotateChild.localPosition = Vector3.forward;
        }
        public void SetView(MazeMap map, MazePOV pov)
        {
            view = pov;
            maze = map;
        }
        public void ChangeView(MazePOV pov)
        {
            view = pov;
            view.Update(feet, lastForward, lastTilt);
        }
        public static Vector3 Turn(Vector3 last, float delta)
        {
            rotateChild.position = last;
            rotateParent.Rotate(Vector3.up, delta, Space.World);
            return rotateChild.position;
        }
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
                            feet=currentCell.AddLocalElevation(currentCell.position ) ;
                        }
                        changed = true;
                    }
                    break;
            }
            if (changed) view.Update(feet, lastForward, lastTilt);

        }
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
        bool DashDirection(MazeCell cell, out MazeCell md)
        {
            int level = cell.z + (dashMode == DashMode.Up ? 1 : -1);
            md = null;
            if (level >= 0 && level < maze.levels)
                if (maze.cells[cell.x, cell.y, level] != null)
                    if (maze.cells[cell.x, cell.y, level].situation != BundleSituation.Void && maze.cells[cell.x, cell.y, level].situation != BundleSituation.Hanging)
                        md = maze.cells[cell.x, cell.y, level];
            return md != null;
        }
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

        public void ActivateDash(DashMode mode)
        {
            if (movementMode != MovementMode.Dash && dasher.dashing == DashStatus.None && canDash)
            {
                movementMode = MovementMode.Dash;
                dashMode = mode;
                Vector3 v = Vector3.zero;
                bool horizontal = true;
                switch (dashMode)
                {
                    case DashMode.Look:
                        v = lastForward;
                        break;
                    case DashMode.Right: v = Vector3.forward; break;
                    case DashMode.Forward: v = Vector3.right; break;
                    case DashMode.Left: v = Vector3.left; break;
                    case DashMode.Back: v = Vector3.back; break;
                    case DashMode.Down: horizontal = false; break;
                    case DashMode.Up: horizontal = false; break;
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

