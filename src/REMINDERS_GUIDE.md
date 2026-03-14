# reminders.json Configuration Guide

This guide documents the full reminder configuration surface used by Macrosharp, including supported fields, defaults, recurrence behaviors, popup formatting tags, and runtime behavior.

## 1. File Location

The app loads reminders from:

- `src/reminders.json` in this repository layout

At runtime, the host resolves this file via `PathLocator.GetConfigPath("reminders.json")`.

## 2. Top-Level Schema

```json
{
  "version": 1,
  "settings": { },
  "reminders": [ ]
}
```

Top-level fields:

- `version` (number)
  - Minimum normalized value: `1`
- `settings` (object)
  - Global defaults and policies
- `reminders` (array)
  - Reminder definitions

## 3. Enum Serialization Rules

Enum values are serialized and deserialized as **camelCase strings**.

Examples:

- `ReminderRecurrenceKind.EveryInterval` -> `"everyInterval"`
- `ReminderPopupPosition.BottomRight` -> `"bottomRight"`
- `ReminderMissedPolicy.FireWithinGraceWindow` -> `"fireWithinGraceWindow"`
- `DayOfWeek.Monday` -> `"monday"`

Recommended: always use camelCase for enum strings in `reminders.json`.

## 4. settings Object

```json
"settings": {
  "enabled": true,
  "localTimeOnly": true,
  "missedPolicy": "skip",
  "startupGraceMinutes": 0,
  "defaultChannels": {
    "toast": true,
    "popup": true,
    "sound": false
  },
  "popupDefaults": {
    "enabled": true,
    "position": "bottomRight",
    "monitorIndex": null,
    "durationSeconds": 10,
    "opacityPercent": 70,
    "snoozeMinutes": [5, 10, 15]
  }
}
```

### 4.1 settings.enabled

- Type: `bool`
- Default: `true`
- Behavior:
  - If `false`, scheduler does not fire reminders.

### 4.2 settings.localTimeOnly

- Type: `bool`
- Default: `true`
- Current behavior:
  - Present in schema, but currently not enforcing a separate UTC/local mode in scheduling logic.
  - Recurrence calculation uses local time.

### 4.3 settings.missedPolicy

- Type: enum string
- Values:
  - `"skip"`
  - `"fireWithinGraceWindow"`
  - `"fireAllMissed"`
- Default: `"skip"`

Behavior notes:

- `skip`: missed reminders are not replayed.
- `fireAllMissed`: due missed reminder can fire immediately.
- `fireWithinGraceWindow`: fires missed reminder if within `startupGraceMinutes`.

### 4.4 settings.startupGraceMinutes

- Type: integer
- Default: `0`
- Used with `missedPolicy = "fireWithinGraceWindow"`.

### 4.5 settings.defaultChannels

- Type: object
- Used as fallback when a reminder omits `channels`.

Fields:

- `toast` (bool, default `true`)
- `popup` (bool, default `true`)
- `sound` (bool, default `false`)

### 4.6 settings.popupDefaults

- Type: object
- Used as fallback when a reminder omits `popup`.

Fields:

- `enabled` (bool, default `true`)
- `position` (enum string, default `"bottomRight"`)
  - Allowed values: `topLeft`, `topCenter`, `topRight`, `middleLeft`, `center`, `middleRight`, `bottomLeft`, `bottomCenter`, `bottomRight`
- `monitorIndex` (int?, default `null`)
  - `null` means primary monitor
  - values are 0-based monitor indices and out-of-range values are clamped
- `durationSeconds` (int, default `10`)
  - Normalized range: `3..120`
- `opacityPercent` (int, default `70`)
  - Normalized range: `30..100`
- `snoozeMinutes` (int array, default `[5,10,15]`)
  - If empty, normalized back to `[5,10,15]`

## 5. reminders[] Item Schema

