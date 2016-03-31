using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubtitleBroom
{
    class Config
    {
        private const string fileName = "SubtitleBroomData";

        public static string LastDirectory { get; set; }

        public static ICollection<string> IgnoredVideos { get; set; }

        public static void Load()
        {
            IgnoredVideos = new List<string>();

            if (!File.Exists(fileName))
                return;

            foreach (var line in File.ReadAllLines(fileName))
            {
                var pair = line.Split('=');
                switch (pair[0])
                {
                    case "LastDirectory":
                        LastDirectory = pair[1];
                        break;
                    case "IgnoredVideos":
                        if (!string.IsNullOrEmpty(pair[1]))
                            ((List<string>) IgnoredVideos).AddRange(pair[1].Split(';'));
                        break;
                }
            }
        }

        public static void Save()
        {
            var sb = new StringBuilder();
            sb.Append("LastDirectory=");
            sb.AppendLine(LastDirectory);
            sb.Append("IgnoredVideos=");
            sb.AppendLine(string.Join(";", IgnoredVideos));

            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
