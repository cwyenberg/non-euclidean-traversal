
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
    private readonly List<(int a, int b)> connections3d = new();
    private readonly Quaternion worldQuaternion = Quaternion.identity;
    private readonly List<Vector2> vertices3dAngles = new();

    // Materials and sprites for drawing
    private Material lineMaterial;
    private Sprite vertexSprite;

    // Configurable fields
    [SerializeField] private float vertexRadius = 0.12f;
    [SerializeField] private float edgeWidth = 0.035f;

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
    }

    private void Start()
    {
        if (lineMaterial == null) return;

        AssembleSoccerBall();

        int a = AddVertex(new Vector2(-2f, 0f));
        int b = AddVertex(new Vector2(0f, 1.5f));
        int c = AddVertex(new Vector2(2f, 0f));
        int d = AddVertex(new Vector2(0f, -1f));

        AddEdge(a, b);
        AddEdge(b, c);
        AddEdge(c, d);
        AddEdge(d, a);
        AddEdge(a, c);
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
            Debug.Log($"Count: {vertices3d.Count}, a: {a}, b: {b}");
            float distance = Vector3.Distance(vertices3d[a], vertices3d[b]);
            if (Mathf.Abs(distance - 2f * scale) < 1e-5f)  
            {
                connections3d.Add((a, b));
            }
        }

        Debug.Log(connections3d.Count);

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

    public int AddVector3d(Vector3 position)
    {
        // The 3d vertices are simply mathematical tools;
        //  they do not render, but are the fundamental
        //  objects being projected into the 2d canvas.
        int index = vertices3d.Count;
        vertices3d.Add(position);

        float theta = Mathf.Acos(position.z);
        float phi = Mathf.Repeat(
            Mathf.Atan2(position.y, position.x),
            2f * Mathf.PI
        );
        vertices3dAngles.Add(new Vector2(theta, phi));

        return index;

    }

    public Vector2 mapVertice(Vector2 vertice3dAngles)
    {
        // Maps a 3d vector onto the 2d canvas according
        //  to documentation.
        float radius = vertice3dAngles[0];
        float theta = vertice3dAngles[1];

        return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;

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
        line.positionCount = 2;
        line.startWidth = edgeWidth;
        line.endWidth = edgeWidth;
        line.material = lineMaterial;
        line.startColor = Color.white;
        line.endColor = Color.white;
        line.sortingOrder = 0;

        edges.Add(line);
        connections.Add((a, b));

        UpdateEdges();
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

            edges[i].SetPosition(0, vertexObjects[a].position);
            edges[i].SetPosition(1, vertexObjects[b].position);
        }
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
