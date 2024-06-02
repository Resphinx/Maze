using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    /// <summary>
    /// This class contains the post-instantiation characteristics and some criteria for it.
    /// </summary>
    public class CloneResult
    {
        /// <summary>
        /// The created game object
        /// </summary>
        public GameObject gameObject;
        /// <summary>
        /// The side on which the game object should be created.
        /// </summary>
        public int sideIndex = 0;
        /// <summary>
        /// The prefab index that the game object is instantied by.
        /// </summary>
        public int prefabIndex;
        /// <summary>
        /// If only the prefabs with edge <see cref="Selector"/> should be considered
        /// </summary>
        public bool isEdge = false;
        /// <summary>
        /// If only the prefabs with corner <see cref="Selector"/> should be considered
        /// </summary>
        public bool isCorner = false;
        /// If only the prefabs with void <see cref="Selector"/> should be considered
        public bool isVoid = false;
        /// <summary>
        /// The level on which the game object is instantiated (this is used for naming the game object)
        /// </summary>
        public int level = 0;
        /// <summary>
        /// If the created game object has a prefab with <see cref="PrefabSettings.alwaysVisible"/> as true.
        /// </summary>
        public bool alwaysVisible = false;
    }
    /// <summary>
    /// This class is repsonsible for preparing the prefabs and instantiating game objects and maze elements based on them.
    /// </summary>
    public class PrefabManager
    {
        /// <summary>
        /// The maze where the elements should be created in.
        /// </summary>
        public MazeMap maze;
        /// <summary>
        /// The name of the prefab (not important).
        /// </summary>
        public string name;
        /// <summary>
        /// The root object where the different versions (or orientations) of the prefabs belong to.
        /// </summary>
        public GameObject root;
        /// <summary>
        /// The prefab instances for different sides.
        /// </summary>
        public GameObject[] side;
        /// <summary>
        /// The prefab setting for this prefab manager.
        /// </summary>
        public PrefabSettings settings;
        /// <summary>
        /// See <see cref="PrefabSettings.byCount"/> and <see cref="Counting.byCount"/>. This value will be final after localization.
        /// </summary>
        public bool byCount;
        /// <summary>
        /// The pool initially set as an integer by <see cref="PrefabSettings.byCount"/> and <see cref="Counting.byCount"/> (after localization).
        /// </summary>
        public int initialPool;
        /// <summary>
        /// For each set of perfab managers, their pool is calculated based on their <see cref="initialPool"/> values.
        /// </summary>
        public float pool = 0;
        /// <summary>
        /// See <see cref="PrefabSettings.positions"/> and <see cref="LocalSettings.placements"/>. This value will be final after localization.
        /// </summary>
        public Vector3Int[] positions;
        public int[] directions;
        /// <summary>
        /// The prefab type: <c>Ordered</c>: the sides are important; <c>Random</c>: the side order is not important; and <c>Mono</c> only one version exists that may or not be sided.
        /// </summary>
        public enum PrefabType { Ordered, Random, Mono }
        /// <summary>
        /// The prefab type of this prefab manager.
        /// </summary>
        public PrefabType prefabType;
        /// <summary>
        /// The order correspondence when imported from Blender (due to rotations).
        /// </summary>
        public static int[] order = new int[] { 2, 3, 0, 1 };
        /// <summary>
        /// (deprecated) If this is true, certain operations will not trigger. 
        /// </summary>
        public static bool onCreation = false;
        /// <summary>
        /// Creates a prefab manager based only one orientation of an element. This is useful for columns or otehr elements that have limited initial directions (see <see cref="PrefabSettings.side"/>).
        /// </summary>
        /// <param name="maze">The destination maze</param>
        /// <param name="name">Name of the prefab manager</param>
        /// <param name="handle">The parent object of the intended instantiable objects</param>
        /// <param name="addSides">If this is true, it will create other missing sides and the prefab type will be set to <c>Ordered</c></param>
        /// <returns></returns>
        public static PrefabManager CreateMono(MazeMap maze, string name, GameObject handle, bool addSides = false)
        {
            onCreation = true;
            PrefabManager pm = new PrefabManager()
            {
                name = name,
                root = Clone(handle),
                maze = maze,
            };
            pm.root.transform.SetParent(maze.prefabClone.transform);
            pm.SetSettings(handle);
            if (!addSides || (int)pm.settings.side > (int)Sides.Z_Negative || !pm.settings.rotatable)
            {
                pm.side = new GameObject[] { pm.root };
                pm.prefabType = PrefabType.Mono;
            }
            else
            {
                pm.side = new GameObject[4];
                for (int i = 0; i < 4; i++)
                    if ((int)pm.settings.side == i)
                        pm.side[i] = pm.root.transform.GetChild(0).gameObject;
                    else
                    {
                        pm.side[i] = Clone(pm.root.transform.GetChild(0).gameObject, pm.root.transform);
                        pm.side[i].transform.Rotate(Vector2.up, -90 * (i - (int)pm.settings.side), Space.World);
                    }
                pm.prefabType = PrefabType.Ordered;
            }
            onCreation = false;
            return pm;
        }
        /// <summary>
        /// Creates a prefab manager based on non-ordered elements. This is useful for floors which follow a different order than normal cell sides .
        /// </summary>
        /// <param name="maze">The destination maze</param>
        /// <param name="name">Name of the prefab manager</param>
        /// <param name="handle">The parent object of the intended instantiable objects</param>
        /// <returns></returns>
        public static PrefabManager CreateRandom(MazeMap maze, string name, GameObject handle)
        {
            PrefabSettings mc = handle.GetComponent<PrefabSettings>();
            if (mc.Bundled)
                return CreateOrdered(maze, name, handle);
            onCreation = true;
            PrefabManager pm = new PrefabManager()
            {
                name = name,
                root = Clone(handle),
                maze = maze,
            };
            pm.root.transform.SetParent(maze.prefabClone.transform);
            pm.SetSettings(handle);
            pm.side = new GameObject[4];

            pm.prefabType = PrefabType.Random;
            int cc = pm.root.transform.childCount;
            pm.side = new GameObject[cc];
            for (int i = 0; i < cc; i++)
                pm.side[i] = pm.root.transform.GetChild(i).gameObject;

            onCreation = false;
            return pm;
        }
        /// <summary>
        /// Creates a prefab manager based on ordered elements with four possible orientations. This is useful for walls and items.
        /// </summary>
        /// <param name="maze">The destination maze</param>
        /// <param name="name">Name of the prefab manager</param>
        /// <param name="handle">The parent object of the intended instantiable objects</param>
        /// <returns></returns>
        public static PrefabManager CreateOrdered(MazeMap maze, string name, GameObject handle)
        {
            onCreation = true;
            if ((int)handle.GetComponent<PrefabSettings>().side <= (int)Sides.Z_Negative) return CreateMono(maze, name, handle, true);
            PrefabManager pm = new PrefabManager()
            {
                name = name,
                root = Clone(handle),
                maze = maze,
            };
            pm.root.transform.SetParent(maze.prefabClone.transform);
            pm.SetSettings(handle);
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
        /// <summary>
        /// Assigns the prefab setting and localizes it.
        /// </summary>
        /// <param name="handle"></param>
        public void SetSettings(GameObject handle)
        {
            settings = root.GetComponent<PrefabSettings>();
     //       PrefabSettings ps = handle.GetComponent<PrefabSettings>();
        //    settings.local = ps.local;
            ApplyLocal();
        }
        /// <summary>
        /// Applies the <see cref="LocalSettings"/> if exists.
        /// </summary>
        public void ApplyLocal()
        {
            if (settings.local == null)
            {
                byCount = settings.byCount;
                initialPool = settings.pool;

                positions = new Vector3Int[settings.positions.Length];
                directions = new int[settings.positions.Length];
                for (int i = 0; i < settings.positions.Length; i++)
                {
                    Placement p = settings.positions[i];
                    positions[i] = p.Vector;
                    directions[i] = p.d;
                }
            }
            else
            {
                Debug.Log("sorted " + settings.name); 
                if (settings.local.counting.asIs)
                {
                    byCount = settings.byCount;
                    initialPool = settings.pool;
                }
                else
                {
                    byCount = settings.local.counting.byCount;
                    initialPool = settings.local.counting.pool;
                }
                if (settings.local.placements.Length > 0)
                {
                    positions = new Vector3Int[settings.local.placements.Length];
                    directions = new int[settings.local.placements.Length];
                    for (int i = 0; i < settings.local.placements.Length; i++)
                    {
                        Placement p = settings.local.placements[i];
                        positions[i] = p.Vector;
                        directions[i] = p.d;
                    }
                }
            }
        }
        /// <summary>
        /// Sets the <see cref="pool"/> of individual prefabs in a list based on their <see cref="initialPool"/>'s weight in the aggregation of initial pools.
        /// </summary>
        /// <param name="list"></param>
        public static void SetPool(List<PrefabManager> list)
        {
            int total = 0;
            //Debug.Log("pools :" + list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].pool = total += list[i].initialPool;
            }
            for (int i = 0; i < list.Count; i++)
            { list[i].pool /= total; }

        }
        /// <summary>
        /// Check if the value a <see cref="Selector"/> matches a condition.
        /// </summary>
        /// <param name="c">The condition</param>
        /// <param name="s">The selector</param>
        /// <returns></returns>
        public static bool Check(bool c, Selector s)
        {
            if (c)
                return s == Selector.Always || s == Selector.Both;
            else
                return s == Selector.Never || s == Selector.Both;
        }
        /// <summary>
        /// Selects a random prefab manager from a list based on the list's pools and certain conditions.
        /// </summary>
        /// <param name="list">The list of possible prefabs</param>
        /// <param name="cr">Most conditions; also the index of the prefab in the list is set to <see cref="CloneResult.prefabIndex"/></param>
        /// <param name="sided">If the sides (<see cref="CloneResult.sideIndex"/>) should be considered</param>
        /// <returns>Returns the selected prefab (it is never null).</returns>
        public static PrefabManager GetPool(List<PrefabManager> list, CloneResult cr, bool sided)
        {
            float r = UnityEngine.Random.value;
            List<PrefabManager> l = new List<PrefabManager>();

            for (int i = 0; i < list.Count; i++)
            {
                bool byCount = list[i].byCount;

                if (!byCount)
                    if (!sided || list[i].prefabType != PrefabType.Mono || (int)list[i].settings.side == cr.sideIndex)
                        if (Check(cr.isEdge, list[i].settings.edge))
                            //                       if (Check(cr.isVoid, list[i].modelCount.voidType))
                            if (Check(cr.isCorner, list[i].settings.corner))
                                l.Add(list[i]);


            }
            SetPool(l);
            cr.prefabIndex = l.Count - 1;
            for (int i = 0; i < l.Count - 1; i++)
                if (r < l[i].pool)
                {
                    cr.prefabIndex = i;
                    break;
                }
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
            cr.alwaysVisible = l.settings.alwaysVisible;
            //        if (list[prefabIndex].name == "columns") Debug.Log("selected pool: " + prefabIndex + " " + list[prefabIndex].side[0].name);
            //     prefabIndex = list[prefabIndex].allIndex;
            cr.sideIndex = UnityEngine.Random.Range(0, l.side.Length);
            GameObject go = cr.gameObject = Clone(l.side[cr.sideIndex], l.maze.levelRoot[cr.level].transform, cr.level, scale);
            go.transform.localPosition = p;
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
            cr.alwaysVisible = l.settings.alwaysVisible;
            //        if (list[prefabIndex].name == "columns") Debug.Log("selected pool: " + prefabIndex + " " + list[prefabIndex].side[0].name);
            //     prefabIndex = list[prefabIndex].allIndex;
            //      Debug.Log("mcoun:: " + l.side[cr.sideIndex].name + (l.modelCount == null ? " null" : " " + l.modelCount.byCount));

            int index;
            GameObject go;
            Vector3 offset = Vector3.zero;
            Vector2Int d;

            if (l.side.Length == 1)
            {
                go = cr.gameObject = Clone(l.side[0], l.maze.levelRoot[cr.level].transform, cr.level, scale);
                if (l.settings.centerType == CenterType.SelfCenter)
                {
                    d = MazeCell.Delta((int)l.settings.side);
                    offset = new Vector3(0.5f * d.x * l.maze.size, 0, 0.5f * d.y * l.maze.size);

                }
            }
            else
            {
                index = rotatable && l.settings.switchSides ? (cr.sideIndex + UnityEngine.Random.Range(0, 2) * 2) % 4 : cr.sideIndex;
                go = cr.gameObject = Clone(l.side[index], l.maze.levelRoot[cr.level].transform, cr.level, scale);
                if (index == cr.sideIndex)
                {
                    if (l.settings.centerType == CenterType.SelfCenter)
                    {
                        d = MazeCell.Delta(index);
                        offset = new Vector3(0.5f * d.x * l.maze.size, 0, 0.5f * d.y * l.maze.size);
                    }
                }
                else
                {
                    d = MazeCell.Delta(cr.sideIndex);
                    if (l.settings.centerType == CenterType.SelfCenter)
                        offset = new Vector3(0.5f * d.x * l.maze.size, 0, 0.5f * d.y * l.maze.size);
                    else
                    {
                        //          go.transform.localPosition = p + offset; 
                        go.name += $" <{index}> ";
                        d = MazeCell.Delta(cr.sideIndex);
                        offset = new Vector3(d.x * l.maze.size, 0, d.y * l.maze.size);
                    }
                    Debug.Log(go.name + " " + cr.sideIndex + "|" + index + " " + d.ToString());
                }
            }
            go.transform.localPosition = p + offset;
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
                go = cr.gameObject = Clone(list[0].side[index], list[0].maze.levelRoot[cr.level].transform, cr.level, scale);
                cr.alwaysVisible = list[0].settings.alwaysVisible;
            }
            else
                go = RandomIndexed(cell.position, list, cr, scale, false);
            go.transform.localPosition = cell.position;
            go.transform.Rotate(list[0].maze.root.transform.up, rot * 90, Space.World);
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
