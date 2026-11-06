using System.Collections.Generic;
using UnityEngine;


public abstract class WorldObject
{

    // Vertices of the base object in 3d space, Euclidean coords.
    protected readonly List<Vector3> baseObjectVerts3d = new();
    // Connections between the vertices of the base object in 3d space.
    protected readonly List<(int a, int b)> connections3d = new();

    // Lines to render with
    private readonly List<LineRenderer> edges2d = new();

    private readonly Transform transform;
    private readonly Material lineMaterial;
    private readonly float edgeWidth;
    private readonly int numCovers;
    private readonly int curveSegments;


    public WorldObject(
        Transform transform,
        Material lineMaterial,
        float edgeWidth,
        int numCovers,
        int curveSegments)
        {
            this.transform = transform;
            this.lineMaterial = lineMaterial;
            this.edgeWidth = edgeWidth;
            this.numCovers = numCovers;
            this.curveSegments = curveSegments;

            Initialize();

        }

    // Populate the baseObjectVerts3d and connections3d lists with the geometry of the object.
    // This must be implemented for every object.
    public abstract void BuildMe();

    
    public void Initialize()
    {
        // Build the baseObjectVerts3d and connections3d.
        BuildMe();

        // Assemble the edges in 2d based on the 3d connections.
        AssembleEdges();
        
        // Update the canvas edges based on the current worldQuatTheta and worldQuatDirn.
        Update();

    }


    public void Update()
    {

        // Update the canvas edges based on the current worldQuatTheta and worldQuatDirn.
        UpdateCanvasEdges();

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


    private void DrawEdge(
        int indexA, int indexB, int cover, LineRenderer line)
    {
        for (int j = 0; j <= curveSegments; j++)
        {
            float t = (float)j / curveSegments;

            Vector2 point2d = EvalCanvasPosn(
                baseObjectVerts3d[indexA], baseObjectVerts3d[indexB],
                cover, t);
            Vector3 point = new(point2d.x, point2d.y, 0f);

            line.SetPosition(j, point);
        }
    }


    private void UpdateCanvasEdges()
    {
        for (int cover = 0; cover < numCovers; cover++)
        for (int i = 0; i < connections3d.Count; i++)
        {
            var (a, b) = connections3d[i];
            DrawEdge(a, b, cover, edges2d[cover * connections3d.Count + i]);
        }
    }



    private Vector2 EvalCanvasPosn(
        Vector3 baseVert3dA, Vector3 baseVert3dB,
        int cover, float t)
    {
        // Rotate the original 3D endpoints.
        Vector3 vert3dA = Fns.RotateVert(
            baseVert3dA,
            angle: PlayerMovement.worldQuatTheta, qDirn: PlayerMovement.worldQuatDirn);
        Vector3 vert3dB = Fns.RotateVert(
            baseVert3dB,
            angle: PlayerMovement.worldQuatTheta, qDirn: PlayerMovement.worldQuatDirn);

        // Interpolate and map to the spherical coords.
        Vector3 vert3dInterp = (1f - t) * vert3dA + t * vert3dB;
        Vector2 vert3dAngles = ComputeVertSphericalAngles(vert3dInterp.normalized);

        // Map to the cover
        vert3dAngles[0] = (
            cover % 2 == 0
            ? Mathf.PI * cover + vert3dAngles[0]
            : Mathf.PI * (cover + 1) - vert3dAngles[0]
        );
        vert3dAngles[1] = Mathf.Repeat(
            vert3dAngles[1] + cover * Mathf.PI, 2f * Mathf.PI);

        return MapSphericalAnglesToCanvas(vert3dAngles);

    }


    private Vector2 ComputeVertSphericalAngles(Vector3 vert3d)
    {
        // Compute the spherical angles from a 3d Euclidean vector.

        float theta = Mathf.Acos(vert3d.z);
        float phi = Mathf.Repeat(
            Mathf.Atan2(vert3d.y, vert3d.x),
            2f * Mathf.PI
        );

        return new Vector2(theta, phi);
    }

    private Vector2 MapSphericalAnglesToCanvas(Vector2 vert3dAngles)
    {
        // Maps Spherical angles onto the 2d canvas

        float radius = vert3dAngles[0];
        float theta = vert3dAngles[1];

        return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;
    }

}



public class SoccerBall : WorldObject
{

