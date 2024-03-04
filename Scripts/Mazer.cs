using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Resphinx.Maze
{
    public enum MovementMode { Normal, Dash }
    public enum DashMode { Forward, Up, Down }
    public class Mazer : MonoBehaviour
    {
        public static Mazer Instance;
        public float size, height;
        public static bool inGame = false, paused = false;
       
        public int col;
        public int row;
        public int levelCount;
        public CameraPosition cameraPosition = CameraPosition.Cell;
        public bool rotateCamera = false;
        public float relativeDistance = 2;

        public GameObject prefabRoot, character;
        public ItemID[] mazeItems;
        public MazeMap maze;
        public MazeWalker walker;

        public static MovementMode movementMode = MovementMode.Normal;
        public static DashMode dashMode;

        public static float ActiveTime = 0, MaxTime = 900;
        bool checkingVision;
        public static UserInputs inputs;
        private void Start()
        {
            Instance = this;
            Init();
        }
        public void Init()
        {

            checkingVision = false;
            MazeWalker.mouseTilt = true;
            MazeWalker.speedBoost = 1;
            UnityEngine.Random.InitState(DateTime.Now.Millisecond);
            if (maze != null) maze.DestroyEverything();
            walker = null;
            MazeMap.maze = maze = new MazeMap(row, col, levelCount, size, height);

            MazeMap.maze.root = new GameObject("Maze Root");
            maze.itemManager.SetItems(mazeItems);
            maze.SetPrefabs(prefabRoot);
            UserInputs.InitDefault();
            maze.Initialize();
            maze.GenerateModel(false);
            MazeMap.maze.prefabClone.SetActive(false);
            prefabRoot.SetActive(false);

            walker = new MazeWalker() { maze = maze};
            walker.SetCameraTransform(character);
            checkingVision = true;


            ActiveTime = 0;
            VisionMap.calculating = true;

            Application.targetFrameRate = 10;
            maze.SetVision();
        }
        public void VisionComplete()
        {
            maze.vision.HideAll();
            maze.transparency = false;
            maze.SetCurrentCell();
            Application.targetFrameRate = 60;
            checkingVision = false;
            inGame = true;

        }
        private void Update()
        {
            UserInputs.Update();
        }
        private void FixedUpdate()
        {
            if (checkingVision)
            {
                if (VisionMap.calculating)
                    return;
                else
                    VisionComplete();
            }
            if (!inGame || paused) return;
            ActiveTime += Time.deltaTime;
            
            walker.Update();

            if (UserInputs.Pressed(UserInputs.Dash))
                if (movementMode != MovementMode.Dash)
                {
                    movementMode = MovementMode.Dash;
                    dashMode = DashMode.Forward;
                }
            if (UserInputs.Pressed(UserInputs.Up))
                if (movementMode != MovementMode.Dash)
                {
                    movementMode = MovementMode.Dash;
                    dashMode = DashMode.Up;
                }
            if (UserInputs.Pressed(UserInputs.Down))
                if (movementMode != MovementMode.Dash)
                {
                    movementMode = MovementMode.Dash;
                    dashMode = DashMode.Down;
                }

        }
    }
}
