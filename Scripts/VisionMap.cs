using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    public enum VisionItemType { Floor, Column, Opaque, Transparent, Open }
    public enum VisibilityAction { OnlyDisappear, OnlyAppear, BothActions }
    public class LevelVision
    {
        /// <summary>
        /// Active maze
        /// </summary>
        MazeMap maze;
        /// <summary>
        /// All structural objects in this level (items are childrent to cell floors)
        /// </summary>
        public List<GameObject> all = new List<GameObject>();
        /// <summary>
        /// The last active state of all objects. This is used to minimise calling activeSelf and SetActive.
        /// </summary>
        public bool[] lastState;
         /// <summary>
        /// arrays of structural objects
        /// </summary>
        int[,] wallV, wallH, openH, openV, seeH, seeV, floor, col;
        bool[,] noneH, noneV;
        /// <summary>
        ///  The flattenend coordinates of columns
        /// </summary>
        Vector2[,] points;
        public bool levelActive = true;
        /// <summary>
        /// The level of this map
        /// </summary>          
        public int level;
        public CellBundle[] bundles;
        public int[] bundleLocalIndex;
        public LevelVision(MazeMap m, int level)
        {
            this.level = level;
            maze = m;
            wallV = new int[maze.cols + 1, maze.rows];
            wallH = new int[maze.cols, maze.rows + 1];
            openV = new int[maze.cols + 1, maze.rows];
            openH = new int[maze.cols, maze.rows + 1];
            seeV = new int[maze.cols + 1, maze.rows];
            seeH = new int[maze.cols, maze.rows + 1];
            noneH = new bool[maze.cols, maze.rows + 1];
            noneV = new bool[maze.cols + 1, maze.rows];
            floor = new int[maze.cols, maze.rows];
            col = new int[maze.cols + 1, maze.rows + 1];
            points = new Vector2[maze.cols + 1, maze.rows + 1];
            //      intersected = new int[maze.cols + 1, maze.rows + 1];
            for (int i = 0; i <= maze.cols; i++)
                for (int j = 0; j <= maze.rows; j++)
                {
                    col[i, j] = -1;
                    if (i < maze.cols) { wallH[i, j] = seeH[i, j] = openH[i, j] = -1; noneH[i, j] = false; }
                    if (j < maze.rows) { wallV[i, j] = seeV[i, j] = openV[i, j] = -1; noneV[i, j] = false; }

                    if (i < maze.cols && j < maze.rows) floor[i, j] = -1;
                    points[i, j] = new Vector2(i - 0.5f, j - 0.5f);
                    //         intersected[i, j] = -1;
                }
        }
        static Vector2[][] povs = new Vector2[][]
         {
            Points(0),
            Points(1),
            Points(2),
            Points(3),
         };
        const int PointCount = 10;
        static Vector2[] Points(int d)
        {
            Vector2 perp = d % 2 == 0 ? Vector2.up : Vector2.right;
            float x = d == 0 ? 1 : 0;
            float y = d == 1 ? 1 : 0;
            float dp = 1f / (PointCount + 1);
            Vector2 o = new Vector2(x, y);
            Vector2[] ps = new Vector2[PointCount];
            for (int i = 0; i < PointCount; i++)
            {
                ps[i] = o + dp * (i + 1) * perp;
            }
            return ps;
        }

        const int AngleStepCount = 20;
        static Vector2[][] rays = new Vector2[][]
        {
            Rays(0),
            Rays(1),
            Rays(2),
            Rays(3),
        };
        static Vector2[] Rays(int d)
        {
            float da = Mathf.PI / (AngleStepCount + 2);
            float a0 = (d - 1) * Mathf.PI / 2 + da;
            Vector2[] vs = new Vector2[AngleStepCount + 1];
            for (int i = 0; i <= AngleStepCount; i++)
            {
                vs[i] = new Vector2(Mathf.Cos(a0 + i * da), Mathf.Sin(a0 + i * da));
            }
            return vs;
        }
        public static byte Min(byte a, byte b) { return a < b ? a : b; }
        /// <summary>
        /// Adds a structural item to this vision level. 
        /// </summary>
        /// <param name="g">The game object representing the item.</param>
        /// <param name="x">The x position of the game object.</param>
        /// <param name="y">The y position of the game object</param>
        /// <param name="type">Type of the object.</param>
        /// <param name="side">The side of the item, not applicable for floors and columns</param>
        public void AddItem(GameObject g, int x, int y, VisionItemType type, int side = 0)
        {
            int[,] a = null;
            switch (type)
            {
                case VisionItemType.Floor: floor[x, y] = all.Count; all.Add(g); break;
                case VisionItemType.Column: col[x, y] = all.Count; all.Add(g); break;
                case VisionItemType.Opaque: a = side % 2 == 0 ? wallV : wallH; break;
                case VisionItemType.Open: a = side % 2 == 0 ? openV : openH; break;
                case VisionItemType.Transparent: a = side % 2 == 0 ? seeV : seeH; break;
            }
            if (a != null)
            {
                int i = side == 0 ? x + 1 : x;
                int j = side == 1 ? y + 1 : y;
                a[i, j] = all.Count;
                all.Add(g);
            }
        }
        public void AddItem(int x, int y, int side = 0)
        {
            int i = side == 0 ? x + 1 : x;
            int j = side == 1 ? y + 1 : y;
            if (side % 2 == 0) noneV[i, j] = true;
            else noneH[i, j] = true;

        }
        public void BundlesOnLevel()
        {
            List<CellBundle> bs = new List<CellBundle>();
            bundleLocalIndex = new int[maze.bundles.Count];
            for (int i = 0; i < bundleLocalIndex.Length; i++) bundleLocalIndex[i] = -1;
            for (int i = 0; i < maze.bundles.Count; i++)
            {
                CellBundle b = maze.bundles[i];
                if (b.height > 1)
                    if ((b.handle.z == level || b.levels[1] == level) && b.levels[0] != b.levels[1])
                    {
                        bundleLocalIndex[i] = bs.Count;
                        bs.Add(b);
                    }
            }
            bundles = bs.ToArray();
            for (int i = 0; i < maze.cols; i++)
                for (int j = 0; j < maze.rows; j++)
                    if (maze.cells[i, j, level] != null)
                        maze.cells[i, j, level].InitializeVisibility(all.Count, bundles.Length);

        }
        /// <summary>
        /// Generates the vision map for a level
        /// </summary>
        public void Vision(bool raycast, byte growOffset = 0)
        {
            BundlesOnLevel();
            for (int j = 0; j < maze.rows; j++)
                for (int i = 0; i < maze.cols; i++)
                {
                    VisionMap.currentCellIndex = j * maze.cols + i;
                    MazeCell mc = maze.cells[i, j, level];
                    bool vis = false;
                    if (mc.situation != BundleSituation.Hanging && mc.situation != BundleSituation.Void)
                        if (mc.bundle == null) vis = true;
                        else if (mc.situation != BundleSituation.Middle) vis = true;
                        else if (mc.bundle.levels[0] == mc.bundle.levels[1]) vis = true;
                    if (vis)
                    {
                        for (int k = 0; k < 2; k++)
                            if (raycast) VisionRayCast(mc, k == 1, growOffset);
                            else VisionAround(mc, k == 1, growOffset);
                    }
                }
            for (int i = 0; i < bundles.Length; i++)
                bundles[i].SetOffsets(this);
            for (int j = 0; j < maze.rows; j++)
                for (int i = 0; i < maze.cols; i++)
                    VisibleBundleOnLevel(maze.cells[i, j, level]);

        }

        /// <summary>
        /// The points on all sides from where rays are cast for vision mapping. 
        /// </summary>

        byte FloorVisibility(MazeCell cell, int x, int y, bool transparency)
        {
            int index = transparency ? 1 : 0;
            byte v = 255;
            for (int i = 0; i < 4; i++)
            {
                int x1 = i == 0 ? x + 1 : x;
                int y1 = i == 1 ? y + 1 : y;
                if (i % 2 == 0)
                {
                    if (openV[x1, y1] >= 0) v = Min(v, cell.offset[openV[x1, y1], index]);
                    else if (seeV[x1, y1] >= 0 && transparency) v = Min(v, cell.offset[seeV[x1, y1], index]);
                }
                else
                {
                    if (openH[x1, y1] >= 0) v = Min(v, cell.offset[openH[x1, y1], index]);
                    else if (seeH[x1, y1] >= 0 && transparency) v = Min(v, cell.offset[seeH[x1, y1], index]);
                }
            }
            return v;
        }

        void VisibleBundleOnLevel(MazeCell cell)
        {
            CellBundle b;
            bool[] bs = new bool[bundles.Length];
            for (int i = 0; i < maze.cols; i++)
                for (int j = 0; j < maze.rows; j++)
                    if (maze.cells[i, j, level] != null)
                    {
                        if ((b = maze.cells[i, j, level].bundle) != null)
                        {
                            int index = bundleLocalIndex[b.index];
                            if (b != cell.bundle && b.height > 1)
                            {
                                if (floor[i, j] >= 0)
                                {
                                    if (!bs[index])
                                    {
                                        bs[index] = true;
                                        cell.bundleOffset[index, 0] = cell.offset[floor[i, j], 0];
                                        cell.bundleOffset[index, 1] = cell.offset[floor[i, j], 1];
                                        cell.toHandleRow[index] = b.levels[0] == level;
                                    }
                                    else
                                    {
                                        cell.bundleOffset[index, 0] = Min(cell.bundleOffset[index, 0], cell.offset[floor[i, j], 0]);
                                        cell.bundleOffset[index, 1] = Min(cell.bundleOffset[index, 1], cell.offset[floor[i, j], 1]);
                                    }
                                }
                            }
                            else if (b == cell.bundle && b.height > 1)
                            {
                                cell.bundleOffset[index, 0] = 0;
                                cell.bundleOffset[index, 1] = 0;
                            }
                        }
                    }
        }
        void VisionRayCast(MazeCell cell, bool transparency, int maxOffset)
        {
            int index = transparency ? 1 : 0;

            if (floor[cell.x, cell.y] >= 0) cell.offset[floor[cell.x, cell.y], index] = 0;

            for (int d = 0; d < 4; d++)
            {
                byte visibility = 1;
                int x = d == 0 ? cell.x + 1 : cell.x;
                int y = d == 1 ? cell.y + 1 : cell.y;
                if (d % 2 == 0)
                {
                    if (openV[x, y] >= 0 || (transparency && seeV[x, y] >= 0) || noneV[x, y]) visibility = 0;
                }
                else if (openH[x, y] >= 0 || (transparency && seeH[x, y] >= 0 || noneH[x, y])) visibility = 0;

                if (visibility <= maxOffset)
                    for (int i = 0; i < PointCount; i++)
                    {
                        Vector2 p = new Vector2(cell.x + povs[d][i].x, cell.y + povs[d][i].y);
                        for (int j = 0; j <= AngleStepCount; j++)
                            RayCast(cell, x, y, d, p, rays[d][j], visibility, maxOffset, transparency);
                    }
            }

            ShowCell(cell, cell, 0, index);

            for (int i = 0; i < maze.cols; i++)
                for (int j = 0; j < maze.rows; j++)
                    if (i != cell.x || j != cell.y)
                    {
                        byte v = FloorVisibility(cell, i, j, transparency);
                        if (floor[i, j] >= 0) cell.offset[floor[i, j], index] = v;

                        //TODO add transparency to pairs
                    }
            for (int i = 0; i < maze.cols; i++)
                for (int j = 0; j < maze.rows; j++)
                    if (i != cell.x || j != cell.y)
                        if (floor[i, j] >= 0)
                        {
                            byte v = cell.offset[floor[i, j], index];
                            ShowCell(cell, maze.cells[i, j, level], v, index);
                        }

        }

        void VisionAround(MazeCell cell, bool transparency, byte maxOffset)
        {
            int index = transparency ? 1 : 0;
            for (int i = cell.x - maxOffset; i <= cell.x + maxOffset; i++)
                for (int j = cell.y - maxOffset; j <= cell.y + maxOffset; j++)
                {
                    if (i < maze.cols && j < maze.rows && i >= 0 && j >= 0)
                    {
                        byte offset = (byte)Mathf.Max(Mathf.Abs(i - cell.x), Mathf.Abs(j - cell.y));
                        if (floor[i, j] >= 0) cell.offset[floor[i, j], index] = offset;
                    }
                }
            for (int i = cell.x - maxOffset; i <= cell.x + maxOffset; i++)
                for (int j = cell.y - maxOffset; j <= cell.y + maxOffset; j++)
                {
                    if (i < maze.cols && j < maze.rows && i >= 0 && j >= 0)
                    {
                        byte offset = (byte)Mathf.Max(Mathf.Abs(i - cell.x), Mathf.Abs(j - cell.y));
                        ShowCell(cell, maze.cells[i, j, level], offset, index);
                    }
                }
            if (floor[cell.x, cell.y] >= 0) cell.offset[floor[cell.x, cell.y], index] = 0;
            //        for (int i = 0; i < alwaysVisible.Count; i++) if (alwaysVisible[i]) cell.offset[i, index] = 0;
        }
        byte RayVisiblity(MazeCell cell, int i, int j, int[,] open, int[,] wall, int[,] see, byte ray, bool t)
        {
            byte visibility;
            int index = t ? 1 : 0;
            int k = -1;
            bool hitWall = false;
            if (open[i, j] >= 0) k = open[i, j];
            else if (see[i, j] >= 0 && t) k = see[i, j];
            else if (wall[i, j] >= 0) { k = wall[i, j]; hitWall = true; }
            if (k >= 0)
            {
                visibility = cell.offset[k, index];
                if (visibility == 0 && ray > 0) if (visibility > ray) cell.offset[k, index] = ray;
                if (visibility > 0 && ray == 0) cell.offset[k, index] = 0;
            }
            if (hitWall)
                ray++;
            return ray;
        }
        void ShowItem(MazeCell cell, int i, int j, int[,] open, int[,] wall, int[,] see, byte offset, int index)
        {
            byte vis;
            int w = -1;
            if (open == null) w = col[i, j];
            else if (open[i, j] >= 0) w = open[i, j];
            else if (see[i, j] >= 0 && index == 1) w = see[i, j];
            else if (wall[i, j] >= 0) w = wall[i, j];
            if (w >= 0) cell.offset[w, index] = Min(cell.offset[w, index], offset);
        }
        void ShowCell(MazeCell cell, MazeCell mc, byte offset, int index)
        {
            for (int m = 0; m < 4; m++)
            {
                Vector2Int v = mc.around[m + 4];
                ShowItem(cell, v.x, v.y, null, null, null, offset, index);
                v = mc.around[m];
                if (m % 2 == 0) ShowItem(cell, v.x, v.y, openH, wallH, seeH, offset, index);
                else ShowItem(cell, v.x, v.y, openV, wallV, seeV, offset, index);
            }

        }
        void RayCast(MazeCell cell, int x, int y, int d, Vector2 p, Vector2 ray, byte lastVis, int maxOffset, bool transparency)
        {
            float lastM = 0.03f;
            int i = x;
            int j = y;
            int potX, potY;
            int nextX = x, nextY = y;
            int dx = ray.x < 0 ? -1 : 1, dy = ray.y < 0 ? -1 : 1;
            float m, n, m0, m1, n0, n1;
            bool vertical = d % 2 == 0;
            while (true)
            {
                if (ray.x != 0 && ray.y != 0)
                {
                    m0 = (nextX - p.x) / ray.x;
                    m1 = ((nextX + dx) - p.x) / ray.x;
                    n0 = (nextY - p.y) / ray.y;
                    n1 = ((nextY + dy) - p.y) / ray.y;

                    if (m0 > lastM + 0.01) { m = m0; potX = nextX; } else { m = m1; potX = nextX + dx; }
                    if (n0 > lastM + 0.01) { n = n0; potY = nextY; } else { n = n1; potY = nextY + dy; }
                    if (m < n)
                    {
                        nextX += dx;
                        i = potX;
                        if (!vertical && ray.y < 0) j--;
                        vertical = true;
                        lastM = m;
                    }
                    else
                    {
                        nextY += dy;
                        j = potY;
                        if (vertical && ray.x < 0) i--;
                        vertical = false;
                        lastM = n;
                    }
                }
                else if (ray.x == 0)
                {
                    j += dy;
                    vertical = false;
                }
                else
                {
                    i += dx;
                    vertical = true;
                }
                //        bool breakLoop;
                if (vertical && (i < 0 || i > maze.cols || j < 0 || j >= maze.rows)) break;
                else if (!vertical && (i < 0 || i >= maze.cols || j < 0 || j > maze.rows)) break;
                else
                {
                    if (vertical) lastVis = RayVisiblity(cell, i, j, openV, wallV, seeV, lastVis, transparency);
                    else lastVis = RayVisiblity(cell, i, j, openH, wallH, seeH, lastVis, transparency);
                    if (lastVis > maxOffset) break;
                }
            }
        }
        public void Show(MazeCell cell, bool show, byte offset = 0)
        {
            bool active;
            int hid = 0;
            if (cell != null)
                for (int i = 0; i < all.Count; i++)
                {
                    byte v = cell.offset[i, maze.transparency ? 0 : 1];
                    if (v == 0) active = show;
                    else active = v <= offset && show;
                    if (show == active && lastState[i] != active) { all[i].SetActive(active); lastState[i] = active; if (!active) hid++; }
                }
        }

        public void Apply(MazeCell cell, byte offset, bool considerLevel = true)
        {
            string s = cell.ijk.ToString();
            //     bool[] bundleVisibility = new bool[maze.bundles.Count];
            int inv = 0, vis = 0, totVis = 0, byoffset = 0;
            bool active;
            if (cell.situation != BundleSituation.Middle)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    byte v = cell.offset[i, maze.transparency ? 0 : 1];
                    active = v <= offset;
                    byoffset += v < 3 ? 1 : 0;
                    if (lastState[i] != active) { all[i].SetActive(active); lastState[i] = active; inv += active ? 0 : 1; vis += active ? 1 : 0; }
                    totVis += active ? 1 : 0;
                }
                Debug.Log($"Cell changed to {s} (paired: {!considerLevel}), visible: {totVis} (appeared: {vis}), disappeared: {inv} of total: {all.Count} ");
                if (considerLevel)
                    ApplyOnBundles(cell, offset);
                if (considerLevel) maze.vision.SetAlwaysVisible(level);
            }
        }

        public void ShowAll(List<MazeCell> cell, int startIndex, int endIndex, byte offset = 0)
        {
            for (int i = 0; i < all.Count; i++)
            {
                bool active = false;
                byte v;
                for (int j = startIndex; j <= endIndex; j++)
                {
                    v = cell[j].offset[i, maze.transparency ? 0 : 1];
                    if (v <= offset) { active = true; break; }
                }
                if (active && !lastState[i]) { all[i].SetActive(active); lastState[i] = active; }
            }
        }
        public void ApplyAll(List<MazeCell> cells, int mainCount, byte offsetMain, byte otherOffsets)
        {
            int startIndex = 1, endIndex = Mathf.Min(cells.Count - 1, mainCount - 1);
            if (cells.Count > 0)
            {
                Apply(cells[0], offsetMain);
                if (endIndex > 0) ShowAll(cells, startIndex, endIndex, otherOffsets);
            }
            if (cells.Count > endIndex + 1)
                ShowAll(cells, endIndex + 1, cells.Count - 1, otherOffsets);
        }
        public void RemoveTransparency(MazeCell cell, byte offset = 0, bool considerLevel = true)
        {
            for (int i = 0; i < all.Count; i++)
            {
                byte op = cell.offset[i, 0];
                byte tr = cell.offset[i, 1];
                if (tr <= offset)
                {
                    if (op > offset)
                        if (lastState[i]) all[i].SetActive(lastState[i] = false);
                }
                else if (op <= offset)
                    if (!lastState[i]) all[i].SetActive(lastState[i] = true);
            }
            if (considerLevel && cell.bundle.index >= 0) RemoveTransparency(cell, offset, false);
        }

        void ApplyOnBundles(MazeCell cell, byte offset)
        {
            int t = maze.transparency ? 1 : 0;
            bool[] levelVisibility = new bool[maze.levelRoot.Length];
            levelVisibility[level] = true;
            bool[] bundleVisibility = new bool[bundles.Length];
            int z;
             for (int i = 0; i < bundles.Length; i++)
            {
                  if (cell.bundleOffset[i, t] <= offset)
                {
                    z = bundles[i].levels[0] == level ? bundles[i].levels[1] : bundles[i].levels[0];
                      if (levelVisibility[z])
                        bundles[i].Show(maze, z, offset, maze.transparency);
                    else
                    {
                        levelVisibility[z] = true;
                          if (!bundleVisibility[i])
                        {
                            bundles[i].Apply(maze, z, offset, maze.transparency);
                            bundleVisibility[i] = true;
                        }
                    }
                }
            }
            for (int i = 0; i < maze.levelRoot.Length; i++)
                if (maze.vision.levels[i].levelActive != levelVisibility[i])
                {
                      maze.levelRoot[i].SetActive(levelVisibility[i]);
                    maze.vision.levels[i].levelActive = levelVisibility[i];
                }
        }
    }
    public class VisionMap
    {
        public const int Floor = 1;
        public const int Column = 2;
        public const int Open = 3;
        public const int Transparent = 4;
        public const int Opaque = 5;

        public MazeMap maze;
        public List<VisiblePair> alwaysVisible = new List<VisiblePair>();
        public List<GameObject> grid = new List<GameObject>();
        public List<GameObject> others = new List<GameObject>();
        public LevelVision[] levels;
        public static int currentCalculatedLevel = 0, currentCellIndex = 0;
        public bool calculating;
        public VisionMap(MazeMap m)
        {
            maze = m;
            levels = new LevelVision[maze.levels];
            for (int i = 0; i < levels.Length; i++)
                levels[i] = new LevelVision(m, i);
        }
        public void AddItem(GameObject g, int i, int j, int k, VisionItemType type, int d = 0)
        {
            levels[k].AddItem(g, i, j, type, d);
            grid.Add(g);
        }
        public void AddItem(int i, int j, int k, int d = 0)
        {
            levels[k].AddItem(i, j, d);
        }
        public void AddItem(GameObject g)
        {
            others.Add(g);
        }
        public void SetLastStates()
        {
            for (int k = 0; k < maze.levels; k++)
            {
                levels[k].lastState = new bool[levels[k].all.Count];
                for (int i = 0; i < levels[k].all.Count; i++)
                {
                    levels[k].lastState[i] = levels[k].all[i].activeSelf;
                }
            }
        }
        public async void VisionAsync(bool rayCast, byte growOffset = 0)
        {
            Debug.Log("calculating in vision");
            await Task.Run(() =>
            {
                lock (maze)
                {
                    for (int k = 0; k < maze.levels; k++)
                    {
                        currentCalculatedLevel = k;
                        levels[k].Vision(rayCast, growOffset);
                    }
                    calculating = false;
                    Debug.Log("calculation op finished");
                }
            });
        }
        public void Vision(bool rayCast, byte growOffset = 0)
        {
            lock (maze)
            {
                for (int k = 0; k < maze.levels; k++)
                {
                    currentCalculatedLevel = k;
                    levels[k].Vision(rayCast, growOffset);
                }
                calculating = false;
                Debug.Log("calculation op finished");
            }
        }
        public void HideAll()
        {
            for (int i = 0; i < levels.Length; i++)
                for (int j = 0; j < levels[i].all.Count; j++)
                {
                    levels[i].all[i].SetActive(false);
                    levels[i].lastState[i] = false;
                }
        }
        public void AddAlwaysVisible(GameObject g, int l0, int l1)
        {
            alwaysVisible.Add(new VisiblePair(maze.root, g, l0, l1));
        }
        public void SetAlwaysVisible(int l)
        {
            foreach (VisiblePair vp in alwaysVisible)
                vp.SetLevel(l);
        }
    }

}