    public SoccerBall(
        Transform transform,
        Material lineMaterial,
        float edgeWidth,
        int numCovers,
        int curveSegments)
        : base(transform, lineMaterial, edgeWidth, numCovers, curveSegments)
    {
    }


    public override void BuildMe()
    {
        // Build the soccer ball geometry
        float tau = (1f + Mathf.Sqrt(5f)) / 2f;         // Golden ratio
        float scale = 1f / Mathf.Sqrt(9f * tau + 10f);  // Scale factor

        // Assemble the verts
        float[] signs = { -1f, 1f };

        foreach (float sign1 in signs)
        foreach (float sign2 in signs)
        {
            // Family A:
            baseObjectVerts3d.Add(
                new Vector3(0f, sign1, sign2 * 3f * tau) * scale);
            baseObjectVerts3d.Add(
                new Vector3(sign1, sign2 * 3f * tau, 0f) * scale);
            baseObjectVerts3d.Add(
                new Vector3(sign2 * 3f * tau, 0f, sign1) * scale);

            foreach (float sign3 in signs)
            {
                // Family B
                baseObjectVerts3d.Add(
                    new Vector3(sign1, sign2 * (2f + tau), sign3 * 2f * tau) * scale);
                baseObjectVerts3d.Add(
                    new Vector3(sign2 * (2f + tau), sign3 * 2f * tau, sign1) * scale);
                baseObjectVerts3d.Add(
                    new Vector3(sign3 * 2f * tau, sign1, sign2 * (2f + tau)) * scale);

                // Family c
                baseObjectVerts3d.Add(
                    new Vector3(sign1 * tau, sign2 * 2f, sign3 * (2f * tau + 1f)) * scale);
                baseObjectVerts3d.Add(
                    new Vector3(sign2 * 2f, sign3 * (2f * tau + 1f), sign1 * tau) * scale);
                baseObjectVerts3d.Add(
                    new Vector3(sign3 * (2f * tau + 1f), sign1 * tau, sign2 * 2f) * scale);

            }

        }

        // Assemble the connections
        for (int a = 0; a < baseObjectVerts3d.Count; a++)
        for (int b = a+1; b < baseObjectVerts3d.Count; b++)
        {
            float distance = Vector3.Distance(
                baseObjectVerts3d[a], baseObjectVerts3d[b]);
            if (Mathf.Abs(distance - 2f * scale) < 1e-5f)  
            {
                connections3d.Add((a, b));
            }
        }

    }
}




public class Bullet : WorldObject
{

    public Bullet(
        Transform transform,
        Material lineMaterial,
        float edgeWidth,
        int numCovers,
        int curveSegments)
        : base(transform, lineMaterial, edgeWidth, numCovers, curveSegments)
    {
    }


    public override void BuildMe()
    {
        // Build the bullet geometry

        const float scale = 0.1f;  // Scale factor

        baseObjectVerts3d.Add(new Vector3(scale, -scale, 1f));
        baseObjectVerts3d.Add(new Vector3(scale, scale, 1f));
        baseObjectVerts3d.Add(new Vector3(-scale, scale, 1f));
        baseObjectVerts3d.Add(new Vector3(-scale, -scale, 1f));
        foreach (Vector3 vert3d in baseObjectVerts3d)
        {
            // Normalize to sphere
            vert3d.Normalize();
        }

        // Assemble the connections
        connections3d.Add((0, 1));
        connections3d.Add((1, 2));
        connections3d.Add((2, 3));
        connections3d.Add((3, 0));

    }
}