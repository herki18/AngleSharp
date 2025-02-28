using System;
using System.Linq;
using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Css.Dom;
using NUnit.Framework;

namespace AngleSharp.LayoutEngine.Tests
{
    [TestFixture]
    public class RenderTreeTests : LayoutEngineTestBase
    {
        [Test]
        public void BuildRenderTree_EmptyDocument_ReturnsEmptyTree()
        {
            // Arrange
            var document = CreateDocument("<html></html>");
            var builder = new RenderTreeBuilder();

            // Act
            var renderTree = builder.BuildRenderTree(document);

            // Assert
            Assert.That(renderTree, Is.Not.Null);
            Assert.That(renderTree.Root, Is.Not.Null);
            Assert.That(renderTree.Root.Children, Is.Empty);
        }

        [Test]
        public void BuildRenderTree_BasicHtmlStructure_CreatesCorrectTree()
        {
            // Arrange
            var document = CreateDocument(@"
                <html>
                    <body>
                        <div id='test'>Hello</div>
                    </body>
                </html>");
            var builder = new RenderTreeBuilder();

            // Act
            var renderTree = builder.BuildRenderTree(document);

            // Assert
            Assert.That(renderTree.Root, Is.Not.Null);
            var body = renderTree.Root.Children.FirstOrDefault();
            Assert.That(body, Is.Not.Null);
            var div = body.Children.FirstOrDefault();
            Assert.That(div, Is.Not.Null);
            Assert.That(div.Id, Is.EqualTo("test"));
        }

        [Test]
        public void BuildRenderTree_WithDisplayNone_ExcludesNode()
        {
            // Arrange
            var document = CreateDocument(@"
                <html>
                    <body>
                        <div style='display: none'>Hidden</div>
                        <div>Visible</div>
                    </body>
                </html>");
            var builder = new RenderTreeBuilder();

            // Act
            var renderTree = builder.BuildRenderTree(document);

            // Assert
            var body = renderTree.Root.Children.FirstOrDefault();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.Children.Count(), Is.EqualTo(1));
            Assert.That(body.Children.First().Ref.TextContent, Is.EqualTo("Visible"));
        }

        [Test]
        public void FindNodeById_ExistingId_ReturnsCorrectNode()
        {
            // Arrange
            var document = CreateDocument(@"
                <html>
                    <body>
                        <div id='target'>Found me!</div>
                    </body>
                </html>");
            var builder = new RenderTreeBuilder();
            var renderTree = builder.BuildRenderTree(document);

            // Act
            var node = renderTree.FindNodeById("target");

            // Assert
            Assert.That(node, Is.Not.Null);
            Assert.That(node.Id, Is.EqualTo("target"));
        }

        [Test]
        public void GetChangedNodes_StyleChange_DetectsChange()
        {
            // Arrange
            var document = CreateDocument(@"
                <html>
                    <body>
                        <div id='test'>Test</div>
                    </body>
                </html>");
            var builder = new RenderTreeBuilder();
            var firstTree = builder.BuildRenderTree(document);

            // Modify style
            var element = document.GetElementById("test");
            Assert.That(element, Is.Not.Null);
            element.SetAttribute("style", "color: red");

            // Act
            var secondTree = builder.BuildRenderTree(document);

            // Assert
            var changedNodes = builder.GetChangedNodes().ToList();
            Assert.That(changedNodes, Is.Not.Empty);
            Assert.That(changedNodes[0].Id, Is.EqualTo("test"));
        }

        [Test]
        public void GetAddedNodes_NewElement_DetectsAddition()
        {
            // Arrange
            var document = CreateDocument(@"
                <html>
                    <body>
                        <div id='container'></div>
                    </body>
                </html>");
            var builder = new RenderTreeBuilder();
            var firstTree = builder.BuildRenderTree(document);

            // Add new element
            var container = document.GetElementById("container");
            var newDiv = document.CreateElement("div");
            newDiv.Id = "new";
            Assert.That(container, Is.Not.Null);
            container.AppendChild(newDiv);

            // Act
            var secondTree = builder.BuildRenderTree(document);

            // Assert
            var addedNodes = builder.GetAddedNodes().ToList();
            Assert.That(addedNodes, Is.Not.Empty);
            Assert.That(addedNodes[0].Id, Is.EqualTo("new"));
        }

        [Test]
        public void GetRemovedNodes_DeletedElement_DetectsRemoval()
        {
            // Arrange
            var document = CreateDocument(@"
                <html>
                    <body>
                        <div id='container'>
                            <div id='toRemove'>Remove me</div>
                        </div>
                    </body>
                </html>");
            var builder = new RenderTreeBuilder();
            var firstTree = builder.BuildRenderTree(document);

            // Remove element
            var elementToRemove = document.GetElementById("toRemove");
            Assert.That(elementToRemove, Is.Not.Null);
            elementToRemove.Remove();

            // Act
            var secondTree = builder.BuildRenderTree(document);

            // Assert
            var removedNodes = builder.GetRemovedNodes().ToList();
            Assert.That(removedNodes, Is.Not.Empty);
            Assert.That(removedNodes[0].Id, Is.EqualTo("toRemove"));
        }
    }
}