```json
{
  "id": "break-20",
  "title": "Eye break",
  "message": "[b]20-20-20 rule:[/b] ...",
  "enabled": true,
  "channels": {
    "toast": true,
    "popup": true,
    "sound": false
  },
  "popup": {
    "enabled": true,
    "position": "bottomRight",
    "monitorIndex": null,
    "durationSeconds": 10,
    "opacityPercent": 70,
    "snoozeMinutes": [5, 10, 15]
  },
  "recurrence": { }
}
```

### 5.1 id

- Type: string
- Required: effectively yes
- If missing/blank: auto-generated GUID string.

### 5.2 title

- Type: string
- Default/fallback: `"Reminder"` if blank.

### 5.3 message

- Type: string
- Supports lightweight markup for popup rendering.

### 5.4 enabled

- Type: bool
- Default: `true`

### 5.5 channels

- Type: object (same shape as `settings.defaultChannels`)
- Optional
- If omitted: inherited from `settings.defaultChannels`.

### 5.6 popup

- Type: object (same shape as `settings.popupDefaults`)
- Optional
- If omitted: inherited from `settings.popupDefaults`.

### 5.7 recurrence

- Type: object
- Required for meaningful scheduling

## 6. recurrence Object

Common fields:

- `kind` (enum string)
- `startDate` (`yyyy-MM-dd`) where applicable
- `time` (`HH:mm`) where applicable
- `interval` (`HH:mm:ss`) where applicable
- `anchor` (`programStart` or `explicitStart`) for interval mode
- `daysOfWeek` (array of day names) for weekly mode
- `dayOfMonth` (1..31, clamped) for monthly day mode
- `nthWeek` (1..5 clamped) and `nthWeekday` for monthly nth-weekday mode

### 6.1 kind = once

Example:

```json
"recurrence": {
  "kind": "once",
  "startDate": "2026-03-20",
  "time": "14:30"
}
```

Behavior:

- Fires only if scheduled time is in the future.
- Past one-time reminders do not re-trigger.

### 6.2 kind = everyInterval

Example (program start anchor):

```json
"recurrence": {
  "kind": "everyInterval",
  "interval": "00:20:00",
  "anchor": "programStart"
}
```

Example (explicit anchor):

```json
"recurrence": {
  "kind": "everyInterval",
  "interval": "01:00:00",
  "anchor": "explicitStart",
  "startDate": "2026-03-14",
  "time": "09:00"
}
```

Behavior:

- Interval must be valid `HH:mm:ss` and > 0.
- `programStart`: anchor is app startup local time.
- `explicitStart`: anchor comes from `startDate + time`.

### 6.3 kind = daily

Example:

```json
"recurrence": {
  "kind": "daily",
  "time": "10:30"
}
```

Behavior:

- Fires next occurrence at `time` every day.

### 6.4 kind = weekly

Example:

```json
"recurrence": {
  "kind": "weekly",
  "time": "09:00",
  "daysOfWeek": ["monday", "wednesday", "friday"]
}
```

Behavior:

- `daysOfWeek` can contain enum day names.
- If `daysOfWeek` is empty, scheduler uses current day-of-week as fallback.

### 6.5 kind = monthlyDayOfMonth

Example:

```json
"recurrence": {
  "kind": "monthlyDayOfMonth",
  "time": "08:15",
  "dayOfMonth": 31
}
```

Behavior:

- Day is clamped to `1..31`.
- If day exceeds month length, reminder is clamped to month end.

### 6.6 kind = monthlyNthWeekday

Example:

```json
"recurrence": {
  "kind": "monthlyNthWeekday",
  "time": "11:00",
  "nthWeek": 2,
  "nthWeekday": "tuesday"
}
```

Behavior:

- `nthWeek` clamped to `1..5`.
- Finds nth weekday in each month; skips months where nth occurrence does not exist.

## 7. Supported Message Markup (Popup)

Reminder popup supports these tags:

- `[b]...[/b]` -> bold
- `[i]...[/i]` -> italic
- `[color=#RRGGBB]...[/color]` -> colored text

Example:

