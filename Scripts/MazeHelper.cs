using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    /// <summary>
    /// This class manages a character's dashing between maze cells.
    /// </summary>
    public class MazeDasher
    {
        /// <summary>
        /// The local vector and origin of the dash. The vector always starts from the origin and points to the center of the destination cell.
        /// </summary>
        Vector3 vector, origin;
        /// <summary>
        /// The current position of the character during the dash.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The dashing speed (unit/s).
        /// </summary>
        public float speed;
        /// <summary>
        ///  The current progress of the dash (0 to 1).
        /// </summary>
        public float progress;
        /// <summary>
        /// The dashing status (See <see cref="DashStatus"/>).
        /// </summary>
        public DashStatus dashing = DashStatus.None;
        /// <summary>
        /// The destination cell.
        /// </summary>
        public MazeCell destination;
        /// <summary>
        /// Initializes the dashing. Please note that you should create a dasher somewhere in your code before using this method (see also <seealso cref="Init(Vector3, Vector3, float)"/>. 
        /// </summary>
        /// <param name="from">The origin of the dash</param>
        /// <param name="dest">The destination cell.</param>
        /// <param name="duration">The duration of the dash</param>
        public void Init(Vector3 from, MazeCell dest, float duration)
        {
            origin = from;
            position = from;
            vector = dest.position - from;
            speed = 1 / duration;
            progress = 0;
            dashing = DashStatus.Dashing;
            destination = dest;
        }
        /// <summary>
        /// Initializes the dashing. Please note that you should create a dasher somewhere in your code before using this method (see also <seealso cref="Init(Vector3, MazeCell, float)"/>. 
        /// </summary>
        /// <param name="from">The origin of the dash</param>
        /// <param name="to">The destination of the dash.</param>
        /// <param name="duration">The duration of the dash</param>
        public void Init(Vector3 from, Vector3 to, float duration)
        {
            origin = from;
            position = from;
            vector = to - from;
            speed = 1 / duration;
            progress = 0;
            dashing = DashStatus.Dashing;
            //        destination = d;
        }
        /// <summary>
        /// Checks if the character reaches to the destination within a delta time.
        /// </summary>
        /// <param name="dt">Delta time</param>
        /// <returns></returns>
        public bool TryReach(float dt)
        {
            progress += dt * speed;
            position = origin + progress * vector;
            if (progress >= 1)
            {
                position = origin + vector;
                dashing = DashStatus.None;
                return true;
            }
            else return false;
        }
    }
    /// <summary>
    /// This class manages the items of a similar type or <see cref="id"/>.
    /// </summary>
    [Serializable]
    public class ItemID
    {
        /// <summary>
        /// This custom string is the type of the item. Items with the same ID do not appear in the same cell. See also <seealso cref="PrefabSettings.id"/>.
        /// </summary>
        public string id = "";
        /// <summary>
        /// The chance of an item of this type appearing in a cell (0 .. 1)
        /// </summary>
        public float chance = 0.2f;
        /// <summary>
        /// The prefab manager of items (that includes their game objects).
        /// </summary>
        public List<PrefabManager> items = new List<PrefabManager>();
        /// <summary>
        /// Adds an item of this ID to the inventory of <see cref="items"/>.
        /// </summary>
        /// <param name="maze">The maze</param>
        /// <param name="handle">The game object representing the item</param>
        /// <returns></returns>
        public GameObject AddItem(MazeMap maze, GameObject handle)
        {
            PrefabManager.onCreation = true;
            PrefabSettings mc = handle.GetComponent<PrefabSettings>();
            PrefabManager pm;
            if (mc.rotatable)
                items.Add(pm = PrefabManager.CreateOrdered(maze, id, handle));
            else
                items.Add(pm = PrefabManager.CreateMono(maze, id, handle));
            PrefabManager.onCreation = false;
            return pm.root;
        }
    }
    /// <summary>
    /// This class contains all item settings of a maze.
    /// </summary>
    public class ItemManager
    {
        /// <summary>
        /// The items types (See <see cref="ItemID"/>).
        /// </summary>
        public ItemID[] ids;
        /// <summary>
        /// A dictioray that corresponds an ID with its index in <see cref="ids"/>.
        /// </summary>
        public Dictionary<string, int> itemDictionary = new Dictionary<string, int>();
        /// <summary>
        /// Add an item to the list of items (creates a new item ID if a corresponding id is not found). This is used to categorize all items in a maze.
        /// </summary>
        /// <param name="maze">The parent maze</param>
        /// <param name="g">The iems' game object</param>
        public void AddItem(MazeMap maze, GameObject g)
        {
            string s = g.GetComponent<PrefabSettings>().id;
            ItemID iid = null;
            foreach (ItemID id in ids)
                if (id.id == s.ToLower())
                {
                    iid = id;
                    break;
                }
            if (iid != null)
                iid.AddItem(maze, g);
        }
        /// <summary>
        /// Instantiate a game object of a certain id.
        /// </summary>
        /// <param name="cell">The containing cell</param>
        /// <param name="index">The index of the id</param>
        /// <param name="cr">The characteristics of the result, see <see cref="CloneResult"/> </param>
        /// <returns>Returns the instantiated item if lucky (see <see cref="ItemID.chance"/>) or null.</returns>
        public GameObject GetItem(MazeCell cell, int index, CloneResult cr)
        {
            GameObject g = null;
            PrefabManager pm = PrefabManager.GetPool(ids[index].items, cr, false);
            if (pm != null)
            {
                // finding possible sides
                int side = -1;
                if (pm.settings.adjacentTo == ItemWallRelation.Both) side = UnityEngine.Random.Range(0, 4);
                else
                {
                    List<int> possible = new List<int>();
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int ij = MazeCell.Side(i);
                        if ((cell.neighbors[ij.x, ij.y] == null) == (pm.settings.adjacentTo == ItemWallRelation.OnlyClosed))
                            possible.Add(i);
                    }
                    side = possible.Count > 0 ? possible[UnityEngine.Random.Range(0, possible.Count)] : -1;
                }

                if (pm.settings.rotatable)
                {
                    if (side >= 0)
                    {
                        g = PrefabManager.Clone(pm.side[side], cell.floor.transform);
                        g.transform.localPosition = Vector3.zero;

                    }
                }
                else if (side >= 0)
                {
                    bool sideMatched = pm.settings.side switch
                    {
                        Sides.X_Positive => side == 0,
                        Sides.Z_Positive => side == 1,
                        Sides.X_Negative => side == 2,
                        Sides.Z_Negative => side == 3,
                        Sides.X => side % 2 == 0,
                        Sides.Z => side % 2 == 1,
                        _ => true
                    };
                    if (sideMatched)
                    {
                        g = PrefabManager.Clone(pm.root, cell.floor.transform);
                        g.transform.localPosition = Vector3.zero;
                    }
                }
                cr.sideIndex = side;
            }

            return g;
        }
        /// <summary>
        /// Set all items defined in the maze owner (see <see cref="MazeOwner.mazeItems"/>.
        /// </summary>
        /// <param name="mazeItems">The list of items (the same array as <see cref="MazeOwner.mazeItems"/>)</param>
        internal void SetItems(ItemID[] mazeItems)
        {
            ids = mazeItems;
            for (int i = 0; i < ids.Length; i++)
                itemDictionary.Add(ids[i].id, i);
        }
    }
    /// <summary>
    /// A bundle of cells that are created to gether and should not have any wall between them. These are most useful for defining ramps or elements that should be only put in a maze in an area not one cell.
    /// A bundle is a 2D surface (a ramp, for example).
    /// </summary>
    public class CellBundle
    {
        /// <summary>
        /// The bundles index in <see cref="MazeMap.bundles"/>.
        /// </summary>
        public int index;
        /// <summary>
        /// The length of the bundle (relative to <see cref="side"/>)
        /// </summary>
        public int length;
        /// <summary>
        /// The width of the bundle (relative to <see cref="side"/>)
        /// </summary>
        public int width;
        /// <summary>
        /// The height of the bundle.
        /// </summary>
        public int height;
        /// <summary>
        /// The handle cell of the bundle. This is used to outline the bundle based on its dimensions and <see cref="side"/>. Please note that the height is on the local Y axis but it corresponds with the z component of the bundle's and the cell's integer coordinates.
        /// </summary>
        public MazeCell handle;
        /// <summary>
        /// All the cells in the bundle, including the invalid cells (under and above the ramp).
        /// </summary>
        public MazeCell[,,] cells;
        /// <summary>
        /// The valid and walkable cells in the bundle.
        /// </summary>
        public MazeCell[,] path;
        /// <summary>
        /// The maze containing the bundle.
        /// </summary>
        public MazeMap maze;
        /// <summary>
        /// The first row of the bundle (containing the <see cref="handle"/>, in the <see cref="length"/> and <see cref="side"/> direction). In a ramp, this row and <see cref="latter"/> are the entrances for the bundle.
        /// </summary>
        public byte[,] first;
        /// <summary>
        /// The last row of the bundle (in the <see cref="length"/> and <see cref="side"/> direction).In a ramp, this row and <see cref="first"/> are the entrances for the bundle.
        /// </summary>
        public byte[,] latter;
        /// <summary>
        /// If the <see cref="length"/> is on the local X axis.
        /// </summary>
        public bool onX;
        /// <summary>
        /// If the bundle ramp is ascending (relative to <see cref="handle"/> (=1) or descending (=-1). This value is not boolean because it is directly used as a factor to calculate placement and movement vectors.
        /// </summary>
        public int ascending;
        /// <summary>
        /// The side direction of the bundle's length (see <see cref="MazeCell.Delta(int)"/> and <see cref="MazeCell.Side(int)"/>).
        /// </summary>
           public int side;
        /// <summary>
        /// The levels covered by this bundle.
        /// </summary>
        public int[] levels;
        /// <summary>
        /// The reference point is a local position that the feet position is calculated based on. 
        /// </summary>
        public Vector3 reference;
        /// <summary>
        /// The is the walking vector (relative to the <see cref="handle"/>) on the bundle.
        /// </summary>
       public Vector3 walk;
        /// <summary>
        /// This is used in <see cref="MazeMap"/> to create bundles. 
        /// </summary>
        /// <param name="maze">The containing maze</param>
        /// <param name="handle">The handle cell</param>
        /// <param name="side">The extension side</param>
        /// <param name="x">The x coordinate of the handle</param>
        /// <param name="y">The y coordinate of the handle</param>
        /// <param name="z">The height of the maze. It can be negative that will make <see cref="ascending"/> negative. z=0 means it's a flat bundle.</param>
        public CellBundle(MazeMap maze, MazeCell handle, int side, int x, int y, int z)
        {
            length = x;
            width = y;
            height = Mathf.Abs(z) + 1;
            ascending = z >= 0 ? 1 : -1;
            cells = new MazeCell[length, width, height];
            path = new MazeCell[length, width];
            levels = new int[] { handle.z, handle.z + z };
            this.side = side;
            onX = side % 2 == 0;
            this.handle = handle;
            this.maze = maze;
            index = maze.bundles.Count;
            maze.bundles.Add(this);
            float h = z * maze.height;
            float s = maze.size;
            reference = side switch
            {
                0 => handle.position - s * Vector3.right / 2,
                1 => handle.position - s * Vector3.forward / 2,
                2 => handle.position + s * Vector3.right / 2,
                _ => handle.position + s * Vector3.forward / 2,
            };
            walk = side switch
            {
                0 => new Vector3(length * s, h, 0),
                1 => new Vector3(0, h, length * s),
                2 => new Vector3(-length * s, h, 0),
                _ => new Vector3(0, h, -length * s),
            };

        }
        /// <summary>
        /// Sets a cell on the bundle. The bundle is created first without its cells defined. so this is used later to set its cells. 
        /// </summary>
        /// <param name="mc">The cell</param>
        /// <param name="i">The cells x coordinate relative to bundle</param>
        /// <param name="j">The cells y coordinate relative to bundle</param>
        /// <param name="k">The cells z coordinate relative to bundle</param>
        /// <returns>Returns if the cell is on the walking path (and is added to <see cref="path"/>.</returns>
        public bool Add(MazeCell mc, int i, int j, int k = 0)
        {
            //  Debug.Log($"pair {i}, {j}, {k} : {length},{width},{height}");
            cells[i, j, k] = mc;
            //    if (i + j + k == 0) handle = mc;          
            bool onPath = k == (height - 1) * (i + 1) / length;
            Connection cend = height == 1 ? Connection.Pending : Connection.Open;
            Connection cside = height == 1 ? Connection.Pending : Connection.Unpassable;
            if (onPath)
            {
                path[i, j] = mc;
                mc.situation = mc == handle ? BundleSituation.Handle : BundleSituation.Entrance;
                mc.Set(MazeCell.X(side), i == 0 ? cend : Connection.None);
                mc.Set(side, i == length - 1 ? cend : Connection.None);
                mc.Set((side + 1) % 4, j == width - 1 ? cside : Connection.None);
                mc.Set((side + 3) % 4, j == 0 ? cside : Connection.None);
            }
            else
            {
                mc.Set(MazeCell.X(side), i == 0 ? Connection.Unpassable : Connection.None);
                mc.Set(side, i == length - 1 ? Connection.Unpassable : Connection.None);
                mc.Set((side + 1) % 4, j == width - 1 ? Connection.Unpassable : Connection.None);
                mc.Set((side + 3) % 4, j == 0 ? Connection.Unpassable : Connection.None);
                mc.situation = BundleSituation.Hanging;
            }
            return onPath;
        }
        /// <summary>
        /// Sets the neighborhood connections with cells on the periphery of the bundle.
        /// </summary>
        /// <param name="side">The side to check the neighborhoods.</param>
        public void SetNeighbors(int side)
        {
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                {
                    if (i > 0)
                    {
                        path[i, j].Neighbor(MazeCell.X(side), path[i - 1, j]);
                        path[i - 1, j].Neighbor(side, path[i, j]);
                    }
                    if (j > 0)
                    {
                        path[i, j].Neighbor(MazeCell.X(side), path[i, j - 1]);
                        path[i, j - 1].Neighbor(side, path[i, j]);
                    }
                }
        }
        /// <summary>
        /// Add the created and added cells in the bundle to the containing maze.
        /// </summary>
        public void AddCellsToMaze()
        {
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                    for (int k = 0; k < height; k++)
                    {
                        MazeCell c = cells[i, j, k];
                        maze.cells[c.x, c.y, c.z] = cells[i, j, k];
                    }
        }
        /// <summary>
        /// Because all the cells in a bundle are considered as one for the sake of rendering, their visibility offset is defined together here. See <see cref="VisionMap"/> for more info.
        /// </summary>
        /// <param name="map">The vision map for the level.</param>
        public void SetOffsets(LevelVision map)
        {
            int count = map.all.Count;
            byte[,] bs;
            int x, z;
            if (map.level == levels[0])
            {
                bs = first = new byte[count, 2];
                z = levels[0];
                x = 0;
            }
            else
            {
                bs = latter = new byte[count, 2];
                z = levels[1];
                x = length - 1;
            }
            for (int i = 0; i < count; i++)
                for (int t = 0; t < 2; t++)
                {
                    bs[i, t] = cells[x, 0, z].offset[i, t];
                    for (int j = 1; j < width; j++)
                        bs[i, t] = LevelVision.Min(bs[i, t], cells[x, j, z].offset[i, t]);
                }
        }
        /// <summary>
        /// Shows the elements visible from this bundle. The difference between Show and <see cref="Apply"/> is that the latter also hides which should not be visible. This method is called only if the bundle is multi-level, and when the visibility may need to be updated from a cell that has visibility to this bundle.
        /// </summary>
        /// <param name="maze">The containing maze</param>
        /// <param name="level">The level of the cell from which this method is called</param>
        /// <param name="offset">The vision offset <see cref="VisionMap"/></param>
        /// <param name="transparency">Whether it should consider <see cref="WallType.SeeThrough"/> walls as transparent or not</param>
        public void Show(MazeMap maze, int level, byte offset, bool transparency)
        {
            int it = transparency ? 1 : 0;
            int oz;
            oz = level == handle.z ? levels[0] : levels[1];
            byte[,] v = level == levels[0] ? first : latter;
            int cc = v.GetLength(0);
            LevelVision lv = maze.vision.levels[oz];
            for (int i = 0; i < cc; i++)
            {
                byte q = v[i, it];
                bool active = q <= offset;

                if (active && !lv.lastState[i])
                {
                    lv.all[i].SetActive(active);
                    lv.lastState[i] = active;
                }
            }
        }
        /// <summary>
        /// Shows or hides elements based on their visibility from this bundle. The difference between Apply and <see cref="Show"/> is that the latter does not hide which should not be visible. This method is called only if the bundle is multi-level, and when the visibility may need to be updated from a cell that has visibility to this bundle.
        /// </summary>
        /// <param name="maze">The containing maze</param>
        /// <param name="level">The level of the cell from which this method is called</param>
        /// <param name="offset">The vision offset <see cref="VisionMap"/></param>
        /// <param name="transparency">Whether it should consider <see cref="WallType.SeeThrough"/> walls as transparent or not</param>
        public void Apply(MazeMap maze, int level, byte offset, bool transparency)
        {
            int it = transparency ? 1 : 0;
            int oz;

            byte[,] v = level == levels[0] ? first : latter;
            int l = v.GetLength(0);
            LevelVision lv = maze.vision.levels[level];
            for (int i = 0; i < l; i++)
            {
                byte q = v[i, it];
                bool active = q <= offset;
                //    if (active) Debug.Log("active " + lv.all[i].name);
                if (lv.lastState[i] != active)
                {
                    lv.all[i].SetActive(active);
                    lv.lastState[i] = active;
                }
            }
        }
        /// <summary>
        /// If it is possible to place a column around a cell in the maze.
        /// </summary>
        /// <param name="maze"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static bool ColumnPossible(MazeMap maze, int x, int y, int z)
        {
            for (int i = 0; i < 4; i++)
            {
                int xi = i < 2 ? x : x - 1;
                int yi = i % 3 == 0 ? y - 1 : y;
                if (maze.InRange(xi, yi))
                {
                    if (maze.cells[xi, yi, z].bundle == null && maze.cells[xi, yi, z].situation != BundleSituation.Void) return true;
                }
                else return true;
            }
            return false;
        }

    }
    /// <summary>
    /// This class is used when <see cref="PrefabSettings.alwaysVisible"/> is set true for a bundled floor. It creates visual-only clones of the game objects and make sure the right clone is visible through different levels. 
    /// </summary>
    public class VisibleClones
    {
        /// <summary>
        /// The cloned items (items[0] is the original).
        /// </summary>
        public GameObject[] items;
        /// <summary>
        /// The levels the clones extend to.
        /// </summary>
        public int[] levels;
        /// <summary>
        /// The starting level
        /// </summary>
        int startLevel;
        /// <summary>
        /// If the ramp is ascending (that is used with <see cref="startLevel"/> to iterate levels)
        /// </summary>
        bool ascending;
        /// <summary>
        /// The last visibility state of the items (this is too minimize reference to <see cref="GameObject.activeSelf"/>). 
        /// </summary>
        bool[] lastStates;
        /// <summary>
        /// Create clones from a game object.
        /// </summary>
        /// <param name="root">The maze root object</param>
        /// <param name="g">The clonable game object</param>
        /// <param name="startLevel">Starting level</param>
        /// <param name="endLevel">End level</param>
        public VisibleClones(GameObject root, GameObject g, int startLevel, int endLevel)
        {
            this.startLevel = startLevel;
            ascending = endLevel >= startLevel;
            GameObject pairRoot = new GameObject("aw " + g.name);
            pairRoot.transform.parent = root.transform;
            pairRoot.transform.position = g.transform.position;
            pairRoot.transform.rotation = g.transform.rotation;
            int count = Mathf.Abs(endLevel - startLevel) + 1;
            int d = startLevel > endLevel ? -1 : 1;
            items = new GameObject[count];
            levels = new int[count];
            lastStates = new bool[count];
            for (int i = 0; i < count; i++)
            {
                lastStates[i] = false;
                levels[i] = startLevel + d;
                if (i == 0) items[i] = g;
                else
                {
                    items[i] = GameObject.Instantiate(g);
                    items[i].name = g.name + " " + i;
                }
                items[i].transform.parent = pairRoot.transform;
                items[i].transform.localPosition = Vector3.zero;
                items[i].transform.localRotation = Quaternion.identity;
                items[i].SetActive(false);
            }
        }
        /// <summary>
        /// Initializes the vision tracks for the clones. <see cref="VisionTrack"/> components are used to control rendering of items based on the viewpoint.
        /// </summary>
        /// <param name="lv">The vision map of the level</param>
        public void InitializeVision(LevelVision lv)
        {
            for (int i = 1; i < items.Length; i++)
            {
                VisionTrack vt = items[i].GetComponent<VisionTrack>();
                if (vt == null) vt = items[i].AddComponent<VisionTrack>();
                vt.Initialize(lv.allMaterials, lv.maze.owner.walker, lv.level);
            }
        }
        /// <summary>
        /// Changes the visibility of the clones based on the level.
        /// </summary>
        /// <param name="level">The current level</param>
        public void SetLevel(int level)
        {
            int index = ascending ? level - startLevel : startLevel - level;
            for (int i = 0; i < items.Length; i++)
                if (i == index)
                {
                    if (!lastStates[i])
                    {
                        lastStates[i] = true;
                        items[i].SetActive(true);
                    }
                }
                else if (lastStates[i])
                {
                    lastStates[i] = false;
                    items[i].SetActive(false);

                }
        }
    }
}
