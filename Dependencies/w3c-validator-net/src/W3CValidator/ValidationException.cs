namespace W3CValidator;

/// <summary>
///   <para>Base exception that indicates an error in the process of document's validation.</para>
/// </summary>
public class ValidationException : Exception
{
  /// <summary>
  ///   <para>Initializes a new instance of the exception with a specified error message and a reference to the inner exception that is the cause of this exception.</para>
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="inner">The exception that is the cause of the current exception, or a <c>null</c> reference if no inner exception is specified.</param>
  public ValidationException(string message = null, Exception inner = null) : base(message, inner) {}
}