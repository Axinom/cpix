﻿namespace Axinom.Cpix
{
	/// <summary>
	/// Base class for all types of CPIX entities. For internal use only.
	/// </summary>
	/// <remarks>
	/// Acts as an internal interface to the entities, exposing internal general purpose entity features.
	/// </remarks>
	public abstract class Entity
	{
		/// <summary>
		/// Validates that the current state of the entity is valid for a newly created/added entity.
		/// The document is supplied to facilitate cross-checking with other parts of the document.
		/// </summary>
		internal abstract void ValidateNewEntity(CpixDocument document);

		/// <summary>
		/// Validates that the current state of the entity is valid for a loaded entity.
		/// The document is supplied to facilitate cross-checking with other parts of the document.
		/// </summary>
		internal abstract void ValidateLoadedEntity(CpixDocument document);
	}
}
