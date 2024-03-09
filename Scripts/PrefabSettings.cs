using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    public enum ModelType { Wall, Column, Floor, Item }
    public enum WallType { Open, SeeThrough, Closed}
    public enum Selector { Never, Always, Both }
    public enum VoidType { Never, Up, Down, Both}
    public enum Vertical { Up, Down, None}
    public enum Sides { X_Positive, Z_Positive, X_Negative,  Z_Negative, None, X, Z}
    public enum ItemWallRelation { OnlyOpen, OnlyClosed, Both}
    public enum CenterType { CellCenter, SelfCenter}
    public class PrefabSettings : MonoBehaviour
    {
        public ModelType type;
        // all
        public bool alwaysVisible = false;
        public bool byCount = false;
        public int count = 1;        
        public CenterType centerType = CenterType.CellCenter;
        public Selector edge = Selector.Both, corner = Selector.Both;
        // floor
        public bool Bundled { get { return length != 1; } }
        public int length = 1, width = 1 , height = 0;
        public Vector3Int[] positions;
        public int[] directions;
        // wall 
        public WallType wallType = WallType.Open;
        public Vector2 opening = Vector2.up;
        public bool mirrored = true;
        public bool switchSides=true;
        // item
        public string id = "";
        public bool rotatable = false;
        public Sides side = Sides.None;
        public ItemWallRelation adjacentTo = ItemWallRelation.Both;

     //   public VoidType voidType;
    }
}
