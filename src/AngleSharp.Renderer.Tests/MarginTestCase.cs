using System.Collections.Generic;

namespace AngleSharp.Renderer.Tests
{
    public class MarginTestCase
    {
        public string Html { get; init; }
        public double ExpectedX { get; init; }
        public double ExpectedY { get; init; }
        public string Description { get; init; }

        public MarginTestCase(string html, double expectedX, double expectedY, string description)
        {
            Html = html;
            ExpectedX = expectedX;
            ExpectedY = expectedY;
            Description = description;
        }
    }


    public static class MarginTestData
    {
        public static IEnumerable<MarginTestCase> ParentChildTopMarginCases
        {
            get
            {
                yield return new MarginTestCase(
                    "<body id='d0' style='margin: 0px;'>" +
                        "<div id='d1' style='margin-top: 20px;'>" +
                            "<div id='d2' style='margin-top: 30px;'></div>" +
                        "</div>" +
                    "</body>",
                    0, 30,
                    "Parent 20px + Child 30px margins collapse to largest (30px)"
                );

                yield return new MarginTestCase(
                    "<body id='d0' style='margin: 0px;'>" +
                        "<div id='d1' style='margin-top: 0px;'>" +
                            "<div id='d2' style='margin-top: 30px;'></div>" +
                        "</div>" +
                    "</body>",
                    0, 30,
                    "Parent 0px + Child 30px margins collapse to child margin"
                );

                yield return new MarginTestCase(
                    "<body id='d0' style='margin: 0px;'>" +
                        "<div id='d1' style='margin-top: 20px;'>" +
                            "<div id='d2' style='margin-top: -10px;'></div>" +
                        "</div>" +
                    "</body>",
                    0, 10,
                    "Parent 20px + Child -10px margins collapse to sum (10px)"
                );

                yield return new MarginTestCase(
                    "<body id='d0' style='margin: 0px;'>" +
                        "<div id='d1' style='margin-top: -20px;'>" +
                            "<div id='d2' style='margin-top: -10px;'></div>" +
                        "</div>" +
                    "</body>",
                    0, -20,
                    "Negative margins: Parent -20px + Child -10px collapse to smallest (-20px)"
                );
            }
        }

        public static IEnumerable<MarginTestCase> ParentChildBottomMarginCases
        {
            get
            {
                yield return new MarginTestCase(
                    "<body id='d0' style='margin: 0px;'>" +
                        "<div id='d1' style='margin-bottom: 20px;'>" +
                            "<div id='d2' style='margin-bottom: 30px;'></div>" +
                        "</div>" +
                        "<div id='d1.1' style='background: red; height: 1px;'></div>" +
                    "</body>",
                    0, 30,
                    "Parent and child bottom margins collapse to largest (30px)"
                );

                yield return new MarginTestCase(
                    "<body id='d0' style='margin: 0px;'>" +
                        "<div id='d1' style='margin-bottom: 0px;'>" +
                            "<div id='d2' style='margin-bottom: 30px;'></div>" +
                        "</div>" +
                        "<div id='d1.1' style='background: red; height: 1px;'></div>" +
                    "</body>",
                    0, 30,
                    "Parent 0px + Child 30px bottom margins collapse to child margin"
                );
            }
        }

        public static IEnumerable<MarginTestCase> DeepHierarchyMarginCases
        {
            get
            {
                yield return new MarginTestCase(
                    "<body id='d0' style='margin: 0px;'>" +
                        "<div id='d1' style='margin-top: 20px;'>" +
                            "<div id='d2' style='margin-top: 30px;'>" +
                                "<div id='d3' style='margin-top: 10px;'></div>" +
                            "</div>" +
                        "</div>" +
                    "</body>",
                    0, 30,
                    "Three levels deep: margins collapse to largest (30px)"
                );
            }
        }
    }
}