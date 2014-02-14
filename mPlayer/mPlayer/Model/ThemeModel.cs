using mPlayer.Properties;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Linq;

namespace mPlayer.Model
{
    public class ThemeModel : BindableObject
    {
        private string[] themeColors = new string[] { "#3b83e6", "#e93a95", "#5ea63a", "#a354ec", "#5a5ad8", "#e83030", "#f56119", "#cf41e0", "#1f1f1f" };
        public string[] ThemeColors
        {
            get { return themeColors; }
        }
    }
}
