namespace AngleSharp.Css.Dom
{
    using AngleSharp.Attributes;
    using AngleSharp.Common;
    using System;

    /// <summary>
    /// Represents a CSS declaration block, including its underlying state, where
    /// this underlying state depends upon the source of the CSSStyleDeclaration instance.
    /// </summary>
    [DomName("CSSStyleDeclaration")]
    public interface ICssStyleDeclaration : ICssProperties, IStyleFormattable, IBindable, ICssStyleDeclarationBase
    {
        /// <summary>
        /// Gets the name of the property with the specified index.
        /// </summary>
        /// <param name="index">The index of the property to retrieve.</param>
        /// <returns>The name of the property at the given index.</returns>
        [DomName("item")]
        [DomAccessor(Accessors.Getter)]
        String this[Int32 index] { get; }

        /// <summary>
        /// Gets or sets the textual representation of the declaration block.
        /// </summary>
        [DomName("cssText")]
        String CssText { get; set; }

        /// <summary>
        /// Gets the containing rule, if any.
        /// </summary>
        [DomName("parentRule")]
        ICssRule Parent { get; }

        /// <summary>
        /// Sets the parent rule.
        /// </summary>
        /// <param name="rule">The rule to use as parent.</param>
        void SetParent(ICssRule rule);

        #region Properties

        /// <summary>
        /// Gets how a flex item's lines align within the flex
        /// container when there is extra space along the axis that is
        /// perpendicular to the axis defined by the flex-direction property.
        /// </summary>
        [DomName("alignContent")]
        String AlignContent { get; set;}

        /// <summary>
        /// Gets the alignment value (perpendicular to the layout axis
        /// defined by the flex-direction property) of flex items of the flex
        /// container.
        /// </summary>
        [DomName("alignItems")]
        String AlignItems { get; set;}

        /// <summary>
        /// Gets the alignment value (perpendicular to the layout axis
        /// defined by the flex-direction property) of flex items of the flex
        /// container.
        /// </summary>
        [DomName("alignSelf")]
        String AlignSelf { get; set;}

        /// <summary>
        /// Gets a string that indicates whether the object represents
        /// a keyboard shortcut.
        /// </summary>
        [DomName("accelerator")]
        String Accelerator { get; set; }

        /// <summary>
        /// Sets which baseline of this element is to be aligned with
        /// the corresponding baseline of the parent.
        /// </summary>
        [DomName("alignmentBaseline")]
        String AlignmentBaseline { get; set; }

        /// <summary>
        /// Sets one or more shorthand values that define all animation
        /// properties (except animation-play-state) for a set of corresponding
        /// object properties identified in the CSS @keyframes at-rule
        /// specified by the animations-name property.
        /// </summary>
        [DomName("animation")]
        String Animation { get; set; }

        /// <summary>
        /// Gets the offset within an animation cycle (the amount of
        /// time from the start of a cycle) before the animation is displayed
        /// for a set of corresponding object properties identified in the CSS
        /// @keyframes at-rule specified by the animation-name property.
        /// </summary>
        [DomName("animationDelay")]
        String AnimationDelay { get; set; }

        /// <summary>
        /// Gets the direction of play for an animation cycle.
        /// </summary>
        [DomName("animationDirection")]
        String AnimationDirection { get; set; }

        /// <summary>
        /// Sets the length of time to complete one cycle of the
        /// animation.
        /// </summary>
        [DomName("animationDuration")]
        String AnimationDuration { get; set; }

        /// <summary>
        /// Gets whether the effects of an animation are visible before
        /// or after it plays.
        /// </summary>
        [DomName("animationFillMode")]
        String AnumationFillMode { get; set; }

        /// <summary>
        /// Sets the number of times an animation cycle is played.
        /// </summary>
        [DomName("animationIterationCount")]
        String AnimationIterationCount { get; set; }

        /// <summary>
        /// Gets one or more animation names. An animation name selects
        /// a CSS @keyframes at-rule.
        /// </summary>
        [DomName("animationName")]
        String AnimationName { get; set; }

        /// <summary>
        /// Gets whether an animation is playing or paused.
        /// </summary>
        [DomName("animationPlayState")]
        String AnimationPlayState { get; set; }

        /// <summary>
        /// Gets the intermediate property values to be used during a
        /// single cycle of an animation on a set of corresponding object
        /// properties identified in the CSS @keyframes at-rule specified by
        /// the animation-name property.
        /// </summary>
        [DomName("animationTimingFunction")]
        String AnimationTimingFunction { get; set; }

        /// <summary>
        /// Gets a value that specifies whether the back face (reverse
        /// side) of an object is visible.
        /// </summary>
        [DomName("backfaceVisibility")]
        String BackfaceVisibility { get; set; }

        /// <summary>
        /// Gets up to five separate background properties of an object.
        /// </summary>
        [DomName("background")]
        String Background { get; set; }

        /// <summary>
        /// Gets how the background image (or images) is attached to
        /// the object within the document.
        /// </summary>
        [DomName("backgroundAttachment")]
        String BackgroundAttachment { get; set; }

        /// <summary>
        /// Gets the background painting area or areas relative to the
        /// element's bounding boxes.
        /// </summary>
        [DomName("backgroundClip")]
        String BackgroundClip { get; set; }

        /// <summary>
        /// Gets the color behind the content of the object.
        /// </summary>
        [DomName("backgroundColor")]
        String BackgroundColor { get; set; }

        /// <summary>
        /// Gets the background image or images of the object.
        /// </summary>
        [DomName("backgroundImage")]
        String BackgroundImage { get; set; }

        /// <summary>
        /// Gets the positioning area of an element or multiple
        /// elements.
        /// </summary>
        [DomName("backgroundOrigin")]
        String BackgroundOrigin { get; set; }

