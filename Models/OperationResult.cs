namespace ControlFinancieroProject.Models
{
    public class OperationResult
    {
        private OperationResult(bool isSuccess, bool isNotFound, string? errorField, string? errorMessage)
        {
            IsSuccess = isSuccess;
            IsNotFound = isNotFound;
            ErrorField = errorField;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public bool IsNotFound { get; }

        public string? ErrorField { get; }

        public string? ErrorMessage { get; }

        public static OperationResult Success() => new(true, false, null, null);

        public static OperationResult Failure(string errorMessage, string? errorField = null) =>
            new(false, false, errorField, errorMessage);

        public static OperationResult NotFound(string errorMessage = "El recurso solicitado no existe.") =>
            new(false, true, null, errorMessage);
    }
}
