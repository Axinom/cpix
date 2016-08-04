namespace Axinom.Cpix
{
	/// <summary>
	/// Entity base class for internal use only.
	/// </summary>
	/// <remarks>
	/// Acts as an internal interface to the entities, exposing general purpose entity features but only to this library.
	/// </remarks>
	public abstract class Entity
	{
		/// <summary>
		/// Validates that the current state of the entity is valid for a newly created/added entity.
		/// The document is supplied to facilitate cross-checking with other parts of the document.
		/// </summary>
		internal abstract void ValidateNewEntity(CpixDocument document);

		/// <summary>
		/// Validates that the current state of the entity is valid for a loaded existing entity.
		/// The document is supplied to facilitate cross-checking with other parts of the document.
		/// </summary>
		internal abstract void ValidateExistingEntity(CpixDocument document);
	}
}
