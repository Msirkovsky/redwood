﻿using Redwood.Framework.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redwood.Framework.ResourceManagement.ClientGlobalize
{
    public class JQueryGlobalizeResourceRepository : IRedwoodResourceRepository
    {
        public ResourceBase FindResource(string name)
        {
            return new ScriptResource()
            {
                Url = string.Format("/{0}?{1}={2}", Constants.GlobalizeCultureUrlPath, Constants.GlobalizeCultureUrlIdParameter, name),
                Dependencies = new[] { Constants.GlobalizeResourceName },
                // TODO: cdn?
            };
        }
    }
}
