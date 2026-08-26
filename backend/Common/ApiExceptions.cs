namespace IncidentsDsi.Api.Common;

public sealed class ValidationException(string message) : Exception(message);
