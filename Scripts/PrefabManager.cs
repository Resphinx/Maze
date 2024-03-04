using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    public class CloneResult
    {
        public GameObject gameObject;
        public int sideIndex = 0, prefabIndex;
        public bool isEdge = false, isCorner = false, isVoid = false;
        public int level = 0;
        public bool alwaysVisible = false;
        public CloneResult Criteria(bool e, bool c, bool v, int level = -1, int sideIndex = -1)
        {
            isCorner = c;
            isVoid = v;
            isEdge = e;
            this.level = level;
            if (sideIndex >= 0) this.sideIndex = sideIndex;
            return this;
        }
    }
    public class PrefabManager
    {
        public string name;
        public GameObject root;
        public GameObject[] side;
        public PrefabSettings modelCount;
        //     public int[] childIndex;
        public int allIndex;
        //    public int count = 1;
        public float pool = 0;
        public bool pairable = false;
        //   public bool pooled = true;
        public enum PrefabType { Ordered, Random, Mono }
        public PrefabType prefabType;
        public static int[] order = new int[] { 2, 3, 0, 1 };
        public static bool onCreation = false;
        public static PrefabManager CreateMono(string name, GameObject handle, bool addSides = false)
        {
            onCreation = true;
            PrefabManager pm = new PrefabManager()
            {
                name = name,
                root = Clone(handle),
            };
            pm.root.transform.SetParent(MazeMap.maze.prefabClone.transform);
            pm.modelCount = pm.root.GetComponent<PrefabSettings>();
            if (!addSides || (int)pm.modelCount.side > (int)Sides.Z_Negative || !pm.modelCount.rotatable)
            {
                pm.side = new GameObject[] { pm.root };
                  pm.prefabType = PrefabType.Mono;
            }
            else
            {
                pm.side = new GameObject[4];
                for (int i = 0; i < 4; i++)
                    if ((int)pm.modelCount.side == i)
                        pm.side[i] = pm.root.transform.GetChild(0).gameObject;
                    else
                    {
                        pm.side[i] = Clone(pm.root.transform.GetChild(0).gameObject, pm.root.transform);
                        pm.side[i].transform.Rotate(Vector2.up, -90 * (i - (int)pm.modelCount.side), Space.World);
                    }
                pm.prefabType = PrefabType.Ordered;
            }
            onCreation = false;
            return pm;
        }
        public static PrefabManager CreateRandom(string name, GameObject handle)
        {
            PrefabSettings mc = handle.GetComponent<PrefabSettings>();
            if (mc.paired)
                return CreateQuadro(name, handle);
            onCreation = true;
            PrefabManager pm = new PrefabManager()
            {
                name = name,
                root = Clone(handle)
            };
            pm.root.transform.SetParent(MazeMap.maze.prefabClone.transform);
            pm.modelCount = pm.root.GetComponent<PrefabSettings>();

            pm.side = new GameObject[4];
            //    childIndex = new int[type == PrefabType.Mono ? 1 : 4];
            //     for (int i = 0; i < side.Length; i++) childIndex[i] = -1;

            pm.prefabType = PrefabType.Random;
            int cc = pm.root.transform.childCount;
            pm.side = new GameObject[cc];
            for (int i = 0; i < cc; i++)
                pm.side[i] = pm.root.transform.GetChild(i).gameObject;

            onCreation = false;
            return pm;
        }
        public static PrefabManager CreateQuadro(string name, GameObject handle)
        {
            onCreation = true;
            if ((int)handle.GetComponent<PrefabSettings>().side <= (int)Sides.Z_Negative) return CreateMono(name, handle, true);
            PrefabManager pm = new PrefabManager()
            {
                name = name,
                root = Clone(handle)
            };
            pm.root.transform.SetParent(MazeMap.maze.prefabClone.transform);
            pm.modelCount = pm.root.GetComponent<PrefabSettings>();
            pm.side = new GameObject[4];
            pm.prefabType = PrefabType.Ordered;
            int ncc = 0, cc = pm.root.transform.childCount;
            Transform[] children = new Transform[cc];
            int[] indices = new int[cc];
            for (int i = 0; i < cc; i++)
            {
                Transform child = pm.root.transform.GetChild(i);
                if ("0123456789".IndexOf(child.name[^1]) >= 0)
                {
                    string last = "";
                    for (int j = child.name.Length - 1; j >= 0; j--)
                        if ("0123456789".IndexOf(child.name[j]) >= 0) last = child.name[j] + last;
                        else break;
                    if (last != "")
                    {
                        indices[ncc] = int.Parse(last);
                        children[ncc++] = child;
                    }
                }
            }
            for (int i = 0; i < ncc - 1; i++)
                for (int j = i + 1; j < ncc; j++)
                    if (indices[i] > indices[j])
                    {
                        (indices[i], indices[j]) = (indices[j], indices[i]);
                        (children[i], children[j]) = (children[j], children[i]);
                    }

            for (int i = 0; i < 4; i++)
            {
                pm.side[i] = children[order[i]].gameObject;
                //       Debug.Log("sorted " + pm.side[i].name);
            }
            onCreation = false;
            return pm;
        }

        public static void SetPool(List<PrefabManager> list)
        {
            int total = 0;
            //Debug.Log("pools :" + list.Count);
            for (int i = 0; i < list.Count; i++) { list[i].pool = total += list[i].modelCount == null ? 1 : list[i].modelCount.count; }
            for (int i = 0; i < list.Count; i++) { list[i].pool /= total; }

        }

        public static bool Check(bool c, Selector s)
        {
            if (c)
                return s == Selector.Always || s == Selector.Both;
            else
                return s == Selector.Never || s == Selector.Both;
        }
        public static PrefabManager GetPool(List<PrefabManager> list, CloneResult cr, bool sided)
        {
            float r = UnityEngine.Random.value;
            List<PrefabManager> l = new List<PrefabManager>();
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].modelCount.byCount)
                    if (!sided || list[i].prefabType != PrefabType.Mono || (int)list[i].modelCount.side == cr.sideIndex)
                        if (Check(cr.isEdge, list[i].modelCount.edge))
                            //                       if (Check(cr.isVoid, list[i].modelCount.voidType))
                            if (Check(cr.isCorner, list[i].modelCount.corner))
                                l.Add(list[i]);


            }
            SetPool(l);
            cr.prefabIndex = l.Count - 1;
            for (int i = 0; i < l.Count - 1; i++)
                if (r < l[i].pool)
                { cr.prefabIndex = i; break; }
            return l[cr.prefabIndex];
        }
        /// <summary>
        /// For floors
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="list"></param>
        /// <param name="prefabIndex"></param>
        /// <returns></returns>
        public static GameObject RandomNoIndex(Vector3 p, List<PrefabManager> list, CloneResult cr, float scale)
        {
            PrefabManager l = GetPool(list, cr, true);
            cr.alwaysVisible = l.modelCount == null ? false : l.modelCount.alwaysVisible;
            //        if (list[prefabIndex].name == "columns") Debug.Log("selected pool: " + prefabIndex + " " + list[prefabIndex].side[0].name);
            //     prefabIndex = list[prefabIndex].allIndex;
            cr.sideIndex = UnityEngine.Random.Range(0, l.side.Length);
            GameObject go = cr.gameObject = Clone(l.side[cr.sideIndex], MazeMap.maze.levelRoot[cr.level].transform, cr.level, scale);
            go.transform.position = p;
            return go;
        }

        public static GameObject RandomIndexed(MazeCell cell, List<PrefabManager> list, CloneResult cr, float scale, bool rotatable = true)
        {
            GameObject go = RandomIndexed(cell.position, list, cr, scale, rotatable);
            //      Debug.Log("wall= " + go.name + " " + cell.position.y);
            return go;

        }
        /// <summary>
        /// for columns or cells
        /// </summary>
        /// <param name="p"></param>
        /// <param name="list"></param>
        /// <param name="index"></param>
        /// <param name="prefabIndex"></param>
        /// <returns></returns>
        public static GameObject RandomIndexed(Vector3 p, List<PrefabManager> list, CloneResult cr, float scale, bool rotatable = true)
        {
            PrefabManager l = list.Count == 1 ? list[0] : GetPool(list, cr, true);
            cr.prefabIndex = list.IndexOf(l);
            cr.alwaysVisible = l.modelCount.alwaysVisible;
            //        if (list[prefabIndex].name == "columns") Debug.Log("selected pool: " + prefabIndex + " " + list[prefabIndex].side[0].name);
            //     prefabIndex = list[prefabIndex].allIndex;
            //      Debug.Log("mcoun:: " + l.side[cr.sideIndex].name + (l.modelCount == null ? " null" : " " + l.modelCount.byCount));

            int index;
            GameObject go;
            if (l.side.Length == 1)
            {
                index = 0;
                go = cr.gameObject = Clone(l.side[index], MazeMap.maze.levelRoot[cr.level].transform, cr.level, scale);
                go.transform.position = p;
            }
            else
            {
                index = rotatable && l.modelCount.switchSides ? (cr.sideIndex + UnityEngine.Random.Range(0, 2) * 2) % 4 : cr.sideIndex;
                go = cr.gameObject = Clone(l.side[index], MazeMap.maze.levelRoot[cr.level].transform, cr.level, scale);
                if (index == cr.sideIndex)
                    go.transform.position = p;
                else
                {
                    go.name += $" <{index}> ";
                    Vector2 d = MazeCell.Delta(cr.sideIndex);
                    go.transform.position = new Vector3(p.x + d.x * MazeMap.maze.size, p.y, p.z + d.y * MazeMap.maze.size);
                }
            }
            return go;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cell"></param>
        /// <returns></returns>

        public static GameObject RandomFloor(MazeCell cell, List<PrefabManager> list, CloneResult cr, float scale)
        {
            int index = 1;
            int rot = 0;
            int n = 0;
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    if (cell.neighbors[i, j] != null)
                        n += i * 4 * (j + 1) + (1 - i) * (j + 1);
            switch (n)
            {
                case 1: break;
                case 2: rot = 2; break;
                case 3: index = 5; break;
                case 4: rot = 1; break;
                case 5: index = 2; break;
                case 6: index = 2; rot = 1; break;
                case 7: index = 3; rot = 1; break;
                case 8: rot = -1; break;
                case 9: index = 2; rot = -1; break;
                case 10: index = 2; rot = 2; break;
                case 11: index = 3; rot = -1; break;
                case 12: index = 5; rot = 1; break;
                case 13: index = 3; break;
                case 14: index = 3; rot = 2; break;
                default: index = 4; break;
            }
            rot = -rot;
            index--;
            cr.sideIndex = index;
            GameObject go;
            if (list.Count == 1)
            {
                go = cr.gameObject = Clone(list[0].side[index], MazeMap.maze.levelRoot[cr.level].transform, cr.level, scale);
                cr.alwaysVisible = list[0].modelCount.alwaysVisible;
            }
            else
                go = RandomIndexed(cell.position, list, cr, scale, false);
            go.transform.position = cell.position;
            go.transform.Rotate(Vector2.up, rot * 90, Space.World);
            return go;
        }
        public static GameObject Clone(GameObject original, Transform newParent = null, int level = -1, float scale = 1f)
        {
            GameObject clone = GameObject.Instantiate(original);
            clone.transform.parent = newParent;
            clone.transform.rotation = original.transform.rotation;
            clone.transform.localScale = original.transform.localScale * scale;
            //    Sticker[] st = original.GetComponentsInChildren<Sticker>();
            //    Debug.Log("uv orig "+original.name+" " + st.Length);
            Mesh m;
            Vector2[] uv;

            CloneRecursive(original.transform, clone.transform, level);

            return clone;
        }
        public static void CloneRecursive(Transform original, Transform clone, int level)
        {
            clone.gameObject.layer = original.gameObject.layer;
            MeshRenderer ro = original.GetComponent<MeshRenderer>();
            MeshRenderer rc = clone.GetComponent<MeshRenderer>();
               if (ro != null)
            {
                rc.lightmapIndex = ro.lightmapIndex;
                rc.lightmapScaleOffset = ro.lightmapScaleOffset;
              }

            int cc = original.transform.childCount;
            for (int i = 0; i < cc; i++)
                CloneRecursive(original.GetChild(i), clone.GetChild(i), level);
        }
    }
}
