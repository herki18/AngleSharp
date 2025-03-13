namespace AngleSharp.StyleSystem.Computation.Properties
{
    using AngleSharp.Css.Values;
    using AngleSharp.StyleSystem.Interfaces;
    using AngleSharp.StyleSystem.Models;
    using AngleSharp.Css;

    /// <summary>
    /// Represents box properties in a computed style.
    /// </summary>
    public class BoxProperties : IBoxProperties
    {
        private readonly IComputedStyle _owner;
        private readonly IRenderDevice _renderDevice;
        private CssLengthValue _width = CssLengthValue.Auto;
        private CssLengthValue _height = CssLengthValue.Auto;
        private Edges _margin = Edges.Uniform(CssLengthValue.Zero);
        private Edges _border = Edges.Uniform(CssLengthValue.Zero);
        private Edges _padding = Edges.Uniform(CssLengthValue.Zero);

        /// <summary>
        /// Initializes a new instance of the BoxProperties class.
        /// </summary>
        /// <param name="owner">The computed style that owns these properties.</param>
        /// <param name="renderDevice">The render device for unit conversions.</param>
        public BoxProperties(IComputedStyle owner, IRenderDevice renderDevice)
        {
            _owner = owner;
            _renderDevice = renderDevice;
        }

        /// <summary>
        /// Gets the width of the box.
        /// </summary>
        public CssLengthValue Width => _width;

        /// <summary>
        /// Gets the height of the box.
        /// </summary>
        public CssLengthValue Height => _height;

        /// <summary>
        /// Gets the inline size (width or height depending on writing mode).
        /// </summary>
        public CssLengthValue InlineSize => _owner.WritingMode.IsHorizontal ? _width : _height;

        /// <summary>
        /// Gets the block size (height or width depending on writing mode).
        /// </summary>
        public CssLengthValue BlockSize => _owner.WritingMode.IsHorizontal ? _height : _width;

        /// <summary>
        /// Gets the margin edges.
        /// </summary>
        public Edges Margin => _margin;

        /// <summary>
        /// Gets the border edges.
        /// </summary>
        public Edges Border => _border;

        /// <summary>
        /// Gets the padding edges.
        /// </summary>
        public Edges Padding => _padding;

        /// <summary>
        /// Gets the logical margin edges.
        /// </summary>
        public LogicalEdges LogicalMargin => ConvertToLogical(_margin);

        /// <summary>
        /// Gets the logical border edges.
        /// </summary>
        public LogicalEdges LogicalBorder => ConvertToLogical(_border);

        /// <summary>
        /// Gets the logical padding edges.
        /// </summary>
        public LogicalEdges LogicalPadding => ConvertToLogical(_padding);

        /// <summary>
        /// Sets the width value.
        /// </summary>
        public void SetWidth(CssLengthValue? value)
        {
            _width = value ?? CssLengthValue.Auto;
        }

        /// <summary>
        /// Sets the height value.
        /// </summary>
        public void SetHeight(CssLengthValue? value)
        {
            _height = value ?? CssLengthValue.Auto;
        }

        /// <summary>
        /// Sets the margin top value.
        /// </summary>
        public void SetMarginTop(CssLengthValue? value)
        {
            _margin = new Edges(
                value ?? CssLengthValue.Zero,
                _margin.Right,
                _margin.Bottom,
                _margin.Left);
        }

        /// <summary>
        /// Sets the margin right value.
        /// </summary>
        public void SetMarginRight(CssLengthValue? value)
        {
            _margin = new Edges(
                _margin.Top,
                value ?? CssLengthValue.Zero,
                _margin.Bottom,
                _margin.Left);
        }

        /// <summary>
        /// Sets the margin bottom value.
        /// </summary>
        public void SetMarginBottom(CssLengthValue? value)
        {
            _margin = new Edges(
                _margin.Top,
                _margin.Right,
                value ?? CssLengthValue.Zero,
                _margin.Left);
        }

        /// <summary>
        /// Sets the margin left value.
        /// </summary>
        public void SetMarginLeft(CssLengthValue? value)
        {
            _margin = new Edges(
                _margin.Top,
                _margin.Right,
                _margin.Bottom,
                value ?? CssLengthValue.Zero);
        }

        /// <summary>
        /// Sets the border top value.
        /// </summary>
        public void SetBorderTop(CssLengthValue? value)
        {
            _border = new Edges(
                value ?? CssLengthValue.Zero,
                _border.Right,
                _border.Bottom,
                _border.Left);
        }

        /// <summary>
        /// Sets the border right value.
        /// </summary>
        public void SetBorderRight(CssLengthValue? value)
        {
            _border = new Edges(
                _border.Top,
                value ?? CssLengthValue.Zero,
                _border.Bottom,
                _border.Left);
        }

        /// <summary>
        /// Sets the border bottom value.
        /// </summary>
        public void SetBorderBottom(CssLengthValue? value)
        {
            _border = new Edges(
                _border.Top,
                _border.Right,
                value ?? CssLengthValue.Zero,
                _border.Left);
        }

        /// <summary>
        /// Sets the border left value.
        /// </summary>
        public void SetBorderLeft(CssLengthValue? value)
        {
            _border = new Edges(
                _border.Top,
                _border.Right,
                _border.Bottom,
                value ?? CssLengthValue.Zero);
        }

        /// <summary>
        /// Sets the padding top value.
        /// </summary>
        public void SetPaddingTop(CssLengthValue? value)
        {
            _padding = new Edges(
                value ?? CssLengthValue.Zero,
                _padding.Right,
                _padding.Bottom,
                _padding.Left);
        }

        /// <summary>
        /// Sets the padding right value.
        /// </summary>
        public void SetPaddingRight(CssLengthValue? value)
        {
            _padding = new Edges(
                _padding.Top,
                value ?? CssLengthValue.Zero,
                _padding.Bottom,
                _padding.Left);
        }

        /// <summary>
        /// Sets the padding bottom value.
        /// </summary>
        public void SetPaddingBottom(CssLengthValue? value)
        {
            _padding = new Edges(
                _padding.Top,
                _padding.Right,
                value ?? CssLengthValue.Zero,
                _padding.Left);
        }

        /// <summary>
        /// Sets the padding left value.
        /// </summary>
        public void SetPaddingLeft(CssLengthValue? value)
        {
            _padding = new Edges(
                _padding.Top,
                _padding.Right,
                _padding.Bottom,
                value ?? CssLengthValue.Zero);
        }

        private LogicalEdges ConvertToLogical(Edges physicalEdges)
        {
            if (_owner.WritingMode.IsHorizontal)
            {
                if (_owner.WritingMode.IsRightToLeft)
                {
                    return new LogicalEdges(
                        physicalEdges.Top,
                        physicalEdges.Left,
                        physicalEdges.Bottom,
                        physicalEdges.Right);
                }
                else
                {
                    return new LogicalEdges(
                        physicalEdges.Top,
                        physicalEdges.Right,
                        physicalEdges.Bottom,
                        physicalEdges.Left);
                }
            }
            else
            {
                if (_owner.WritingMode.IsRightToLeft)
                {
                    return new LogicalEdges(
                        physicalEdges.Right,
                        physicalEdges.Bottom,
                        physicalEdges.Left,
                        physicalEdges.Top);
                }
                else
                {
                    return new LogicalEdges(
                        physicalEdges.Left,
                        physicalEdges.Bottom,
                        physicalEdges.Right,
                        physicalEdges.Top);
                }
            }
        }
    }
}