namespace AngleSharp.Css.Dom
{
    using AngleSharp.Css;
    using AngleSharp.Dom;
    using System;

    /// <summary>
    /// Take an url.
    /// </summary>
    sealed class UrlFunction : DocumentFunction
    {
        #region Fields

        private readonly IUrl _expected;

        #endregion

        #region ctor

        public UrlFunction(String url)
            : base(FunctionNames.Url, url)
        {
            _expected = Url.Create(Data);
        }

        #endregion

        #region Methods

        public override Boolean Matches(IUrl actual)
        {
            return !_expected.IsInvalid && _expected.Equals(actual);
        }

        #endregion
    }
}