        /// <summary>
        /// Gets the position of the background of the object.
        /// </summary>
        [DomName("backgroundPosition")]
        String BackgroundPosition { get; set; }

/// <summary>
        /// Gets the x-coordinate of the background-position property.
        /// </summary>
        [DomName("backgroundPositionX")]
        String BackgroundPositionX { get; set; }

        /// <summary>
        /// Gets the y-coordinate of the background-position property.
        /// </summary>
        [DomName("backgroundPositionY")]
        String BackgroundPositionY { get; set; }

        /// <summary>
        /// Gets whether and how the background image (or images) is
        /// tiled.
        /// </summary>
        [DomName("backgroundRepeat")]
        String BackgroundRepeat { get; set; }

        /// <summary>
        /// Gets the size of the background images.
        /// </summary>
        [DomName("backgroundSize")]
        String BackgroundSize { get; set; }

        /// <summary>
        /// Gets a value that indicates how the dominant baseline
        /// should be repositioned relative to the dominant baseline of the
        /// parent text content element.
        /// </summary>
        [DomName("baselineShift")]
        String BaselineShift { get; set; }

        /// <summary>
        /// Sets the location of the Dynamic HTML (DHTML) behavior
        /// DHTML Behaviors.
        /// </summary>
        [DomName("behavior")]
        String Behavior { get; set; }

        /// <summary>
        /// Gets the position of the object relative to the top of
        /// the next positioned object in the document hierarchy.
        /// </summary>
        [DomName("border")]
        String Bottom { get; set; }

        /// <summary>
        /// Gets the properties of a border drawn around an object.
        /// </summary>
        [DomName("border")]
        String Border { get; set; }

        /// <summary>
        /// Gets the properties of the bottom border of the object.
        /// </summary>
        [DomName("borderBottom")]
        String BorderBottom { get; set; }

        /// <summary>
        /// Gets the foreground color of the bottom border of an
        /// object.
        /// </summary>
        [DomName("borderBottomColor")]
        String BorderBottomColor { get; set; }

        /// <summary>
        /// Gets the radii of the quarter ellipse that defines the
        /// shape of the lower-left corner for the outer border edge of the
        /// current box.
        /// </summary>
        [DomName("borderBottomLeftRadius")]
        String BorderBottomLeftRadius { get; set; }

        /// <summary>
        /// Gets one or two values that define the radii of the quarter
        /// ellipse that defines the shape of the lower-right corner for the
        /// outer border edge of the current box.
        /// </summary>
        [DomName("borderBottomRightRadius")]
        String BorderBottomRightRadius { get; set; }

        /// <summary>
        /// Sets the style of the bottom border of the object.
        /// </summary>
        [DomName("borderBottomStyle")]
        String BorderBottomStyle { get; set; }

        /// <summary>
        /// Sets the thickness of the bottom border of the object.
        /// </summary>
        [DomName("borderBottomWidth")]
        String BorderBottomWidth { get; set; }

        /// <summary>
        /// Gets whether the row and cell borders of a table are joined
        /// in a single border or detached as in standard HTML.
        /// </summary>
        [DomName("borderCollapse")]
        String BorderCollapse { get; set; }

        /// <summary>
        /// Gets the border color of the object.
        /// </summary>
        [DomName("borderColor")]
        String BorderColor { get; set; }

        /// <summary>
        /// Gets an image to be used in place of the border styles.
        /// </summary>
        [DomName("borderImage")]
        String BorderImage { get; set; }

        /// <summary>
        /// Gets the amount by which the border image area extends
        /// beyond the border box.
        /// </summary>
        [DomName("borderImageOutset")]
        String BorderImageOutset { get; set; }

        /// <summary>
        /// Gets how the image is scaled and tiled.
        /// </summary>
        [DomName("borderImageRepeat")]
        String BorderImageRepeat { get; set; }

        /// <summary>
        /// Gets four inward offsets, this property slices the
        /// specified border image into a three by three grid: four corners,
        /// four edges, and a central region.
        /// </summary>
        [DomName("borderImageSlice")]
        String BorderImageSlice { get; set; }

        /// <summary>
        /// Sets the path of the image to be used for the border.
        /// </summary>
        [DomName("borderImageSource")]
        String BorderImageSource { get; set; }

        /// <summary>
        /// Sets the inward offsets from the outer border edge.
        /// </summary>
        [DomName("borderImageWidth")]
        String BorderImageWidth { get; set; }

        /// <summary>
        /// Gets the properties of the left border of the object.
        /// </summary>
        [DomName("borderLeft")]
        String BorderLeft { get; set; }

        /// <summary>
        /// Gets the foreground color of the left border of an object.
        /// </summary>
        [DomName("borderLeftColor")]
        String BorderLeftColor { get; set; }

        /// <summary>
        /// Gets the style of the left border of the object.
        /// </summary>
        [DomName("borderLeftStyle")]
        String BorderLeftStyle { get; set; }

        /// <summary>
        /// Gets the thickness of the left border of the object.
        /// </summary>
        [DomName("borderLeftWidth")]
        String BorderLeftWidth { get; set; }

        /// <summary>
        /// Gets the radii of a quarter ellipse that defines the shape
        /// of the corners for the outer border edge of the current box.
        /// </summary>
        [DomName("borderRadius")]
        String BorderRadius { get; set; }

        /// <summary>
        /// Gets the properties of the right border of the object.
        /// </summary>
        [DomName("borderRight")]
        String BorderRight { get; set; }

        /// <summary>
        /// Gets the foreground color of the right border of an object.
        /// </summary>
        [DomName("borderRightColor")]
        String BorderRightColor { get; set; }

