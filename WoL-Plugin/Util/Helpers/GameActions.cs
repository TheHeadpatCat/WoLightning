using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Action = Lumina.Excel.Sheets.Action;

namespace WoLightning.WoL_Plugin.Util.Helpers
{
    public class GameActions
    {
        private Plugin plugin;
        private IReadOnlyDictionary<uint, Action> Actions;

        public GameActions(Plugin plugin, IReadOnlyDictionary<uint, Action> Actions)
        {
            this.plugin = plugin;
            this.Actions = Actions;
        }

        public List<Action>? findActionsByName(String Name)
        {
            List<Action>? output = [];

            foreach (var (id, Action) in Actions)
            {
                if (output.Count >= 14) break;
                string ActionName = Action.Name.ToString().ToLower();
                if (ActionName.Equals(Name.ToLower()) && !output.Contains(Action)) output.Add(Action);
                if (ActionName.Contains(Name.ToLower()) && !output.Contains(Action)) output.Add(Action);
            }

            return output;
        }
        public List<Action>? findActions(List<ushort> ids)
        {
            List<Action>? output = [];

            foreach (var id in ids)
            {
                Action? s = getAction(id);
                if (s != null) output.Add((Action)s);
            }

            return output;
        }

        public Action? getAction(String Name)
        {
            Action? output = null;
            foreach (var (id, Action) in Actions)
            {
                string ActionName = Action.Name.ToString().ToLower();
                if (ActionName.Equals(Name.ToLower()))
                {
                    output = Action;
                    break;
                }
                if (output == null && ActionName.Contains(Name.ToLower())) output = Action;
            }
            return output;
        }
        public Action? getAction(uint Id)
        {
            Action? output = null;
            foreach (var (Eid, Action) in Actions)
            {
                if (Eid == Id)
                {
                    output = Action;
                    break;
                }

            }
            return output;
        }
    }
}
