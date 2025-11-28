using System;
using System.Collections.Generic;
using System.IO;

namespace TelegramPlugin.Services
{
    public static class MediaHelper
    {
        private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".webp", ".svg" };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".gif", ".mov", ".avi", ".mkv", ".webm", ".wmv" };

        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".aiff" }; // возможно добавлю, пока пусть будет.


        extension(string path)
        {
            public bool IsPhoto() =>
                !string.IsNullOrEmpty(path) && PhotoExtensions.Contains(Path.GetExtension(path));

            public bool IsVideo() =>
                !string.IsNullOrEmpty(path) && VideoExtensions.Contains(Path.GetExtension(path));

            public bool IsAudio() =>
                !string.IsNullOrEmpty(path) && AudioExtensions.Contains(Path.GetExtension(path)); //аналогично
        }
    }
}