        /// <summary>
        /// Gets the style of the right border of the object.
        /// </summary>
        [DomName("borderRightStyle")]
        String BorderRightStyle { get; set; }

        /// <summary>
        /// Gets the thickness of the right border of the object.
        /// </summary>
        [DomName("borderRightWidth")]
        String BorderRightWidth { get; set; }

        /// <summary>
        /// Gets the distance between the borders of adjoining cells in
        /// a table.
        /// </summary>
        [DomName("borderSpacing")]
        String BorderSpacing { get; set; }

        /// <summary>
        /// Gets the style of the left, right, top, and bottom borders
        /// of the object.
        /// </summary>
        [DomName("borderStyle")]
        String BorderStyle { get; set; }

        /// <summary>
        /// Gets the properties of the top border of the object.
        /// </summary>
        [DomName("borderTop")]
        String BorderTop { get; set; }

        /// <summary>
        /// Gets the foreground color of the top border of an object.
        /// </summary>
        [DomName("borderTopColor")]
        String BorderTopColor { get; set; }

        /// <summary>
        /// Gets one or two values that define the radii of the
        /// quarter ellipse that defines the shape of the upper-left corner for
        /// the outer border edge of the current box.
        /// </summary>
        [DomName("borderTopLeftRadius")]
        String BorderTopLeftRadius { get; set; }

        /// <summary>
        /// Gets one or two values that define the radii of the quarter
        /// ellipse that defines the shape of the upper-right corner for the
        /// outer border edge of the current box.
        /// </summary>
        [DomName("borderTopRightRadius")]
        String BorderTopRightRadius { get; set; }

        /// <summary>
        /// Gets the style of the top border of the object.
        /// </summary>
        [DomName("borderTopStyle")]
        String BorderTopStyle { get; set; }

        /// <summary>
        /// Gets the thickness of the top border of the object.
        /// </summary>
        [DomName("borderTopWidth")]
        String BorderTopWidth { get; set; }

        /// <summary>
        /// Gets the thicknesses of the left, right, top, and bottom
        /// borders of an object.
        /// </summary>
        [DomName("borderWidth")]
        String BorderWidth { get; set; }

        /// <summary>
        /// Gets one or more set of shadow values that attaches one or
        /// more drop shadows to the current box.
        /// </summary>
        [DomName("boxShadow")]
        String BoxShadow { get; set; }

        /// <summary>
        /// Gets the box model to use for object sizing.
        /// </summary>
        [DomName("boxSizing")]
        String BoxSizing { get; set; }

        /// <summary>
        /// Gets the column-break behavior that follows a content block
        /// in a multi-column element.
        /// </summary>
        [DomName("breakAfter")]
        String BreakAfter { get; set; }

        /// <summary>
        /// Gets the column-break behavior that precedes a content
        /// block in a multi-column element.
        /// </summary>
        [DomName("breakBefore")]
        String BreakBefore { get; set; }

        /// <summary>
        /// Gets the column-break behavior that occurs within a
        /// content block in a multi-column element.
        /// </summary>
        [DomName("breakInside")]
        String BreakInside { get; set; }

        /// <summary>
        /// Gets where the caption of a table is located.
        /// </summary>
        [DomName("captionSide")]
        String CaptionSide { get; set; }

        /// <summary>
        /// Gets whether the object allows floating objects on its left
        /// side, right side, or both, so that the next text displays past the
        /// floating objects.
        /// </summary>
        [DomName("clear")]
        String Clear { get; set; }

        /// <summary>
        /// Gets which part of a positioned object is visible.
        /// </summary>
        [DomName("clip")]
        String Clip { get; set; }

        /// <summary>
        /// Gets the bottom coordinate of the object clipping region.
        /// </summary>
        [DomName("clipBottom")]
        String ClipBottom { get; set; }

        /// <summary>
        /// Gets the left coordinate of the object clipping region.
        /// </summary>
        [DomName("clipLeft")]
        String ClipLeft { get; set; }

        /// <summary>
        /// Gets a reference to the SVG graphical object
        /// that will be used as the clipping path.
        /// </summary>
        [DomName("clipPath")]
        String ClipPath { get; set; }

        /// <summary>
        /// Gets the right coordinate of the object clipping region.
        /// </summary>
        [DomName("clipRight")]
        String ClipRight { get; set; }

        /// <summary>
        /// Gets the algorithm used to determine what parts of the
        /// canvas are affected by the fill operation.
        /// </summary>
        [DomName("clipRule")]
        String ClipRule { get; set; }

        /// <summary>
        /// Gets the top coordinate of the object clipping region.
        /// </summary>
        [DomName("clipTop")]
        String ClipTop { get; set; }

        /// <summary>
        /// Gets the foreground color of the text of an object.
        /// </summary>
        [DomName("color")]
        String Color { get; set; }

        /// <summary>
        /// Gets which color space to use for filter effects.
        /// </summary>
        [DomName("colorInterpolationFilters")]
        String ColorInterpolationFilters { get; set; }

        /// <summary>
        /// Gets the optimal number of columns in a multi-column
        /// element.
        /// </summary>
        [DomName("columnCount")]
        String ColumnCount { get; set; }

        /// <summary>
        /// Gets a value that indicates how the column lengths in a
        /// multi-column element are affected by the content flow.
        /// </summary>
        [DomName("columnFill")]
        String ColumnFill { get; set; }

        /// <summary>
        /// Gets the width of the gap between columns in a multi-column
        /// element.
        /// </summary>
        [DomName("columnGap")]
        String ColumnGap { get; set; }

        /// <summary>
        /// Gets a shorthand value  that specifies values for the
        /// columnRuleWidth, columnRuleStyle, and the columnRuleColor of a
        /// multi-column element.
        /// </summary>
        [DomName("columnRule")]
        String ColumnRule { get; set; }

