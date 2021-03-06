﻿using System;
using System.Collections.Generic;
using System.Linq;
using Redwood.Framework.Controls;

namespace Redwood.Framework.Binding
{
    public abstract class BindingExpression
    {

        /// <summary>
        /// Gets or sets the expression.
        /// </summary>
        public string Expression { get; set; }


        /// <summary>
        /// Evaluates the binding.
        /// </summary>
        public abstract object Evaluate(RedwoodBindableControl control, RedwoodProperty property);

        /// <summary>
        /// Translates the binding to client script.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="property"></param>
        public abstract string TranslateToClientScript(RedwoodBindableControl control, RedwoodProperty property);



        /// <summary>
        /// Initializes a new instance of the <see cref="BindingExpression"/> class.
        /// </summary>
        public BindingExpression(string expression) : this()
        {
            Expression = expression;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingExpression"/> class.
        /// </summary>
        public BindingExpression()
        {
        }



        public virtual BindingExpression Clone()
        {
            return (BindingExpression)Activator.CreateInstance(GetType(), Expression);
        }
    }
}
