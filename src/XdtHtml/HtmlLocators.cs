using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using XdtHtml.Properties;
using HtmlAgilityPack;

namespace XdtHtml
{
    internal sealed class DefaultLocator : Locator
    {
        // Uses all the default behavior

        private static DefaultLocator instance = null;
        internal static DefaultLocator Instance {
            get {
                if (instance == null) {
                    instance = new DefaultLocator();
                }
                return instance;
            }
        }
    }

    public sealed class Match : Locator
    {
        protected override string ConstructPredicate() {
            EnsureArguments(1);

            string keyPredicate = null;

            foreach (string key in Arguments) {
                HtmlAttribute keyAttribute = CurrentElement.Attributes[key];

                if (keyAttribute != null) {
                    string keySegment = String.Format(CultureInfo.InvariantCulture, "@{0}='{1}'", keyAttribute.Name, keyAttribute.Value);
                    if (keyPredicate == null) {
                        keyPredicate = keySegment;
                    }
                    else {
                        keyPredicate = String.Concat(keyPredicate, " and ", keySegment);
                    }
                }
                else {
                    throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_MatchAttributeDoesNotExist, key));
                }
            }

            return keyPredicate;
        }
    }

    public sealed class Condition : Locator
    {
        protected override string ConstructPredicate() {
            EnsureArguments(1, 1);

            return Arguments[0];
        }
    }

    /// <summary>
    /// This type of locator preserves previous behavior of assuming to be complete if rooted
    /// Also preserves the behavior of adding current node to context if relative
    /// </summary>
    public sealed class FullXPath : Locator
    {
        protected override string ParentPath
        {
            get
            {
                return ConstructPath();
            }
        }

        protected override string ConstructPath()
        {
            EnsureArguments(1, 1);

            string xpath = Arguments[0];
            if (!xpath.StartsWith("/", StringComparison.Ordinal))
            {
                // Relative XPath
                xpath = AppendStep(base.ParentPath, NextStepNodeTest);
                xpath = AppendStep(xpath, Arguments[0]);
                xpath = xpath.Replace("/./", "/");
            }

            return xpath;
        }
    }

    public sealed class XPath : Locator
    {
        protected override string ParentPath {
            get {
                return ConstructPath();
            }
        }

        protected override string ConstructPath() {
            EnsureArguments(1, 1);

            string xpath = Arguments[0];
            if (xpath.StartsWith("/", StringComparison.Ordinal))
                return xpath;

            // Relative XPath
            // In XPath locator we expect the XPath to contain everything including currentElement,
            // so we are not appending current element axis
            // This allows the use of XPath to replace the current element with one with diferent name
            // See test case of replace_element
            xpath = AppendStep(base.ParentPath, Arguments[0]);
            xpath = xpath.Replace("/./", "/");

            return xpath;
        }
    }
}