        /// <summary>
        /// Gets the color for all column rules in a multi-column
        /// element.
        /// </summary>
        [DomName("columnRuleColor")]
        String ColumnRuleColor { get; set; }

        /// <summary>
        /// Gets the style for all column rules in a multi-column
        /// element.
        /// </summary>
        [DomName("columnRuleStyle")]
        String ColumnRuleStyle { get; set; }

        /// <summary>
        /// Sets the width of all column rules in a multi-column
        /// element.
        /// </summary>
        [DomName("columnRuleWidth")]
        String ColumnRuleWidth { get; set; }

        /// <summary>
        /// Gets a shorthand value that specifies values for the
        /// column-width and the column-count of a multi-column element.
        /// </summary>
        [DomName("columns")]
        String Columns { get; set; }

        /// <summary>
        /// Gets the number of columns that a content block
        /// spans in a multi-column element.
        /// </summary>
        [DomName("columnSpan")]
        String ColumnSpan { get; set; }

        /// <summary>
        /// Gets the optimal width of the columns in a multi-column
        /// element.
        /// </summary>
        [DomName("columnWidth")]
        String ColumnWidth { get; set; }

        /// <summary>
        /// Gets generated content to insert before or after an
        /// element.
        /// </summary>
        [DomName("content")]
        String Content { get; set; }

        /// <summary>
        /// Gets a list of counters to increment.
        /// </summary>
        [DomName("counterIncrement")]
        String CounterIncrement { get; set; }

        /// <summary>
        /// Gets a list of counters to create or reset to zero.
        /// </summary>
        [DomName("counterReset")]
        String CounterReset { get; set; }

        /// <summary>
        /// Gets a value that specifies whether a box should float to
        /// the left, right, or not at all.
        /// </summary>
        [DomName("cssFloat")]
        String CssFloat { get; set; }

        /// <summary>
        /// Gets the type of cursor to display as the mouse pointer
        /// moves over the object.
        /// </summary>
        [DomName("cursor")]
        String Cursor { get; set; }

        /// <summary>
        /// Gets the reading order of the object.
        /// </summary>
        [DomName("direction")]
        String Direction { get; set; }

        /// <summary>
        /// Gets a value that indicates whether and how the object is
        /// rendered.
        /// </summary>
        [DomName("display")]
        String Display { get; set; }

        /// <summary>
        /// Gets a value that determines or redetermines a
        /// scaled-baseline table.
        /// </summary>
        [DomName("dominantBaseline")]
        String DominantBaseline { get; set; }

        /// <summary>
        /// Gets whether to show or hide a cell without content.
        /// </summary>
        [DomName("emptyCells")]
        String EmptyCells { get; set; }

        /// <summary>
        /// Sets a shared background image all graphic elements within a
        /// container.
        /// </summary>
        [DomName("enableBackground")]
        String EnableBackground { get; set; }

        /// <summary>
        /// Gets a value that indicates the color to paint the
        /// interior of the given graphical element.
        /// </summary>
        [DomName("fill")]
        String Fill { get; set; }

        /// <summary>
        /// Gets a value that specifies the opacity of the painting
        /// operation that is used to paint the interior of the current object.
        /// </summary>
        [DomName("fillOpacity")]
        String FillOpacity { get; set; }

        /// <summary>
        /// Gets a value that indicates the algorithm that is to be
        /// used to determine what parts of the canvas are included inside the
        /// shape.
        /// </summary>
        [DomName("fillRule")]
        String FillRule { get; set; }

        /// <summary>
        /// Sets the filter property is generally used to apply a previously
        /// define filter to an applicable element.
        /// </summary>
        [DomName("filter")]
        String Filter { get; set; }

        /// <summary>
        /// Gets the parameter values of a flexible length, the
        /// positive and negative flexibility, and the preferred size.
        /// </summary>
        [DomName("flex")]
        String Flex { get; set; }

        /// <summary>
        /// Gets the initial main size of the flex item.
        /// </summary>
        [DomName("flexBasis")]
        String FlexBasis { get; set; }

        /// <summary>
        /// Gets the direction of the main axis which specifies how the
        /// flex items are displayed in the flex container.
        /// </summary>
        [DomName("flexDirection")]
        String FlexDirection { get; set; }

        /// <summary>
        /// Gets the shorthand property to set both the flex-direction
        /// and flex-wrap properties of a flex container.
        /// </summary>
        [DomName("flexFlow")]
        String FlexFlow { get; set; }

        /// <summary>
        /// Gets the flex grow factor for the flex item.
        /// </summary>
        [DomName("flexGrow")]
        String FlexGrow { get; set; }

        /// <summary>
        /// Sets the flex shrink factor for the flex item.
        /// </summary>
        [DomName("flexShrink")]
        String FlexShrink { get; set; }

        /// <summary>
        /// Gets whether flex items wrap and the direction they wrap
        /// onto multiple lines or columns based on the space available in the
        /// flex container.
        /// </summary>
        [DomName("flexWrap")]
        String FlexWrap { get; set; }

        /// <summary>
        /// Gets a combination of separate font properties of the
        /// object. Alternatively, sets or retrieves one or more of six
        /// user-preference fonts.
        /// </summary>
        [DomName("font")]
        String Font { get; set; }

        /// <summary>
        /// Sets the name of the font used for text in the object.
        /// </summary>
        [DomName("fontFamily")]
        String FontFamily { get; set; }

        /// <summary>
        /// Gets one or more values that specify glyph substitution and
        /// positioning in fonts that include OpenType layout features.
        /// </summary>
        [DomName("fontFeatureSettings")]
        String FontFeatureSettings { get; set; }

