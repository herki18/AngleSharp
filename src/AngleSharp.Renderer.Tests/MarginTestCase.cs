using System.Collections.Generic;
using NUnit.Framework;

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

        public override string ToString()
        {
            return $"{Description} - Expected X: {ExpectedX}, Expected Y: {ExpectedY}";
        }
    }


    public static class MarginTestData
    {
        public static IEnumerable<TestCaseData<MarginTestCase>> ParentChildTopMarginCases
        {
            get
            {
                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-top: 20px;'>
                            <div id='d2' style='margin-top: 30px;'></div>
                        </div>
                    </body>",
                    0, 30,
                    "Parent 20px + Child 30px margins collapse to largest (30px)"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-top: 0px;'>
                            <div id='d2' style='margin-top: 30px;'></div>
                        </div>
                    </body>",
                    0, 30,
                    "Parent 0px + Child 30px margins collapse to child margin"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-top: 20px;'>
                            <div id='d2' style='margin-top: -10px;'></div>
                        </div>
                    </body>",
                    0, 10,
                    "Parent 20px + Child -10px margins collapse to sum (10px)"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-top: -20px;'>
                            <div id='d2' style='margin-top: -10px;'></div>
                        </div>
                    </body>",
                    0, -20,
                    "Negative margins: Parent -20px + Child -10px collapse to smallest (-20px)"
                ));
            }
        }

        public static IEnumerable<TestCaseData<MarginTestCase>> ParentChildBottomMarginCases
        {
            get
            {
                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-bottom: 20px;'>
                            <div id='d2' style='margin-bottom: 30px;'></div>
                        </div>
                        <div id='d1.1' style='background: red; height: 1px;'></div>
                    </body>",
                    0, 30,
                    "Parent and child bottom margins collapse to largest (30px)"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-bottom: 0px;'>
                            <div id='d2' style='margin-bottom: 30px;'></div>
                        </div>
                        <div id='d1.1' style='background: red; height: 1px;'></div>
                    </body>",
                    0, 30,
                    "Parent 0px + Child 30px bottom margins collapse to child margin"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-bottom: 20px;'>
                            <div id='d2' style='margin-bottom: -10px;'></div>
                        </div>
                        <div id='d1.1' style='background: red; height: 1px;'></div>
                    </body>",
                    0, 10,
                    "Parent 20px + Child -10px bottom margins collapse to sum (10px)"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-bottom: -20px;'>
                            <div id='d2' style='margin-bottom: -10px;'></div>
                        </div>
                        <div id='d1.1' style='background: red; height: 1px;'></div>
                    </body>",
                    0, -30,
                    "Negative margins: Parent -20px + Child -10px bottom margins collapse to sum (-30px)"
                ));
            }
        }

        public static IEnumerable<TestCaseData<MarginTestCase>> DeepHierarchyMarginCases
        {
            get
            {
                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-top: 20px;'>
                            <div id='d2' style='margin-top: 30px;'>
                                <div id='d3' style='margin-top: 10px;'></div>
                            </div>
                        </div>
                    </body>",
                    0, 30,
                    "Three levels deep: margins collapse to largest (30px)"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-top: 20px;'>
                            <div id='d2' style='margin-top: -10px;'>
                                <div id='d3' style='margin-top: 15px;'></div>
                            </div>
                        </div>
                    </body>",
                    0, 10,
                    "Three levels with negative margin: collapse to sum of highest and negative"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-top: 0px;'>
                            <div id='d2' style='margin-top: 30px;'>
                                <div id='d3' style='margin-top: 0px;'></div>
                            </div>
                        </div>
                    </body>",
                    0, 30,
                    "Three levels with zero margins: collapse to highest margin"
                ));

                yield return new TestCaseData<MarginTestCase>(new MarginTestCase(
                    @"<body id='d0' style='margin: 0px;'>
                        <div id='d1' style='margin-top: 20px; padding: 10px;'>
                            <div id='d2' style='margin-top: 30px;'>
                                <div id='d3' style='margin-top: 10px;'></div>
                            </div>
                        </div>
                    </body>",
                    10, 40,
                    "Three levels with padding: affects position but not margin collapse"
                ));
            }
        }
    }
}