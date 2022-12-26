namespace AgarmeServer.Others
{
    [Obsolete("this helper class has been entirely abandoned")]
    public class LockHelper
    {
        public SpinLock CellsLock = new SpinLock();
        public SpinLock ClientLock = new SpinLock();

        public void CellExit(ref bool is_cell_locked)
        {
            if (is_cell_locked)
                CellsLock.Exit();
        }

        public void ClientExit(ref bool is_client_locked)
        {
            if (is_client_locked)
                ClientLock.Exit();
        }

        public void ClientEnter(ref bool is_client_locked) { ClientLock.Enter(ref is_client_locked); }
        public void CellEnter(ref bool is_cell_locked) { CellsLock.Enter(ref is_cell_locked); }

    }
}
