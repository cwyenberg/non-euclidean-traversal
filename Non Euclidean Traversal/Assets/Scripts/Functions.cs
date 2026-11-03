using System.Collections.Generic;
using UnityEngine;


public static class Fns
{
    public static Vector3 RotateVert(Vector3 vIn, float angle, Vector3 qDirn)
    {
        // Rodrigues' rotation formula

        // Trig vals
        float c = Mathf.Cos(angle);
        float s = Mathf.Sin(angle);

        // Implement formula
        return vIn * c
            + Vector3.Cross(qDirn, vIn) * s
            + (1f - c) * Vector3.Dot(qDirn, vIn) * qDirn;

    }

}