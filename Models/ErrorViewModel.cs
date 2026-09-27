using Microsoft.AspNetCore.Http;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// Everything the Error view needs to describe one failed request.
    /// Used by HomeController.Error (AUDIT.md P0.3).
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Correlation id, so a line in the log can be matched to what the user saw.
        /// </summary>
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);

        /// <summary>
        /// 500 when an exception was caught, 404 for an unknown address, and so on.
        /// </summary>
        public int StatusCode { get; set; } = StatusCodes.Status500InternalServerError;

        /// <summary>
        /// The address that failed. Only filled in when an exception was caught.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// The exception message - only filled in while running in Development.
        /// </summary>
        public string? Detail { get; set; }

        public bool IsNotFound => StatusCode == StatusCodes.Status404NotFound;

        public string Title => IsNotFound
            ? "Page not found"
            : "Something went wrong";

        public string Message => IsNotFound
            ? "The page you tried to open does not exist. It may have been moved, or the address may be typed incorrectly."
            : "The system could not finish that request. The problem has been written to the log - please try again, and tell your administrator if it keeps happening.";
    }
}
