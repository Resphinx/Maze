using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Resphinx.Maze
{
    /// <summary>
    /// The situation of a cell in relation to a construct:
    /// - <see cref="Unbundled"/>: the cell is not part of a bundle.
    /// - <see cref="Handle"/>: the cell is the handle of a non-void bundle, that is where the floor object is placed.
    /// - <see cref="Entrance"/>: a navigable cell of a bundle in its first or last row.
    /// - <see cref="Hanging"/>: a non-navigable cell of a bundle.
    /// - <see cref="Middle"/>: a navigable cell of a bundle in its middle.
    /// - <see cref="Void"/>: a non-navigable cell in a void bundle.
    /// </summary>
    public enum BundleSituation { Unbundled, Handle, Entrance, Hanging, Middle, Void }
    /// <summary>
    /// The type of a cell's connection to other cells.
    /// - <see cref="Open"/>: the player can pass through the shared side.
    /// - <see cref="Closed"/>: there is a wall on the shared side that can be passed by dash.
    /// - <see cref="Penfing"/>: temporary, only used during maze generation.
    /// - <see cref="Unpassable"/>: like <see cref="Closed"/> but passable by a dash.
    /// - <see cref="None"/>: No wall prefab shoud be placed there.
    /// </summary>
    public enum Connection { Open, Closed, Pending, Unpassable, None }
    /// <summary>
    /// The class representing a cell in a maze.
    /// </summary>
    public class MazeCell
    {
        MazeMap maze;
        public int x, y, z, index;

        public CellBundle bundle = null;

        public BundleSituation situation = BundleSituation.Unbundled;
        public Vector3Int ijk;
        public Vector3 position;
        // the 2x2 arrays bellow are arranged as follows: 
        // 0,0 => x-
        // 0,1 => x+
        // 1,0 => z-
        // 1,1 => z+
        // you can convert direction index (0..4) to the above indeices by Side method.
        public MazeCell[,] neighbors = new MazeCell[2, 2];
        public Vector2[,] opening = new Vector2[2, 2];
        public bool[,] allowPass = new bool[2, 2];
        public Connection[,] connection = new Connection[2, 2];

        public Vector3 walk = Vector3.right;

        public GameObject floor;
        public PrefabManager floorPrefab = null;
        public int shapeIndex = 0;
        public static List<MazeCell> shapeBack = new List<MazeCell>();
        public static int lastRevivedIndex = -1;

        public float shapeBackTime = 0;
        public WallData[] wallData = new WallData[4];
        public GameObject[] columns = new GameObject[4];
        public int[] wallStages = new int[4];
        public int[] columnStages = new int[4];
        public bool[] transWall = new bool[4];
        public GameObject[] items;
        public int[] itemRotation;
        public byte[,] offset, bundleOffset;
        public bool[] toHandleRow;
        public Vector2Int[] around = new Vector2Int[8];
        //    Vector3[] corners = new Vector3[4];

        public MazeCell(MazeMap map, int x, int y, int z)
        {
            this.maze = map;
            this.x = x;
            this.y = y;
            this.z = z;
            index = maze.cols * y + x;
            ijk = new Vector3Int(x, y, z);
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    connection[i, j] = Connection.Pending;
                    opening[i, j] = Vector2.up;
                    allowPass[i, j] = true;
                    around[j * 2 + i] = i == j ? new Vector2Int(x, y) : new Vector2Int(x + i, y + j);
                    around[j * 2 + i + 4] = new Vector2Int(x + i, y + j);
                }
            //        xy = MazeManager.maze.size * new Vector2(x, y);
            position = maze.size * new Vector3(x, 0, y) + maze.height * new Vector3(0, z, 0);
        }
        public static MazeCell Void(MazeMap map, int x, int y, int z)
        {
            MazeCell cell = new MazeCell(map, x, y, z);
            cell.situation = BundleSituation.Void;
            return cell;
        }
        const int _o = 0;
        const int _p = 1;
        const int _h = 2;
        const int _v = 3;

        public static CellBundle FlatBundle(MazeMap maze, int x, int y, int z, int side, int length, int width)
        {
            int[,] Xi = new int[length, width];
            int[,] Yi = new int[length, width];

            //   Debug.Log("creating pair...");
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                {
                    Xi[i, j] = side switch { 0 => x + i, 1 => x - j, 2 => x - i, _ => x + j };
                    Yi[i, j] = side switch { 0 => y + j, 1 => y + i, 2 => y - j, _ => y - i };
                 }
            // checking validity of the cells
            int cc = 0;
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)

                    if (maze.InRange(Xi[i, j], Yi[i, j]) && z < maze.levels && z >= 0)
                        if (maze.cells[Xi[i, j], Yi[i, j], z] == null) cc++;
            if (cc != length * width) return null;
            // creating the ramp
            CellBundle cb = new CellBundle(maze, new MazeCell(maze, x, y, z), side, length, width, 1);
            cb.handle.bundle = cb;
            MazeCell mc;
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                {
                      if (i + j == 0) mc = cb.handle;
                    else mc = new MazeCell(maze, Xi[i, j], Yi[i, j], z) { bundle = cb };
                    if (cb.Add(mc, i, j))
                        maze.bundledCells.Add(mc);
                }
            cb.SetNeighbors(side);
            return cb;
        }
        public static CellBundle SlopeBundle(MazeMap maze, int x, int y, int z, int side, int length, int width, int height)
        {
            int habs = Mathf.Abs(height) + 1;
            int dh = height > 0 ? 1 : -1;
            if (habs == 0) return FlatBundle(maze, x, y, z, side, length, width);
            int[,,] Xi = new int[length, width, habs];
            int[,,] Yi = new int[length, width, habs];
            int[,,] Zi = new int[length, width, habs];
            int cc = 0;
            //   Debug.Log("creating pair...");
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                    for (int k = 0; k < habs; k++)
                    {
                        Xi[i, j, k] = side switch { 0 => x + i, 1 => x - j, 2 => x - i, _ => x + j };
                        Yi[i, j, k] = side switch { 0 => y + j, 1 => y + i, 2 => y - j, _ => y - i };
                        Zi[i, j, k] = z + k * dh;
                        //        Debug.Log($"pair {i}: {Xi[i]},{Yi[i]},{Zi[i]}");
                    }
            // checking validity of the cells
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                    if (maze.InRange(Xi[i, j, 0], Yi[i, j, 0]))
                        for (int k = 0; k < habs; k++)
                            if (z + k * dh < maze.levels && z + k * dh >= 0)
                                if (maze.cells[Xi[i, j, k], Yi[i, j, k], z + k * dh] == null) cc++;
            if (cc != habs * length * width) return null;
            Vector2Int d = Delta(side), dx = Delta(X(side));
            cc = 0;
            int cc2 = 0;
            int l1 = length - 1;
            int h1 = habs - 1;
            for (int j = 0; j < width; j++)
            {
                if (maze.InRange(Xi[0, j, 0] + dx.x, Yi[0, j, 0] + dx.y))
                    if (maze.cells[Xi[0, j, 0] + dx.x, Yi[0, j, 0] + dx.y, z] == null) cc++;
                if (maze.InRange(Xi[l1, j, h1] + d.x, Yi[l1, j, h1] + d.y))
                    if (maze.cells[Xi[l1, j, h1] + d.x, Yi[l1, j, h1] + d.y, z] == null) cc2++;
            }
            if (cc == 0 || cc2 == 0) return null;
            // creating the ramp
            CellBundle cb = new CellBundle(maze, new MazeCell(maze, x, y, z), side, length, width, height);
            cb.handle.bundle = cb;
            MazeCell mc;

            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                    for (int k = 0; k < habs; k++)
                    {
                        //      Debug.Log($"pair {i}: {Xi[i]},{Yi[i]},{Zi[i]}");
                        if (i + j + k == 0) mc = cb.handle;
                        else mc = new MazeCell(maze, Xi[i, j, k], Yi[i, j, k], Zi[i, j, k]) { bundle = cb };
                        cb.Add(mc, i, j, k);

                    }
            cb.SetNeighbors(side);
            return cb;
        }

        internal void InitializeVisibility(int count, int bundleCount)
        {
            offset = new byte[count, 2];
            for (int i = 0; i < count; i++)
                for (int j = 0; j < 2; j++)
                    offset[i, j] = 255;

            bundleOffset = new byte[bundleCount, 2];
            toHandleRow = new bool[bundleCount];
            for (int i = 0; i < bundleCount; i++)
                for (int j = 0; j < 2; j++)
                    bundleOffset[i, j] = 255;

        }

        public void Neighbor(int d, MazeCell m)
        {
            switch (d)
            {
                case 0: neighbors[0, 1] = m; break;
                case 1: neighbors[1, 1] = m; break;
                case 2: neighbors[0, 0] = m; break;
                case 3: neighbors[1, 0] = m; break;
            }
        }
        public static Vector2Int Delta(int dir)
        {
            int dx = dir switch { 0 => 1, 1 => 0, 2 => -1, _ => 0 };
            int dy = dir switch { 0 => 0, 1 => 1, 2 => 0, _ => -1 };
            return new Vector2Int(dx, dy);
        }
        public static Vector2Int Side(int d)
        {
            return new Vector2Int(d % 2, 1 - d / 2);
        }
        public static int X(int dir)
        {
            return (dir + 2) % 4;
        }
        public Vector2Int Neighbor(int dir)
        {
            Vector2Int d = Delta(dir);
            return new Vector2Int(x + d.x, y + d.y);
        }
        public bool Connected(int dir)
        {
            Vector2Int d = Delta(dir);
            return Connected(d.x, d.y);
        }
        public Connection ConnectionType(int dir)
        {
            Vector2Int d = Delta(dir);
            int a = d.x == 0 ? 1 : 0;
            int b = a == 0 ? (d.x < 0 ? 0 : 1) : (d.y < 0 ? 0 : 1);
            return connection[a, b];
        }

        public Connection Get(int dir)
        {
            Vector2Int d = Delta(dir);

            int a = d.x == 0 ? 1 : 0;
            int b = a == 0 ? (d.x < 0 ? 0 : 1) : (d.y < 0 ? 0 : 1);
            return connection[a, b];
        }
        public bool Connected(int dx, int dy)
        {
            int a = dx == 0 ? 1 : 0;
            int b = a == 0 ? (dx < 0 ? 0 : 1) : (dy < 0 ? 0 : 1);
            return connection[a, b] == Connection.Open;
        }
        public bool IsClosed(int dir)
        {
            return Get(dir) == Connection.Closed;
        }
        public bool IsOpen(int dir)
        {
            return Get(dir) == Connection.Open;
        }
        public bool NoWall(int dir)
        {
            return Get(dir) == Connection.None;

        }
        public void Set(int dir, Connection c = Connection.Open, bool sides = false)
        {
            Vector2Int d;
            int a, b;
            if (sides)
            {
                d = Delta((dir + 1) % 4);
                a = d.x == 0 ? 1 : 0;
                b = a == 0 ? (d.x < 0 ? 0 : 1) : (d.y < 0 ? 0 : 1);
                connection[a, b] = c;
                d = Delta((dir + 3) % 4);
                a = d.x == 0 ? 1 : 0;
                b = a == 0 ? (d.x < 0 ? 0 : 1) : (d.y < 0 ? 0 : 1);
                connection[a, b] = c;
            }
            else
            {
                d = Delta(dir);
                a = d.x == 0 ? 1 : 0;
                b = a == 0 ? (d.x < 0 ? 0 : 1) : (d.y < 0 ? 0 : 1);
                connection[a, b] = c;
            }
        }
        public bool Connectable(int dir)
        {
            Connection c = Get(dir);
            return c == Connection.Open || c == Connection.Pending;
        }
        public bool Pending(int dir)
        {
            return Get(dir) == Connection.Pending;
        }
        public void SetFloor(GameObject go, float size)
        {
            floor = go;
            floor.transform.localScale *= size;
        }
        public WallData SetWall(GameObject go, int side, bool start = true)
        {
            wallData[side] = new WallData();
            wallData[side].opaque = go;
            //      wallData[side].opaque.name += " " + x + "," + y + ": " + side;
            wallData[side].cell[start ? 0 : 1] = this;
            //     wallData[side].opaque.transform.position = position;
            return wallData[side];
        }
        public WallData SetWall(GameObject go, int side, bool mirrored, Vector2 opening)
        {
            wallData[side] = new WallData();
            wallData[side].opaque = go;
            //   wallData[side].opaque.name += " " + x + "," + y + ": " + side;
            wallData[side].cell[0] = this;
            //      wallData[side].opaque.transform.position = position;
            wallData[side].mirrored = true;
            wallData[side].opening = opening;
            Vector2Int ij = Side(side);
            this.opening[ij.x, ij.y] = new Vector2(mirrored && side > 1 ? 1 - opening.y : opening.x, mirrored && side > 1 ? 1 - opening.x : opening.y);
            return wallData[side];
        }
        public void SetWall(WallData wd, int side)
        {
            wallData[side] = wd;
            wallData[side].cell[1] = this;
            if (wd.opening.x >= -0.001)
            {
                Vector2Int ij = Side(side);
                opening[ij.x, ij.y] = new Vector2(wd.mirrored && side > 1 ? 1 - wd.opening.y : wd.opening.x, wd.mirrored && side > 1 ? 1 - wd.opening.x : wd.opening.y);
            }
        }



        float Dist(Vector2 p, Vector2 u, float c)
        {
            return Mathf.Abs(Vector2.Dot(p, u) + c) / u.magnitude;
        }


        public Vector3 NextPosition(Vector3 feet, Vector3 u, float speed, float dt)
        {
            Vector3 v = speed * dt * u;
            float dy;
            Vector3 f = feet + v;
            if (bundle != null && situation != BundleSituation.Hanging)
                if (bundle.height != 1)
                {
                    Vector3 w = f - bundle.reference;
                    if (bundle.onX)
                        dy = w.x / bundle.walk.x * bundle.walk.y;
                    else
                        dy = w.z / bundle.walk.z * bundle.walk.y;
                    return new Vector3(f.x, bundle.reference.y + dy, f.z);
                }
            return new Vector3(f.x, position.y, f.z);

        }


        public MazeCell CanGo(Vector3 p)
        {
            float q, s = maze.size, s2 = maze.size / 2;
            float min = maze.size / 15;
            float rx = p.x >= 0 ? p.x % s : (p.x + s) % s;
            float rz = p.z >= 0 ? p.z % s : (p.z + s) % s;
            rx -= s2;
            rz -= s2;
            rx = Mathf.Abs(rx);
            if (rx > s2) rx = s2;
            rz = Mathf.Abs(rz);
            if (rz > s2) rz = s2;
            MazeCell r = this;
            if (rx < min && rz < min) r = null;
            else if (rx < min)
            {
                q = (p.z - position.z + s2) / s;
                if (p.x < position.x)
                {
                    if (neighbors[0, 0] != null && allowPass[0, 0])
                    {
                        if (q > opening[0, 0].x && q < opening[0, 0].y)
                        {
                            if (Mathf.Abs(p.x - position.x) > s2) r = neighbors[0, 0];
                        }
                        else
                            r = null;

                    }
                    else
                        r = null;
                }
                else
                {
                    if (neighbors[0, 1] != null && allowPass[0, 1])
                    {
                        if (q >= opening[0, 1].x && q <= opening[0, 1].y)
                        {
                            if (Mathf.Abs(p.x - position.x) > s2) r = neighbors[0, 1];
                        }
                        else
                            r = null;
                    }
                    else
                        r = null;
                }
            }
            else if (rz < min)
            {
                q = (p.x - position.x + s2) / s;
                if (p.z < position.z)
                {
                    if (neighbors[1, 0] != null && allowPass[1, 0])
                    {
                        if (q >= opening[1, 0].x && q <= opening[1, 0].y)
                        {
                            if (Mathf.Abs(p.z - position.z) > s2) r = neighbors[1, 0];
                        }
                        else
                            r = null;
                    }
                    else
                        r = null;
                }
                else
                {
                    if (neighbors[1, 1] != null && allowPass[1, 1])
                    {
                        if (q >= opening[1, 1].x && q <= opening[1, 1].y)
                        {
                            if (Mathf.Abs(p.z - position.z) > s2) r = neighbors[1, 1];
                        }
                        else
                            r = null;
                    }
                    else
                        r = null;
                }
            }
            return r;
        }
        public void EnterCell(bool checkShape)
        {
            maze.vision.levels[z].Apply(this, maze.owner.currentVisionOffset);
        }
        public void LeaveCell()
        {
            //          maze.vision.levels[z].Apply(this);
        }
        public Vector3 AddLocalElevation(Vector3 feet)
        {
            return feet;
        }
    }
}
