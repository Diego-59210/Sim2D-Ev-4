using System;
using System.Collections.Generic;
using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class CustomCollider2D : MonoBehaviour
    {
        public enum ShapeType
        {
            Circle,
            Triangle,
            Rectangle
        }

        public ShapeType type = ShapeType.Rectangle;

        public Circle2D circleOverride;
        public Triangle2D triangleShape;

        private SpriteRenderer _sr;
        
        private IHasCollider _colliderListener;
        
        [NonSerialized]
        public CustomRigidbody2D rigidBody;

        public Vector2 Center => (Vector2) transform.position;
        public Quaternion Rot => transform.rotation;

        private bool _registered;
        private void Start()
        {
            RegisterToPhysicsManager();
            rigidBody = GetComponent<CustomRigidbody2D>();
            
        }
        
        
        private void RegisterToPhysicsManager()
        {
            if (_registered) return;
            PhysicsManager2D.RegisterCollider(this);
            
            _registered = true;
        }

        private void UnregisterFromPhysicsManager()
        {
            if (!_registered) return;
            PhysicsManager2D.UnregisterCollider(this);
            _registered = false;
        }

        // Radio del círculo en mundo
        public float CircleRadius
        {
            get
            {
                if (type != ShapeType.Circle) return 0f;
                if (circleOverride.radius > 0f) return circleOverride.radius;

                var size = _sr.sprite != null ? _sr.sprite.bounds.size : Vector3.one;
                var s = transform.lossyScale;
                float rx = Mathf.Abs(size.x * s.x) * 0.5f;
                float ry = Mathf.Abs(size.y * s.y) * 0.5f;
                return (rx + ry) * 0.5f;
            }
        }

        private SpriteRenderer GetSpriteRenderer()
        {
            if(_sr==null) _sr = GetComponent<SpriteRenderer>();
            return _sr;
        }

        // Half-extents del rectángulo (OBB)
        public Vector2 RectHalf
        {
            get
            {
                if (type != ShapeType.Rectangle) return Vector2.zero;

                var size = _sr.sprite != null ? (Vector2) _sr.sprite.bounds.size : Vector2.one;
                var s = (Vector2) transform.lossyScale;
                return new Vector2(Mathf.Abs(size.x * s.x) * 0.5f, Mathf.Abs(size.y * s.y) * 0.5f);
            }
        }

        public void Awake()
        {
            GetSpriteRenderer();
            _colliderListener = GetComponent<IHasCollider>();
        }
        
        public Rectangle2D GetRectWorldVerts()
        {
            Vector2 h = RectHalf;
            Vector2 c = Center;
            Vector2 right = transform.right;
            Vector2 up = transform.up;

            return new Rectangle2D(){
                RU = c + right * h.x + up * h.y, // top-right
                LU = c + -right * h.x + up * h.y, // top-left
                LD = c + -right * h.x - up * h.y, // bottom-left
                RD = c + right * h.x - up * h.y // bottom-right
            };
        }

        // Vértices triángulo en mundo (CCW)
        public Triangle2D GetTriangleWorldVerts()
        {
            var s = transform.lossyScale;
            Vector2 A = (Vector2) (Rot * Vector3.Scale(new Vector3(triangleShape.A.x, triangleShape.A.y, 0f), new Vector3(s.x, s.y, 1f))) +
                        Center;
            Vector2 B = (Vector2) (Rot * Vector3.Scale(new Vector3(triangleShape.B.x, triangleShape.B.y, 0f), new Vector3(s.x, s.y, 1f))) +
                        Center;
            Vector2 C = (Vector2) (Rot * Vector3.Scale(new Vector3(triangleShape.C.x, triangleShape.C.y, 0f), new Vector3(s.x, s.y, 1f))) +
                        Center;
            return new Triangle2D() { A = A, B = B, C = C };
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            GetSpriteRenderer();
            Gizmos.color = Color.cyan;
            switch (type)
            {
                case ShapeType.Circle:
                    UnityEditor.Handles.color = Color.cyan;
                    UnityEditor.Handles.DrawWireDisc(Center, Vector3.forward, CircleRadius);
                    break;
                case ShapeType.Rectangle:
                    var r = GetRectWorldVerts();
                    Gizmos.DrawLine(r.LU, r.RU);
                    Gizmos.DrawLine(r.RU, r.RD);
                    Gizmos.DrawLine(r.RD, r.LD);
                    Gizmos.DrawLine(r.LD, r.LU);
                    break;
                case ShapeType.Triangle:
                    var t = GetTriangleWorldVerts();
                    Gizmos.DrawLine(t.A, t.B);
                    Gizmos.DrawLine(t.B, t.C);
                    Gizmos.DrawLine(t.C, t.A);
                    break;
            }
        }
#endif
        public IList<Vector2> GetPolygonVertices()
        {
            switch (type)
            {
                case ShapeType.Circle:
                    return null;
                case ShapeType.Rectangle:
                    var r = GetRectWorldVerts();
                    return new List<Vector2>() {r.LU, r.LD, r.RD, r.RU};
                case ShapeType.Triangle:
                    var t = GetTriangleWorldVerts();
                    return new List<Vector2>() {t.A, t.B, t.C};
                default:
                    return null;
            }
        }

        public void InformOnCollisionEnter2D(CollisionInfo collisionInfo)
        {
            _colliderListener?.OnInformCollisionEnter2D(collisionInfo);
        }

        public void AddRigidbodyReference(CustomRigidbody2D customRigidbody2D)
        {
            rigidBody = customRigidbody2D;
        }
    }

    public class Shape2D
    {
        public Sat2DShape.ShapeKind kind;
    }
    
    [Serializable]
    public class Circle2D : Shape2D
    {
        public Vector2 center;
        public float radius;

        public Circle2D()
        {
            center = Vector2.zero;
            radius = 0f;
        }
    }
    
    [Serializable]
    public class Rectangle2D : Shape2D
    {
        //Left Up, Left Down, Right Up, Right Down
        public Vector2 LU;
        public Vector2 LD;
        public Vector2 RU;
        public Vector2 RD;
        
        public Rectangle2D()
        {
            LU = new Vector2(-1f, 1f);
            LU = new Vector2(-1f, -1f);
            LU = new Vector2(1f, 1f);
            LU = new Vector2(-1f, 1f);
        }
    }
    
    [Serializable]
    public class Triangle2D : Shape2D
    {
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;
        
        public Triangle2D()
        {
            A = new Vector2(-0.5f, -0.3f);
            B = new Vector2(-1f, -1f);
            C = new Vector2(1f, -1f);
        }
    }
}