        /// <summary>
        /// Sets a value that indicates the font size used for text in
        /// the object.
        /// </summary>
        [DomName("fontSize")]
        String FontSize { get; set; }

        /// <summary>
        /// Gets a value that specifies an aspect value for an element
        /// that will effectively preserve the x-height of the first choice
        /// font, whether it is substituted or not.
        /// </summary>
        [DomName("fontSizeAdjust")]
        String FontSizeAdjust { get; set; }

        /// <summary>
        /// Gets a value that indicates a normal, condensed, or
        /// expanded face of a font family.
        /// </summary>
        [DomName("fontStretch")]
        String FontStretch { get; set; }

        /// <summary>
        /// Sets the font style of the object as italic, normal, or
        /// oblique.
        /// </summary>
        [DomName("fontStyle")]
        String FontStyle { get; set; }

        /// <summary>
        /// Gets whether the text of the object is in small capital
        /// letters.
        /// </summary>
        [DomName("fontVariant")]
        String FontVariant { get; set; }

        /// <summary>
        /// Gets of sets the weight of the font of the object.
        /// </summary>
        [DomName("fontWeight")]
        String FontWeight { get; set; }

        /// <summary>
        /// Gets a value that alters the orientation of a sequence of
        /// characters relative to an inline-progression-direction of
        /// horizontal.
        /// </summary>
        [DomName("glyphOrientationHorizontal")]
        String GlyphOrientationHorizontal { get; set; }

        /// <summary>
        /// Gets a value that alters the orientation of a sequence
        /// of characters relative to an inline-progression-direction of
        /// vertical.
        /// </summary>
        [DomName("glyphOrientationVertical")]
        String GlyphOrientationVertical { get; set; }

        /// <summary>
        /// Gets the height of the object.
        /// </summary>
        [DomName("height")]
        String Height { get; set; }

        /// <summary>
        /// Gets the state of an IME.
        /// </summary>
        [DomName("imeMode")]
        String ImeMode { get; set; }

        /// <summary>
        /// Gets a how flex items are aligned along the main axis of
        /// the flex container after any flexible lengths and auto margins are
        /// resolved.
        /// </summary>
        [DomName("justifyContent")]
        String JustifyContent { get; set; }

        /// <summary>
        /// Gets the composite document grid properties that specify
        /// the layout of text characters.
        /// </summary>
        [DomName("layoutGrid")]
        String LayoutGrid { get; set; }

        /// <summary>
        /// Sets the size of the character grid used for rendering
        /// the text content of an element.
        /// </summary>
        [DomName("layoutGridChar")]
        String LayoutGridChar { get; set; }

        /// <summary>
        /// Gets the gridline value used for rendering the text content
        /// of an element.
        /// </summary>
        [DomName("layoutGridLine")]
        String LayoutGridLine { get; set; }

        /// <summary>
        /// Gets whether the text layout grid uses two dimensions.
        /// </summary>
        [DomName("layoutGridMode")]
        String LayoutGridMode { get; set; }

        /// <summary>
        /// Gets the type of grid used for rendering the text content
        /// of an element.
        /// </summary>
        [DomName("layoutGridType")]
        String LayoutGridType { get; set; }

        /// <summary>
        /// Sets the position of the object relative to the left edge
        /// of the next positioned object in the document hierarchy.
        /// </summary>
        [DomName("left")]
        String Left { get; set; }

        /// <summary>
        /// Gets the amount of additional space between letters in the
        /// object.
        /// </summary>
        [DomName("letterSpacing")]
        String LetterSpacing { get; set; }

        /// <summary>
        /// Gets the distance between lines in the object.
        /// </summary>
        [DomName("lineHeight")]
        String LineHeight { get; set; }

        /// <summary>
        /// Gets up to three separate list-style properties of the
        /// object.
        /// </summary>
        [DomName("listStyle")]
        String ListStyle { get; set; }

        /// <summary>
        /// Gets a value that indicates which image to use as a
        /// list-item marker for the object.
        /// </summary>
        [DomName("listStyleImage")]
        String ListStyleImage { get; set; }

        /// <summary>
        /// Gets a variable that indicates how the list-item marker is
        /// drawn relative to the content of the object.
        /// </summary>
        [DomName("listStylePosition")]
        String ListStylePosition { get; set; }

        /// <summary>
        /// Gets the predefined type of the line-item marker for the
        /// object.
        /// </summary>
        [DomName("listStyleType")]
        String ListStyleType { get; set; }

        /// <summary>
        /// Gets the width of the top, right, bottom, and left margins
        /// of the object.
        /// </summary>
        [DomName("margin")]
        String Margin { get; set; }

        /// <summary>
        /// Gets the height of the bottom margin of the object.
        /// </summary>
        [DomName("marginBottom")]
        String MarginBottom { get; set; }

        /// <summary>
        /// Gets the width of the left margin of the object.
        /// </summary>
        [DomName("marginLeft")]
        String MarginLeft { get; set; }

        /// <summary>
        /// Gets the width of the right margin of the object.
        /// </summary>
        [DomName("marginRight")]
        String MarginRight { get; set; }

        /// <summary>
        /// Gets the height of the top margin of the object.
        /// </summary>
        [DomName("marginTop")]
        String MarginTop { get; set; }

        /// <summary>
        /// Gets a value that specifies the marker symbol that is
        /// used for all vertices on the given path element or basic shape.
        /// </summary>
        [DomName("marker")]
        String Marker { get; set; }

        /// <summary>
        /// Gets a value that defines the arrowhead or polymarker that
        /// is drawn at the final vertex of a given path element or basic
        /// shape.
        /// </summary>
        [DomName("markerEnd")]
        String MarkerEnd { get; set; }

