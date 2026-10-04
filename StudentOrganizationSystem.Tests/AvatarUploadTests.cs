using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using StudentOrganizationSystem.Services;
using Xunit;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// The profile-picture upload rules (module 7). Pure, so no web host is needed -
/// a <see cref="FormFile"/> stands in for the posted file.
/// </summary>
public class AvatarUploadTests
{
    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.JPEG", true)]
    [InlineData("photo.png", true)]
    [InlineData("photo.gif", true)]
    [InlineData("photo.webp", true)]
    [InlineData("photo.svg", false)]
    [InlineData("photo.exe", false)]
    [InlineData("photo", false)]
    [InlineData("", false)]
    public void AvatarUpload_only_accepts_image_extensions(string name, bool expected)
    {
        var allowed = AvatarUpload.IsAllowed(File(name, 100), out _);

        Assert.Equal(expected, allowed);
    }

    [Fact]
    public void AvatarUpload_rejects_missing_empty_and_oversized_files()
    {
        Assert.False(AvatarUpload.IsAllowed(null, out var missing));
        Assert.False(string.IsNullOrEmpty(missing));

        Assert.False(AvatarUpload.IsAllowed(File("photo.png", 0), out var empty));
        Assert.False(string.IsNullOrEmpty(empty));

        Assert.False(AvatarUpload.IsAllowed(
            File("photo.png", AvatarUpload.MaxBytes + 1), out var big));
        Assert.False(string.IsNullOrEmpty(big));

        // Exactly at the limit is fine.
        Assert.True(AvatarUpload.IsAllowed(
            File("photo.png", AvatarUpload.MaxBytes), out var ok));
        Assert.True(string.IsNullOrEmpty(ok));
    }

    [Fact]
    public void AvatarUpload_builds_a_safe_unique_name()
    {
        var name = AvatarUpload.BuildFileName(7, @"..\..\evil.PNG");

        Assert.StartsWith("7_", name);
        Assert.EndsWith(".png", name);
        Assert.DoesNotContain("evil", name);
        Assert.DoesNotContain("..", name);

        // A disallowed extension falls back to .png, and two calls never collide.
        Assert.EndsWith(".png", AvatarUpload.BuildFileName(3, "script.exe"));
        Assert.NotEqual(name, AvatarUpload.BuildFileName(7, "evil.png"));
    }

    private static IFormFile File(string name, long length)
    {
        var stream = new MemoryStream(new byte[Math.Min(length, 16)]);

        return new FormFile(stream, 0, length, "avatar", name);
    }
}
