using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MarchingCubes
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CubesMarching : MonoBehaviour
    {
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;

        [SerializeField] private float heightThreshold = 0.5f;

        [SerializeField] private float noiseResolution = 1;

        [SerializeField] private bool visualizeNoise;
        [SerializeField] private bool use3DNoise;

        private float[,,] _heights;

        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();

        private MeshFilter _meshFilter;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            StartCoroutine(UpdateAll());
        }

        private IEnumerator UpdateAll()
        {
            while (true)
            {
                SetHeights();
                MarchCubes();
                SetMesh();
                yield return new WaitForSeconds(1);
            }
        }

        private void MarchCubes()
        {
            _vertices.Clear();
            _triangles.Clear();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < width; z++)
                    {
                        float[] cubeCorners = new float[8];

                        for (int i = 0; i < cubeCorners.Length; i++)
                        {
                            Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                            cubeCorners[i] = _heights[corner.x, corner.y, corner.z];
                        }

                        MarchCube(new Vector3Int(x, y, z), cubeCorners);
                    }
                }
            }
        }

        private void MarchCube(Vector3 position, float[] cubeCorners)
        {
            int configIndex = GetConfigurationIndex(cubeCorners);

            if (configIndex == 0 || configIndex == 255) return;

            int edgeIndex = 0;

            for (int t = 0; t < 5; t++)
            {
                for (int v = 0; v < 3; v++)
                {
                    int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];
                    if (triTableValue == -1) return;

                    Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
                    Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];

                    Vector3 vertex = Vector3.Lerp(edgeStart, edgeEnd,
                        (heightThreshold - cubeCorners[GetEdgeEndVertex(MarchingTable.Edges[triTableValue, 0])]) /
                        (cubeCorners[GetEdgeEndVertex(MarchingTable.Edges[triTableValue, 1])] -
                         cubeCorners[GetEdgeEndVertex(MarchingTable.Edges[triTableValue, 0])])
                    );

                    _vertices.Add(vertex);
                    _triangles.Add(_vertices.Count - 1);

                    edgeIndex++;
                }
            }
        }

        private int GetEdgeEndVertex(Vector3 position)
        {
            for (int i = 0; i < MarchingTable.Corners.Length; i++)
                if (position == MarchingTable.Corners[i])
                    return i;

            return 0;
        }

        private int GetConfigurationIndex(float[] cubeCorners)
        {
            int configIndex = 0;

            for (int i = 0; i < cubeCorners.Length; i++)
            {
                if (cubeCorners[i] > heightThreshold)
                {
                    configIndex |= 1 << i;
                }
            }

            return configIndex;
        }

        private void SetMesh()
        {
            Mesh mesh = new Mesh
            {
                vertices = _vertices.ToArray(),
                triangles = _triangles.ToArray()
            };

            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;
        }

        private void SetHeights()
        {
            _heights = new float[width + 1, height + 1, width + 1];

            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    for (int z = 0; z < width + 1; z++)
                    {
                        if (use3DNoise)
                        {
                            float currentHeight = (float)NoiseS3D.Noise(x * noiseResolution / 15, y * noiseResolution / 15, z * noiseResolution / 15);
                            
                            _heights[x, y, z] = currentHeight;
                        }
                        else
                        {
                            float currentHeight = height * Mathf.PerlinNoise(x * noiseResolution, z * noiseResolution);
                            float newHeight;

                            if (y <= currentHeight - 0.5f)
                                newHeight = 0f;
                            else if (y > currentHeight + 0.5f)
                                newHeight = 1f;
                            else if (y > currentHeight)
                                newHeight = y - currentHeight;
                            else
                                newHeight = currentHeight - y;

                            _heights[x, y, z] = newHeight;
                        }
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!visualizeNoise || !Application.isPlaying) return;

            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    for (int z = 0; z < width + 1; z++)
                    {
                        Gizmos.color = new Color(_heights[x, y, z], _heights[x, y, z], _heights[x, y, z], 1);
                        Gizmos.DrawSphere(new Vector3(x, y, z), 0.2f);
                    }
                }
            }
        }
    }
}