namespace AngleSharp.Text
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    /// <summary>
    /// Char array based immutable text source
    /// </summary>
    public sealed class CharArrayTextSource : IReadOnlyTextSource
    {
        private Int32 _index;
        private String? _content;

        private readonly Char[] _array;
        private readonly ReadOnlyMemory<Char> _memory;
        private readonly Int32 _length;

        /// <summary>
        /// Creates a new char array based text source
        /// </summary>
        /// <param name="array">The character array to use</param>
        /// <param name="length">The effective length of the array</param>
        public CharArrayTextSource(Char[] array, Int32 length)
        {
            _array = array;
            _length = length;
            _memory = array.AsMemory(0, length);
        }

        #region Properties

        /// <inheritdoc />
        public String Text
        {
            get
            {
                return _content ??= new String(_array, 0, _length);
            }
        }

        /// <inheritdoc />
        public Char this[Int32 index] => _array[index];

        /// <inheritdoc />
        public Int32 Length => _length;

        /// <inheritdoc />
        public Encoding CurrentEncoding
        {
            get => TextEncoding.Utf8;
            set { }
        }

        /// <inheritdoc />
        public Int32 Index
        {
            get => _index;
            set => _index = value;
        }

        #endregion

        #region Disposable

        /// <inheritdoc />
        public void Dispose()
        {
        }

        #endregion

        #region Text Methods

        /// <inheritdoc />
        public Char ReadCharacter()
        {
            if (_index < _length)
            {
                return _array[_index++];
            }

            _index += 1;
            return Symbols.EndOfFile;
        }

        /// <inheritdoc />
        public String ReadCharacters(Int32 characters)
        {
            return ReadMemory(characters).ToString();
        }

        /// <summary>
        /// Reads characters as StringOrMemory
        /// </summary>
        /// <param name="characters">The number of characters to read</param>
        /// <returns>The StringOrMemory representation of the characters</returns>
        public StringOrMemory ReadMemory(Int32 characters)
        {
            var start = _index;
            var end = start + characters;

            if (end <= _length)
            {
                _index += characters;
                return _memory.Slice(start, characters);
            }

            _index += characters;
            characters = Math.Min(characters, _length - start);
            return _memory.Slice(start, characters);
        }

        /// <inheritdoc />
        public Task PrefetchAsync(Int32 length, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PrefetchAllAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Boolean TryGetContentLength(out Int32 length)
        {
            length = _length;
            return true;
        }

        #endregion
    }
}