namespace PUCV.PhysicEngine2D
{
    public interface ICustomTrigger
    {
        void OnInformTriggerEnter2D(CollisionInfo info);
        void OnInformTriggerStay2D(CollisionInfo info);
        void OnInformTriggerExit2D(CollisionInfo info);
    }
}

