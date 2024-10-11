namespace Pulse.Configuration;

internal static class Constants {
	/// <summary>
	/// The value used to display and serialize empty strings
	/// </summary>
	public const string EmptyValue = "NaN";

	/// <summary>
	/// Checks if a string is <see cref="EmptyValue"/>
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static bool IsEmptyOrDefault(this string str) => string.Equals(str, EmptyValue, StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrWhiteSpace(str);
}