﻿using System;
using System.Collections.Generic;
using System.Linq;
using Redwood.Framework.Controls;
using Redwood.Framework.Controls.Infrastructure;
using Redwood.Framework.Hosting;

namespace Redwood.Framework.Runtime
{
    public interface IRedwoodViewBuilder
    {

        RedwoodView BuildView(RedwoodRequestContext context);
    
    }
}
