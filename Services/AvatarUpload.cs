using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace StudentOrganizationSystem.Services
{
    /// <summary>
    /// The rules for a profile-picture upload: which files are accepted and how a
    /// safe, unique stored name is produced. The user-supplied file name is never
    /// trusted, and only raster image extensions are allowed (no SVG, which can
    /// carry script). Pure, so it is unit-tested directly.
    /// </summary>
    public static class AvatarUpload
    {
        /// <summary>2 MB - generous for a profile photo.</summary>
        public const long MaxBytes = 2 * 1024 * 1024;

        public static readonly IReadOnlyList<string> AllowedExtensions =
            new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        /// <summary>
        /// True when the upload may be stored. <paramref name="error"/> always gets
        /// a message the caller can show when the answer is false.
        /// </summary>
        public static bool IsAllowed(IFormFile? file, out string error)
        {
            if (file == null || file.Length == 0)
            {
                error = "Please choose a picture to upload.";
                return false;
            }

            if (file.Length > MaxBytes)
            {
                error = "That picture is larger than 2 MB. Please choose a smaller one.";
                return false;
            }

            var extension = ExtensionOf(file.FileName);

            if (!AllowedExtensions.Contains(extension))
            {
                error = "Only JPG, PNG, GIF or WEBP pictures are allowed.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// A safe, unique stored name built from the user id, a random suffix and
        /// the validated extension. Any path in the original name is discarded, so
        /// the value can never escape the upload folder.
        /// </summary>
        public static string BuildFileName(int userId, string? originalName)
        {
            var extension = ExtensionOf(originalName);

            if (!AllowedExtensions.Contains(extension))
            {
                extension = ".png";
            }

            return $"{userId}_{Guid.NewGuid():N}{extension}";
        }

        private static string ExtensionOf(string? name)
        {
            return Path.GetExtension(name ?? string.Empty).ToLowerInvariant();
        }
    }
}
