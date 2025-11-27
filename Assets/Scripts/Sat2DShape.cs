using System;
using UnityEngine;

public class Sat2DShape : MonoBehaviour
{
    public enum ShapeKind { Rectangle, Triangle, Circle }
    public enum TriangleLayout { Up, Down, Left, Right } // cómo se arma el triángulo desde el rectángulo
    public enum CircleFit { Circumscribed, Inscribed }    // circunscrito (cubre todo) o inscrito (dentro)

    [Header("Tipo de figura")]
    public ShapeKind shape = ShapeKind.Rectangle;

    [Header("Ajustes de figura")]
    public TriangleLayout triangleLayout = TriangleLayout.Up;
    public CircleFit circleFit = CircleFit.Circumscribed;

    [Header("Debug")]
    public Color gizmoColor = new Color(1f, 0.8f, 0.2f, 1f);
    public bool drawGizmos = true;

    private SpriteRenderer sr;

    // Salidas calculadas
    public Vector2[] worldPolygon; // usado para rectángulo y triángulo (convexos)
    public Vector2 circleCenter;   // usado para círculo
    public float circleRadius;

    public Bounds WorldAABB => sr.bounds;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        RebuildShape();
    }

    private void Start()
    {
        Sat2DManager.AddToSATResolver(this);
    }

    void OnValidate()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr) RebuildShape();
    }

    void LateUpdate()
    {
        // Por si cambian transform/animaciones/escala cada frame
        RebuildShape();
    }

    /// <summary>
    /// Reconstruye la geometría de la figura en coordenadas de mundo a partir del SpriteRenderer.
    /// </summary>
    public void RebuildShape()
    {
        if (sr == null || sr.sprite == null)
        {
            worldPolygon = null;
            circleRadius = 0f;
            return;
        }

        // 1) Obtenemos el rectángulo local del sprite (antes de escala y rotación)
        // sprite.bounds está en espacio local del sprite, centrado en el pivote del sprite.
        Bounds localSpriteBounds = sr.sprite.bounds; // en unidades del sprite (PPU)

        // 2) Convertimos a mundo con la matriz localToWorld
        // Construimos las 4 esquinas del rectángulo orientado (en mundo)
        Vector3 min = localSpriteBounds.min;
        Vector3 max = localSpriteBounds.max;

        Vector3[] localCorners = new Vector3[4];
        localCorners[0] = new Vector3(min.x, max.y, 0); // TL
        localCorners[1] = new Vector3(max.x, max.y, 0); // TR
        localCorners[2] = new Vector3(max.x, min.y, 0); // BR
        localCorners[3] = new Vector3(min.x, min.y, 0); // BL

        Matrix4x4 localToWorld = transform.localToWorldMatrix;

        Vector2[] orientedRect = new Vector2[4];
        for (int i = 0; i < 4; i++)
            orientedRect[i] = localToWorld.MultiplyPoint3x4(localCorners[i]);

        switch (shape)
        {
            case ShapeKind.Rectangle:
                worldPolygon = orientedRect;
                circleRadius = 0f;
                break;

            case ShapeKind.Triangle:
                worldPolygon = BuildTriangleFromOrientedRect(orientedRect, triangleLayout);
                circleRadius = 0f;
                break;

            case ShapeKind.Circle:
                BuildCircleFromOrientedRect(orientedRect, circleFit, out circleCenter, out circleRadius);
                worldPolygon = null;
                break;
        }
    }

    /// <summary>
    /// Devuelve un triángulo usando 3 esquinas del rectángulo orientado.
    /// </summary>
    private Vector2[] BuildTriangleFromOrientedRect(Vector2[] rect4, TriangleLayout layout)
    {
        // rect4 ordenado TL, TR, BR, BL
        switch (layout)
        {
            case TriangleLayout.Up:    return new[] { rect4[0], rect4[1], Mid(rect4[3], rect4[2]) }; // base arriba, vértice medio abajo
            case TriangleLayout.Down:  return new[] { rect4[3], rect4[2], Mid(rect4[0], rect4[1]) }; // base abajo, vértice medio arriba
            case TriangleLayout.Left:  return new[] { rect4[0], rect4[3], Mid(rect4[1], rect4[2]) }; // base izquierda
            case TriangleLayout.Right: return new[] { rect4[1], rect4[2], Mid(rect4[0], rect4[3]) }; // base derecha
        }
        // fallback
        return new[] { rect4[0], rect4[1], rect4[2] };
    }

    /// <summary>
    /// Círculo calculado a partir del rectángulo orientado.
    /// - Circunscrito: radio = mitad de la diagonal (cubre todo el sprite).
    /// - Inscrito: radio = menor half-extent del rectángulo orientado (cabe dentro).
    /// </summary>
    private void BuildCircleFromOrientedRect(Vector2[] rect4, CircleFit fit, out Vector2 center, out float radius)
    {
        center = (rect4[0] + rect4[1] + rect4[2] + rect4[3]) * 0.25f;

        // Ejes del rectángulo (bordes opuestos)
        float width  = ((rect4[1] - rect4[0]).magnitude + (rect4[2] - rect4[3]).magnitude) * 0.5f;
        float height = ((rect4[0] - rect4[3]).magnitude + (rect4[1] - rect4[2]).magnitude) * 0.5f;

        float rx = width * 0.5f;
        float ry = height * 0.5f;

        if (fit == CircleFit.Circumscribed)
            radius = Mathf.Sqrt(rx * rx + ry * ry); // mitad de la diagonal
        else
            radius = Mathf.Min(rx, ry); // cabe dentro
    }

    private Vector2 Mid(Vector2 a, Vector2 b) => (a + b) * 0.5f;

    // ===================== API pública para SATWorld =====================

    public bool IsCircle => shape == ShapeKind.Circle;

    public Vector2[] GetWorldPolygon()
    {
        if (shape == ShapeKind.Circle) return null;
        return worldPolygon;
    }

    public void GetCircle(out Vector2 c, out float r)
    {
        c = circleCenter;
        r = circleRadius;
    }

    // ===================== Gizmos =====================

    void OnDrawGizmosSelected() { if (drawGizmos) DrawGizmosInternal(); }
    void OnDrawGizmos() { if (drawGizmos) DrawGizmosInternal(); }

    private void DrawGizmosInternal()
    {
        if (!enabled || sr == null) return;

        Gizmos.color = gizmoColor;

        if (shape == ShapeKind.Circle)
        {
            // circle
            int segments = 48;
            Vector3 prev = circleCenter + new Vector2(circleRadius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float t = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 p = circleCenter + new Vector2(Mathf.Cos(t) * circleRadius, Mathf.Sin(t) * circleRadius);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
        else if (worldPolygon != null && worldPolygon.Length >= 3)
        {
            for (int i = 0; i < worldPolygon.Length; i++)
            {
                int j = (i + 1) % worldPolygon.Length;
                Gizmos.DrawLine(worldPolygon[i], worldPolygon[j]);
            }
        }

        // AABB del Sprite (útil para debug)
        var b = sr.bounds;
        Gizmos.color = new Color(1, 1, 1, 0.15f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
