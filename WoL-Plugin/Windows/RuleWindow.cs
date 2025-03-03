using Dalamud.Interface.Windowing;
using System;
using WoLightning.WoL_Plugin.Game.Rules;

namespace WoLightning.Windows;

public class RuleWindow : Window, IDisposable
{
    readonly Plugin Plugin;
    BaseRule? currentRule = null;

    public RuleWindow(Plugin plugin)
        : base("RuleWindow")
    {
        Plugin = plugin;
    }

    public void setCurrentRule(BaseRule? rule)
    {
        currentRule = rule;
    }
    public void Dispose() { }
    public override async void Draw()
    {
        if (this.IsOpen && !Plugin.ConfigWindow.IsOpen) this.Toggle();
        
        if (currentRule != null) currentRule.DrawRuleWindow();
        else if (this.IsOpen) this.Toggle();
    }
}
