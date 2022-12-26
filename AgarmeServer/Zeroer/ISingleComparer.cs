namespace AgarmeServer.Zeroer
{
    public interface ISingleComparer<in T>
    {
        int Compare(T x);
    }
}