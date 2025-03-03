namespace AngleSharp.Css.Tests.Declarations
{
    using NUnit.Framework;
    using static AngleSharp.Css.Tests.CssConstructionFunctions;

    [TestFixture]
    public class CssAllPropertyTests
    {
        [Test]
        public void CssAllInheritLegal()
        {
            var snippet = "all: inherit";
            var property = ParseDeclaration(snippet);
            Assert.AreEqual("all", property.Name);
            Assert.IsFalse(property.IsImportant);
            Assert.True(property.IsInherited);
            Assert.IsTrue(property.HasValue);
            Assert.AreEqual("inherit", property.Value);
        }

        [Test]
        public void CssAllInitialLegal()
        {
            var snippet = "all: initial";
            var property = ParseDeclaration(snippet);
            Assert.AreEqual("all", property.Name);
            Assert.IsFalse(property.IsImportant);
            Assert.IsFalse(property.IsInherited);
            Assert.IsTrue(property.HasValue);
            Assert.AreEqual("initial", property.Value);
        }

        [Test]
        public void CssAllUnsetLegal()
        {
            var snippet = "all: unset";
            var property = ParseDeclaration(snippet);
            Assert.AreEqual("all", property.Name);
            Assert.IsFalse(property.IsImportant);
            Assert.IsFalse(property.IsInherited);
            Assert.IsTrue(property.HasValue);
            Assert.AreEqual("unset", property.Value);
        }

        [Test]
        public void CssAllRevertLegal()
        {
            var snippet = "all: revert";
            var property = ParseDeclaration(snippet);
            Assert.AreEqual("all", property.Name);
            Assert.IsFalse(property.IsImportant);
            Assert.IsFalse(property.IsInherited);
            Assert.IsTrue(property.HasValue);
            Assert.AreEqual("revert", property.Value);
        }

        [Test]
        public void CssAllImportantValid()
        {
            var snippet = "all: initial !important";
            var property = ParseDeclaration(snippet);
            Assert.AreEqual("all", property.Name);
            Assert.IsTrue(property.IsImportant);
            Assert.IsFalse(property.IsInherited);
            Assert.IsTrue(property.HasValue);
            Assert.AreEqual("initial", property.Value);
        }

        [Test]
        public void CssAllNonKeywordIllegal()
        {
            var snippet = "all: none";
            var property = ParseDeclaration(snippet);
            Assert.AreEqual("all", property.Name);
            Assert.IsFalse(property.IsImportant);
            Assert.IsFalse(property.IsInherited);
            Assert.IsFalse(property.HasValue);
        }

        [Test]
        public void CssAllColorValueIllegal()
        {
            var snippet = "all: red";
            var property = ParseDeclaration(snippet);
            Assert.AreEqual("all", property.Name);
            Assert.IsFalse(property.IsImportant);
            Assert.IsFalse(property.IsInherited);
            Assert.IsFalse(property.HasValue);
        }

        [Test]
        public void CssAllMultipleValuesIllegal()
        {
            var snippet = "all: inherit initial";
            var property = ParseDeclaration(snippet);
            Assert.AreEqual("all", property.Name);
            Assert.IsFalse(property.IsImportant);
            Assert.IsFalse(property.IsInherited);
            Assert.IsFalse(property.HasValue);
        }
    }
}