using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Resphinx.Maze
{
    public class MazeDasher
    {
        Vector3 vector, from;
        public Vector3 position;
        float speed, progress;
        public DashStatus dashing = DashStatus.None;
        public MazeCell destination;
        public void Init(Vector3 from, MazeCell d, float time)
        {
            this.from = from;
            position = from;
            vector = d.position - from;
            speed = 1 / time;
            progress = 0;
            dashing = DashStatus.Dashing;
            destination = d;
        }
        public bool Update(float dt)
        {
            progress += dt * speed;
            position = from + progress * vector;
            if (progress >= 1)
            {
                position = from + vector;
                dashing = DashStatus.None;
                return true;
            }
            else return false;
        }
    }
    [Serializable]
    public class ItemID
    {
        public string id = "";
        public float chance = 0.2f;
        public List<PrefabManager> items = new List<PrefabManager>();

        public GameObject AddItem(GameObject handle)
        {
            PrefabManager.onCreation = true;
            PrefabSettings mc = handle.GetComponent<PrefabSettings>();
            PrefabManager pm;
            if (mc.rotatable)
                items.Add(pm = PrefabManager.CreateQuadro(id, handle));
            else
                items.Add(pm = PrefabManager.CreateMono(id, handle));
            PrefabManager.onCreation = false;
            return pm.root;
        }
    }
    public class ItemManager
    {

        public ItemID[] ids;
        public Dictionary<string, int> itemDictionary = new Dictionary<string, int>();
        public ItemID Find(string s)
        {
            foreach (ItemID id in ids)
                if (id.id == s.ToLower()) return id;
            return null;
        }
        public void AddItem(GameObject g)
        {
            string s = g.GetComponent<PrefabSettings>().id;
            ItemID iid = Find(s);
            if (iid != null)
                iid.AddItem(g);
        }
        public GameObject GetItem(MazeCell cell, int index, CloneResult cr)
        {
            GameObject g = null;
            PrefabManager pm = PrefabManager.GetPool(ids[index].items, cr, false);
            if (pm != null)
            {
                // finding possible sides
                int side;
                if (pm.settings.adjacentTo == ItemWallRelation.Both) side = UnityEngine.Random.Range(0, 4);
                else
                {
                    List<int> possible = new List<int>();
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int ij = MazeCell.Side(i);
                        if ((cell.neighbors[ij.x, ij.y] == null) == (pm.settings.adjacentTo == ItemWallRelation.OnlyClosed))
                            possible.Add(i);
                    }
                    side = possible.Count > 0 ? possible[UnityEngine.Random.Range(0, possible.Count)] : -1;
                }

                if (pm.settings.rotatable)
                {
                    if (side >= 0)
                    {
                        g = PrefabManager.Clone(pm.side[side], cell.floor.transform);
                        g.transform.position = cell.floor.transform.position;

                    }
                }
                else if (side >= 0)
                {
                    bool sideMatched = pm.settings.side switch
                    {
                        Sides.X_Positive => side == 0,
                        Sides.Z_Positive => side == 1,
                        Sides.X_Negative => side == 2,
                        Sides.Z_Negative => side == 3,
                        Sides.X => side % 2 == 0,
                        Sides.Z => side % 2 == 1,
                        _ => true
                    };
                    if (sideMatched)
                    {
                        g = PrefabManager.Clone(pm.root, cell.floor.transform);
                        g.transform.position = cell.floor.transform.position;
                    }
                }
            }
            return g;
        }

        internal void SetItems(ItemID[] mazeItems)
        {
            ids = mazeItems;
            for (int i = 0; i < ids.Length; i++)
                itemDictionary.Add(ids[i].id, i);
        }
    }
   

}
