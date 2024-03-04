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
            this.alwaysVisible.Add(alwaysVisible);
        }
        /// <summary>
        /// Generates the vision map for a level
        /// </summary>
        public void Vision()
        {
            List<int> show = new List<int>(), hide = new List<int>();
            //   int k = 0;
            //     Vision(maze.cells[7, 7, 0]);
            for (int j = 0; j < maze.rows; j++)
                for (int i = 0; i < maze.cols; i++)
                {
                    VisionMap.currentCellIndex = j * maze.cols + i;
                    if (maze.cells[i, j, level].situation != PairSituation.Undefined && maze.cells[i, j, level].situation != PairSituation.Void) Vision(maze.cells[i, j, level]);
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
        bool ColumnVisibility(MazeCell cell, int x, int y)
        {
            for (int d = 0; d < 4; d++)
            {
                int i = d != 2 ? x : x - 1;
                int j = d != 3 ? y : y - 1;
                if (d % 2 == 0)
                {
                    if (i < 0 || i >= maze.cols || j < 0) continue;
                    if (openH[i, j] >= 0)
                        if (cell.visibility[openH[i, j]] != Visibility.Invisible) return true;
                    if (wallH[i, j] >= 0)
                        if (cell.visibility[wallH[i, j]] != Visibility.Invisible) return true;
                }
                else
                {
                    if (i < 0 || j >= maze.rows || j < 0) continue;
                    if (openV[i, j] >= 0)
                        if (cell.visibility[openV[i, j]] != Visibility.Invisible) return true;
                    if (wallV[i, j] >= 0)
                        if (cell.visibility[wallV[i, j]] != Visibility.Invisible) return true;
                }
            }
            return false;
        }
        void Vis(MazeCell cell, int x, int y, bool vertical)
        {
            if (vertical)
            {
                if (openV[x, y] >= 0) cell.visibility[openV[x, y]] = Visibility.Visible;
                else if (seeV[x, y] >= 0)
                {
                    cell.visibility[seeV[x, y]] = Visibility.Transparent;
                    cell.visibility[wallV[x, y]] = Visibility.Opaque;
                }
                else if (wallV[x, y] >= 0) cell.visibility[wallV[x, y]] = Visibility.Visible;
            }
            else
            {
                if (openH[x, y] >= 0) cell.visibility[openH[x, y]] = Visibility.Visible;
                else if (seeH[x, y] >= 0)
                {
                    cell.visibility[seeH[x, y]] = Visibility.Transparent;
                    cell.visibility[wallH[x, y]] = Visibility.Opaque;
                }
                else if (wallH[x, y] >= 0) cell.visibility[wallH[x, y]] = Visibility.Visible;
            }

        }
        void Vision(MazeCell cell)
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
                // inside cell
                for (int ci = 0; ci < 2; ci++)
                    for (int cj = 0; cj < 2; cj++)
                        if (col[ci + cell.x, cj + cell.y] >= 0)
                            cell.visibility[col[ci + cell.x, cj + cell.y]] = Visibility.Visible;

                for (int i = 0; i < PointCount; i++)
                {
                    Vector2 p = new Vector2(cell.x + povs[d][i].x, cell.y + povs[d][i].y);
                    for (int j = 0; j <= AngleStepCount; j++)
                        Vision(cell, x, y, d, p, rays[d][j]);
                }
                Vis(cell, cell.x, cell.y, true);
                Vis(cell, cell.x + 1, cell.y, true);
                Vis(cell, cell.x, cell.y, false);
                Vis(cell, cell.x, cell.y + 1, false);

            }
            for (int i = 0; i <= maze.cols; i++)
                for (int j = 0; j <= maze.rows; j++)
                {
                    if (i < maze.cols && j < maze.rows)
                        // 
                        if (i != cell.x || j != cell.y)
                            if (FloorVisibility(cell, i, j))
                            {
                                if (floor[i, j] >= 0) cell.visibility[floor[i, j]] = Visibility.Visible;
                                if (maze.cells[i, j, level].pair != null)
                                    if (maze.cells[i, j, level].pair.z != level) cell.visiblePair.Add(maze.cells[i, j, level].pair);
                            }
                    if (col[i, j] >= 0)
                        if (ColumnVisibility(cell, i, j)) cell.visibility[col[i, j]] = Visibility.Visible;
                }
            for (int i = 0; i < all.Count; i++)
                if (alwaysVisible[i])
                    cell.visibility[i] = Visibility.Visible;

            int av = 0, tvx = 0;
            for (int i = 0; i <= maze.cols; i++)
                for (int j = 0; j <= maze.cols; j++)
                {
                    if (j < maze.rows) if (seeV[i, j] >= 0) tvx++;
                    if (i < maze.cols) if (seeH[i, j] >= 0) tvx++;
                }
            for (int i = 0; i < all.Count; i++)
                if (cell.visibility[i] == Visibility.Transparent) av++;
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

                    //    if (m0 < lastM || n0 < lastM) Debug.Log("apsd " + x + "," + y + " " + m0 + "," + n0 + "," + lastM);
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

                if (vertical && (i < 0 || i > maze.cols || j < 0 || j >= maze.rows)) break;
                else if (!vertical && (i < 0 || i >= maze.cols || j < 0 || j > maze.rows)) break;
                else
                {
                    if (vertical)
                    {
                        if (openV[i, j] >= 0) cell.visibility[openV[i, j]] = Visibility.Visible;
                        else if (seeV[i, j] >= 0)
                        {
                            cell.visibility[seeV[i, j]] = Visibility.Transparent;
                            cell.visibility[wallV[i, j]] = Visibility.Opaque;
                        }
                        else if (wallV[i, j] >= 0) { cell.visibility[wallV[i, j]] = Visibility.Visible; break; }
                    }
                    else
                    {
                        if (openH[i, j] >= 0) cell.visibility[openH[i, j]] = Visibility.Visible;
                        else if (seeH[i, j] >= 0)
                        {
                            cell.visibility[seeH[i, j]] = Visibility.Transparent;
                            cell.visibility[wallH[i, j]] = Visibility.Opaque;
                        }
                        else if (wallH[i, j] >= 0) { cell.visibility[wallH[i, j]] = Visibility.Visible; break; }
                    }
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
        public async void Vision()
        {
            Debug.Log("calculating in vision");
            await Task.Run(() =>
            {
                lock (maze)
                {
                    //     int k = 0;
                    for (int k = 0; k < maze.levels; k++)
                    {
                        currentCalculatedLevel = k;
                        levels[k].Vision();
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
