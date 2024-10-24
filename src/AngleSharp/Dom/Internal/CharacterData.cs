namespace AngleSharp.Dom
{
    using System;

    /// <summary>
    /// The base class for all characterdata implementations.
    /// </summary>
    public abstract class CharacterData : Node, ICharacterData
    {
        #region Fields

        private String _content;

        #endregion

        #region ctor

        internal CharacterData(Document owner, String name, NodeType type)
            : this(owner, name, type, String.Empty)
        {
        }

        internal CharacterData(Document owner, String name, NodeType type, String content)
            : base(owner, name, type)
        {
            _content = content;
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public IElement? PreviousElementSibling
        {
            get
            {
                var parent = Parent;

                if (parent != null)
                {
                    var found = false;

                    for (var i = parent.ChildNodes.Length - 1; i >= 0; i--)
                    {
                        if (Object.ReferenceEquals(parent.ChildNodes[i], this))
                        {
                            found = true;
                        }
                        else if (found && parent.ChildNodes[i] is IElement childEl)
                        {
                            return childEl;
                        }
                    }
                }

                return null;
            }
        }

        /// <inheritdoc />
        public IElement? NextElementSibling
        {
            get
            {
                var parent = Parent;

                if (parent != null)
                {
                    var n = parent.ChildNodes.Length;
                    var found = false;

                    for (var i = 0; i < n; i++)
                    {
                        if (Object.ReferenceEquals(parent.ChildNodes[i], this))
                        {
                            found = true;
                        }
                        else if (found && parent.ChildNodes[i] is IElement childEl)
                        {
                            return childEl;
                        }
                    }
                }

                return null;
            }
        }

        internal Char this[Int32 index]
        {
            get => _content[index];
            set
            {
                if (index >= 0)
                {
                    if (index >= Length)
                    {
                        _content = _content.PadRight(index) + value.ToString();
                    }
                    else
                    {
                        var chrs = _content.ToCharArray();
                        chrs[index] = value;
                        _content = new String(chrs);
                    }
                }
            }
        }

        /// <inheritdoc />
        public Int32 Length => _content.Length;

        /// <inheritdoc />
        public sealed override String NodeValue
        {
            get => Data;
            set => Data = value;
        }

        /// <inheritdoc />
        public sealed override String TextContent
        {
            get => Data;
            set => Data = value;
        }

        /// <inheritdoc />
        public String Data
        {
            get => _content;
            set => Replace(0, Length, value);
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public String Substring(Int32 offset, Int32 count)
        {
            var length = _content.Length;

            if (offset > length)
            {
                throw new DomException(DomError.IndexSizeError);
            }

            if (offset + count > length)
            {
                return _content.Substring(offset);
            }

            return _content.Substring(offset, count);
        }

        /// <inheritdoc />
        public void Append(String value) => Replace(_content.Length, 0, value);

        /// <inheritdoc />
        public void Insert(Int32 offset, String data) => Replace(offset, 0, data);

        /// <inheritdoc />
        public void Delete(Int32 offset, Int32 count) => Replace(offset, count, String.Empty);

        /// <inheritdoc />
        public void Replace(Int32 offset, Int32 count, String data)
        {
            var owner = Owner;
            var length = _content.Length;

            if (offset > length)
            {
                throw new DomException(DomError.IndexSizeError);
            }

            if (offset + count > length)
            {
                count = length - offset;
            }

            var previous = _content;
            var deleteOffset = offset + data.Length;
            _content = _content.Insert(offset, data);

            if (count > 0)
            {
                _content = _content.Remove(deleteOffset, count);
            }

            owner.QueueMutation(MutationRecord.CharacterData(target: this, previousValue: previous));
            foreach (var m in owner.GetAttachedReferences<Range>())
            {
                if (m.Head == this && m.Start > offset && m.Start <= offset + count)
                {
                    m.StartWith(this, offset);
                }
                if (m.Tail == this && m.End > offset && m.End <= offset + count)
                {
                    m.EndWith(this, offset);
                }
                if (m.Head == this && m.Start > offset + count)
                {
                    m.StartWith(this, m.Start + data.Length - count);
                }
                if (m.Tail == this && m.End > offset + count)
                {
                    m.EndWith(this, m.End + data.Length - count);
                }
            }

            ViewSync?.UpdateText(this);
        }

        /// <inheritdoc />
        public void Before(params INode[] nodes) => this.InsertBefore(nodes);

        /// <inheritdoc />
        public void After(params INode[] nodes) => this.InsertAfter(nodes);

        /// <inheritdoc />
        public void Replace(params INode[] nodes) => this.ReplaceWith(nodes);

        /// <inheritdoc />
        public void Remove() => this.RemoveFromParent();

        #endregion
    }
}
