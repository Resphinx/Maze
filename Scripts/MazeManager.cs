using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Resphinx.Maze
{
    /// <summary>
    /// The root class for managing all mazes. There should only be on manager in the scene.
    /// </summary>
    public class MazeManager : MonoBehaviour
    {
        /// <summary>
        /// The list of all maze owners.
        /// </summary>
        public static List<MazeOwner> owners = new List<MazeOwner>();
        /// <summary>
        /// The index of the current/active maze in <see cref="owners"/>.
        /// </summary>
        public static int currentMaze = -1;
        /// <summary>
        /// The index of the last maze in <see cref="owners"/>. This is used when switching mazes via <see cref="ChangeOwner(int, MazeCell)"/>.
        /// </summary>
        public static int lastMaze;
        /// <summary>
        /// The index of the next maze in <see cref="owners"/>. This is used when switching mazes via <see cref="ChangeOwner(int, MazeCell)"/>.
        /// </summary>
        public static int nextMaze;
        /// <summary>
        /// A dasher to animate switching mazes.
        /// </summary>
        public static MazeDasher dasher;
        /// <summary>
        /// A game object for creating a hole for <see cref="VisionTrack"/>.
        /// </summary>
        public static GameObject circle;
        /// <summary>
        /// Whether the manager is in the process of switching mazes.
        /// </summary>
        static bool switching = false;
        /// <summary>
        /// The main character in the maze
        /// </summary>
        public GameObject character;
        /// <summary>
        /// A spotlight that can move above the character
        /// </summary>
        public Light spotLight;
        /// <summary>
        /// The camera for rendering the hole.
        /// </summary>
        public static Camera farNear;
        /// <summary>
        /// A material where <see cref="farNear"/>'s render is applied on its texture.
        /// </summary>
        public Material unlit;
        private void Start()
        {
            CreateCircle();
            MazeWalker.InitializeRotation();
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
            farNear = Instantiate(Camera.main.gameObject).GetComponent<Camera>();
            farNear.tag = "Untagged";
            farNear.targetTexture = rt;
            farNear.transform.parent = Camera.main.transform;
            farNear.transform.localPosition = Vector3.zero;
            farNear.transform.localRotation = Quaternion.identity;
            circle.transform.parent = Camera.main.transform;
            circle.transform.rotation = Quaternion.identity;
            farNear.gameObject.GetComponent<AudioListener>().enabled = false;
            //   farNear.nearClipPlane = 0.1f;
            unlit.SetTexture("_MainTex", rt);
            //     unlit.SetTexture("_MainTex", farNear.targetTexture);
            CalcAllVisions();
            dasher = new MazeDasher();
        }
        /// <summary>
        /// Calculates all the vision maps in all mazes.
        /// </summary>
        async void CalcAllVisions()
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < owners.Count; i++)
                    owners[i].VisionStart();
            });

        }
        /// <summary>
        /// Switches mazes
        /// </summary>
        /// <param name="index">The index of the next maze</param>
        /// <param name="cell">The cell where the character should land on</param>
        public static void ChangeOwner(int index, MazeCell cell = null)
        {
            owners[index].maze.SetCurrentCell(cell);
            dasher.Init(owners[currentMaze].walker.view.camera.position, owners[index].walker.view.camera.position, 1);
            lastMaze = currentMaze;
            owners[lastMaze].inGame = false;
            nextMaze = index;
            currentMaze = -1;
            switching = true;

        }
        private void FixedUpdate()
        {
            if (owners.Count > 0)
                if (!VisionTrack.ready)
                    if (owners[0].ready)
                    {
                        currentMaze = 0;
                        owners[0].inGame = true;
                        VisionTrack.ready = true;
                        Debug.Log("current maze =" + 0);
                    }

            if (!VisionTrack.ready) return;

            if (currentMaze < 0 && switching)
                if (dasher.TryReach(Time.deltaTime))
                {
                    currentMaze = nextMaze;

                    switching = false;
                    owners[nextMaze].inGame = true;
                }
                else
                {
                    Vector3[] vec0 = new Vector3[] { owners[lastMaze].walker.view.camera.up, owners[lastMaze].walker.view.camera.forward };
                    Vector3[] vec1 = new Vector3[] { owners[nextMaze].walker.view.camera.up, owners[nextMaze].walker.view.camera.forward };
                    Vector3 u = vec0[0] + dasher.progress * (vec1[0] - vec0[0]);
                    Vector3 f = vec0[1] + dasher.progress * (vec1[1] - vec0[1]);
                    Camera.main.transform.position = dasher.position;
                    Camera.main.transform.LookAt(Camera.main.transform.position + f, u);
                    spotLight.transform.position = dasher.position + 15 * u;
                    spotLight.transform.LookAt(dasher.position);
                }
            if (currentMaze >= 0)
            {
                character.transform.position = owners[currentMaze].walker.view.feet.position;
                character.transform.rotation = owners[currentMaze].walker.view.character.rotation;
                spotLight.transform.position = owners[currentMaze].walker.view.feet.position + 15 * owners[currentMaze].maze.root.transform.up;
                spotLight.transform.LookAt(owners[currentMaze].walker.view.feet.position);
                if (UserInputs.Pressed(UserInputs.Teleport))
                {
                    if (owners.Count > 1)
                        ChangeOwner((currentMaze + 1) % owners.Count);
                }
                else if (VisionTrack.shouldFade)
                {
                    circle.gameObject.SetActive(true);
                    farNear.Render();
                }
                else circle.gameObject.SetActive(false);

            }
        }
        /// <summary>
        ///
        /// </summary>
        public void CreateCircle()
        {
            circle = new GameObject("circle");
            MeshFilter filter = circle.AddComponent<MeshFilter>();
            Mesh m = new Mesh();

            Vector3[] v = new Vector3[9];
            Vector3[] n = new Vector3[9];
            Vector2[] uv = new Vector2[9];
            int[] t = new int[24];
            for (int i = 0; i < 8; i++)
            {
                v[i] = 0.5f * new Vector3(Mathf.Cos(i * Mathf.PI / 4), Mathf.Sin(i * Mathf.PI / 4), 0);
                uv[i] = new Vector2(v[i].x + 0.5f, v[i].y + 0.5f);
                n[i] = -Vector3.forward;
                t[i * 3] = 8;
                t[i * 3 + 1] = (i + 1) % 8;
                t[i * 3 + 2] = i;
            }
            v[^1] = Vector3.zero;
            n[^1] = -Vector3.forward;
            uv[^1] = 0.5f * Vector3.one;

            m.vertices = v;
            m.uv = uv;
            m.normals = n;
            m.triangles = t;
            m.RecalculateNormals();
            filter.sharedMesh = m;

            MeshRenderer mr = circle.AddComponent<MeshRenderer>();
            mr.sharedMaterial = unlit;
        }
    }
}
