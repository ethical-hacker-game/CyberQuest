using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class AnimatedTriangleBackground : MonoBehaviour
{
    [Header("Grid")]
    [Min(1)] public int columns = 20;
    [Min(1)] public int rows = 12;

    public float width = 20f;
    public float height = 12f;

    [Tooltip("Random initial displacement of the points.")]
    [Range(0f, 1f)]
    public float jitter = 0.35f;

    [Header("Animation")]
    public float noiseScale = 0.25f;
    public float noiseSpeed = 0.15f;
    public float movementStrength = 0.2f;
    public float depthMovement = 0f;

    [Header("Rendering")]
    public Material triangleMaterial;
    public Material lineMaterial;

    [Header("Optional dots")]
    public ParticleSystem dotsParticleSystem;
    public float dotSize = 0.06f;

    private Mesh _mesh;

    private Vector3[] _baseVertices;
    private Vector3[] _animatedVertices;

    private int[] _triangleIndices;
    private int[] _lineIndices;

    private ParticleSystem.Particle[] _dotParticles;

    private void Awake()
    {
        GenerateMesh();
    }

    private void Update()
    {
        AnimateVertices();
    }

    [ContextMenu("Regenerate")]
    public void GenerateMesh()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh
            {
                name = "Animated Triangle Background"
            };

            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }
        else
        {
            _mesh.Clear();
        }

        GenerateVertices();
        GenerateTrianglesAndLines();

        _mesh.SetVertices(_animatedVertices);

        _mesh.subMeshCount = 2;
        _mesh.SetIndices(
            _triangleIndices,
            MeshTopology.Triangles,
            0,
            false
        );

        _mesh.SetIndices(
            _lineIndices,
            MeshTopology.Lines,
            1,
            false
        );

        // Larger bounds so Unity doesn't disable the object
        // when points move slightly outside the original area.
        _mesh.bounds = new Bounds(
            Vector3.zero,
            new Vector3(
                width + movementStrength * 4f,
                height + movementStrength * 4f,
                10f + depthMovement * 4f
            )
        );

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (triangleMaterial != null && lineMaterial != null)
        {
            meshRenderer.sharedMaterials = new[]
            {
                triangleMaterial,
                lineMaterial
            };
        }

        SetupDots();
    }

    private void GenerateVertices()
    {
        int vertexCount = (columns + 1) * (rows + 1);

        _baseVertices = new Vector3[vertexCount];
        _animatedVertices = new Vector3[vertexCount];

        float cellWidth = width / columns;
        float cellHeight = height / rows;

        System.Random random = new System.Random(12345);

        int index = 0;

        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= columns; x++)
            {
                float positionX = -width * 0.5f + x * cellWidth;
                float positionY = -height * 0.5f + y * cellHeight;

                bool isBorder =
                    x == 0 ||
                    x == columns ||
                    y == 0 ||
                    y == rows;

                // Borders remain regular so the mesh doesn't create holes.
                if (!isBorder)
                {
                    float randomX =
                        ((float)random.NextDouble() * 2f - 1f) *
                        cellWidth *
                        jitter;

                    float randomY =
                        ((float)random.NextDouble() * 2f - 1f) *
                        cellHeight *
                        jitter;

                    positionX += randomX;
                    positionY += randomY;
                }

                Vector3 vertex = new Vector3(positionX, positionY, 0f);

                _baseVertices[index] = vertex;
                _animatedVertices[index] = vertex;

                index++;
            }
        }
    }

    private void GenerateTrianglesAndLines()
    {
        List<int> triangles = new List<int>();
        List<int> lines = new List<int>();

        HashSet<ulong> registeredEdges = new HashSet<ulong>();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int bottomLeft = GetVertexIndex(x, y);
                int bottomRight = GetVertexIndex(x + 1, y);
                int topLeft = GetVertexIndex(x, y + 1);
                int topRight = GetVertexIndex(x + 1, y + 1);

                // Alternating diagonal direction makes
                // the grid look more natural.
                if ((x + y) % 2 == 0)
                {
                    AddTriangle(
                        triangles,
                        lines,
                        registeredEdges,
                        bottomLeft,
                        bottomRight,
                        topLeft
                    );

                    AddTriangle(
                        triangles,
                        lines,
                        registeredEdges,
                        bottomRight,
                        topRight,
                        topLeft
                    );
                }
                else
                {
                    AddTriangle(
                        triangles,
                        lines,
                        registeredEdges,
                        bottomLeft,
                        bottomRight,
                        topRight
                    );

                    AddTriangle(
                        triangles,
                        lines,
                        registeredEdges,
                        bottomLeft,
                        topRight,
                        topLeft
                    );
                }
            }
        }

        _triangleIndices = triangles.ToArray();
        _lineIndices = lines.ToArray();
    }

    private void AddTriangle(
        List<int> triangles,
        List<int> lines,
        HashSet<ulong> registeredEdges,
        int a,
        int b,
        int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);

        AddEdge(lines, registeredEdges, a, b);
        AddEdge(lines, registeredEdges, b, c);
        AddEdge(lines, registeredEdges, c, a);
    }

    private void AddEdge(
        List<int> lines,
        HashSet<ulong> registeredEdges,
        int first,
        int second)
    {
        uint min = (uint)Mathf.Min(first, second);
        uint max = (uint)Mathf.Max(first, second);

        ulong edgeKey = ((ulong)min << 32) | max;

        if (!registeredEdges.Add(edgeKey))
            return;

        lines.Add(first);
        lines.Add(second);
    }

    private int GetVertexIndex(int x, int y)
    {
        return y * (columns + 1) + x;
    }

    private void AnimateVertices()
    {
        if (_mesh == null || _animatedVertices == null)
            return;

        float time = Time.time * noiseSpeed;

        for (int i = 0; i < _baseVertices.Length; i++)
        {
            Vector3 basePosition = _baseVertices[i];

            float noiseX = Mathf.PerlinNoise(
                basePosition.x * noiseScale + time,
                basePosition.y * noiseScale + 17.37f
            ) * 2f - 1f;

            float noiseY = Mathf.PerlinNoise(
                basePosition.x * noiseScale + 53.12f,
                basePosition.y * noiseScale + time
            ) * 2f - 1f;

            float noiseZ = Mathf.PerlinNoise(
                basePosition.x * noiseScale + time,
                basePosition.y * noiseScale + time + 91.73f
            ) * 2f - 1f;

            _animatedVertices[i] = basePosition + new Vector3(
                noiseX * movementStrength,
                noiseY * movementStrength,
                noiseZ * depthMovement
            );
        }

        _mesh.SetVertices(_animatedVertices);

        UpdateDots();
    }

    private void SetupDots()
    {
        if (dotsParticleSystem == null)
            return;

        ParticleSystem.MainModule main = dotsParticleSystem.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = _animatedVertices.Length;

        ParticleSystem.EmissionModule emission = dotsParticleSystem.emission;
        emission.enabled = false;

        _dotParticles =
            new ParticleSystem.Particle[_animatedVertices.Length];

        for (int i = 0; i < _dotParticles.Length; i++)
        {
            _dotParticles[i].position = _animatedVertices[i];
            _dotParticles[i].startSize = dotSize;
            _dotParticles[i].startLifetime = 999999f;
            _dotParticles[i].remainingLifetime = 999999f;
        }

        dotsParticleSystem.SetParticles(
            _dotParticles,
            _dotParticles.Length
        );
    }

    private void UpdateDots()
    {
        if (dotsParticleSystem == null || _dotParticles == null)
            return;

        for (int i = 0; i < _dotParticles.Length; i++)
        {
            _dotParticles[i].position = _animatedVertices[i];
            _dotParticles[i].startSize = dotSize;
            _dotParticles[i].remainingLifetime = 999999f;
        }

        dotsParticleSystem.SetParticles(
            _dotParticles,
            _dotParticles.Length
        );
    }
}