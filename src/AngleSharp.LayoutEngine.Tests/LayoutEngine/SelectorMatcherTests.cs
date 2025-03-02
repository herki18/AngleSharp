// namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;
//
// using Css;
// using StyleComputation;
//
// [TestFixture]
// public class SelectorMatcherTests
// {
//     private SelectorMatcher _matcher;
//     private IRenderDevice _device;
//
//     [SetUp]
//     public void Setup()
//     {
//         _device = new MockRenderDevice { ViewPortWidth = 1024, ViewPortHeight = 768 };
//         _matcher = new SelectorMatcher(_device);
//     }
//
//     [Test]
//     public void MatchRules_WithSimpleSelector_ReturnsMatchingRule()
//     {
//         // Arrange
//         var document = BrowsingContext.New(Configuration.Default).OpenNew();
//         var element = document.CreateElement("div");
//         element.ClassName = "test";
//
//         var stylesheet = ParseStylesheet("div.test { color: red; }");
//         var stylesheetEntry = new StylesheetEntry(stylesheet, StylesheetOrigin.Author);
//
//         // Act
//         var matches = _matcher.MatchRules(element, new[] { stylesheetEntry }).ToList();
//
//         // Assert
//         Assert.That(matches, Has.Count.EqualTo(1));
//         Assert.That(matches[0].Rule.SelectorText, Is.EqualTo("div.test"));
//     }
//
//     [Test]
//     public void MatchRules_WithMediaQuery_FiltersByMedia()
//     {
//         // Arrange
//         var document = BrowsingContext.New(Configuration.Default).OpenNew();
//         var element = document.CreateElement("div");
//
//         var stylesheet = ParseStylesheet("@media (max-width: 800px) { div { color: red; } }");
//         var stylesheetEntry = new StylesheetEntry(stylesheet, StylesheetOrigin.Author);
//
//         // Act - should NOT match as our device is 1024px wide
//         var matches = _matcher.MatchRules(element, new[] { stylesheetEntry }).ToList();
//
//         // Assert
//         Assert.That(matches, Is.Empty);
//
//         // Now change device width to match media query
//         _device.ViewPortWidth = 700;
//         matches = _matcher.MatchRules(element, new[] { stylesheetEntry }).ToList();
//
//         // Should now match
//         Assert.That(matches, Has.Count.EqualTo(1));
//     }
//
//     // Helper methods...
// }