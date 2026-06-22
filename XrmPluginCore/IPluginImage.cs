using System;
using Microsoft.Xrm.Sdk;

namespace XrmPluginCore;

/// <summary>
/// Non-generic base interface implemented by all generated plugin image wrappers
/// (PreImage/PostImage). Exposes the members that are always available on an image,
/// regardless of the concrete entity type.
/// <para>
/// Use this (or one of the more specific interfaces) as a parameter type on a service
/// method to share functionality across the per-registration concrete image types.
/// </para>
/// </summary>
public interface IPluginImage
{
	/// <summary>
	/// The unique identifier (primary key) of the record the image was captured for.
	/// </summary>
	Guid Id { get; }

	/// <summary>
	/// The logical name of the entity captured in the image.
	/// </summary>
	string LogicalName { get; }

	/// <summary>
	/// The underlying entity captured in the image.
	/// </summary>
	Entity Entity { get; }
}

/// <summary>
/// Plugin image wrapper with type-safe access to the underlying entity.
/// </summary>
/// <typeparam name="TEntity">The early-bound entity type captured in the image.</typeparam>
public interface IPluginImage<out TEntity> : IPluginImage where TEntity : Entity
{
	/// <summary>
	/// The strongly-typed entity captured in the image.
	/// </summary>
	new TEntity Entity { get; }
}

/// <summary>
/// Marker interface for a pre-image (the entity state before the operation).
/// </summary>
public interface IPluginPreImage : IPluginImage
{
}

/// <summary>
/// Type-safe pre-image (the entity state before the operation).
/// </summary>
/// <typeparam name="TEntity">The early-bound entity type captured in the image.</typeparam>
public interface IPluginPreImage<out TEntity> : IPluginImage<TEntity>, IPluginPreImage where TEntity : Entity
{
}

/// <summary>
/// Marker interface for a post-image (the entity state after the operation).
/// </summary>
public interface IPluginPostImage : IPluginImage
{
}

/// <summary>
/// Type-safe post-image (the entity state after the operation).
/// </summary>
/// <typeparam name="TEntity">The early-bound entity type captured in the image.</typeparam>
public interface IPluginPostImage<out TEntity> : IPluginImage<TEntity>, IPluginPostImage where TEntity : Entity
{
}
