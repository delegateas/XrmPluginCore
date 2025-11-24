using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.Linq.Expressions;

#pragma warning disable CS0618 // Type or member is obsolete - we use AddImage internally

namespace XrmPluginCore.Plugins
{
    /// <summary>
    /// Helper class for retrieving plugin images and wrapping actions.
    /// </summary>
    internal static class PluginImageHelper
    {
        internal static Action<IExtendedServiceProvider> WrapAction<TService>(Action<TService> action)
        {
            return sp => action(sp.GetRequiredService<TService>());
        }

        internal static Action<IExtendedServiceProvider> WrapActionWithPreImage<TService, TPreImage>(
            Action<TService, TPreImage> action)
            where TPreImage : class
        {
            return sp => action(
                sp.GetRequiredService<TService>(),
                GetPreImage<TPreImage>(sp));
        }

        internal static Action<IExtendedServiceProvider> WrapActionWithPostImage<TService, TPostImage>(
            Action<TService, TPostImage> action)
            where TPostImage : class
        {
            return sp => action(
                sp.GetRequiredService<TService>(),
                GetPostImage<TPostImage>(sp));
        }

        internal static Action<IExtendedServiceProvider> WrapActionWithBothImages<TService, TPreImage, TPostImage>(
            Action<TService, TPreImage, TPostImage> action)
            where TPreImage : class
            where TPostImage : class
        {
            return sp => action(
                sp.GetRequiredService<TService>(),
                GetPreImage<TPreImage>(sp),
                GetPostImage<TPostImage>(sp));
        }

        private static T GetPreImage<T>(IExtendedServiceProvider sp) where T : class
        {
            var context = sp.GetService<IPluginExecutionContext>();
            var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();
            if (preImageEntity == null) return null;
            return (T)Activator.CreateInstance(typeof(T), preImageEntity);
        }

        private static T GetPostImage<T>(IExtendedServiceProvider sp) where T : class
        {
            var context = sp.GetService<IPluginExecutionContext>();
            var postImageEntity = context?.PostEntityImages?.Values?.FirstOrDefault();
            if (postImageEntity == null) return null;
            return (T)Activator.CreateInstance(typeof(T), postImageEntity);
        }
    }

    /// <summary>
    /// Base builder for plugin steps that may have images.
    /// Use WithPreImage/WithPostImage to add type-safe images, then call Execute to complete registration.
    /// </summary>
    public class PluginStepBuilder<TEntity, TService> where TEntity : Entity
    {
        private readonly PluginStepRegistration registration;

        internal PluginStepBuilder(
            PluginStepConfigBuilder<TEntity> configBuilder,
            PluginStepRegistration registration)
        {
			ConfigBuilder = configBuilder;
            this.registration = registration;
        }

		/// <summary>
		/// Gets the underlying config builder for additional configuration (filtered attributes, deployment, etc.)
		/// </summary>
		public PluginStepConfigBuilder<TEntity> ConfigBuilder { get; }

		/// <summary>
		/// Add filtered attributes to this step.
		/// </summary>
		public PluginStepBuilder<TEntity, TService> AddFilteredAttributes(params Expression<Func<TEntity, object>>[] attributes)
        {
			ConfigBuilder.AddFilteredAttributes(attributes);
            return this;
        }

        /// <summary>
        /// Add a PreImage to this step. Returns a builder that requires PreImage in Execute.
        /// </summary>
        public PluginStepBuilderWithPreImage<TEntity, TService> WithPreImage(params Expression<Func<TEntity, object>>[] attributes)
        {
			ConfigBuilder.AddImage(Enums.ImageType.PreImage, attributes);
            return new PluginStepBuilderWithPreImage<TEntity, TService>(ConfigBuilder, registration);
        }

        /// <summary>
        /// Add a PostImage to this step. Returns a builder that requires PostImage in Execute.
        /// </summary>
        public PluginStepBuilderWithPostImage<TEntity, TService> WithPostImage(params Expression<Func<TEntity, object>>[] attributes)
        {
			ConfigBuilder.AddImage(Enums.ImageType.PostImage, attributes);
            return new PluginStepBuilderWithPostImage<TEntity, TService>(ConfigBuilder, registration);
        }

        /// <summary>
        /// Complete registration with an action that receives only the service.
        /// </summary>
        public PluginStepConfigBuilder<TEntity> Execute(Action<TService> action)
        {
            registration.Action = PluginImageHelper.WrapAction(action);
            return ConfigBuilder;
        }
    }

    /// <summary>
    /// Builder for plugin steps with a PreImage. Execute requires accepting PreImage.
    /// </summary>
    public class PluginStepBuilderWithPreImage<TEntity, TService> where TEntity : Entity
    {
        private readonly PluginStepRegistration registration;

        internal PluginStepBuilderWithPreImage(
            PluginStepConfigBuilder<TEntity> configBuilder,
            PluginStepRegistration registration)
        {
			ConfigBuilder = configBuilder;
            this.registration = registration;
        }