        /// <summary>
        /// Gets a value that defines the arrowhead or polymarker that
        /// is drawn at every other vertex (that is, every vertex except the
        /// first and last) of a given path element or basic shape.
        /// </summary>
        [DomName("markerMid")]
        String MarkerMid { get; set; }

        /// <summary>
        /// Gets a value that defines the arrowhead or polymarker that
        /// is drawn at the first vertex of a given path element or basic
        /// shape.
        /// </summary>
        [DomName("markerStart")]
        String MarkerStart { get; set; }

        /// <summary>
        /// Gets a value that indicates a SVG mask.
        /// </summary>
        [DomName("mask")]
        String Mask { get; set; }

        /// <summary>
        /// Gets the maximum height for an element.
        /// </summary>
        [DomName("maxHeight")]
        String MaxHeight { get; set; }

        /// <summary>
        /// Gets the maximum width for an element.
        /// </summary>
        [DomName("maxWidth")]
        String MaxWidth { get; set; }

        /// <summary>
        /// Gets the minimum height for an element.
        /// </summary>
        [DomName("minHeight")]
        String MinHeight { get; set; }

        /// <summary>
        /// Gets the minimum width for an element.
        /// </summary>
        [DomName("minWidth")]
        String MinWidth { get; set; }

        /// <summary>
        /// Gets a value that specifies object or group opacity in CSS
        /// or SVG.
        /// </summary>
        [DomName("opacity")]
        String Opacity { get; set; }

        /// <summary>
        /// Gets the order, which property specifies the order used to
        /// lay out flex items in their flex container. Elements are laid out
        /// by ascending order of the order value. Elements with the same order
        /// value are laid out in the order they appear in the source code.
        /// </summary>
        [DomName("order")]
        String Order { get; set; }

        /// <summary>
        /// Gets the minimum number of lines of a paragraph that must
        /// appear at the bottom of a page.
        /// </summary>
        [DomName("orphans")]
        String Orphans { get; set; }

        /// <summary>
        /// Gets the outline frame.
        /// </summary>
        [DomName("outline")]
        String Outline { get; set; }

        /// <summary>
        /// Gets the color of the outline frame.
        /// </summary>
        [DomName("outlineColor")]
        String OutlineColor { get; set; }

        /// <summary>
        /// Gets the style of the outline frame.
        /// </summary>
        [DomName("outlineStyle")]
        String OutlineStyle { get; set; }

        /// <summary>
        /// Gets the width of the outline frame.
        /// </summary>
        [DomName("outlineWidth")]
        String OutlineWidth { get; set; }

        /// <summary>
        /// Gets a value indicating how to manage the content of the
        /// object when the content exceeds the height or width of the object.
        /// </summary>
        [DomName("overflow")]
        String Overflow { get; set; }

        /// <summary>
        /// Gets how to manage the content of the object when the
        /// content exceeds the width of the object.
        /// </summary>
        [DomName("overflowX")]
        String OverflowX { get; set; }

        /// <summary>
        /// Gets how to manage the content of the object when the
        /// content exceeds the height of the object.
        /// </summary>
        [DomName("overflowY")]
        String OverflowY { get; set; }

        /// <summary>
        /// Gets the amount of space to insert between the object and
        /// its margin or, if there is a border, between the object and its
        /// border.
        /// </summary>
        [DomName("padding")]
        String Padding { get; set; }

        /// <summary>
        /// Gets the amount of space to insert between the bottom
        /// border of the object and the content.
        /// </summary>
        [DomName("paddingBottom")]
        String PaddingBottom { get; set; }

        /// <summary>
        /// Gets the amount of space to insert between the left
        /// border of the object and the content.
        /// </summary>
        [DomName("paddingLeft")]
        String PaddingLeft { get; set; }

        /// <summary>
        /// Gets the amount of space to insert between the right border
        /// of the object and the content.
        /// </summary>
        [DomName("paddingRight")]
        String PaddingRight { get; set; }

        /// <summary>
        /// Gets the amount of space to insert between the top border
        /// of the object and the content.
        /// </summary>
        [DomName("paddingTop")]
        String PaddingTop { get; set; }

        /// <summary>
        /// Gets a value indicating whether a page break occurs after
        /// the object.
        /// </summary>
        [DomName("pageBreakAfter")]
        String PageBreakAfter { get; set; }

        /// <summary>
        /// Gets a string indicating whether a page break occurs before
        /// the object.
        /// </summary>
        [DomName("pageBreakBefore")]
        String PageBreakBefore { get; set; }

        /// <summary>
        /// Gets a string indicating whether a page break is allowed to
        /// occur inside the object.
        /// </summary>
        [DomName("pageBreakInside")]
        String PageBreakInside { get; set; }

        /// <summary>
        /// Gets a value that represents the perspective from which all
        /// child elements of the object are viewed.
        /// </summary>
        [DomName("perspective")]
        String Perspective { get; set; }

        /// <summary>
        /// Gets one or two values that represent the origin (the
        /// vanishing point for the 3-D space) of an object with an perspective
        /// property declaration.
        /// </summary>
        [DomName("perspectiveOrigin")]
        String PerspectiveOrigin { get; set; }

        /// <summary>
        /// Gets a value that specifies under what circumstances a
        /// given graphics element can be the target element for a pointer
        /// event in SVG.
        /// </summary>
        [DomName("pointerEvents")]
        String PointerEvents { get; set; }

        /// <summary>
        /// Gets the pairs of strings to be used as quotes in generated
        /// content.
        /// </summary>
        [DomName("quotes")]
        String Quotes { get; set; }

        /// <summary>
        /// Gets the type of positioning used for the object.
        /// </summary>
        [DomName("position")]
        String Position { get; set; }

