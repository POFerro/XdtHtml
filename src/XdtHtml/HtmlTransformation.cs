using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.IO;
using XdtHtml.Properties;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;
using AngleSharp;

namespace XdtHtml
{
    public class HtmlTransformation : IServiceProvider, IDisposable
    {
        internal static readonly string TransformNamespace = "http://schemas.microsoft.com/XML-Document-Transform";
        internal static readonly string SupressWarnings = "SupressWarnings";

        #region private data members
        private readonly string transformFile;

        private readonly IElement htmlTransformation;
        private IDocument htmlTarget;
        private HtmlTransformableDocument htmlTransformable;

        private readonly HtmlTransformationLogger logger = null;

        private NamedTypeFactory namedTypeFactory;
        private ServiceContainer transformationServiceContainer = new ServiceContainer();
        private ServiceContainer documentServiceContainer = null;

        private bool hasTransformNamespace = false;
        #endregion

        public HtmlTransformation(string transformFile)
            : this(transformFile, true, null)
        {
        }

        public HtmlTransformation(string transformFile, IHtmlTransformationLogger logger)
            : this(transformFile, true, logger)
        {
        }

        public HtmlTransformation(string transform, bool isTransformAFile, IHtmlTransformationLogger logger)
        {
            this.transformFile = transform;
            this.logger = new HtmlTransformationLogger(logger);

            var parser = new HtmlParser(HtmlDocumentExtensions.ParserDefaultOptions());
            htmlTransformation = parser.ParseFragment(isTransformAFile ? File.ReadAllText(transform) : transform, null).OfType<IElement>().First();

            InitializeTransformationServices();

            PreprocessTransformDocument();
        }

        public HtmlTransformation(Stream transformStream, IHtmlTransformationLogger logger)
        {
            this.logger = new HtmlTransformationLogger(logger);
            this.transformFile = String.Empty;

            var parser = new HtmlParser(HtmlDocumentExtensions.ParserDefaultOptions());
            htmlTransformation = parser.ParseFragment(transformStream, null).OfType<IElement>().First();

            InitializeTransformationServices();

            PreprocessTransformDocument();
        }

        public HtmlTransformation(TextReader transformReader, IHtmlTransformationLogger logger)
        {
            this.logger = new HtmlTransformationLogger(logger);
            this.transformFile = String.Empty;

            var parser = new HtmlParser(HtmlDocumentExtensions.ParserDefaultOptions());
            htmlTransformation = parser.ParseFragment(transformReader.ReadToEnd(), null).OfType<IElement>().First();

            InitializeTransformationServices();

            PreprocessTransformDocument();
        }

        public bool HasTransformNamespace
        {
            get
            {
                return hasTransformNamespace;
            }
        }

        private void InitializeTransformationServices()
        {
            // Initialize NamedTypeFactory
            namedTypeFactory = new NamedTypeFactory(transformFile);
            transformationServiceContainer.AddService(namedTypeFactory.GetType(), namedTypeFactory);

            // Initialize TransformationLogger
            transformationServiceContainer.AddService(logger.GetType(), logger);
        }

        private void InitializeDocumentServices(IDocument document)
        {
            Debug.Assert(documentServiceContainer == null);
            documentServiceContainer = new ServiceContainer();

            if (document is IHtmlOriginalDocumentService)
            {
                documentServiceContainer.AddService(typeof(IHtmlOriginalDocumentService), document);
            }
        }

        private void ReleaseDocumentServices()
        {
            if (documentServiceContainer != null)
            {
                documentServiceContainer.RemoveService(typeof(IHtmlOriginalDocumentService));
                documentServiceContainer = null;
            }
        }

