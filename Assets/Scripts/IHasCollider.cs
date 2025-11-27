namespace PUCV.PhysicEngine2D
{
    public interface IHasCollider
    {
        void OnInformCollisionEnter2D(CollisionInfo collisionInfo);
    }
}