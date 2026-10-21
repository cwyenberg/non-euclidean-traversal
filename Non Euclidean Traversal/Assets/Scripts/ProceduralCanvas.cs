
using System.Collections.Generic;
using UnityEngine;

public class ProceduralCanvas : MonoBehaviour
{
    // Canvas lists
    private readonly List<Vector2> vertices = new();
    private readonly List<Transform> vertexObjects = new();
    private readonly List<LineRenderer> edges = new();
    private readonly List<(int a, int b)> connections = new();

    // Mathematical objects lists
    private readonly List<Vector3> vertices3d = new();
    private readonly List<Vector2> verticesMapped = new();
    private readonly List<(int a, int b)> connections3d = new();
    private readonly List<Vector2> vertices3dAngles = new();
    private float thetaMaxDistRad = 0f;

    // Materials and sprites for drawing
    private Material lineMaterial;
    private Sprite vertexSprite;

    // Configurable fields
    [SerializeField] private float vertexRadius = 0.12f;
    [SerializeField] private float edgeWidth = 0.035f;
    [SerializeField] private float thetaCoverCount = 2f;
    [SerializeField] private int curveSegments = 10;
    [SerializeField] private float worldQuatTheta = 0f;
    [SerializeField] private Vector3 worldQuatDirn = new Vector3(0f, 0f, 1f);


    private void Awake()
    {
        // Material for visible 2D lines.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("Could not find Sprites/Default shader.");
            return;
        }

        lineMaterial = new Material(shader);

