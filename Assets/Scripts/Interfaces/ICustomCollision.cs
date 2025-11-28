namespace PUCV.PhysicEngine2D
{
    public interface ICustomCollision 
    {
        void OnInformCollisionEnter2D(CollisionInfo collisionInfo);
        void OnInformCollisionStay2D(CollisionInfo collisionInfo);
        void OnInformCollisionExit2D(CollisionInfo collisionInfo);
    }
}

