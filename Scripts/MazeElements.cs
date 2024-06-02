using System;
using System.Collections.Generic;
using UnityEngine;
namespace Resphinx.Maze
{    
    /// <summary>
    /// This component is added to a <see cref="MazeOwner"/> game object to define maze elements which are not unique to outside of the main root element. Please not that each element in <see cref="items"/> should have a <see cref="PrefabSettings"/> component attached to it.
    /// </summary>
    public class MazeElements : MonoBehaviour
    {
        /// <summary>
        /// The local settings for each prefab setting.
        /// </summary>
        public LocalSettings[] items;          
    }
   
}