using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PUCV.PhysicEngine2D
{
    public class CustomRigidbody2D : MonoBehaviour
    {
        public Vector2 velocity;
        public float mass = 1f;
        public float stopThreshold = 0.15f;

        public bool useStopThreshold = true;
        public bool useGravity = true;
        public float gravityScale = 1f;
        private CustomCollider2D _customCollider;

        //Detener el cuerpo para evitar pequeños movimientos.
        public void ApplyStopThreshold()
        {
            if (!useStopThreshold) return;

            if (velocity.sqrMagnitude < stopThreshold * stopThreshold)
            {
                useGravity = false; 
                velocity = Vector2.zero;
            }
        }

        public Vector2 GetWorldPosition()
        {
            return transform.position;
        }
        
        public void SetWoldPosition(Vector2 newPos)
        {
            transform.position = newPos;
        }

        public CustomCollider2D GetCollider()
        {
            return _customCollider;
        }
    }
}
