using Macrosharp.Devices.Core;
using Macrosharp.Devices.Keyboard;
using Macrosharp.Infrastructure;
using Macrosharp.UserInterfaces.DynamicWindow;
using Macrosharp.Win32.Abstractions.Explorer;

namespace Macrosharp.Runtime.FeatureRegistration.HotkeyRegistrations;

public static class FileManagementHotkeyRegistry
{
    public static void Register(HotkeyManager hotkeyManager, string sourceContext)
    {
        // Ctrl + Shift + M -> Create new file in Explorer
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_M,
            Modifiers.CTRL_SHIFT,
            () =>
            {
                ExplorerFileAutomation.CreateNewFile();
            },
            ExplorerHotkeys.IsExplorerOrDesktopFocused,
            description: "Create a new file in Explorer or Desktop.",
            sourceContext: sourceContext
        );

        // Shift + F2 -> Copy full path of selected files to clipboard
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.F2,
            Modifiers.SHIFT,
            () =>
            {
                var paths = ExplorerHotkeys.GetSelectedFilePaths();
                if (paths.Count > 0)
                {
                    string text = string.Join(Environment.NewLine, paths);
                    KeyboardSimulator.SetClipboardText(text);
                    Console.WriteLine($"Copied {paths.Count} path(s) to clipboard.");
                    AudioPlayer.PlaySuccessAsync();
                }
            },
            ExplorerHotkeys.IsExplorerOrDesktopFocused,
            description: "Copy selected file paths to clipboard.",
            sourceContext: sourceContext
        );

        // ` + P -> Convert selected PowerPoint files to PDF
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_P,
            Modifiers.BACKTICK,
            () => ExplorerFileAutomation.OfficeFilesToPdf("PowerPoint"),
            () => ExplorerHotkeys.IsExplorerOrDesktopFocused() && !Modifiers.IsScrollLockOn,
            description: "Convert selected PowerPoint files to PDF.",
            sourceContext: sourceContext
        );

        // ` + O -> Convert selected Word files to PDF
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_O,
            Modifiers.BACKTICK,
            () => ExplorerFileAutomation.OfficeFilesToPdf("Word"),
            () => ExplorerHotkeys.IsExplorerOrDesktopFocused() && !Modifiers.IsScrollLockOn,
            description: "Convert selected Word files to PDF.",
            sourceContext: sourceContext
        );

        // ` + E -> Convert selected Excel files to PDF
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_E,
            Modifiers.BACKTICK,
            () => ExplorerFileAutomation.OfficeFilesToPdf("Excel"),
            () => ExplorerHotkeys.IsExplorerOrDesktopFocused() && !Modifiers.IsScrollLockOn,
            description: "Convert selected Excel files to PDF.",
            sourceContext: sourceContext
        );

        // Ctrl + Shift + P -> Merge selected images into PDF (Normal mode)
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_P,
            Modifiers.CTRL_SHIFT,
            () => ExplorerFileAutomation.ImagesToPdf(),
            ExplorerHotkeys.IsExplorerOrDesktopFocused,
            description: "Merge selected images into a PDF.",
            sourceContext: sourceContext
        );

        // Ctrl + Shift + Alt + P -> Merge selected images into PDF (Resize mode)
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_P,
            Modifiers.CTRL_SHIFT_ALT,
            () =>
            {
                var win = new SimpleWindow("Images -> PDF (Resize)", labelWidth: 160, inputFieldWidth: 120);

                win.CreateDynamicInputWindow(inputLabels: ["Target Width:", "Width Threshold:", "Min Width:", "Min Height:"], placeholders: ["690", "1200", "100", "100"]);

                if (win.userInputs.Count < 4)
                    return;

                int targetWidth = int.TryParse(win.userInputs[0], out int tw) && tw > 0 ? tw : 690;
                int widthThreshold = int.TryParse(win.userInputs[1], out int wt) && wt > 0 ? wt : 1200;
                int minWidth = int.TryParse(win.userInputs[2], out int mw) && mw > 0 ? mw : 100;
                int minHeight = int.TryParse(win.userInputs[3], out int mh) && mh > 0 ? mh : 100;

                Console.WriteLine($"Images->PDF Resize: targetWidth={targetWidth}, widthThreshold={widthThreshold}, minWidth={minWidth}, minHeight={minHeight}");
                ExplorerFileAutomation.ImagesToPdf(mode: ExplorerFileAutomation.ImagesToPdfMode.Resize, targetWidth: targetWidth, widthThreshold: widthThreshold, minWidth: minWidth, minHeight: minHeight);
            },
            ExplorerHotkeys.IsExplorerOrDesktopFocused,
            description: "Merge selected images into a PDF with custom resize options.",
            sourceContext: sourceContext
        );

        // Ctrl + Alt + Win + I -> Convert selected images to .ico
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_I,
            Modifiers.CTRL_ALT_WIN,
            () => ExplorerHotkeys.ConvertSelectedImagesToIco(),
            ExplorerHotkeys.IsExplorerOrDesktopFocused,
            description: "Convert selected images to ICO files.",
            sourceContext: sourceContext
        );

        // Ctrl + Alt + Win + M -> Convert selected .mp3 files to .wav
        hotkeyManager.RegisterConditionalHotkey(
            VirtualKey.KEY_M,
            Modifiers.CTRL_ALT_WIN,
            () => ExplorerHotkeys.ConvertSelectedMp3ToWav(),
            ExplorerHotkeys.IsExplorerOrDesktopFocused,
            description: "Convert selected MP3 files to WAV.",
            sourceContext: sourceContext
        );
    }
}

