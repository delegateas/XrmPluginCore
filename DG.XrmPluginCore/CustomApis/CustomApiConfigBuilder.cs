using DG.XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using DG.XrmPluginCore.Models.CustomApi;

namespace DG.XrmPluginCore.CustomApis
{
    public class CustomApiConfigBuilder
    {
        private readonly Collection<RequestParameter> RequestParameters = new Collection<RequestParameter>();
        private readonly Collection<ResponseProperty> ResponseProperties = new Collection<ResponseProperty>();

        private Config Config { get; }

        public CustomApiConfigBuilder(string name)
        {
            Config = new Config()
            {
                Name = name,
                DisplayName = name,
                UniqueName = name,
                IsFunction = false,
                EnabledForWorkflow = false,
                AllowedCustomProcessingStepType = 0, // None
                BindingType = 0, // Global
                BoundEntityLogicalName = "",
                PluginType = null,
                IsCustomizable = false,
                IsPrivate = false,
                ExecutePrivilegeName = null, // TODO
                Description = $"CustomAPI Definition for {name}",
                OwnerId = "", // TODO
                OwnerType = "" // TODO
            };
        }

        public Config Build()
        {
            return new Config
            {
                UniqueName = Config.UniqueName,
                Name = Config.Name,
                DisplayName = Config.DisplayName,
                IsFunction = Config.IsFunction,
                EnabledForWorkflow = Config.EnabledForWorkflow,
                AllowedCustomProcessingStepType = Config.AllowedCustomProcessingStepType,
                BindingType = Config.BindingType,
                BoundEntityLogicalName = Config.BoundEntityLogicalName,
                PluginType = Config.PluginType,
                OwnerId = Config.OwnerId,
                OwnerType = Config.OwnerType,
                IsCustomizable = Config.IsCustomizable,
                IsPrivate = Config.IsPrivate,
                ExecutePrivilegeName = Config.ExecutePrivilegeName,
                Description = Config.Description
            };
        }

        public CustomApiConfigBuilder AllowCustomProcessingStep(AllowedCustomProcessingStepType type)
        {
            Config.AllowedCustomProcessingStepType = type;
            return this;
        }

        public CustomApiConfigBuilder Bind<T>(BindingType bindingType) where T : Entity
        {
            Config.BindingType = bindingType;
            Config.BoundEntityLogicalName = Activator.CreateInstance<T>().LogicalName;
            return this;
        }

        public CustomApiConfigBuilder MakeFunction()
        {
            Config.IsFunction = true;
            return this;
        }
        public CustomApiConfigBuilder MakePrivate()
        {
            Config.IsPrivate = true;
            return this;
        }

        public CustomApiConfigBuilder EnableForWorkFlow()
        {
            Config.EnabledForWorkflow = true;
            return this;
        }

        public CustomApiConfigBuilder EnableCustomization()
        {
            Config.IsCustomizable = true;
            return this;
        }

        public CustomApiConfigBuilder SetDescription(string description)
        {
            Config.Description = description;
            return this;
        }

        public CustomApiConfigBuilder AddRequestParameter(string uniqueName, CustomApiParameterType type,
            string displayName = null,
            string description = null,
            bool isCustomizable = false,
            bool isOptional = false,
            string logicalEntityName = null)
        {
            var parameterName = $"{Config.Name}-In-{uniqueName}";

            RequestParameters.Add(new RequestParameter(parameterName, uniqueName, displayName ?? parameterName, description, isCustomizable, logicalEntityName, type, isOptional));
            return this;
        }

        public CustomApiConfigBuilder AddRequestParameter(RequestParameter reqParam)
        {
            reqParam.Name = $"{Config.Name}-In-{reqParam.UniqueName}";
            reqParam.DisplayName = reqParam.DisplayName ?? reqParam.Name;

            RequestParameters.Add(reqParam);
            return this;
        }

        public CustomApiConfigBuilder AddResponseProperty(string uniqueName, CustomApiParameterType type,
            string displayName = null,
            string description = null,
            bool isCustomizable = false,
            string logicalEntityName = null)
        {
            var parameterName = $"{Config.Name}-Out-{uniqueName}";

            ResponseProperties.Add(new ResponseProperty(parameterName, uniqueName, displayName ?? parameterName, description, isCustomizable, logicalEntityName, type));
            return this;
        }

        public CustomApiConfigBuilder AddResponseProperty(ResponseProperty respProperty)
        {
            respProperty.Name = $"{Config.Name}-Out-{respProperty.UniqueName}";
            respProperty.DisplayName = respProperty.DisplayName ?? respProperty.Name;

            ResponseProperties.Add(respProperty);
            return this;
        }

        public IEnumerable<RequestParameter> GetRequestParameters()
        {
            foreach (var requestParameter in RequestParameters)
            {
                yield return new RequestParameter(
                    requestParameter.Name,
                    requestParameter.UniqueName,
                    requestParameter.DisplayName,
                    requestParameter.Description,
                    requestParameter.IsCustomizable,
                    requestParameter.LogicalEntityName,
                    requestParameter.Type,
                    requestParameter.IsOptional
                );
            }
        }

        public IEnumerable<ResponseProperty> GetResponseProperties()
        {
            foreach (var responseProperty in ResponseProperties)
            {
                yield return new ResponseProperty(
                    responseProperty.Name,
                    responseProperty.UniqueName,
                    responseProperty.DisplayName,
                    responseProperty.Description,
                    responseProperty.IsCustomizable,
                    responseProperty.LogicalEntityName,
                    responseProperty.Type);
            }
        }
    }
}