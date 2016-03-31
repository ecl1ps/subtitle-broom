using System.Windows;

namespace SubtitleBroom
{
    public partial class App : Application
    {
        public App()
        {
            Config.Load();
            
            Exit += (sender, args) => Config.Save();
        }
    }
}