        // Generate a circular sprite for each vertex.
        Texture2D texture = new Texture2D(64, 64);
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float dx = (x + 0.5f - 32f) / 32f;
            float dy = (y + 0.5f - 32f) / 32f;
            texture.SetPixel(x, y,
                dx * dx + dy * dy <= 1f
                    ? Color.white
                    : Color.clear);
        }

        texture.Apply();

        vertexSprite = Sprite.Create(
            texture,
            new Rect(0, 0, 64, 64),
            new Vector2(0.5f, 0.5f),
            64f);

        thetaMaxDistRad = Mathf.PI * thetaCoverCount;
    }

    private void Start()
    {
        if (lineMaterial == null) return;

        AssembleSoccerBall();

        foreach ((int a, int b) in connections3d)
        {
            AddEdge(a, b);
        }

        // int a = AddVertex(new Vector2(-2f, 0f));
        // int b = AddVertex(new Vector2(0f, 1.5f));
        // int c = AddVertex(new Vector2(2f, 0f));
        // int d = AddVertex(new Vector2(0f, -1f));

        // AddEdge(a, b);
        // AddEdge(b, c);
        // AddEdge(c, d);
        // AddEdge(d, a);
        // AddEdge(a, c);
    }

    private void AssembleSoccerBall()
    {
        // Assemble the soccer ball geometry
        float phi = (1f + Mathf.Sqrt(5f)) / 2f;         // Golden ratio
        float scale = 1f / Mathf.Sqrt(9f * phi + 10f);  // Scale factor

        // Assemble the vertices
        float[] signs = { -1f, 1f };
        int t;

        foreach (float sign1 in signs)
        foreach (float sign2 in signs)
        {
            // Family A:
            t = AddVector3d(new Vector3(0f, sign1, sign2 * 3f * phi) * scale);
            t = AddVector3d(new Vector3(sign1, sign2 * 3f * phi, 0f) * scale);
            t = AddVector3d(new Vector3(sign2 * 3f * phi, 0f, sign1) * scale);

            foreach (float sign3 in signs)
            {
                // Family B
                t = AddVector3d(new Vector3(sign1, sign2 * (2f + phi), sign3 * 2f * phi) * scale);
                t = AddVector3d(new Vector3(sign2 * (2f + phi), sign3 * 2f * phi, sign1) * scale);
                t = AddVector3d(new Vector3(sign3 * 2f * phi, sign1, sign2 * (2f + phi)) * scale);

                // Family c
                t = AddVector3d(new Vector3(sign1 * phi, sign2 * 2f, sign3 * (2f * phi + 1f)) * scale);
                t = AddVector3d(new Vector3(sign2 * 2f, sign3 * (2f * phi + 1f), sign1 * phi) * scale);
                t = AddVector3d(new Vector3(sign3 * (2f * phi + 1f), sign1 * phi, sign2 * 2f) * scale);

            }

        }

        // Assemble the connections
        for (int a = 0; a < vertices3d.Count; a++)
        for (int b = a+1; b < vertices3d.Count; b++)
        {
            float distance = Vector3.Distance(vertices3d[a], vertices3d[b]);
            if (Mathf.Abs(distance - 2f * scale) < 1e-5f)  
            {
                connections3d.Add((a, b));
            }
        }

    }

    public int AddVertex(Vector2 position)
    {
        int index = vertices.Count;
        vertices.Add(position);

        GameObject obj = new GameObject("Vertex " + index);
        obj.transform.SetParent(transform, false);
        obj.transform.position = new Vector3(
            position.x, position.y, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = vertexSprite;
        sr.color = new Color(0.2f, 0.75f, 1f);
        sr.sortingOrder = 10;

        obj.transform.localScale = Vector3.one *
            (vertexRadius * 2f);

        vertexObjects.Add(obj.transform);
        return index;
    }

    private int AddVector3d(Vector3 position)
    {
        // The 3d vertices are simply mathematical tools;
        //  they do not render, but are the fundamental
        //  objects being projected into the 2d canvas.
        Vector3 vertex3dRotated = rotateVertex(position);
        vertices3d.Add(vertex3dRotated);
        Vector2 vertex3dAngles = computeVertex3dAngles(vertex3dRotated);
        vertices3dAngles.Add(vertex3dAngles);

        Vector2 mappedVertex = mapVertex(vertex3dAngles);
        verticesMapped.Add(mappedVertex);

        int index = AddVertex(mappedVertex);

        return index;

    }

    private Vector2 computeVertex3dAngles(Vector3 vertex3d)
    {
        float theta = Mathf.Acos(vertex3d.z);
        float phi = Mathf.Repeat(
            Mathf.Atan2(vertex3d.y, vertex3d.x),
            2f * Mathf.PI
        );
        return new Vector2(theta, phi);
    }

    private Vector2 mapVertex(Vector2 vertex3dAngles)
    {
        // Maps a 3d vector onto the 2d canvas according
        //  to documentation.
        float radius = vertex3dAngles[0];
        float theta = vertex3dAngles[1];

        return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;

    }

    public Vector3 rotateVertex(Vector3 vertIn)
    {
        Vector3 vertOut = new();

        vertOut = (
            vertIn * Mathf.Cos(worldQuatTheta)
            + Vector3.Cross(worldQuatDirn, vertIn) * Mathf.Sin(worldQuatTheta)
            + worldQuatDirn * (Vector3.Dot(worldQuatDirn, vertIn))
                            * (1f - Mathf.Cos(worldQuatTheta))
        );

        return vertOut;

    }

    public void SetVertex(int index, Vector2 position)
    {
        vertices[index] = position;
        vertexObjects[index].position =
            new Vector3(position.x, position.y, 0f);

        UpdateEdges();
    }

    public void AddEdge(int a, int b)
    {
        if (a < 0 || b < 0 ||
            a >= vertices.Count || b >= vertices.Count)
        {
            Debug.LogError("Invalid vertex index.");
            return;
        }

        GameObject obj = new GameObject("Edge");
        obj.transform.SetParent(transform, false);

        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = curveSegments + 1;
        line.startWidth = edgeWidth;
        line.endWidth = edgeWidth;
        line.material = lineMaterial;
        line.startColor = Color.white;
        line.endColor = Color.white;
        line.sortingOrder = 0;

        for (int i = 0; i <= curveSegments; i++)
        {
            float t = (float)i / curveSegments;

            Vector2 point2d = EvaluateCurve(a, b, t);
            Vector3 point = new Vector3(point2d.x, point2d.y, 0f);
            line.SetPosition(i, point);
        }

    }

    private void LateUpdate()
    {
        UpdateEdges();
    }

    private void UpdateEdges()
    {
        for (int i = 0; i < edges.Count; i++)
        {
            var (a, b) = connections[i];

            LineRenderer line = edges[i];

            for (int j = 0; j <= curveSegments; j++)
            {
                float t = (float)j / curveSegments;

                Vector2 point2d = EvaluateCurve(a, b, t);
                Vector3 point = new Vector3(point2d.x, point2d.y, 0f);
                line.SetPosition(j, point);
            }
        }
    }

    private Vector2 EvaluateCurve(int a, int b, float t)
    {
        // Grab the endpoint vectors
        Vector3 vertex3dA = vertices3d[a];
        Vector3 vertex3dB = vertices3d[b];

        // Interpolate and map to the sphere
        Vector3 vertex3dInterp = (1f - t) * vertex3dA + t * vertex3dB;
        Vector3 unitVector = vertex3dInterp.normalized;

        // Compute the angles
        Vector2 vertex3dAngles = computeVertex3dAngles(unitVector);

        // Map to a 2d position
        return mapVertex(vertex3dAngles);

    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);

        if (vertexSprite != null)
        {
            Destroy(vertexSprite.texture);
            Destroy(vertexSprite);
        }
    }
}
