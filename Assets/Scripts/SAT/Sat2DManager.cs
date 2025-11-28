using System;
using System.Collections.Generic;
using UnityEngine;

public class Sat2DManager : MonoBehaviour
{
    [Tooltip("Si es true, divide el MTV entre 2 y mueve a ambos cuerpos; si es false, mueve solo el segundo.")]
    public bool pushBoth = true;

    private List<Sat2DShape> shapes = new List<Sat2DShape>();

    private static Sat2DManager _instance;

    void Awake()
    {
        _instance = this;
    }

    private void FixedUpdate()
    {
        ResolveAllPairs();
    }
    
    public static void AddToSATResolver(Sat2DShape shape)
    {
        _instance.shapes.Add(shape);
    }

    private void ResolveAllPairs()
    {
        int n = shapes.Count;
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                var A = shapes[i];
                var B = shapes[j];

                SAT2DMath.Sat2DCollisionResult res;

                if (!A.IsCircle && !B.IsCircle)
                {
                    // Polígono vs Polígono
                    var polyA = A.GetWorldPolygon();
                    var polyB = B.GetWorldPolygon();
                    if (polyA == null || polyB == null) continue;

                    res = SAT2DMath.PolygonVsPolygon(polyA, polyB);
                }
                else if (A.IsCircle && B.IsCircle)
                {
                    // Círculo vs Círculo
                    A.GetCircle(out var cA, out var rA);
                    B.GetCircle(out var cB, out var rB);
                    res = SAT2DMath.CircleVsCircle(cA, rA, cB, rB);
                }
                else
                {
                    // Círculo vs Polígono
                    Sat2DShape circ = A.IsCircle ? A : B;
                    Sat2DShape poly  = A.IsCircle ? B : A;

                    circ.GetCircle(out var c, out var r);
                    var polyVerts = poly.GetWorldPolygon();
                    if (polyVerts == null) continue;

                    res = SAT2DMath.CircleVsPolygon(c, r, polyVerts);
                }

                if (res.collided)
                {
                    // Aplica MTV (mínimo desplazamiento)
                    if (pushBoth)
                    {
                        A.transform.position -= (Vector3)(res.mtv * 0.5f);
                        B.transform.position += (Vector3)(res.mtv * 0.5f);
                    }
                    else
                    {
                        B.transform.position += (Vector3)res.mtv;
                    }
                }
            }
        }
    }
}
