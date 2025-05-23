﻿using Microsoft.Xrm.Sdk;
using System;

namespace DG.XrmPluginCore
{
    public abstract class AbstractPlugin : IPlugin
    {
        /// <summary>
        /// Gets the name of the child class.
        /// </summary>
        /// <value>The name of the child class.</value>
        protected string ChildClassName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractPlugin"/> class.
        /// </summary>
        protected AbstractPlugin()
        {
            ChildClassName = GetType().ToString();
        }

        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
        /// The plug-in's Execute method should be written to be stateless as the constructor 
        /// is not called for every invocation of the plug-in. Also, multiple system threads 
        /// could execute the plug-in at the same time. All per invocation state information 
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        public abstract void Execute(IServiceProvider serviceProvider);
    }
}
