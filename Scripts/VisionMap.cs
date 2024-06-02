using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    /// <summary>
    /// Types of elements which affect the vision settings
    /// </summary>
    public enum VisionItemType { Floor, Column, Opaque, Transparent, Open }
    /// <summary>
    /// The vision map settings of a level. A <see cref="VisionMap"/> contains multiple of this class.
    /// </summary>
    public class LevelVision
    {
        /// <summary>
        /// The maze for this vision map
        /// </summary>
        public MazeMap maze;
        /// <summary>
        /// All structural objects in this level (items are children to cell floors). This list is used to show or hide objects. Each cell has a byte[] (<see cref="MazeCell.offset"/>) of its own that contains the visibility of these objects from that cell.
        /// </summary>
        public List<GameObject> all = new List<GameObject>();
        /// <summary>
        /// The last active state of all objects. This is used to minimise calling activeSelf and SetActive.
        /// </summary>
        public bool[] lastState;
        /// <summary>
        /// Arrays of structural objects
        /// </summary>
        int[,] wallV, wallH, openH, openV, seeH, seeV, floor, col;
        bool[,] noneH, noneV;
        /// <summary>
        ///  The flattenend coordinates of columns
        /// </summary>
        Vector2[,] points;
        /// <summary>
        /// The visibility of the level. This will be true for the active level and levels visible from the visible bundles on that level.
        /// </summary>
        public bool levelActive = true;
        /// <summary>
        /// The level of this vision map.
        /// </summary>          
        public int level;
        /// <summary>
        /// The cell bundles with their first or last row on this level
        /// </summary>
        public CellBundle[] bundles;
        /// <summary>
        /// The index of bundles in the maze in <see cref="bundles"/>
        /// </summary>
        public int[] bundleLocalIndex;
        /// <summary>
        /// The number of vision tracks
        /// </summary>
        public int trackCount = 0;
        /// <summary>
        /// List of all materials used in structrual elements in this maze. This is used for swapping their materials with <see cref="MazeOwner.fadeMaterial"/> in case they block the character.
        /// </summary>
        public List<Material> allMaterials = new List<Material>();
        /// <summary>
        /// Creates an vision map for a level
        /// </summary>
        /// <param name="m">The containing maze</param>
        /// <param name="level">The level index</param>
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
        /// <summary>
        /// point on the edge of each cell from which rays are cast for vision mapping
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
        /// <summary>
        /// Rays cast from each side of the maze for vision mapping
        /// </summary>
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
        /// <summary>
        /// Finds the minimum of two bytes. It is a vestige of something more complex, and will be removed. 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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
            VisionTrack vt = all[^1].AddComponent<VisionTrack>();
            vt.Initialize(allMaterials, maze.owner.walker, level);
        }
        /// <summary>
        /// Adds a non-visual item (usually for walls between the cells of a bundle.
        /// </summary>
        /// <param name="x">X of the cell</param>
        /// <param name="y">Y of the cell</param>
        /// <param name="side">Side or direction of the wall</param>
        public void AddItem(int x, int y, int side = 0)
        {
            int i = side == 0 ? x + 1 : x;
            int j = side == 1 ? y + 1 : y;
            if (side % 2 == 0) noneV[i, j] = true;
            else noneH[i, j] = true;
        }
        /// <summary>
        /// Finds bundles on a level and populates <see cref="bundles"/> and <see cref="bundleLocalIndex"/>.
        /// </summary>
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
        /// Generates the vision map of this level. It calls <see cref="VisionRayCast(MazeCell, bool, int)"/> or <see cref="VisionAround(MazeCell, bool, byte)"/> depending on <c>raycast</c> value.
        /// </summary>
        /// <param name="raycast">If the <see cref="VisionMode"/> is RayCast</param>
        /// <param name="growOffset">The maximum offset around the directly visible cells, see <see cref="MazeOwner.maxVisionOffset"/></param>
        public void Vision(bool raycast, byte growOffset = 0)
        {
            BundlesOnLevel();
            for (int j = 0; j < maze.rows; j++)
                for (int i = 0; i < maze.cols; i++)
                {
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
        /// Checks the visibility offset of a cell's floor from another cell. The offset of a cell floor is equal to the smallest offset of its non-opaque walls. Once a floor is given an offset, all elements to that cell are given the same offset, unless they have a lower offset. 
        /// </summary>
        /// <param name="cell">The cell from which the visibility is mapped</param>
        /// <param name="x">X of the floor's cell</param>
        /// <param name="y">Y of the floor's cell</param>
        /// <param name="transparency">Whether seethrough walls should be considered</param>
        /// <returns></returns>
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
        /// <summary>
        /// Finds the visibility offset of the cell bundles from a cell and assigns the values to <see cref="MazeCell.bundleOffset"/>.
        /// </summary>
        /// <param name="cell"></param>
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
        /// <summary>
        /// Finds the offsets of all elements from a cell by casting rays from the edges of that cell. Everytime a ray hits a closed wall, its offset is increased. If the offset is equal to <c>maxOffset</c> (i.e. <see cref="MazeOwner.maxVisionOffset"/>), the ray stops. 
        /// </summary>
        /// <param name="cell">The source cell</param>
        /// <param name="transparency">Whether seethrough walls are active</param>
        /// <param name="maxOffset">The maximium visibility offset</param>
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
        /// <summary>
        /// Assigns visibility offsets of cells based on their minimum X or Y distance from the source cell. Cells with more distance than <c>maxOffset</c> are given the offset value of 255 or invisible.
        /// </summary>
        /// <param name="cell">The source cell</param>
        /// <param name="transparency">Whether seethrough walls are active</param>
        /// <param name="maxOffset">The maximium visibility offset</param>
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
        /// <summary>
        /// Extends a ray to hit the next cell edge (wall) on its path. Before calling this method, it should be clear whether the ray will hit a X-aligned or Y-aligned wall (so to pass the right array of walls).
        /// </summary>
        /// <param name="cell">The source cell</param>
        /// <param name="i">The X of the wall</param>
        /// <param name="j">The Y of the wall</param>
        /// <param name="open">The list of open wall (either vertical or horizontal)</param>
        /// <param name="wall">The list of closed wall (either vertical or horizontal)</param>
        /// <param name="see">The list of seethrough wall (either vertical or horizontal)</param>
        /// <param name="ray">The current ray's offset</param>
        /// <param name="transparency">Whetehr to consider seethrough walls</param> 
        /// <returns>If hits a closed wall returns <c>ray+1</c>, otherwise, returns <c>ray</c></returns>
        byte RayVisiblity(MazeCell cell, int i, int j, int[,] open, int[,] wall, int[,] see, byte ray, bool transparency)
        {
            byte visibility;
            int index = transparency ? 1 : 0;
            int k = -1;
            bool hitWall = false;
            if (open[i, j] >= 0) k = open[i, j];
            else if (see[i, j] >= 0 && transparency) k = see[i, j];
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
        /// <summary>
        /// Sets the visibility offset of an element at a certain location to its corresponding index in a cell's <see cref="MazeCell.offset"/>. This is called three times: once for columns (the <c>open</c> array should be null) and other times for the X-aligning and Y0aligning walls repectively.
        /// </summary>
        /// <param name="cell">The cell</param>
        /// <param name="i">X cordinate of the element</param>
        /// <param name="j">Y coordinate of the element</param>
        /// <param name="open">The list of open wall (either vertical or horizontal)</param>
        /// <param name="wall">The list of closed wall (either vertical or horizontal)</param>
        /// <param name="see">The list of seethrough wall (either vertical or horizontal)</param>
        /// <param name="offset">The visibility offset</param>
        /// <param name="index">0 for no-seethrough and 1 for seethrough active</param>
        void ShowItem(MazeCell cell, int i, int j, int[,] open, int[,] wall, int[,] see, byte offset, int index)
        {
            int w = -1;
            if (open == null) w = col[i, j];
            else if (open[i, j] >= 0) w = open[i, j];
            else if (see[i, j] >= 0 && index == 1) w = see[i, j];
            else if (wall[i, j] >= 0) w = wall[i, j];
            if (w >= 0) cell.offset[w, index] = Min(cell.offset[w, index], offset);
        }
        /// <summary>
        /// Sets the visibility offset of all elements around a specified cell. This method calls <see cref="ShowItem"/>.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="mc"></param>
        /// <param name="offset"></param>
        /// <param name="index"></param>
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
        /// <summary>
        /// Casts a ray from a specific point on the edge of a cell to find the visibility offset of all the walls on its path. This method calls <see cref="RayVisiblity"/> based on whether it hits an X-aligned or Y-aligned wall.
        /// </summary>
        /// <param name="cell">The source cell</param>
        /// <param name="x">The X the wall/edge where the ray originates</param>
        /// <param name="y">The Y the wall/edge where the ray originates</param>
        /// <param name="d">The side or direction of that wall</param>
        /// <param name="p">The exact position of the ray's start</param>
        /// <param name="ray">The ray's vector</param>
        /// <param name="lastVis">The offset of the starting position (0 if the wall is open and 1 if closed)</param>
        /// <param name="maxOffset">The maximum offset that the ray can travel (see <see cref="MazeOwner.maxVisionOffset"></see></param>
        /// <param name="transparency">Whether seetrhough walls are active</param>
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
        /// <summary>
        /// Sets the visibility of all elements visible from a cell. This is only called for cells in a bundle which is visible from another cell. This method either hides or shows elements, but not both as <see cref="Apply(MazeCell, byte, bool)"/> does.
        /// </summary>
        /// <param name="cell">The cell (not the original cell)</param>
        /// <param name="show">Whether to show or hides.</param>
        /// <param name="offset">The offset upto which elements should be visible</param>
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
        /// <summary>
        /// Sets the visibility of elements from a cell. It only changes the visibility of elements if it is different from their current status.
        /// </summary>
        /// <param name="cell">The source cell</param>
        /// <param name="offset">The current visible offset</param>
        /// <param name="considerLevel">If this is true, it also checks for the vsisble bundles and sets the visibilty of all elements from those bundled cells. </param>
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
        /// <summary>
        /// This is used to update visibilities when the transparency (seethrough) is changed to inactive. The method is also called within itself but only if <c>considerLevel</c> is true.
        /// </summary>
        /// <param name="cell">The source cell</param>
        /// <param name="offset">The current visibility offset</param>
        /// <param name="considerLevel">Changing visibilities in other levels.</param>
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
        /// <summary>
        /// Applies the visibility offset of the current cell on bundles visible from it. This method calls <see cref="CellBundle.Show(MazeMap, int, byte, bool)"/> and <see cref="CellBundle.Apply(MazeMap, int, byte, bool)"/>.
        /// </summary>
        /// <param name="cell">The current source cell</param>
        /// <param name="offset">The current visibility offset</param>
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
    /// <summary>
    /// This class is responsible for creating vision maps for the entire maze. It contains <see cref="LevelVision"/> instances for levels which contain the vision maps for each level.
    /// </summary>
    public class VisionMap
    {
        /// <summary>
        /// The corresponding maze
        /// </summary>
        public MazeMap maze;
        /// <summary>
        /// List of clones of elements that should always be visible. There is a clone for each level. The clone system is considered due to maintaining the visibilty of the element even if the level of its original element is hidden.
        /// </summary>
        public List<VisibleClones> alwaysVisible = new List<VisibleClones>();
        /// <summary>
        /// List of all structural elements created in the maze. This is used primarily to destroy these objects in <see cref="MazeMap.DestroyEverything"/>.
        /// </summary>
        public List<GameObject> grid = new List<GameObject>();
        /// <summary>
        /// List of all non-structural elements created in the maze. This is used primarily to destroy these objects in <see cref="MazeMap.DestroyEverything"/>.
        /// </summary>
        public List<GameObject> others = new List<GameObject>();
        /// <summary>
        /// The vision maps for individual levels.
        /// </summary>
        public LevelVision[] levels;

        //     public static int currentCalculatedLevel = 0, currentCellIndex = 0;
        /// <summary>
        /// If the vision is being calculated.
        /// </summary>
        public bool calculating;
        /// <summary>
        /// Creates empty vision map and its level vision maps. The calculation does not start here but with <see cref="Calculate(bool, byte)"/>
        /// </summary>
        /// <param name="m">The subjected maze</param>
        public VisionMap(MazeMap m)
        {
            maze = m;
            levels = new LevelVision[maze.levels];
            for (int i = 0; i < levels.Length; i++)
                levels[i] = new LevelVision(m, i);
        }
        /// <summary>
        /// Add an element to a level map vision. See <see cref="LevelVision.AddItem(GameObject, int, int, VisionItemType, int)"/> for the parameters.
        /// </summary>
        /// <param name="g">See above</param>
        /// <param name="i">See above</param>
        /// <param name="j">See above</param>
        /// <param name="k">The level index</param>
        /// <param name="type">See above</param>
        /// <param name="d">See above</param>
        public void AddItem(GameObject g, int i, int j, int k, VisionItemType type, int d = 0)
        {
            levels[k].AddItem(g, i, j, type, d);
            grid.Add(g);
        }
        /// <summary>
        /// Add a non-visual element to a level map vision. See <see cref="LevelVision.AddItem(int, int, int)"/> for the parameters.
        /// </summary>
        /// <param name="i">See above</param>
        /// <param name="j">See above</param>
        /// <param name="k">The level index</param>
        /// <param name="d">See above</param>
        public void AddItem(int i, int j, int k, int d = 0)
        {
            levels[k].AddItem(i, j, d);
        }
        /// <summary>
        /// Adds a non vision-related item to the maze.
        /// </summary>
        /// <param name="g"></param>
        public void AddItem(GameObject g)
        {
            others.Add(g);
        }
        /// <summary>
        /// Sets the current visibility state of gameobjects in all levels. This sets individual <see cref="LevelVision.lastState"/>s of objects.
        /// </summary>
        public void SetLastStates()
        {
            for (int k = 0; k < maze.levels; k++)
            {
                levels[k].lastState = new bool[levels[k].all.Count];
                for (int i = 0; i < levels[k].all.Count; i++)
                    levels[k].lastState[i] = levels[k].all[i].activeSelf;
            }
        }
        /// <summary>
        /// Calculate the vision maps for the parent maze
        /// </summary>
        /// <param name="rayCast">Whether the <see cref="MazeOwner.visionOffsetMode"/> is set to <see cref="VisionMode.RayCast"/>/></param>
        /// <param name="growOffset">The maximum visibility offset set by <see cref="MazeOwner.maxVisionOffset"/></param>
        public void Calculate(bool rayCast, byte growOffset = 0)
        {
            lock (maze)
            {
                for (int k = 0; k < maze.levels; k++)
                {
                    levels[k].Vision(rayCast, growOffset);
                }
                calculating = false;
                Debug.Log("calculation op finished");
            }
        }
        /// <summary>
        /// Hides all the elements in the maze. 
        /// </summary>
        public void HideAll()
        {
            for (int i = 0; i < levels.Length; i++)
                for (int j = 0; j < levels[i].all.Count; j++)
                {
                    levels[i].all[i].SetActive(false);
                    levels[i].lastState[i] = false;
                }
        }
        /// <summary>
        /// Adds a clone of a game object that should always be active.
        /// </summary>
        /// <param name="g">The source game object</param>
        /// <param name="l0">The game object's original level</param>
        /// <param name="l1">The level for the instantiated clone</param>
        public void AddAlwaysVisible(GameObject g, int l0, int l1)
        {
            alwaysVisible.Add(new VisibleClones(maze.root, g, l0, l1));
        }
        /// <summary>
        /// Sets the active level for all <see cref="alwaysVisible"/> objects. This calls <see cref="VisibleClones.SetLevel(int)"/> to make sure the right cloned object is active in each level.
        /// </summary>
        /// <param name="l"></param>
        public void SetAlwaysVisible(int l)
        {
            foreach (VisibleClones vp in alwaysVisible)
                vp.SetLevel(l);
        }
    }

}
