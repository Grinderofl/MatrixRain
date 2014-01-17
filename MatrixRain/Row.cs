using System.Collections.Generic;

namespace MatrixRain
{
    public class Row
    {
        public Row()
        {
            Changes = new Dictionary<int, KeyValuePair<char, int>>();
        }
        public char[] Letters;
        public int Position;
        public int X;
        public int Y;
        public Dictionary<int, KeyValuePair<char, int>> Changes;
    }
}