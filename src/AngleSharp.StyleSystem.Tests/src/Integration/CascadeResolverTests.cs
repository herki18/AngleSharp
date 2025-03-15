// namespace AngleSharp.StyleSystem.Tests.Integration;
//
// using AngleSharp.Css;
// using AngleSharp.Css.Dom;
// using AngleSharp.Css.Parser;
// using AngleSharp.Dom;
// using AngleSharp.StyleSystem.Computation;
// using AngleSharp.StyleSystem.Interfaces;
// using AngleSharp.StyleSystem.Models;
//
// /// <summary>
// /// Tests for the CascadeResolver component using the improved StyleSystemTestFixture.
// /// </summary>
// [TestFixture]
// public class CascadeResolverTests : StyleSystemTestFixture
// {
//     private ICascadeResolver _cascadeResolver;
//
//     [SetUp]
//     public override void SetupFixture()
//     {
//         // Initialize the base fixture
//         base.SetupFixture();
//
//         // Get the cascade resolver from context
//         var cascadeResolver = Context.GetService<ICascadeResolver>();
//         _cascadeResolver = cascadeResolver ?? new CascadeResolver(Context);
//
//         Assert.That(_cascadeResolver, Is.Not.Null, "Failed to initialize CascadeResolver");
//     }
//
//     [Test]
//     public void ResolveCascade_WithNoRules_ReturnsEmptyDeclaration()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = new List<MatchedRule>();
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.Length, Is.EqualTo(0));
//     }
//
//     [Test]
//     public void ResolveCascade_WithSingleRule_AppliesRuleProperties()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = new List<MatchedRule>
//         {
//             CreateMatchedRule("div", "color: red;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 0, 1))
//         };
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.Length, Is.EqualTo(1));
//         AssertPropertyValue(result, "color", "rgba(255, 0, 0, 1)");
//     }
//
//     [Test]
//     public void ResolveCascade_WithMultipleRules_AppliesCorrectCascade()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "color: red; font-size: 12px;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div", "color: blue; margin: 10px;", StylesheetOrigin.Author, 0, 0, 0, 1)
//         );
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         Assert.That(result.Length, Is.EqualTo(6)); // color, font-size, margin + longhand margins
//         AssertPropertyValue(result, "color", "rgba(0, 0, 255, 1)"); // Later rule wins
//         AssertPropertyValue(result, "font-size", "12px");
//         AssertPropertyValue(result, "margin", "10px");
//     }
//
//     [Test]
//     public void ResolveCascade_WithSpecificity_HigherSpecificityWins()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "color: red;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div.special", "color: blue;", StylesheetOrigin.Author, 0, 0, 1, 1) // Higher specificity
//         );
//
//         // Act and Assert
//         AssertCascadeResult(element, "color", "rgba(0, 0, 255, 1)", matchedRules);
//     }
//
//     [Test]
//     public void ResolveCascade_WithOrigin_HigherOriginWins()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//
//         // Test the origin cascade using dedicated helper method
//         TestOriginCascade(
//             element,
//             "color",
//             "rgba(0, 0, 255, 1)", // Expected value (blue)
//             CreateRuleWithImportance("div", "color", "red", false), // UserAgent rule
//             CreateRuleWithImportance("div", "color", "green", false), // User rule
//             CreateRuleWithImportance("div", "color", "blue", false) // Author rule
//         );
//     }
//
//     [Test]
//     public void ResolveCascade_WithImportant_ImportantOverridesNormal()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = new List<MatchedRule>
//         {
//             CreateMatchedRule("div", "color: red !important;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 0, 1)),
//             CreateMatchedRule("div", "color: blue;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 1, 0)) // Higher specificity
//         };
//
//         // Act and Assert
//         AssertCascadeResult(element, "color", "rgba(255, 0, 0, 1)", matchedRules, true);
//     }
//
//     [Test]
//     public void ResolveCascade_WithInlineStyle_AppliesInlineStyle()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         SetInlineStyle(element, "color: green; padding: 5px;");
//
//         var matchedRules = new List<MatchedRule>
//         {
//             CreateMatchedRule("div", "color: red; margin: 10px;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 0, 1))
//         };
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         AssertPropertyValue(result, "color", "rgba(0, 128, 0, 1)"); // Inline style wins
//         AssertPropertyValue(result, "margin", "10px"); // Rule property remains
//         AssertPropertyValue(result, "padding", "5px"); // Inline style added
//     }
//
//     [Test]
//     public void ResolveCascade_WithImportantInRuleAndInline_ImportantRuleWins()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         SetInlineStyle(element, "color: green;");
//
//         var matchedRules = new List<MatchedRule>
//         {
//             CreateMatchedRule("div", "color: red !important;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 0, 1))
//         };
//
//         // Act and Assert
//         AssertCascadeResult(element, "color", "rgba(255, 0, 0, 1)", matchedRules, true);
//     }
//
//     [Test]
//     public void ResolveCascade_WithImportantInlineOverImportantUserAgent_InlineImportantWins()
//     {
//         // Test using the dedicated helper method for inline and !important interaction
//         TestInlineStyleCascade(
//             "color: green", // Inline style
//             true, // Is important
//             "color", // Property to test
//             new[] { ("div", "color: red", StylesheetOrigin.UserAgent, true) }, // One UserAgent rule with !important
//             "rgba(0, 128, 0, 1)", // Expected result (green)
//             true // Expected to be important
//         );
//     }
//
//     [Test]
//     public void ResolveCascade_WithComplexSpecificityOrder_FollowsCorrectCascade()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "color: black;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div.test", "color: red;", StylesheetOrigin.Author, 0, 0, 1, 1),
//             ("body div", "color: green;", StylesheetOrigin.Author, 0, 0, 0, 2),
//             ("#content div", "color: blue;", StylesheetOrigin.Author, 0, 1, 0, 1)
//         );
//
//         // Act and Assert
//         AssertCascadeResult(element, "color", "rgba(0, 0, 255, 1)", matchedRules); // ID selector wins
//     }
//
//     [Test]
//     public void ResolveCascade_WithImportantRulesFromDifferentOrigins_HigherOriginImportantWins()
//     {
//         // Test using the dedicated helper for importance cascade across origins
//         TestImportanceCascade(
//             CreateTestElement("div"),
//             "color",
//             new[]
//             {
//                 ("div", "color: red", StylesheetOrigin.UserAgent, true),
//                 ("div", "color: green", StylesheetOrigin.User, true),
//                 ("div", "color: blue", StylesheetOrigin.Author, true)
//             },
//             "rgba(0, 0, 255, 1)", // Author !important wins
//             true
//         );
//     }
//
//     [Test]
//     public void ResolveCascade_WithMultipleProperties_EachPropertyFollowsOwnCascade()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "color: red; background: blue;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div.test", "color: green;", StylesheetOrigin.Author, 0, 0, 1, 1),
//             ("div", "color: yellow !important; font-size: 12px;", StylesheetOrigin.Author, 0, 0, 0, 1)
//         );
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         AssertPropertyValue(result, "color", "rgba(255, 255, 0, 1)"); // !important wins for color
//         AssertPropertyValue(result, "background", "rgba(0, 0, 255, 1)"); // Only rule with background
//         AssertPropertyValue(result, "font-size", "12px"); // Last rule wins for font-size
//     }
//
//     [Test]
//     public void ResolveCascade_WithInlineImportant_OverridesAuthorImportant()
//     {
//         // Test using dedicated helper for inline vs important
//         TestInlineStyleCascade(
//             "color: green", // Inline style
//             true, // Is important
//             "color", // Property to test
//             new[] { ("div", "color: red", StylesheetOrigin.Author, true) }, // Author rule with !important
//             "rgba(0, 128, 0, 1)", // Expected result (green)
//             true // Expected to be important
//         );
//     }
//
//     [Test]
//     public void ResolveCascade_WithShorthandAndLonghand_HandlesCorrectly()
//     {
//         // Test using dedicated helper for shorthand/longhand interaction
//         TestShorthandLonghandCascade(
//             CreateTestElement("div"),
//             "margin",
//             new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" },
//             new[]
//             {
//                 ("div", "margin: 10px;", StylesheetOrigin.Author, Zero, Zero, Zero, One),
//                 ("div", "margin-top: 20px;", StylesheetOrigin.Author, Zero, Zero, Zero, One)
//             },
//             new[] { "20px", "10px", "10px", "10px" }
//         );
//     }
//
//     [Test]
//     public void ResolveCascade_WithUserStyleBeatingAuthorStyle_UserImportantWins()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = new List<MatchedRule>
//         {
//             CreateMatchedRule("div", "color: green !important;", StylesheetOrigin.User, CreateSpecificity(0, 0, 0, 1)),
//             CreateMatchedRule("div", "color: blue;", StylesheetOrigin.Author, CreateSpecificity(0, 1, 0, 1)) // Higher specificity
//         };
//
//         // Act and Assert
//         AssertCascadeResult(element, "color", "rgba(0, 128, 0, 1)", matchedRules, true); // User !important wins
//     }
//
//     #region Additional Tests Using New Helpers
//
//     [Test]
//     public void ResolveCascade_WithNestedProperties_HandlesProperly()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "border: 1px solid black;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div", "border-color: red;", StylesheetOrigin.Author, 0, 0, 0, 1)
//         );
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         AssertPropertyValue(result, "border-width", "1px");
//         AssertPropertyValue(result, "border-style", "solid");
//         AssertPropertyValue(result, "border-color", "rgba(255, 0, 0, 1)");
//     }
//
//     [Test]
//     public void ResolveCascade_WithImportantInDifferentProperties_EachFollowsOwnCascade()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "color: red !important; font-size: 12px;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div", "color: blue; font-size: 16px !important;", StylesheetOrigin.Author, 0, 0, 0, 1)
//         );
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert
//         Assert.That(result, Is.Not.Null);
//         AssertPropertyValue(result, "color", "rgba(255, 0, 0, 1)"); // First rule's !important wins
//         AssertPropertyValue(result, "font-size", "16px"); // Second rule's !important wins
//     }
//
//     [Test]
//     public void ResolveCascade_WithUserAgentImportantVsAuthorImportant_AuthorWins()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//
//         // Test using the importance cascade helper
//         TestImportanceCascade(
//             element,
//             "color",
//             new[]
//             {
//                 ("div", "color: red", StylesheetOrigin.UserAgent, true),
//                 ("div", "color: blue", StylesheetOrigin.Author, true)
//             },
//             "rgba(0, 0, 255, 1)", // Author !important wins
//             true
//         );
//     }
//
//     [Test]
//     public void ResolveCascade_WithUserImportantVsAuthorImportant_AuthorWins()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//
//         // Test using the importance cascade helper
//         TestImportanceCascade(
//             element,
//             "color",
//             new[]
//             {
//                 ("div", "color: green", StylesheetOrigin.User, true),
//                 ("div", "color: blue", StylesheetOrigin.Author, true)
//             },
//             "rgba(0, 0, 255, 1)", // Author !important wins
//             true
//         );
//     }
//
//     [Test]
//     public void ResolveCascade_WithSameSpecificityInSameOrigin_LastOneWins()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "color: red;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div", "color: green;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div", "color: blue;", StylesheetOrigin.Author, 0, 0, 0, 1)
//         );
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert
//         AssertPropertyValue(result, "color", "rgba(0, 0, 255, 1)"); // Last rule (blue) wins
//     }
//
//     [Test]
//     public void ResolveCascade_WithSpecificityDifferences_HigherSpecificityWins()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//
//         // Create rules with progressively higher specificity
//         var matchedRules = new List<MatchedRule>
//         {
//             // Element selector (0,0,0,1)
//             CreateMatchedRule("div", "color: black;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 0, 1)),
//
//             // Two element selectors (0,0,0,2)
//             CreateMatchedRule("html div", "color: gray;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 0, 2)),
//
//             // Element + class selector (0,0,1,1)
//             CreateMatchedRule("div.test", "color: red;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 1, 1)),
//
//             // Element + class + pseudo-class (0,0,2,1)
//             CreateMatchedRule("div.test:hover", "color: green;", StylesheetOrigin.Author, CreateSpecificity(0, 0, 2, 1)),
//
//             // ID selector (0,1,0,0)
//             CreateMatchedRule("#main", "color: blue;", StylesheetOrigin.Author, CreateSpecificity(0, 1, 0, 0))
//         };
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert - ID selector should have highest specificity
//         AssertPropertyValue(result, "color", "rgba(0, 0, 255, 1)");
//     }
//
//     [Test]
//     public void ResolveCascade_WithLayoutProperties_FollowsCorrectCascade()
//     {
//         // Arrange
//         var element = CreateTestElement("div");
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "width: 100px; height: 100px;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div.large", "width: 200px;", StylesheetOrigin.Author, 0, 0, 1, 1),
//             ("div", "height: 150px !important;", StylesheetOrigin.Author, 0, 0, 0, 1)
//         );
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, element);
//
//         // Assert - specificity wins for width, !important wins for height
//         AssertPropertyValue(result, "width", "200px"); // Higher specificity
//         AssertPropertyValue(result, "height", "150px"); // !important
//     }
//
//     [Test]
//     public void ResolveCascade_WithInitialAndInherit_HandlesKeywords()
//     {
//         // Arrange
//         var parent = CreateTestElement("div");
//         SetInlineStyle(parent, "color: red;");
//
//         var child = CreateTestElement("div", parentElement: parent);
//         var matchedRules = CreateMatchedRuleSet(
//             ("div", "color: inherit;", StylesheetOrigin.Author, 0, 0, 0, 1),
//             ("div", "background-color: initial;", StylesheetOrigin.Author, 0, 0, 0, 1)
//         );
//
//         // Act
//         var result = _cascadeResolver.ResolveCascade(matchedRules, child);
//
//         // Assert
//         // Note: Since this is a pure cascade test, inherit doesn't resolve to parent's value yet
//         // That happens during the full style computation with InheritanceProcessor
//         AssertPropertyValue(result, "color", "inherit");
//         AssertPropertyValue(result, "background-color", "initial");
//     }
//
//     #endregion
//
//     // Helper method to assert property values more concisely
//     private void AssertPropertyValue(ICssStyleDeclaration declaration, string propertyName, string expectedValue)
//     {
//         var actualValue = declaration.GetPropertyValue(propertyName);
//         Assert.That(actualValue, Is.EqualTo(expectedValue),
//             $"Property '{propertyName}' should be '{expectedValue}' but was '{actualValue}'");
//     }
// }