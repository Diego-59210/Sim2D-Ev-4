using PUCV.PhysicEngine2D;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEvents2D : MonoBehaviour, ICustomTrigger
{
    public string targetTag;
    public UnityEvent enterEvent;
    public UnityEvent stayEvent;
    public UnityEvent exitEvent;

    public void OnInformTriggerEnter2D(CollisionInfo collisionInfo)
    {
        if (collisionInfo.otherCollider.gameObject.CompareTag(targetTag))
        {
            enterEvent?.Invoke();
        }
    }
    public void OnInformTriggerStay2D(CollisionInfo collisionInfo)
    {
        if (collisionInfo.otherCollider.gameObject.CompareTag(targetTag))
        {
            stayEvent?.Invoke();
        }
    }
    public void OnInformTriggerExit2D(CollisionInfo collisionInfo)
    {
        if (collisionInfo.otherCollider.gameObject.CompareTag(targetTag))
        {
            exitEvent?.Invoke();
        }
    }
}
