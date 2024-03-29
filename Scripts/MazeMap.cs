using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace Resphinx.Maze
{

    public class WallData
    {
        public GameObject opaque, seeThrough;
        public bool mirrored = false;
        public Vector2 opening = Vector2.left;
        public MazeCell[] cell = new MazeCell[2];
    }
    public class MazeMap
    {
        public MazeOwner owner;
        public GameObject root;

        public float activeTime;
        public const int VisibilityStateCount = 2;
        public const int TransChance = 7;
        public const float MaxOpenCell = 0.9f;
        public MazeCell[,,] cells;

        public int rows, cols, levels;
        public Vector2Int[] corner;
        public float size, height;
        int total2D;
        List<PrefabManager> wallPrefabs = new List<PrefabManager>();
        List<PrefabManager> floorPrefabs = new List<PrefabManager>();
        List<PrefabManager> columnPrefabs = new List<PrefabManager>();
        List<PrefabManager> openPrefabs = new List<PrefabManager>();
        List<PrefabManager> seePrefabs = new List<PrefabManager>();
        public List<WallData> seeThroughWalls = new List<WallData>();

        GameObject structure;
        public GameObject[] levelRoot;
        public GameObject prefabClone;
        public ItemManager itemManager;

        public VisionMap vision;
        public List<MazeCell> bundledCells = new List<MazeCell>();
        public List<CellBundle> bundles = new List<CellBundle>();
        public int lastBundleIndex = 0;

        public bool transparency = false;
        public MazeMap(int row, int col, int level, float size, float height)
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
        public void SetRoot(MazeOwner owner)
        {
            this.owner = owner;
            root = new GameObject("Maze Root " + owner.mazeIndex);
        }
        public void SetPrefabs(GameObject root)
        {
            MazeElements me = root.GetComponent<MazeElements>();
            structure = root;
            PrefabManager pm;
            prefabClone = new GameObject("prefabs");
            prefabClone.transform.parent = this.root.transform;
            vision.AddItem(prefabClone);
            int cc = root.transform.childCount;
            for (int i = 0; i < cc; i++)
                GetElement(root.transform.transform.GetChild(i).gameObject);

            if (me != null)
                for (int i = 0; i < me.items.Length; i++)
                    GetElement(me.items[i]);


        }
        void GetElement(GameObject go)
        {
            PrefabSettings mc = go.GetComponent<PrefabSettings>();
            if (mc != null)
                switch (mc.type)
                {
                    case ModelType.Wall:
                        if (mc.wallType == WallType.Closed) wallPrefabs.Add(PrefabManager.CreateQuadro(this, "wall", go));
                        else if (mc.wallType == WallType.Open) openPrefabs.Add(PrefabManager.CreateQuadro(this, "open", go));
                        else seePrefabs.Add(PrefabManager.CreateQuadro(this, "see", go));
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
                                    else if (!mc.IsOpen(j) && !nc.IsOpen(MazeCell.X(j)))
                                    {
                                        if (mc.Connectable(j) && nc.Connectable(MazeCell.X(j)))
                                        {
                                            if (mc.IsOpen(j) || nc.IsOpen(MazeCell.X(j)))
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
        public bool InRange(int x, int y)
        {
            return x >= 0 & x < cols && y >= 0 & y < rows;
        }


        public MazeCell Cell(int l, int a)
        {
            Vector2Int v = XY(a);
            if (InRange(v.x, v.y))
                return cells[v.x, v.y, l];
            return null;
        }


        public Vector2Int XY(int a)
        {
            return new Vector2Int(a % cols, a / cols);
        }
        public int XY(int x, int y)
        {
            return y * cols + x;
        }

        public bool AreNeighbors(MazeCell a, MazeCell b)
        {
            return Mathf.Abs(a.x - b.x) <= 1 && Mathf.Abs(a.y - b.y) <= 1;
        }

        int[] available = new int[4];
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

        bool CellPossible(int x, int y, int dir, out Vector2Int p)
        {
            Vector2Int d = MazeCell.Delta(dir);
            p = new Vector2Int(x + d.x, y + d.y);
            return InRange(x + d.x, y + d.y);
        }
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
        Vector2Int First(int k)
        {
            for (int m = 0; m < cols; m++)
                for (int n = 0; n < rows; n++)
                    if (cells[m, n, k] == null)
                        return new Vector2Int(m, n);
            return Vector2Int.zero;
        }
        void CreateCells(int lev)
        {
            int[] dir = new int[total2D];
            int i;
            for (i = 0; i < total2D; i++)
                dir[i] = 0;
            i = 0;
            Vector2Int first = First(lev);
            MazeCell[] path = new MazeCell[total2D];
            int d;

            path[0] = new MazeCell(this, first.x, first.y, lev) { };
            while (i >= 0)
            {
                d = RandomDir(dir[path[i].index]);
                if (d >= 0)
                {
                    dir[path[i].index] |= 1 << d;
                    if (CellPossible(path[i].x, path[i].y, d, out Vector2Int p))
                        if (cells[p.x, p.y, lev] == null)
                        {
                            path[i].Set(d);
                            i++;

                            path[i] = cells[p.x, p.y, lev] = new MazeCell(this, p.x, p.y, lev) { };
                            path[i].Set(MazeCell.X(d));
                            path[i].Neighbor(MazeCell.X(d), path[i - 1]);
                            path[i - 1].Neighbor(d, path[i]);
                        }
                        else if (cells[p.x, p.y, lev].situation != BundleSituation.Void && cells[p.x, p.y, lev].IsOpen(MazeCell.X(d)))
                        {
                            path[i].Set(d);
                            cells[p.x, p.y, lev].Set(MazeCell.X(d));
                            path[i].Neighbor(d, cells[p.x, p.y, lev]);
                            cells[p.x, p.y, lev].Neighbor(MazeCell.X(d), path[i]);
                        }
                }
                else
                    i--;
            }
            for (int m = 0; m < cols; m++)
                for (int n = 0; n < rows; n++)
                    if (cells[m, n, lev] == null)
                        cells[m, n, lev] = MazeCell.Void(this, m, n, lev);
        }
        void CreateBundle(PrefabManager pm)
        {
            if (pm.settings.height != 0)
                if (levels == 0) return;

            int dy = pm.settings.height;
            int k = 0;
            int x, y, z;
            for (int i = 0; i < pm.settings.positions.Length; i++)
                if (k < pm.settings.count)
                {
                    x = pm.settings.positions[i].x;
                    y = pm.settings.positions[i].y;
                    z = pm.settings.positions[i].z;
                    CellBundle cb = MazeCell.SlopeBundle(this, x, y, z, pm.settings.directions[i], pm.settings.length, pm.settings.width, dy);
                    if (cb != null)
                    {
                        cb.AddCellsToMaze();
                        cb.handle.floorPrefab = pm;
                    }
                    k++;
                }
        }
        public bool Check(int x, int y, int z, int d, int h)
        {
            Vector3Int[] v3 = new Vector3Int[h == 0 ? 2 : 4];
            v3[0] = new Vector3Int(x, y, z);
            Vector2Int delta = MazeCell.Delta(d);
            v3[1] = new Vector3Int(x + delta.x, y + delta.y, z);
            if (h != 0)
            {
                v3[2] = new Vector3Int(x, y, z + h);
                v3[3] = new Vector3Int(x + delta.x, y + delta.y, z + h);
            }
            for (int i = 0; i < v3.Length; i++)
                if (cells[v3[i].x, v3[i].y, v3[i].z] != null)
                    return false;

            return true;
        }
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
                                 mc.floor.name = "fl-pair " + mc.x + "," + mc.y;
                                if (cr.alwaysVisible)
                                    vision.AddAlwaysVisible(mc.floor, k, mc.floorPrefab.settings.height + k);
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
                                    vision.AddAlwaysVisible(mc.floor, k, mc.floorPrefab.settings.height + k);
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
                                                vision.AddItem(go, mc.x, mc.y, k, VisionItemType.Opaque, side);
                                                if (cr.alwaysVisible) vision.AddAlwaysVisible(go, k, k);
                                                wallData = mc.SetWall(go, side);
                                                if (seePrefabs.Count > 0)
                                                {
                                                    rand = UnityEngine.Random.Range(0, TransChance);
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
                                            Debug.Log("none: " + mc.ijk.ToString() + cells[v.x, v.y, k].ijk.ToString());
                                        }
                                    }
                                }
                                else
                                {
                                    cr.isEdge = true;
                                    go = PrefabManager.RandomIndexed(mc, wallPrefabs, cr, size);
                                    go.name = $"edge {i},{j},{k}-{side}";
                                    vision.AddItem(go, mc.x, mc.y, k, VisionItemType.Opaque, side);
                                    if (cr.alwaysVisible) vision.AddAlwaysVisible(go, k, k);
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
                            if (cr.alwaysVisible) vision.AddAlwaysVisible(go, k, k);
                        }
                //     else Debug.Log("col " + i + ", " + j + ", " + k);
            }
            root.transform.SetPositionAndRotation(owner.transform.position, owner.transform.rotation);
        }
        public void SetCurrentCell(int lev)
        {
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                    if (cells[i, j, lev] != null)
                        if (cells[i, j, lev].situation == BundleSituation.Unbundled)
                        {
                            owner.walker.SetCurrentCell(i, j, lev);
                            return;
                        }
        }
        public void SetCurrentCell(MazeCell cell)
        {
            if (cell != null) owner.walker.SetCurrentCell(cell.x, cell.y, cell.z);
        }
        public void SetVision(bool rayCast, byte growOffset = 0)
        {
            vision.SetLastStates();
            vision.VisionAsync(rayCast, growOffset);
        }
        public void SetTransparency(bool t)
        {
            transparency = t;
            MazeCell mc = owner.walker.currentCell;
            if (t) vision.levels[mc.z].Apply(owner.walker.currentCell, owner.currentVisionOffset);
            else
                vision.levels[mc.z].RemoveTransparency(mc);

        }

    }

}