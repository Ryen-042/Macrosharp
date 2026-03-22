# Cmder Startup Guide: Run Macrosharp Console Host as Admin

This guide shows how to make Cmder start the Macrosharp console host automatically in a ConEmu-hosted tab, elevated (Administrator), when Cmder launches.

## Goal

- Start this app at Cmder startup:
  - `src/Macrosharp.Hosts/Macrosharp.Hosts.Console`
- Run it in a ConEmu-hosted tab (not via a `cmd /k` wrapper).
- Run it as Administrator (UAC prompt expected).

## Prerequisites

- Cmder installed (ConEmu-based).
- .NET SDK installed (project currently targets `net10.0-windows10.0.17763.0`).
- Repository is available at:
  - `E:\Development\Automation\Macrosharp`

## Option A (Recommended): Run `dotnet run` directly in an elevated ConEmu tab

Use this if you want to always run the latest source.

1. Open Cmder.
2. Open Settings (`Win + Alt + P`).
3. Go to `Startup` -> `Tasks`.
4. Click `+` to create a new task.
5. Name the task, for example: `Macrosharp::ConsoleHostAdmin`.
6. In `Commands`, paste exactly:

```cmd
dotnet run --project "E:\Development\Automation\Macrosharp\src\Macrosharp.Hosts\Macrosharp.Hosts.Console\Macrosharp.Hosts.Console.csproj" -new_console:a:t:"Macrosharp Host"
```

7. Click `Save settings`.
8. Go to `Startup`.
9. Under startup mode, choose `Specified named task`.
10. Select `{Macrosharp::ConsoleHostAdmin}`.
11. Click `Save settings` and restart Cmder.

Expected result:
- Cmder starts and opens a tab named `Macrosharp Host`.
- Windows asks for elevation (UAC).
- The console host starts from source.

Why this is preferred:
- This runs the command directly as a ConEmu task command, so the tab stays ConEmu-hosted and keeps ConEmu terminal capabilities (ANSI colors and related features).

## Option B: Run built EXE in an elevated startup tab

Use this if you prefer launching the compiled executable directly.

1. Build once from repo root:

```powershell
cd E:\Development\Automation\Macrosharp\src
dotnet build .\Macrosharp.sln
```

2. Create a Cmder task as above, but use this command:

```cmd
"E:\Development\Automation\Macrosharp\src\Macrosharp.Hosts\Macrosharp.Hosts.Console\bin\Debug\net10.0-windows10.0.17763.0\Macrosharp.Hosts.Console.exe" -new_console:a:t:"Macrosharp Host"
```

Note:
- If you build in `Release`, update the path accordingly.

## Optional: Start Cmder itself at Windows sign-in (still elevated host tab)

If you also want this to happen automatically at OS logon:

1. Open `Task Scheduler`.
2. Create a task (not Basic Task).
3. In `General`:
   - Check `Run with highest privileges`.
4. In `Triggers`:
   - Add `At log on`.
5. In `Actions`:
   - Program/script: path to `Cmder.exe` (or `ConEmu64.exe`).
   - Add arguments:

```cmd
-run {Macrosharp::ConsoleHostAdmin}
```

6. Save task.

This avoids manual Cmder startup and launches your elevated host task automatically.

## Change New Console Default Without Affecting Startup

You can change which task is preselected for creating a new console/tab without changing startup behavior.

These are separate settings:
- Startup app launch uses `Startup` -> `Specified named task`.
- New tab default uses `Startup` -> `Tasks` -> `Default task for new console`.

To keep Macrosharp auto-start on launch, but use a different default for new tabs:

1. Keep this setting unchanged:
  - `Startup` -> `Specified named task` = `{Macrosharp::ConsoleHostAdmin}`.
2. Go to `Startup` -> `Tasks`.
3. Select the task you want as your normal new-tab default (for example your preferred shell task).
4. Enable `Default task for new console` for that task.
5. Save settings.

Result:
- Cmder still starts Macrosharp automatically at app startup.
- Clicking `+` (new console) uses your chosen default task instead of `{Macrosharp::ConsoleHostAdmin}`.

## Optional: Run from `cmd` in Headless Mode

Use this only if you want background execution without opening a visible Cmder/ConEmu tab.

Important tradeoff:
- This mode does not use ConEmu hosting, so ConEmu tab features (rendering, ANSI experience, tab controls) are not applicable.

Recommended approach (Task Scheduler):

1. Open `Task Scheduler`.
2. Create a task (not Basic Task).
3. In `General`:
  - Check `Run with highest privileges`.
  - Choose `Run whether user is logged on or not` if you want true background operation.
4. In `Triggers`:
  - Add `At log on` (or your preferred trigger).
5. In `Actions`:
  - Program/script:

```cmd
cmd.exe
```

  - Add arguments:

```cmd
/c dotnet run --project "E:\Development\Automation\Macrosharp\src\Macrosharp.Hosts\Macrosharp.Hosts.Console\Macrosharp.Hosts.Console.csproj"
```

6. Save task.

Alternative (built EXE):

```cmd
/c "E:\Development\Automation\Macrosharp\src\Macrosharp.Hosts\Macrosharp.Hosts.Console\bin\Debug\net10.0-windows10.0.17763.0\Macrosharp.Hosts.Console.exe"
```

If your goal is best interactive terminal behavior, prefer Option A (ConEmu-hosted startup task).

## Troubleshooting

- `dotnet` not found:
  - Ensure .NET SDK is installed and on `PATH`.
- No elevation prompt:
  - Confirm `-new_console:a` is present in the task command.
- App behaves differently vs non-admin:
  - This is expected for low-level hooks/input behavior around elevated windows.

## Notes About `macrosharp` Alias

If you still want to use your alias, it can also be called directly from a task command (without `cmd /k`), for example:

```cmd
macrosharp -new_console:t:"Macrosharp Host"
```

But alias usage is optional. The direct `dotnet run --project ...` task above is usually simpler and more portable.

## Reference Notes

- Cmder uses ConEmu Tasks for startup commands.
- `-new_console:a` is the ConEmu switch to request elevation (RunAs/Admin).
- Startup mode `Specified named task` is the supported way to launch a predefined task on Cmder start.