
namespace AgarmeServer.Zeroer
{
    public interface IWritableBuffer
    {
        /// <summary>
        /// 写入一定长度的未定义数据，并返回内存块的指针由你自己去赋值
        /// </summary>
        /// <param name="len">数据的长度</param>
        /// <returns>该未定义区域的指针</returns>
        unint WriteUndefined(unint len);
    }
}