```text
[b]Hydrate[/b]: drink [color=#58d68d]water[/color] every [i]hour[/i].
```

Notes:

- Use `#RRGGBB` hex colors.
- Keep tags well-formed and paired.

## 8. Channels and Delivery Behavior

For each fired reminder:

- If app is in silent mode: channels are suppressed.
- If `channels.toast = true`: toast notification is shown with rich tags stripped to plain text.
- If `channels.sound = true`: sound is played.
- If `channels.popup = true` and popup enabled: popup window is shown.

Popup stacking behavior:

- Concurrent popups are slot-allocated so they do not overlap.
- Right-anchored positions stack vertically then overflow to columns on the left.
- Left-anchored positions stack vertically then overflow to columns on the right.
- Center-anchored positions stack around center first, then overflow into side columns.

Popup user actions:

- Dismiss
- Snooze (uses first value from `snoozeMinutes` list)
- Timeout (auto-close after `durationSeconds`)

## 9. Runtime Metadata Fields

These fields can be written back by the app:

- `lastTriggeredUtc` (ISO timestamp)
  - used for dedup/resume logic
- `lastTriggerWasMonthEndClamp` (bool)
  - indicates a monthly day trigger was clamped to month end

You can omit these fields; app will manage them.

## 10. Auto-Normalization and Recovery

On load/save, configuration manager normalizes values:

- Ensures non-null `settings`, `defaultChannels`, `popupDefaults`, `reminders`.
- Fills missing reminder IDs/titles/messages/recurrence/channels/popup.
- Clamps:
  - popup duration to `3..120`
  - popup opacity to `30..100`
- Restores empty snooze list to `[5,10,15]`.

When config is invalid/corrupt:

- App logs load error.
- Attempts to backup original content to `reminders.bakN.json` (same directory).
- Recovers by writing the current/default valid configuration.

## 11. Comprehensive Example

```json
{
  "version": 1,
  "settings": {
    "enabled": true,
    "localTimeOnly": true,
    "missedPolicy": "fireWithinGraceWindow",
    "startupGraceMinutes": 15,
    "defaultChannels": {
      "toast": true,
      "popup": true,
      "sound": false
    },
    "popupDefaults": {
      "enabled": true,
      "position": "bottomRight",
      "monitorIndex": null,
      "durationSeconds": 12,
      "opacityPercent": 75,
      "snoozeMinutes": [5, 10, 20]
    }
  },
  "reminders": [
    {
      "id": "eye-break-20",
      "title": "Eye break",
      "message": "[b]20-20-20[/b]: look [color=#5dade2]20 ft[/color] away for [i]20 sec[/i].",
      "enabled": true,
      "channels": { "toast": true, "popup": true, "sound": false },
      "recurrence": {
        "kind": "everyInterval",
        "interval": "00:20:00",
        "anchor": "programStart"
      }
    },
    {
      "id": "weekly-planning",
      "title": "Weekly planning",
      "message": "[b]Plan your week[/b]",
      "enabled": true,
      "recurrence": {
        "kind": "weekly",
        "time": "09:00",
        "daysOfWeek": ["monday"]
      }
    },
    {
      "id": "month-end-report",
      "title": "Month-end report",
      "message": "Prepare month-end report",
      "enabled": true,
      "recurrence": {
        "kind": "monthlyDayOfMonth",
        "time": "17:00",
        "dayOfMonth": 31
      }
    }
  ]
}
```

## 12. Quick Validation Checklist

- Enum values are camelCase strings.
- `time` is `HH:mm`.
- `interval` is `HH:mm:ss`.
- `startDate` is `yyyy-MM-dd` when required.
- Weekly `daysOfWeek` values are valid day names.
- Monthly `nthWeek` in `1..5`.
- Popup opacity in `30..100` and duration in `3..120`.
- Message tags are well-formed.

## 13. Operational Features

The host exposes tray actions under Reminders:

- Reload reminders config
- Add reminder
- Edit reminder
- Delete reminder

Config file changes are also watched and auto-reloaded.
