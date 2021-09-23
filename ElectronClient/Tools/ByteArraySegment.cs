namespace ElectronMS
{
    internal sealed class ByteArraySegment
    {
        private byte[] mBuffer = null;
        private int mStart = 0;
        private int mLength = 0;

        public ByteArraySegment(byte[] pBuffer)
        {
            mBuffer = pBuffer;
            mLength = mBuffer.Length;
        }
        public ByteArraySegment(byte[] pBuffer, int pStart, int pLength)
        {
            mBuffer = pBuffer;
            mStart = pStart;
            mLength = pLength;
        }

        public byte[] Buffer { get { return mBuffer; } }
        public int Start { get { return mStart; } }
        public int Length { get { return mLength; } }
        public bool Advance(int pLength)
        {
            mStart += pLength;
            mLength -= pLength;
            if (mLength <= 0) return true;
            return false;
        }
    }
}
