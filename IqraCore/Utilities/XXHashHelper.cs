using System.IO.Hashing;
using System.Text;

namespace IqraCore.Utilities
{
    public static class XXHashHelper
    {
        public static ulong ComputeHashInUlong(string input)
        {
            return XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(input), 0);
        }

        public static byte[] ComputeHashInBytes(string input)
        {
            return XxHash64.Hash(Encoding.UTF8.GetBytes(input), 0);
        }
    }
}