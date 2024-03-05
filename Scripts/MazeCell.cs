using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Resphinx.Maze
{
    public enum PairSituation { Normal, Handle, Pair, Undefined, Void }
    public enum Connection { Open, Closed, Pending, Unpassable, None }

    public class Visibility
    {
        public bool visible = false;
        public int offset = int.MaxValue;
        public static Visibility Visible = new Visibility() { visible = true };
        public Visibility() { }
        public Visibility(Visibility v)
        {
            visible = v.visible;
            offset = v.offset;
        }
        public static Visibility Min(Visibility a, Visibility b)
        {
            Visibility v = new Visibility();
            v.offset = Mathf.Min(a.offset, b.offset);
            if (a.visible || b.visible) v.visible = true;
            return v;
        }
    }
    public class MazeCell
    {
        static int lastPairIndex = 0;
        public int x, y, z, index;

        public int pairStart = -1, pairCount = 0;
        public bool pairedOnX = true;
        public int pairDirection = -1;
        public int pairIndex = 0;
        public bool pairEnding = false;
        public MazeCell otherSide = null;
        float offset, sign;
        public static float H2S;

        public PairSituation situation = PairSituation.Normal;
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
        float factor = 1;
        Vector3 reference;

        public GameObject floor, shape;
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
        public Visibility[,] visibility;
        public List<MazeCell> visiblePairOpaque, visiblePairTransparent;
        public Vector2Int[] around = new Vector2Int[8];
        //    Vector3[] corners = new Vector3[4];

        public MazeCell(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            index = MazeMap.maze.cols * y + x;
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
            reference = position = MazeMap.maze.size * new Vector3(x, 0, y) + MazeMap.maze.height * new Vector3(0, z, 0);
        }
        public static MazeCell Void(int x, int y, int z)
        {
            MazeCell cell = new MazeCell(x, y, z);
            cell.situation = PairSituation.Void;
            return cell;
        }
        const int _o = 0;
        const int _p = 1;
        const int _h = 2;
        const int _v = 3;
        public static MazeCell[] CreatePath(int x, int y, int z, int side, int length, int height)
        {
            int[] Xi = new int[length];
            int[] Yi = new int[length];
            int[] Zi = new int[length];

            Debug.Log("creating pair...");
            for (int i = 0; i < length; i++)
            {
                Xi[i] = side switch { 0 => x + i * 1, 2 => x - i * 1, _ => x };
                Yi[i] = side switch { 1 => y + i * 1, 3 => y - i * 1, _ => y };
                Zi[i] = z + height * (i + 1) / length;
                //        Debug.Log($"pair {i}: {Xi[i]},{Yi[i]},{Zi[i]}");
            }
            // checking validity of the cells
            for (int i = 0; i < length; i++)
                for (int j = 0; j <= height; j++)
                {
                    int zj = Zi[i] == z + height ? Zi[i] - j : Zi[i] + j;
                    if (MazeMap.maze.InRange(Xi[i], Yi[i]) && zj < MazeMap.maze.levels && zj >= 0)
                        if (MazeMap.maze.cells[Xi[i], Yi[i], zj] != null) return null;
                }
            Vector2Int d = Delta(side);
            if (!MazeMap.maze.InRange(Xi[^1] + d.x, Yi[^1] + d.y)) return null;
            else if (MazeMap.maze.cells[Xi[^1] + d.x, Yi[^1] + d.y, Zi[^1]] != null) return null;
            d = Delta(X(side));
            if (!MazeMap.maze.InRange(Xi[0] + d.x, Yi[0] + d.y)) return null;
            else if (MazeMap.maze.cells[Xi[0] + d.x, Yi[0] + d.y, Zi[0]] != null) return null;

            // creating the ramp
            MazeCell[] r = new MazeCell[height == 0 ? length : length * 2];
            int lastZ = z, nextZ;
            lastPairIndex++;
            int pairStart = MazeMap.maze.pairs.Count;
            for (int i = 0; i < length; i++)
            {
                Debug.Log($"pair {i}: {Xi[i]},{Yi[i]},{Zi[i]}");
                nextZ = i < length - 1 ? Zi[i + 1] : Zi[^1];
                MazeCell mc = r[i] = new MazeCell(Xi[i], Yi[i], Zi[i]) { pairIndex = lastPairIndex };
                MazeMap.maze.pairs.Add(mc);
                mc.situation = i == 0 ? PairSituation.Handle : PairSituation.Pair;
                mc.Set(X(side), Zi[i] == lastZ ? Connection.Open : Connection.None);
                mc.Set(side, Zi[i] == nextZ ? Connection.Open : Connection.None);
                mc.Set(side, height == 0 ? Connection.Closed : Connection.Unpassable, true);
                lastZ = Zi[i];
                mc.pairStart = pairStart;
                mc.pairCount = length;
                if (i > 0)
                {
                    mc.Neighbor(X(side), r[i - 1]);
                    r[i - 1].Neighbor(side, r[i]);
                }
                mc.pairedOnX = side % 2 == 0;
                mc.pairDirection = side;
                if (height != 0)
                    r[i].CalculateWalkVector(side, length, height, r[0]);

                if (height != 0)
                {
                    int zj = Zi[i] == z ? z + height : z;
                    mc = r[length + i] = new MazeCell(Xi[i], Yi[i], zj) { pairIndex = lastPairIndex };
                    Debug.Log($"void {i}: {Xi[i]},{Yi[i]},{zj}");
                    mc.situation = PairSituation.Undefined;
                    mc.Set(side, i == length - 1 ? Connection.Unpassable : Connection.None);
                    mc.Set(X(side), i == length - 1 ? Connection.Unpassable : Connection.None);
                    mc.Set(side, Connection.Unpassable, true);
                }
            }
            r[0].otherSide = r[length - 1];
            r[length - 1].otherSide = r[0];
            r[0].pairEnding = r[length - 1].pairEnding = true;
            Debug.Log("fisished pair..." + r.Length);
            return r;
        }

        internal void InitializeVisibility(int count)
        {
            visibility = new Visibility[count, 2];
            for (int i = 0; i < count; i++)
                for (int j = 0; j < 2; j++)
                    visibility[i, j] = new Visibility();
            visiblePairOpaque = new List<MazeCell>();
            visiblePairTransparent = new List<MazeCell>();
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

        public void CalculateWalkVector(int side, int length, int height, MazeCell handle)
        {
            float h = height * MazeMap.maze.height;
            float s = MazeMap.maze.size;
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
            factor = walk.magnitude / (length * s);
            Debug.Log("pair vect:" + walk.ToString() + " " + reference.ToString() + " " + factor);
        }
        public Vector3 NextPosition(Vector3 feet, Vector3 u, float speed, float dt)
        {
            Vector3 v = speed * dt * u;
            float dy;
            Vector3 f = feet + v;
            if (pairStart >= 0 && situation != PairSituation.Undefined)
            {
                Vector3 w = f - reference;
                if (pairedOnX)
                    dy = w.x / walk.x * walk.y;
                else
                    dy = w.z / walk.z * walk.y;
                return new Vector3(f.x, reference.y + dy, f.z);
            }
            else return new Vector3(f.x, position.y, f.z);

        }
        public Vector3 AddWalk(Vector3 u, float dt)
        {
            if (walk.y != 0)
            {
                Vector3 w = walk.normalized;
                Vector3 v = walk;
                v.y = 0;
                float a = Vector3.Angle(u, v);
                float e = Mathf.Cos(a * Mathf.Deg2Rad);

                float t = w.y / v.magnitude;
                float vm = e * u.magnitude;
                float vh = vm * t;
                return dt * new Vector3(u.x, vh, u.z);
            }
            else
            {
                return dt * u;
            }
        }

        public MazeCell CanGo(Vector3 p)
        {
            float q, s = MazeMap.maze.size, s2 = MazeMap.maze.size / 2;
            float min = MazeMap.maze.size / 15;
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
            MazeMap.maze.vision.levels[z].Apply(this, Mazer.Instance.currentVisionOffset);
        }
        public void LeaveCell()
        {
            //          MazeMap.maze.vision.levels[z].Apply(this);
        }
        public void ReviveShape()
        {
            shapeIndex = -shapeIndex;
            shape.SetActive(true);
        }
    }
}
