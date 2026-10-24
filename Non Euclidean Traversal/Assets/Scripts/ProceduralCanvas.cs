
using System.Collections.Generic;
using UnityEngine;

public class ProceduralCanvas : MonoBehaviour
{
    // 2d lists
    private readonly List<Vector2> verts2d = new();
    private readonly List<(int a, int b)> connections2d = new();
    private readonly List<LineRenderer> edges2d = new();


    // 3d lists
    private readonly List<Vector3> baseMeshVerts3d = new();
    private readonly List<(int a, int b)> connections3d = new();
    private readonly List<Vector2> vertAngles3d = new();
    private float thetaMaxDistRad = 0f;

    // Materials and sprites for drawing
    private Material lineMaterial;

    // Configurable fields
    [SerializeField] private float edgeWidth = 0.035f;
    [SerializeField] private int numCovers = 1;
    [SerializeField] private int curveSegments = 10;
    [SerializeField] private float worldQuatTheta = 0f;
    [SerializeField] private Vector3 worldQuatDirn = new(0f, 0f, 1f);
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

        // Initialize some values for the spherical mapping.
        thetaMaxDistRad = Mathf.PI * numCovers;

    }

    private void Start()
    {
        if (lineMaterial == null) return;

        // First, assemble the soccer ball geometry and its connections in 3d.
        AssembleSoccerBall();

        // Next, assemble the edges in 2d based on the 3d connections.
        AssembleEdges();

        // And initialize the canvas with the initial positions of the vertices and edges.
        UpdateCanvas();

    }

    private void AssembleSoccerBall()
    {
        // Assemble the soccer ball geometry
        float tau = (1f + Mathf.Sqrt(5f)) / 2f;         // Golden ratio
        float scale = 1f / Mathf.Sqrt(9f * tau + 10f);  // Scale factor

        // Assemble the verts
        float[] signs = { -1f, 1f };

        foreach (float sign1 in signs)
        foreach (float sign2 in signs)
        {
            // Family A:
            baseMeshVerts3d.Add(new Vector3(0f, sign1, sign2 * 3f * tau) * scale);
            baseMeshVerts3d.Add(new Vector3(sign1, sign2 * 3f * tau, 0f) * scale);
            baseMeshVerts3d.Add(new Vector3(sign2 * 3f * tau, 0f, sign1) * scale);

            foreach (float sign3 in signs)
            {
                // Family B
                baseMeshVerts3d.Add(new Vector3(sign1, sign2 * (2f + tau), sign3 * 2f * tau) * scale);
                baseMeshVerts3d.Add(new Vector3(sign2 * (2f + tau), sign3 * 2f * tau, sign1) * scale);
                baseMeshVerts3d.Add(new Vector3(sign3 * 2f * tau, sign1, sign2 * (2f + tau)) * scale);

                // Family c
                baseMeshVerts3d.Add(new Vector3(sign1 * tau, sign2 * 2f, sign3 * (2f * tau + 1f)) * scale);
                baseMeshVerts3d.Add(new Vector3(sign2 * 2f, sign3 * (2f * tau + 1f), sign1 * tau) * scale);
                baseMeshVerts3d.Add(new Vector3(sign3 * (2f * tau + 1f), sign1 * tau, sign2 * 2f) * scale);

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

    private void AssembleEdges()
    {
        // Assemble the edges in 2d based on the 3d connections.

        // Outer index denotes the cover ID in case of multiple covers;
        // inner index denotes the edge ID in the connections list.

        for (int cover = 0; cover < numCovers; cover++)
        foreach (var (a, b) in connections3d)
        {
            AddEdge();
        }

    }




    // public int AddVert(Vector2 position)
    // {
    //     int index = verts.Count;
    //     verts.Add(position);

    //     GameObject obj = new("Vertex " + index);
    //     obj.transform.SetParent(transform, false);
    //     obj.transform.position = (Vector3)position;

    //     SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
    //     sr.sprite = vertSprite;
    //     sr.color = new Color(0.2f, 0.75f, 1f);
    //     sr.sortingOrder = 10;

    //     obj.transform.localScale = Vector3.one *
    //         (vertRadius * 2f);

    //     vertObjects.Add(obj.transform);
        
    //     return index;

    // }

    // private int AddBaseMeshVert3d(Vector3 position)
    // {
    //     // The 3d verts are simply mathematical tools;
    //     //  they do not render, but are the fundamental
    //     //  objects being projected onto the 2d canvas.
    //     baseMeshVerts3d.Add(position);

    //     Vector3 vert3dRotated = RotateVert(position);
    //     Vector2 vert3dAngles = ComputeVert3dAngles(vert3dRotated);
    //     vertAngles3d.Add(vert3dAngles);

    //     Vector2 mappedVert = MapVert(vert3dAngles);
    //     vertsMapped.Add(mappedVert);

    //     int index = AddVert(mappedVert);

    //     return index;

    // }


    private Vector2 ComputeVert3dAngles(Vector3 vert3d)
    {
        float theta = Mathf.Acos(vert3d.z);
        float phi = Mathf.Repeat(
            Mathf.Atan2(vert3d.y, vert3d.x),
            2f * Mathf.PI
        );

        return new Vector2(theta, phi);
    }


    private Vector2 Map3dAnglesToCanvas(Vector2 vert3dAngles)
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
            + (1f - Mathf.Cos(worldQuatTheta)) * Vector3.Dot(worldQuatDirn, vertIn) * worldQuatDirn
        );

        return vertOut;

    }


    private void AddEdge()
    {

        GameObject obj = new("Edge");
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
        edges2d.Add(line);

    }


    private void DrawEdge(int a, int b, int cover, LineRenderer line)
    {
        for (int j = 0; j <= curveSegments; j++)
        {
            float t = (float)j / curveSegments;

            Vector2 point2d = EvalCanvasPosn(a, b, cover, t);
            Vector3 point = new(point2d.x, point2d.y, 0f);

            line.SetPosition(j, point);
        }
    }

    private void UpdateCanvas()
    {
        for (int cover = 0; cover < numCovers; cover++)
        for (int i = 0; i < connections3d.Count; i++)
        {
            var (a, b) = connections3d[i];
            DrawEdge(a, b, cover, edges2d[cover * connections3d.Count + i]);
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
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.forward);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.back);
        }

        if (dirty)
        {
            Quaternion worldQ = Quaternion.AngleAxis(worldQuatTheta * 180f / Mathf.PI, worldQuatDirn);
            worldQ = q * worldQ;
            worldQ.ToAngleAxis(out float worldQuatThetaDeg, out worldQuatDirn);
            worldQuatTheta = worldQuatThetaDeg * Mathf.PI / 180f;

            UpdateCanvas();
            Debug.Log(worldQ);

        }
    }


    // private void LateUpdate()
    // {

    // }

    // private void UpdateVertices()
    // {
    //     for (int i = 0; i < baseMeshVerts3d.Count; i++)
    //     {
    //         Vector3 rotated = RotateVert(baseMeshVerts3d[i]);

    //         Vector2 angles = ComputeVert3dAngles(rotated.normalized);
    //         Vector2 mapped = MapVert(angles);

    //         vertAngles3d[i] = angles;
    //         vertsMapped[i] = mapped;
    //         verts[i] = mapped;
    //         vertObjects[i].position = (Vector3)mapped;
    //     }
    // }

    private Vector2 EvalCanvasPosn(int a, int b, int cover, float t)
    {
        // Rotate the original 3D endpoints.
        Vector3 vert3dA = RotateVert(baseMeshVerts3d[a]);
        Vector3 vert3dB = RotateVert(baseMeshVerts3d[b]);

        // Interpolate and map to the spherical coords.
        Vector3 vert3dInterp = (1f - t) * vert3dA + t * vert3dB;
        Vector2 vert3dAngles = ComputeVert3dAngles(vert3dInterp.normalized);

        // Map to the cover
        vert3dAngles[0] = (
            cover % 2 == 0
            ? Mathf.PI * cover + vert3dAngles[0]
            : Mathf.PI * (cover + 1) - vert3dAngles[0]
        );
        vert3dAngles[1] = Mathf.Repeat(
            vert3dAngles[1] + cover * Mathf.PI, 2f * Mathf.PI);

        return Map3dAnglesToCanvas(vert3dAngles);

    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);

    }
}
