using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PUCV.PhysicEngine2D
{
    public class CustomRigidbody2D : MonoBehaviour
    {
        public float mass = 1f;
        [Range(0f, 1f)] public float restitution = 0.2f; 
        [Range(0f, 1f)] public float friction = 0.2f;

        // runtime
        [HideInInspector] public Vector2 velocity;
        [HideInInspector] public Vector2 accumulatedForces;
        private float _invMass;

        public bool IsStatic => mass <= 0f;

        void Awake()
        {
            _invMass = mass > 0f ? 1f / mass : 0f;
        }

        public float InverseMass => _invMass;

        public void AddForce(Vector2 f)
        {
            accumulatedForces += f;
        }

        public void ClearForces()
        {
            accumulatedForces = Vector2.zero;
        }

        public void IntegrateVelocity(float dt)
        {
            if (IsStatic) return;
            Vector2 accel = accumulatedForces * _invMass;
            velocity += accel * dt;
        }

        public void IntegratePosition(float dt)
        {
            if (IsStatic) return;
            transform.position = (Vector2)transform.position + velocity * dt;
        }

        public Vector2 GetWorldPosition() => (Vector2)transform.position;
        public void SetWorldPosition(Vector2 p) => transform.position = p;
    }
}