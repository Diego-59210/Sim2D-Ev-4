using PUCV.PhysicEngine2D;
using UnityEngine;
using UnityEngine.Events;

public class CollisionEvents2D : MonoBehaviour, ICustomCollision
{
    public string targetTag;
    public UnityEvent enterEvent;
    public UnityEvent stayEvent;
    public UnityEvent exitEvent;

    public void OnInformCollisionEnter2D(CollisionInfo collisionInfo)
    {
        if (collisionInfo.otherCollider.gameObject.CompareTag(targetTag))
        {
            enterEvent?.Invoke();
        }
    }
    public void OnInformCollisionStay2D(CollisionInfo collisionInfo)
    {
        if (collisionInfo.otherCollider.gameObject.CompareTag(targetTag))
        {
            stayEvent?.Invoke();
        }
    }
    public void OnInformCollisionExit2D(CollisionInfo collisionInfo)
    {
        if (collisionInfo.otherCollider.gameObject.CompareTag(targetTag))
        {
            exitEvent?.Invoke();
        }
    }
}