		/// <summary>
		/// Gets the underlying config builder for additional configuration.
		/// </summary>
		public PluginStepConfigBuilder<TEntity> ConfigBuilder { get; }

		/// <summary>
		/// Add filtered attributes to this step.
		/// </summary>
		public PluginStepBuilderWithPreImage<TEntity, TService> AddFilteredAttributes(params Expression<Func<TEntity, object>>[] attributes)
        {
			ConfigBuilder.AddFilteredAttributes(attributes);
            return this;
        }

        /// <summary>
        /// Add a PostImage to this step. Returns a builder that requires both images in Execute.
        /// </summary>
        public PluginStepBuilderWithBothImages<TEntity, TService> WithPostImage(params Expression<Func<TEntity, object>>[] attributes)
        {
			ConfigBuilder.AddImage(Enums.ImageType.PostImage, attributes);
            return new PluginStepBuilderWithBothImages<TEntity, TService>(ConfigBuilder, registration);
        }

        /// <summary>
        /// Complete registration with an action that receives service and PreImage.
        /// </summary>
        /// <typeparam name="TPreImage">The PreImage wrapper type (generated by source generator)</typeparam>
        public PluginStepConfigBuilder<TEntity> Execute<TPreImage>(Action<TService, TPreImage> action)
            where TPreImage : class
        {
            registration.Action = PluginImageHelper.WrapActionWithPreImage(action);
            return ConfigBuilder;
        }
    }

    /// <summary>
    /// Builder for plugin steps with a PostImage. Execute requires accepting PostImage.
    /// </summary>
    public class PluginStepBuilderWithPostImage<TEntity, TService> where TEntity : Entity
    {
        private readonly PluginStepRegistration registration;

        internal PluginStepBuilderWithPostImage(
            PluginStepConfigBuilder<TEntity> configBuilder,
            PluginStepRegistration registration)
        {
			ConfigBuilder = configBuilder;
            this.registration = registration;
        }

		/// <summary>
		/// Gets the underlying config builder for additional configuration.
		/// </summary>
		public PluginStepConfigBuilder<TEntity> ConfigBuilder { get; }

		/// <summary>
		/// Add filtered attributes to this step.
		/// </summary>
		public PluginStepBuilderWithPostImage<TEntity, TService> AddFilteredAttributes(params Expression<Func<TEntity, object>>[] attributes)
        {
			ConfigBuilder.AddFilteredAttributes(attributes);
            return this;
        }

        /// <summary>
        /// Add a PreImage to this step. Returns a builder that requires both images in Execute.
        /// </summary>
        public PluginStepBuilderWithBothImages<TEntity, TService> WithPreImage(params Expression<Func<TEntity, object>>[] attributes)
        {
			ConfigBuilder.AddImage(Enums.ImageType.PreImage, attributes);
            return new PluginStepBuilderWithBothImages<TEntity, TService>(ConfigBuilder, registration);
        }

        /// <summary>
        /// Complete registration with an action that receives service and PostImage.
        /// </summary>
        /// <typeparam name="TPostImage">The PostImage wrapper type (generated by source generator)</typeparam>
        public PluginStepConfigBuilder<TEntity> Execute<TPostImage>(Action<TService, TPostImage> action)
            where TPostImage : class
        {
            registration.Action = PluginImageHelper.WrapActionWithPostImage(action);
            return ConfigBuilder;
        }
    }

    /// <summary>
    /// Builder for plugin steps with both PreImage and PostImage. Execute requires accepting both.
    /// </summary>
    public class PluginStepBuilderWithBothImages<TEntity, TService> where TEntity : Entity
    {
        private readonly PluginStepRegistration registration;

        internal PluginStepBuilderWithBothImages(
            PluginStepConfigBuilder<TEntity> configBuilder,
            PluginStepRegistration registration)
        {
			ConfigBuilder = configBuilder;
            this.registration = registration;
        }

		/// <summary>
		/// Gets the underlying config builder for additional configuration.
		/// </summary>
		public PluginStepConfigBuilder<TEntity> ConfigBuilder { get; }

		/// <summary>
		/// Add filtered attributes to this step.
		/// </summary>
		public PluginStepBuilderWithBothImages<TEntity, TService> AddFilteredAttributes(params Expression<Func<TEntity, object>>[] attributes)
        {
			ConfigBuilder.AddFilteredAttributes(attributes);
            return this;
        }

        /// <summary>
        /// Complete registration with an action that receives service, PreImage, and PostImage.
        /// </summary>
        /// <typeparam name="TPreImage">The PreImage wrapper type (generated by source generator)</typeparam>
        /// <typeparam name="TPostImage">The PostImage wrapper type (generated by source generator)</typeparam>
        public PluginStepConfigBuilder<TEntity> Execute<TPreImage, TPostImage>(Action<TService, TPreImage, TPostImage> action)
            where TPreImage : class
            where TPostImage : class
        {
            registration.Action = PluginImageHelper.WrapActionWithBothImages(action);
            return ConfigBuilder;
        }
    }
}
