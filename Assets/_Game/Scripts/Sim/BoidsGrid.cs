using UnityEngine;

namespace BaitSwitch.Sim
{
    /// <summary>
    /// Uniform grid rebuilt every tick with a counting sort. Preallocated, zero allocations.
    /// Fish in cell c are Sorted[Start[c] .. Start[c+1]).
    /// </summary>
    public sealed class BoidsGrid
    {
        public readonly int W, H;
        public readonly int[] Start;
        public readonly int[] Sorted;
        readonly int[] _cellOf, _fill;
        readonly float _minX, _minY, _inv;

        public BoidsGrid(Vector2 min, Vector2 max, float cellSize, int capacity)
        {
            _minX = min.x; _minY = min.y; _inv = 1f / cellSize;
            W = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) * _inv));
            H = Mathf.Max(1, Mathf.CeilToInt((max.y - min.y) * _inv));
            Start = new int[W * H + 1];
            _fill = new int[W * H];
            Sorted = new int[capacity];
            _cellOf = new int[capacity];
        }

        public int CellX(float x) => Mathf.Clamp((int)((x - _minX) * _inv), 0, W - 1);
        public int CellY(float y) => Mathf.Clamp((int)((y - _minY) * _inv), 0, H - 1);

        public void Build(FishBody[] fish, int n)
        {
            System.Array.Clear(Start, 0, Start.Length);
            for (int i = 0; i < n; i++)
            {
                int c = CellY(fish[i].pos.y) * W + CellX(fish[i].pos.x);
                _cellOf[i] = c;
                Start[c + 1]++;
            }
            for (int c = 0; c < W * H; c++) { Start[c + 1] += Start[c]; _fill[c] = Start[c]; }
            for (int i = 0; i < n; i++) Sorted[_fill[_cellOf[i]]++] = i;
        }
    }
}
