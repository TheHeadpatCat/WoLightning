using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Util.UI_Elements
{
    public class WordListBox
    {
        public string Name;
        public Plugin Plugin;
        public List<SpecificWord> Words;
        public bool HasPerWordOptions = true;
        ShockOptionsEditor optionsEditor;

        ShockOptions currentOptions;
        string Input = string.Empty;
        int Index = -1;
        String SelectedWord = string.Empty;
        int[] Settings = new int[3];

        bool Punctuation = false;
        bool ProperCase = false;

        public WordListBox(string name, Plugin plugin, List<SpecificWord> words)
        {
            Name = name;
            Words = words;
            Plugin = plugin;
            currentOptions = new();
            optionsEditor = new(Name, plugin, currentOptions);
            optionsEditor.AutoSave = false;
            optionsEditor.HasCooldown = false;
        }

        public void Draw(ref bool changed)
        {
            if (Words == null) return;

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 230);
            if (ImGui.InputTextWithHint($"##{Name}Input", "Click on a entry to edit it.", ref Input, 48))
            {
                if (Index != -1) // Get rid of the old settings, otherwise we build connections between two items
                {
                    int[] copyArray = new int[3];
                    Settings.CopyTo(copyArray, 0);
                    Settings = copyArray;
                }
                Index = -1;
            }

            ImGui.Checkbox($"Punctuation##{Name}Punctuation", ref Punctuation);
            HoverText.ShowHint("If this is enabled, the Word has to be properly written out on its own." +
                "\nIf the word is \"Master\" then writing \"i like my master\" is accepted, while \"ilikemymaster\" isn't.");

            ImGui.SameLine();

            ImGui.Checkbox($"Proper Case##{Name}ProperCase", ref ProperCase);
            HoverText.ShowHint("If this is enabled, the Word has to properly match the Case." +
                "\nIf the word is \"Collar\" then writing \"i have a Collar\" is accepted, while \"i have a collar\" isn't.");

            if (HasPerWordOptions)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                optionsEditor.Draw();
            }

            ImGui.Separator();

            ImGui.Spacing();

            if (Input.Length == 0) ImGui.BeginDisabled();
            if (ImGui.Button($"Add / Edit##{Name}Add", new Vector2(ImGui.GetWindowWidth() / 2 - 15, 35)))
            {
                SpecificWord? target = null;
                foreach (SpecificWord item in Words)
                {
                    if (item.Word.ToLower() == Input.ToLower())
                    {
                        target = item;
                        break;
                    }
                }
                if (target != null)
                {
                    Words.Remove(target);
                }

                target = new SpecificWord(Input);
                target.NeedsPunctuation = Punctuation;
                target.NeedsProperCase = ProperCase;
                target.ShockOptions = optionsEditor.Options;
                Words.Add(target);

                Index = -1;
                Input = new String("");
                Settings = new int[3];
                SelectedWord = new String("");
                currentOptions = new(target.ShockOptions.toSimpleArray());
                optionsEditor.Options = currentOptions;

                changed = true;
            }
            if (Input.Length == 0) ImGui.EndDisabled();
            ImGui.SameLine();

            if (Index == -1) ImGui.BeginDisabled();
            if (ImGui.Button($"Remove##{Name}Remove", new Vector2(ImGui.GetWindowWidth() / 2 - 15, 35)))
            {
                SpecificWord? target = null;
                foreach (SpecificWord item in Words)
                {
                    if (item.Word.ToLower() == Input.ToLower())
                    {
                        target = item;
                        break;
                    }
                }
                if (target != null)
                {
                    Words.Remove(target);
                    currentOptions = new(target.ShockOptions.toSimpleArray());
                }
                else
                {
                    currentOptions = new();
                }
                Index = -1;
                Input = new String("");
                Settings = new int[3];
                SelectedWord = new String("");
                optionsEditor.Options = currentOptions;

                changed = true;
            }
            if (Index == -1) ImGui.EndDisabled();

            ImGui.Spacing();

            if (ImGui.BeginListBox($"##{Name}ListBox", new Vector2(ImGui.GetWindowWidth() - 15, 340)))
            {
                int index = 0;
                foreach (SpecificWord item in Words)
                {
                    string mode = new String("");
                    string durS = new String("");
                    bool is_Selected = (Index == index);

                    string selectableText = $"[{item.Word}] - Punctuation: {item.NeedsPunctuation} - Proper Case: {item.NeedsProperCase}";
                    if (HasPerWordOptions) selectableText += $"\nShockers: {item.ShockOptions.getShockerCount()} - Mode: {item.ShockOptions.OpMode} - Intensity: {item.ShockOptions.Intensity} - Duration: {item.ShockOptions.durationString()} - Warning: {item.ShockOptions.WarningMode}";

                    if (ImGui.Selectable(selectableText, ref is_Selected))
                    {
                        SelectedWord = item.Word;
                        Index = index;
                        Input = item.Word;
                        Punctuation = item.NeedsPunctuation;
                        ProperCase = item.NeedsProperCase;
                        currentOptions = new(item.ShockOptions.toSimpleArray());
                        optionsEditor.Options = currentOptions;
                    }
                    ImGui.Separator();
                    index++;
                }
                ImGui.EndListBox();
            }
        }
    }

}

