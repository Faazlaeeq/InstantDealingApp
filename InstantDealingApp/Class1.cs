using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace InstantDealingApp
{
    public static class FontHelper
    {
        private static FontFamily _urduFont;

        public static FontFamily GetUrduFont()
        {
            if (_urduFont == null)
            {
                try
                {
                    // Load font from embedded resource
                    // Replace "YourProjectName" with your actual project name
                    // Replace "JameelNooriNastaleeq.ttf" with your actual font filename
                    _urduFont = new FontFamily(new Uri("pack://application:,,,/"),
                        "./font/#Alvi Nastaleeq Regular");
                }
                catch
                {
                    // Fallback to system font if custom font fails to load
                    _urduFont = new FontFamily("Arial Unicode MS");
                }
            }
            return _urduFont;
        }
    }
}
