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
        ///  List of objects that should always be active
        /// </summary>
        public List<bool> alwaysVisible = new List<bool>();
        /// <summary>
        /// arrays of structural objects
        /// </summary>
        int[,] wallV, wallH, openH, openV, seeH, seeV, floor, col;
        /// <summary>
        ///  The flattenend coordinates of columns
        /// </summary>
        Vector2[,] points;
        public bool levelActive = true;
        /// <summary>
        /// The level of this map
        /// </summary>          
        int level;

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
            floor = new int[maze.cols, maze.rows];
            col = new int[maze.cols + 1, maze.rows + 1];
            points = new Vector2[maze.cols + 1, maze.rows + 1];
            //      intersected = new int[maze.cols + 1, maze.rows + 1];
            for (int i = 0; i <= maze.cols; i++)
                for (int j = 0; j <= maze.rows; j++)
                {
                    col[i, j] = -1;
                    if (i < maze.cols) { wallH[i, j] = seeH[i, j] = openH[i, j] = -1; }
                    if (j < maze.rows) { wallV[i, j] = seeV[i, j] = openV[i, j] = -1; }

                    if (i < maze.cols && j < maze.rows) floor[i, j] = -1;
                    points[i, j] = new Vector2(i - 0.5f, j - 0.5f);
                    //         intersected[i, j] = -1;
                }
        }
        /// <summary>
        /// Adds a structural item to this vision level. 
        /// </summary>
        /// <param name="g">The game object representing the item.</param>
        /// <param name="x">The x position of the game object.</param>
        /// <param name="y">The y position of the game object</param>
        /// <param name="type">Type of the object.</param>
        /// <param name="alwaysVisible">Whether the item is always active. See <see cref="ModelCount.alwaysVisible"/></param>
        /// <param name="side">The side of the item, not applicable for floors and columns</param>
        public void AddItem(GameObject g, int x, int y, VisionItemType type, bool alwaysVisible, int side = 0)
        {
            int[,] a = null;
            int q = -1;
            switch (type)
            {
                case VisionItemType.Floor: floor[x, y] = all.Count; all.Add(g); break;
                case VisionItemType.Column: col[x, y] = all.Count; all.Add(g); break;
                case VisionItemType.Opaque: a = side % 2 == 0 ? wallV : wallH; q = 1; break;
                case VisionItemType.Open: a = side % 2 == 0 ? openV : openH; q = 0; break;
                case VisionItemType.Transparent: a = side % 2 == 0 ? seeV : seeH; q = 2; break;
            }
            if (a != null)
            {
                int i = side == 0 ? x + 1 : x;
                int j = side == 1 ? y + 1 : y;
                a[i, j] = all.Count;
                all.Add(g);
            }
            this.alwaysVisible.Add(alwaysVisible);
        }
        /// <summary>
        /// Generates the vision map for a level
        /// </summary>
        public void Vision(bool raycast, int growOffset = 0)
        {
            for (int j = 0; j < maze.rows; j++)
                for (int i = 0; i < maze.cols; i++)
                {
                    VisionMap.currentCellIndex = j * maze.cols + i;
                    if (maze.cells[i, j, level].situation != PairSituation.Undefined && maze.cells[i, j, level].situation != PairSituation.Void)
                    {
                        if (raycast) VisionRayCast(maze.cells[i, j, level], growOffset);
                        else VisionAround(maze.cells[i, j, level], growOffset);
                    }
                }
        }

        /// <summary>
        /// The points on all sides from where rays are cast for vision mapping. 
        /// </summary>
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
        bool GrowVisible(Visibility[] current, int x, int y, int grow)
        {
            for (int i = x - grow; i <= x + grow; i++)
                for (int j = y - grow; j <= y + grow; j++)
                    if (x != i || y != j)
                        if (i >= 0 && j >= 0 && i < maze.cols && j < maze.rows)
                            if (floor[i, j] >= 0)
                                if (current[floor[i, j]] == Visibility.Visible)
                                    return true;
            return false;
        }
        bool FloorVisibility(MazeCell cell, int x, int y)
        {
            for (int i = 0; i < 4; i++)
            {
                int x1 = i == 0 ? x + 1 : x;
                int y1 = i == 1 ? y + 1 : y;
                if (i % 2 == 0)
                {
                    if (openV[x1, y1] >= 0) { if (cell.visibility[openV[x1, y1]] != Visibility.Invisible) return true; }
                    else if (seeV[x1, y1] >= 0) { if (cell.visibility[seeV[x1, y1]] != Visibility.Invisible) return true; }
                }
                else
                {
                    if (openH[x1, y1] >= 0) { if (cell.visibility[openH[x1, y1]] != Visibility.Invisible) return true; }
                    else if (seeH[x1, y1] >= 0) { if (cell.visibility[seeH[x1, y1]] != Visibility.Invisible) return true; }
                }
            }
            return false;
        }

    
        void VisionRayCast(MazeCell cell, int growOffset = 0)
        {
            cell.visibility = new Visibility[all.Count];
            cell.visiblePair = new List<MazeCell>();
            for (int i = 0; i < cell.visibility.Length; i++) cell.visibility[i] = Visibility.Invisible;

            if (floor[cell.x, cell.y] >= 0) cell.visibility[floor[cell.x, cell.y]] = Visibility.Visible;

            if (cell.pair != null)
                if (cell.pair.z != level)
                    cell.visiblePair.Add(cell.pair);
            for (int d = 0; d < 4; d++)
            {
                int x = d == 0 ? cell.x + 1 : cell.x;
                int y = d == 1 ? cell.y + 1 : cell.y;
                if (d % 2 == 0) if (seeV[x, y] < 0 && openV[x, y] < 0) continue;
                if (d % 2 == 1) if (seeH[x, y] < 0 && openH[x, y] < 0) continue;

                for (int i = 0; i < PointCount; i++)
                {
                    Vector2 p = new Vector2(cell.x + povs[d][i].x, cell.y + povs[d][i].y);
                    for (int j = 0; j <= AngleStepCount; j++)
                        Vision(cell, x, y, d, p, rays[d][j]);
                }
            }
            Vis(cell, cell);
            for (int i = 0; i < maze.cols; i++)
                for (int j = 0; j < maze.rows; j++)
                    if (i != cell.x || j != cell.y)
                        if (FloorVisibility(cell, i, j))
                        {
                            if (floor[i, j] >= 0) cell.visibility[floor[i, j]] = Visibility.Visible;
                            if (maze.cells[i, j, level].pair != null)
                                if (maze.cells[i, j, level].pair.z != level) cell.visiblePair.Add(maze.cells[i, j, level].pair);
                        }

            for (int i = 0; i < all.Count; i++)
                if (alwaysVisible[i])
                    cell.visibility[i] = Visibility.Visible;

          
            Visibility[] current = new Visibility[all.Count];
            cell.visibility.CopyTo(current, 0);
            bool vis;
            for (int i = 0; i < maze.cols; i++)
                for (int j = 0; j < maze.rows; j++)
                {
                    int fi = floor[i, j];
                    if (fi >= 0)
                    {
                        vis = current[fi] == Visibility.Visible;
                        if (!vis)
                            if (vis = GrowVisible(current, i, j, growOffset))
                                cell.visibility[fi] = Visibility.Visible;
                        if (vis)                            Vis(cell, MazeMap.maze.cells[i, j, level]);

                    }
                }
        }
        void VisionAround(MazeCell cell, int growOffset = 0)
        {
            cell.visibility = new Visibility[all.Count];
            cell.visiblePair = new List<MazeCell>();
            for (int i = 0; i < cell.visibility.Length; i++) cell.visibility[i] = Visibility.Invisible;

            //   if (floor[cell.x, cell.y] >= 0) cell.visibility[floor[cell.x, cell.y]] = Visibility.Visible;
            //  if (cell.pair != null) if (cell.pair.z != level) cell.visiblePair.Add(cell.pair);

            for (int i = cell.x - growOffset; i <= cell.x + growOffset; i++)
                for (int j = cell.y - growOffset; j <= cell.y + growOffset; j++)
                {
                    if (i < maze.cols && j < maze.rows && i >= 0 && j >= 0)
                    {
                        if (floor[i, j] >= 0) cell.visibility[floor[i, j]] = Visibility.Visible;
                        Vis(cell, MazeMap.maze.cells[i, j, level]);
                    }
                }
        }
        bool Vis(MazeCell cell, int i, int j, int[,] open, int[,] wall, int[,] see)
        {
            if (open[i, j] >= 0) cell.visibility[open[i, j]] = Visibility.Visible;
            if (see[i, j] >= 0)
            {
                cell.visibility[see[i, j]] = Visibility.Transparent;
                cell.visibility[wall[i, j]] = Visibility.Opaque;
            }
            else if (wall[i, j] >= 0) { cell.visibility[wall[i, j]] = Visibility.Visible; return true; }
            return false;
        }
        void Vis(MazeCell cell, MazeCell mc)
        {
             for (int m = 0; m < 4; m++)
            {
                Vector2Int v = mc.around[m + 4];
                if (col[v.x, v.y] >= 0) cell.visibility[col[v.x, v.y]] = Visibility.Visible;
                v = mc.around[m];
                if (m % 2 == 0) Vis(cell, v.x, v.y, openH, wallH, seeH);
                else Vis(cell, v.x, v.y, openV, wallV, seeV);
            }

        }
        void Vision(MazeCell cell, int x, int y, int d, Vector2 p, Vector2 ray)
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
                bool breakLoop;
                if (vertical && (i < 0 || i > maze.cols || j < 0 || j >= maze.rows)) break;
                else if (!vertical && (i < 0 || i >= maze.cols || j < 0 || j > maze.rows)) break;
                else
                {
                    if (vertical) breakLoop = Vis(cell, i, j, openV, wallV, seeV);
                    else breakLoop = Vis(cell, i, j, openH, wallH, seeH);
                    if (breakLoop) break;
                }
            }
        }
        public void Show(MazeCell cell, bool show)
        {
            bool active;
            if (cell != null)
                for (int i = 0; i < all.Count; i++)
                {
                    if (cell.visibility[i] == Visibility.Visible) active = show;
                    else if (cell.visibility[i] == Visibility.Transparent) active = maze.transparency && show;
                    else if (cell.visibility[i] == Visibility.Opaque) active = !maze.transparency && show;
                    else active = false;
                    if (lastState[i] != active) { all[i].SetActive(active); lastState[i] = active; }

                }
        }
        public void Apply(MazeCell cell, bool considerLevel = true)
        {
            string s = cell.ijk.ToString();

            float inv = 0, vis = 0, totVis = 0;
            bool active;
            for (int i = 0; i < all.Count; i++)
            {
                if (cell.visibility[i] == Visibility.Visible) active = true;
                else if (cell.visibility[i] == Visibility.Transparent) active = maze.transparency;
                else if (cell.visibility[i] == Visibility.Opaque) active = !maze.transparency;
                else { active = false; }

                if (lastState[i] != active) { all[i].SetActive(active); lastState[i] = active; inv += active ? 0 : 1; vis += active ? 1 : 0; }
                totVis += active ? 1 : 0;
            }
            Debug.Log($"Cell changed to {s} (paired: {!considerLevel}), visible: {totVis} (appeared: {vis}), disappeared: {inv} of total: {all.Count}");
            if (considerLevel) ApplyOnPairs(cell);

        }
        public void RemoveTransparency(MazeCell cell, bool considerLevel = true)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (cell.visibility[i] == Visibility.Transparent) { if (lastState[i]) all[i].SetActive(lastState[i] = false); }
                else if (cell.visibility[i] == Visibility.Opaque) { if (!lastState[i]) all[i].SetActive(lastState[i] = true); }
            }
            if (considerLevel && cell.pair != null) RemoveTransparency(cell, false);
        }
        void ApplyOnPairs(MazeCell cell)
        {
            //    Debug.Log("first called aop");
            bool[] levelVisibility = new bool[MazeMap.maze.levelRoot.Length];
            levelVisibility[level] = true;
            int z;
            MazeCell pair;
            for (int i = 0; i < cell.visiblePair.Count; i++)
            {
                pair = cell.visiblePair[i];
                z = pair.z;
                if (levelVisibility[z])
                    maze.vision.levels[z].Show(pair, true);
                else
                {
                    levelVisibility[z] = true;
                    //  Debug.Log("first: visib: " + pair.ijk.ToString()+ (pair.visibility == null));
                    maze.vision.levels[z].Apply(pair, false);
                }

                if (cell.pair != null)
                //    if (false)
                {
                    z = cell.pair.z;
                    if (levelVisibility[z])
                        maze.vision.levels[z].Show(cell.pair, true);
                    else
                    {
                        levelVisibility[z] = true;
                        //                   maze.levelRoot[z].SetActive(true);
                        maze.vision.levels[z].Apply(cell.pair, false);
                    }
                }
            }
            for (int i = 0; i < MazeMap.maze.levelRoot.Length; i++)
                if (MazeMap.maze.vision.levels[i].levelActive != levelVisibility[i])
                {
                    MazeMap.maze.levelRoot[i].SetActive(levelVisibility[i]);
                    MazeMap.maze.vision.levels[i].levelActive = levelVisibility[i];
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
        public List<GameObject> grid = new List<GameObject>();
        public List<GameObject> others = new List<GameObject>();
        public LevelVision[] levels;
        public static int currentCalculatedLevel = 0, currentCellIndex = 0;
        public VisionMap(MazeMap m)
        {
            maze = m;
            levels = new LevelVision[maze.levels];
            for (int i = 0; i < levels.Length; i++)
                levels[i] = new LevelVision(m, i);
        }
        public void AddItem(GameObject g, int i, int j, int k, VisionItemType type, bool alwaysVisible, int d = 0)
        {
            levels[k].AddItem(g, i, j, type, alwaysVisible, d);
            grid.Add(g);
        }
        public void AddItem(GameObject g)
        {
            others.Add(g);
        }
        public static bool calculating;
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
        public async void Vision(bool rayCast, int growOffset = 0)
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
        public void HideAll()
        {
            for (int i = 0; i < levels.Length; i++)
                for (int j = 0; j < levels[i].all.Count; j++)
                {
                    levels[i].all[i].SetActive(false);
                    levels[i].lastState[i] = false;
                }
        }

    }

}
