using System;
using System.IO;
using System.Text;


    /// <summary>
    /// Much faster reader that only reads memory buffer
    /// </summary>
    internal unsafe class FastBigEndianReader : IDataReader, IDisposable
    {
        private long position = 0;
        private readonly byte[] buffer;

        public byte[] Buffer
        {
            get { return buffer; }
        }

        public FastBigEndianReader(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public byte ReadByte()
        {
            fixed (byte* pbyte = &buffer[position++])
            {
                return *pbyte;
            }
        }

        public sbyte ReadSByte()
        {
            fixed (byte* pbyte = &buffer[position++])
            {
                return (sbyte)*pbyte;
            }
        }

        public long Position
        {
            get { return position; }
        }

        public long BytesAvailable
        {
            get { return buffer.Length - position; }
        }

        public short ReadShort()
        {
            var position = this.position;
            this.position += 2;
            fixed (byte* pbyte = &buffer[position])
            {
                return (short) ((*pbyte << 8) | (*(pbyte + 1)));
            }
        }

        public int ReadInt()
        {
            var position = this.position;
            this.position += 4;
            fixed (byte* pbyte = &buffer[position])
            {
                return ( *pbyte << 24 ) | ( *( pbyte + 1 ) << 16 ) | ( *( pbyte + 2 ) << 8 ) | ( *( pbyte + 3 ) );
            }
        }

        public long ReadLong()
        {
            var position = this.position;
            this.position += 8;
            fixed (byte* pbyte = &buffer[position])
            {
                int i1 = ( *pbyte << 24 ) | ( *( pbyte + 1 ) << 16 ) | ( *( pbyte + 2 ) << 8 ) | ( *( pbyte + 3 ) );
                int i2  = ( *( pbyte + 4 ) << 24 ) | ( *( pbyte + 5 ) << 16 ) | ( *( pbyte + 6 ) << 8 ) | ( *( pbyte + 7 ) );
                return (uint)i2 | ( (long)i1 << 32 );
            }
        }

        public ushort ReadUShort()
        {
            return (ushort)ReadShort();
        }

        public uint ReadUInt()
        {
            return (uint)ReadInt();
        }

        public ulong ReadULong()
        {
            return (ulong)ReadLong();
        }

        public byte[] ReadBytes(int n)
        {
            var dst = new byte[n];
            fixed (byte* pSrc = &buffer[position], pDst = dst)
            {
                byte* ps = pSrc;
                byte* pd = pDst;

                // Loop over the count in blocks of 4 bytes, copying an integer (4 bytes) at a time:
                for (int i = 0; i < n / 4; i++)
                {
                    *( (int*)pd ) = *( (int*)ps );
                    pd += 4;
                    ps += 4;
                }

                // Complete the copy by moving any bytes that weren't moved in blocks of 4:
                for (int i = 0; i < n % 4; i++)
                {
                    *pd = *ps;
                    pd++;
                    ps++;
                }
            }

            position += n;

            return dst;
        }

        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public char ReadChar()
        {
            return (char)ReadShort();
        }

        public float ReadFloat()
        {
            int val = ReadInt();
            return *(float*)&val;
        }

        public double ReadDouble()
        {
            long val = ReadLong();
            return *(double*)&val;
        }

        public string ReadUTF()
        {
            ushort length = ReadUShort();

            byte[] bytes = ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public string ReadUTFBytes(ushort len)
        {
            byte[] bytes = ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        #region Custom Methods

        private const int INT_SIZE = 32;
        private const int SHORT_SIZE = 16;
        private const int SHORT_MIN_VALUE = -32768;
        private const int SHORT_MAX_VALUE = 32767;
        private const int UNSIGNED_SHORT_MAX_VALUE = 65536;
        private const int CHUNCK_BIT_SIZE = 7;

        private const int MASK_1 = 128;
        private const int MASK_0 = 127;


        public int ReadVarInt()
        {
            var local_4 = 0;
            var local_1 = 0;
            var local_2 = 0;
            var local_3 = false;

            while (local_2 < INT_SIZE)
            {
                local_4 = ReadByte();
                local_3 = (local_4 & MASK_1) == MASK_1;

                if (local_2 > 0)
                {
                    local_1 += ((local_4 & MASK_1) << local_2);
                }
                else
                {
                    local_1 += (local_4 & MASK_0);
                }

                local_2 += CHUNCK_BIT_SIZE;

                if (!local_3)
                {
                    return local_1;
                }
            }

            throw new System.Exception("Too much data");
        }

        public uint ReadVarUhInt()
        {
            return (uint)(ReadVarInt());
        }

        public int ReadVarShort()
        {
            var local_4 = 0;
            var local_1 = 0;
            var local_2 = 0;
            var local_3 = false;

            while (local_2 < SHORT_SIZE)
            {
                local_4 = ReadByte();
                local_3 = (local_4 & MASK_1) == MASK_1;

                if (local_2 > 0)
                {
                    local_1 += ((local_4 & MASK_1) << local_2);
                }
                else
                {
                    local_1 += (local_4 & MASK_0);
                }

                local_2 += CHUNCK_BIT_SIZE;

                if (!local_3)
                {
                    if (local_1 > SHORT_MAX_VALUE)
                    {
                        local_1 = local_1 - UNSIGNED_SHORT_MAX_VALUE;
                    }
                    return local_1;
                }
            }

            throw new System.Exception("Too much data");
        }

        public uint ReadVarUhShort()
        {
            return (uint)(ReadVarShort());
        }

        public double ReadVarLong()
        {
            return ReadInt();
        }

        public double ReadVarUhLong()
        {
            return ReadUInt();
        }

        #endregion

        public void Seek(int offset, SeekOrigin seekOrigin)
        {
            if (seekOrigin == SeekOrigin.Begin)
                position = offset;
            else if (seekOrigin == SeekOrigin.End)
                position = buffer.Length + offset;
            else if (seekOrigin == SeekOrigin.Current)
                position += offset;
        }

        public void SkipBytes(int n)
        {
            position += n;
        }

        public void Dispose()
        {
            
        }
    }
