namespace AngleSharp.Performance.Css
{
    using System;
    using ExCSS;

    class ExCssParser : ITestee
    {
        public String Name => "ExCSS";

        public Type Library => typeof(ExCssParser);

        public void Run(String source)
        {
            var parser = new ExCssParser();
            parser.Run(source);
        }
    }
}
