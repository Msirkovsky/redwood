﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Redwood.VS2015Extension.RwHtmlEditorExtensions.Completions.RwHtml.Base
{
    public abstract class BindingValueCompletionProviderBase : RwHtmlCompletionProviderBase
    {
        public override TriggerPoint TriggerPoint
        {
            get { return TriggerPoint.BindingValue; }
        }
    }
}