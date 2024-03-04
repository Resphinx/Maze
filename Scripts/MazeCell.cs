using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Resphinx.Maze
{
    public enum PairSituation { Normal, Handle, Pair, Undefined, Void }
    public enum Connection { Open, Closed, Pending, Unpassable, None }
    public enum Visibility { Invisible, Visible, Transparent, Opaque }
    public class MazeCell
    {
        static int lastPairIndex = 0;
        public int x, y, z, index;

        public MazeCell pair = null;
        public bool pairedOnX = true;
        public int pairDirection = -1;
        public int pairIndex = 0;
        float offset, sign;
        public static float H2S;

        public PairSituation situation = PairSituation.Normal;
        public Vector3Int ijk;
        public Vector3 position;
        public MazeCell[,] neighbors = new MazeCell[2, 2];
        public Vector2[,] opening = new Vector2[2, 2];
        public Connection[,] connection = new Connection[2, 2];
        public int stage;
        public Vector3 walk = Vector3.right;
        public const int Left = 0;
        public const int Right = 2;
        public const int Up = 1;
        public const int Down = 3;
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

        public Visibility[] visibility;
        public List<MazeCell> visiblePair;
        //    Vector3[] corners = new Vector3[4];

        public MazeCell(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            index = MazeMap.maze.cols * y + x;
            //          ij = new Vector2Int(x, y);
            ijk = new Vector3Int(x, y, z);
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    connection[i, j] = Connection.Pending;
                    opening[i, j] = Vector2.up;
                }
            //        xy = MazeManager.maze.size * new Vector2(x, y);
            position = MazeMap.maze.size * new Vector3(x, 0, y) + MazeMap.maze.height * new Vector3(0, z, 0);
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
        public static MazeCell[] CreatePair(int x, int y, int z, int d, int h)
        {
            int x1 = d switch { 0 => x + 1, 2 => x - 1, _ => x };
            int y1 = d switch { 1 => y + 1, 3 => y - 1, _ => y };
            int z1 = h == 0 ? z : (h < 0 ? z - 1 : z + 1);

            Debug.Log("creating pair ...");
            if (MazeMap.maze.cells[x, y, z] != null) return null;
            if (MazeMap.maze.cells[x, y, z1] != null) return null;
            if (MazeMap.maze.cells[x1, y1, z] != null) return null;
            if (MazeMap.maze.cells[x1, y1, z1] != null) return null;
            Debug.Log("creating pair succeeded");

            MazeCell[] r = new MazeCell[h == 0 ? 2 : 4];
            r[_o] = new MazeCell(x, y, z);
            r[_o].situation = PairSituation.Handle;
            r[_o].Set(d, h == 0 ? Connection.Open : Connection.None);
            r[_o].Set(X(d));
            r[_o].Set(d, h == 0 ? Connection.Closed : Connection.Unpassable, true);

            r[_p] = new MazeCell(x1, y1, z + h);
            r[_o].pair = r[_p];
            r[_p].pair = r[_o];
            r[_o].Neighbor(d, r[_p]);
            r[_p].Neighbor(X(d), r[_o]);
            r[_p].situation = PairSituation.Pair;
            r[_o].pairedOnX = r[_p].pairedOnX = d % 2 == 0;
            r[_o].pairDirection = d;
            if (h == 0)
            {
                r[_p].Set(d, Connection.Pending);
                r[_p].Set(X(d));
                r[_p].Set(d, Connection.Closed, true);
            }
            else
            {
                r[_p].Set(d, Connection.Open);
                r[_p].Set(X(d), Connection.None);
                r[_p].Set(d, Connection.Unpassable, true);

                r[_h] = new MazeCell(x1, y1, z);
                r[_h].Set(d, Connection.Unpassable);
                r[_h].Set(X(d), Connection.None);
                r[_h].Set(d, Connection.Unpassable, true);
                r[_h].situation = PairSituation.Undefined;
                r[_h].pair = r[_p];
                r[_v] = new MazeCell(x, y, z1);
                r[_v].Set(d, Connection.None);
                r[_v].Set(X(d), Connection.Unpassable);
                r[_v].Set(d, Connection.Unpassable, true);
                r[_v].situation = PairSituation.Undefined;
                r[_v].pair = r[_o];
            }
            if (h != 0)
            {
                r[_o].CalculateWalkVector(d, h > 0, true);
                r[_p].CalculateWalkVector(d, h > 0, false);
            }

            lastPairIndex++;
            for (int i = 0; i < r.Length; i++) r[i].pairIndex = lastPairIndex;

            return r;
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
        public bool PairedDirection(int dir)
        {
            if (pair != null && situation != PairSituation.Undefined)
            {
                Vector2Int d = Delta(dir);
                return d.x + x == pair.x && d.y + y == pair.y;
            }
            else return false;
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
            wallData[side].opaque.name += " " + x + "," + y + ": " + side;
            wallData[side].cell[start ? 0 : 1] = this;
            //     wallData[side].opaque.transform.position = position;
            return wallData[side];
        }
        public WallData SetWall(GameObject go, int side, bool mirrored, Vector2 opening)
        {
            wallData[side] = new WallData();
            wallData[side].opaque = go;
            wallData[side].opaque.name += " " + x + "," + y + ": " + side;
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
        public void CalculateWalkVector(int dir, bool ascending, bool startIsLevel)
        {
            //   float h = pair.position.y - position.y;
            float s = MazeMap.maze.size / 2; ;
            switch (dir)
            {
                case 0:
                case 1:
                    offset = startIsLevel ? s : -s;
                    sign = ascending ? 1 : -1;
                    break;
                case 2:
                case 3:
                    offset = startIsLevel ? -s : s;
                    sign = ascending ? -1 : 1;
                    break;
            }            //     walk = (q - p).normalized;
        }
        public Vector3 NextPosition(Vector3 feet, Vector3 u, float speed, float dt)
        {
            Vector3 v = speed * dt * u;
            float dy;
            Vector3 f = feet + v;
            if (pair != null && situation != PairSituation.Undefined)
            {
                if (pairedOnX)
                    dy = (f.x - position.x + offset);
                else
                    dy = (f.z - position.z + offset);
                dy *= sign * H2S;
                return new Vector3(f.x, position.y + dy, f.z);
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
        public Vector3 Sit(Vector3 feet)
        {
            if (walk.y == 0) return new Vector3(feet.x, position.y, feet.z);
            else
            {
                float y;
                float offset = MazeMap.maze.size / 2;
                if (position.x == pair.position.x)
                    y = position.y + (feet.z - position.z + offset) / (offset * 2) * walk.y / 2;
                else
                    y = position.y + (feet.x - position.x + offset) / (offset * 2) * walk.y / 2;
                return new Vector3(feet.x, y, feet.z);
            }
        }
        public MazeCell CanGo(Vector3 p)
        {
            //        Vector2 u = q - p;
            //   u = new Vector2(-u.y, u.x);
            //     float c = -Vector2.Dot(u, p);
            float q, s = MazeMap.maze.size, s2 = MazeMap.maze.size / 2;
            float min = MazeMap.maze.size / 15;
            float rx = p.x % s;
            float rz = p.z % s;
            rx -= s2;
            rz -= s2;
            rx = Mathf.Abs(rx);
            if (rx > s2) rx = s2;
            if (rz > s2) rz = s2;
            rz = Mathf.Abs(rz);
            //   Debug.Log("cango: " + rx + " " + rz);
            MazeCell r = this;
            int varDebug = 0;

            if (rx < min && rz < min) r = null;
            else if (rx < min)
            {
                q = (p.z - position.z + s2) / s;
                if (p.x < position.x)
                {
                    varDebug = 10;
                    if (neighbors[0, 0] != null)
                    {
                        varDebug = 11;
                        if (q > opening[0, 0].x && q < opening[0, 0].y)
                        {
                            //          Debug.Log(q + ", " + opening[0, 0].ToString());
                            if (Mathf.Abs(p.x - position.x) > s2) r = neighbors[0, 0];
                        }
                        else
                        {
                            //            Debug.Log(q + " " + Mathf.Abs(p.x - position.x));
                            r = null;
                        }

                    }
                    else
                        r = null;
                }
                else
                {
                    varDebug = 20;
                    if (neighbors[0, 1] != null)
                    {
                        varDebug = 21;
                        if (q >= opening[0, 1].x && q <= opening[0, 1].y)
                        {
                            //       Debug.Log(q + ", " + opening[0, 1].ToString());
                            if (Mathf.Abs(p.x - position.x) > s2) r = neighbors[0, 1];
                        }
                        else
                        {
                            //        Debug.Log(q + " " + Mathf.Abs(p.x - position.x));
                            r = null;
                        }

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
                    varDebug = 31;
                    if (neighbors[1, 0] != null)
                    {
                        varDebug = 32;
                        if (q >= opening[1, 0].x && q <= opening[1, 0].y)
                        {
                            //       Debug.Log(q + ", " + opening[1, 0].ToString());
                            if (Mathf.Abs(p.z - position.z) > s2) r = neighbors[1, 0];
                        }
                        else
                        {
                            //         Debug.Log(q + " " + Mathf.Abs(p.z - position.z));
                            r = null;
                        }
                    }
                    else
                        r = null;
                }
                else
                {
                    varDebug = 40;
                    if (neighbors[1, 1] != null)
                    {
                        varDebug = 41;
                        if (q >= opening[1, 1].x && q <= opening[1, 1].y)
                        {
                            //   Debug.Log(q + ", " + opening[1, 1].ToString());
                            if (Mathf.Abs(p.z - position.z) > s2) r = neighbors[1, 1];
                        }
                        else
                        {
                            //    Debug.Log(q + " " + Mathf.Abs(p.z - position.z));
                            r = null;
                        }

                    }
                    else
                        r = null;
                }
            }
            if (r == null) Debug.Log("cango: " + ijk.ToString() + " " + varDebug + " " + rx + ", " + rz);
            return r;
        }
        public void EnterCell(bool checkShape)
        {          
            MazeMap.maze.vision.levels[z].Apply(this);
        }
        public void ReviveShape()
        {
            shapeIndex = -shapeIndex;
            shape.SetActive(true);
        }
    }
}
