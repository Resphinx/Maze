using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Resphinx.Maze
{
    public class MazeManager : MonoBehaviour
    {
        public static List<MazeOwner> owners = new List<MazeOwner>();
        public static int currentMaze = -1, lastMaze, nextMaze;
        public static MazeDasher dasher;
        static bool teleporting = false;
        public GameObject character;
        public Light spotLight;
        private void Start()
        {
            currentMaze = 0;
            MazeWalker.InitializeRotation();
            InitAllMazes();
            dasher = new MazeDasher();
        }
        async void InitAllMazes()
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < owners.Count; i++)
                    owners[i].VisionStart();
                if (owners.Count > 0) owners[0].inGame = true;
            });

        }
        public static void ChangeOwner(int index, MazeCell cell = null)
        {
            owners[index].maze.SetCurrentCell(cell);
            dasher.Init(owners[currentMaze].walker.view.camera.position, owners[index].walker.view.camera.position, 1);
            lastMaze = currentMaze;
            owners[lastMaze].inGame = false;
            nextMaze = index;
            currentMaze = -1;
            teleporting = true;

        }
        private void FixedUpdate()
        {
            if (currentMaze < 0 && teleporting)
                if (dasher.TryReach(Time.deltaTime))
                {
                    currentMaze = nextMaze;
                    teleporting = false;
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
                    if (owners.Count > 1)
                       ChangeOwner((currentMaze + 1) % owners.Count );
            }
        }
    }
}
