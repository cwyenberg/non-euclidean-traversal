
using System.Collections.Generic;
using UnityEngine;

public class ProceduralCanvas : MonoBehaviour
{
    // Canvas lists
    private readonly List<Vector2> verts = new();
    private readonly List<Transform> vertObjects = new();
    private readonly List<LineRenderer> edges = new();
    private readonly List<(int a, int b)> connections = new();

    // Mathematical objects lists
    private readonly List<Vector3> baseMeshVerts3d = new();
    private readonly List<Vector2> vertsMapped = new();
    private readonly List<(int a, int b)> connections3d = new();
    private readonly List<Vector2> verts3dAngles = new();
    private float thetaMaxDistRad = 0f;

    // Materials and sprites for drawing
    private Material lineMaterial;
    private Sprite vertSprite;

    // Configurable fields
    [SerializeField] private float vertRadius = 0.12f;
    [SerializeField] private float edgeWidth = 0.035f;
    [SerializeField] private float thetaCoverCount = 2f;
    [SerializeField] private int curveSegments = 10;
    [SerializeField] private float worldQuatTheta = 0f;
    [SerializeField] private Vector3 worldQuatDirn = new Vector3(0f, 0f, 1f);
    [SerializeField] private float moveSpeed = 10f;


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

        vertSprite = Sprite.Create(
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

        UpdateVertices();
        UpdateEdges();

    }

    private void AssembleSoccerBall()
    {
        // Assemble the soccer ball geometry
        float phi = (1f + Mathf.Sqrt(5f)) / 2f;         // Golden ratio
        float scale = 1f / Mathf.Sqrt(9f * phi + 10f);  // Scale factor

        // Assemble the verts
        float[] signs = { -1f, 1f };
        int t;

        foreach (float sign1 in signs)
        foreach (float sign2 in signs)
        {
            // Family A:
            t = AddBaseMeshVert3d(new Vector3(0f, sign1, sign2 * 3f * phi) * scale);
            t = AddBaseMeshVert3d(new Vector3(sign1, sign2 * 3f * phi, 0f) * scale);
            t = AddBaseMeshVert3d(new Vector3(sign2 * 3f * phi, 0f, sign1) * scale);

            foreach (float sign3 in signs)
            {
                // Family B
                t = AddBaseMeshVert3d(new Vector3(sign1, sign2 * (2f + phi), sign3 * 2f * phi) * scale);
                t = AddBaseMeshVert3d(new Vector3(sign2 * (2f + phi), sign3 * 2f * phi, sign1) * scale);
                t = AddBaseMeshVert3d(new Vector3(sign3 * 2f * phi, sign1, sign2 * (2f + phi)) * scale);

                // Family c
                t = AddBaseMeshVert3d(new Vector3(sign1 * phi, sign2 * 2f, sign3 * (2f * phi + 1f)) * scale);
                t = AddBaseMeshVert3d(new Vector3(sign2 * 2f, sign3 * (2f * phi + 1f), sign1 * phi) * scale);
                t = AddBaseMeshVert3d(new Vector3(sign3 * (2f * phi + 1f), sign1 * phi, sign2 * 2f) * scale);

            }

        }

        // Assemble the connections
        for (int a = 0; a < baseMeshVerts3d.Count; a++)
        for (int b = a+1; b < baseMeshVerts3d.Count; b++)
        {
            float distance = Vector3.Distance(baseMeshVerts3d[a], baseMeshVerts3d[b]);
            if (Mathf.Abs(distance - 2f * scale) < 1e-5f)  
            {
                connections3d.Add((a, b));
            }
        }

    }

