using PUCV.PhysicEngine2D;
using UnityEngine;

public class BallMovement : MonoBehaviour
{
    [Header("Ball Settings")]
    public float launchForce = 10f;
    public float slowFactor = 0.5f;
    public float pushStrength = 10f;
    private CustomRigidbody2D _rigidbody;

    void Start()
    {
        _rigidbody = GetComponent<CustomRigidbody2D>();
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MoveBallToMouse();
        }
    }

    void MoveBallToMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector2 direction = (mousePos - transform.position).normalized;

        if (_rigidbody != null)
        {
            _rigidbody.velocity = direction * launchForce;
        }
    }

    /* public void OnInformCollisionEnter2D(CollisionInfo collisionInfo)
    {
        if (_rigidbody == null) return;

        _rigidbody.velocity *= slowFactor;

        if (collisionInfo.otherRigidbody == null) return;
        
        Vector2 ballVelocity = -_rigidbody.velocity;

        Vector2 impulse = ballVelocity.normalized * pushStrength;

        collisionInfo.otherRigidbody.velocity += impulse;
    }

    public void OnInformCollisionExit2D(CollisionInfo collisionInfo)
    {
        throw new System.NotImplementedException();
    } */
}