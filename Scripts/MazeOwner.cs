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
    /// The active movement modes
    /// </summary>
    public enum MovementMode { Normal, Dash }
    /// <summary>
    /// The direction of the dash
    /// </summary>
    public enum DashDirection { Right, Forward, Left, Back, Look, Up, Down, }
    /// <summary>
    /// The mode for calculating the vision. The RayCast mode finds all the elements visible from within a cell. The Around mode only shows the cells and elements in them around that cell. 
    /// </summary>
    public enum VisionMode { RayCast, Around }
    /// <summary>
    /// This is used to determine how to show the character when it is behind higher-level elements. TurnOff disables the renderer of the blocking elements; Replace changes their material with a transparent one (set by <see cref="MazeOwner.fadeMaterial"/>); and Camera creates the illusion of a hole in the elements.
    /// </summary>
    public enum HoleType { TurnOff, Replace, Camera}
    /// <summary>
    /// This is the main component for defining a maze in the Editor. The game object containing this component will be considered as the root of the maze. You can have multiple mazes defined by this components (attached to different objects).
    /// </summary>
    public class MazeOwner : MonoBehaviour
    {
        /// <summary>
        /// The width of a (square) cell (in local X and Z axes).
        /// </summary>
        public float size;
        /// <summary>
        /// The floor-to-floor height of each cell.
        /// </summary>
        public float height;
        /// <summary>
        /// Whether the game is active (this will be set true after vision calculation is completed).
        /// </summary>
        public bool inGame { get; set; } = false;
        /// <summary>
        /// Whether the game is paused.
        /// </summary>
        public bool paused { get; set; } = false;
        /// <summary>
        /// If everything is ready (after vision caluculations).
        /// </summary>
        public bool ready { get; set; } = false;
        /// <summary>
        /// The index of this maze in all the mazes created.
        /// </summary>
        public int mazeIndex { get; set; }
        /// <summary>
        /// The number of cells in the X direction or local X axis of the maze.
        /// </summary>
        public int col;
        /// <summary>
        /// The number of cells in the Y direction or local Z axis of the maze.
        /// </summary>
        public int row;
        /// <summary>
        /// The number of levels or cell-count in the Z direction or local Y axis of the maze.
        /// </summary>
        public int levelCount;
        /// <summary>
        /// The material used to hide or fade elements blocking the character (see <see cref="VisionTrack"/>).
        /// </summary>
        public Material fadeMaterial;
        /// <summary>
        /// Type of fading by the <see cref="fadeMaterial"/>.
        /// </summary>
        public HoleType fadeType;
        /// <summary>
        /// The initional camera position. See <see cref="MazePOV"/> for more info.
        /// </summary>
        public CameraPosition initialViewMode = CameraPosition.FirstPerson;
        /// <summary>
        /// The distance of the camera from the character's feet (x:horizontal and y:vertical distance). See <see cref="MazePOV.offset"/>.
        /// </summary>
        public Vector2 cameraDistance = new Vector2(-5, 4);
        /// <summary>
        /// The vision mode of the maze.
        /// </summary>
        public VisionMode visionOffsetMode = VisionMode.Around;
        /// <summary>
        /// The maximum vision offset. When calculating the <see cref="VisionMap"/>, the offset of elements upto this number is calculated and stored. The remaining items are given an offset of 255 or always invisible.
        /// </summary>
        public byte maxVisionOffset = 0;
        /// <summary>
        /// The current vision offset (<= <see cref="maxVisionOffset"/>. Changing this will change the visibility status of elements based on their vision offset.
        /// </summary>
        public byte currentVisionOffset = 0;
        /// <summary>
        /// 
        /// </summary>
        public float viewAngle = 50;
        /// <summary>
        /// The prefab root is a the parent of maze elements (to which <see cref="PrefabSettings"/> components are attached. Alternatively, you can attach a <see cref="MazeElements"/> to this (Maze Owner) game object.
        /// </summary>
        public GameObject prefabRoot;
        /// <summary>
        /// The list of item types.
        /// </summary>
        public ItemID[] mazeItems;
        /// <summary>
        /// The maze that is created for this owner.
        /// </summary>
        public MazeMap maze;
        /// <summary>
        /// The walk and movement manager for this maze.
        /// </summary>
        public MazeWalker walker;
        /// <summary>
        /// The time this maze has been active (changing the active maze pauses this time).
        /// </summary>
        public float ActiveTime { get { return activeTime; } }
        float activeTime = 0;
        bool checkingVision;
        int currentViewMode = 0;
        List<MazePOV> views;
        private void Start()
        {
            Init();
        }
        /// <summary>
        /// Initializes the maze. This includes creating the <see cref="walker"/>, points of <see cref="views"/> and the <see cref="maze"/> model itself.
        /// </summary>
        public void Init()
        {
            checkingVision = false;
            UnityEngine.Random.InitState(DateTime.Now.Millisecond);
            if (maze != null) maze.DestroyEverything();
            walker = new MazeWalker();
            maze = new MazeMap( col, row, levelCount, size, height) { owner = this };
             mazeIndex = MazeManager.owners.Count;
            maze.SetRoot(this);
            views = new() { new FirstPerson(this), new ThirdPersonRotate(this, cameraDistance), new ThirdPersonStatic(this, cameraDistance) };
            walker.SetView(maze, views[currentViewMode]);
            maze.itemManager.SetItems(mazeItems);
            maze.SetPrefabs(prefabRoot);
            UserInputs.InitDefault();
            maze.Initialize();
            maze.GenerateModel(false);
            maze.prefabClone.SetActive(false);
            prefabRoot.SetActive(false);
      
            //    FirstPersonCamera fpc = new FirstPersonCamera(this);
            currentViewMode = 0;
            walker.mouseTilt = true;
            walker.speedBoost = 1;
            checkingVision = true;
            maze.vision.SetLastStates();
            maze.vision.calculating = true;
            activeTime = 0;
        
            MazeManager.owners.Add(this);
        }
        /// <summary>
        /// Starts calculating vision map. This is called in an async method in <see cref="MazeManager"/>.
        /// </summary>
        public void VisionStart()
        {
            Debug.Log(mazeIndex + " > vision start");
            maze.vision.Calculate(visionOffsetMode == VisionMode.RayCast, maxVisionOffset);
            Debug.Log(mazeIndex + " > vision end");
        }
        /// <summary>
        /// This is called when the vision calculation is completed.
        /// </summary>
        public void VisionComplete()
        {
            Debug.Log(mazeIndex + " > vision complete");
            maze.vision.HideAll();
            maze.transparency = false;
            maze.SetCurrentCell(0);
            Application.targetFrameRate = 60;
            checkingVision = false;
            ready = true;
            //            inGame = true;

        }
        private void Update()
        {
            UserInputs.Update();
        }

        private void FixedUpdate()
        {
            VisionTrack.shouldFade = false;
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
            if (UserInputs.Pressed(UserInputs.Dash)) walker.ActivateDash(DashDirection.Look);
            if (UserInputs.Pressed(UserInputs.Up)) walker.ActivateDash(DashDirection.Up);
            if (UserInputs.Pressed(UserInputs.Down)) walker.ActivateDash(DashDirection.Down);

        }
    }
}