    public int AddVert(Vector2 position)
    {
        int index = verts.Count;
        verts.Add(position);

        GameObject obj = new GameObject("Vertex " + index);
        obj.transform.SetParent(transform, false);
        obj.transform.position = new Vector3(
            position.x, position.y, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = vertSprite;
        sr.color = new Color(0.2f, 0.75f, 1f);
        sr.sortingOrder = 10;

        obj.transform.localScale = Vector3.one *
            (vertRadius * 2f);

        vertObjects.Add(obj.transform);
        return index;
    }

    private int AddBaseMeshVert3d(Vector3 position)
    {
        // The 3d verts are simply mathematical tools;
        //  they do not render, but are the fundamental
        //  objects being projected into the 2d canvas.
        baseMeshVerts3d.Add(position);

        Vector3 vert3dRotated = RotateVert(position);
        Vector2 vert3dAngles = ComputeVert3dAngles(vert3dRotated);
        verts3dAngles.Add(vert3dAngles);

        Vector2 mappedVert = MapVert(vert3dAngles);
        vertsMapped.Add(mappedVert);

        int index = AddVert(mappedVert);

        return index;

    }


    private Vector2 ComputeVert3dAngles(Vector3 vert3d)
    {
        float theta = Mathf.Acos(vert3d.z);
        float phi = Mathf.Repeat(
            Mathf.Atan2(vert3d.y, vert3d.x),
            2f * Mathf.PI
        );
        return new Vector2(theta, phi);
    }


    private Vector2 MapVert(Vector2 vert3dAngles)
    {
        // Maps a 3d vector onto the 2d canvas according
        //  to documentation.
        float radius = vert3dAngles[0];
        float theta = vert3dAngles[1];

        return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;

    }


    public Vector3 RotateVert(Vector3 vertIn)
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

    // public void SetVert(int index, Vector2 position)
    // {
    //     verts[index] = position;
    //     vertObjects[index].position =
    //         new Vector3(position.x, position.y, 0f);

    //     UpdateEdges();
    // }


    public void AddEdge(int a, int b)
    {
        if (a < 0 || b < 0 ||
            a >= baseMeshVerts3d.Count ||
            b >= baseMeshVerts3d.Count)
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

        // Keep the renderer and its endpoints in matching lists.
        edges.Add(line);
        connections.Add((a, b));

        // Draw the initial curve.
        DrawEdge(a, b, line);

    }


    private void DrawEdge(int a, int b, LineRenderer line)
    {
        for (int j = 0; j <= curveSegments; j++)
        {
            float t = (float)j / curveSegments;

            Vector2 point2d = EvaluateCurve(a, b, t);
            Vector3 point = new Vector3(
                point2d.x, point2d.y, 0f);

            line.SetPosition(j, point);
        }
    }

    private void UpdateEdges()
    {
        for (int i = 0; i < edges.Count; i++)
        {
            var (a, b) = connections[i];
            DrawEdge(a, b, edges[i]);
        }
    }


    void Update()
    {
        bool dirty = false;
        Quaternion q = new();

        if (Input.GetKey(KeyCode.UpArrow))
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.right);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.left);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.up);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.down);
        }

        if (dirty)
        {
            Quaternion worldQ = Quaternion.AngleAxis(worldQuatTheta * 180f / Mathf.PI, worldQuatDirn);
            worldQ *= q;
            worldQ.ToAngleAxis(out float worldQuatThetaDeg, out worldQuatDirn);
            worldQuatTheta = worldQuatThetaDeg * Mathf.PI / 180f;

            UpdateVertices();
            UpdateEdges();
            Debug.Log(worldQ);

        }
    }


    // private void LateUpdate()
    // {

    // }

    private void UpdateVertices()
    {
        for (int i = 0; i < baseMeshVerts3d.Count; i++)
        {
            Vector3 rotated = RotateVert(baseMeshVerts3d[i]);

            Vector2 angles = ComputeVert3dAngles(rotated.normalized);
            Vector2 mapped = MapVert(angles);

            verts3dAngles[i] = angles;
            vertsMapped[i] = mapped;
            verts[i] = mapped;

            vertObjects[i].position = new Vector3(
                mapped.x, mapped.y, 0f);
        }
    }

    private Vector2 EvaluateCurve(int a, int b, float t)
    {
        // Rotate the original 3D endpoints.
        Vector3 vert3dA = RotateVert(baseMeshVerts3d[a]);
        Vector3 vert3dB = RotateVert(baseMeshVerts3d[b]);

        // Interpolate and map to the sphere.
        Vector3 vert3dInterp = (1f - t) * vert3dA + t * vert3dB;
        Vector3 unitVector = vert3dInterp.normalized;

        Vector2 vert3dAngles = ComputeVert3dAngles(unitVector);

        return MapVert(vert3dAngles);
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);

        if (vertSprite != null)
        {
            Destroy(vertSprite.texture);
            Destroy(vertSprite);
        }
    }
}
