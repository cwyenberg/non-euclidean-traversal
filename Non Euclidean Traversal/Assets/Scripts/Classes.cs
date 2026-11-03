using System.Collections.Generic;
using UnityEngine;


public class WorldObject
{

    // Vertices of the base object in 3d space, Euclidean coords.
    private readonly List<Vector3> baseObjectVerts3d = new();
    // Connections between the vertices of the base object in 3d space.
    private readonly List<(int a, int b)> connections3d = new();

    // Lines to render with
    private readonly List<LineRenderer> edges2d = new();

    


}