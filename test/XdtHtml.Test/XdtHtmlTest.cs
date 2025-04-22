using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace XdtHtml.Test
{
    [TestClass]
    public class XdtHtmlTest
    {
        protected ILogger Log { get; private set; }

        public XdtHtmlTest()
        {
            this.Log = Host.CreateDefaultBuilder()
                        .Build()
                        .Services
                        .GetRequiredService<ILogger<XdtHtmlTest>>();
        }

        [TestMethod]
        public void TestReplaceBasic()
        {
            ApplyTransform("sample_page", "replace_buttons");
        }

        [TestMethod]
        public void TestReplaceDiferentTarget()
        {
            ApplyTransform("sample_page", "replace_buttons_xpath", "replace_buttons_xpath_result");
        }

        [TestMethod]
        public void TestReplaceDiferentTargetFullXpath()
        {
            ApplyTransform("sample_page", "replace_buttons_xpath_full", "replace_buttons_xpath_result");
        }

        [TestMethod]
        public void TestReplaceElementXPath()
        {
            ApplyTransform("sample_page", "replace_element_xpath");
        }

        [TestMethod]
        public void TestReplaceIndentWithComments()
        {
            ApplyTransform("replace_indent_comments_input", "replace_indent_comments", "replace_indent_comments_result");
        }

        [TestMethod]
        public void TestMoveElementsAfter()
        {
            ApplyTransform("sample_page", "move_element_after");
        }

        [TestMethod]
        public void TestMoveAfterIndent()
        {
            ApplyTransform("move_after_indent_input", "move_after_indent");
        }

        [TestMethod]
        public void TestMoveElementsBefore()
        {
            ApplyTransform("sample_page", "move_element_before");
        }


        [TestMethod]
        public void TestMoveElementsInto()
        {
            ApplyTransform("sample_page", "move_into_element");
        }

        [TestMethod]
        public void TestMoveElementsIntoBegining()
        {
            ApplyTransform("sample_page", "move_into_begining_element");
        }

        [TestMethod]
        public void TestMoveElementsIntoEnd()
        {
            ApplyTransform("sample_page", "move_into_end_element");
        }

        [TestMethod]
        public void TestMoveElementsIntoNewElement()
        {
            ApplyTransform("moveinto_new_element_input", "moveinto_new_element");
        }

        [TestMethod]
        public void TestInsertAfterElement()
        {
            ApplyTransform("sample_page", "insert_after_element");
        }

        [TestMethod]
        public void TestInsertBeforeElement()
        {
            ApplyTransform("sample_page", "insert_before_element");
        }

        [TestMethod]
        public void TestInsertElementsInto()
        {
            ApplyTransform("sample_page", "insert_into_element");
        }

        [TestMethod]
        public void TestInsertElementsIntoBegining()
        {
            ApplyTransform("sample_page", "insert_into_begining_element");
        }

        [TestMethod]
        public void TestInsertElementsIntoEnd()
        {
            ApplyTransform("sample_page", "insert_into_end_element");
        }

        [TestMethod]
        public void TestReplaceElementTransform()
        {
            ApplyTransform("sample_page_cards", "replace_element_transform");
        }

        [TestMethod]
        public void TestReplaceIndentNoPreamble()
        {
            ApplyTransform("replace_indent_no_preamble_input", "replace_indent_no_preamble");
        }

        [TestMethod]
        public void TestInsertTransformComment()
        {
            ApplyTransform("insert_transform_comment_input", "insert_transform_comment");
        }

        [TestMethod]
        public void TestCompositeTransform()
        {
            ApplyTransform("composite_transform_input", "composite_transform");
        }

        [TestMethod]
        public void TestContentTextNode()
        {
            ApplyTransform("content_textnode_input", "content_textnode");
        }

        [TestMethod]
        public void TestRemoveAttributes()
        {
            ApplyTransform("AberturaContaPresencialCriarProcesso.StepValidacao", "AberturaContaPresencialCriarProcesso.StepValidacao_delta", "AberturaContaPresencialCriarProcesso.StepValidacao_expected");
        }

        [TestMethod]
        public void TestMoveIntoNewElement()
        {
            ApplyTransform("sample_move_into_new_element_original", "sample_move_into_new_element_input", "sample_move_into_new_element_result");
        }

        protected void ApplyTransform(string contentFileName, string transformFileName, string resultFileName = null)
        {
            resultFileName ??= transformFileName + "_result";

            this.Log.LogInformation($"Running test case transformation file {transformFileName}, expecting result {resultFileName}");

            var x = new HtmlTransformableDocument();
            x.Load($"./test_files/{contentFileName}.html");

            Transform.TransformMessageType = MessageType.Normal;
            using (var transform = new HtmlTransformation($"./test_files/{transformFileName}.html", new NetCoreHtmlTransformationLogger(this.Log, false)))
            {
                if (transform.Apply(x))
                {
                    var expected = File.ReadAllText($"./test_files/{resultFileName}.html");
                    var actual = x.DocumentNode.OuterHtml;
                    Assert.AreEqual(
                        expected,
                        actual,
                        $"Difference:<{Difference(expected, actual)}>"
                        );
                    ;
                }
                else
                    Assert.Fail("Transform failed to apply");
            }
        }

        public static string Difference(string str1, string str2)
        {
            if (str1 == null)
            {
                return str2;
            }
            if (str2 == null)
            {
                return str1;
            }

            var set1 = str1.Split(' ').Distinct().ToList();
            var set2 = str2.Split(' ').Distinct().ToList();

            var diff = set2.Count() > set1.Count() ? set2.Except(set1).ToList() : set1.Except(set2).ToList();

            return string.Join("", diff);
        }
    }
}
