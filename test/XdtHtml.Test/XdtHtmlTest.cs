using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

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
            ApplyTransform("sample_page", "replace_buttons2");
        }

        [TestMethod]
        public void TestMoveElementsAfter()
        {
            ApplyTransform("sample_page", "move_elements");
        }

        [TestMethod]
        public void TestMoveElementsBefore()
        {
            ApplyTransform("sample_page", "move_elements2");
        }

        protected void ApplyTransform(string contentFileName, string transformFileName, string resultFileName = null)
        {
            var x = new HtmlTransformableDocument();
            x.Load($"./test_files/{contentFileName}.html");

            Transform.TransformMessageType = MessageType.Normal;
            using (var transform = new HtmlTransformation($"./test_files/{transformFileName}.html", new NetCoreHtmlTransformationLogger(this.Log, false)))
            {
                if (transform.Apply(x))
                {
                    Assert.AreEqual(
                        File.ReadAllText($"./test_files/{resultFileName ?? transformFileName + "_result"}.html"),
                        x.DocumentNode.OuterHtml
                        );
                    ;
                }
            }
        }
    }
}
