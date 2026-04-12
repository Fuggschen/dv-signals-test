using Newtonsoft.Json;
using Signals.Common.Displays;
using Signals.Game.Controllers;
using System.Collections.Generic;
using System.IO;

namespace Signals.Game.Displays
{
    internal class SignalNameDisplay : InfoDisplay
    {
        private const string OverrideFileName = "signal_name_overrides.json";

        private static Dictionary<string, string> _nameOverrides = new Dictionary<string, string>();

        internal static void LoadOverrides(string modPath)
        {
            var filePath = Path.Combine(modPath, OverrideFileName);

            if (!File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (parsed != null)
            {
                _nameOverrides = parsed;
            }
        }

        public SignalNameDisplay(InfoDisplayDefinition definition, BasicSignalController controller) : base(definition, controller) { }

        public override void UpdateDisplay()
        {
            var name = Controller.Name;

            if (name != DisplayText)
            {
                if (_nameOverrides.TryGetValue(name, out var overrideName))
                {
                    name = overrideName;
                }

                DisplayText = name;
            }
        }
    }
}