        private void PreprocessTransformDocument()
        {
            hasTransformNamespace = false;
            hasTransformNamespace = htmlTransformation.LookupPrefix(TransformNamespace) != null;

            if (hasTransformNamespace)
            {
                // This will look for all nodes from our namespace in the document,
                // and do any initialization work
                //XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());
                //namespaceManager.AddNamespace("xdt", TransformNamespace);
                var namespaceNodes = htmlTransformation
                .Descendants<HtmlElement>()
                .Where(n => n.NamespaceUri == TransformNamespace);

                foreach (var element in namespaceNodes)
                {

                    HtmlElementContext context = null;
                    try
                    {
                        switch (element.LocalName)
                        {
                            case "Import":
                                context = CreateElementContext(null, element);
                                PreprocessImportElement(context);
                                break;
                            default:
                                logger.LogWarning(element, Resources.XMLTRANSFORMATION_UnknownXdtTag, element.TagName);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (context != null)
                        {
                            ex = WrapException(ex, context);
                        }

                        logger.LogErrorFromException(ex);
                        throw new HtmlTransformationException(Resources.XMLTRANSFORMATION_FatalTransformSyntaxError, ex);
                    }
                    finally
                    {
                        context = null;
                    }
                }
            }
        }

        public void AddTransformationService(System.Type serviceType, object serviceInstance)
        {
            transformationServiceContainer.AddService(serviceType, serviceInstance);
        }

        public void RemoveTransformationService(System.Type serviceType)
        {
            transformationServiceContainer.RemoveService(serviceType);
        }

        public bool Apply(HtmlTransformableDocument document)
        {
            return Apply(document.Document);
        }

        public bool Apply(IDocument htmlTarget)
        {
            Debug.Assert(this.htmlTarget == null, "This method should not be called recursively");

            if (this.htmlTarget == null)
            {
                // Reset the error state
                logger.HasLoggedErrors = false;

                this.htmlTarget = htmlTarget;
                this.htmlTransformable = htmlTarget as HtmlTransformableDocument;
                try
                {
                    if (hasTransformNamespace)
                    {
                        InitializeDocumentServices(htmlTarget);

                        TransformLoop(htmlTransformation);
                    }
                    else
                    {
                        logger.LogMessage(MessageType.Normal, "The expected namespace {0} was not found in the transform file", TransformNamespace);
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
                finally
                {
                    ReleaseDocumentServices();

                    this.htmlTarget = null;
                    this.htmlTransformable = null;
                }

                return !logger.HasLoggedErrors;
            }
            else
            {
                return false;
            }
        }

        private void TransformLoop(IElement htmlSource)
        {
            TransformLoop(new HtmlNodeContext(htmlSource));
        }

        private void TransformLoop(HtmlNodeContext parentContext)
        {
            foreach (IElement element in parentContext.Node.ChildNodes.OfType<IElement>())
            {
                HtmlElementContext context = CreateElementContext(parentContext as HtmlElementContext, element);
                try
                {
                    HandleElement(context);
                }
                catch (Exception ex)
                {
                    HandleException(ex, context);
                }
            }
        }

        private HtmlElementContext CreateElementContext(HtmlElementContext parentContext, IElement element)
        {
            return new HtmlElementContext(parentContext, element, htmlTarget, this);
        }

        private void HandleException(Exception ex)
        {
            logger.LogErrorFromException(ex);
        }

        private void HandleException(Exception ex, HtmlNodeContext context)
        {
            HandleException(WrapException(ex, context));
        }

        private Exception WrapException(Exception ex, HtmlNodeContext context)
        {
            return HtmlNodeException.Wrap(ex, context.Node);
        }

        private void HandleElement(HtmlElementContext context)
        {
            string argumentString;
            Transform transform = context.ConstructTransform(out argumentString);
            if (transform != null)
            {

                bool fOriginalSupressWarning = logger.SupressWarnings;

                IAttr SupressWarningsAttribute = context.Element.Attributes[HtmlTransformation.SupressWarnings];
                if (SupressWarningsAttribute != null)
                {
                    bool fSupressWarning = System.Convert.ToBoolean(SupressWarningsAttribute.Value, System.Globalization.CultureInfo.InvariantCulture);
                    logger.SupressWarnings = fSupressWarning;
                }

                try
                {
                    OnApplyingTransform();

                    transform.Execute(context, argumentString);

                    OnAppliedTransform();
                }
                catch (Exception ex)
                {
                    HandleException(ex, context);
                }
                finally
                {
                    // reset back the SupressWarnings back per node
                    logger.SupressWarnings = fOriginalSupressWarning;
                }
            }

            // process children
            TransformLoop(context);
        }

        private void OnApplyingTransform()
        {
            if (htmlTransformable != null)
            {
                htmlTransformable.OnBeforeChange();
            }
        }

        private void OnAppliedTransform()
        {
            if (htmlTransformable != null)
            {
                htmlTransformable.OnAfterChange();
            }
        }

        private void PreprocessImportElement(HtmlElementContext context)
        {
            string assemblyName = null;
            string nameSpace = null;
            string path = null;

            foreach (IAttr attribute in context.Element.Attributes)
            {
                switch (attribute.Name)
                {
                    case "assembly":
                        assemblyName = attribute.Value;
                        continue;
                    case "namespace":
                        nameSpace = attribute.Value;
                        continue;
                    case "path":
                        path = attribute.Value;
                        continue;
                }

                throw new HtmlNodeException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_ImportUnknownAttribute, attribute.Name), attribute);
            }

            if (assemblyName != null && path != null)
            {
                throw new HtmlNodeException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_ImportAttributeConflict), context.Element);
            }
            else if (assemblyName == null && path == null)
            {
                throw new HtmlNodeException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_ImportMissingAssembly), context.Element);
            }
            else if (nameSpace == null)
            {
                throw new HtmlNodeException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_ImportMissingNamespace), context.Element);
            }
            else
            {
                if (assemblyName != null)
                {
                    namedTypeFactory.AddAssemblyRegistration(assemblyName, nameSpace);
                }
                else
                {
                    namedTypeFactory.AddPathRegistration(path, nameSpace);
                }
            }
        }

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            object service = null;
            if (documentServiceContainer != null)
            {
                service = documentServiceContainer.GetService(serviceType);
            }
            if (service == null)
            {
                service = transformationServiceContainer.GetService(serviceType);
            }
            return service;
        }

        #endregion

        #region Dispose Pattern
        protected virtual void Dispose(bool disposing)
        {
            if (transformationServiceContainer != null)
            {
                transformationServiceContainer.Dispose();
                transformationServiceContainer = null;
            }

            if (documentServiceContainer != null)
            {
                documentServiceContainer.Dispose();
                documentServiceContainer = null;
            }

            //if (htmlTransformable!= null)
            //{
            //    htmlTransformable.Dispose();
            //    htmlTransformable = null;
            //}

            //if (htmlTransformation as XmlFileInfoDocument != null)
            //{
            //    (htmlTransformation as XmlFileInfoDocument).Dispose();
            //    htmlTransformation = null;
            //}
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HtmlTransformation()
        {
            Debug.Fail("call dispose please");
            Dispose(false);
        }
        #endregion

    }
}
