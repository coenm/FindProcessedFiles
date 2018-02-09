﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ExifToolWrapper
{
    public class ExifToolStayOpenStream : Stream
    {
        private readonly Encoding _encoding;
        private readonly byte[] _cache;
        private const int OneMb = 1024 * 1024;
        private int _index;
        private const string Prefix = "\r\n{ready";
        private const string Suffix = "}";
        private readonly byte[] _endOfMessageSequenceStart;
        private readonly byte[] _endOfMessageSequenceEnd;

        public ExifToolStayOpenStream(Encoding encoding)
        {
            _encoding = encoding ?? new UTF8Encoding();
            _cache = new byte[OneMb];
            _index = 0;
            _endOfMessageSequenceStart = Encoding.ASCII.GetBytes(Prefix);
            _endOfMessageSequenceEnd = Encoding.ASCII.GetBytes(Suffix);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                return;
            if (count == 0)
                return;
            if (offset + count > buffer.Length)
                return;

            if (count > OneMb - _index)
                throw new ArgumentOutOfRangeException();

            Array.Copy(buffer, 0, _cache, _index, count);
            _index += count;

            var lastEndIndex = 0;

            for (var i = 0; i < _index - 1; i++)
            {
                var key = "";

                var j = 0;
                while (j < _endOfMessageSequenceStart.Length && _cache[i + j] == _endOfMessageSequenceStart[j])
                    j++;

                if (j != _endOfMessageSequenceStart.Length)
                    continue;

                var content = _encoding.GetString(_cache, lastEndIndex, i-lastEndIndex);
                j = j + i;
                    
                // expect numbers as key.
                while (j < _index && _cache[j] >= '0' && _cache[j] <= '9')
                {
                    key += (char)_cache[j];
                    j++;
                }

                if (key.Length == 0)
                {
                    // no key found. 
                    continue;
                }

                var k = 0;
                while (k < _endOfMessageSequenceEnd.Length && _cache[j+k] == _endOfMessageSequenceEnd[k])
                    k++;

                if (k != _endOfMessageSequenceEnd.Length)
                    continue;

                j += k;

                // clear line ending after '}'
                if (j < _index && (_cache[j] == '\r' || _cache[j] == '\n'))
                    j++;
                if (j < _index && (_cache[j] == '\r' || _cache[j] == '\n'))
                    j++;


                Update(this, new DataCapturedArgs(key, content));

                i = j;
                lastEndIndex = j;
            }

            Debug.Assert(lastEndIndex <= _index);

            if (lastEndIndex == 0)
                return;

            if (_index > lastEndIndex)
                Array.Copy(_cache, lastEndIndex, _cache, 0, _index - lastEndIndex);

            _index -= lastEndIndex;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public event EventHandler<DataCapturedArgs> Update = delegate { };
    }
}