#if UNITY_EDITOR
namespace VRCAvatarActions
{
    public abstract class NonMenuActions : BaseActions
    {
        public abstract void Build(ActionsBuilder builder, MenuActions.MenuAction parentAction);
    }
}
#endif