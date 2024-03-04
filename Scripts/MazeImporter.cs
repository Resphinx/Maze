using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resphinx.Maze
{
    public class MazeImporter
    {
        byte[,] data;
        public bool Read(string[] lines)
        {
            List<int> start = new List<int>();
            List<int> count = new List<int>();
            List<byte> bs = new List<byte>();
            int maxC = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string[] splt = lines[i].Split(',');
                int c = 0;
                start.Add(i);
                for (int j = 0; j < splt.Length; j++)
                {
                    if (byte.TryParse(splt[j], out byte b))
                        bs.Add(b);
                    else
                        bs.Add((byte)0);
                    c++;
                }
                count.Add(c);
                maxC = maxC >= c ? maxC : c;
            }
            if (maxC == 0) return false;

            data = new byte[lines.Length, maxC];
            for (int i = 0; i < lines.Length; i++)
                for (int j = 0; j < maxC; j++)
                    data[i, j] = j < count[i] ? bs[start[i] + j] : (byte)0;

            return true;
        }
    }
}
