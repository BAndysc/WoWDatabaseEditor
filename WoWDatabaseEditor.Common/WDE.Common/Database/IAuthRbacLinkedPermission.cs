namespace WDE.Common.Database
{
    public interface IAuthRbacLinkedPermission
    {
        uint Id { get; }
        uint LinkedId { get; }
    }
}