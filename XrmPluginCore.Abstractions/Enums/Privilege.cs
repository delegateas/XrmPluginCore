namespace XrmPluginCore.Enums
{
	/// <summary>
	/// Standard Dataverse table (entity) privileges.<br/>
	/// Each value maps to a privilege whose name follows the convention
	/// <c>prv{Privilege}{EntityLogicalName}</c>, e.g. <see cref="Read"/> on the
	/// <c>account</c> table resolves to <c>prvReadaccount</c>.
	/// </summary>
	public enum Privilege
	{
		/// <summary>
		/// Permission to create a record (<c>prvCreate...</c>).
		/// </summary>
		Create = 0,

		/// <summary>
		/// Permission to read a record (<c>prvRead...</c>).
		/// </summary>
		Read = 1,

		/// <summary>
		/// Permission to update a record (<c>prvWrite...</c>). This is the "Update" of CRUD;
		/// Dataverse names the update privilege "Write".
		/// </summary>
		Write = 2,

		/// <summary>
		/// Permission to delete a record (<c>prvDelete...</c>).
		/// </summary>
		Delete = 3,

		/// <summary>
		/// Permission to associate a record with this table from another record (<c>prvAppend...</c>).
		/// </summary>
		Append = 4,

		/// <summary>
		/// Permission to associate another record with a record of this table (<c>prvAppendTo...</c>).
		/// </summary>
		AppendTo = 5,

		/// <summary>
		/// Permission to assign a record to another user (<c>prvAssign...</c>).
		/// </summary>
		Assign = 6,

		/// <summary>
		/// Permission to share a record with another user or team (<c>prvShare...</c>).
		/// </summary>
		Share = 7,
	}
}
