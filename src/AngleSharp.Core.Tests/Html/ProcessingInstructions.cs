namespace AngleSharp.Core.Tests.Html
{
    using AngleSharp.Html.Parser;
    using Dom;
    using NUnit.Framework;

    [TestFixture]
    public class ProcessingInstructions
    {
        [Test]
        public void ParsingWithSupportForProcessingInstructionsShouldProduceNode()
        {
            var source = @"<?foo bar>";
            var parser = new HtmlParser(new HtmlParserOptions
            {
                IsSupportingProcessingInstructions = true
            });
            var document = parser.ParseDocument(source);
            Assert.AreEqual(Dom.NodeType.ProcessingInstruction, (NodeType)document.ChildNodes[0].NodeType);
        }

        [Test]
        public void ParsingWithoutSupportForProcessingInstructionsShouldCommentThemOut()
        {
            var source = @"<?foo bar>";
            var parser = new HtmlParser(new HtmlParserOptions
            {
                IsSupportingProcessingInstructions = false
            });
            var document = parser.ParseDocument(source);
            Assert.AreEqual(Dom.NodeType.Comment, (NodeType)document.ChildNodes[0].NodeType);
        }
    }
}
