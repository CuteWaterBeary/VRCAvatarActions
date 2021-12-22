#if UNITY_EDITOR

namespace VRCAvatarActions
{
    public abstract partial class BaseActions
    {
        public enum TrackingTypeEnum
        {
            Generic = 1,
            ThreePoint = 3,
            FourPoint = 4,
            FullBody = 6,
        }
    }
}
#endif