namespace AngleSharp.Css.Tests.Styling
{
    using AngleSharp.Css.Converters;
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Parser;
    using NUnit.Framework;
    using System;
    using System.IO;
    using static CssConstructionFunctions;
    using static ValueConverters;

    [TestFixture]
    public class CssSheetTests
    {
        [Test]
        public void CssSheetOnEofDuringRuleWithoutSemicolon()
        {
            var sheet = ParseStyleSheet(@"
h1 {
 color: red;
 font-weight: bold");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var h1 = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("h1", h1.SelectorText);
            Assert.AreEqual("rgba(255, 0, 0, 1)", h1.Style.Color);
            Assert.AreEqual("bold", h1.Style.FontWeight);
        }

        [Test]
        public void CssSheet1WithDoubleMarkedCommentFromIssue93()
        {
            var sheet = ParseStyleSheet(@"
            /**special css**/
            .dis-none { display: none;}
            .dis { display: block; }
            /*common css*/
            .dis2 { display: block; }
            ");
            var css = sheet.ToCss();
            Assert.AreEqual(3, sheet.Rules.Length);
            Assert.AreEqual(".dis-none { display: none }", sheet.Rules[0].CssText);
            Assert.AreEqual(".dis { display: block }", sheet.Rules[1].CssText);
            Assert.AreEqual(".dis2 { display: block }", sheet.Rules[2].CssText);
        }

        [Test]
        public void CssSheet2WithDoubleMarkedCommentFromIssue93()
        {
            var sheet = ParseStyleSheet(@"
            /**special css**/
            .dis-none { display: none;}
            .dis { display: block; }
            ");
            var css = sheet.ToCss();
            Assert.AreEqual(2, sheet.Rules.Length);
            Assert.AreEqual(".dis-none { display: none }", sheet.Rules[0].CssText);
            Assert.AreEqual(".dis { display: block }", sheet.Rules[1].CssText);
        }

        [Test]
        public void CssSheetSerializeListStyleNone()
        {
            var cssSrc = ".T1 {list-style:NONE}";
            var expected = ".T1 { list-style: none }";
            var stylesheet = ParseStyleSheet(cssSrc);
            var cssText = stylesheet.ToCss();
            Assert.AreEqual(expected, cssText);
        }

        [Test]
        public void CssSheetSerializeBorder1pxOutset()
        {
            var cssSrc = ".T2 { border:1px  outset }";
            var expected = ".T2 { border: 1px outset }";
            var stylesheet = ParseStyleSheet(cssSrc);
            var cssText = stylesheet.ToCss();
            Assert.AreEqual(expected, cssText);
        }

        [Test]
        public void CssSheetSerializeBorder1pxSolidWithColor()
        {
            var cssSrc = "#rule1 { border: 1px solid #BBCCEB; border-top: none }";
            var expected =
                "#rule1 { border-top: none; border-right: 1px solid rgba(187, 204, 235, 1); border-bottom: 1px solid rgba(187, 204, 235, 1); border-left: 1px solid rgba(187, 204, 235, 1) }";
            var stylesheet = ParseStyleSheet(cssSrc);
            var cssText = stylesheet.ToCss();
            Assert.AreEqual(expected, cssText);
        }

        [Test]
        public void CssSheetSerializeBackgroundWithUrlPositionRepeatX()
        {
            var cssSrc = "#rule2 { background:url(/_static/img/bx_tile.gif) top left repeat-x; }";
            var expected = "#rule2 { background: url(\"/_static/img/bx_tile.gif\") left top repeat-x }";
            var stylesheet = ParseStyleSheet(cssSrc);
            var cssText = stylesheet.ToCss();
            Assert.AreEqual(expected, cssText);
        }

        [Test]
        public void CssSheetIgnoreVendorPrefixes()
        {
            var css = @".something {
  -o-border-radius: 5px;
  -webkit-border-radius: 5px;
  border-radius: 5px;
  display: -webkit-box;
  display: -webkit-flex;
  display: -ms-flexbox;
  display: flex;
  background: -webkit-linear-gradient(red, green);
  background: linear-gradient(red, green);
}";
            var stylesheet = ParseStyleSheet(css);
            Assert.AreEqual(1, stylesheet.Rules.Length);
            var style = stylesheet.Rules[0] as ICssStyleRule;
            Assert.IsNotNull(style);
            Assert.AreEqual(15, style.Style.Length);
        }

        [Test]
        public void CssSheetSimpleStyleRuleStringification()
        {
            var css = @"html { font-family: sans-serif }";
            var stylesheet = ParseStyleSheet(css);
            Assert.AreEqual(1, stylesheet.Rules.Length);
            var rule = stylesheet.Rules[0];
            Assert.IsInstanceOf<CssStyleRule>(rule);
            Assert.AreEqual(css, rule.CssText);
        }

        [Test]
        public void CssSheetCloseStringsEndOfLine()
        {
            var sheet = ParseStyleSheet(@"p {
        color: green;
        font-family: 'Courier New Times
        color: red;
        color: green;
      }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var p = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual(1, p.Style.Length);
            Assert.AreEqual("p", p.SelectorText);
            Assert.AreEqual("color", p.Style[0]);
            Assert.AreEqual("rgba(0, 128, 0, 1)", p.Style.Color);
            Assert.AreEqual("", p.Style.FontFamily);
        }

        [Test]
        public void CssSheetOnEofDuringRuleWithinString()
        {
            var sheet = ParseStyleSheet(@"
#something {
 content: 'hi there");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var id = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("#something", id.SelectorText);
            Assert.AreEqual("\"hi there\"", id.Style.Content);
        }

        [Test]
        public void CssSheetOnEofDuringAtMediaRuleWithinString()
        {
            var sheet = ParseStyleSheet(@"  @media screen {
    p:before { content: 'Hello");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssMediaRule>(sheet.Rules[0]);
            var media = sheet.Rules[0] as CssMediaRule;
            Assert.AreEqual("screen", media.Media.MediaText);
            Assert.AreEqual(1, media.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(media.Rules[0]);
            var p = media.Rules[0] as ICssStyleRule;
            Assert.AreEqual("p:before", p.SelectorText);
            Assert.AreEqual("\"Hello\"", p.Style.Content);
        }

        [Test]
        public void CssSheetDoIgnoreUnknownPropertyByDefault()
        {
            var sheet = ParseStyleSheet(@"h1 { color: red; rotation: 70minutes }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var h1 = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("h1", h1.SelectorText);
            Assert.AreEqual(1, h1.Style.Length);
            Assert.AreEqual("color", h1.Style[0]);
            Assert.AreEqual("rgba(255, 0, 0, 1)", h1.Style.Color);
        }

        [Test]
        public void CssSheetNotIgnoreUnknownPropertyViaOptions()
        {
            var sheet = ParseStyleSheet(@"h1 { color: red; rotation: 70minutes }", new CssParserOptions
            {
                IsIncludingUnknownDeclarations = true,
            });
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var h1 = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("h1", h1.SelectorText);
            Assert.AreEqual(2, h1.Style.Length);
            Assert.AreEqual("color", h1.Style[0]);
            Assert.AreEqual("rgba(255, 0, 0, 1)", h1.Style.Color);
            Assert.AreEqual("rotation", h1.Style[1]);
        }

        [Test]
        public void CssSheetInvalidStatementRulesetUnexpectedAtKeyword()
        {
            var sheet = ParseStyleSheet(@"p @here {color: red}");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.AreEqual(null, (sheet.Rules[0] as ICssStyleRule).SelectorText);
        }

        [Test]
        public void CssSheetInvalidStatementAtRuleUnexpectedAtKeyword()
        {
            var sheet = ParseStyleSheet(@"@foo @bar;");
            Assert.AreEqual(0, sheet.Rules.Length);
        }

        [Test]
        public void CssSheetInvalidStatementRulesetUnexpectedRightBrace()
        {
            var sheet = ParseStyleSheet(@"}} {{ - }}");
            Assert.AreEqual(0, sheet.Rules.Length);
        }

        [Test]
        public void CssSheetInvalidStatementRulesetUnexpectedRightBraceWithValidQualifiedRule()
        {
            var sheet = ParseStyleSheet(@"}} {{ - }}
#hi { color: green; }");
            Assert.AreEqual(1, sheet.Rules.Length);
            var style = sheet.Rules[0] as ICssStyleRule;
            Assert.NotNull(style);
            Assert.AreEqual("#hi", style.SelectorText);
            Assert.AreEqual(1, style.Style.Length);
            Assert.AreEqual("rgba(0, 128, 0, 1)", style.Style.Color);
        }

        [Test]
        public void CssSheetInvalidStatementRulesetUnexpectedRightParenthesis()
        {
            var sheet = ParseStyleSheet(@") ( {} ) p {color: red }");
            Assert.AreEqual(0, sheet.Rules.Length);
        }

        [Test]
        public void CssSheetInvalidStatementRulesetUnexpectedRightParenthesisWithValidQualifiedRule()
        {
            var sheet = ParseStyleSheet(@") {} p {color: green }");
            Assert.AreEqual(1, sheet.Rules.Length);
            var style = sheet.Rules[0] as ICssStyleRule;
            Assert.NotNull(style);
            Assert.AreEqual("p", style.SelectorText);
            Assert.AreEqual(1, style.Style.Length);
            Assert.AreEqual("rgba(0, 128, 0, 1)", style.Style.Color);
        }

        [Test]
        public void CssSheetIgnoreUnknownAtRule()
        {
            var sheet = ParseStyleSheet(@"@three-dee {
  @background-lighting {
    azimuth: 30deg;
    elevation: 190deg;
  }
  h1 { color: red }
}
h1 { color: blue }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var h1 = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("h1", h1.SelectorText);
            Assert.AreEqual(1, h1.Style.Length);
            Assert.AreEqual("color", h1.Style[0]);
            Assert.AreEqual("rgba(0, 0, 255, 1)", h1.Style.Color);
        }

        [Test]
        public void CssSheetKeepValidValueFloat()
        {
            var sheet = ParseStyleSheet(@"img { float: left }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var img = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("img", img.SelectorText);
            Assert.AreEqual(1, img.Style.Length);
            Assert.AreEqual("float", img.Style[0]);
            Assert.AreEqual("left", img.Style.CssFloat);
        }

        [Test]
        public void CssSheetIgnoreInvalidValueFloat()
        {
            var sheet = ParseStyleSheet(@"img { float: left here }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var img = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("img", img.SelectorText);
            Assert.AreEqual(0, img.Style.Length);
            Assert.AreEqual("", img.Style.CssFloat);
        }

        [Test]
        public void CssSheetIgnoreInvalidValueBackground()
        {
            var sheet = ParseStyleSheet(@"img { background: ""red"" }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var img = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("img", img.SelectorText);
            Assert.AreEqual(0, img.Style.Length);
            Assert.AreEqual("", img.Style.Background);
        }

        [Test]
        public void CssSheetIgnoreInvalidValueBorderWidth()
        {
            var sheet = ParseStyleSheet(@"img { border-width: 3 }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var img = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("img", img.SelectorText);
            Assert.AreEqual(0, img.Style.Length);
        }

        [Test]
        public void CssSheetWellformedDeclaration()
        {
            var sheet = ParseStyleSheet(@"p { color:green; }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var p = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("p", p.SelectorText);
            Assert.AreEqual(1, p.Style.Length);
            Assert.AreEqual("color", p.Style[0]);
            Assert.AreEqual("rgba(0, 128, 0, 1)", p.Style.Color);
        }

        [Test]
        public void CssSheetMalformedDeclarationMissingColon()
        {
            var sheet = ParseStyleSheet(@"p { color:green; color }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var p = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("p", p.SelectorText);
            Assert.AreEqual(1, p.Style.Length);
            Assert.AreEqual("color", p.Style[0]);
            Assert.AreEqual("rgba(0, 128, 0, 1)", p.Style.Color);
        }

        [Test]
        public void CssSheetMalformedDeclarationMissingColonWithRecovery()
        {
            var sheet = ParseStyleSheet(@"p { color:red;   color; color:green }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var p = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("p", p.SelectorText);
            Assert.AreEqual(1, p.Style.Length);
            Assert.AreEqual("color", p.Style[0]);
            Assert.AreEqual("rgba(0, 128, 0, 1)", p.Style.Color);
        }

        [Test]
        public void CssSheetMalformedDeclarationMissingValue()
        {
            var sheet = ParseStyleSheet(@"p { color:green; color: }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var p = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("p", p.SelectorText);
            Assert.AreEqual(1, p.Style.Length);
            Assert.AreEqual("color", p.Style[0]);
            Assert.AreEqual("rgba(0, 128, 0, 1)", p.Style.Color);
        }

        [Test]
        public void CssSheetMalformedDeclarationUnexpectedTokens()
        {
            var sheet = ParseStyleSheet(@"p { color:green; color{;color:maroon} }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var p = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("p", p.SelectorText);
            Assert.AreEqual(1, p.Style.Length);
            Assert.AreEqual("color", p.Style[0]);
            Assert.AreEqual("rgba(0, 128, 0, 1)", p.Style.Color);
        }

        [Test]
        public void CssSheetMalformedDeclarationUnexpectedTokensWithRecovery()
        {
            var sheet = ParseStyleSheet(@"p { color:red;   color{;color:maroon}; color:green }");
            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.IsInstanceOf<CssStyleRule>(sheet.Rules[0]);
            var p = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("p", p.SelectorText);
            Assert.AreEqual(1, p.Style.Length);
            Assert.AreEqual("color", p.Style[0]);
            Assert.AreEqual("rgba(0, 128, 0, 1)", p.Style.Color);
        }

        [Test]
        public void CssCreateValueListConformal()
        {
            var valueString = "24px 12px 6px";
            var converter = LengthConverter.Periodic();
            var value = converter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssCreateValueListNonConformal()
        {
            var valueString = "  24px  12px 6px  13px ";
            var converter = LengthConverter.Periodic();
            var value = converter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssCreateValueListEmpty()
        {
            var valueString = "";
            var value = FontFamiliesConverter.Convert(valueString);
            Assert.IsNull(value);
        }

        [Test]
        public void CssCreateValueListSpaces()
        {
            var valueString = "  ";
            var value = FontFamiliesConverter.Convert(valueString);
            Assert.IsNull(value);
        }

        [Test]
        public void CssCreateValueListIllegal()
        {
            var valueString = " , ";
            var value = FontFamiliesConverter.Convert(valueString);
            Assert.IsNull(value);
        }

        [Test]
        public void CssCreateMultipleValues()
        {
            var valueString = "Arial, Verdana, Helvetica, Sans-Serif";
            var value = FontFamiliesConverter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssCreateMultipleValuesNonConformal()
        {
            var valueString = "  Arial  ,  Verdana  ,Helvetica,Sans-Serif   ";
            var value = FontFamiliesConverter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssColorBlack()
        {
            var valueString = "#000000";
            var value = ColorConverter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssColorRed()
        {
            var valueString = "#FF0000";
            var value = ColorConverter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssColorMixedShort()
        {
            var valueString = "#07C";
            var value = ColorConverter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssColorGreenShort()
        {
            var valueString = "#00F";
            var value = ColorConverter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssColorRedShort()
        {
            var valueString = "#F00";
            var value = ColorConverter.Convert(valueString);
            Assert.IsNotNull(value);
        }

        [Test]
        public void CssRgbaFunction()
        {
            var names = new[] { "border-top-color", "border-right-color", "border-bottom-color", "border-left-color" };
            var decls = ParseDeclarations("border-color: rgba(82, 168, 236, 0.8)");
            Assert.IsNotNull(decls);
            Assert.AreEqual(4, decls.Length);

            for (int i = 0; i < decls.Length; i++)
            {
                var propertyName = decls[i];
                var property = decls.GetProperty(propertyName);
                Assert.AreEqual(names[i], property.Name);
                Assert.AreEqual(propertyName, property.Name);
                Assert.IsFalse(property.IsImportant);
                Assert.AreEqual("rgba(82, 168, 236, 0.8)", property.Value);
            }
        }

        [Test]
        public void CssMarginAll()
        {
            var names = new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" };
            var decls = ParseDeclarations("margin: 20px;");
            Assert.IsNotNull(decls);
            Assert.AreEqual(4, decls.Length);

            for (int i = 0; i < decls.Length; i++)
            {
                var propertyName = decls[i];
                var decl = decls.GetProperty(propertyName);
                Assert.AreEqual(names[i], decl.Name);
                Assert.AreEqual(propertyName, decl.Name);
                Assert.IsFalse(decl.IsImportant);
                Assert.AreEqual("20px", decl.Value);
            }
        }

        [Test]
        public void CssSeveralFontFamily()
        {
            var prop = ParseDeclaration("font-family: \"Helvetica Neue\", Helvetica, Arial, sans-serif");
            Assert.AreEqual("font-family", prop.Name);
            Assert.IsFalse(prop.IsImportant);
            Assert.AreEqual("\"Helvetica Neue\", Helvetica, Arial, sans-serif", prop.Value);
        }

        [Test]
        public void CssFontWithSlashAndContent()
        {
            var decl = ParseDeclarations("font: bold 1em/2em monospace; content: \" (\" attr(href) \")\"");
            Assert.IsNotNull(decl);
            Assert.AreEqual(8, decl.Length);

            Assert.AreEqual("bold 1em / 2em monospace", decl.GetPropertyValue("font"));

            var content = decl.GetProperty("content");
            Assert.AreEqual("content", content.Name);
            Assert.IsFalse(content.IsImportant);
            Assert.AreEqual("\" (\" attr(href) \")\"", content.Value);
        }

        [Test]
        public void CssBackgroundWebkitGradientIsInvalid()
        {
            var background =
                ParseDeclaration(
                    "background: -webkit-gradient(linear, left top, left bottom, color-stop(0%, #FFA84C), color-stop(100%, #FF7B0D))");
            Assert.IsFalse(background.HasValue);
        }

        [Test]
        public void CssBackgroundColorRgba()
        {
            var background = ParseDeclaration("background-color: rgba(255, 123, 13, 1)");
            Assert.AreEqual("background-color", background.Name);
            Assert.IsFalse(background.IsImportant);
            Assert.AreEqual("rgba(255, 123, 13, 1)", background.Value);
        }

        [Test]
        public void CssFontWithFraction()
        {
            var font = ParseDeclaration("font:bold 40px/1.13 'PT Sans Narrow', sans-serif");
            Assert.AreEqual("font", font.Name);
            Assert.IsFalse(font.IsImportant);
        }

        [Test]
        public void CssTextShadow()
        {
            var textShadow = ParseDeclaration("text-shadow: 0 0 10px #000");
            Assert.AreEqual("text-shadow", textShadow.Name);
            Assert.IsFalse(textShadow.IsImportant);
        }

        [Test]
        public void CssBackgroundWithImage()
        {
            var background = ParseDeclaration("background:url(../images/ribbon.svg) no-repeat");
            Assert.AreEqual("background", background.Name);
            Assert.IsFalse(background.IsImportant);
        }

        [Test]
        public void CssContentWithCounter()
        {
            var content = ParseDeclaration("content:counter(paging, decimal-leading-zero)");
            Assert.AreEqual("content", content.Name);
            Assert.IsFalse(content.IsImportant);
        }

        [Test]
        public void CssBackgroundColorRgb()
        {
            var backgroundColor = ParseDeclaration("background-color: rgb(245, 0, 111)");
            Assert.AreEqual("background-color", backgroundColor.Name);
            Assert.IsFalse(backgroundColor.IsImportant);
        }

        [Test]
        public void CssImportSheet()
        {
            var rule = "@import url(fonts.css);";
            var decl = ParseRule(rule);
            Assert.IsNotNull(decl);
            Assert.IsInstanceOf<CssImportRule>(decl);
            var importRule = (CssImportRule)decl;
            Assert.AreEqual("fonts.css", importRule.Href);
        }

        [Test]
        public void CssContentEscaped()
        {
            var content = ParseDeclaration("content:'\005E'");
            Assert.AreEqual("content", content.Name);
            Assert.IsFalse(content.IsImportant);
        }

        [Test]
        public void CssContentCounter()
        {
            var content = ParseDeclaration("content:counter(list)'.'");
            Assert.AreEqual("content", content.Name);
            Assert.IsFalse(content.IsImportant);
        }

        [Test]
        public void CssTransformTranslate()
        {
            var transform = ParseDeclaration("transform:translateY(-50%)");
            Assert.AreEqual("transform", transform.Name);
            Assert.IsFalse(transform.IsImportant);
        }

        [Test]
        public void CssBoxShadowMultiline()
        {
            var boxShadow = ParseDeclaration(@"
        box-shadow:
			0 0 0 10px rgba(60, 61, 64, 0.6),
			0 0 50px #3C3D40;");
            Assert.AreEqual("box-shadow", boxShadow.Name);
            Assert.IsFalse(boxShadow.IsImportant);
        }

        [Test]
        public void CssDisplayBlock()
        {
            var display = ParseDeclaration("display:block");
            Assert.AreEqual("display", display.Name);
            Assert.IsFalse(display.IsImportant);
            Assert.AreEqual("block", display.Value);
        }

        [Test]
        public void CssSheetWithDataUrlAsBackgroundImage()
        {
            var sheet = ParseStyleSheet(
                ".App_Header_ .logo { background-image: url(\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEcAAAAcCAMAAAAEJ1IZAAAABGdBTUEAALGPC/xhBQAAVAI/VAI/VAI/VAI/VAI/VAI/VAAAA////AI/VRZ0U8AAAAFJ0Uk5TYNV4S2UbgT/Gk6uQt585w2wGXS0zJO2lhGttJK6j4YqZSobH1AAAAAElFTkSuQmCC\"); background-size: 71px 28px; background-position: 0 19px; width: 71px; }");
            Assert.IsNotNull(sheet);
            Assert.AreEqual(1, sheet.Rules.Length);
            var rule = sheet.Rules[0] as CssStyleRule;
            Assert.IsNotNull(rule);
            Assert.AreEqual(5, rule.Style.Length);
            Assert.AreEqual(".App_Header_ .logo", rule.SelectorText);
            var decl = rule.Style as ICssStyleDeclaration;
            Assert.AreEqual(
                "url(\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEcAAAAcCAMAAAAEJ1IZAAAABGdBTUEAALGPC/xhBQAAVAI/VAI/VAI/VAI/VAI/VAI/VAAAA////AI/VRZ0U8AAAAFJ0Uk5TYNV4S2UbgT/Gk6uQt585w2wGXS0zJO2lhGttJK6j4YqZSobH1AAAAAElFTkSuQmCC\")",
                decl.BackgroundImage);
            Assert.AreEqual("71px 28px", decl.BackgroundSize);
            Assert.AreEqual("0 19px", decl.BackgroundPosition);
            Assert.AreEqual("71px", decl.Width);
        }

        [Test]
        public void CssSheetFromStreamWeirdBytesLeadingToInfiniteLoop()
        {
            var bs = new Byte[8];
            bs[0] = 239;
            bs[1] = 187;
            bs[2] = 191;
            bs[3] = 117;
            bs[4] = 43;
            bs[5] = 63;
            bs[6] = 63;
            bs[7] = 63;

            using (var memoryStream = new MemoryStream(bs, false))
            {
                var sheet = ParseStyleSheet(memoryStream);
            }
        }

        [Test]
        public void CssSheetFromStreamOnlyZerosAvailable()
        {
            var bs = new Byte[7180];

            using (var memoryStream = new MemoryStream(bs, false))
            {
                var sheet = ParseStyleSheet(memoryStream);
                Assert.IsNotNull(sheet);
                Assert.AreEqual(0, sheet.Rules.Length);
            }
        }

        [Test]
        public void CssSheetFromStringWithQuestionMarksLeadingToInfiniteLoop()
        {
            var sheet = ParseStyleSheet("U+???\0");
            Assert.IsNotNull(sheet);
            Assert.AreEqual(0, sheet.Rules.Length);
        }

        [Test]
        public void CssDefaultSheetSupportsRoundTripping()
        {
            var originalSourceCode = @"p.info {
	font-family: arial, sans-serif;
	line-height: 150%;
	margin-left: 2em;
	padding: 1em;
	border: 3px solid red;
	background-color: #f89;
	display: inline-block;
}
p.info span {
	font-weight: bold;
}
p.info span::after {
	content: ': ';
}";
            var initialSheet = ParseStyleSheet(originalSourceCode);
            var initialSourceCode = initialSheet.ToCss();
            var finalSheet = ParseStyleSheet(initialSourceCode);
            var finalSourceCode = finalSheet.ToCss();
            Assert.AreEqual(initialSourceCode, finalSourceCode);
            Assert.AreEqual(initialSheet.Rules.Length, finalSheet.Rules.Length);
        }

        [Test]
        public void CssParseSheetWithStyleMediaAndStyleRule()
        {
            var sheet = ParseStyleSheet(
                @".mobile,.tablet{display:none;} @media only screen and(max-width:51.875em){.tablet{display:block;}} .disp {display:block;}");
            Assert.AreEqual(3, sheet.Rules.Length);
            Assert.AreEqual(CssRuleType.Style, sheet.Rules[0].Type);
            Assert.AreEqual(CssRuleType.Media, sheet.Rules[1].Type);
            Assert.AreEqual(CssRuleType.Style, sheet.Rules[2].Type);
        }

        [Test]
        public void CssParseSheetWithMediaAndTwoStyleRules()
        {
            var sheet = ParseStyleSheet(
                @"@media only screen and(max-width:51.875em){.tablet{display:block;}} .mobile,.tablet{display:none;} .disp {display:block;}");
            Assert.AreEqual(3, sheet.Rules.Length);
            Assert.AreEqual(CssRuleType.Media, sheet.Rules[0].Type);
            Assert.AreEqual(CssRuleType.Style, sheet.Rules[1].Type);
            Assert.AreEqual(CssRuleType.Style, sheet.Rules[2].Type);
        }

        [Test]
        public void CssParseSheetWithTwoStyleAndMediaRule()
        {
            var sheet = ParseStyleSheet(
                @".mobile,.tablet{display:none;} .disp {display:block;} @media only screen and(max-width:51.875em){.tablet{display:block;}}");
            Assert.AreEqual(3, sheet.Rules.Length);
            Assert.AreEqual(CssRuleType.Style, sheet.Rules[0].Type);
            Assert.AreEqual(CssRuleType.Style, sheet.Rules[1].Type);
            Assert.AreEqual(CssRuleType.Media, sheet.Rules[2].Type);
        }

        [Test]
        public void CssParseImportStatementWithNoMediaTextFollowedByStyle()
        {
            var src = "@import url(import3.css); p { color : #f00; }";
            var sheet = ParseStyleSheet(src);
            Assert.AreEqual(2, sheet.Rules.Length);
            var import = sheet.Rules[0] as ICssImportRule;
            var style = sheet.Rules[1] as ICssStyleRule;
            Assert.IsNotNull(import);
            Assert.IsNotNull(style);
            Assert.AreEqual(0, import.Media.Length);
            Assert.AreEqual("", import.Media.MediaText);
            Assert.AreEqual("import3.css", import.Href);
            Assert.AreEqual("p", style.Selector.Text);
            Assert.AreEqual(1, style.Style.Length);
        }

        [Test]
        public void CssParseMediaRuleWithInvalidMediumEntities()
        {
            var src =
                "@media only screen and (min--moz-device-pixel-ratio:1.5),only screen and (-o-min-device-pixel-ratio:3/2),only screen and (-webkit-min-device-pixel-ratio:1.5),only screen and (min-device-pixel-ratio:1.5){.favicon{background-image:url('../img/favicons-sprite32.png?v=1b9547cf9cee3350a5b4875951e3e552');background-size:16px 5634px}}";
            var sheet = ParseStyleSheet(src);
            Assert.AreEqual(1, sheet.Rules.Length);
            var media = sheet.Rules[0] as ICssMediaRule;
            Assert.IsNotNull(media);
            Assert.AreEqual(4, media.Media.Length);
            Assert.AreEqual(1, media.Rules.Length);
        }

        [Test]
        public void CssParseStyleWithInvalidSurrogatePair()
        {
            var src = @"span.berschrift2Zchn
{mso-style-name:""\00DCberschrift 2 Zchn"";
mso-style-priority:9;
mso-style-link:""\00DCberschrift 2"";
font-family:""Cambria"",""serif"";
color:#4F81BD;
font-weight:bold;}";
            var sheet = ParseStyleSheet(src);
            Assert.AreEqual(1, sheet.Rules.Length);
            var style = sheet.Rules[0] as ICssStyleRule;
            Assert.IsNotNull(style);
            Assert.AreEqual("span.berschrift2Zchn", style.SelectorText);
            Assert.AreEqual(3, style.Style.Length);
        }

        [Test]
        public void CssParseMsViewPortWithoutOptions()
        {
            var css = "@-ms-viewport{width:device-width} .dsip { display: block; }";
            var doc = ParseStyleSheet(css);
            var result = doc.ToCss();
            Assert.AreEqual(".dsip { display: block }", result);
        }

        [Test]
        public void CssParseMsViewPortWithUnknownRules()
        {
            var options = new CssParserOptions()
            {
                IsIncludingUnknownDeclarations = true,
                IsIncludingUnknownRules = true
            };
            var css = "@-ms-viewport{width:device-width} .dsip { display: block; }";
            var doc = ParseStyleSheet(css, options);
            var result = doc.ToCss();
            Assert.AreEqual("@-ms-viewport{width:device-width}" + Environment.NewLine + ".dsip { display: block }",
                result);
        }

        [Test]
        public void CssParseMediaAndMsViewPortWithoutOptions()
        {
            var css =
                "@media screen and (max-width: 400px) {  @-ms-viewport { width: 320px; }  }  .dsip { display: block; }";
            var doc = ParseStyleSheet(css);
            var result = doc.ToCss();
            Assert.AreEqual(
                "@media screen and (max-width: 400px) { }" + Environment.NewLine + ".dsip { display: block }", result);
        }

        [Test]
        public void CssParseMediaAndMsViewPortWithUnknownRules()
        {
            var options = new CssParserOptions()
            {
                IsIncludingUnknownDeclarations = true,
                IsIncludingUnknownRules = true
            };
            var css =
                "@media screen and (max-width: 400px) {  @-ms-viewport { width: 320px; }  }  .dsip { display: block; }";
            var doc = ParseStyleSheet(css, options);
            var result = doc.ToCss();
            Assert.AreEqual(
                "@media screen and (max-width: 400px) { @-ms-viewport { width: 320px; } }" + Environment.NewLine +
                ".dsip { display: block }", result);
        }

        [Test]
        public void CssStyleSheetInsertAndDeleteShouldWork()
        {
            var s = ParseStyleSheet(String.Empty);
            Assert.AreEqual(0, s.Rules.Length);

            s.Insert("a {color: blue}", 0);
            Assert.AreEqual(1, s.Rules.Length);

            s.Insert("a *:first-child, a img {border: none}", 1);
            Assert.AreEqual(2, s.Rules.Length);

            s.RemoveAt(1);
            Assert.AreEqual(1, s.Rules.Length);

            s.RemoveAt(0);
            Assert.AreEqual(0, s.Rules.Length);
        }

        [Test]
        public void CssStyleSheetShouldIgnoreHtmlCommentTokens()
        {
            var parser = new CssParser();
            var source = "<!-- body { font-family: Verdana } div.hidden { display: none } -->";
            var sheet = parser.ParseStyleSheet(source);
            Assert.AreEqual(2, sheet.Rules.Length);

            Assert.AreEqual(CssRuleType.Style, sheet.Rules[0].Type);
            var body = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("body", body.SelectorText);
            Assert.AreEqual(1, body.Style.Length);
            Assert.AreEqual("Verdana", body.Style.FontFamily);

            Assert.AreEqual(CssRuleType.Style, sheet.Rules[1].Type);
            var div = sheet.Rules[1] as ICssStyleRule;
            Assert.AreEqual("div.hidden", div.SelectorText);
            Assert.AreEqual(1, div.Style.Length);
            Assert.AreEqual("none", div.Style.Display);
        }

        [Test]
        public void CssStyleSheetShouldExpandBorderColorCorrectly_Issue23()
        {
            var parser = new CssParser();
            var source = "body { border-color: red }";
            var sheet = parser.ParseStyleSheet(source);

            Assert.AreEqual(1, sheet.Rules.Length);
            Assert.AreEqual(CssRuleType.Style, sheet.Rules[0].Type);

            var body = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("border-color: rgba(255, 0, 0, 1)", body.Style.CssText);
            Assert.AreEqual("rgba(255, 0, 0, 1)", body.Style.BorderColor);
            Assert.AreEqual("rgba(255, 0, 0, 1)", body.Style.BorderLeftColor);
            Assert.AreEqual("rgba(255, 0, 0, 1)", body.Style.BorderRightColor);
            Assert.AreEqual("rgba(255, 0, 0, 1)", body.Style.BorderTopColor);
            Assert.AreEqual("rgba(255, 0, 0, 1)", body.Style.BorderBottomColor);
        }

        [Test]
        public void CssStyleSheetShouldCollapseBorderColorCorrectly_Issue23()
        {
            var parser = new CssParser();
            var source = "body { border-color: red }";
            var sheet = parser.ParseStyleSheet(source);

            var body = sheet.Rules[0] as ICssStyleRule;
            body.Style.BorderLeftColor = "blue";
            body.Style.BorderRightColor = "blue";
            Assert.AreEqual("border-color: rgba(255, 0, 0, 1) rgba(0, 0, 255, 1)", body.Style.CssText);
            Assert.AreEqual("rgba(255, 0, 0, 1) rgba(0, 0, 255, 1)", body.Style.BorderColor);
            Assert.AreEqual("rgba(0, 0, 255, 1)", body.Style.BorderLeftColor);
            Assert.AreEqual("rgba(0, 0, 255, 1)", body.Style.BorderRightColor);
            Assert.AreEqual("rgba(255, 0, 0, 1)", body.Style.BorderTopColor);
            Assert.AreEqual("rgba(255, 0, 0, 1)", body.Style.BorderBottomColor);
        }

        [Test]
        public void CssStyleSheetShouldCollapseFullBorderCorrectly_Issue23()
        {
            var parser = new CssParser();
            var source = "body { border: 1px  solid  red }";
            var sheet = parser.ParseStyleSheet(source);

            var body = sheet.Rules[0] as ICssStyleRule;
            Assert.AreEqual("border: 1px solid rgba(255, 0, 0, 1)", body.Style.CssText);
            body.Style.BorderLeftColor = "blue";
            body.Style.BorderTopWidth = "medium";
            Assert.AreEqual(
                "border-top: 3px solid rgba(255, 0, 0, 1); border-right: 1px solid rgba(255, 0, 0, 1); border-bottom: 1px solid rgba(255, 0, 0, 1); border-left: 1px solid rgba(0, 0, 255, 1)",
                body.Style.CssText);
            Assert.AreEqual("rgba(255, 0, 0, 1) rgba(255, 0, 0, 1) rgba(255, 0, 0, 1) rgba(0, 0, 255, 1)",
                body.Style.BorderColor);
            Assert.AreEqual("3px 1px 1px", body.Style.BorderWidth);
            Assert.AreEqual("solid", body.Style.BorderStyle);
        }

        [Test]
        public void CssStyleSheetInsertShouldSetParentStyleSheetCorrectly()
        {
            var s = ParseStyleSheet(String.Empty);
            s.Insert("a {color: blue}", 0);
            Assert.AreEqual(s, s.Rules[0].Owner);
        }

        [Test]
        public void GetImageRefOfACertainDeclarationFromSheet()
        {
            var s = ParseStyleSheet("body { background: url(http://example.com/foo.png) no-repeat }");
            var rule = s.GetStyleRuleWith("body");
            var url = rule.GetValueOf("background-image").AsUrl();
            Assert.AreEqual("http://example.com/foo.png", url);
        }

        [Test]
        public void GetBorderRightColorOfACertainDeclarationFromSheet()
        {
            var s = ParseStyleSheet("p > a { border: 1px solid red }");
            var rule = s.GetStyleRuleWith("p > a");
            var color = rule.GetValueOf("border-right-color").AsRgba();
            Assert.AreEqual(0x00_00_ff_ff, color);
        }

        [Test]
        public void CssColorFunctionsMixAllShouldWork()
        {
            var parser = new CssParser();
            var source = @"
.rgbNumber { color: rgb(255, 128, 0); }
.rgbPercent { color: rgb(100%, 50%, 0%); }
.rgbaNumber { color: rgba(255, 128, 0, 0.0); }
.rgbaPercent { color: rgba(100%, 50%, 0%, 0.0); }
.hsl { color: hsl(120, 100%, 50%); }
.hslAngle { color: hsl(120deg, 100%, 50%); }
.hsla { color: hsla(120, 100%, 50%, 0.25); }
.hslaAngle { color: hsla(120deg, 100%, 50%, 0.25); }
.grayNumber { color: gray(128); }
.grayPercent { color: gray(50%); }
.grayPercentAlpha { color: gray(50%, 0.5); }
.hwb { color: hwb(120, 60%, 20%); }
.hwbAngle { color: hwb(120deg, 60%, 20%); }
.hwbAlpha { color: hwb(120, 10%, 50%, 0.5); }
.hwbAngleAlpha { color: hwb(120deg, 10%, 50%, 0.5); }";
            var sheet = parser.ParseStyleSheet(source);
            Assert.AreEqual(15, sheet.Rules.Length);

            var rgbNumber = (sheet.Rules[0] as ICssStyleRule).Style.Color;
            var rgbPercent = (sheet.Rules[1] as ICssStyleRule).Style.Color;
            var rgbaNumber = (sheet.Rules[2] as ICssStyleRule).Style.Color;
            var rgbaPercent = (sheet.Rules[3] as ICssStyleRule).Style.Color;
            var hsl = (sheet.Rules[4] as ICssStyleRule).Style.Color;
            var hslAngle = (sheet.Rules[5] as ICssStyleRule).Style.Color;
            var hsla = (sheet.Rules[6] as ICssStyleRule).Style.Color;
            var hslaAngle = (sheet.Rules[7] as ICssStyleRule).Style.Color;
            var grayNumber = (sheet.Rules[8] as ICssStyleRule).Style.Color;
            var grayPercent = (sheet.Rules[9] as ICssStyleRule).Style.Color;
            var grayPercentAlpha = (sheet.Rules[10] as ICssStyleRule).Style.Color;
            var hwb = (sheet.Rules[11] as ICssStyleRule).Style.Color;
            var hwbAngle = (sheet.Rules[12] as ICssStyleRule).Style.Color;
            var hwbAlpha = (sheet.Rules[13] as ICssStyleRule).Style.Color;
            var hwbAngleAlpha = (sheet.Rules[14] as ICssStyleRule).Style.Color;

            Assert.IsNotNull(rgbNumber);
            Assert.IsNotNull(rgbPercent);
            Assert.IsNotNull(rgbaPercent);
            Assert.IsNotNull(hsl);
            Assert.IsNotNull(hslAngle);
            Assert.IsNotNull(hsla);
            Assert.IsNotNull(hslaAngle);
            Assert.IsNotNull(grayNumber);
            Assert.IsNotNull(grayPercent);
            Assert.IsNotNull(grayPercentAlpha);
            Assert.IsNotNull(hwb);
            Assert.IsNotNull(hwbAngle);
            Assert.IsNotNull(hwbAlpha);
            Assert.IsNotNull(hwbAngleAlpha);
        }

        [Test]
        public void Parser_CanParseLogicalBorderWidthProperties()
        {
            // Test border-block-width
            var declaration1 = ParseDeclaration("border-block-width: 2px");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("border-block-width", declaration1.Name);
            Assert.AreEqual("2px", declaration1.Value);

            // Test border-inline-width
            var declaration2 = ParseDeclaration("border-inline-width: 3px");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("border-inline-width", declaration2.Name);
            Assert.AreEqual("3px", declaration2.Value);
        }

        [Test]
        public void Parser_CanParseLogicalBorderStyleProperties()
        {
            // Test border-block-style
            var declaration1 = ParseDeclaration("border-block-style: solid");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("border-block-style", declaration1.Name);
            Assert.AreEqual("solid", declaration1.Value);

            // Test border-inline-style
            var declaration2 = ParseDeclaration("border-inline-style: dashed");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("border-inline-style", declaration2.Name);
            Assert.AreEqual("dashed", declaration2.Value);
        }

        [Test]
        public void Parser_CanParseLogicalBorderColorProperties()
        {
            // Test border-block-color
            var declaration1 = ParseDeclaration("border-block-color: red");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("border-block-color", declaration1.Name);

            // Test border-inline-color
            var declaration2 = ParseDeclaration("border-inline-color: blue");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("border-inline-color", declaration2.Name);
        }

        [Test]
        public void Parser_CanParseLogicalBorderShorthandProperties()
        {
            // Test border-block
            var declaration1 = ParseDeclaration("border-block: 1px solid red");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("border-block", declaration1.Name);

            // Test border-inline
            var declaration2 = ParseDeclaration("border-inline: 2px dashed blue");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("border-inline", declaration2.Name);
        }

        [Test]
        public void Parser_CanParseLogicalBorderRadiusProperties()
        {
            // Test border-start-start-radius
            var declaration1 = ParseDeclaration("border-start-start-radius: 10px");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("border-start-start-radius", declaration1.Name);
            Assert.AreEqual("10px", declaration1.Value);

            // Test border-start-end-radius
            var declaration2 = ParseDeclaration("border-start-end-radius: 5px");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("border-start-end-radius", declaration2.Name);
            Assert.AreEqual("5px", declaration2.Value);

            // Test border-end-start-radius
            var declaration3 = ParseDeclaration("border-end-start-radius: 8px");
            Assert.IsNotNull(declaration3);
            Assert.AreEqual("border-end-start-radius", declaration3.Name);
            Assert.AreEqual("8px", declaration3.Value);

            // Test border-end-end-radius
            var declaration4 = ParseDeclaration("border-end-end-radius: 12px");
            Assert.IsNotNull(declaration4);
            Assert.AreEqual("border-end-end-radius", declaration4.Name);
            Assert.AreEqual("12px", declaration4.Value);
        }

        [Test]
        public void Parser_CanParseLogicalDimensionProperties()
        {
            // Test block-size
            var declaration1 = ParseDeclaration("block-size: 200px");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("block-size", declaration1.Name);
            Assert.AreEqual("200px", declaration1.Value);

            // Test inline-size
            var declaration2 = ParseDeclaration("inline-size: 300px");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("inline-size", declaration2.Name);
            Assert.AreEqual("300px", declaration2.Value);

            // Test min-block-size
            var declaration3 = ParseDeclaration("min-block-size: 100px");
            Assert.IsNotNull(declaration3);
            Assert.AreEqual("min-block-size", declaration3.Name);
            Assert.AreEqual("100px", declaration3.Value);

            // Test max-inline-size
            var declaration4 = ParseDeclaration("max-inline-size: 500px");
            Assert.IsNotNull(declaration4);
            Assert.AreEqual("max-inline-size", declaration4.Name);
            Assert.AreEqual("500px", declaration4.Value);
        }

        [Test]
        public void Parser_CanParseLogicalMarginProperties()
        {
            // Test margin-block
            var declaration1 = ParseDeclaration("margin-block: 10px");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("margin-block", declaration1.Name);
            Assert.AreEqual("10px", declaration1.Value);

            // Test margin-inline
            var declaration2 = ParseDeclaration("margin-inline: 15px 20px");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("margin-inline", declaration2.Name);
            Assert.AreEqual("15px 20px", declaration2.Value);

            // Test margin-block-start
            var declaration3 = ParseDeclaration("margin-block-start: 8px");
            Assert.IsNotNull(declaration3);
            Assert.AreEqual("margin-block-start", declaration3.Name);
            Assert.AreEqual("8px", declaration3.Value);

            // Test margin-inline-end
            var declaration4 = ParseDeclaration("margin-inline-end: 12px");
            Assert.IsNotNull(declaration4);
            Assert.AreEqual("margin-inline-end", declaration4.Name);
            Assert.AreEqual("12px", declaration4.Value);
        }

        [Test]
        public void Parser_CanParseLogicalPaddingProperties()
        {
            // Test padding-block
            var declaration1 = ParseDeclaration("padding-block: 10px");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("padding-block", declaration1.Name);
            Assert.AreEqual("10px", declaration1.Value);

            // Test padding-inline
            var declaration2 = ParseDeclaration("padding-inline: 15px 20px");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("padding-inline", declaration2.Name);
            Assert.AreEqual("15px 20px", declaration2.Value);

            // Test padding-block-start
            var declaration3 = ParseDeclaration("padding-block-start: 8px");
            Assert.IsNotNull(declaration3);
            Assert.AreEqual("padding-block-start", declaration3.Name);
            Assert.AreEqual("8px", declaration3.Value);

            // Test padding-inline-end
            var declaration4 = ParseDeclaration("padding-inline-end: 12px");
            Assert.IsNotNull(declaration4);
            Assert.AreEqual("padding-inline-end", declaration4.Name);
            Assert.AreEqual("12px", declaration4.Value);
        }

        [Test]
        public void Parser_CanParseLogicalInsetProperties()
        {
            // Test inset
            var declaration1 = ParseDeclaration("inset: 10px");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("inset", declaration1.Name);
            Assert.AreEqual("10px", declaration1.Value);

            // Test inset-block
            var declaration2 = ParseDeclaration("inset-block: 15px 20px");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("inset-block", declaration2.Name);
            Assert.AreEqual("15px 20px", declaration2.Value);

            // Test inset-inline
            var declaration3 = ParseDeclaration("inset-inline: 8px");
            Assert.IsNotNull(declaration3);
            Assert.AreEqual("inset-inline", declaration3.Name);
            Assert.AreEqual("8px", declaration3.Value);

            // Test inset-block-start
            var declaration4 = ParseDeclaration("inset-block-start: 12px");
            Assert.IsNotNull(declaration4);
            Assert.AreEqual("inset-block-start", declaration4.Name);
            Assert.AreEqual("12px", declaration4.Value);
        }

        [Test]
        public void Parser_CanParseLogicalOverflowProperties()
        {
            // Test overflow-block
            var declaration1 = ParseDeclaration("overflow-block: scroll");
            Assert.IsNotNull(declaration1);
            Assert.AreEqual("overflow-block", declaration1.Name);
            Assert.AreEqual("scroll", declaration1.Value);

            // Test overflow-inline
            var declaration2 = ParseDeclaration("overflow-inline: hidden");
            Assert.IsNotNull(declaration2);
            Assert.AreEqual("overflow-inline", declaration2.Name);
            Assert.AreEqual("hidden", declaration2.Value);
        }

        [Test]
        public void Parser_CanParseStyleRuleWithLogicalProperties()
        {
            // Arrange
            var css = @"
        .logical-box {
            border-block: 1px solid blue;
            margin-inline: 20px;
            padding-block-start: 10px;
            inset-inline-end: 5px;
        }
    ";

            // Act
            var sheet = ParseStyleSheet(css);
            var rule = sheet.Rules[0] as ICssStyleRule;

            // Assert
            Assert.IsNotNull(rule);
            Assert.AreEqual(".logical-box", rule.SelectorText);

            // Verify the total number of properties
            Assert.AreEqual(10, rule.Style.Length);

            // Check that each expected property has a non-empty value
            // For border-block expanded properties
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("border-block-start-width")));
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("border-block-end-width")));
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("border-block-start-style")));
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("border-block-end-style")));
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("border-block-start-color")));
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("border-block-end-color")));

            // For margin-inline expanded properties
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("margin-inline-start")));
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("margin-inline-end")));

            // For direct properties
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("padding-block-start")));
            Assert.IsFalse(string.IsNullOrEmpty(rule.Style.GetPropertyValue("inset-inline-end")));

            // Test the specific values
            Assert.AreEqual("1px", rule.Style.GetPropertyValue("border-block-start-width"));
            Assert.AreEqual("solid", rule.Style.GetPropertyValue("border-block-start-style"));
            Assert.AreEqual("rgba(0, 0, 255, 1)", rule.Style.GetPropertyValue("border-block-start-color"));
            Assert.AreEqual("20px", rule.Style.GetPropertyValue("margin-inline-start"));
            Assert.AreEqual("10px", rule.Style.GetPropertyValue("padding-block-start"));
            Assert.AreEqual("5px", rule.Style.GetPropertyValue("inset-inline-end"));
        }

        [Test]
        public void Parser_LogicalPropertiesInMediaRules()
        {
            // Arrange
            var css = @"
                @media screen and (min-width: 600px) {
                    .logical-container {
                        block-size: 50vh;
                        inline-size: 80vw;
                    }
                }
            ";

            // Act
            var sheet = ParseStyleSheet(css);
            var mediaRule = sheet.Rules[0] as ICssMediaRule;
            var styleRule = mediaRule?.Rules[0] as ICssStyleRule;

            // Assert
            Assert.IsNotNull(mediaRule);
            Assert.AreEqual(1, mediaRule.Rules.Length);
            Assert.IsNotNull(styleRule);
            Assert.AreEqual(".logical-container", styleRule.SelectorText);
            Assert.AreEqual(2, styleRule.Style.Length);
            Assert.AreEqual("block-size", styleRule.Style[0]);
            Assert.AreEqual("inline-size", styleRule.Style[1]);
        }

        [Test]
        public void Parser_LogicalPropertiesWithCalcValues()
        {
            // Arrange & Act
            var declaration = ParseDeclaration("inline-size: calc(100% - 20px)");

            // Assert
            Assert.IsNotNull(declaration);
            Assert.AreEqual("inline-size", declaration.Name);
            Assert.AreEqual("calc(100% - 20px)", declaration.Value);
        }

        [Test]
        public void Parser_CanParseMixedLogicalAndPhysicalProperties()
        {
            // Arrange
            var css = @"
                .mixed-box {
                    width: 100px;
                    inline-size: 200px;
                    height: 50px;
                    block-size: 80px;
                }
            ";

            // Act
            var sheet = ParseStyleSheet(css);
            var rule = sheet.Rules[0] as ICssStyleRule;

            // Assert
            Assert.IsNotNull(rule);
            Assert.AreEqual(".mixed-box", rule.SelectorText);
            Assert.AreEqual(4, rule.Style.Length);

            // Note: the order might be different depending on implementation
            Assert.IsTrue(rule.Style.GetPropertyValue("width") == "100px");
            Assert.IsTrue(rule.Style.GetPropertyValue("inline-size") == "200px");
            Assert.IsTrue(rule.Style.GetPropertyValue("height") == "50px");
            Assert.IsTrue(rule.Style.GetPropertyValue("block-size") == "80px");
        }

        [Test]
        public void LogicalBorderShorthand_ExpandsToLonghandProperties()
        {
            // Test border-block expansion
            var decls = ParseDeclarations("border-block: 2px solid blue");
            Assert.IsNotNull(decls);
            Assert.AreEqual(6, decls.Length); // Expands to start and end properties (width, style, color)

            // Verify all longhand properties are set
            Assert.AreEqual("2px", decls.GetPropertyValue("border-block-start-width"));
            Assert.AreEqual("solid", decls.GetPropertyValue("border-block-start-style"));
            Assert.AreEqual("rgba(0, 0, 255, 1)", decls.GetPropertyValue("border-block-start-color"));
            Assert.AreEqual("2px", decls.GetPropertyValue("border-block-end-width"));
            Assert.AreEqual("solid", decls.GetPropertyValue("border-block-end-style"));
            Assert.AreEqual("rgba(0, 0, 255, 1)", decls.GetPropertyValue("border-block-end-color"));
        }

        [Test]
        public void LogicalBorderInlineShorthand_ExpandsToLonghandProperties()
        {
            // Test border-inline expansion
            var decls = ParseDeclarations("border-inline: 1px dashed red");
            Assert.IsNotNull(decls);
            Assert.AreEqual(6, decls.Length); // Expands to start and end properties (width, style, color)

            // Verify all longhand properties are set
            Assert.AreEqual("1px", decls.GetPropertyValue("border-inline-start-width"));
            Assert.AreEqual("dashed", decls.GetPropertyValue("border-inline-start-style"));
            Assert.AreEqual("rgba(255, 0, 0, 1)", decls.GetPropertyValue("border-inline-start-color"));
            Assert.AreEqual("1px", decls.GetPropertyValue("border-inline-end-width"));
            Assert.AreEqual("dashed", decls.GetPropertyValue("border-inline-end-style"));
            Assert.AreEqual("rgba(255, 0, 0, 1)", decls.GetPropertyValue("border-inline-end-color"));
        }

        [Test]
        public void LogicalMarginShorthand_ExpandsCorrectly()
        {
            // One value
            var decls1 = ParseDeclarations("margin-block: 10px");
            Assert.IsNotNull(decls1);
            Assert.AreEqual(2, decls1.Length);
            Assert.AreEqual("10px", decls1.GetPropertyValue("margin-block-start"));
            Assert.AreEqual("10px", decls1.GetPropertyValue("margin-block-end"));

            // Two values
            var decls2 = ParseDeclarations("margin-inline: 5px 15px");
            Assert.IsNotNull(decls2);
            Assert.AreEqual(2, decls2.Length);
            Assert.AreEqual("5px", decls2.GetPropertyValue("margin-inline-start"));
            Assert.AreEqual("15px", decls2.GetPropertyValue("margin-inline-end"));
        }

        [Test]
        public void LogicalPaddingShorthand_ExpandsCorrectly()
        {
            // One value
            var decls1 = ParseDeclarations("padding-block: 10px");
            Assert.IsNotNull(decls1);
            Assert.AreEqual(2, decls1.Length);
            Assert.AreEqual("10px", decls1.GetPropertyValue("padding-block-start"));
            Assert.AreEqual("10px", decls1.GetPropertyValue("padding-block-end"));

            // Two values
            var decls2 = ParseDeclarations("padding-inline: 5px 15px");
            Assert.IsNotNull(decls2);
            Assert.AreEqual(2, decls2.Length);
            Assert.AreEqual("5px", decls2.GetPropertyValue("padding-inline-start"));
            Assert.AreEqual("15px", decls2.GetPropertyValue("padding-inline-end"));
        }

        [Test]
        public void LogicalInsetShorthand_ExpandsCorrectly()
        {
            // Test inset with 1 value
            var decls1 = ParseDeclarations("inset: 10px");
            Assert.IsNotNull(decls1);
            Assert.AreEqual(4, decls1.Length);
            Assert.AreEqual("10px", decls1.GetPropertyValue("top"));
            Assert.AreEqual("10px", decls1.GetPropertyValue("right"));
            Assert.AreEqual("10px", decls1.GetPropertyValue("bottom"));
            Assert.AreEqual("10px", decls1.GetPropertyValue("left"));

            // Test inset with 4 values
            var decls2 = ParseDeclarations("inset: 5px 10px 15px 20px");
            Assert.IsNotNull(decls2);
            Assert.AreEqual(4, decls2.Length);
            Assert.AreEqual("5px", decls2.GetPropertyValue("top"));
            Assert.AreEqual("10px", decls2.GetPropertyValue("right"));
            Assert.AreEqual("15px", decls2.GetPropertyValue("bottom"));
            Assert.AreEqual("20px", decls2.GetPropertyValue("left"));

            // Test inset-block with 2 values
            var decls3 = ParseDeclarations("inset-block: 10px 20px");
            Assert.IsNotNull(decls3);
            Assert.AreEqual(2, decls3.Length);
            Assert.AreEqual("10px", decls3.GetPropertyValue("inset-block-start"));
            Assert.AreEqual("20px", decls3.GetPropertyValue("inset-block-end"));
        }

        [Test]
        public void BlockSize_ParsesCorrectDimensions()
        {
            var decls = ParseDeclarations("block-size: 200px; min-block-size: 100px; max-block-size: 300px");
            Assert.IsNotNull(decls);
            Assert.AreEqual(3, decls.Length);
            Assert.AreEqual("200px", decls.GetPropertyValue("block-size"));
            Assert.AreEqual("100px", decls.GetPropertyValue("min-block-size"));
            Assert.AreEqual("300px", decls.GetPropertyValue("max-block-size"));
        }

        [Test]
        public void InlineSize_ParsesCorrectDimensions()
        {
            var decls = ParseDeclarations("inline-size: 400px; min-inline-size: 200px; max-inline-size: 600px");
            Assert.IsNotNull(decls);
            Assert.AreEqual(3, decls.Length);
            Assert.AreEqual("400px", decls.GetPropertyValue("inline-size"));
            Assert.AreEqual("200px", decls.GetPropertyValue("min-inline-size"));
            Assert.AreEqual("600px", decls.GetPropertyValue("max-inline-size"));
        }

        [Test]
        public void CssStyleSheet_RoundTripsLogicalProperties()
        {
            var originalCss = @"
.logical-box {
    border-block: 1px solid blue;
    border-inline: 2px dashed red;
    margin-block: 10px 20px;
    padding-inline: 5px 15px;
    block-size: 200px;
    inline-size: 400px;
    inset-block: 5px 10px;
    inset-inline: 15px 20px;
}";

            var sheet = ParseStyleSheet(originalCss);
            var serialized = sheet.ToCss();
            var reparsed = ParseStyleSheet(serialized);

            // Compare the two style sheets
            Assert.AreEqual(sheet.Rules.Length, reparsed.Rules.Length);

            var originalRule = sheet.Rules[0] as ICssStyleRule;
            var reparsedRule = reparsed.Rules[0] as ICssStyleRule;

            Assert.AreEqual(originalRule.SelectorText, reparsedRule.SelectorText);
            Assert.AreEqual(originalRule.Style.Length, reparsedRule.Style.Length);

            // Check a few specific properties
            Assert.AreEqual(
                originalRule.Style.GetPropertyValue("border-block"),
                reparsedRule.Style.GetPropertyValue("border-block")
            );

            Assert.AreEqual(
                originalRule.Style.GetPropertyValue("inline-size"),
                reparsedRule.Style.GetPropertyValue("inline-size")
            );
        }

        [Test]
        public void LogicalBorderProperties_WithDifferentComponents()
        {
            var sheet = ParseStyleSheet(@"
.mixed-borders {
    border-block-width: 2px;
    border-block-style: solid;
    border-block-color: blue;
    border-inline-width: 1px;
    border-inline-style: dashed;
    border-inline-color: red;
}");

            var rule = sheet.Rules[0] as ICssStyleRule;
            Assert.IsNotNull(rule);
            Assert.AreEqual(12, rule.Style.Length);

            // Verify individual property values
            Assert.AreEqual("2px", rule.Style.GetPropertyValue("border-block-width"));
            Assert.AreEqual("solid", rule.Style.GetPropertyValue("border-block-style"));
            Assert.AreEqual("rgba(0, 0, 255, 1)", rule.Style.GetPropertyValue("border-block-color"));
            Assert.AreEqual("1px", rule.Style.GetPropertyValue("border-inline-width"));
            Assert.AreEqual("dashed", rule.Style.GetPropertyValue("border-inline-style"));
            Assert.AreEqual("rgba(255, 0, 0, 1)", rule.Style.GetPropertyValue("border-inline-color"));
        }

        [Test]
        public void LogicalProperties_WithCalculatedValues()
        {
            var sheet = ParseStyleSheet(@"
.calculated-logical {
    block-size: calc(100vh - 20px);
    inline-size: calc(50% + 30px);
    padding-block-start: calc(1em + 10px);
    margin-inline-end: calc(2vw - 5px);
}");

            var rule = sheet.Rules[0] as ICssStyleRule;
            Assert.IsNotNull(rule);
            Assert.AreEqual(4, rule.Style.Length);

            // Verify calculated values are parsed correctly
            Assert.AreEqual("calc(100vh - 20px)", rule.Style.GetPropertyValue("block-size"));
            Assert.AreEqual("calc(50% + 30px)", rule.Style.GetPropertyValue("inline-size"));
            Assert.AreEqual("calc(1em + 10px)", rule.Style.GetPropertyValue("padding-block-start"));
            Assert.AreEqual("calc(2vw - 5px)", rule.Style.GetPropertyValue("margin-inline-end"));
        }

        [Test]
        public void LogicalProperties_WithShorthandsAndLonghands()
        {
            var sheet = ParseStyleSheet(@"
.mixed-shorthands-longhands {
    border-block: 2px solid blue;
    border-inline-start-width: 3px;
    border-inline-end-style: dotted;
    margin-block: 10px;
    margin-block-end: 20px;
}");

            var rule = sheet.Rules[0] as ICssStyleRule;
            Assert.IsNotNull(rule);

            // Shorthands should be expanded
            Assert.AreEqual("2px", rule.Style.GetPropertyValue("border-block-start-width"));
            Assert.AreEqual("solid", rule.Style.GetPropertyValue("border-block-start-style"));
            Assert.AreEqual("rgba(0, 0, 255, 1)", rule.Style.GetPropertyValue("border-block-start-color"));

            // Longhands should override shorthand values
            Assert.AreEqual("3px", rule.Style.GetPropertyValue("border-inline-start-width"));
            Assert.AreEqual("dotted", rule.Style.GetPropertyValue("border-inline-end-style"));

            // Later longhands should override earlier shorthands
            Assert.AreEqual("10px", rule.Style.GetPropertyValue("margin-block-start"));
            Assert.AreEqual("20px", rule.Style.GetPropertyValue("margin-block-end"));
        }

        [Test]
        public void Inset_WithAutoValues()
        {
            var sheet = ParseStyleSheet(@"
.auto-insets {
    inset-block-start: auto;
    inset-inline-end: auto;
}");

            var rule = sheet.Rules[0] as ICssStyleRule;
            Assert.IsNotNull(rule);
            Assert.AreEqual(2, rule.Style.Length);

            Assert.AreEqual("auto", rule.Style.GetPropertyValue("inset-block-start"));
            Assert.AreEqual("auto", rule.Style.GetPropertyValue("inset-inline-end"));
        }

        [Test]
        public void InsetBlock_WithPercentageValues()
        {
            var sheet = ParseStyleSheet(@"
.percentage-insets {
    inset-block: 10% 20%;
    inset-inline: 30% 40%;
}");

            var rule = sheet.Rules[0] as ICssStyleRule;
            Assert.IsNotNull(rule);
            Assert.AreEqual(4, rule.Style.Length);

            Assert.AreEqual("10%", rule.Style.GetPropertyValue("inset-block-start"));
            Assert.AreEqual("20%", rule.Style.GetPropertyValue("inset-block-end"));
            Assert.AreEqual("30%", rule.Style.GetPropertyValue("inset-inline-start"));
            Assert.AreEqual("40%", rule.Style.GetPropertyValue("inset-inline-end"));
        }
    }
}