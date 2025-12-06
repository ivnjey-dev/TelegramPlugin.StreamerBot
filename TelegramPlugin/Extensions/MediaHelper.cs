using System;
using System.Collections.Generic;
using System.IO;

namespace TelegramPlugin.Services
{
    internal static class MediaHelper
    {
        private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".webp", ".svg" };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".gif", ".mov", ".avi", ".mkv", ".webm", ".wmv" };

        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".aiff" }; // возможно добавлю, пока пусть будет.


        public static bool IsPhoto(this string path) =>
            !string.IsNullOrEmpty(path) && PhotoExtensions.Contains(Path.GetExtension(path));

        public static bool IsVideo(this string path) =>
            !string.IsNullOrEmpty(path) && VideoExtensions.Contains(Path.GetExtension(path));

        public static bool IsAudio(this string path) =>
            !string.IsNullOrEmpty(path) && AudioExtensions.Contains(Path.GetExtension(path)); //аналогично
        
        public static bool IsUrl(this string path, out Uri? uri)
        {
            return Uri.TryCreate(path, UriKind.Absolute, out uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                   || !Path.IsPathRooted(path);
        }
    }
}