using DG.XrmPluginCore.Abstractions.Enums;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace DG.XrmPluginCore.CustomApis
{
    public class CustomAPIConfig : ICustomAPIConfig
    {
        public AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; private set; }
        public BindingType BindingType { get; private set; }
        public string BoundEntityLogicalName { get; private set; }
        public string Description { get; private set; }
        public string DisplayName { get; private set; }
        public string ExecutePrivilegeName { get; private set; }
        public bool IsCustomizable { get; private set; }
        public bool IsFunction { get; private set; }
        public bool IsPrivate { get; private set; }
        public string Name { get; private set; }
        public string UniqueName { get; private set; }
        public string PluginType { get; private set; }
        public bool EnabledForWorkflow { get; private set; }

        public Collection<RequestParameterConfig> _RequestParameters = new Collection<RequestParameterConfig>();
        public Collection<ResponsePropertyConfig> _ResponseProperties = new Collection<ResponsePropertyConfig>();

        public CustomAPIConfig(string name)
        {
            Name = name;
            DisplayName = name;
            UniqueName = name;
            IsFunction = false;
            EnabledForWorkflow = false;
            AllowedCustomProcessingStepType = 0; // None
            BindingType = 0; // Global
            BoundEntityLogicalName = "";

            PluginType = null;
            IsCustomizable = false;
            IsPrivate = false;
            ExecutePrivilegeName = null; // TODO
            Description = $"CustomAPI Definition for {name}";
        }

        public CustomAPIConfig AllowCustomProcessingStep(AllowedCustomProcessingStepType type)
        {
            AllowedCustomProcessingStepType = type;
            return this;
        }

        public CustomAPIConfig Bind<T>(BindingType bindingType) where T : Entity
        {
            BindingType = bindingType;
            BoundEntityLogicalName = Activator.CreateInstance<T>().LogicalName;
            return this;
        }

        public CustomAPIConfig MakeFunction()
        {
            IsFunction = true;
            return this;
        }
        public CustomAPIConfig MakePrivate()
        {
            IsPrivate = true;
            return this;
        }

        public CustomAPIConfig EnableForWorkFlow()
        {
            EnabledForWorkflow = true;
            return this;
        }

        public CustomAPIConfig EnableCustomization()
        {
            IsCustomizable = true;
            return this;
        }

        public CustomAPIConfig SetDescription(string description)
        {
            Description = description;
            return this;
        }

        public CustomAPIConfig AddRequestParameter(string name, CustomApiParameterType type,
            string displayName = null,
            string description = null,
            bool isCustomizable = false,
            bool isOptional = false,
            string logicalEntityName = null)
        {
            _RequestParameters.Add(new RequestParameterConfig(Name, name, type, displayName, description, isCustomizable, isOptional, logicalEntityName));
            return this;
        }

        public CustomAPIConfig AddRequestParameter(RequestParameterConfig reqParam)
        {
            reqParam.SetNameFromAPI(Name);
            _RequestParameters.Add(reqParam);
            return this;
        }

        public CustomAPIConfig AddResponseProperty(string uniqueName, CustomApiParameterType type,
            string displayName = null,
            string description = null,
            bool isCustomizable = false,
            string logicalEntityName = null)
        {
            _ResponseProperties.Add(new ResponsePropertyConfig(Name, uniqueName, type, displayName, description, isCustomizable, logicalEntityName));
            return this;
        }

        public CustomAPIConfig AddResponseProperty(ResponsePropertyConfig respProperty)
        {
            respProperty.SetNameFromAPI(Name);
            _ResponseProperties.Add(respProperty);
            return this;
        }

        public IEnumerable<RequestParameterConfig> GetRequestParameters()
        {
            foreach (var requestParameter in _RequestParameters)
            {
                yield return new RequestParameterConfig
                {
                    Name = requestParameter.Name,
                    UniqueName = requestParameter.UniqueName,
                    Type = requestParameter.Type,
                    DisplayName = requestParameter.DisplayName,
                    Description = requestParameter.Description,
                    IsCustomizable = requestParameter.IsCustomizable,
                    IsOptional = requestParameter.IsOptional,
                    LogicalEntityName = requestParameter.LogicalEntityName
                };
            }
        }

        public IEnumerable<ResponsePropertyConfig> GetResponseProperties()
        {
            foreach (var responseProperty in _ResponseProperties)
            {
                yield return new ResponsePropertyConfig
                {
                    Name = responseProperty.Name,
                    UniqueName = responseProperty.UniqueName,
                    Type = responseProperty.Type,
                    DisplayName = responseProperty.DisplayName,
                    Description = responseProperty.Description,
                    IsCustomizable = responseProperty.IsCustomizable,
                    LogicalEntityName = responseProperty.LogicalEntityName
                };
            }
        }
    }
}