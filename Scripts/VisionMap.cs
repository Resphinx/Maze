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
                        maze.cells[i, j, level].InitializeVisibility(all.Count);
                        for (int k = 0; k < 2; k++)
                            if (raycast) VisionRayCast(maze.cells[i, j, level], k == 1, growOffset);
                            else VisionAround(maze.cells[i, j, level], k == 1, growOffset);
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

        Visibility FloorVisibility(MazeCell cell, int x, int y, bool transparency)
        {
            int index = transparency ? 1 : 0;
            Visibility v = new Visibility();
            for (int i = 0; i < 4; i++)
            {
                int x1 = i == 0 ? x + 1 : x;
                int y1 = i == 1 ? y + 1 : y;
                if (i % 2 == 0)
                {
                    if (openV[x1, y1] >= 0) v = Visibility.Min(v, cell.visibility[openV[x1, y1], index]);
                    else if (seeV[x1, y1] >= 0 && transparency) v = Visibility.Min(v, cell.visibility[seeV[x1, y1], index]);
                }
                else
                {
                    if (openH[x1, y1] >= 0) v = Visibility.Min(v, cell.visibility[openH[x1, y1], index]);
                    else if (seeH[x1, y1] >= 0 && transparency) v = Visibility.Min(v, cell.visibility[seeH[x1, y1], index]);
                }
            }
            return v;
        }


        void VisionRayCast(MazeCell cell, bool transparency, int maxOffset)
        {
            int index = transparency ? 1 : 0;
            List<MazeCell> pair = transparency ? cell.visiblePairTransparent : cell.visiblePairOpaque;

            if (floor[cell.x, cell.y] >= 0) cell.visibility[floor[cell.x, cell.y], index] = Visibility.Visible;

            if (cell.pairStart >= 0)
            {
                if (cell.pairEnding)
                {
                    if (cell.z != cell.otherSide.z) pair.Add(cell.otherSide);
                }
                else
                {
                    pair.Add(maze.pairs[cell.pairStart]);
                    pair.Add(maze.pairs[cell.pairStart + cell.pairCount - 1]);
               //      return;
                }
            }
            for (int d = 0; d < 4; d++)
            {
                Visibility visibility = new Visibility() { offset = 1 };
                int x = d == 0 ? cell.x + 1 : cell.x;
                int y = d == 1 ? cell.y + 1 : cell.y;
                if (d % 2 == 0)
                {
                    if (openV[x, y] >= 0 || (transparency && seeV[x, y] >= 0)) { visibility.visible = true; visibility.offset = 0; }
                }
                else if (openH[x, y] >= 0 || (transparency && seeH[x, y] >= 0)) { visibility.visible = true; visibility.offset = 0; }

                if (visibility.offset <= maxOffset)
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
                        Visibility v = FloorVisibility(cell, i, j, transparency);
                        if (floor[i, j] >= 0)                            cell.visibility[floor[i, j], index] = v;                        
                        if (v.visible)
                            if (maze.cells[i, j, level].pairEnding && maze.cells[i, j, level].pairIndex != cell.pairIndex)
                                pair.Add(maze.cells[i, j, level].otherSide);
                        //TODO add transparency to pairs
                    }
            for (int i = 0; i < maze.cols; i++)
                for (int j = 0; j < maze.rows; j++)
                    if (i != cell.x || j != cell.y)
                        if (floor[i, j] >= 0)
                        {
                            Visibility v = cell.visibility[floor[i, j], index];
                            ShowCell(cell, maze.cells[i, j, level], v.offset, index);
                        }

        }
      
        void VisionAround(MazeCell cell, bool transparency, int maxOffset)
        {
            int index = transparency ? 1 : 0;
            for (int i = cell.x - maxOffset; i <= cell.x + maxOffset; i++)
                for (int j = cell.y - maxOffset; j <= cell.y + maxOffset; j++)
                {
                    if (i < maze.cols && j < maze.rows && i >= 0 && j >= 0)
                    {
                        int offset = Mathf.Max(Mathf.Abs(i - cell.x), Mathf.Abs(j - cell.y));
                        if (floor[i, j] >= 0) cell.visibility[floor[i, j], index] = new Visibility() {  offset = offset };
               //         ShowCell(cell, MazeMap.maze.cells[i, j, level], offset, index);
                    }
                }
            for (int i = cell.x - maxOffset; i <= cell.x + maxOffset; i++)
                for (int j = cell.y - maxOffset; j <= cell.y + maxOffset; j++)
                {
                    if (i < maze.cols && j < maze.rows && i >= 0 && j >= 0)
                    {
                        int offset = Mathf.Max(Mathf.Abs(i - cell.x), Mathf.Abs(j - cell.y));
                        ShowCell(cell,maze.cells[i, j, level], offset, index);
                    }
                }
            if (floor[cell.x, cell.y] >= 0) cell.visibility[floor[cell.x, cell.y], index] = Visibility.Visible;
            for (int i = 0; i < all.Count; i++) if (alwaysVisible[i]) cell.visibility[i, index] = Visibility.Visible;
        }
        void RayVisiblity(MazeCell cell, int i, int j, int[,] open, int[,] wall, int[,] see, Visibility ray, bool t)
        {
            Visibility visibility;
            int index = t ? 1 : 0;
            int k = -1;
            bool hitWall = false;
            if (open[i, j] >= 0) k = open[i, j];
            else if (see[i, j] >= 0 && t) k = see[i, j];
            else if (wall[i, j] >= 0) { k = wall[i, j]; hitWall = true; }
            if (k >= 0)
            {
                visibility = cell.visibility[k, index];
                if (!visibility.visible && !ray.visible) if (visibility.offset > ray.offset) visibility.offset = ray.offset;
                if (!visibility.visible && ray.visible) { visibility.visible = true; visibility.offset = 0; }
            }
            if (hitWall)
            {
                ray.visible = false;
                ray.offset++;
            }
        }
        void ShowItem(MazeCell cell, int i, int j, int[,] open, int[,] wall, int[,] see, int offset, int index)
        {
            Visibility vis;
            int w = -1;
            if (open == null) w = col[i, j];
            else if (open[i, j] >= 0) w = open[i, j];
            else if (see[i, j] >= 0 && index == 1) w = see[i, j];
            else if (wall[i, j] >= 0) w = wall[i, j];
            if (w >= 0)
            {
                vis = cell.visibility[w, index];
                if (offset == 0) { vis.visible = true; vis.offset = 0; }
                else if (!vis.visible || vis.offset > offset) { vis.offset = offset; }
            }
        }
        void ShowCell(MazeCell cell, MazeCell mc, int offset, int index)
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
        void RayCast(MazeCell cell, int x, int y, int d, Vector2 p, Vector2 ray, Visibility vis, int maxOffset, bool transparency)
        {
            float lastM = 0.03f;
            int i = x;
            int j = y;
            int potX, potY;
            int nextX = x, nextY = y;
            int dx = ray.x < 0 ? -1 : 1, dy = ray.y < 0 ? -1 : 1;
            float m, n, m0, m1, n0, n1;
            bool vertical = d % 2 == 0;
            Visibility lastVis = new Visibility(vis);
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
                    if (vertical) RayVisiblity(cell, i, j, openV, wallV, seeV, lastVis, transparency);
                    else RayVisiblity(cell, i, j, openH, wallH, seeH, lastVis, transparency);
                    if (!lastVis.visible && lastVis.offset > maxOffset) break;
                }
            }
        }
        public void Show(MazeCell cell, bool show, int offset = 0)
        {
            bool active;
            int hid = 0;
            if (cell != null)
                for (int i = 0; i < all.Count; i++)
                {
                    Visibility v = cell.visibility[i, maze.transparency ? 0 : 1];
                    if (v.visible) active = show;
                    else active = v.offset <= offset ? show : false;
                    if (show == active && lastState[i] != active) { all[i].SetActive(active); lastState[i] = active; if (!active) hid++; }
                }
        }
        public void Apply(MazeCell cell, int offset, bool considerLevel = true)
        {
            string s = cell.ijk.ToString();

            float inv = 0, vis = 0, totVis = 0, byoffset = 0;
            bool active;
            for (int i = 0; i < all.Count; i++)
            {
                Visibility v = cell.visibility[i, maze.transparency ? 0 : 1];
                if (v.visible) active = true;
                else { active = v.offset <= offset; if (active) byoffset++; }

                if (lastState[i] != active) { all[i].SetActive(active); lastState[i] = active; inv += active ? 0 : 1; vis += active ? 1 : 0; }
                totVis += active ? 1 : 0;
            }
            Debug.Log($"Cell changed to {s} (paired: {!considerLevel}), visible: {totVis} (appeared: {vis}), disappeared: {byoffset} of total: {all.Count}");
            if (considerLevel) ApplyOnPairs(cell, 0);

        }
        public void RemoveTransparency(MazeCell cell, int offset = 0, bool considerLevel = true)
        {
            for (int i = 0; i < all.Count; i++)
            {
                Visibility op = cell.visibility[i, 0];
                Visibility tr = cell.visibility[i, 1];
                if (tr.visible || tr.offset <= offset)
                {
                    if (!op.visible && op.offset > offset)
                        if (lastState[i]) all[i].SetActive(lastState[i] = false);
                }
                else if (op.visible || op.offset <= offset)
                    if (!lastState[i]) all[i].SetActive(lastState[i] = true);
            }
            if (considerLevel && cell.pairIndex >= 0) RemoveTransparency(cell, offset, false);
        }
        void ApplyOnPairs(MazeCell cell, int offset)
        {
            bool[] levelVisibility = new bool[maze.levelRoot.Length];
            levelVisibility[level] = true;
            int z;
            MazeCell pair;
            for (int i = 0; i < cell.visiblePairOpaque.Count; i++)
            {
                pair = cell.visiblePairOpaque[i];
                z = pair.z;
                if (levelVisibility[z])
                    maze.vision.levels[z].Show(pair, true, offset);
                else
                {
                    levelVisibility[z] = true;
                    maze.vision.levels[z].Apply(pair, offset, false);
                }

            }
            for (int i = 0; i < maze.levelRoot.Length; i++)
                if (maze.vision.levels[i].levelActive != levelVisibility[i])
                {
                    maze.levelRoot[i].SetActive(levelVisibility[i]);
                    maze.vision.levels[i].levelActive = levelVisibility[i];
                    Debug.Log("level " + i + " " + levelVisibility[i]);
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
