// Copyright © 2024 ema
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.HtmlViewer;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace QuickLook.Plugin.MarkmapViewer;

public class Plugin : IViewer
{
    private WebpagePanel? _panel;

    public int Priority => 1;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && new[] { ".markmap", ".mm", ".mm.md", ".mm.mdown", ".mm.rmd", ".mm.markdown" }.Any(path.ToLower().EndsWith);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size(1000, 720);
    }

    public void View(string path, ContextObject context)
    {
        _panel = new WebpagePanel();

        if (OSThemeHelper.AppsUseDarkTheme())
        {
            // Invoke using reflection: WebView2.CreationProperties.AdditionalBrowserArguments
            // This approach allows the library to avoid direct dependency on WebView2
            if (typeof(WebpagePanel).GetField("_webView", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo fieldInfo)
            {
                object? webView2 = fieldInfo.GetValue(_panel);

                if (webView2?.GetType().GetProperty("CreationProperties", BindingFlags.Public | BindingFlags.Instance) is PropertyInfo creationPropertiesProperty)
                {
                    object? creationProperties = creationPropertiesProperty.GetValue(webView2);

                    if (creationProperties?.GetType().GetProperty("AdditionalBrowserArguments", BindingFlags.Public | BindingFlags.Instance) is PropertyInfo additionalBrowserArgumentsProperty)
                    {
                        string additionalBrowserArguments = (additionalBrowserArgumentsProperty.GetValue(creationProperties) as string) ?? string.Empty;
                        additionalBrowserArgumentsProperty.SetValue(creationProperties, additionalBrowserArguments + " --enable-features=WebContentsForceDark");
                    }
                }
            }
        }

        context.ViewerContent = _panel;
        context.Title = Path.GetFileName(path);

        _panel.NavigateToHtml(GenerateMarkdownHtml(path));
        _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _panel?.Dispose();
        _panel = null!;
    }

    private string GenerateMarkdownHtml(string path)
    {
        string html = null!;

        if (Tools.MarkmapCommandExists())
        {
            string file = Directory.Exists(@".\QuickLook.Plugin\QuickLook.Plugin.MarkmapViewer")
                            ? Path.GetFullPath(@".\QuickLook.Plugin\QuickLook.Plugin.MarkmapViewer\markmap.html")
                            : Path.Combine(SettingHelper.LocalDataPath, @"QuickLook.Plugin\QuickLook.Plugin.MarkmapViewer\markmap.html");

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            using Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c markmap.cmd --offline --no-open --output \"{file}\" \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            if (File.Exists(file))
            {
                html = File.ReadAllText(file);
                File.Delete(file);
            }
        }

        if (string.IsNullOrEmpty(html))
        {
            bool isForceDarkMode = OSThemeHelper.AppsUseDarkTheme();

            html =
                $$"""
                <html>
                markmap-cli not installed.<br>
                <br>
                Installation<br>
                yarn global add markmap-cli<br>
                or<br>
                npm install -g markmap-cli<br>
                </html>
                <style>
                body {
                    background: {{(isForceDarkMode ? "#1e1e1e" : "#ffffff")}};
                    color: {{(isForceDarkMode ? "#d4d4d4" : "#000000")}};
                    font-family: Arial, sans-serif;
                    text-align: center;
                    margin-top: 20%;
                }
                </style>
                """;
        }

        return html;
    }
}

file static class Tools
{
    public static bool MarkmapCommandExists()
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "/c where markmap",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            }
        };

        process.Start();
        process.WaitForExit();
        return process.StandardOutput.ReadToEnd().Length >= 1;
    }
}
