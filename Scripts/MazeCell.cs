using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Resphinx.Maze
{
    /// <summary>
    /// The situation of a cell in relation to a construct:
    /// - <see cref="Unbundled"/>: the cell is not part of a bundle.
    /// - <see cref="Handle"/>: the cell is the handle of a non-void bundle, that is where the floor object is placed.
    /// - <see cref="Entrance"/>: a navigable cell of a bundle in its first or last row.
    /// - <see cref="Hanging"/>: a non-navigable cell of a bundle.
    /// - <see cref="Middle"/>: a navigable cell of a bundle in its middle.
    /// - <see cref="Void"/>: a non-navigable cell in a void bundle.
    /// </summary>
    public enum BundleSituation { Unbundled, Handle, Entrance, Hanging, Middle, Void }
    /// <summary>
    /// The type of a cell's connection to other cells.
    /// - <see cref="Open"/>: the player can pass through the shared side.
    /// - <see cref="Closed"/>: there is a wall on the shared side that can be passed by dash.
    /// - <see cref="Penfing"/>: temporary, only used during maze generation.
    /// - <see cref="Unpassable"/>: like <see cref="Closed"/> but passable by a dash.
    /// - <see cref="None"/>: No wall prefab shoud be placed there.
    /// </summary>
    public enum Connection { Open, Closed, Pending, Unpassable, None }
    /// <summary>
    /// The class representing a cell in a maze.
    /// </summary>
    public class MazeCell
    {
        MazeMap maze;
        /// <summary>
        /// The integer coordinates of the cell in the maze
        /// </summary>
        public int x, y, z, index;
        /// <summary>
        /// The <see cref="CellBundle"/> to which the cell belongs
        /// </summary>
        public CellBundle bundle = null;
        /// <summary>
        /// Bundle type of this cell
        /// </summary>
        public BundleSituation situation = BundleSituation.Unbundled;
        /// <summary>
        /// The integer vector representing the <see cref="x"/>, <see cref="y"/> and <see cref="z"/> of the cell.
        /// </summary>
        public Vector3Int ijk;
        /// <summary>
        /// The cell's center relative to the maze transform.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The 2x2 arrays bellow are arranged as follows: 
        /// 0,0 => x-
        /// 0,1 => x+
        /// 1,0 => z-
        /// 1,1 => z+
        /// You can convert direction index (0..4) to the above indeices by Side method.
        /// </summary>
        public MazeCell[,] neighbors = new MazeCell[2, 2];
        /// <summary>
        /// Determines what part of an <see cref="WallType.Open"/> wall is passable for each of the walls (see <see cref="neighbors"/> for the wall indexes in the array).
        /// </summary>
        public Vector2[,] opening = new Vector2[2, 2];
        /// <summary>
        /// Whether any type of passing through the walls is allowed (including dashes). See <see cref="neighbors"/> for the wall indexes in the array. 
        /// </summary>
        public bool[,] allowPass = new bool[2, 2];
        /// <summary>
        /// <see cref="Connection"/> types of between cells (see <see cref="neighbors"/> for the wall indexes in the array).
        /// </summary>
        public Connection[,] connection = new Connection[2, 2];
        /// <summary>
        /// The slope vector of on the cell's floor.
        /// </summary>
        public Vector3 walk = Vector3.right;
        /// <summary>
        /// A game object representing the cell's floor.
        /// </summary>
        public GameObject floor;
        /// <summary>
        /// The <see cref="PrefabManager"/> of the cell's floor.
        /// </summary>
        public PrefabManager floorPrefab = null;
        /// <summary>
        /// Not used.
        /// </summary>
        public int shapeIndex = 0;
        /// <summary>
        /// Not used. 
        /// </summary>
        public static List<MazeCell> shapeBack = new List<MazeCell>();
        /// <summary>
        ///  Not used.
        /// </summary>
        public static int lastRevivedIndex = -1;
        /// <summary>
        /// Not Used.
        /// </summary>
        public float shapeBackTime = 0;
        /// <summary>
        /// The <see cref="wallData"/> of the cell. The indexes of the array are x+, y+, x- and y-;
        /// </summary>
        public WallData[] wallData = new WallData[4];
        /// <summary>
        /// The columns around the cell. If the cell's bottomleft corner is x,y, the indexes of the array are x+,y x+,y+, x,y+, x,y 
        /// </summary>
        public GameObject[] columns = new GameObject[4];
        /// <summary>
        /// Game objects representing the items in the cell. For items see <see cref="ItemManager"/>.
        /// </summary>
        public GameObject[] items;
        /// <summary>
        /// The side that the <see cref="items"/> are located on. See <see cref="Side(int)"/> for the side indexes.
        /// </summary>
        public int[] itemRotation;
        /// <summary>
        /// The visibility of all game objects in the same level from this cell. The visibilities are stepped as a byte, which can be controlled via <see cref="MazeOwner.currentVisionOffset"/>. See also <see cref="VisionMap"/>.
        /// </summary>
        public byte[,] offset;
        /// <summary>
        /// The visibility of bundles from this cell. If a bundle is visible, all of its cell are also rendered. See also <see cref="VisionMap"/>.
        /// </summary>
        public byte[,] bundleOffset;

        public bool[] toHandleRow;
        /// <summary>
        /// All cells around this cell, from x+,y clockwise.
        /// </summary>
        public Vector2Int[] around = new Vector2Int[8];
        //    Vector3[] corners = new Vector3[4];
        /// <summary>
        /// The constructor for the cell based on its parent map and coordinates. Please note that z is elevation not, y.
        /// </summary>
        /// <param name="map">The parent map. See <see cref="MazeMap"/></param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="z">The z coordinate.</param>
        public MazeCell(MazeMap map, int x, int y, int z)
        {
            this.maze = map;
            this.x = x;
            this.y = y;
            this.z = z;
            index = maze.cols * y + x;
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
            position = maze.size * new Vector3(x, 0, y) + maze.height * new Vector3(0, z, 0);
        }
        /// <summary>
        /// Generates a void cell.See <see cref="MazeCell"/>'s constructor.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static MazeCell Void(MazeMap map, int x, int y, int z)
        {
            MazeCell cell = new MazeCell(map, x, y, z);
            cell.situation = BundleSituation.Void;
            return cell;
        }
        const int _o = 0;
        const int _p = 1;
        const int _h = 2;
        const int _v = 3;
        /// <summary>
        /// Creates a mono-level <see cref="CellBundle"/>. 
        /// </summary>
        /// <param name="maze">The parent maze.</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="z">The z coordinate.</param>
        /// <param name="side">The side to which the bundle extends, relative to x,y coordinate. See <see cref="Side(int)"/> for more info.</param>
        /// <param name="length">The length of the bundle in the side's direction.</param>
        /// <param name="width">The width of the bundle, perpendicular to the side's direction.</param>
        /// <returns>The bundle if possible, and null if at least one cell is already defined or outside the maze.</returns>
        public static CellBundle FlatBundle(MazeMap maze, int x, int y, int z, int side, int length, int width)
        {
            int[,] Xi = new int[length, width];
            int[,] Yi = new int[length, width];

            //   Debug.Log("creating pair...");
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                {
                    Xi[i, j] = side switch { 0 => x + i, 1 => x - j, 2 => x - i, _ => x + j };
                    Yi[i, j] = side switch { 0 => y + j, 1 => y + i, 2 => y - j, _ => y - i };
                }
            // checking validity of the cells
            int cc = 0;
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)

                    if (maze.InRange(Xi[i, j], Yi[i, j]) && z < maze.levels && z >= 0)
                        if (maze.cells[Xi[i, j], Yi[i, j], z] == null) cc++;
            if (cc != length * width) return null;
            // creating the ramp
            CellBundle cb = new CellBundle(maze, new MazeCell(maze, x, y, z), side, length, width, 1);
            cb.handle.bundle = cb;
            MazeCell mc;
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                {
                    if (i + j == 0) mc = cb.handle;
                    else mc = new MazeCell(maze, Xi[i, j], Yi[i, j], z) { bundle = cb };
                    if (cb.Add(mc, i, j))
                        maze.bundledCells.Add(mc);
                }
            cb.SetNeighbors(side);
            return cb;
        }
        /// <summary>
        /// Creates a multi-level <see cref="CellBundle"/>. The cell bundle will function as a slope (so it is 2D in terms of navigation). 
        /// <param name="maze">The parent maze.</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="z">The z coordinate.</param>
        /// <param name="side">The side to which the bundle extends, relative to x,y coordinate. See <see cref="Side(int)"/> for more info.</param>
        /// <param name="length">The length of the bundle in the side's direction.</param>
        /// <param name="width">The width of the bundle, perpendicular to the side's direction.</param>
        /// <param name="height">The height of the bundle.</param>
        /// <returns>The bundle if possible, and null if at least one cell is already defined or outside the maze.</returns>
        public static CellBundle SlopeBundle(MazeMap maze, int x, int y, int z, int side, int length, int width, int height)
        {
            int habs = Mathf.Abs(height) + 1;
            int dh = height > 0 ? 1 : -1;
            if (habs == 0) return FlatBundle(maze, x, y, z, side, length, width);
            int[,,] Xi = new int[length, width, habs];
            int[,,] Yi = new int[length, width, habs];
            int[,,] Zi = new int[length, width, habs];
            int cc = 0;
            //   Debug.Log("creating pair...");
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                    for (int k = 0; k < habs; k++)
                    {
                        Xi[i, j, k] = side switch { 0 => x + i, 1 => x - j, 2 => x - i, _ => x + j };
                        Yi[i, j, k] = side switch { 0 => y + j, 1 => y + i, 2 => y - j, _ => y - i };
                        Zi[i, j, k] = z + k * dh;
                        //        Debug.Log($"pair {i}: {Xi[i]},{Yi[i]},{Zi[i]}");
                    }
            // checking validity of the cells
            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                    if (maze.InRange(Xi[i, j, 0], Yi[i, j, 0]))
                        for (int k = 0; k < habs; k++)
                            if (z + k * dh < maze.levels && z + k * dh >= 0)
                                if (maze.cells[Xi[i, j, k], Yi[i, j, k], z + k * dh] == null) cc++;
            if (cc != habs * length * width) return null;
            Vector2Int d = Delta(side), dx = Delta(X(side));
            cc = 0;
            int cc2 = 0;
            int l1 = length - 1;
            int h1 = habs - 1;
            for (int j = 0; j < width; j++)
            {
                if (maze.InRange(Xi[0, j, 0] + dx.x, Yi[0, j, 0] + dx.y))
                    if (maze.cells[Xi[0, j, 0] + dx.x, Yi[0, j, 0] + dx.y, z] == null) cc++;
                if (maze.InRange(Xi[l1, j, h1] + d.x, Yi[l1, j, h1] + d.y))
                    if (maze.cells[Xi[l1, j, h1] + d.x, Yi[l1, j, h1] + d.y, z] == null) cc2++;
            }
            if (cc == 0 || cc2 == 0) return null;
            // creating the ramp
            CellBundle cb = new CellBundle(maze, new MazeCell(maze, x, y, z), side, length, width, height);
            cb.handle.bundle = cb;
            MazeCell mc;

            for (int i = 0; i < length; i++)
                for (int j = 0; j < width; j++)
                    for (int k = 0; k < habs; k++)
                    {
                        //      Debug.Log($"pair {i}: {Xi[i]},{Yi[i]},{Zi[i]}");
                        if (i + j + k == 0) mc = cb.handle;
                        else mc = new MazeCell(maze, Xi[i, j, k], Yi[i, j, k], Zi[i, j, k]) { bundle = cb };
                        cb.Add(mc, i, j, k);

                    }
            cb.SetNeighbors(side);
            return cb;
        }
        /// <summary>
        /// Initializes the <see cref="offset"/> and <see cref="bundleOffset"/>.
        /// </summary>
        /// <param name="count">The offset's count (the number of other objects in this level).</param>
        /// <param name="bundleCount">The bundles' count (in this level).</param>
        internal void InitializeVisibility(int count, int bundleCount)
        {
            offset = new byte[count, 2];
            for (int i = 0; i < count; i++)
                for (int j = 0; j < 2; j++)
                    offset[i, j] = 255;

            bundleOffset = new byte[bundleCount, 2];
            toHandleRow = new bool[bundleCount];
            for (int i = 0; i < bundleCount; i++)
                for (int j = 0; j < 2; j++)
                    bundleOffset[i, j] = 255;

        }
        /// <summary>
        /// Sets the neighbor at the specified direction (See <see cref="Side(int)"/> for directions).
        /// </summary>
        /// <param name="d">The direction index.</param>
        /// <param name="m">The naighboring cell.</param>
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
        /// <summary>
        /// Returns the coordinate delta based on a direction. For an x,y cell, the directions 0 to 3 indicate a clockwise rotation: right or x+,y, up or x,y+, left or x-,y and down or x,y-. To convert a direction to 2x2 arrays see <see cref="Side"/>. 
        /// </summary>
        /// <param name="dir">The direction, between 0 and 3 </param>
        /// <returns>A vector representing the delta movement.</returns>
        public static Vector2Int Delta(int dir)
        {
            int dx = dir switch { 0 => 1, 1 => 0, 2 => -1, _ => 0 };
            int dy = dir switch { 0 => 0, 1 => 1, 2 => 0, _ => -1 };
            return new Vector2Int(dx, dy);
        }
        /// <summary>
        /// Returns the indexes of side items or neighbors in various arrays. See<see cref="Delta"/> for the directions. Directions 0 to 3 are represented by 0,0 1,0 0,1 and 1,1.
        /// </summary>
        /// <param name="dir">The direction, between 0 and 3</param>
        /// <returns>An integer vector, whose components can be used as indexes of arrays.</returns>
        public static Vector2Int Side(int dir)
        {
            return new Vector2Int(dir % 2, 1 - dir / 2);
        }
        /// <summary>
        /// Returns the opposite direction index.
        /// </summary>
        /// <param name="dir">The source direction.</param>
        /// <returns>The opposite direction.</returns>
        public static int X(int dir)
        {
            return (dir + 2) % 4;
        }
        /// <summary>
        /// Returns the coordinates of a neighboring cell to this cell (even if it doesn't exist) based on direction.
        /// </summary>
        /// <param name="dir">The direction.</param>
        /// <returns>The coordinates of the neighbor.</returns>
        public Vector2Int Neighbor(int dir)
        {
            Vector2Int d = Delta(dir);
            return new Vector2Int(x + d.x, y + d.y);
        }
        /// <summary>
        /// Checks if this cell is connected (i.e. <see cref="Connection.Open"/>) to a cell at a certain direction (see <see cref="Delta"/> for directions).
        /// </summary>
        /// <param name="dir">The directions.</param>
        /// <returns>Whether it is connected.</returns>
        public bool Connected(int dir)
        {
            Vector2Int d = Delta(dir);
            int a = d.x == 0 ? 1 : 0;
            int b = a == 0 ? (d.x < 0 ? 0 : 1) : (d.y < 0 ? 0 : 1);
            return connection[a, b] == Connection.Open;
        }
        /// <summary>
        /// Returns the type of connection this cell has at a certain direction.
        /// </summary>
        /// <param name="dir">The direction.</param>
        /// <returns>See <see cref="Connection"/></returns>
        public Connection Get(int dir)
        {
            Vector2Int d = Delta(dir);

            int a = d.x == 0 ? 1 : 0;
            int b = a == 0 ? (d.x < 0 ? 0 : 1) : (d.y < 0 ? 0 : 1);
            return connection[a, b];
        }
        /// <summary>
        /// Checks if there is no wall (i.e. <see cref="Connection.None"/>) defined between this cell and one in a specified direciton.
        /// </summary>
        /// <param name="dir">The direction.</param>
        /// <returns>True if there is no wall.</returns>
        public bool NoWall(int dir)
        {
            return Get(dir) == Connection.None;

        }
        /// <summary>
        /// Sets the type of <see cref="Connection"/> (assigns it to <see cref="connection"/>) in a specified direction (or opposite directions).
        /// </summary>
        /// <param name="dir">The direction</param>
        /// <param name="c">The type of connection</param>
        /// <param name="sides">If true, it also sets the opposite side of dir.</param>
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
        /// <summary>
        /// Checks if the cell in a specified direction can be connected to this cell. 
        /// </summary>
        /// <param name="dir">The direction.</param>
        /// <returns>True if the connection is possible.</returns>
        public bool Connectable(int dir)
        {
            Connection c = Get(dir);
            return c == Connection.Open || c == Connection.Pending;
        }
        /// <summary>
        /// Sets the cell's wall on a specified side. This is mostly used for none-open wall types (see <see cref="WallType"/>s).
        /// </summary>
        /// <param name="go">The game object representing the wall.</param>
        /// <param name="side">The side.</param>
        /// <param name="start">Whether this cell should be the first cell in the walls <see cref="WallData.cell"/>s.</param>
        /// <returns>Returns the created <see cref="WallData"/>.</returns>
        public WallData SetWall(GameObject go, int side, bool start = true)
        {
            wallData[side] = new WallData();
            wallData[side].opaque = go;
            wallData[side].cell[start ? 0 : 1] = this;
            return wallData[side];
        }
        /// <summary>
        /// Sets the cell's wall on a specified side. This is mostly used for open wall types (see <see cref="WallType"/>s).
        /// </summary>
        /// <param name="go">The game object representing the wall.</param>
        /// <param name="side">The side.</param>
        /// <param name="mirrored">A mirrored wall means that if the side is not 0 or 1, the openings are mirrored for it. This is used when the <see cref="PrefabSettings.mirrored"/> is true.</param>
        /// <param name="opening">The <see cref="opening"/> of the wall.</param>
        /// <returns>Returns the created <see cref="WallData"/>.</returns>
        public WallData SetWall(GameObject go, int side, bool mirrored, Vector2 opening)
        {
            wallData[side] = new WallData();
            wallData[side].opaque = go;
            wallData[side].cell[0] = this;
            wallData[side].mirrored = true;
            wallData[side].opening = opening;
            Vector2Int ij = Side(side);
            this.opening[ij.x, ij.y] = new Vector2(mirrored && side > 1 ? 1 - opening.y : opening.x, mirrored && side > 1 ? 1 - opening.x : opening.y);
            return wallData[side];
        }
        /// <summary>
        /// Assigns an already created wall (for a neighboring cell) to this cell. 
        /// </summary>
        /// <param name="wd">The <see cref="WallData"/> representing the wall.</param>
        /// <param name="side">The walls' side.</param>
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
        /// <summary>
        /// Return the next position of the character based on its current position, speed and delta-time.
        /// </summary>
        /// <param name="feet">The current position</param>
        /// <param name="u">The walking direction</param>
        /// <param name="speed">The walking speed</param>
        /// <param name="dt">Delta time</param>
        /// <returns>The new position (that can be the current position if walking is not possible).</returns>
        public Vector3 NextPosition(Vector3 feet, Vector3 u, float speed, float dt)
        {
            Vector3 v = speed * dt * u;
            float dy;
            Vector3 f = feet + v;
            if (bundle != null && situation != BundleSituation.Hanging)
                if (bundle.height != 1)
                {
                    Vector3 w = f - bundle.reference;
                    if (bundle.onX)
                        dy = w.x / bundle.walk.x * bundle.walk.y;
                    else
                        dy = w.z / bundle.walk.z * bundle.walk.y;
                    return new Vector3(f.x, bundle.reference.y + dy, f.z);
                }
            return new Vector3(f.x, position.y, f.z);

        }
        /// <summary>
        /// Checks if movement to a point is possible (the movement will not be possible if the point is too close to non-passable walls, opening sides, or columns. 
        /// !!! There is an unfixed bug here that may cause the character to get stuck near the corner of openings. !!!
        /// </summary>
        /// <param name="p">The intended point.</param>
        /// <returns>True if the point is within the walkable range of the cell.</returns>
        public MazeCell CanGo(Vector3 p)
        {
            float q, s = maze.size, s2 = maze.size / 2;
            float min = maze.size / 15;
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
        /// <summary>
        /// This method is called (sync) when the character enters (by walking) or lands (by dashing) on the cell. See also <see cref="LeaveCell"/>.
        /// </summary>
        /// <param name="checkShape">Not used.</param>
        public void EnterCell(bool checkShape)
        {
            maze.vision.levels[z].Apply(this, maze.owner.currentVisionOffset);
        }
        /// <summary>
        /// This method is called (sync) when the character leaves (by walking) or dashes out the cell. See also <see cref="EnterCell"/>.
        /// </summary>
        public void LeaveCell()
        {
            //          maze.vision.levels[z].Apply(this);
        }
        /// <summary>
        /// Not used now. This will be used later for non-flat cells.
        /// </summary>
        /// <param name="feet"></param>
        /// <returns></returns>
        public Vector3 AddLocalElevation(Vector3 feet)
        {
            return feet;
        }
    }
}