        /// <summary>
        /// Gets the position of the object relative to the right edge
        /// of the next positioned object in the document hierarchy.
        /// </summary>
        [DomName("right")]
        String Right { get; set; }

        /// <summary>
        /// Gets a value that indicates how to align the ruby text
        /// content.
        /// </summary>
        [DomName("rubyAlign")]
        String RubyAlign { get; set; }

        /// <summary>
        /// Gets a value that indicates whether, and on which side,
        /// ruby text is allowed to partially overhang any adjacent text in
        /// addition to its own base, when the ruby text is wider than the
        /// ruby base.
        /// </summary>
        [DomName("rubyOverhang")]
        String RubyOverhang { get; set; }

        /// <summary>
        /// Gets a value that controls the position of the ruby text
        /// with respect to its base.
        /// </summary>
        [DomName("rubyPosition")]
        String RubyPosition { get; set; }

        /// <summary>
        /// Gets the color of the top and left edges of the scroll
        /// box and scroll arrows of a scroll bar.
        /// </summary>
        [DomName("scrollbar3dLightColor")]
        String Scrollbar3dLightColor { get; set; }

        /// <summary>
        /// Gets the color of the arrow elements of a scroll arrow.
        /// </summary>
        [DomName("scrollbarArrowColor")]
        String ScrollbarArrowColor { get; set; }

        /// <summary>
        /// Gets the color of the gutter of a scroll bar.
        /// </summary>
        [DomName("scrollbarDarkShadowColor")]
        String ScrollbarDarkShadowColor { get; set; }

        /// <summary>
        /// Gets the color of the scroll box and scroll arrows of a
        /// scroll bar.
        /// </summary>
        [DomName("scrollbarFaceColor")]
        String ScrollbarFaceColor { get; set; }

        /// <summary>
        /// Gets the color of the top and left edges of the scroll box
        /// and scroll arrows of a scroll bar.
        /// </summary>
        [DomName("scrollbarHighlightColor")]
        String ScrollbarHighlightColor { get; set; }

        /// <summary>
        /// Gets the color of the bottom and right edges of the scroll
        /// box and scroll arrows of a scroll bar.
        /// </summary>
        [DomName("scrollbarShadowColor")]
        String ScrollbarShadowColor { get; set; }

        /// <summary>
        /// Gets the color of the track element of a scroll bar.
        /// </summary>
        [DomName("scrollbarTrackColor")]
        String ScrollbarTrackColor { get; set; }

        /// <summary>
        /// Gets a value that indicates the color to paint along the
        /// outline of a given graphical element.
        /// </summary>
        [DomName("stroke")]
        String Stroke { get; set; }

        /// <summary>
        /// Gets one or more values that indicate the pattern of dashes
        /// and gaps used to stroke paths.
        /// </summary>
        [DomName("strokeDasharray")]
        String StrokeDasharray { get; set; }

        /// <summary>
        /// Gets a value that specifies the distance into the dash
        /// pattern to start the dash.
        /// </summary>
        [DomName("strokeDashoffset")]
        String StrokeDashoffset { get; set; }

        /// <summary>
        /// Gets a value that specifies the shape to be used at the end
        /// of open subpaths when they are stroked.
        /// </summary>
        [DomName("strokeLinecap")]
        String StrokeLinecap { get; set; }

        /// <summary>
        /// Gets a value that specifies the shape to be used at the
        /// corners of paths or basic shapes when they are stroked.
        /// </summary>
        [DomName("strokeLinejoin")]
        String StrokeLinejoin { get; set; }

        /// <summary>
        /// Gets a value that indicates the limit on the ratio of the
        /// length of miter joins (as specified in the StrokeLinejoin
        /// property).
        /// </summary>
        [DomName("strokeMiterlimit")]
        String StrokeMiterlimit { get; set; }

        /// <summary>
        /// Gets a value that specifies the opacity of the painting
        /// operation that is used to stroke the current object.
        /// </summary>
        [DomName("strokeOpacity")]
        String StrokeOpacity { get; set; }

        /// <summary>
        /// Gets a value that specifies the width of the stroke on the
        /// current object.
        /// </summary>
        [DomName("strokeWidth")]
        String StrokeWidth { get; set; }

        /// <summary>
        /// Gets a string that indicates whether the table layout is
        /// fixed.
        /// </summary>
        [DomName("tableLayout")]
        String TableLayout { get; set; }

        /// <summary>
        /// Gets whether the text in the object is left-aligned,
        /// right-aligned, centered, or justified.
        /// </summary>
        [DomName("textAlign")]
        String TextAlign { get; set; }

        /// <summary>
        /// Gets a value that indicates how to align the last line or
        /// only line of text in the specified object.
        /// </summary>
        [DomName("textAlignLast")]
        String TextAlignLast { get; set; }

        /// <summary>
        /// Gets aligns a string of text relative to the specified point.
        /// </summary>
        [DomName("textAnchor")]
        String TextAnchor { get; set; }

