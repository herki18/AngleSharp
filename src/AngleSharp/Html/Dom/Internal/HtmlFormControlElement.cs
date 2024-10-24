namespace AngleSharp.Html.Dom
{
    using AngleSharp.Dom;
    using System;
    using System.Linq;

    /// <summary>
    /// Represents the base class for all HTML form control elements.
    /// </summary>
    public abstract class HtmlFormControlElement : HtmlElement, ILabelabelElement, IValidation
    {
        #region Fields

        private readonly NodeList _labels;
        private readonly ValidityState _vstate;
        private String? _error;

        #endregion

        #region ctor

        /// <inheritdoc />
        public HtmlFormControlElement(Document owner, String name, String? prefix, NodeFlags flags = NodeFlags.None)
            : base(owner, name, prefix, flags | NodeFlags.Special)
        {
            _vstate = new ValidityState();
            _labels = [];
        }

        #endregion

        #region Properties

        /// <summary>
        ///
        /// </summary>
        public String? Name
        {
            get => this.GetOwnAttribute(AttributeNames.Name);
            set => this.SetOwnAttribute(AttributeNames.Name, value);
        }

        /// <summary>
        ///
        /// </summary>
        public IHtmlFormElement? Form => GetAssignedForm();

        /// <summary>
        ///
        /// </summary>
        public Boolean IsDisabled
        {
            get => this.GetBoolAttribute(AttributeNames.Disabled) || IsFieldsetDisabled();
            set => this.SetBoolAttribute(AttributeNames.Disabled, value);
        }

        /// <summary>
        ///
        /// </summary>
        public Boolean Autofocus
        {
            get => this.GetBoolAttribute(AttributeNames.AutoFocus);
            set => this.SetBoolAttribute(AttributeNames.AutoFocus, value);
        }

        /// <summary>
        ///
        /// </summary>
        public INodeList Labels => _labels;

        /// <inheritdoc />
        public String? ValidationMessage => _vstate.IsCustomError ? _error : String.Empty;

        /// <inheritdoc />
        public Boolean WillValidate => !IsDisabled && CanBeValidated();

        /// <inheritdoc />
        public IValidityState Validity
        {
            get { Check(_vstate); return _vstate; }
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public override Node Clone(Document owner, Boolean deep)
        {
            var node = (HtmlFormControlElement)base.Clone(owner, deep);
            node.SetCustomValidity(_error);
            return node;
        }

        /// <inheritdoc />
        public Boolean CheckValidity()
        {
            return WillValidate && Validity.IsValid;
        }

        /// <inheritdoc />
        public void SetCustomValidity(String? error)
        {
            _error = error;
            ResetValidity(_vstate);
        }

        #endregion

        #region Helpers

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected virtual Boolean IsFieldsetDisabled()
        {
            var fieldSets = this.GetAncestors().OfType<IHtmlFieldSetElement>();

            foreach (var fieldSet in fieldSets)
            {
                if (fieldSet.IsDisabled)
                {
                    var firstLegend = fieldSet.ChildNodes.FirstOrDefault(m => m is IHtmlLegendElement);
                    return firstLegend == null || !this.IsDescendantOf(firstLegend);
                }
            }

            return false;
        }

        internal virtual void ConstructDataSet(FormDataSet dataSet, IHtmlElement submitter)
        { }

        internal virtual void Reset()
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        protected virtual void Check(ValidityState state)
        {
            ResetValidity(state);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        protected void ResetValidity(ValidityState state)
        {
            state.IsCustomError = !String.IsNullOrEmpty(_error);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected abstract Boolean CanBeValidated();

        #endregion
    }
}
