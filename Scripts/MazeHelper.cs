using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    public class MazeDasher
    {
        Vector3 vector, from;
        public Vector3 position;
        public float speed, progress;
        public DashStatus dashing = DashStatus.None;
        public MazeCell destination;
        public void Init(Vector3 from, MazeCell d, float time)
        {
            this.from = from;
            position = from;
            vector = d.position - from;
            speed = 1 / time;
            progress = 0;
            dashing = DashStatus.Dashing;
            destination = d;
        }
        public void Init(Vector3 from, Vector3 to, float time)
        {
            this.from = from;
            position = from;
            vector = to - from;
            speed = 1 / time;
            progress = 0;
            dashing = DashStatus.Dashing;
            //        destination = d;
        }
        public bool TryReach(float dt)
        {
            progress += dt * speed;
            position = from + progress * vector;
            if (progress >= 1)
            {
                position = from + vector;
                dashing = DashStatus.None;
                return true;
            }
            else return false;
        }
    }
    [Serializable]
    public class ItemID
    {
        public string id = "";
        public float chance = 0.2f;
        public List<PrefabManager> items = new List<PrefabManager>();

        public GameObject AddItem(MazeMap maze, GameObject handle)
        {
            PrefabManager.onCreation = true;
            PrefabSettings mc = handle.GetComponent<PrefabSettings>();
            PrefabManager pm;
            if (mc.rotatable)
                items.Add(pm = PrefabManager.CreateQuadro(maze, id, handle));
            else
                items.Add(pm = PrefabManager.CreateMono(maze, id, handle));
            PrefabManager.onCreation = false;
            return pm.root;
        }
    }
    public class ItemManager
    {

        public ItemID[] ids;
        public Dictionary<string, int> itemDictionary = new Dictionary<string, int>();
        public ItemID Find(string s)
        {
            foreach (ItemID id in ids)
                if (id.id == s.ToLower()) return id;
            return null;
        }
        public void AddItem(MazeMap maze, GameObject g)
        {
            string s = g.GetComponent<PrefabSettings>().id;
            ItemID iid = Find(s);
            if (iid != null)
                iid.AddItem(maze, g);
        }
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

        internal void SetItems(ItemID[] mazeItems)
        {
            ids = mazeItems;
            for (int i = 0; i < ids.Length; i++)
                itemDictionary.Add(ids[i].id, i);
        }
    }
    public class CellBundle
    {
        public int index, length, width, height;
        public MazeCell handle;
        public MazeCell[,,] cells;
        public MazeCell[,] path;
        public MazeMap maze;
        public byte[,] first, latter;
        public bool onX;
        public int ascending, side;
        public int[] levels;
        public Vector3 reference, walk;
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
        public void Show(MazeMap maze, int level, byte offset, bool t)
        {
            int it = t ? 1 : 0;
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
        public void Apply(MazeMap maze, int level, byte offset, bool t)
        {
            int it = t ? 1 : 0;
            int oz;

             byte[,] v = level == levels[0] ? first : latter;
            int l = v.GetLength(0);
             LevelVision lv = maze.vision.levels[level];
            for (int i = 0; i < l; i++)
            {
                byte q = v[i, it];
                bool active = q <= offset;
                if (active) Debug.Log("active " + lv.all[i].name);
                if (lv.lastState[i] != active)
                {
                    lv.all[i].SetActive(active);
                    lv.lastState[i] = active;
                }
            }
        }

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
    public class VisiblePair
    {
        public GameObject[] items;
        public int[] levels;
        int startLevel;
        bool ascending;
        bool[] lastStates;
        GameObject root;
        public VisiblePair(GameObject root, GameObject g, int startLevel, int endLevel)
        {
            this.startLevel = startLevel;
            ascending = endLevel >= startLevel;
            GameObject pairRoot = this.root = new GameObject("aw " + g.name);
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
        public void SetLevel(int l)
        {
            int index = ascending ? l - startLevel : startLevel - l;
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
