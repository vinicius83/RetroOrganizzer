using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetroOrganizzer.Data
{
    public class PreferencesService
    {
        public void RootFolder(string folder)
        {
            Preferences.Default.Set("rootFolder", folder);
        }
    }
}
