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
    public enum CameraPosition { Cell, Above }
    public class MazeWalker
    {
        //    public bool dashEnabled = false;
        public MazeMap maze;
        public bool canDash = true;
        public Transform camera, character;
        public Vector3 feet;
        public  float moveSpeed = 2f;
        public float dashTime = 0.3f;
        public float turnSpeed = 150;
        public  float speedBoost = 1;
        MazeDasher dasher = new MazeDasher();
        public MazeCell lastCell, currentCell;
        public float elevation = 1.5f;
        float currentBounceAngle = 0;
        float bounceAngleChange = Mathf.PI * 3.5f;
        public Color navigationColor = Color.white;
        public  bool mouseTilt = true;
        public DashMode dashMode = DashMode.Look;
        public MovementMode movementMode = MovementMode.Normal;
        public void SetCameraTransform(GameObject player)
        {
            character = player.transform;
            camera = new GameObject("camera base").transform;
            character.transform.parent = camera;
            character.transform.localPosition = -elevation * Vector3.up;
            Transform camX = Camera.main.transform;
            camX.SetParent(camera, false);
            if (maze.owner.cameraPosition == CameraPosition.Cell)
            {
                camX.transform.localPosition = Vector3.zero;
                camX.rotation = Quaternion.identity;
            }
            else
            {
                camX.transform.localPosition = maze.owner.cameraDistance * 0.7f * maze.size * (Vector3.up - Vector3.forward);
                camX.transform.LookAt(character.transform.position - camX.position);
            }
            //       Debug.Log("set cam: " + camera.position.y);
        }
        void SetPosition(bool dash)
        {
            camera.position = feet;
            camera.position += elevation * Vector3.up;
            if (!dash)
                if (maze.owner.cameraPosition == CameraPosition.Cell)
                {
                    camera.position += Mathf.Sin(currentBounceAngle) * 0.02f * Vector3.up;
                    currentBounceAngle += Time.deltaTime * bounceAngleChange * (1 + (speedBoost - 1) / 2);
                }

            //    Debug.Log("set pos: " + camera.position.y);
        }
        public void Update()
        {
            if (Mazer.paused)
            {
                if (UserInputs.Pressed(UserInputs.Pause))
                    Mazer.paused = false;
                return;
            }

            Vector3 dir, fwd3d = new Vector3(character.forward.x, 0, character.forward.z).normalized;

            bool rightTurn, foreMove;
            switch (movementMode)
            {
                case MovementMode.Normal:
                    if ((foreMove = UserInputs.Hold(UserInputs.Forward)) || UserInputs.Hold(UserInputs.Back))
                    {
                        dir = (foreMove ? 1 : -1) * fwd3d;
                        MazeCell nextCell = MoveDirection(dir, currentCell, out Vector3 next);
                        if (nextCell != currentCell)
                        {
                            lastCell = currentCell;
                            currentCell = nextCell;
                            lastCell.LeaveCell();
                            currentCell.EnterCell(true);
                        }
                        feet = next;
                        SetPosition(false);
                    }
                    if ((rightTurn = UserInputs.Hold(UserInputs.Right)) || UserInputs.Hold(UserInputs.Left))
                    {
                        int turn = rightTurn ? 1 : -1;
                        fwd3d = Turn(turn);
                    }
                    break;
                case MovementMode.Dash:
                    if (dasher.dashing == DashStatus.Dashing)
                    {
                        bool dashFinalized = dasher.Update(Time.deltaTime);
                        feet = dasher.position;
                        SetPosition(true);
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
                        }
                    }
                    break;
            }
            if (maze.owner.cameraPosition == CameraPosition.Cell && mouseTilt)
            {
                float tilt = GetTilt();
                Vector3 fwd = new Vector3(fwd3d.x, mouseTilt ? tilt : camera.forward.y, fwd3d.z).normalized;
                camera.forward = fwd;
            }
        }
        float GetTilt()
        {
            if (Mouse.current != null)
            {
                float h = Screen.height / 2;
                float y = Mouse.current.position.y.value - h;
                y /= h;
                float ya = Mathf.Abs(y);
                float r = 0;
                if (ya < 0.2f) y = 0;
                else
                {
                    y = (Mathf.Sign(y) * (ya - 0.2f)) * 1.25f;
                    ya = Mathf.Abs(y);
                    if (ya > 1) y = Mathf.Sign(y);
                }
                return Mathf.Sin(y * Mathf.PI / 3);
            }
            else return 0;
        }
        public void SetCurrentCell(int x, int y, int z)
        {
            feet = new Vector3(x * maze.size, z * maze.height, y * maze.size);
            camera.position = feet + elevation * Vector3.up;
            //     Debug.Log("set cell: " + camera.position.y);
            currentCell = maze.cells[x, y, z];
            lastCell = currentCell;
            maze.vision.levels[z].Apply(currentCell , maze.owner.currentVisionOffset);
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
                        if (maze.cells[cell.x + x, cell.y + y, cell.z].situation != PairSituation.Void)
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
                    if (maze.cells[cell.x, cell.y, level].situation != PairSituation.Void && maze.cells[cell.x, cell.y, level].situation != PairSituation.Undefined)
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
        Vector3 Turn(int dir)
        {
            if (maze.owner.cameraPosition == CameraPosition.Above)
            {
                if (maze.owner.rotateCamera)
                {
                    camera.Rotate(Vector3.up, dir * Time.deltaTime * turnSpeed, Space.World);
                    return new Vector3(camera.forward.x, 0, camera.forward.z);
                }
                else
                {
                    character.Rotate(Vector3.up, dir * Time.deltaTime * turnSpeed, Space.World);
                    return new Vector3(character.forward.x, 0, character.forward.z);
                }
            }
            else
            {
                camera.Rotate(Vector3.up, dir * Time.deltaTime * turnSpeed, Space.World);
                return new Vector3(camera.forward.x, 0, camera.forward.z);
            }
        }
        public void ActivateDash(DashMode mode)
        {
            if (movementMode != MovementMode.Dash && dasher.dashing == DashStatus.None)
            {
                movementMode = MovementMode.Dash;
                dashMode = mode;
                Vector3 v = Vector3.zero;
                bool horizontal = true;
                switch (dashMode)
                {
                    case DashMode.Look:
                        v = maze.owner.cameraPosition == CameraPosition.Above && !maze.owner.rotateCamera ? character.transform.forward : camera.transform.forward;
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

