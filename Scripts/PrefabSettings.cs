using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    
    /// <summary>
    /// Type of the element in the maze.
    /// </summary>
    public enum ModelType { Wall, Column, Floor, Item }
    /// <summary>
    /// Type of the wall. Open walls are passable and see-through; SeeThrough wall are only see-through and Closed walls are opaque and block access (please note that visibility is in terms of whether gameobjects behind them will be active or not).
    /// </summary>
    public enum WallType { Open, SeeThrough, Closed }
    /// <summary>
    /// This is used for setting if an item can be on the edges or corners of the maze.
    /// </summary>
    public enum Selector { Never, Always, Both }
    /// <summary>
    /// Not used.
    /// </summary>
    public enum VoidType { Never, Up, Down, Both }
    /// <summary>
    /// Not used.
    /// </summary>
    public enum Vertical { Up, Down, None }
    /// <summary>
    /// The initial side of a prefab. 
    /// </summary>
    public enum Sides { X_Positive, Z_Positive, X_Negative, Z_Negative, None, X, Z }
    /// <summary>
    /// Sets what kind of wall, an item can be placed next to.
    /// </summary>
    public enum ItemWallRelation { OnlyOpen, OnlyClosed, Both }
    /// <summary>
    /// Declares whether a prefab's origin is on its own center or on the cell's center.
    /// </summary>
    public enum CenterType { CellCenter, SelfCenter }
    /// <summary>
    /// This class is used to set the properties of the maze elements' prefabs. 
    /// </summary>
    public class PrefabSettings : MonoBehaviour
    {
        /// <summary>
        /// The type of the element.
        /// </summary>
        public ModelType type;
        // all
        /// <summary>
        /// If the elements instantiated from this prefab should always be active, regardless of their level or position.
        /// </summary>
        public bool alwaysVisible = false;
        /// <summary>
        /// If byCOunt is true, the number of the instances will be fixed (equal to <see cref="pool"/>). Otherwise, they will be randomized based the pool's weight.
        /// </summary>
        public bool byCount = false;
        /// <summary>
        /// The pool is used for the number of instantiated elements (if <see cref="byCount"/>=true) or as the weight of this element in the pool of random similar-type prefabs.
        /// </summary>
        public int pool = 1;
        /// <summary>
        /// See <see cref="CenterType"/>.
        /// </summary>
        public CenterType centerType = CenterType.CellCenter;
        /// <summary>
        /// The possibility of placing this element on the maze's horizontal edges (including beside voids).
        /// </summary>
        public Selector edge = Selector.Both;
        /// <summary>
        /// The possibility of placing this element on the maze's horizontal corners.
        /// </summary>
        public Selector corner = Selector.Both;
        // floor
        /// <summary>
        /// Only for floors <see cref="type"/>s: Whether this element is a bundle or not (decided by setting <see cref="length"/>).
        /// </summary>
        public bool Bundled { get { return length != 1; } }
        /// <summary>
        /// Only for floors <see cref="type"/>s: the length (cell count) of the element. Lengths over 1 makes this a bundle.
        /// </summary>
        public int length = 1;
        /// <summary>
        /// Only for floor <see cref="type"/>: the width (cell count) of the element.
        /// </summary>
        public int width = 1;
        /// <summary>
        /// Only for floor <see cref="type"/>: the height (cell count) of the element.
        /// </summary>
        public int height = 0;
        /// <summary>
        /// Positions of instances of this prefab on the maze.
        /// </summary>
        public Placement[] positions;
        // wall 
        /// <summary>
        /// Only for wall <see cref="type"/>: type of the wall.
        /// </summary>
        public WallType wallType = WallType.Open;
        /// <summary>
        /// Only for wall <see cref="type"/> and <see cref="WallType.Open"/>: The vector representing the portion of the wall this is open (walkable). Its x and y should be between 0 and 1, and y >= x.
        /// </summary>
        public Vector2 opening = Vector2.up;
        /// <summary>
        /// Only for wall <see cref="type"/> and <see cref="WallType.Open"/>: If true, some instances will have mirrored openings.
        /// </summary>
        public bool mirrored = true;
        /// <summary>
        /// Only for wall <see cref="type"/>: allows that this element to be put at its opposite direction (or <see cref="side"/>).
        /// </summary>
        public bool switchSides = true;
        // item
        /// <summary>
        /// Only for item <see cref="type"/>: this id indicates the "type" of item, and so items of the same type will not be put in the same cell.
        /// </summary>
        public string id = "";
        /// <summary>
        /// Indicates if the element can be put on all directions, not just that set by <see cref="side"/>.
        /// </summary>
        public bool rotatable = false;
        /// <summary>
        /// The initial side of the element (wall or item).
        /// </summary>
        public Sides side = Sides.None;
        /// <summary>
        /// See <see cref="ItemWallRelation"/>.
        /// </summary>
        public ItemWallRelation adjacentTo = ItemWallRelation.Both;
        /// <summary>
        /// This is used to localize prefabs for different mazes (see also <see cref="MazeElements"/>).
        /// </summary>
        [HideInInspector]
        public LocalSettings local;
        //   public VoidType voidType;
        private void Start()
        {
            local = null;   
        }
    }
    /// <summary>
    /// This class is used to enter specific coordinates for elements in the maze.
    /// </summary>
    [Serializable]
    public class Placement
    {
        public int x, y, z, d;
        public Vector3Int Vector { get { return new Vector3Int(x, y, z); } }
    }
    /// <summary>
    /// This class is used to create local versions (relative to a maze) from a prefab setting.
    /// </summary>
    [Serializable]
    public class LocalSettings
    {
        /// <summary>
        /// The source prefab setting.
        /// </summary>
        public PrefabSettings prefab;
        /// <summary>
        /// The number or weight of the local instances when generating the maze.
        /// </summary>
        public Counting counting;
        /// <summary>
        /// Placement of the instances.
        /// </summary>
        public Placement[] placements;
    }
    /// <summary>
    /// Defines how many and how the instances of a prefab should be included in a maze.
    /// </summary>
    [Serializable]
    public class Counting
    {
        /// <summary>
        /// Uses the same counts in the parent prefab setting.
        /// </summary>
        public bool asIs = true;
        /// <summary>
        /// See <see cref="PrefabSettings.byCount"/>.
        /// </summary>
        public bool byCount = true;
        /// <summary>
        /// See <see cref="PrefabSettings.pool"/>.
        /// </summary>
        public int pool = 1;

    }
}
