namespace DocumentSubmission.Domain.Exceptions;

/// <summary>Base class for expected business outcomes; the API maps each subtype to an HTTP status.</summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>The submission (or a version of it) does not exist or is not visible to the caller. Maps to 404.</summary>
public sealed class NotFoundException(string message) : DomainException(message);

/// <summary>The caller is not the participant allowed to perform the action. Maps to 403.</summary>
public sealed class NotAllowedException(string message) : DomainException(message);

/// <summary>The input breaks a business rule (e.g. ineligible reviewer or recipient). Maps to 400.</summary>
public sealed class InvalidInputException(string message) : DomainException(message);

/// <summary>The action is not allowed in the submission's current state. Maps to 409.</summary>
public sealed class InvalidStateException(string message) : DomainException(message);
