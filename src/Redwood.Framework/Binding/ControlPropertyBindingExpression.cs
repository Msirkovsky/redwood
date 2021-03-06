﻿using System;
using System.Collections.Generic;
using System.Linq;
using Redwood.Framework.Controls;

namespace Redwood.Framework.Binding
{
    public class ControlPropertyBindingExpression : BindingExpression
    {

        public ControlPropertyBindingExpression()
        {
        }

        public ControlPropertyBindingExpression(string expression)
            : base(expression)
        {
        }



        public override object Evaluate(RedwoodBindableControl control, RedwoodProperty property)
        {
            // find the parent markup control and calculate number of DataContext changes
            int numberOfDataContextChanges;
            var current = control.GetClosestControlBindingTarget(out numberOfDataContextChanges);

            // get the property
            var sourceProperty = RedwoodProperty.ResolveProperty(current.GetType(), Expression);
            if (sourceProperty == null)
            {
                throw new Exception(string.Format("The markup control of type '{0}' does not have a property '{1}'!", current.GetType(), Expression));        // TODO: exception handling
            }

            // check whether the property contains binding
            if (current is RedwoodBindableControl)
            {
                var originalBinding = ((RedwoodBindableControl)current).GetBinding(sourceProperty);
                if (originalBinding != null && originalBinding.GetType() == typeof(ValueBindingExpression))
                {
                    // ValueBindingExpression must be modified to be evaluated against the original DataContext
                    return new ValueBindingExpression(string.Join(".", Enumerable.Range(0, numberOfDataContextChanges).Select(i => "_parent").Concat(new[] { originalBinding.Expression })));
                }
                else if (originalBinding != null)
                {
                    return originalBinding;
                }
            }

            // otherwise evaluate on server
            return current.GetValue(sourceProperty);
        }

        public override string TranslateToClientScript(RedwoodBindableControl control, RedwoodProperty property)
        {
            throw new InvalidOperationException("The {controlProperty: ...} binding cannot be translated to client script!");
        }
    }
}
