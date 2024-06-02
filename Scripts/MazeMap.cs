using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace Resphinx.Maze
{
    /// <summary>
    /// This class is used to store information when a wall is created to share it between adjacent cells.
    /// </summary>
    public class WallData
    {
        /// <summary>
        /// The opaque or base version of the wall.
        /// </summary>
        public GameObject opaque;
        /// <summary>
        /// The transparent version of the wall, if exists.
        /// </summary>
        public GameObject seeThrough;
        /// <summary>
        /// See <see cref="MazeCell.opening"/> and <see cref="PrefabSettings.mirrored"/>.
        /// </summary>
        public bool mirrored = false;
        /// <summary>
        /// See <see cref="MazeCell.opening"/>. The default value is 0 and 1 (fully open).
        /// </summary>
        public Vector2 opening = Vector2.left;
        /// <summary>
        /// The adjacent or containing cells.
        /// </summary>
        public MazeCell[] cell = new MazeCell[2];
    }
    /// <summary>
    /// This class represents the 3D model of the entire maze. 
    /// </summary>
    public class MazeMap
    {
        /// <summary>
        /// The <see cref="MazeOwner"/> component that controls this maze.
        /// </summary>
        public MazeOwner owner;
        /// <summary>
        /// The parent game object of the maze, whose transform and positions are local to this transform.
        /// </summary>
        public GameObject root;
        /// <summary>
        /// Defines the chance of a seethrough version of a closed wall (the chance is 1/value).
        /// </summary>
        public const int SeeThroughPool = 7;

        /// <summary>
        /// The cells of the maze (the 0 and 1 dimensions are for horizontal distribution of the cells).
        /// </summary>
        public MazeCell[,,] cells;
        /// <summary>
        /// The number of cells in the X direction or local X axis of the maze (same as <see cref="MazeOwner.col"/>}.
        /// </summary>
        public int cols;
        /// <summary>
        /// The number of cells in the Y direction or local Z axis of the maze (same as <see cref="MazeOwner.row"/>}.
        /// </summary>
        public int rows;
        /// <summary>
        /// The number of levels or cell-count in the Z direction or local Y axis of the maze (same as <see cref="MazeOwner.levelCount"/>}.
        /// </summary>
        public int levels;
        /// <summary>
        /// The width of a (square) cell (in local X and Z axes).
        /// </summary>
        public float size;
        /// <summary>
        /// The floor-to-floor height of each cell.
        /// </summary>
        public float height;
        int total2D;

        List<PrefabManager> wallPrefabs = new List<PrefabManager>();
        List<PrefabManager> floorPrefabs = new List<PrefabManager>();
        List<PrefabManager> columnPrefabs = new List<PrefabManager>();
        List<PrefabManager> openPrefabs = new List<PrefabManager>();
        List<PrefabManager> seePrefabs = new List<PrefabManager>();
        public List<WallData> seeThroughWalls = new List<WallData>();

        GameObject structure;
        /// <summary>
        /// The root game object for individual levels.
        /// </summary>
        public GameObject[] levelRoot;
        /// <summary>
        /// The root game object for prefabs used in this maze.
        /// </summary>
        public GameObject prefabClone;
        /// <summary>
        /// The item manager in this maze. See <see cref="ItemID"/> and <see cref="itemManager"/>.
        /// </summary>
        public ItemManager itemManager;
        /// <summary>
        /// The vision map for this maze.
        /// </summary>
        public VisionMap vision;
        /// <summary>
        /// List of all bundled cells in the maze, reflecting those in <see cref="bundles"/>, respectively.
        /// </summary>
        public List<MazeCell> bundledCells = new List<MazeCell>();
        /// <summary>
        /// The cell bundles in this maze.
        /// </summary>
        public List<CellBundle> bundles = new List<CellBundle>();
        /// <summary>
        /// This controls whether the opaque or see-through version of the walls should be active.
        /// </summary>
        public bool transparency = false;
        /// <summary>
        /// Creates an empty maze based on dimensions.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="level"></param>
        /// <param name="size"></param>
        /// <param name="height"></param>
        public MazeMap(int col, int row, int level, float size, float height)
        {
            this.size = size;
            this.height = height;
            rows = row;
            cols = col;
            levels = level;
            total2D = row * col;


            cells = new MazeCell[col, row, level];
            vision = new VisionMap(this);
            itemManager = new ItemManager();
        }
        /// <summary>
        /// Sets the <see cref="owner"/> and creates a new game object as the <see cref="root"/> for this maze.
        /// </summary>
        /// <param name="owner">The owner</param>
        public void SetRoot(MazeOwner owner)
        {
            this.owner = owner;
            root = new GameObject("Maze Root " + owner.mazeIndex);
        }
        /// <summary>
        /// Sets the <see cref="prefabClone"/> and creates clones of the prefabs and localizes them (these clones are not the maze elements, but are used as prefabs for creating the maze elements). This is to make sure different mazes do not share unwanted prefab settings.
        /// </summary>
        /// <param name="root"></param>
        public void SetPrefabs(GameObject root)
        {
            MazeElements me = owner.GetComponent<MazeElements>();
            structure = root;
            PrefabSettings pm;
            prefabClone = new GameObject("prefabs");
            prefabClone.transform.parent = this.root.transform;
            vision.AddItem(prefabClone);
            int cc = root.transform.childCount;
            List<PrefabSettings> settings = new List<PrefabSettings>();

            for (int i = 0; i < cc; i++)
                if ((pm = root.transform.transform.GetChild(i).GetComponent<PrefabSettings>()) != null)
                {
                    pm.local = null;
                    settings.Add(pm);
                }
            if (me != null)
                for (int i = 0; i < me.items.Length; i++)
                    if (me.items[i].prefab != null)
                    {
                        Debug.Log(me.items[i].prefab.name);
                        me.items[i].prefab.local = me.items[i];
                        if (settings.IndexOf(me.items[i].prefab) < 0)
                            settings.Add(me.items[i].prefab);
                    }

            foreach (PrefabSettings ps in settings)
                AddElement(ps);

        }
        /// <summary>
        /// Creates the rotated gameobjects in a prefab setting suitable for instantiating as maze elements
        /// </summary>
        /// <param name="setting"></param>
        void AddElement(PrefabSettings setting)
        {
            //     PrefabSettings mc = go.GetComponent<PrefabSettings>();
            GameObject go = setting.gameObject;
            switch (setting.type)
            {
                case ModelType.Wall:
                    if (setting.wallType == WallType.Closed) wallPrefabs.Add(PrefabManager.CreateOrdered(this, "wall", go));
                    else if (setting.wallType == WallType.Open) openPrefabs.Add(PrefabManager.CreateOrdered(this, "open", go));
                    else seePrefabs.Add(PrefabManager.CreateOrdered(this, "see", go));
                    break;
                case ModelType.Column:
                    columnPrefabs.Add(PrefabManager.CreateRandom(this, "columns", go));
                    break;
                case ModelType.Floor:
                    floorPrefabs.Add(PrefabManager.CreateRandom(this, "floor", go));
                    break;
                case ModelType.Item:
                    itemManager.AddItem(this, go);
                    break;
            }
        }
        /// <summary>
        /// Destroys the maze. This is only called now when a new maze is generated for the owner of this maze.
        /// </summary>
        public void DestroyEverything()
        {
            for (int i = vision.grid.Count - 1; i >= 0; i--)
                try
                {
                    GameObject go = vision.grid[i];
                    vision.grid.RemoveAt(i);
                    GameObject.Destroy(go);
                }
                catch (Exception ex) { }
            for (int i = vision.others.Count - 1; i >= 0; i--)
                try
                {
                    GameObject go = vision.others[i];
                    vision.others.RemoveAt(i);
                    GameObject.Destroy(go);
                }
                catch (Exception ex) { }
            GameObject.Destroy(root);
            wallPrefabs.Clear();
            columnPrefabs.Clear();
            openPrefabs.Clear();
            floorPrefabs.Clear();
            seePrefabs.Clear();
            seeThroughWalls.Clear();
        }
        /// <summary>
        /// Initializes the maze by adding void and bundles, and then initializing cells. This does not populate elements (see <see cref="GenerateModel(bool)"/> for this). 
        /// </summary>
        public void Initialize()
        {


            DateTime now = DateTime.Now;
            int ms = now.Millisecond;
            UnityEngine.Random.InitState(ms);
            CreateVoids();
            foreach (PrefabManager pm in floorPrefabs)
            {
                if (pm.settings != null)
                    if (pm.settings.Bundled)
                        CreateBundle(pm);
            }
            for (int k = 0; k < levels; k++)
            {
                CreateCells(k);
                bool und;
                for (int i = 0; i < total2D; i++)
                {
                    MazeCell nc, mc = Cell(k, i);
                    if (mc != null)
                        if (mc.situation != BundleSituation.Hanging)
                            for (int j = 0; j < 4; j++)
                            {
                                Vector2Int d = MazeCell.Delta(j);
                                und = false;
                                if (InRange(mc.x + d.x, mc.y + d.y))
                                {
                                    nc = cells[mc.x + d.x, mc.y + d.y, k];
                                    if (nc.situation == BundleSituation.Hanging) mc.Set(j, Connection.Unpassable);
                                    else if (!mc.Connected(j) && !nc.Connected(MazeCell.X(j)))
                                    {
                                        if (mc.Connectable(j) && nc.Connectable(MazeCell.X(j)))
                                        {
                                            if (mc.Connected(j) || nc.Connected(MazeCell.X(j)))
                                                und = true;
                                            else if (UnityEngine.Random.Range(0, 10) == 0)
                                                und = true;
                                        }
                                        if (und)
                                        {
                                            mc.Set(j);
                                            nc.Set(MazeCell.X(j));
                                            mc.Neighbor(j, nc);
                                            nc.Neighbor(MazeCell.X(j), mc);
                                        }
                                        else if (!mc.NoWall(j) && !nc.NoWall(MazeCell.X(j)))
                                        {
                                            mc.Set(j, Connection.Closed);
                                            nc.Set(MazeCell.X(j), Connection.Closed);
                                        }
                                    }
                                }
                                else mc.Set(j, Connection.Unpassable);

                            }
                }
            }
        }
        /// <summary>
        /// Whether a coordinate is within the maze horizonal range (even if it is void)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>True if in range</returns>
        public bool InRange(int x, int y)
        {
            return x >= 0 & x < cols && y >= 0 & y < rows;
        }
        /// <summary>
        /// Gives a cell by its index in a level (see <see cref="XY(int)"/> and <see cref="XY(int, int)"/>). This method doesn't check whether the cell is in range or not.
        /// </summary>
        /// <param name="lvl">The cell's level</param>
        /// <param name="index">The cell's index</param>
        /// <returns>Returns a cell if found, or null if the cell doesn't exist</returns>
        public MazeCell Cell(int lvl, int index)
        {
            Vector2Int v = XY(index);
            if (InRange(v.x, v.y))
                return cells[v.x, v.y, lvl];
            return null;
        }
        /// <summary>
        /// Returns the coordinates of a cell index. See <see cref="XY(int, int)"/> for calculation of the index.
        /// </summary>
        /// <param name="index">The cell's index</param>
        /// <returns>A vector representing the x and y coordinates. Please note that it may not be in range.</returns>
        public Vector2Int XY(int index)
        {
            return new Vector2Int(index % cols, index / cols);
        }
        /// <summary>
        /// Returns the index of a cell's horizontal coordinate (x and y). The index is calculated as <see cref="MazeCell.y"/> * <see cref="cols"/> + <see cref="MazeCell.x"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int XY(int x, int y)
        {
            return y * cols + x;
        }
        int[] available = new int[4];
        /// <summary>
        /// Returns a random direction index (see <see cref="MazeCell.Side(int)"/>). This is used when generating the maze to ensure the maze navigation looks random. 
        /// </summary>
        /// <param name="current">The current direction of the cell</param>
        /// <returns></returns>
        int RandomDir(int current)
        {
            int k = 0;
            for (int i = 0; i < 4; i++)
                if (((1 << i) & current) == 0)
                { available[k] = i; k++; }
            if (k == 0) return -1;
            int d = UnityEngine.Random.Range(0, k);
            return available[d];
        }
        /// <summary>
        /// Check if a neighboring cell is in range.
        /// </summary>
        /// <param name="x">X of the current cell</param>
        /// <param name="y">Y of the current cell</param>
        /// <param name="dir">The intended direction</param>
        /// <param name="p">The coordinates of the neighbor</param>
        /// <returns>True if the neighbor is in range</returns>
        bool CellPossible(int x, int y, int dir, out Vector2Int p)
        {
            Vector2Int d = MazeCell.Delta(dir);
            p = new Vector2Int(x + d.x, y + d.y);
            return InRange(x + d.x, y + d.y);
        }
        /// <summary>
        /// Creates voids based on <see cref="VoidMaker"/> components attached to the prefab root. This is called before creating bundles and cells.
        /// </summary>
        void CreateVoids()
        {
            VoidMaker[] voids = structure.GetComponentsInChildren<VoidMaker>();
            for (int i = 0; i < voids.Length; i++)
            {
                int xMax = cols - voids[i].size.x;
                int yMax = rows - voids[i].size.y;
                int x, y, z;
                for (int j = 0; j < voids[i].count; j++)
                {
                    if (j < voids[i].positions.Length && voids[i].positions[j].z < levels && voids[i].positions[j].z >= 0)
                    {
                        x = voids[i].positions[j].x;
                        y = voids[i].positions[j].y;
                        z = voids[i].positions[j].z;
                    }
                    else
                    {
                        x = UnityEngine.Random.Range(0, xMax);
                        y = UnityEngine.Random.Range(0, yMax);
                        z = UnityEngine.Random.Range(0, levels);
                    }
                    for (int m = 0; m < voids[i].size.x; m++)
                        for (int n = 0; n < voids[i].size.y; n++)
                            if (x < xMax && y < yMax && x + m >= 0 && y + n >= 0)
                                cells[x + m, y + n, z] = MazeCell.Void(this, x + m, y + n, z);
                }
            }
        }
        /// <summary>
        /// Finds the first undefined cell on a level
        /// </summary>
        /// <param name="lvl">The current level</param>
        /// <returns>The cell's coordinate</returns>
        Vector2Int First(int lvl)
        {
            for (int m = 0; m < cols; m++)
                for (int n = 0; n < rows; n++)
                    if (cells[m, n, lvl] == null)
                        return new Vector2Int(m, n);
            return Vector2Int.zero;
        }
        /// <summary>
        /// Creates the cells and maze's navigation path for a level. This is called after creating voids and bundles.
        /// </summary>
        /// <param name="lvl">The level</param>
        void CreateCells(int lvl)
        {
            int[] dir = new int[total2D];
            int i;
            for (i = 0; i < total2D; i++)
                dir[i] = 0;
            i = 0;
            Vector2Int first = First(lvl);
            MazeCell[] path = new MazeCell[total2D];
            int d;

            path[0] = new MazeCell(this, first.x, first.y, lvl) { };
            while (i >= 0)
            {
                d = RandomDir(dir[path[i].index]);
                if (d >= 0)
                {
                    dir[path[i].index] |= 1 << d;
                    if (CellPossible(path[i].x, path[i].y, d, out Vector2Int p))
                        if (cells[p.x, p.y, lvl] == null)
                        {
                            path[i].Set(d);
                            i++;

                            path[i] = cells[p.x, p.y, lvl] = new MazeCell(this, p.x, p.y, lvl) { };
                            path[i].Set(MazeCell.X(d));
                            path[i].Neighbor(MazeCell.X(d), path[i - 1]);
                            path[i - 1].Neighbor(d, path[i]);
                        }
                        else if (cells[p.x, p.y, lvl].situation != BundleSituation.Void && cells[p.x, p.y, lvl].Connected(MazeCell.X(d)))
                        {
                            path[i].Set(d);
                            cells[p.x, p.y, lvl].Set(MazeCell.X(d));
                            path[i].Neighbor(d, cells[p.x, p.y, lvl]);
                            cells[p.x, p.y, lvl].Neighbor(MazeCell.X(d), path[i]);
                        }
                }
                else
                    i--;
            }
            for (int m = 0; m < cols; m++)
                for (int n = 0; n < rows; n++)
                    if (cells[m, n, lvl] == null)
                        cells[m, n, lvl] = MazeCell.Void(this, m, n, lvl);
        }
        /// <summary>
        /// Creates bundles based on a <see cref="PrefabSettings"/>. This is called after creating voids but before creating cells.
        /// </summary>
        /// <param name="pm">The prefab manager containing the prefab setting</param>
        void CreateBundle(PrefabManager pm)
        {
            if (pm.settings.height != 0)
                if (levels == 0) return;

            int dy = pm.settings.height;
            int k = 0;
            int x, y, z;
            for (int i = 0; i < pm.positions.Length; i++)
                if (k < pm.initialPool)
                {
                    x = pm.positions[i].x;
                    y = pm.positions[i].y;
                    z = pm.positions[i].z;
                    CellBundle cb = MazeCell.SlopeBundle(this, x, y, z, pm.directions[i], pm.settings.length, pm.settings.width, dy);
                    if (cb != null)
                    {
                        cb.AddCellsToMaze();
                        cb.handle.floorPrefab = pm;
                    }
                    k++;
                }
        }
        /// <summary>
        /// Generates the 3D elements of the maze. 
        /// </summary>
        /// <param name="scale">This represents the localScale of the elements.</param>
        public void GenerateModel(bool scale)
        {
            int side, rand;
            GameObject go;
            WallData wallData;
            float size = scale ? this.size : 1;

            levelRoot = new GameObject[levels];
            for (int k = 0; k < levels; k++)
            {
                levelRoot[k] = new GameObject("level-" + k);
                levelRoot[k].transform.parent = root.transform;
            }


            CloneResult cr = new CloneResult();
            for (int k = 0; k < levels; k++)
            {
                cr.level = k;

                for (int i = 0; i < cols; i++)
                    for (int j = 0; j < rows; j++)
                    {

                        // floors
                        MazeCell mc = cells[i, j, k];
                        cr.isEdge = mc.x == 0 || mc.x == cols - 1 || mc.y == 0 || mc.y == rows - 1;
                        cr.isCorner = (mc.x == 0 || mc.x == cols - 1) && (mc.y == 0 || mc.y == rows - 1);
                        if (mc.situation != BundleSituation.Void)
                        {
                            if (mc.floorPrefab != null)
                            {
                                cr.sideIndex = mc.bundle.side;
                                vision.AddItem(mc.floor = PrefabManager.RandomIndexed(mc, new List<PrefabManager>() { mc.floorPrefab }, cr, size), mc.x, mc.y, k, VisionItemType.Floor);
                                mc.floor.name = "fl-pair " + mc.x + "," + mc.y + "," + mc.z;
                                if (cr.alwaysVisible)
                                {
                                    vision.AddAlwaysVisible(mc.floor, k, mc.floorPrefab.settings.height + k);
                                    vision.alwaysVisible[^1].InitializeVision(vision.levels[k]);
                                }
                            }
                            else if (mc.situation == BundleSituation.Middle || mc.situation == BundleSituation.Entrance)
                            {
                                vision.AddItem(mc.floor = new GameObject("fl-empty" + mc.x + "," + mc.y), mc.x, mc.y, k, VisionItemType.Floor);
                                mc.floor.transform.parent = levelRoot[k].transform;
                                mc.floor.transform.localPosition = mc.position;
                            }
                            else if (mc.situation == BundleSituation.Unbundled)
                            {
                                vision.AddItem(mc.floor = PrefabManager.RandomFloor(mc, floorPrefabs, cr, size), mc.x, mc.y, k, VisionItemType.Floor);
                                mc.floor.name = "fl-solo " + mc.x + "," + mc.y;
                                if (cr.alwaysVisible)
                                {
                                    vision.AddAlwaysVisible(mc.floor, k, mc.floorPrefab.settings.height + k);
                                    vision.alwaysVisible[^1].InitializeVision(vision.levels[k]);
                                }
                            }
                            // walls
                            for (side = 0; side < 4; side++)
                            {
                                Vector2Int v = mc.Neighbor(side);
                                cr.sideIndex = side;
                                bool hasAdjacent = true;
                                if (!InRange(v.x, v.y)) hasAdjacent = false;
                                else if (cells[v.x, v.y, k].situation == BundleSituation.Void) hasAdjacent = false;

                                if (hasAdjacent)
                                {
                                    if (v.x >= mc.x && v.y >= mc.y)
                                    {
                                        if (cells[v.x, v.y, k].bundle == null || mc.bundle == null || cells[v.x, v.y, k].bundle != mc.bundle)
                                        {
                                            cr.isEdge = false;
                                            cr.isCorner = false;
                                            if (mc.Connected(side))
                                            {
                                                go = PrefabManager.RandomIndexed(mc, openPrefabs, cr, size);
                                                go.name = $"open {i},{j},{k}-{side}";
                                                vision.AddItem(go, mc.x, mc.y, k, VisionItemType.Open, side);
                                                if (cr.alwaysVisible) vision.AddAlwaysVisible(go, k, k);
                                                wallData = mc.SetWall(go, side, openPrefabs[cr.prefabIndex].settings.mirrored, openPrefabs[cr.prefabIndex].settings.opening);
                                            }
                                            else
                                            {
                                                go = PrefabManager.RandomIndexed(mc, wallPrefabs, cr, size);
                                                go.name = $"closed {i},{j},{k}-{side}";
                                                Debug.Log(go.name);
                                                vision.AddItem(go, mc.x, mc.y, k, VisionItemType.Opaque, side);
                                                if (cr.alwaysVisible) vision.AddAlwaysVisible(go, k, k);
                                                wallData = mc.SetWall(go, side);
                                                if (seePrefabs.Count > 0)
                                                {
                                                    rand = UnityEngine.Random.Range(0, SeeThroughPool);
                                                    if (rand == 2)
                                                    {
                                                        seeThroughWalls.Add(wallData);
                                                        go = PrefabManager.RandomIndexed(mc, seePrefabs, cr, size);
                                                        go.name = $"trans {i},{j},{k}-{side}";
                                                        vision.AddItem(go, mc.x, mc.y, k, VisionItemType.Transparent, side);
                                                        if (cr.alwaysVisible) vision.AddAlwaysVisible(go, k, k);
                                                        wallData.seeThrough = go;
                                                    }
                                                }
                                            }
                                            cells[v.x, v.y, k].SetWall(wallData, MazeCell.X(side));
                                        }
                                        else
                                        {
                                            vision.AddItem(mc.x, mc.y, k, side);
                                            //     Debug.Log("none: " + mc.ijk.ToString() + cells[v.x, v.y, k].ijk.ToString());
                                        }
                                    }
                                }
                                else
                                {
                                    cr.isEdge = true;
                                    go = PrefabManager.RandomIndexed(mc, wallPrefabs, cr, size);
                                    go.name = $"edge {i},{j},{k}-{side}";
                                    Debug.Log(go.name);
                                    vision.AddItem(go, mc.x, mc.y, k, VisionItemType.Opaque, side);
                                    if (cr.alwaysVisible)
                                    {
                                        vision.AddAlwaysVisible(go, k, k);
                                        vision.alwaysVisible[^1].InitializeVision(vision.levels[k]);
                                    }
                                    mc.SetWall(go, side, v.x >= mc.x && v.y >= mc.y);
                                }
                            }
                            // items
                            cells[i, j, k].items = new GameObject[itemManager.ids.Length];
                            cells[i, j, k].itemRotation = new int[itemManager.ids.Length];
                            for (int itemIndex = 0; itemIndex < itemManager.ids.Length; itemIndex++)
                                if (UnityEngine.Random.value < itemManager.ids[itemIndex].chance)
                                {
                                    if (cells[i, j, k].situation != BundleSituation.Unbundled) continue;
                                    go = itemManager.GetItem(cells[i, j, k], itemIndex, cr);
                                    if (go != null)
                                    {
                                        go.name = $"item {itemIndex} ({i},{j},{k})";
                                        cells[i, j, k].items[itemIndex] = go;
                                        cells[i, j, k].itemRotation[itemIndex] = cr.sideIndex;
                                    }
                                }
                        }
                    }


                cr.isEdge = cr.isCorner = false;
                for (int i = 0; i <= cols; i++)
                    for (int j = 0; j <= rows; j++)
                        if (CellBundle.ColumnPossible(this, i, j, k))
                        {
                            cr.isEdge = i == 0 || i == cols || j == 0 || j == rows;
                            cr.isCorner = (i == 0 || i == cols) && (j == 0 || j == rows);
                            Vector3 p = new Vector3((i - 0.5f) * this.size, k * height, (j - 0.5f) * this.size);
                            go = PrefabManager.RandomNoIndex(p, columnPrefabs, cr, size);
                            go.name = $"col {i},{j},{k}";
                            vision.AddItem(go, i, j, k, VisionItemType.Column);
                            if (cr.alwaysVisible)
                            {
                                vision.AddAlwaysVisible(go, k, k);
                                vision.alwaysVisible[^1].InitializeVision(vision.levels[k]);
                            }
                        }
                //     else Debug.Log("col " + i + ", " + j + ", " + k);
            }
            root.transform.SetPositionAndRotation(owner.transform.position, owner.transform.rotation);
        }
        /// <summary>
        /// Positions the character (<see cref="MazeWalker"/>) within the first availble cell on a level. See alse <see cref="SetCurrentCell(MazeCell)"/>
        /// </summary>
        /// <param name="lvl">The level</param>
        public void SetCurrentCell(int lvl)
        {
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                    if (cells[i, j, lvl] != null)
                        if (cells[i, j, lvl].situation == BundleSituation.Unbundled)
                        {
                            owner.walker.SetCurrentCell(i, j, lvl);
                            return;
                        }
        }
        /// <summary>
        ///  Positions the character (<see cref="MazeWalker"/>) on a specified on a level. See alse <see cref="SetCurrentCell(int)"/>
        /// </summary>
        /// <param name="cell">The destination cell</param>
        public void SetCurrentCell(MazeCell cell)
        {
            if (cell != null) owner.walker.SetCurrentCell(cell.x, cell.y, cell.z);
        }
    }
}