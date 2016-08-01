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
		/// </summary>
		internal abstract void ValidateNewEntity();
	}
}
