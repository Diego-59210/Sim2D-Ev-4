using System.Collections.Generic;
using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    [DefaultExecutionOrder(-100)]
    public class PhysicsManager2D : MonoBehaviour
    {
        private static PhysicsManager2D _instance;
        public static PhysicsManager2D Instance => _instance;

        [Header("World Settings")]
        public Vector2 globalGravity = new Vector2(0f, -9.81f);

        [Header("Collision Settings")]
        [Range(0f, 1f)] public float positionalCorrectionPercent = 0.8f;
        public float penetrationSlop = 0.01f;

        private readonly List<CustomCollider2D> _colliders = new List<CustomCollider2D>();
        private List<InternalCollisionInfo> _currentCollisionList = new List<InternalCollisionInfo>();
        private List<InternalCollisionInfo> _prevCollisionList = new List<InternalCollisionInfo>();
        private HashSet<(CustomCollider2D, CustomCollider2D)> _currentPairs = new HashSet<(CustomCollider2D, CustomCollider2D)>();
        private HashSet<(CustomCollider2D, CustomCollider2D)> _previousPairs = new HashSet<(CustomCollider2D, CustomCollider2D)>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public static void RegisterCollider(CustomCollider2D c)
        {
            if (_instance == null) return;
            if (c == null) return;
            if (!_instance._colliders.Contains(c))
                _instance._colliders.Add(c);
        }

        public static void UnregisterCollider(CustomCollider2D c)
        {
            if (_instance == null) return;
            if (c == null) return;
            _instance._colliders.Remove(c);
        }

        // -------------------------------------------------------
        // FIXED UPDATE LOOP
        // -------------------------------------------------------
        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            if (_colliders.Count == 0) return;

            // 0) Clear forces on each rigidbody (start-of-step)
            for (int i = 0; i < _colliders.Count; i++)
            {
                var rb = _colliders[i].rigidBody;
                if (rb != null) rb.ClearForces();
            }

            // 1) Apply global uniform gravity
            ApplyUniformGravity();

            // 2) Integrate velocities (semi-implicit Euler)
            for (int i = 0; i < _colliders.Count; i++)
            {
                var rb = _colliders[i].rigidBody;
                if (rb == null) continue;
                rb.IntegrateVelocity(dt);
            }

            // 3) Collision detection (SAT)
            CalculateCollisions();

            // 4) Resolve collisions (impulse + positional correction)
            ResolveCollisions(dt);

            // 5) Integrate positions
            for (int i = 0; i < _colliders.Count; i++)
            {
                var rb = _colliders[i].rigidBody;
                if (rb == null) continue;
                rb.IntegratePosition(dt);
            }


            // -----------------------------
            // 6) Build collision pairs for STAY logic
            // -----------------------------

            // Save previous frame’s pairs
            _previousPairs.Clear();
            foreach (var p in _currentPairs)
                _previousPairs.Add(p);

            // Rebuild current pairs from this frame
            _currentPairs.Clear();
            foreach (var c in _currentCollisionList)
            {
                var a = c.bodyACollider;
                var b = c.bodyBCollider;
                if (a == null || b == null) continue;

                // deterministic ordering
                if (a.GetInstanceID() < b.GetInstanceID())
                    _currentPairs.Add((a, b));
                else
                    _currentPairs.Add((b, a));
            }


            // ------------------------------------
            // 7) Inform events → Enter + Stay
            // ------------------------------------
            InformCollisionAndTriggerEnter();


            // ------------------------------------
            // 8) Inform events → Exit
            // ------------------------------------
            InformCollisionAndTriggerExit();


            // ------------------------------------
            // 9) Copy current collisions into prev list
            // ------------------------------------
            _prevCollisionList.Clear();
            _prevCollisionList.AddRange(_currentCollisionList);
        }

        // -------------------------------------------------------
        // GRAVITY
        // -------------------------------------------------------
        private void ApplyUniformGravity()
        {
            foreach (var col in _colliders)
            {
                var rb = col.rigidBody;
                if (rb == null) continue;
                if (rb.IsStatic) continue;

                rb.AddForce(globalGravity * rb.mass);
            }
        }

        // -------------------------------------------------------
        // COLLISION DETECTION (SAT)
        // -------------------------------------------------------
        private void CalculateCollisions()
        {
            _currentCollisionList.Clear();

            var detected = SAT2DMath.DetectCollisions(_colliders);
            if (detected != null && detected.Count > 0)
                _currentCollisionList.AddRange(detected);

            // Was this colliding last frame?
            foreach (var cur in _currentCollisionList)
            {
                cur.wasCollidedLastFrame = false;

                foreach (var prev in _prevCollisionList)
                {
                    bool same =
                        (prev.bodyACollider == cur.bodyACollider && prev.bodyBCollider == cur.bodyBCollider) ||
                        (prev.bodyACollider == cur.bodyBCollider && prev.bodyBCollider == cur.bodyACollider);

                    if (same)
                    {
                        cur.wasCollidedLastFrame = true;
                        break;
                    }
                }
            }
        }

        // -------------------------------------------------------
        // RESOLUTION (SKIPS TRIGGERS)
        // -------------------------------------------------------
        private void ResolveCollisions(float dt)
        {
            foreach (var info in _currentCollisionList)
            {
                // Skip triggers
                if (info.bodyACollider.isTrigger || info.bodyBCollider.isTrigger)
                    continue;

                var a = info.bodyARigidbody;
                var b = info.bodyBRigidbody;

                Vector2 mtv = info.mtvB - info.mtvA;
                float penetration = mtv.magnitude;

                Vector2 normal =
                    mtv.sqrMagnitude > 1e-9f ?
                    mtv.normalized :
                    info.contactNormalBA.normalized;

                float invMassA = a != null ? a.InverseMass : 0f;
                float invMassB = b != null ? b.InverseMass : 0f;
                float invMassSum = invMassA + invMassB;

                // --- positional correction ---
                if (invMassSum > 0f && penetration > penetrationSlop)
                {
                    float correctionMag = (Mathf.Max(penetration - penetrationSlop, 0f) /
                                           invMassSum) * positionalCorrectionPercent;
                    Vector2 correction = normal * correctionMag;

                    if (a != null && !a.IsStatic) a.SetWorldPosition(a.GetWorldPosition() - correction * invMassA);
                    if (b != null && !b.IsStatic) b.SetWorldPosition(b.GetWorldPosition() + correction * invMassB);
                }

                // --- impulses ---
                Vector2 velA = a != null ? a.velocity : Vector2.zero;
                Vector2 velB = b != null ? b.velocity : Vector2.zero;
                Vector2 relVel = velB - velA;

                float velAlongNormal = Vector2.Dot(relVel, normal);
                if (velAlongNormal > 0f) continue;

                float e = 0f;
                if (a != null) e = Mathf.Max(e, a.restitution);
                if (b != null) e = Mathf.Max(e, b.restitution);

                float j = -(1f + e) * velAlongNormal;
                j /= invMassSum;

                Vector2 impulse = j * normal;

                if (a != null && !a.IsStatic) a.velocity -= impulse * invMassA;
                if (b != null && !b.IsStatic) b.velocity += impulse * invMassB;

                // --- friction ---
                relVel = (b != null ? b.velocity : Vector2.zero) - (a != null ? a.velocity : Vector2.zero);

                Vector2 tangent = relVel - Vector2.Dot(relVel, normal) * normal;
                if (tangent.sqrMagnitude > 1e-9f) tangent.Normalize();

                float jt = -Vector2.Dot(relVel, tangent);
                jt /= invMassSum;

                float mu = 0f;
                if (a != null) mu = Mathf.Max(mu, a.friction);
                if (b != null) mu = Mathf.Max(mu, b.friction);

                float jtClamped = Mathf.Clamp(jt, -j * mu, j * mu);
                Vector2 frictionImpulse = jtClamped * tangent;

                if (a != null && !a.IsStatic) a.velocity -= frictionImpulse * invMassA;
                if (b != null && !b.IsStatic) b.velocity += frictionImpulse * invMassB;
            }
        }

        // -------------------------------------------------------
        // EVENTS (COLLISION + TRIGGER)
        // -------------------------------------------------------
        private void InformCollisionAndTriggerEnter()
        {
            foreach (var info in _currentCollisionList)
            {
                bool isTrigger = info.bodyACollider.isTrigger || info.bodyBCollider.isTrigger;
                bool isNewPair = !_previousPairs.Contains(SortPair(info.bodyACollider, info.bodyBCollider));

                var aInfo = info.GetCollInfoForBodyA();
                var bInfo = info.GetCollInfoForBodyB();

                if (isNewPair)
                {
                    if (isTrigger)
                    {
                        info.bodyACollider?.InformOnTriggerEnter2D(aInfo);
                        info.bodyBCollider?.InformOnTriggerEnter2D(bInfo);
                    }
                    else
                    {
                        info.bodyACollider?.InformOnCollisionEnter2D(aInfo);
                        info.bodyBCollider?.InformOnCollisionEnter2D(bInfo);
                    }
                }
                else
                {
                    if (isTrigger)
                    {
                        info.bodyACollider?.InformOnTriggerStay2D(aInfo);
                        info.bodyBCollider?.InformOnTriggerStay2D(bInfo);
                    }
                    else
                    {
                        info.bodyACollider?.InformOnCollisionStay2D(aInfo);
                        info.bodyBCollider?.InformOnCollisionStay2D(bInfo);
                    }
                }
            }
        }

        private void InformCollisionAndTriggerExit()
        {
            foreach (var pair in _previousPairs)
            {
                if (_currentPairs.Contains(pair))
                    continue; // still colliding → stay already sent

                // Find corresponding InternalCollisionInfo (only for Exit data)
                InternalCollisionInfo exitInfo = null;
                foreach (var old in _prevCollisionList)
                {
                    if (PairMatches(pair, old))
                    {
                        exitInfo = old;
                        break;
                    }
                }
                if (exitInfo == null) continue;

                bool isTrigger = exitInfo.bodyACollider.isTrigger || exitInfo.bodyBCollider.isTrigger;

                var aInfo = exitInfo.GetCollInfoForBodyA();
                var bInfo = exitInfo.GetCollInfoForBodyB();

                if (isTrigger)
                {
                    exitInfo.bodyACollider?.InformOnTriggerExit2D(aInfo);
                    exitInfo.bodyBCollider?.InformOnTriggerExit2D(bInfo);
                }
                else
                {
                    exitInfo.bodyACollider?.InformOnCollisionExit2D(aInfo);
                    exitInfo.bodyBCollider?.InformOnCollisionExit2D(bInfo);
                }
            }
        }
        private (CustomCollider2D, CustomCollider2D) SortPair(CustomCollider2D a, CustomCollider2D b)
        {
            return (a.GetInstanceID() < b.GetInstanceID()) ? (a, b) : (b, a);
        }

        private bool PairMatches((CustomCollider2D, CustomCollider2D) pair, InternalCollisionInfo info)
        {
            return (pair.Item1 == info.bodyACollider && pair.Item2 == info.bodyBCollider) ||
                (pair.Item1 == info.bodyBCollider && pair.Item2 == info.bodyACollider);
        }
    }
    
    // -------------------------
    // Collision representation classes (kept compatible with existing SAT2DMath)
    // -------------------------
    public class InternalCollisionInfo
    {
        public CustomCollider2D bodyACollider;
        public CustomRigidbody2D bodyARigidbody;
        public CustomCollider2D bodyBCollider;
        public CustomRigidbody2D bodyBRigidbody;

        public bool wasCollidedLastFrame;

        // MTV info (SAT2DMath currently fills mtvA and mtvB)
        public bool hasMTV;
        public Vector2 mtvA;
        public Vector2 mtvB;

        // contact
        public Vector2 contactPoint;
        // contact normals (kept as before)
        public Vector2 contactNormalAB; // contact normal seen from A (A -> ?)
        public Vector2 contactNormalBA; // contact normal seen from B

        public int missingFrames;

        public InternalCollisionInfo(CustomCollider2D colA, CustomCollider2D colB, Vector2 point, Vector2 normal)
        {
            bodyACollider = colA;
            bodyARigidbody = colA?.rigidBody;
            bodyBCollider = colB;
            bodyBRigidbody = colB?.rigidBody;
            contactPoint = point;
            // SAT2DMath historically passed 'normal' as A->B: to keep compatibility we keep the same assignments
            contactNormalAB = -normal;
            contactNormalBA = normal;
        }

        public CollisionInfo GetCollInfoForBodyA()
        {
            return new CollisionInfo()
            {
                otherCollider = bodyBCollider,
                otherRigidbody = bodyBRigidbody,
                contactPoint = contactPoint,
                contactNormal = contactNormalAB,
                mtv = mtvA
            };
        }

        public CollisionInfo GetCollInfoForBodyB()
        {
            return new CollisionInfo()
            {
                otherCollider = bodyACollider,
                otherRigidbody = bodyARigidbody,
                contactPoint = contactPoint,
                contactNormal = contactNormalBA,
                mtv = mtvB
            };
        }
    }

    public class CollisionInfo
    {
        public CustomCollider2D otherCollider;
        public CustomRigidbody2D otherRigidbody;

        public Vector2 contactPoint;
        public Vector2 contactNormal;
        public Vector2 mtv;
    }
}