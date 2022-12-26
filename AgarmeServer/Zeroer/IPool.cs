namespace AgarmeServer.Zeroer
{
    public interface IPool<TContent>
    {
        TContent Rent();

        void Return(TContent content);
    }
}
