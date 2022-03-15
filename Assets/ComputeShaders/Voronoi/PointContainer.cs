using UnityEngine;

namespace ComputeShaders.Voronoi
{
    [System.Serializable]
    public class PointContainer
    {
        private Transform _point;
        private Color _areaColor;


        public PointContainer(Color areaColor, Transform point)
        {
            this.AreaColor = areaColor;
            this._point = point;
        }

        public BufferPoint BlitablePoint
        {
            get
            {
                //project 3d transform point to plane
                return new BufferPoint()
                {
                    Color = _areaColor,
                    Point = Camera.main.WorldToScreenPoint(_point.position)
                };
            }
        }

        public Color AreaColor
        {
            get => _areaColor;
            set => _areaColor = value;
        }

        public Transform Point => _point;
    }
}