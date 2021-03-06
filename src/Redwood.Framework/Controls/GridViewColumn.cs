﻿using Redwood.Framework.Binding;
using System;

namespace Redwood.Framework.Controls
{
    public abstract class GridViewColumn : RedwoodBindableControl 
    {

        [MarkupOptions(AllowBinding = false)]
        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }
        public static readonly RedwoodProperty HeaderTextProperty =
            RedwoodProperty.Register<string, GridViewColumn>(c => c.HeaderText);

        [MarkupOptions(AllowHardCodedValue = false)]
        public object ValueBinding
        {
            get { return GetValue(ValueBindingProperty); }    
            set { SetValue(ValueBindingProperty, value); }
        }
        public static readonly RedwoodProperty ValueBindingProperty =
            RedwoodProperty.Register<object, GridViewColumn>(c => c.ValueBinding);


        [MarkupOptions(AllowBinding = false)]
        public string SortExpression
        {
            get { return (string)GetValue(SortExpressionProperty); }
            set { SetValue(SortExpressionProperty, value); }
        }
        public static readonly RedwoodProperty SortExpressionProperty =
            RedwoodProperty.Register<string, GridViewColumn>(c => c.SortExpression);


        [MarkupOptions(AllowBinding = false)]
        public bool AllowSorting
        {
            get { return (bool)GetValue(AllowSortingProperty); }
            set { SetValue(AllowSortingProperty, value); }
        }
        public static readonly RedwoodProperty AllowSortingProperty =
            RedwoodProperty.Register<bool, GridViewColumn>(c => c.AllowSorting, false);


        public string CssClass
        {
            get { return (string)GetValue(CssClassProperty); }
            set { SetValue(CssClassProperty, value); }
        }
        public static readonly RedwoodProperty CssClassProperty =
            RedwoodProperty.Register<string, GridViewColumn>(c => c.CssClass);


        [MarkupOptions(AllowBinding = false)]
        public string Width
        {
            get { return (string)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }
        public static readonly RedwoodProperty WidthProperty =
            RedwoodProperty.Register<string, GridViewColumn>(c => c.Width);






        public abstract void CreateControls(RedwoodControl container);



        protected ValueBindingExpression CloneValueBinding()
        {
            var binding = GetValueBinding(ValueBindingProperty);
            if (binding == null)
            {
                ThrowValueBindingNotSet();
            }
            return (ValueBindingExpression)binding.Clone();
        }

        private void ThrowValueBindingNotSet()
        {
            throw new Exception(string.Format("The ValueBinding property is not set on the {0} control!", GetType()));
        }


        public virtual void CreateHeaderControls(GridView gridView, string sortCommandPath, HtmlGenericControl cell)
        {
            if (AllowSorting)
            {
                if (string.IsNullOrEmpty(sortCommandPath))
                {
                    throw new Exception("Cannot use column sorting where no sort command is specified. Either put IGridViewDataSet in the DataSource property of the GridView, or set the SortChanged command on the GridView to implement custom sorting logic!");
                }

                var sortExpression = GetSortExpression();

                // TODO: verify that sortExpression is a single property name

                var linkButton = new LinkButton() { Text = HeaderText };
                linkButton.SetBinding(ButtonBase.ClickProperty, new CommandBindingExpression(string.Format("{0} (\"{1}\")", sortCommandPath, sortExpression)));
                cell.Children.Add(linkButton);
            }
            else
            {
                var literal = new Literal(HeaderText);
                cell.Children.Add(literal);
            }
        }

        private string GetSortExpression()
        {
            string sortExpression = null;
            if (string.IsNullOrEmpty(SortExpression))
            {
                var valueBinding = GetValueBinding(ValueBindingProperty);
                if (valueBinding != null)
                {
                    sortExpression = valueBinding.Expression;
                }
                else
                {
                    ThrowValueBindingNotSet();
                }
            }
            else
            {
                sortExpression = SortExpression;
            }
            return sortExpression;
        }
    }

}