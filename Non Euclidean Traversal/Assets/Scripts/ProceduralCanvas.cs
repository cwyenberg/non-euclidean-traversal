
using System.Collections.Generic;
using UnityEngine;

public class ProceduralCanvas : MonoBehaviour
{

    // Objects that will exist on the screen
    private WorldObject soccerBall;

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

    }

    private void Start()
    {
        if (lineMaterial == null) return;

        // Allocate
        soccerBall = new WorldObject(
            transform: transform,
            lineMaterial: lineMaterial,
            edgeWidth: edgeWidth,
            numCovers: numCovers,
            curveSegments: curveSegments,
            worldQuatTheta: worldQuatTheta,
            worldQuatDirn: worldQuatDirn
        );

        // Assemble the soccer ball geometry and its connections in 3d.
        AssembleSoccerBall(soccerBall);

        // Initialize the soccer ball with the initial positions of the vertices and edges.
        soccerBall.Initialize();

        // And initialize the canvas with the initial positions of the vertices and edges.
        soccerBall.Update(worldQuatTheta: worldQuatTheta, worldQuatDirn: worldQuatDirn);

    }

    private void AssembleSoccerBall(WorldObject soccerBall)
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
            soccerBall.baseObjectVerts3d.Add(
                new Vector3(0f, sign1, sign2 * 3f * tau) * scale);
            soccerBall.baseObjectVerts3d.Add(
                new Vector3(sign1, sign2 * 3f * tau, 0f) * scale);
            soccerBall.baseObjectVerts3d.Add(
                new Vector3(sign2 * 3f * tau, 0f, sign1) * scale);

            foreach (float sign3 in signs)
            {
                // Family B
                soccerBall.baseObjectVerts3d.Add(
                    new Vector3(sign1, sign2 * (2f + tau), sign3 * 2f * tau) * scale);
                soccerBall.baseObjectVerts3d.Add(
                    new Vector3(sign2 * (2f + tau), sign3 * 2f * tau, sign1) * scale);
                soccerBall.baseObjectVerts3d.Add(
                    new Vector3(sign3 * 2f * tau, sign1, sign2 * (2f + tau)) * scale);

                // Family c
                soccerBall.baseObjectVerts3d.Add(
                    new Vector3(sign1 * tau, sign2 * 2f, sign3 * (2f * tau + 1f)) * scale);
                soccerBall.baseObjectVerts3d.Add(
                    new Vector3(sign2 * 2f, sign3 * (2f * tau + 1f), sign1 * tau) * scale);
                soccerBall.baseObjectVerts3d.Add(
                    new Vector3(sign3 * (2f * tau + 1f), sign1 * tau, sign2 * 2f) * scale);

            }

        }

        // Assemble the connections
        for (int a = 0; a < soccerBall.baseObjectVerts3d.Count; a++)
        for (int b = a+1; b < soccerBall.baseObjectVerts3d.Count; b++)
        {
            float distance = Vector3.Distance(
                soccerBall.baseObjectVerts3d[a], soccerBall.baseObjectVerts3d[b]);
            if (Mathf.Abs(distance - 2f * scale) < 1e-5f)  
            {
                soccerBall.connections3d.Add((a, b));
            }
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

            soccerBall.Update(
                worldQuatTheta: worldQuatTheta, worldQuatDirn: worldQuatDirn);
            Debug.Log(worldQ);

        }
    }


    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);

    }
}
