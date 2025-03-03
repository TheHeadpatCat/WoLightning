using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.WoL_Plugin.Util.UI
{
    public class InputSearch
    {
        String currentInput = string.Empty;
        List<String> KnownItems = new List<String>();

        public string Name { get; init; }
        public string Hint = string.Empty;

        public InputSearch(string Name,List<String> KnownItems)
        {
            this.Name = Name;
            this.KnownItems = KnownItems;
        }
        public InputSearch(string Name, List<Object> KnownItems)
        {
            this.Name = Name;
            foreach (Object item in KnownItems)
            {
                this.KnownItems.Add(item.ToString()!);
            }
        }

        public void Draw() //todo: this is garbage
        {
            ImGui.BeginGroup();
            
            ImGui.InputTextWithHint("##" + Name, Hint, ref currentInput, 32);
            Vector2 inputSize = ImGui.GetItemRectSize();
            if (currentInput.Length >= 2)
            {
                ImGui.OpenPopup(Name);
            }
            ImGui.EndGroup();

            if (ImGui.BeginPopup(Name,ImGuiWindowFlags.NoMove))
            {
                ImGui.Selectable("Test1");
                ImGui.Selectable("Test2");
                ImGui.EndPopup();
            }
        }

    }
}