        /// <summary>
        /// Gets the autospacing and narrow space width adjustment of
        /// text.
        /// </summary>
        [DomName("textAutospace")]
        String TextAutospace { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the text in the object
        /// has blink, line-through, overline, or underline decorations.
        /// </summary>
        [DomName("textDecoration")]
        String TextDecoration { get; set; }

        /// <summary>
        /// Gets a value that indicates the style of the text decoration.
        /// </summary>
        [DomName("textDecorationStyle")]
        String TextDecorationStyle { get; set; }

        /// <summary>
        /// Gets a value that indicates the line of the text decoration.
        /// </summary>
        [DomName("textDecorationLine")]
        String TextDecorationLine { get; set; }

        /// <summary>
        /// Gets a value that indicates the color of the text decoration.
        /// </summary>
        [DomName("textDecorationColor")]
        String TextDecorationColor { get; set; }

        /// <summary>
        /// Gets the indentation of the first line of text in the
        /// object.
        /// </summary>
        [DomName("textIndent")]
        String TextIndent { get; set; }

        /// <summary>
        /// Gets the type of alignment used to justify text in the
        /// object.
        /// </summary>
        [DomName("textJustify")]
        String TextJustify { get; set; }

        /// <summary>
        /// Gets a value that indicates whether to render ellipses
        /// (...) to indicate text overflow.
        /// </summary>
        [DomName("textOverflow")]
        String TextOverflow { get; set; }

        /// <summary>
        /// Gets a comma-separated list of shadows that attaches one or
        /// more drop shadows to the specified text.
        /// </summary>
        [DomName("textShadow")]
        String TextShadow { get; set; }

        /// <summary>
        /// Gets the rendering of the text in the object.
        /// </summary>
        [DomName("textTransform")]
        String TextTransform { get; set; }

        /// <summary>
        /// Gets the position of the underline decoration that is set
        /// through the text-decoration property of the object.
        /// </summary>
        [DomName("textUnderlinePosition")]
        String TextUnderlinePosition { get; set; }

        /// <summary>
        /// Gets the position of the object relative to the top of the
        /// next positioned object in the document hierarchy.
        /// </summary>
        [DomName("top")]
        String Top { get; set; }

        /// <summary>
        /// Gets a list of one or more transform functions that specify
        /// how to translate, rotate, or scale an element in 2-D or 3-D space.
        /// </summary>
        [DomName("transform")]
        String Transform { get; set; }

        /// <summary>
        /// Gets one or two values that establish the origin of
        /// transformation for an element.
        /// </summary>
        [DomName("transformOrigin")]
        String TransformOrigin { get; set; }

        /// <summary>
        /// Gets a value that specifies how child elements of the
        /// object are rendered in 3-D space.
        /// </summary>
        [DomName("transformStyle")]
        String TransformStyle { get; set; }

        /// <summary>
        /// Gets one or more shorthand values that specify the
        /// transition properties for a set of corresponding object properties
        /// identified in the transition-property property.
        /// </summary>
        [DomName("transition")]
        String Transition { get; set; }

        /// <summary>
        /// Gets one or more values that specify the offset within a
        /// transition (the amount of time from the start of a transition)
        /// before the transition is displayed  for a set of corresponding
        /// object properties identified in the transition property.
        /// </summary>
        [DomName("transitionDelay")]
        String TransitionDelay { get; set; }

        /// <summary>
        /// Gets one or more values that specify the durations of
        /// transitions on a set of corresponding object properties identified
        /// in the transition-property property.
        /// </summary>
        [DomName("transitionDuration")]
        String TransitionDuration { get; set; }

        /// <summary>
        /// Gets a value that identifies the CSS property name or names
        /// to which the transition effect (defined by the transition-duration,
        /// transition-timing-function, and transition-delay properties) is
        /// applied when a new property value is specified.
        /// </summary>
        [DomName("transitionProperty")]
        String TransitionProperty { get; set; }

        /// <summary>
        /// Gets one or more values that specify the intermediate
        /// property values to be used during a transition on a set of
        /// corresponding object properties identified in the
        /// transition-property property.
        /// </summary>
        [DomName("transitionTimingFunction")]
        String TransitionTimingFunction { get; set; }

        /// <summary>
        /// Gets the level of embedding with respect to the
        /// bidirectional algorithm.
        /// </summary>
        [DomName("unicodeBidi")]
        String UnicodeBidi { get; set; }

        /// <summary>
        /// Gets the vertical alignment of the object.
        /// </summary>
        [DomName("verticalAlign")]
        String VerticalAlign { get; set; }

        /// <summary>
        /// Gets whether the content of the object is displayed.
        /// </summary>
        [DomName("visibility")]
        String Visibility { get; set; }

        /// <summary>
        /// Gets a value that indicates whether lines are automatically
        /// broken inside the object.
        /// </summary>
        [DomName("whiteSpace")]
        String WhiteSpace { get; set; }

        /// <summary>
        /// Gets the minimum number of lines of a paragraph that must
        /// appear at the top of a document.
        /// </summary>
        [DomName("widows")]
        String Widows { get; set; }

        /// <summary>
        /// Gets the width of the object.
        /// </summary>
        [DomName("width")]
        String Width { get; set; }

        /// <summary>
        /// Gets line-breaking behavior within words, particularly
        /// where multiple languages appear in the object.
        /// </summary>
        [DomName("wordBreak")]
        String WordBreak { get; set; }

        /// <summary>
        /// Gets the amount of additional space between words in the
        /// object.
        /// </summary>
        [DomName("wordSpacing")]
        String WordSpacing { get; set; }

        /// <summary>
        /// Gets whether to break words when the content exceeds the
        /// boundaries of its container.
        /// </summary>
        [DomName("wordWrap")]
        String WordWrap { get; set; }

        /// <summary>
        /// Gets the overflow-wrap value.
        /// </summary>
        [DomName("overflowWrap")]
        String OverflowWrap { get; set; }

        /// <summary>
        /// Gets the direction and flow of the content in the object.
        /// </summary>
        [DomName("writingMode")]
        String WritingMode { get; set; }

        /// <summary>
        /// Gets the stacking order of positioned objects.
        /// </summary>
        [DomName("zIndex")]
        String ZIndex { get; set; }

        /// <summary>
        /// Gets the magnification scale of the object.
        /// </summary>
        [DomName("zoom")]
        String Zoom { get; set; }

        #endregion
    }
}
