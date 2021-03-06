﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Redwood.Framework.Binding;
using Redwood.Framework.Configuration;
using Redwood.Framework.Controls;
using Redwood.Framework.Hosting;
using Redwood.Framework.Parser;

namespace Redwood.Framework.Runtime
{
    /// <summary>
    /// Default Redwood control resolver.
    /// </summary>
    public class DefaultControlResolver : IControlResolver
    {

        private readonly RedwoodConfiguration configuration;
        private readonly IControlBuilderFactory controlBuilderFactory;
        private readonly IMarkupFileLoader markupFileLoader;

        private static ConcurrentDictionary<string, ControlType> cachedTagMappings = new ConcurrentDictionary<string, ControlType>();
        private static ConcurrentDictionary<Type, ControlResolverMetadata> cachedMetadata = new ConcurrentDictionary<Type, ControlResolverMetadata>();

        private static object locker = new object();
        private static bool isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultControlResolver"/> class.
        /// </summary>
        public DefaultControlResolver(RedwoodConfiguration configuration)
        {
            this.configuration = configuration;
            this.controlBuilderFactory = configuration.ServiceLocator.GetService<IControlBuilderFactory>();
            this.markupFileLoader = configuration.ServiceLocator.GetService<IMarkupFileLoader>();

            if (!isInitialized)
            {
                lock (locker)
                {
                    if (!isInitialized)
                    {
                        InvokeStaticConstructorsOnAllControls();
                        isInitialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// Invokes the static constructors on all controls to register all <see cref="RedwoodProperty"/>.
        /// </summary>
        private void InvokeStaticConstructorsOnAllControls()
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsClass).ToList();
            foreach (var type in allTypes)
            {
                if (type.GetCustomAttribute<ContainsRedwoodPropertiesAttribute>(true) != null)
                {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
            }
        }


        /// <summary>
        /// Resolves the metadata for specified element.
        /// </summary>
        public ControlResolverMetadata ResolveControl(string tagPrefix, string tagName, out object[] activationParameters)
        {
            // html element has no prefix
            if (string.IsNullOrEmpty(tagPrefix))
            {
                activationParameters = new object[] { tagName };
                return ResolveControl(typeof (HtmlGenericControl));
            }

            // find cached value
            var searchKey = GetSearchKey(tagPrefix, tagName);
            activationParameters = null;
            var controlType = cachedTagMappings.GetOrAdd(searchKey, _ => FindControlMetadata(tagPrefix, tagName));
            var metadata = ResolveControl(controlType.Type, controlType.ControlBuilderType);
            return metadata;
        }

        private static string GetSearchKey(string tagPrefix, string tagName)
        {
            return tagPrefix + ":" + tagName;
        }

        /// <summary>
        /// Resolves the control metadata for specified type.
        /// </summary>
        public ControlResolverMetadata ResolveControl(Type type, Type controlBuilderType = null)
        {
            return cachedMetadata.GetOrAdd(type, _ => BuildControlMetadata(type, controlBuilderType));
        }

        /// <summary>
        /// Resolves the binding type.
        /// </summary>
        public Type ResolveBinding(string bindingType)
        {
            if (bindingType == Constants.ValueBinding)
            {
                return typeof (ValueBindingExpression);
            }
            else if (bindingType == Constants.CommandBinding)
            {
                return typeof (CommandBindingExpression);
            }
            else if (bindingType == Constants.ControlStateBinding)
            {
                return typeof (ControlStateBindingExpression);
            }
            else if (bindingType == Constants.ControlPropertyBinding)
            {
                return typeof (ControlPropertyBindingExpression);
            }
            else if (bindingType == Constants.ControlCommandBinding)
            {
                return typeof(ControlCommandBindingExpression);
            }
            else if (bindingType == Constants.ResourceBinding)
            {
                return typeof (ResourceBindingExpression);
            }
            else
            {
                throw new NotSupportedException(string.Format("The binding {{{0}: ... }} is unknown!", bindingType));   // TODO: exception handling
            }
        }

        /// <summary>
        /// Finds the control metadata.
        /// </summary>
        private ControlType FindControlMetadata(string tagPrefix, string tagName)
        {
            // try to match the tag prefix and tag name
            var rules = configuration.Markup.Controls.Where(r => r.IsMatch(tagPrefix, tagName));
            foreach (var rule in rules)
            {
                // validate the rule
                rule.Validate();

                if (string.IsNullOrEmpty(rule.TagName))
                {
                    // find the code only control
                    var compiledControl = FindCompiledControl(tagName, rule.Namespace, rule.Assembly);
                    if (compiledControl != null)
                    {
                        return compiledControl;
                    }
                }
                else
                {
                    // find the markup control
                    return FindMarkupControl(rule.Src);
                }
            }

            throw new Exception(string.Format(Resources.Controls.ControlResolver_ControlNotFound, tagPrefix, tagName));
        }

        /// <summary>
        /// Finds the compiled control.
        /// </summary>
        private ControlType FindCompiledControl(string tagName, string namespaceName, string assemblyName)
        {
            var type = Type.GetType(namespaceName + "." + tagName + ", " + assemblyName, false);
            if (type == null)
            {
                // the control was not found
                return null;
            }

            return new ControlType(type);
        }

        /// <summary>
        /// Finds the markup control.
        /// </summary>
        private ControlType FindMarkupControl(string file)
        {
            var markupFile = markupFileLoader.GetMarkup(configuration, file);
            var controlBuilder = controlBuilderFactory.GetControlBuilder(markupFile);
            return new ControlType(controlBuilder.BuildControl().GetType(), controlBuilder.GetType());
        }

        /// <summary>
        /// Gets the control metadata.
        /// </summary>
        private ControlResolverMetadata BuildControlMetadata(Type controlType, Type controlBuilderType)
        {
            var attribute = controlType.GetCustomAttribute<ControlMarkupOptionsAttribute>();

            var metadata = new ControlResolverMetadata()
            {
                Name = controlType.Name,
                Namespace = controlType.Namespace,
                HasHtmlAttributesCollection = typeof(IControlWithHtmlAttributes).IsAssignableFrom(controlType),
                Type = controlType,
                ControlBuilderType = controlBuilderType,
                Properties = GetControlProperties(controlType),
                IsContentAllowed = attribute.AllowContent
            };
            return metadata;
        }

        /// <summary>
        /// Gets the control properties.
        /// </summary>
        private Dictionary<string, RedwoodProperty> GetControlProperties(Type controlType)
        {
            return RedwoodProperty.ResolveProperties(controlType).ToDictionary(p => p.Name, p => p);
        }
    }
}