#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace AngleSharp.Css.Dom
{
    using AngleSharp.Css;
    using AngleSharp.Css.Parser;
    using AngleSharp.Dom;
    using AngleSharp.Text;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Represents a single CSS declaration block.
    /// </summary>
    public class CssStyleDeclaration : ICssStyleDeclaration
    {
        #region Fields

        private readonly List<ICssProperty> _declarations;
        private readonly IBrowsingContext _context;
        private ICssRule _parent;
        private Boolean _updating;

        #endregion

        #region Events

        public event Action<String> Changed;

        #endregion

        #region ctor

        public CssStyleDeclaration(IBrowsingContext context)
        {
            _declarations = new List<ICssProperty>();
            _context = context;
        }

        public CssStyleDeclaration(ICssRule parent)
            : this(parent.Owner?.Context)
        {
            _parent = parent;
        }

        #endregion

        #region Index

        public String this[Int32 index] => _declarations[index]?.Name;

        public String this[String name] => GetPropertyValue(name);

        #endregion

        #region Properties

        public IEnumerable<ICssProperty> Declarations => _declarations;

        public String CssText
        {
            get => this.ToCss();
            set { Update(value); RaiseChanged(); }
        }

        public Boolean IsReadOnly => _context == null;

        public Int32 Length => Declarations.Count();

        public ICssRule Parent => _parent;

        #endregion

        #region Methods

        public ICssProperty GetProperty(String name)
        {
            for (var i = 0; i < _declarations.Count; i++)
            {
                var declaration = _declarations[i];

                if (declaration.Name.Isi(name))
                {
                    return declaration;
                }
            }

            return GetPropertyShorthand(name);
        }

        public void SetParent(ICssRule parent) => _parent = parent;

        public void Update(String value)
        {
            if (IsReadOnly)
                throw new DomException(DomError.NoModificationAllowed);

            if (!_updating)
            {
                _declarations.Clear();

                if (!String.IsNullOrEmpty(value))
                {
                    var parser = _context.GetService<ICssParser>();
                    var decl = parser?.ParseDeclaration(value);

                    if (decl != null)
                    {
                        _declarations.AddRange(decl);
                    }
                }
            }
        }

        private ICssProperty TryCreateShorthand(String shorthandName, IEnumerable<String> serialized, List<String> usedProperties, Boolean force)
        {
            var factory = _context.GetFactory<IDeclarationFactory>();
            var shorthand = factory.Create(shorthandName);
            var requiredProperties = shorthand.Longhands;

            if (requiredProperties.Length > 0)
            {
                var longhands = Declarations.Where(m => !serialized.Contains(m.Name)).ToList();
                var values = new ICssValue[requiredProperties.Length];
                var important = 0;
                var count = 0;

                for (var i = 0; i < values.Length; i++)
                {
                    var name = requiredProperties[i];
                    var propInfo = factory.Create(name);
                    var property = propInfo.Longhands.Any() ?
                        TryCreateShorthand(name, serialized, usedProperties, force) :
                        longhands.Where(m => m.Name == name).FirstOrDefault();

                    if (property?.Value is not null)
                    {
                        usedProperties.Add(name);
                        count = count + 1;
                        important = important + (property.IsImportant ? 1 : 0);
                        values[i] = property.RawValue;
                    }
                }

                var valid = count == values.Length && (important == 0 || important == count);
                var result = force || valid ? _context.CreateShorthand(shorthandName, values, important != 0) : null;
                return force || result?.RawValue != null ? result : null;
            }

            return _context.CreateProperty(shorthandName);
        }

        public String ToCssBlock(IStyleFormatter formatter)
        {
            var list = new List<ICssProperty>();
            var serialized = new List<String>();
            var factory = _context.GetFactory<IDeclarationFactory>();

            foreach (var declaration in Declarations)
            {
                var property = declaration.Name;

                if (!serialized.Contains(property))
                {
                    var info = factory.Create(property);
                    var shorthands = info.Shorthands;

                    if (shorthands.Any())
                    {
                        var sortedShorthands = shorthands.OrderByDescending(shorthand => factory.Create(property).Longhands.Length);

                        foreach (var shorthandName in sortedShorthands)
                        {
                            var usedProperties = new List<String>();
                            var shorthand = TryCreateShorthand(shorthandName, serialized, usedProperties, false);

                            if (shorthand is not null)
                            {
                                list.Add(shorthand);

                                foreach (var name in usedProperties)
                                {
                                    serialized.Add(name);
                                }

                                break;
                            }
                        }
                    }

                    if (!serialized.Contains(property))
                    {
                        serialized.Add(property);
                        list.Add(declaration);
                    }
                }
            }

            return formatter.BlockDeclarations(list);
        }

        public void ToCss(TextWriter writer, IStyleFormatter formatter) =>
            writer.Write(ToCssBlock(formatter).Trim(' ', '\t', '\r', '\n', '{', '}'));

        public String RemoveProperty(String propertyName)
        {
            if (!IsReadOnly)
            {
                var value = GetPropertyValue(propertyName);
                RemovePropertyByName(propertyName);
                RaiseChanged();
                return value;
            }

            throw new DomException(DomError.NoModificationAllowed);
        }

        public String GetPropertyPriority(String propertyName)
        {
            var property = GetProperty(propertyName);

            if (property == null || !property.IsImportant)
            {
                var info = _context.GetDeclarationInfo(propertyName);
                var longhands = info.Longhands;

                if (longhands.Length == 0)
                {
                    return String.Empty;
                }

                foreach (var longhand in longhands)
                {
                    if (!GetPropertyPriority(longhand).Isi(CssKeywords.Important))
                    {
                        return String.Empty;
                    }
                }
            }

            return CssKeywords.Important;
        }

        public String GetPropertyValue(String propertyName) =>
            GetProperty(propertyName)?.Value ?? String.Empty;

        public void SetPropertyValue(String propertyName, String propertyValue) =>
            SetProperty(propertyName, propertyValue);

        public void SetPropertyPriority(String propertyName, String priority)
        {
            if (IsReadOnly)
                throw new DomException(DomError.NoModificationAllowed);

            if (String.IsNullOrEmpty(priority) || priority.Isi(CssKeywords.Important))
            {
                var info = _context.GetDeclarationInfo(propertyName);
                var important = !String.IsNullOrEmpty(priority);
                var mappings = info.GetMappings();

                foreach (var mapping in mappings)
                {
                    var property = GetProperty(mapping);

                    if (property != null)
                    {
                        property.IsImportant = important;
                    }
                }
            }
        }

        public void SetProperty(String propertyName, String propertyValue, String priority = null)
        {
            if (IsReadOnly)
                throw new DomException(DomError.NoModificationAllowed);

            if (!String.IsNullOrEmpty(propertyValue))
            {
                if (priority is null || priority.Isi(CssKeywords.Important))
                {
                    var property = CreateProperty(propertyName);

                    if (property is not null)
                    {
                        property.Value = propertyValue;

                        if (property.RawValue is not null)
                        {
                            property.IsImportant = priority is not null;
                            SetProperty(property);
                            RaiseChanged();
                        }
                    }
                }
            }
            else
            {
                RemoveProperty(propertyName);
            }
        }

        public void AddProperty(ICssProperty declaration)
        {
            _declarations.Add(declaration);
        }

        public void RemoveProperty(ICssProperty declaration)
        {
            _declarations.Remove(declaration);
        }

        #endregion

        #region Internal Methods

        public void SetDeclarations(IEnumerable<ICssProperty> decls) =>
            ChangeDeclarations(decls,
                m => false,
                (o, n) => !o.IsImportant || n.IsImportant);

        public void UpdateDeclarations(IEnumerable<ICssProperty> decls) =>
            ChangeDeclarations(decls,
                m => !m.CanBeInherited,
                (o, n) => o.IsInherited);

        #endregion

        #region Helpers

        private ICssProperty GetPropertyShorthand(String name) =>
            TryCreateShorthand(name, Enumerable.Empty<String>(), new List<String>(), true);

        private ICssProperty CreateProperty(String propertyName)
        {
            var newProperty = _context.CreateProperty(propertyName);
            var existing = GetProperty(propertyName);

            if (existing is not null)
            {
                newProperty.RawValue = existing.RawValue;
            }

            return newProperty;
        }

        private void SetProperty(ICssProperty property)
        {
            if (property.IsShorthand)
            {
                SetShorthand(property);
            }
            else
            {
                SetLonghand(property);
            }
        }

        private void RemovePropertyByName(String propertyName)
        {
            var info = _context.GetDeclarationInfo(propertyName);
            var longhands = info.Longhands;

            for (var i = 0; i < _declarations.Count; i++)
            {
                var declaration = _declarations[i];

                if (declaration.Name.Is(propertyName))
                {
                    _declarations.RemoveAt(i);
                    break;
                }
            }

            foreach (var longhand in longhands)
            {
                RemovePropertyByName(longhand);
            }
        }

        private void ChangeDeclarations(IEnumerable<ICssProperty> decls, Predicate<ICssProperty> defaultSkip, Func<ICssProperty, ICssProperty, Boolean> removeExisting)
        {
            if (decls is null)
            {
                return;
            }

            var declarations = new List<ICssProperty>();

            foreach (var newdecl in decls)
            {
                var skip = defaultSkip(newdecl);

                for (var i = 0; i < _declarations.Count; i++)
                {
                    var olddecl = _declarations[i];

                    if (olddecl.Name.Is(newdecl.Name))
                    {
                        if (removeExisting.Invoke(olddecl, newdecl))
                        {
                            _declarations.RemoveAt(i);
                        }
                        else
                        {
                            skip = true;
                        }

                        break;
                    }
                }

                if (!skip)
                {
                    declarations.Add(newdecl);
                }
            }

            _declarations.AddRange(declarations);
        }

        private void SetLonghand(ICssProperty property)
        {
            for (var i = 0; i < _declarations.Count; i++)
            {
                var declaration = _declarations[i];

                if (declaration.Name.Is(property.Name))
                {
                    _declarations[i] = property;
                    return;
                }
            }

            _declarations.Add(property);
        }

        private void SetShorthand(ICssProperty shorthand)
        {
            var properties = _context.CreateLonghands(shorthand);

            if (properties is not null)
            {
                foreach (var property in properties)
                {
                    SetProperty(property);
                }
            }
        }

        private void RaiseChanged()
        {
            if (!_updating)
            {
                _updating = true;
                Changed?.Invoke(CssText);
                _updating = false;
            }
        }

        #endregion

        #region Interface implementation

        public IEnumerator<ICssProperty> GetEnumerator() => Declarations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Hardcoded Styles Properties


        public string AlignContent
        {
            get => GetPropertyValue(PropertyNames.AlignContent);
            set => SetProperty(PropertyNames.AlignContent, value);
        }

        public string AlignItems
        {
            get => GetPropertyValue(PropertyNames.AlignItems);
            set => SetProperty(PropertyNames.AlignItems, value);
        }

        public string AlignSelf
        {
            get => GetPropertyValue(PropertyNames.AlignSelf);
            set => SetProperty(PropertyNames.AlignSelf, value);
        }

        public string Accelerator
        {
            get => GetPropertyValue(PropertyNames.Accelerator);
            set => SetProperty(PropertyNames.Accelerator, value);
        }

        public string AlignmentBaseline
        {
            // TODO: should we change Property name it does not align
            get => GetPropertyValue(PropertyNames.AlignBaseline);
            set => SetProperty(PropertyNames.AlignBaseline, value);
        }

        public string Animation
        {
            get => GetPropertyValue(PropertyNames.Animation);
            set => SetProperty(PropertyNames.Animation, value);
        }

        public string AnimationDelay
        {
            get => GetPropertyValue(PropertyNames.AnimationDelay);
            set => SetProperty(PropertyNames.AnimationDelay, value);
        }

        public string AnimationDirection
        {
            get => GetPropertyValue(PropertyNames.AnimationDirection);
            set => SetProperty(PropertyNames.AnimationDirection, value);
        }

        public string AnimationDuration
        {
            get => GetPropertyValue(PropertyNames.AnimationDuration);
            set => SetProperty(PropertyNames.AnimationDuration, value);
        }

        public string AnumationFillMode
        {
            get => GetPropertyValue(PropertyNames.AnimationFillMode);
            set => SetProperty(PropertyNames.AnimationFillMode, value);
        }

        public string AnimationIterationCount
        {
            get => GetPropertyValue(PropertyNames.AnimationIterationCount);
            set => SetProperty(PropertyNames.AnimationIterationCount, value);
        }

        public string AnimationName
        {
            get => GetPropertyValue(PropertyNames.AnimationName);
            set => SetProperty(PropertyNames.AnimationName, value);
        }

        public string AnimationPlayState
        {
            get => GetPropertyValue(PropertyNames.AnimationPlayState);
            set => SetProperty(PropertyNames.AnimationPlayState, value);
        }

        public string AnimationTimingFunction
        {
            get => GetPropertyValue(PropertyNames.AnimationTimingFunction);
            set => SetProperty(PropertyNames.AnimationTimingFunction, value);
        }

        public string BackfaceVisibility
        {
            get => GetPropertyValue(PropertyNames.BackfaceVisibility);
            set => SetProperty(PropertyNames.BackfaceVisibility, value);
        }

        public string Background
        {
            get => GetPropertyValue(PropertyNames.Background);
            set => SetProperty(PropertyNames.Background, value);
        }

        public string BackgroundAttachment
        {
            get => GetPropertyValue(PropertyNames.BackgroundAttachment);
            set => SetProperty(PropertyNames.BackgroundAttachment, value);
        }

        public string BackgroundClip
        {
            get => GetPropertyValue(PropertyNames.BackgroundClip);
            set => SetProperty(PropertyNames.BackgroundClip, value);
        }

        public string BackgroundColor
        {
            get => GetPropertyValue(PropertyNames.BackgroundColor);
            set => SetProperty(PropertyNames.BackgroundColor, value);
        }

        public string BackgroundImage
        {
            get => GetPropertyValue(PropertyNames.BackgroundImage);
            set => SetProperty(PropertyNames.BackgroundImage, value);
        }

        public string BackgroundOrigin
        {
            get => GetPropertyValue(PropertyNames.BackgroundOrigin);
            set => SetProperty(PropertyNames.BackgroundOrigin, value);
        }

        public string BackgroundPosition
        {
            get => GetPropertyValue(PropertyNames.BackgroundPosition);
            set => SetProperty(PropertyNames.BackgroundPosition, value);
        }

        public string BackgroundPositionX
        {
            get => GetPropertyValue(PropertyNames.BackgroundPositionX);
            set => SetProperty(PropertyNames.BackgroundPositionX, value);
        }

        public string BackgroundPositionY
        {
            get => GetPropertyValue(PropertyNames.BackgroundPositionY);
            set => SetProperty(PropertyNames.BackgroundPositionY, value);
        }

        public string BackgroundRepeat
        {
            get => GetPropertyValue(PropertyNames.BackgroundRepeat);
            set => SetProperty(PropertyNames.BackgroundRepeat, value);
        }

        public string BackgroundSize
        {
            get => GetPropertyValue(PropertyNames.BackgroundSize);
            set => SetProperty(PropertyNames.BackgroundSize, value);
        }

        public string BaselineShift
        {
            get => GetPropertyValue(PropertyNames.BaselineShift);
            set => SetProperty(PropertyNames.BaselineShift, value);
        }

        public string Behavior
        {
            get => GetPropertyValue(PropertyNames.Behavior);
            set => SetProperty(PropertyNames.Behavior, value);
        }

        public string Bottom
        {
            get => GetPropertyValue(PropertyNames.Bottom);
            set => SetProperty(PropertyNames.Bottom, value);
        }

        public string Border
        {
            get => GetPropertyValue(PropertyNames.Border);
            set => SetProperty(PropertyNames.Border, value);
        }

        public string BorderBottom
        {
            get => GetPropertyValue(PropertyNames.BorderBottom);
            set => SetProperty(PropertyNames.BorderBottom, value);
        }

        public string BorderBottomColor
        {
            get => GetPropertyValue(PropertyNames.BorderBottomColor);
            set => SetProperty(PropertyNames.BorderBottomColor, value);
        }

        public string BorderBottomLeftRadius
        {
            get => GetPropertyValue(PropertyNames.BorderBottomLeftRadius);
            set => SetProperty(PropertyNames.BorderBottomLeftRadius, value);
        }

        public string BorderBottomRightRadius
        {
            get => GetPropertyValue(PropertyNames.BorderBottomRightRadius);
            set => SetProperty(PropertyNames.BorderBottomRightRadius, value);
        }

        public string BorderBottomStyle
        {
            get => GetPropertyValue(PropertyNames.BorderBottomStyle);
            set => SetProperty(PropertyNames.BorderBottomStyle, value);
        }

        public string BorderBottomWidth
        {
            get => GetPropertyValue(PropertyNames.BorderBottomWidth);
            set => SetProperty(PropertyNames.BorderBottomWidth, value);
        }

        public string BorderCollapse
        {
            get => GetPropertyValue(PropertyNames.BorderCollapse);
            set => SetProperty(PropertyNames.BorderCollapse, value);
        }

        public string BorderColor
        {
            get => GetPropertyValue(PropertyNames.BorderColor);
            set => SetProperty(PropertyNames.BorderColor, value);
        }

        public string BorderImage
        {
            get => GetPropertyValue(PropertyNames.BorderImage);
            set => SetProperty(PropertyNames.BorderImage, value);
        }

        public string BorderImageOutset
        {
            get => GetPropertyValue(PropertyNames.BorderImageOutset);
            set => SetProperty(PropertyNames.BorderImageOutset, value);
        }

        public string BorderImageRepeat
        {
            get => GetPropertyValue(PropertyNames.BorderImageRepeat);
            set => SetProperty(PropertyNames.BorderImageRepeat, value);
        }

        public string BorderImageSlice
        {
            get => GetPropertyValue(PropertyNames.BorderImageSlice);
            set => SetProperty(PropertyNames.BorderImageSlice, value);
        }

        public string BorderImageSource
        {
            get => GetPropertyValue(PropertyNames.BorderImageSource);
            set => SetProperty(PropertyNames.BorderImageSource, value);
        }

        public string BorderImageWidth
        {
            get => GetPropertyValue(PropertyNames.BorderImageWidth);
            set => SetProperty(PropertyNames.BorderImageWidth, value);
        }

        public string BorderLeft
        {
            get => GetPropertyValue(PropertyNames.BorderLeft);
            set => SetProperty(PropertyNames.BorderLeft, value);
        }

        public string BorderLeftColor
        {
            get => GetPropertyValue(PropertyNames.BorderLeftColor);
            set => SetProperty(PropertyNames.BorderLeftColor, value);
        }

        public string BorderLeftStyle
        {
            get => GetPropertyValue(PropertyNames.BorderLeftStyle);
            set => SetProperty(PropertyNames.BorderLeftStyle, value);
        }

        public string BorderLeftWidth
        {
            get => GetPropertyValue(PropertyNames.BorderLeftWidth);
            set => SetProperty(PropertyNames.BorderLeftWidth, value);
        }

        public string BorderRadius
        {
            get => GetPropertyValue(PropertyNames.BorderRadius);
            set => SetProperty(PropertyNames.BorderRadius, value);
        }

        public string BorderRight
        {
            get => GetPropertyValue(PropertyNames.BorderRight);
            set => SetProperty(PropertyNames.BorderRight, value);
        }

        public string BorderRightColor
        {
            get => GetPropertyValue(PropertyNames.BorderRightColor);
            set => SetProperty(PropertyNames.BorderRightColor, value);
        }

        public string BorderRightStyle
        {
            get => GetPropertyValue(PropertyNames.BorderRightStyle);
            set => SetProperty(PropertyNames.BorderRightStyle, value);
        }

        public string BorderRightWidth
        {
            get => GetPropertyValue(PropertyNames.BorderRightWidth);
            set => SetProperty(PropertyNames.BorderRightWidth, value);
        }

        public string BorderSpacing
        {
            get => GetPropertyValue(PropertyNames.BorderSpacing);
            set => SetProperty(PropertyNames.BorderSpacing, value);
        }

        public string BorderStyle
        {
            get => GetPropertyValue(PropertyNames.BorderStyle);
            set => SetProperty(PropertyNames.BorderStyle, value);
        }

        public string BorderTop
        {
            get => GetPropertyValue(PropertyNames.BorderTop);
            set => SetProperty(PropertyNames.BorderTop, value);
        }

        public string BorderTopColor
        {
            get => GetPropertyValue(PropertyNames.BorderTopColor);
            set => SetProperty(PropertyNames.BorderTopColor, value);
        }

        public string BorderTopLeftRadius
        {
            get => GetPropertyValue(PropertyNames.BorderTopLeftRadius);
            set => SetProperty(PropertyNames.BorderTopLeftRadius, value);
        }

        public string BorderTopRightRadius
        {
            get => GetPropertyValue(PropertyNames.BorderTopRightRadius);
            set => SetProperty(PropertyNames.BorderTopRightRadius, value);
        }

        public string BorderTopStyle
        {
            get => GetPropertyValue(PropertyNames.BorderTopStyle);
            set => SetProperty(PropertyNames.BorderTopStyle, value);
        }

        public string BorderTopWidth
        {
            get => GetPropertyValue(PropertyNames.BorderTopWidth);
            set => SetProperty(PropertyNames.BorderTopWidth, value);
        }

        public string BorderWidth
        {
            get => GetPropertyValue(PropertyNames.BorderWidth);
            set => SetProperty(PropertyNames.BorderWidth, value);
        }

        public string BoxShadow
        {
            get => GetPropertyValue(PropertyNames.BoxShadow);
            set => SetProperty(PropertyNames.BoxShadow, value);
        }

        public string BoxSizing
        {
            get => GetPropertyValue(PropertyNames.BoxSizing);
            set => SetProperty(PropertyNames.BoxSizing, value);
        }

        public string BreakAfter
        {
            get => GetPropertyValue(PropertyNames.BreakAfter);
            set => SetProperty(PropertyNames.BreakAfter, value);
        }

        public string BreakBefore
        {
            get => GetPropertyValue(PropertyNames.BreakBefore);
            set => SetProperty(PropertyNames.BreakBefore, value);
        }

        public string BreakInside
        {
            get => GetPropertyValue(PropertyNames.BreakInside);
            set => SetProperty(PropertyNames.BreakInside, value);
        }

        public string CaptionSide
        {
            get => GetPropertyValue(PropertyNames.CaptionSide);
            set => SetProperty(PropertyNames.CaptionSide, value);
        }

        public string Clear
        {
            get => GetPropertyValue(PropertyNames.Clear);
            set => SetProperty(PropertyNames.Clear, value);
        }

        public string Clip
        {
            get => GetPropertyValue(PropertyNames.Clip);
            set => SetProperty(PropertyNames.Clip, value);
        }

        public string ClipBottom
        {
            get => GetPropertyValue(PropertyNames.ClipBottom);
            set => SetProperty(PropertyNames.ClipBottom, value);
        }

        public string ClipLeft
        {
            get => GetPropertyValue(PropertyNames.ClipLeft);
            set => SetProperty(PropertyNames.ClipLeft, value);
        }

        public string ClipPath
        {
            get => GetPropertyValue(PropertyNames.ClipPath);
            set => SetProperty(PropertyNames.ClipPath, value);
        }

        public string ClipRight
        {
            get => GetPropertyValue(PropertyNames.ClipRight);
            set => SetProperty(PropertyNames.ClipRight, value);
        }

        public string ClipRule
        {
            get => GetPropertyValue(PropertyNames.ClipRule);
            set => SetProperty(PropertyNames.ClipRule, value);
        }

        public string ClipTop
        {
            get => GetPropertyValue(PropertyNames.ClipTop);
            set => SetProperty(PropertyNames.ClipTop, value);
        }

        public string Color
        {
            get => GetPropertyValue(PropertyNames.Color);
            set => SetProperty(PropertyNames.Color, value);
        }

        public string ColorInterpolationFilters
        {
            get => GetPropertyValue(PropertyNames.ColorInterpolationFilters);
            set => SetProperty(PropertyNames.ColorInterpolationFilters, value);
        }

        public string ColumnCount
        {
            get => GetPropertyValue(PropertyNames.ColumnCount);
            set => SetProperty(PropertyNames.ColumnCount, value);
        }

        public string ColumnFill
        {
            get => GetPropertyValue(PropertyNames.ColumnFill);
            set => SetProperty(PropertyNames.ColumnFill, value);
        }

        public string ColumnGap
        {
            get => GetPropertyValue(PropertyNames.ColumnGap);
            set => SetProperty(PropertyNames.ColumnGap, value);
        }

        public string ColumnRule
        {
            get => GetPropertyValue(PropertyNames.ColumnRule);
            set => SetProperty(PropertyNames.ColumnRule, value);
        }

        public string ColumnRuleColor
        {
            get => GetPropertyValue(PropertyNames.ColumnRuleColor);
            set => SetProperty(PropertyNames.ColumnRuleColor, value);
        }

        public string ColumnRuleStyle
        {
            get => GetPropertyValue(PropertyNames.ColumnRuleStyle);
            set => SetProperty(PropertyNames.ColumnRuleStyle, value);
        }

        public string ColumnRuleWidth
        {
            get => GetPropertyValue(PropertyNames.ColumnRuleWidth);
            set => SetProperty(PropertyNames.ColumnRuleWidth, value);
        }

        public string Columns
        {
            get => GetPropertyValue(PropertyNames.Columns);
            set => SetProperty(PropertyNames.Columns, value);
        }

        public string ColumnSpan
        {
            get => GetPropertyValue(PropertyNames.ColumnSpan);
            set => SetProperty(PropertyNames.ColumnSpan, value);
        }

        public string ColumnWidth
        {
            get => GetPropertyValue(PropertyNames.ColumnWidth);
            set => SetProperty(PropertyNames.ColumnWidth, value);
        }

        public string Content
        {
            get => GetPropertyValue(PropertyNames.Content);
            set => SetProperty(PropertyNames.Content, value);
        }

        public string CounterIncrement
        {
            get => GetPropertyValue(PropertyNames.CounterIncrement);
            set => SetProperty(PropertyNames.CounterIncrement, value);
        }

        public string CounterReset
        {
            get => GetPropertyValue(PropertyNames.CounterReset);
            set => SetProperty(PropertyNames.CounterReset, value);
        }

        public string CssFloat
        {
            get => GetPropertyValue(PropertyNames.Float);
            set => SetProperty(PropertyNames.Float, value);
        }

        public string Cursor
        {
            get => GetPropertyValue(PropertyNames.Cursor);
            set => SetProperty(PropertyNames.Cursor, value);
        }

        public string Direction
        {
            get => GetPropertyValue(PropertyNames.Direction);
            set => SetProperty(PropertyNames.Direction, value);
        }

        public string Display
        {
            get => GetPropertyValue(PropertyNames.Display);
            set => SetProperty(PropertyNames.Display, value);
        }

        public string DominantBaseline
        {
            get => GetPropertyValue(PropertyNames.DominantBaseline);
            set => SetProperty(PropertyNames.DominantBaseline, value);
        }

        public string EmptyCells
        {
            get => GetPropertyValue(PropertyNames.EmptyCells);
            set => SetProperty(PropertyNames.EmptyCells, value);
        }

        public string EnableBackground
        {
            get => GetPropertyValue(PropertyNames.EnableBackground);
            set => SetProperty(PropertyNames.EnableBackground, value);
        }

        public string Fill
        {
            get => GetPropertyValue(PropertyNames.Fill);
            set => SetProperty(PropertyNames.Fill, value);
        }

        public string FillOpacity
        {
            get => GetPropertyValue(PropertyNames.FillOpacity);
            set => SetProperty(PropertyNames.FillOpacity, value);
        }

        public string FillRule
        {
            get => GetPropertyValue(PropertyNames.FillRule);
            set => SetProperty(PropertyNames.FillRule, value);
        }

        public string Filter
        {
            get => GetPropertyValue(PropertyNames.Filter);
            set => SetProperty(PropertyNames.Filter, value);
        }

        public string Flex
        {
            get => GetPropertyValue(PropertyNames.Flex);
            set => SetProperty(PropertyNames.Flex, value);
        }

        public string FlexBasis
        {
            get => GetPropertyValue(PropertyNames.FlexBasis);
            set => SetProperty(PropertyNames.FlexBasis, value);
        }

        public string FlexDirection
        {
            get => GetPropertyValue(PropertyNames.FlexDirection);
            set => SetProperty(PropertyNames.FlexDirection, value);
        }

        public string FlexFlow
        {
            get => GetPropertyValue(PropertyNames.FlexFlow);
            set => SetProperty(PropertyNames.FlexFlow, value);
        }

        public string FlexGrow
        {
            get => GetPropertyValue(PropertyNames.FlexGrow);
            set => SetProperty(PropertyNames.FlexGrow, value);
        }

        public string FlexShrink
        {
            get => GetPropertyValue(PropertyNames.FlexShrink);
            set => SetProperty(PropertyNames.FlexShrink, value);
        }

        public string FlexWrap
        {
            get => GetPropertyValue(PropertyNames.FlexWrap);
            set => SetProperty(PropertyNames.FlexWrap, value);
        }

        public string Font
        {
            get => GetPropertyValue(PropertyNames.Font);
            set => SetProperty(PropertyNames.Font, value);
        }

        public string FontFamily
        {
            get => GetPropertyValue(PropertyNames.FontFamily);
            set => SetProperty(PropertyNames.FontFamily, value);
        }

        public string FontFeatureSettings
        {
            get => GetPropertyValue(PropertyNames.FontFeatureSettings);
            set => SetProperty(PropertyNames.FontFeatureSettings, value);
        }

        public string FontSize
        {
            get => GetPropertyValue(PropertyNames.FontSize);
            set => SetProperty(PropertyNames.FontSize, value);
        }

        public string FontSizeAdjust
        {
            get => GetPropertyValue(PropertyNames.FontSizeAdjust);
            set => SetProperty(PropertyNames.FontSizeAdjust, value);
        }

        public string FontStretch
        {
            get => GetPropertyValue(PropertyNames.FontStretch);
            set => SetProperty(PropertyNames.FontStretch, value);
        }

        public string FontStyle
        {
            get => GetPropertyValue(PropertyNames.FontStyle);
            set => SetProperty(PropertyNames.FontStyle, value);
        }

        public string FontVariant
        {
            get => GetPropertyValue(PropertyNames.FontVariant);
            set => SetProperty(PropertyNames.FontVariant, value);
        }

        public string FontWeight
        {
            get => GetPropertyValue(PropertyNames.FontWeight);
            set => SetProperty(PropertyNames.FontWeight, value);
        }

        public string GlyphOrientationHorizontal
        {
            get => GetPropertyValue(PropertyNames.GlyphOrientationHorizontal);
            set => SetProperty(PropertyNames.GlyphOrientationHorizontal, value);
        }

        public string GlyphOrientationVertical
        {
            get => GetPropertyValue(PropertyNames.GlyphOrientationVertical);
            set => SetProperty(PropertyNames.GlyphOrientationVertical, value);
        }

        public string Height
        {
            get => GetPropertyValue(PropertyNames.Height);
            set => SetProperty(PropertyNames.Height, value);
        }

        public string ImeMode
        {
            get => GetPropertyValue(PropertyNames.ImeMode);
            set => SetProperty(PropertyNames.ImeMode, value);
        }

        public string JustifyContent
        {
            get => GetPropertyValue(PropertyNames.JustifyContent);
            set => SetProperty(PropertyNames.JustifyContent, value);
        }

        public string LayoutGrid
        {
            get => GetPropertyValue(PropertyNames.LayoutGrid);
            set => SetProperty(PropertyNames.LayoutGrid, value);
        }

        public string LayoutGridChar
        {
            get => GetPropertyValue(PropertyNames.LayoutGridChar);
            set => SetProperty(PropertyNames.LayoutGridChar, value);
        }

        public string LayoutGridLine
        {
            get => GetPropertyValue(PropertyNames.LayoutGridLine);
            set => SetProperty(PropertyNames.LayoutGridLine, value);
        }

        public string LayoutGridMode
        {
            get => GetPropertyValue(PropertyNames.LayoutGridMode);
            set => SetProperty(PropertyNames.LayoutGridMode, value);
        }

        public string LayoutGridType
        {
            get => GetPropertyValue(PropertyNames.LayoutGridType);
            set => SetProperty(PropertyNames.LayoutGridType, value);
        }

        public string Left
        {
            get => GetPropertyValue(PropertyNames.Left);
            set => SetProperty(PropertyNames.Left, value);
        }

        public string LetterSpacing
        {
            get => GetPropertyValue(PropertyNames.LetterSpacing);
            set => SetProperty(PropertyNames.LetterSpacing, value);
        }

        public string LineHeight
        {
            get => GetPropertyValue(PropertyNames.LineHeight);
            set => SetProperty(PropertyNames.LineHeight, value);
        }

        public string ListStyle
        {
            get => GetPropertyValue(PropertyNames.ListStyle);
            set => SetProperty(PropertyNames.ListStyle, value);
        }

        public string ListStyleImage
        {
            get => GetPropertyValue(PropertyNames.ListStyleImage);
            set => SetProperty(PropertyNames.ListStyleImage, value);
        }

        public string ListStylePosition
        {
            get => GetPropertyValue(PropertyNames.ListStylePosition);
            set => SetProperty(PropertyNames.ListStylePosition, value);
        }

        public string ListStyleType
        {
            get => GetPropertyValue(PropertyNames.ListStyleType);
            set => SetProperty(PropertyNames.ListStyleType, value);
        }

        public string Margin
        {
            get => GetPropertyValue(PropertyNames.Margin);
            set => SetProperty(PropertyNames.Margin, value);
        }

        public string MarginBottom
        {
            get => GetPropertyValue(PropertyNames.MarginBottom);
            set => SetProperty(PropertyNames.MarginBottom, value);
        }

        public string MarginLeft
        {
            get => GetPropertyValue(PropertyNames.MarginLeft);
            set => SetProperty(PropertyNames.MarginLeft, value);
        }

        public string MarginRight
        {
            get => GetPropertyValue(PropertyNames.MarginRight);
            set => SetProperty(PropertyNames.MarginRight, value);
        }

        public string MarginTop
        {
            get => GetPropertyValue(PropertyNames.MarginTop);
            set => SetProperty(PropertyNames.MarginTop, value);
        }

        public string Marker
        {
            get => GetPropertyValue(PropertyNames.Marker);
            set => SetProperty(PropertyNames.Marker, value);
        }

        public string MarkerEnd
        {
            get => GetPropertyValue(PropertyNames.MarkerEnd);
            set => SetProperty(PropertyNames.MarkerEnd, value);
        }

        public string MarkerMid
        {
            get => GetPropertyValue(PropertyNames.MarkerMid);
            set => SetProperty(PropertyNames.MarkerMid, value);
        }

        public string MarkerStart
        {
            get => GetPropertyValue(PropertyNames.MarkerStart);
            set => SetProperty(PropertyNames.MarkerStart, value);
        }

        public string Mask
        {
            get => GetPropertyValue(PropertyNames.Mask);
            set => SetProperty(PropertyNames.Mask, value);
        }

        public string MaxHeight
        {
            get => GetPropertyValue(PropertyNames.MaxHeight);
            set => SetProperty(PropertyNames.MaxHeight, value);
        }

        public string MaxWidth
        {
            get => GetPropertyValue(PropertyNames.MaxWidth);
            set => SetProperty(PropertyNames.MaxWidth, value);
        }

        public string MinHeight
        {
            get => GetPropertyValue(PropertyNames.MinHeight);
            set => SetProperty(PropertyNames.MinHeight, value);
        }

        public string MinWidth
        {
            get => GetPropertyValue(PropertyNames.MinWidth);
            set => SetProperty(PropertyNames.MinWidth, value);
        }

        public string Opacity
        {
            get => GetPropertyValue(PropertyNames.Opacity);
            set => SetProperty(PropertyNames.Opacity, value);
        }

        public string Order
        {
            get => GetPropertyValue(PropertyNames.Order);
            set => SetProperty(PropertyNames.Order, value);
        }

        public string Orphans
        {
            get => GetPropertyValue(PropertyNames.Orphans);
            set => SetProperty(PropertyNames.Orphans, value);
        }

        public string Outline
        {
            get => GetPropertyValue(PropertyNames.Outline);
            set => SetProperty(PropertyNames.Outline, value);
        }

        public string OutlineColor
        {
            get => GetPropertyValue(PropertyNames.OutlineColor);
            set => SetProperty(PropertyNames.OutlineColor, value);
        }

        public string OutlineStyle
        {
            get => GetPropertyValue(PropertyNames.OutlineStyle);
            set => SetProperty(PropertyNames.OutlineStyle, value);
        }

        public string OutlineWidth
        {
            get => GetPropertyValue(PropertyNames.OutlineWidth);
            set => SetProperty(PropertyNames.OutlineWidth, value);
        }

        public string Overflow
        {
            get => GetPropertyValue(PropertyNames.Overflow);
            set => SetProperty(PropertyNames.Overflow, value);
        }

        public string OverflowX
        {
            get => GetPropertyValue(PropertyNames.OverflowX);
            set => SetProperty(PropertyNames.OverflowX, value);
        }

        public string OverflowY
        {
            get => GetPropertyValue(PropertyNames.OverflowY);
            set => SetProperty(PropertyNames.OverflowY, value);
        }

        public string Padding
        {
            get => GetPropertyValue(PropertyNames.Padding);
            set => SetProperty(PropertyNames.Padding, value);
        }

        public string PaddingBottom
        {
            get => GetPropertyValue(PropertyNames.PaddingBottom);
            set => SetProperty(PropertyNames.PaddingBottom, value);
        }

        public string PaddingLeft
        {
            get => GetPropertyValue(PropertyNames.PaddingLeft);
            set => SetProperty(PropertyNames.PaddingLeft, value);
        }

        public string PaddingRight
        {
            get => GetPropertyValue(PropertyNames.PaddingRight);
            set => SetProperty(PropertyNames.PaddingRight, value);
        }

        public string PaddingTop
        {
            get => GetPropertyValue(PropertyNames.PaddingTop);
            set => SetProperty(PropertyNames.PaddingTop, value);
        }

        public string PageBreakAfter
        {
            get => GetPropertyValue(PropertyNames.PageBreakAfter);
            set => SetProperty(PropertyNames.PageBreakAfter, value);
        }

        public string PageBreakBefore
        {
            get => GetPropertyValue(PropertyNames.PageBreakBefore);
            set => SetProperty(PropertyNames.PageBreakBefore, value);
        }

        public string PageBreakInside
        {
            get => GetPropertyValue(PropertyNames.PageBreakInside);
            set => SetProperty(PropertyNames.PageBreakInside, value);
        }

        public string Perspective
        {
            get => GetPropertyValue(PropertyNames.Perspective);
            set => SetProperty(PropertyNames.Perspective, value);
        }

        public string PerspectiveOrigin
        {
            get => GetPropertyValue(PropertyNames.PerspectiveOrigin);
            set => SetProperty(PropertyNames.PerspectiveOrigin, value);
        }

        public string PointerEvents
        {
            get => GetPropertyValue(PropertyNames.PointerEvents);
            set => SetProperty(PropertyNames.PointerEvents, value);
        }

        public string Quotes
        {
            get => GetPropertyValue(PropertyNames.Quotes);
            set => SetProperty(PropertyNames.Quotes, value);
        }

        public string Position
        {
            get => GetPropertyValue(PropertyNames.Position);
            set => SetProperty(PropertyNames.Position, value);
        }

        public string Right
        {
            get => GetPropertyValue(PropertyNames.Right);
            set => SetProperty(PropertyNames.Right, value);
        }

        public string RubyAlign
        {
            get => GetPropertyValue(PropertyNames.RubyAlign);
            set => SetProperty(PropertyNames.RubyAlign, value);
        }

        public string RubyOverhang
        {
            get => GetPropertyValue(PropertyNames.RubyOverhang);
            set => SetProperty(PropertyNames.RubyOverhang, value);
        }

        public string RubyPosition
        {
            get => GetPropertyValue(PropertyNames.RubyPosition);
            set => SetProperty(PropertyNames.RubyPosition, value);
        }

        public string Scrollbar3dLightColor
        {
            get => GetPropertyValue(PropertyNames.Scrollbar3dLightColor);
            set => SetProperty(PropertyNames.Scrollbar3dLightColor, value);
        }

        public string ScrollbarArrowColor
        {
            get => GetPropertyValue(PropertyNames.ScrollbarArrowColor);
            set => SetProperty(PropertyNames.ScrollbarArrowColor, value);
        }

        public string ScrollbarDarkShadowColor
        {
            get => GetPropertyValue(PropertyNames.ScrollbarDarkShadowColor);
            set => SetProperty(PropertyNames.ScrollbarDarkShadowColor, value);
        }

        public string ScrollbarFaceColor
        {
            get => GetPropertyValue(PropertyNames.ScrollbarFaceColor);
            set => SetProperty(PropertyNames.ScrollbarFaceColor, value);
        }

        public string ScrollbarHighlightColor
        {
            get => GetPropertyValue(PropertyNames.ScrollbarHighlightColor);
            set => SetProperty(PropertyNames.ScrollbarHighlightColor, value);
        }

        public string ScrollbarShadowColor
        {
            get => GetPropertyValue(PropertyNames.ScrollbarShadowColor);
            set => SetProperty(PropertyNames.ScrollbarShadowColor, value);
        }

        public string ScrollbarTrackColor
        {
            get => GetPropertyValue(PropertyNames.ScrollbarTrackColor);
            set => SetProperty(PropertyNames.ScrollbarTrackColor, value);
        }

        public string Stroke
        {
            get => GetPropertyValue(PropertyNames.Stroke);
            set => SetProperty(PropertyNames.Stroke, value);
        }

        public string StrokeDasharray
        {
            get => GetPropertyValue(PropertyNames.StrokeDasharray);
            set => SetProperty(PropertyNames.StrokeDasharray, value);
        }

        public string StrokeDashoffset
        {
            get => GetPropertyValue(PropertyNames.StrokeDashoffset);
            set => SetProperty(PropertyNames.StrokeDashoffset, value);
        }

        public string StrokeLinecap
        {
            get => GetPropertyValue(PropertyNames.StrokeLinecap);
            set => SetProperty(PropertyNames.StrokeLinecap, value);
        }

        public string StrokeLinejoin
        {
            get => GetPropertyValue(PropertyNames.StrokeLinejoin);
            set => SetProperty(PropertyNames.StrokeLinejoin, value);
        }

        public string StrokeMiterlimit
        {
            get => GetPropertyValue(PropertyNames.StrokeMiterlimit);
            set => SetProperty(PropertyNames.StrokeMiterlimit, value);
        }

        public string StrokeOpacity
        {
            get => GetPropertyValue(PropertyNames.StrokeOpacity);
            set => SetProperty(PropertyNames.StrokeOpacity, value);
        }

        public string StrokeWidth
        {
            get => GetPropertyValue(PropertyNames.StrokeWidth);
            set => SetProperty(PropertyNames.StrokeWidth, value);
        }

        public string TableLayout
        {
            get => GetPropertyValue(PropertyNames.TableLayout);
            set => SetProperty(PropertyNames.TableLayout, value);
        }

        public string TextAlign
        {
            get => GetPropertyValue(PropertyNames.TextAlign);
            set => SetProperty(PropertyNames.TextAlign, value);
        }

        public string TextAlignLast
        {
            get => GetPropertyValue(PropertyNames.TextAlignLast);
            set => SetProperty(PropertyNames.TextAlignLast, value);
        }

        public string TextAnchor
        {
            get => GetPropertyValue(PropertyNames.TextAnchor);
            set => SetProperty(PropertyNames.TextAnchor, value);
        }

        public string TextAutospace
        {
            get => GetPropertyValue(PropertyNames.TextAutospace);
            set => SetProperty(PropertyNames.TextAutospace, value);
        }

        public string TextDecoration
        {
            get => GetPropertyValue(PropertyNames.TextDecoration);
            set => SetProperty(PropertyNames.TextDecoration, value);
        }

        public string TextDecorationStyle
        {
            get => GetPropertyValue(PropertyNames.TextDecorationStyle);
            set => SetProperty(PropertyNames.TextDecorationStyle, value);
        }

        public string TextDecorationLine
        {
            get => GetPropertyValue(PropertyNames.TextDecorationLine);
            set => SetProperty(PropertyNames.TextDecorationLine, value);
        }

        public string TextDecorationColor
        {
            get => GetPropertyValue(PropertyNames.TextDecorationColor);
            set => SetProperty(PropertyNames.TextDecorationColor, value);
        }

        public string TextIndent
        {
            get => GetPropertyValue(PropertyNames.TextIndent);
            set => SetProperty(PropertyNames.TextIndent, value);
        }

        public string TextJustify
        {
            get => GetPropertyValue(PropertyNames.TextJustify);
            set => SetProperty(PropertyNames.TextJustify, value);
        }

        public string TextOverflow
        {
            get => GetPropertyValue(PropertyNames.TextOverflow);
            set => SetProperty(PropertyNames.TextOverflow, value);
        }

        public string TextShadow
        {
            get => GetPropertyValue(PropertyNames.TextShadow);
            set => SetProperty(PropertyNames.TextShadow, value);
        }

        public string TextTransform
        {
            get => GetPropertyValue(PropertyNames.TextTransform);
            set => SetProperty(PropertyNames.TextTransform, value);
        }

        public string TextUnderlinePosition
        {
            get => GetPropertyValue(PropertyNames.TextUnderlinePosition);
            set => SetProperty(PropertyNames.TextUnderlinePosition, value);
        }

        public string Top
        {
            get => GetPropertyValue(PropertyNames.Top);
            set => SetProperty(PropertyNames.Top, value);
        }

        public string Transform
        {
            get => GetPropertyValue(PropertyNames.Transform);
            set => SetProperty(PropertyNames.Transform, value);
        }

        public string TransformOrigin
        {
            get => GetPropertyValue(PropertyNames.TransformOrigin);
            set => SetProperty(PropertyNames.TransformOrigin, value);
        }

        public string TransformStyle
        {
            get => GetPropertyValue(PropertyNames.TransformStyle);
            set => SetProperty(PropertyNames.TransformStyle, value);
        }

        public string Transition
        {
            get => GetPropertyValue(PropertyNames.Transition);
            set => SetProperty(PropertyNames.Transition, value);
        }

        public string TransitionDelay
        {
            get => GetPropertyValue(PropertyNames.TransitionDelay);
            set => SetProperty(PropertyNames.TransitionDelay, value);
        }

        public string TransitionDuration
        {
            get => GetPropertyValue(PropertyNames.TransitionDuration);
            set => SetProperty(PropertyNames.TransitionDuration, value);
        }

        public string TransitionProperty
        {
            get => GetPropertyValue(PropertyNames.TransitionProperty);
            set => SetProperty(PropertyNames.TransitionProperty, value);
        }

        public string TransitionTimingFunction
        {
            get => GetPropertyValue(PropertyNames.TransitionTimingFunction);
            set => SetProperty(PropertyNames.TransitionTimingFunction, value);
        }

        public string UnicodeBidi
        {
            get => GetPropertyValue(PropertyNames.UnicodeBidi);
            set => SetProperty(PropertyNames.UnicodeBidi, value);
        }

        public string VerticalAlign
        {
            get => GetPropertyValue(PropertyNames.VerticalAlign);
            set => SetProperty(PropertyNames.VerticalAlign, value);
        }

        public string Visibility
        {
            get => GetPropertyValue(PropertyNames.Visibility);
            set => SetProperty(PropertyNames.Visibility, value);
        }

        public string WhiteSpace
        {
            get => GetPropertyValue(PropertyNames.WhiteSpace);
            set => SetProperty(PropertyNames.WhiteSpace, value);
        }

        public string Widows
        {
            get => GetPropertyValue(PropertyNames.Widows);
            set => SetProperty(PropertyNames.Widows, value);
        }

        public string Width
        {
            get => GetPropertyValue(PropertyNames.Width);
            set => SetProperty(PropertyNames.Width, value);
        }

        public string WordBreak
        {
            get => GetPropertyValue(PropertyNames.WordBreak);
            set => SetProperty(PropertyNames.WordBreak, value);
        }

        public string WordSpacing
        {
            get => GetPropertyValue(PropertyNames.WordSpacing);
            set => SetProperty(PropertyNames.WordSpacing, value);
        }

        public string WordWrap
        {
            get => GetPropertyValue(PropertyNames.WordWrap);
            set => SetProperty(PropertyNames.WordWrap, value);
        }

        public string OverflowWrap
        {
            get => GetPropertyValue(PropertyNames.OverflowWrap);
            set => SetProperty(PropertyNames.OverflowWrap, value);
        }

        public string WritingMode
        {
            get => GetPropertyValue(PropertyNames.WritingMode);
            set => SetProperty(PropertyNames.WritingMode, value);
        }

        public string ZIndex
        {
            get => GetPropertyValue(PropertyNames.ZIndex);
            set => SetProperty(PropertyNames.ZIndex, value);
        }

        public string Zoom
        {
            get => GetPropertyValue(PropertyNames.Zoom);
            set => SetProperty(PropertyNames.Zoom, value);
        }

        #endregion
    }
}
