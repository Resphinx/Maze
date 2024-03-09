using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Resphinx.Maze
{
    public enum MovementMode { Normal, Dash }
    public enum DashMode { Right, Forward, Left, Back, Look, Up, Down, }
    public enum VisionMode { RayCast, Around }
    public class MazeOwner : MonoBehaviour
    {
        public float size, height;
        public bool inGame = false, paused = false;

        public int mazeIndex { get; set; }
        public int col;
        public int row;
        public int levelCount;
        public CameraPosition initialViewMode = CameraPosition.FirstPerson;
        public Vector2 cameraDistance = new Vector2(-5, 4);
        public VisionMode visionOffsetMode = VisionMode.Around;
        public byte maxVisionOffset = 0;
        public byte currentVisionOffset = 0;
        public int maxRadius = 5;
        public GameObject prefabRoot;
        public ItemID[] mazeItems;
        public MazeMap maze;
        public MazeWalker walker;


        public float activeTime = 0;
        bool checkingVision;
        public static UserInputs inputs;
        int currentViewMode = 0;
        List<MazePOV> views;
        private void Start()
        {
            Init();
        }
        public void Init()
        {

            checkingVision = false;
            UnityEngine.Random.InitState(DateTime.Now.Millisecond);
            if (maze != null) maze.DestroyEverything();
            walker = null;
            maze = new MazeMap(row, col, levelCount, size, height) { owner = this };
            mazeIndex = MazeManager.owners.Count;
            maze.SetRoot(this);
            maze.itemManager.SetItems(mazeItems);
            maze.SetPrefabs(prefabRoot);
            UserInputs.InitDefault();
            maze.Initialize();
            maze.GenerateModel(false);
            maze.prefabClone.SetActive(false);
            prefabRoot.SetActive(false);

            views = new() { new FirstPerson(this), new ThirdPersonRotate(this, cameraDistance), new ThirdPersonStatic(this, cameraDistance) };
            //    FirstPersonCamera fpc = new FirstPersonCamera(this);
            currentViewMode = 0;
            walker = new MazeWalker();
            walker.SetView(maze, views[currentViewMode]);
            walker.mouseTilt = true;
            walker.speedBoost = 1;
            checkingVision = true;
            maze.vision.SetLastStates();
            maze.vision.calculating = true;
            activeTime = 0;
            Debug.Log("mazeIndex " + mazeIndex);

            MazeManager.owners.Add(this);
        }
        public void VisionStart()
        {
            Debug.Log(mazeIndex + " > vision start");
            maze.vision.Vision(visionOffsetMode == VisionMode.RayCast, maxVisionOffset);
            Debug.Log(mazeIndex + " > vision end");
        }
        public void VisionComplete()
        {
            Debug.Log(mazeIndex + " > vision complete");
            maze.vision.HideAll();
            maze.transparency = false;
            maze.SetCurrentCell(0);
            Application.targetFrameRate = 60;
            checkingVision = false;
            //            inGame = true;

        }
        private void Update()
        {
            UserInputs.Update();
        }
        private void FixedUpdate()
        {
            if (checkingVision)
            {
                if (maze.vision.calculating)
                    return;
                else
                    VisionComplete();
            }
            if (!inGame || paused) return;
            activeTime += Time.deltaTime;
            if (UserInputs.Pressed(UserInputs.View))
            {
                currentViewMode = (currentViewMode + 1) % views.Count;
                walker.ChangeView(views[currentViewMode]);
            }

            float h = Screen.height / 2;
            float y = Mouse.current!=null? Mouse.current.position.y.value - h:0;
            y /= h;
            int moveDirection = 0, turnDirection = 0;
            if (UserInputs.Hold(UserInputs.Forward)) moveDirection = 1;
            else if (UserInputs.Hold(UserInputs.Back)) moveDirection = -1;
            if (UserInputs.Hold(UserInputs.Right)) turnDirection = 1;
            else if (UserInputs.Hold(UserInputs.Left)) turnDirection = -1;
            walker.Update(moveDirection, turnDirection, y);

              if (MazeManager.currentMaze == mazeIndex)
                Camera.main.transform.SetPositionAndRotation(walker.view.camera.position, walker.view.camera.rotation);
            if (UserInputs.Pressed(UserInputs.Dash)) walker.ActivateDash(DashMode.Look);
            if (UserInputs.Pressed(UserInputs.Up)) walker.ActivateDash(DashMode.Up);
            if (UserInputs.Pressed(UserInputs.Down)) walker.ActivateDash(DashMode.Down);

        }
    }
}
