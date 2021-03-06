﻿using System;
using System.Collections.Generic; 
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Redwood.Framework.Binding;
using Redwood.Framework.Controls;
using Redwood.Framework.Controls.Infrastructure;
using Redwood.Framework.Hosting;

namespace Redwood.Framework.Tests.Binding
{
    [TestClass]
    public class CommandResolverTests
    {

        [TestMethod]
        public void CommandResolver_Valid_SimpleTest()
        {
            var path = new[] { "A[0]" };
            var command = "Test(StringToPass, _parent.NumberToPass)";

            var testObject = new
            {
                A = new[]
                {
                    new TestA() { StringToPass = "test" }
                },
                NumberToPass = 16
            };
            var viewRoot = new RedwoodView() { DataContext = testObject };

            var placeholder = new HtmlGenericControl("div");
            placeholder.SetBinding(RedwoodBindableControl.DataContextProperty, new ValueBindingExpression(path[0]));
            viewRoot.Children.Add(placeholder);

            var button = new Button();
            button.SetBinding(ButtonBase.ClickProperty, new CommandBindingExpression(command));
            placeholder.Children.Add(button);

            var resolver = new CommandResolver();
            var context = new RedwoodRequestContext() { ViewModel = testObject };
            resolver.GetFunction(viewRoot, context, path, command).GetAction()();

            Assert.AreEqual(testObject.NumberToPass, testObject.A[0].ResultInt);
            Assert.AreEqual(testObject.A[0].ResultString, testObject.A[0].ResultString);
        }

        [TestMethod]
        public void CommandResolver_Valid_SimpleTest2()
        {
            var path = new[] { "A[0]", "StringToPass" };
            var command = "_parent.Test(_parent.StringToPass, _root.NumberToPass)";
            
            var testObject = new
            {
                A = new[]
                {
                    new TestA() { StringToPass = "test" }
                },
                NumberToPass = 16
            };
            var viewRoot = new RedwoodView() { DataContext = testObject };

            var placeholder = new HtmlGenericControl("div");
            placeholder.SetBinding(RedwoodBindableControl.DataContextProperty, new ValueBindingExpression(path[0]));
            viewRoot.Children.Add(placeholder);

            var placeholder2 = new HtmlGenericControl("div");
            placeholder2.SetBinding(RedwoodBindableControl.DataContextProperty, new ValueBindingExpression(path[1]));
            placeholder.Children.Add(placeholder2);

            var button = new Button();
            button.SetBinding(ButtonBase.ClickProperty, new CommandBindingExpression(command));
            placeholder2.Children.Add(button);

            var resolver = new CommandResolver();
            var context = new RedwoodRequestContext() { ViewModel = testObject };
            resolver.GetFunction(viewRoot, context, path, command).GetAction()();

            Assert.AreEqual(testObject.NumberToPass, testObject.A[0].ResultInt);
            Assert.AreEqual(testObject.A[0].ResultString, testObject.A[0].ResultString);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void CommandResolver_CannotCallSetter()
        {
            var testObject = new TestA()
            {
                StringToPass = "a"
            };
            var viewRoot = new RedwoodView() { DataContext = testObject };
            viewRoot.SetBinding(RedwoodProperty.Register<Action, RedwoodView>("Test"), new CommandBindingExpression("set_StringToPass(StringToPass)"));

            var path = new string[] { };
            var command = "set_StringToPass(StringToPass)";

            var resolver = new CommandResolver();
            var context = new RedwoodRequestContext() { ViewModel = testObject };
            resolver.GetFunction(viewRoot, context, path, command).GetAction()();
        }

        public class TestA
        {
            public string StringToPass { get; set; }

            public string ResultString { get; set; }

            public int ResultInt { get; set; }

            public void Test(string s, int i)
            {
                ResultString = s;
                ResultInt = i;
            }
        }
    }
}
