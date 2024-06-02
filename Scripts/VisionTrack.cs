using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    /// <summary>
    /// This class is to track the visibility of the character in case it walkes behind this objkect. If this happens, depending on the <see cref="HoleType"/>, it makes it visible by "fading" the object holding this component. All object will have a vision track attached to them by <see cref="VisionMap"/>.
    /// </summary>
    public class VisionTrack : MonoBehaviour
    {
        public static bool ready = false;
        /// <summary>
        /// The renderers of this object and all of its children.
        /// </summary>
        public MeshRenderer[] renderers;
        /// <summary>
        /// The starting indexes of the <see cref="renderers"/> materials in <see cref="LevelVision.allMaterials"/>. This list is for keeping track of the original materials of the <see cref="renderers"/> when switching their materials for the <see cref="MazeOwner.fadeMaterial"/>.
        /// </summary>
        public int[] materialStart;
        /// <summary>
        /// The count of the materials of the <see cref="renderers"/> in <see cref="LevelVision.allMaterials"/>. This list is for keeping track of the original materials of the <see cref="renderers"/> when switching their materials for the <see cref="MazeOwner.fadeMaterial"/>.
        /// </summary>
        public int[] materialCount;
        /// <summary>
        /// If this object is currently faded (this is to avoid double fading an object).
        /// </summary>
        public bool faded = false;
        /// <summary>
        /// The maze's walker (for getting the character and camera's positions).
        /// </summary>
        public MazeWalker walker;
        /// <summary>
        /// The level of this object's element.
        /// </summary>
        public int level;
        List<Material> all;
        /// <summary>
        /// If true, it means the character is behind some elements and this component should check if it is that element.
        /// </summary>
        public static bool shouldFade = false;
        /// <summary>
        /// Initializes this compoenet.
        /// </summary>
        /// <param name="all">The list of all materials</param>
        /// <param name="walker">The maze's walker</param>
        /// <param name="lvl">The level of this object</param>
        public void Initialize(List<Material> all, MazeWalker walker, int lvl)
        {
            level = lvl;
            this.walker = walker;
            if (walker.maze.owner == null) Debug.Log("nullw " + lvl);
            this.all = all;
            List<MeshRenderer> rlist = new List<MeshRenderer>();
            List<int> start = new List<int>(), end = new List<int>();
            GetMaterials(gameObject, rlist, start, end);
            renderers = rlist.ToArray();
            materialStart = start.ToArray();
            materialCount = end.ToArray();
            //            renderers[0].
        }
        private void FixedUpdate()
        {
            if (ready)
            {
                if (walker == null) Debug.Log("w = " + (walker == null) + " " + gameObject.name);
                if (walker.maze.owner.inGame)
                {
                    bool fade = ShouldFade();
                    if (fade && !faded)
                        Fade(true, walker.maze.owner.fadeMaterial);
                    else if (!fade && faded)
                        Fade(false, walker.maze.owner.fadeMaterial);
                }
            }
        }
        /// <summary>
        /// Gets all materials used in this object and its children and adds them to <see cref="all"/> and <see cref="LevelVision.allMaterials"/>.
        /// </summary>
        /// <param name="g">The game object (this, and recursively its descendants)</param>
        /// <param name="renderers">The list of renderers (this starts with the main objects MeshRenderer and then its descendants' are added to the list)</param>
        /// <param name="start">The list of starting indexes so far (when all operations are concluded it will be cast to <see cref="materialStart"/>)</param>
        /// <param name="end">The list of material counts so far (when all operations are concluded it will be cast to <see cref="materialCount"/>)</param>
        void GetMaterials(GameObject g, List<MeshRenderer> renderers, List<int> start, List<int> end)
        {
            MeshRenderer r = g.GetComponent<MeshRenderer>();
            if (r != null)
            {
                int n = r.sharedMaterials.Length;
                start.Add(all.Count);
                renderers.Add(r);
                end.Add(n);
                all.AddRange(r.sharedMaterials);
            }
            for (int i = 0; i < g.transform.childCount; i++)
                GetMaterials(g.transform.GetChild(i).gameObject, renderers, start, end);
        }
        /// <summary>
        /// Fades or unfades this gameobject
        /// </summary>
        /// <param name="fade">The fade status</param>
        /// <param name="fadeMaterial">The material that should substitute the original material.</param>
        public void Fade(bool fade, Material fadeMaterial)
        {
            switch (walker.maze.owner.fadeType)
            {
                case HoleType.Replace:
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        Material[] ms = new Material[materialCount[i]];
                        for (int j = 0; j < materialCount[i]; j++)
                            ms[j] = fade ? fadeMaterial : all[j + materialStart[i]];
                        renderers[i].sharedMaterials = ms;
                    }
                    break;
                case HoleType.Camera:
                   shouldFade |= fade;
                    break;
                default:
                    for (int i = 0; i < renderers.Length; i++)
                        renderers[i].enabled = !fade;
                    break;
            }
            faded = fade;
        }
        /// <summary>
        /// Checks whetehr this object should fade or not.
        /// </summary>
        /// <returns></returns>
        public bool ShouldFade()
        {
            //    return false;
            if (walker.currentCell.z >= level) return false;
            float f = Vector3.Angle(walker.view.camera.position - walker.currentCell.floor.transform.position, transform.position - walker.currentCell.floor.transform.position);
            //    if (name.IndexOf("5,3") > 0) Debug.Log(f + " " + walker.view.camera.position.ToString() + " " + walker.currentCell.position.ToString() + " " + transform.position.ToString());
            return f < walker.maze.owner.viewAngle;
        }
    }
}
