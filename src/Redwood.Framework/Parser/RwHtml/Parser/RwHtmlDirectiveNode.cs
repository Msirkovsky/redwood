﻿using System;
using System.Collections.Generic;
using System.Linq;
using Redwood.Framework.Parser.RwHtml.Tokenizer;

namespace Redwood.Framework.Parser.RwHtml.Parser
{
    public class RwHtmlDirectiveNode : RwHtmlNode
    {

        public string Name { get; set; }

        public string Value { get; set; }
        
    }
}