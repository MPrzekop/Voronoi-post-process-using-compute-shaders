using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace ComputeShaders.Voronoi
{
    public class VoronoiPostProcessRunner : MonoBehaviour
    {
        [SerializeField] private ComputeShader voronoiShader;
        [SerializeField] private Material copyMaterial;

        [SerializeField, Range(1, 100000)] private int slices;


        [SerializeField, Space(), Header("debug")]
        private List<PointContainer> _points;

        private ComputeBuffer _pointsBuffer;
        private int _voronoiKernel;

        private RenderTexture _result, _pointsID;
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private bool runThisFrame = false;

        void PopulateData(BufferPoint[] data)
        {
            _pointsBuffer.SetData(data);
        }

        //Called if points count changed
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
            voronoiShader.SetBuffer(1, "Points", _pointsBuffer);
         
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


        //create rendertextures
        void CreateTextures()
        {
            _result = new RenderTexture(Screen.width, Screen.height, 24);
            _result.enableRandomWrite = true;
            _result.Create();
            _pointsID = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.RInt);
            _pointsID.enableRandomWrite = true;
            _pointsID.Create();
            copyMaterial.SetTexture(MainTex, _pointsID);
            voronoiShader.SetTexture(_voronoiKernel, "Result", _result);
            voronoiShader.SetTexture(_voronoiKernel, "pointsID", _pointsID);
            voronoiShader.SetTexture(1, "Result", _result);
            voronoiShader.SetTexture(1, "pointsID", _pointsID);
            
        }

        void RunVoronoiGeneratorShader()
        {
            if (runThisFrame) return;
            PopulateData(_points.Select(x => x.BlitablePoint).ToArray());
            voronoiShader.Dispatch(_voronoiKernel, Screen.width / 8, Screen.height / 8, 1);
            runThisFrame = true;
        }

        // Start is called before the first frame update
        void OnEnable()
        {
            if (_pointsBuffer != null)
            {
                _pointsBuffer.Release();
                _pointsBuffer = null;
            }

            _voronoiKernel = voronoiShader.FindKernel("GenerateVoronoi");
            CreateTextures();
            for (int i = 0; i < slices; i++)
            {
                var obj = new GameObject("point " + i);
                obj.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Random.Range(0, Screen.width),
                    Random.Range(0, Screen.height), 10f));
                AddPoint(obj.transform, Color.black);
                obj.transform.SetParent(transform);
            }

            CreateBuffer();
            RunVoronoiGeneratorShader();
        }

        void Update()
        {
            runThisFrame = false;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            //collect screen color
            voronoiShader.SetTexture(0, "screenColor", src);
            voronoiShader.SetTexture(1, "screenColor", src);
            

            //run postProcess shader
            voronoiShader.Dispatch(voronoiShader.FindKernel("VoronoiPostProcess"), Screen.width / 8, Screen.height / 8,
                1);
          
            //copy result texture to camera
            Graphics.Blit(_result, dest, copyMaterial);
            
        }

        private void OnDestroy()
        {
            //release all textures/buffers to avoid memleak
            if (_result != null)
            {
                _result.Release();
            }

            if (_pointsBuffer != null)
            {
                _pointsBuffer.Release();
            }

            if (_pointsID != null)
            {
                _pointsID.Release();
            }
        }
    }
}