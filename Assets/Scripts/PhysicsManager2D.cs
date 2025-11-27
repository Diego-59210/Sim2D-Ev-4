using System.Collections.Generic;
using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class PhysicsManager2D : MonoBehaviour
    {
        private static PhysicsManager2D _instance;
        private bool _registered;
        private List<CustomCollider2D> _colliders = new List<CustomCollider2D>();
        public Vector2 globalGravity = new Vector2(0, -9.81f);
        
        //Per FixedUpdateList
        private List<InternalCollisionInfo> _currentCollisionList = new List<InternalCollisionInfo>();

        private void Awake()
        {
            if (_instance != null)
            {
                DestroyImmediate(this);
                return;
            }
            
            //Singleton
            _instance = this;
            DontDestroyOnLoad(_instance);
        }

        public static void RegisterCollider(CustomCollider2D customCollider2D)
        {
            if(_instance) _instance._colliders.Add(customCollider2D);
        }

        public static void UnregisterCollider(CustomCollider2D customCollider2D)
        {
            if(_instance) _instance._colliders.Remove(customCollider2D);
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            StepApplyGravity(deltaTime);
            StepCalculateCollisions(deltaTime);
            StepApplyMTVAndReflectionToRigidbodies(deltaTime);
            StepApplyMovementToRigidbodies(deltaTime);
            StepInformCollisions(deltaTime);
        }
        private void StepApplyGravity(float deltaTime)
        {
            foreach (var col in _colliders)
            {
                CustomRigidbody2D rb = col.rigidBody;
                if (rb == null) continue;
                if (!rb.useGravity) continue;

                // a = g * gravityScale
                Vector2 acceleration = globalGravity * rb.gravityScale;

                // v = v + a dt
                rb.velocity += acceleration * deltaTime;
            }
        }

        private void StepApplyMTVAndReflectionToRigidbodies(float deltaTime)
        {
            
            foreach (var currCollisionInfo in _currentCollisionList)
            {
                var customRigidbody2DA = currCollisionInfo.bodyARigidbody;
                var customRigidbody2DB = currCollisionInfo.bodyBRigidbody;
                //Move rigidbodies according to MTV
                
                if (currCollisionInfo.hasMTV)
                {
                    if (customRigidbody2DA)
                    {
                        Vector2 position = customRigidbody2DA.GetWorldPosition();
                        position += currCollisionInfo.mtvA;
                        customRigidbody2DA.SetWoldPosition(position);
                    }
                    if (customRigidbody2DB)
                    {
                        Vector2 position = customRigidbody2DB.GetWorldPosition();
                        position += currCollisionInfo.mtvB;
                        customRigidbody2DB.SetWoldPosition(position);
                    }
                }
                
                float restitution = 1f; 

                if (customRigidbody2DA)
                {
                    Vector2 v = customRigidbody2DA.velocity;
                    Vector2 n = currCollisionInfo.contactNormalAB.normalized;

                    Vector2 reflected = v - 2f * Vector2.Dot(v, n) * n;
                    customRigidbody2DA.velocity = reflected * restitution;
                }

                if (customRigidbody2DB)
                {
                    Vector2 v = customRigidbody2DB.velocity;
                    Vector2 n = currCollisionInfo.contactNormalBA.normalized;

                    Vector2 reflected = v - 2f * Vector2.Dot(v, n) * n;
                    customRigidbody2DB.velocity = reflected * restitution;
                }
            }
        }

        private void StepCalculateCollisions(float deltaTime)
        {
            //var currCollisionList = SimpleCollisionMath2D.DetectCollisions(_colliders);
            var currCollisionList = SAT2DMath.DetectCollisions(_colliders);
            
            //Check if collision was present last frame
            foreach (var currCollisionInfo in currCollisionList)
            {
                currCollisionInfo.wasCollidedLastFrame = false;
                foreach (var prevCollisionInfo in _currentCollisionList)
                {
                    if ((prevCollisionInfo.bodyACollider == currCollisionInfo.bodyACollider &&
                         prevCollisionInfo.bodyBCollider == currCollisionInfo.bodyBCollider) ||
                        (prevCollisionInfo.bodyACollider == currCollisionInfo.bodyBCollider &&
                         prevCollisionInfo.bodyBCollider == currCollisionInfo.bodyACollider))
                    {
                        currCollisionInfo.wasCollidedLastFrame = true;
                        break;
                    }
                }
            }

            _currentCollisionList = currCollisionList;
        }
        
        private void StepInformCollisions(float deltaTime)
        {
            foreach (var currCollisionInfo in _currentCollisionList)
            {
                if (currCollisionInfo.wasCollidedLastFrame) continue;
                CollisionInfo a = currCollisionInfo.GetCollInfoForBodyA();
                CollisionInfo b = currCollisionInfo.GetCollInfoForBodyB();
                currCollisionInfo.bodyACollider.InformOnCollisionEnter2D(a);
                currCollisionInfo.bodyBCollider.InformOnCollisionEnter2D(b);
            }
        }

        void StepApplyMovementToRigidbodies(float deltaTime)
        {
            if (_colliders == null || _colliders.Count == 0) return;
            foreach (CustomCollider2D collider in _colliders)
            {
                CustomRigidbody2D rigidbody = collider.rigidBody;
                if (rigidbody == null) continue;
                Vector2 rigidbodyPos = rigidbody.GetWorldPosition();
                rigidbodyPos += rigidbody.velocity*deltaTime;
                rigidbody.SetWoldPosition(rigidbodyPos);
                rigidbody.ApplyStopThreshold();
            }
        }
    }
    
    public class InternalCollisionInfo
    {
        
        public CustomCollider2D bodyACollider;
        public CustomRigidbody2D bodyARigidbody;
        public CustomCollider2D bodyBCollider;
        public CustomRigidbody2D bodyBRigidbody;
        public bool wasCollidedLastFrame;
        //Minimum Translation Vector
        public bool hasMTV;
        public Vector2 mtvA;
        public Vector2 mtvB;

        public Vector2 contactPoint;
        public Vector2 contactNormalAB;
        public Vector2 contactNormalBA;

        public InternalCollisionInfo(
            CustomCollider2D colA, 
            CustomCollider2D colB, 
            Vector2 point, 
            Vector2 normal
            )
        {
            bodyACollider = colA;
            bodyARigidbody = colA.rigidBody;
            bodyBCollider = colB;
            bodyBRigidbody = colB.rigidBody;
            contactPoint = point;
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
                contactNormal = contactNormalAB
            };
        }

        public CollisionInfo GetCollInfoForBodyB()
        {
            return new CollisionInfo()
            {
                otherCollider = bodyACollider,
                otherRigidbody = bodyARigidbody,
                contactPoint = contactPoint,
                contactNormal = contactNormalBA
            };
        }
    }

    public class CollisionInfo
    {
        public CustomCollider2D otherCollider;
        public CustomRigidbody2D otherRigidbody;

        public Vector2 contactPoint;
        public Vector2 contactNormal;
    }
}

