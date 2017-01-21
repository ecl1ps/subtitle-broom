using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubtitleBroom
{
    class Groomer
    {
        private static readonly string[] videoExtensions = { ".avi", ".mp4", ".mkv" };
        private static readonly string subtitleExtensionsPattern = @"\.srt|\.sub";
        private static readonly string videoExtensionsPattern = @"\.avi|\.mp4|\.mkv";
        //private static readonly string[] langCodes = { "cs", "en", "cze", "eng", "ces" };
        private static readonly Dictionary<string, string> langCodeMappings = new Dictionary<string, string> { { "cs", "cs" }, { "en", "en" }, { "cze", "cs" }, { "eng", "en" }, { "ces", "cs" } };

        private static readonly Regex subtitlePattern = new Regex(subtitleExtensionsPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly string rootDirectory;

        public int SubtitlesTotal { get; private set; }
        public int SubtitlesWithoutLang { get; private set; }
        public int SubtitlesRenamed { get; set; }

        public List<FileInfo> SubtitlesNeedMoving { get; private set; }
        public List<FileInfo> SubtitlesWithoutVideo { get; private set; }
        public List<FileInfo> VideosWithoutSubtitle { get; private set; }

        public Groomer(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
            SubtitlesNeedMoving = new List<FileInfo>();
            SubtitlesWithoutVideo = new List<FileInfo>();
            VideosWithoutSubtitle = new List<FileInfo>();
        }

        public async Task MoveSubtitlesNextToVideoAsync()
        {
            await Task.Run(() =>
            {
                foreach (string file in GetFiles(rootDirectory, subtitleExtensionsPattern, SearchOption.AllDirectories))
                {
                    var fi = new FileInfo(file);
                    var subtitleNameWOExt = Path.GetFileNameWithoutExtension(fi.Name);
                    if (fi.Directory != null &&
                        fi.Directory.EnumerateFiles()
                        .Any(f => videoExtensions.Contains(f.Extension) &&
                            subtitleNameWOExt.StartsWith(
                                Path.GetFileNameWithoutExtension(f.Name),
                                StringComparison.OrdinalIgnoreCase)))
                        continue;

                    if (fi.Directory != null && fi.Directory.Parent != null &&
                        fi.Directory.Parent.EnumerateFiles()
                        .Any(f => videoExtensions.Contains(f.Extension) &&
                            subtitleNameWOExt.StartsWith(
                                Path.GetFileNameWithoutExtension(f.Name),
                                StringComparison.OrdinalIgnoreCase)))
                    {
                        var originalDir = fi.Directory;
                        fi.MoveTo(Path.Combine(fi.Directory.Parent.FullName, fi.Name));
                        if (originalDir != null && !originalDir.EnumerateFiles().Any())
                            Directory.Delete(originalDir.FullName);
                    }
                }
            });
        }

        public async Task RenameSubtitlesWithLangCodeAsync()
        {
            await Task.Run(() =>
            {
                SubtitlesRenamed = 0;
                foreach (string file in GetFiles(rootDirectory, subtitleExtensionsPattern, SearchOption.AllDirectories))
                {
                    var fi = new FileInfo(file);
                    var subtitleNameWOExt = Path.GetFileNameWithoutExtension(fi.Name);
                    string langCode = "cs";
                    bool needsRename = true;
                    foreach (var code in langCodeMappings.Keys)
                    {
                        if (subtitleNameWOExt.EndsWith(code, StringComparison.OrdinalIgnoreCase))
                        {
                            langCode = langCodeMappings[code];
                            if (langCode == code)
                            {
                                needsRename = false;
                                break;
                            }

                            subtitleNameWOExt = subtitleNameWOExt.Substring(0, subtitleNameWOExt.Length - code.Length).TrimEnd(' ', '-');
                            break;
                        }
                    }

                    if (!needsRename)
                        continue;

                    fi.MoveTo(string.Format("{3}\\{0}.{1}{2}", subtitleNameWOExt, langCode, fi.Extension, fi.Directory.FullName));
                    SubtitlesRenamed++;
                }
            });
        }

        public async Task CheckSubtitlesAsync()
        {
            await Task.Run(() =>
            {
                SubtitlesNeedMoving.Clear();
                SubtitlesWithoutVideo.Clear();
                SubtitlesTotal = 0;
                SubtitlesWithoutLang = 0;

                foreach (string file in GetFiles(rootDirectory, subtitleExtensionsPattern, SearchOption.AllDirectories))
                {
                    SubtitlesTotal++;

                    var fi = new FileInfo(file);


                    var subtitleNameWOExt = Path.GetFileNameWithoutExtension(fi.Name);
                    bool needsRename = true;
                    foreach (var code in langCodeMappings.Keys)
                    {
                        if (subtitleNameWOExt.EndsWith("." + code, StringComparison.OrdinalIgnoreCase))
                        {
                            if (langCodeMappings[code] == code)
                                needsRename = false;
                            break;
                        }
                    }

                    if (needsRename)
                        SubtitlesWithoutLang++;

                    // check if there is a video file next to subtitle file starting with the same name
                    if (fi.Directory != null && fi.Directory.EnumerateFiles()
                        .Any(f =>
                            videoExtensions.Contains(f.Extension) &&
                            fi.Name.StartsWith(Path.GetFileNameWithoutExtension(f.Name) + ".", StringComparison.Ordinal)))
                        continue;

                    // check if there is a video file in parent folder of the subtitle file starting with the same name
                    if (fi.Directory != null && fi.Directory.Parent != null && fi.Directory.Parent.EnumerateFiles()
                        .Any(f =>
                            videoExtensions.Contains(f.Extension) &&
                            fi.Name.StartsWith(Path.GetFileNameWithoutExtension(f.Name) + ".", StringComparison.OrdinalIgnoreCase)))
                    {
                        SubtitlesNeedMoving.Add(fi);
                        continue;
                    }

                    SubtitlesWithoutVideo.Add(fi);
                }

                foreach (string file in GetFiles(rootDirectory, videoExtensionsPattern, SearchOption.AllDirectories))
                {
                    var video = new FileInfo(file);
                    if (video.Directory != null &&
                        !video.Directory.EnumerateFiles().Any(subtitle =>
                            subtitlePattern.IsMatch(subtitle.Extension) &&
                            subtitle.Name.StartsWith(Path.GetFileNameWithoutExtension(video.Name) + ".", StringComparison.OrdinalIgnoreCase)))
                    {
                        VideosWithoutSubtitle.Add(video);
                    }
                }
            });
        }

        private static IEnumerable<string> GetFiles(string path, string searchPatternExpression = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);
            return Directory.EnumerateFiles(path, "*", searchOption)
                            .Where(file => reSearchPattern.IsMatch(Path.GetExtension(file)));
        }

        public static IEnumerable<string> GetAvailableVideosInDirectory(DirectoryInfo directory)
        {
            return GetFiles(directory.FullName, videoExtensionsPattern);
        }

        public static void RenameSubtitleToMatchVideo(FileInfo subtitle, string video)
        {
            subtitle.MoveTo(string.Format("{0}\\{1}.cs{2}", subtitle.Directory.FullName, Path.GetFileNameWithoutExtension(video), subtitle.Extension));
        }

        public static bool HasSubtitleLangCode(string name)
        {
            var nameWOExt = Path.GetFileNameWithoutExtension(name);
            return nameWOExt != null && (nameWOExt.EndsWith("cs", StringComparison.CurrentCultureIgnoreCase) || nameWOExt.EndsWith("en", StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool HasVideoSubtitle(string videoFile)
        {
            var video = new FileInfo(videoFile);
            return video.Directory != null && video.Directory.EnumerateFiles().Any(subtitle =>
                subtitlePattern.IsMatch(subtitle.Extension) &&
                subtitle.Name.StartsWith(Path.GetFileNameWithoutExtension(video.Name) + ".", StringComparison.OrdinalIgnoreCase));
        }
    }
}
