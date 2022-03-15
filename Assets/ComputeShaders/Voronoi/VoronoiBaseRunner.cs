using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComputeShaders.Voronoi
{
    public class VoronoiBaseRunner : MonoBehaviour
    {
        [SerializeField] private ComputeShader voronoiShader;
        [SerializeField] private Material copyMaterial;

        [SerializeField, Range(1, 10000)] private int slices;

        [SerializeField, Space(), Header("debug")]
        private List<PointContainer> _points;

        private ComputeBuffer _pointsBuffer;
        private int _voronoiKernel;
        private RenderTexture _result;
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private bool runThisFrame;

        void CreateBuffer()
        {
            if (_pointsBuffer != null)
            {
                //if buffer exists release it to avoid memory leak
                _pointsBuffer.Release();
            }

            var data = _points.Select(x => x.BlitablePoint)
                .ToArray(); //get array of blittablePoints - LINQ isn't the fastest, but is good enough for this demo
            _pointsBuffer =
                new ComputeBuffer(data.Length,
                    4 * 4 + 2 * 4 + 4); //init to points.count elements of size Color + Vector2 + int
            PopulateData(data);
            //reset buffer
            voronoiShader.SetInt("PointsCount", data.Length);
            voronoiShader.SetBuffer(_voronoiKernel, "Points", _pointsBuffer);
        }

        void PopulateData(BufferPoint[] data)
        {
            _pointsBuffer.SetData(data);
        }


        void CreateTextures()
        {
            _result = new RenderTexture(Screen.width, Screen.height, 24);
            _result.enableRandomWrite = true;
            _result.Create();

            copyMaterial.SetTexture(MainTex, _result);
            voronoiShader.SetTexture(_voronoiKernel, "Result", _result);
        }

        void RunShader()
        {
            if (runThisFrame) return;
            PopulateData(_points.Select(x => x.BlitablePoint).ToArray());
            voronoiShader.Dispatch(_voronoiKernel, Screen.width / 8, Screen.height / 8, 1);
            runThisFrame = true;
        }

        public void AddPoint(Transform point, Color c, bool updateBuffer = false)
        {
            if (_points == null)
            {
                _points = new List<PointContainer>();
            }

            _points.Add(new PointContainer(c, point));
            if (updateBuffer)
                CreateBuffer();
        }

        // Start is called before the first frame update
        void Start()
        {
            _voronoiKernel = voronoiShader.FindKernel("GenerateVoronoi");
            CreateTextures();
            for (int i = 0; i < slices; i++)
            {
                var obj = new GameObject("point " + i);
                obj.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Random.Range(0, Screen.width),
                    Random.Range(0, Screen.height), 10f));
                AddPoint(obj.transform, Random.ColorHSV());
                obj.transform.SetParent(transform);
                obj.AddComponent<ScenePoint>().OnTransformChange.AddListener(RunShader);
            }

            CreateBuffer();
            RunShader();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(_result, dest, copyMaterial);
        }

        // Update is called once per frame
        void Update()
        {
            runThisFrame = false;
        }
    }
}