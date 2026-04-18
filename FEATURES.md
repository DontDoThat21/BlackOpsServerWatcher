# FEATURES.md

## Overview
A Windows desktop application for monitoring **Plutonium Black Ops 1 (T5) Zombies servers** in real-time. The application continuously queries configured servers over UDP, parses their status, and presents live telemetry in a responsive **WPF (MVVM) dark-themed UI**.

---

# Core Objectives

- Provide real-time visibility into server status
- Track player count and round progression
- Enable proactive alerts when servers become active
- Maintain low-latency, non-blocking querying
- Support persistent server lists via SQLite

---

# Technology Stack

| Layer        | Technology              |
|-------------|-------------------------|
| UI           | WPF (.NET)              |
| Architecture | MVVM                    |
| Language     | C#                      |
| Networking   | UDP (UdpClient)         |
| Persistence  | SQLite                  |
| Notifications| Windows 11 Toasts       |
| Concurrency  | async/await + Tasks     |

---

# UI / UX

## Theme
- Dark theme (default)
- High contrast text for readability
- Accent colors for status indicators

## Layout

### Main Window
- Server list grid (primary view)
- Toolbar (top)
- Status bar (bottom)

---

## Server Grid Columns

| Column        | Description                          |
|--------------|--------------------------------------|
| Name          | User-defined server label            |
| IP Address    | IP + Port                            |
| Status        | Online / Offline                     |
| Player Count  | Current number of players            |
| Max Players   | Server capacity                      |
| Round         | Current zombies round (if available) |
| Map           | Current map                          |
| Ping          | Latency (ms)                         |
| Last Updated  | Timestamp of last successful query   |

---

## Visual Indicators

- Online → Green
- Offline → Red
- Timeout / stale → Yellow

Row highlight:
- Triggered when player count transitions from 0 → >0

---

# Server Management

## Add Server
Inputs:
- IP Address
- Port
- Optional Name

Validation:
- Valid IPv4 format
- Port range (1–65535)

## Remove Server
- Deletes from UI and database

## Edit Server
- Modify name, IP, or port

## Bulk Import (Optional)
- Import from text file (IP:PORT format)

---

# Live Query System

## Protocol
UDP query packet:
\xFF\xFF\xFF\xFFgetstatus\n

---

## Query Engine

### Features
- Fully asynchronous
- Parallel querying (configurable)
- Timeout handling
- Retry logic

### Configurable Settings

| Setting              | Default |
|----------------------|--------|
| Query Interval       | 5 seconds |
| Timeout              | 3 seconds |
| Max Concurrent Calls | 20 |

---

# Continuous Loop (Auto-Reping)

- Runs while app is open
- Periodically queries all servers
- Non-blocking (async)
- Throttled to prevent network flooding

---

# Notification System

## Trigger Condition

Send a Windows toast notification when:

- Previous state: PlayerCount == 0
- New state: PlayerCount > 0

---

## Notification Content

- Title: Server Activity Detected
- Body includes:
  - Server name or IP
  - Player count
  - Map name
  - Round (if available)

---

## Behavior

- Fires only on transition (edge-triggered)
- No duplicate notifications
- Resets when player count returns to 0

---

# Data Persistence (SQLite)

## Purpose

- Persist server list
- Store optional historical snapshots
- Enable future analytics features

---

## Schema

### Servers Table

CREATE TABLE Servers (
    Id INTEGER PRIMARY KEY,
    Name TEXT,
    IP TEXT NOT NULL,
    Port INTEGER NOT NULL,
    CreatedAt DATETIME
);

---

### ServerStatusSnapshots (Optional)

CREATE TABLE ServerStatusSnapshots (
    Id INTEGER PRIMARY KEY,
    ServerId INTEGER,
    PlayerCount INTEGER,
    Round INTEGER,
    Map TEXT,
    Timestamp DATETIME
);

---

# Parsing Engine

## Responsibilities

- Parse raw UDP responses
- Extract key/value pairs
- Extract player list
- Normalize into structured models

---

## Extracted Fields

| Field        | Source        |
|-------------|--------------|
| Player Count | Player lines  |
| Round        | Dvar (if present) |
| Map          | mapname       |
| Game Type    | g_gametype    |

---

# Architecture

## Project Structure

/ServerQuery.Core
    /Networking
    /Parsing
    /Models

/ServerQuery.Data
    /SQLite
    /Repositories

/ServerQuery.UI
    /Views
    /ViewModels
    /Services

---

## Key Components

### ServerQueryClient
- Sends UDP requests
- Handles retries and timeouts

### ServerParser
- Converts raw response into structured data

### QueryScheduler
- Controls polling loop
- Manages concurrency

### NotificationService
- Sends Windows toast notifications

---

# Performance Considerations

- Non-blocking async queries
- Batched server requests
- Rate limiting per server
- Efficient parsing to reduce allocations

---

# Error Handling

- Graceful timeout handling
- Server marked offline after repeated failures
- Logging for:
  - Network errors
  - Parsing failures

---

# Future Enhancements

- Favorites / pinned servers
- Sorting and filtering
- Historical graphs (player trends)
- Configurable refresh intervals
- Export server list
- Discord / webhook notifications
- System tray background monitoring

---

# Non-Functional Requirements

- Responsive UI with 100+ servers
- Minimal CPU usage when idle
- Resilient to packet loss
- Clean MVVM separation

---

# Summary

This application acts as a real-time server telemetry dashboard for Plutonium BO1 Zombies servers by combining:

- UDP-based querying
- Continuous polling
- Reactive UI updates
- Event-driven notifications
- Persistent storage

---

# Implementation Checklist

Items are ordered by dependency — complete phases top-to-bottom.

## Phase 1 — Project Structure & Models

- [ ] **1.1** Create solution with three projects: `ServerQuery.Core`, `ServerQuery.Data`, `ServerQuery.UI` (rename existing project to `ServerQuery.UI`)
- [ ] **1.2** Add `ServerInfo` model: `Id`, `Name`, `IP`, `Port`, `Status`, `PlayerCount`, `MaxPlayers`, `Round`, `Map`, `GameType`, `PingMs`, `LastUpdated`
- [ ] **1.3** Add `ServerStatus` enum: `Unknown`, `Online`, `Offline`, `Stale`
- [ ] **1.4** Add `PlayerInfo` model: `Name`, `Score`, `Ping`
- [ ] **1.5** Add `QueryResult` model wrapping raw response, parsed `ServerInfo`, and a success/error flag
- [ ] **1.6** Add NuGet packages: `Microsoft.Data.Sqlite`, `CommunityToolkit.Mvvm`

---

## Phase 2 — Networking & Parsing

- [ ] **2.1** Implement `ServerQueryClient`: sends `\xFF\xFF\xFF\xFFgetstatus\n` over UDP, returns raw byte response, configurable timeout + retry count
- [ ] **2.2** Implement `ServerResponseParser`: splits response on `\n`, extracts key/value dvars (`mapname`, `g_gametype`, `sv_maxclients`, `round`, etc.) and player lines
- [ ] **2.3** Map parsed fields onto `ServerInfo`; derive `PlayerCount` from player line count
- [ ] **2.4** Unit-test parser with captured real response bytes (no network required)

---

## Phase 3 — Data Layer (SQLite)

- [ ] **3.1** Implement `DatabaseInitializer`: creates `Servers` and `ServerStatusSnapshots` tables on first run using the schema defined above
- [ ] **3.2** Implement `ServerRepository`: `GetAll`, `Add`, `Update`, `Delete` (CRUD for the `Servers` table)
- [ ] **3.3** Implement `SnapshotRepository`: `Insert` for writing periodic status snapshots
- [ ] **3.4** Wire `DatabaseInitializer` to run at app startup before any repository calls

---

## Phase 4 — Query Scheduler

- [ ] **4.1** Implement `QueryScheduler`: runs a continuous async loop, queries all servers in parallel (up to `MaxConcurrentCalls`), fires a `ServerUpdated` event per result
- [ ] **4.2** Expose configurable settings: `QueryIntervalSeconds` (default 5), `TimeoutSeconds` (default 3), `MaxConcurrentCalls` (default 20)
- [ ] **4.3** Implement per-server failure counter; set `Status = Offline` after 3 consecutive timeouts
- [ ] **4.4** Implement stale detection: set `Status = Stale` if `LastUpdated` is older than `2 × QueryInterval`
- [ ] **4.5** Expose `Start()` / `Stop()` / `ForceRefresh()` methods

---

## Phase 5 — Notification Service

- [ ] **5.1** Implement `NotificationService` using `Microsoft.Toolkit.Uwp.Notifications` (Windows toast API)
- [ ] **5.2** Fire notification on `PlayerCount 0 → >0` transition only (edge-triggered, no duplicates)
- [ ] **5.3** Include server name/IP, player count, map, and round in notification body
- [ ] **5.4** Reset notification guard when `PlayerCount` returns to 0

---

## Phase 6 — ViewModel & Bindings

- [ ] **6.1** Implement `MainViewModel` (CommunityToolkit.Mvvm `ObservableObject`): exposes `ObservableCollection<ServerViewModel>`, commands for Add/Remove/Edit/Refresh
- [ ] **6.2** Implement `ServerViewModel`: wraps `ServerInfo`, exposes all grid-bound properties as observable, computes `StatusColor` brush from `Status`
- [ ] **6.3** Implement `AddServerViewModel` + dialog: fields for IP, Port, Name; inline validation messages
- [ ] **6.4** Implement `SettingsViewModel`: exposes query interval, timeout, max concurrent calls; persists to `appsettings.json` or SQLite key-value table
- [ ] **6.5** Subscribe `MainViewModel` to `QueryScheduler.ServerUpdated`; dispatch UI updates on the WPF dispatcher

---

## Phase 7 — UI (WPF / XAML)

- [ ] **7.1** Apply dark theme: define global `ResourceDictionary` with background (`#121212`), surface (`#1E1E1E`), text, and accent colors
- [ ] **7.2** Build `MainWindow`: `DockPanel` with `ToolBar` (top), `DataGrid` (center), `StatusBar` (bottom)
- [ ] **7.3** Configure `DataGrid` columns matching the Server Grid Columns table (non-editable, auto-sizing)
- [ ] **7.4** Apply `StatusColor` binding: row `Background` or `Status` cell foreground driven by `ServerViewModel.StatusColor`
- [ ] **7.5** Highlight row when `PlayerCount` transitions 0 → >0 (animated accent flash, then settle to highlight color)
- [ ] **7.6** Toolbar buttons: **Add**, **Remove** (enabled only when row selected), **Edit** (enabled only when row selected), **Refresh All**
- [ ] **7.7** Status bar: show total servers, online count, last global refresh time
- [ ] **7.8** Build `AddEditServerDialog` (modal `Window` or `Popup`)
- [ ] **7.9** Build `SettingsDialog` with sliders/spinners for query interval and timeout

---

## Phase 8 — App Wiring & Startup

- [ ] **8.1** In `App.xaml.cs`: initialize DI container (or manual composition root), run `DatabaseInitializer`, load saved servers, start `QueryScheduler`
- [ ] **8.2** On app exit: stop `QueryScheduler`, flush any pending snapshots, dispose resources cleanly
- [ ] **8.3** Handle first-run (empty server list): show placeholder text in grid ("No servers — click Add to get started")

---

## Phase 9 — Polish & Non-Functionals

- [ ] **9.1** Verify UI remains responsive with 100 servers being polled simultaneously
- [ ] **9.2** Add structured logging (`Microsoft.Extensions.Logging` → debug output or file sink) for network errors and parse failures
- [ ] **9.3** Add bulk import: parse a `.txt` file of `IP:PORT` lines and add each as a server
- [ ] **9.4** Validate IPv4 format and port range (1–65535) in `AddEditServerDialog` before saving
- [ ] **9.5** Write integration smoke test: spin up a loopback UDP echo, send a canned status response, verify `ServerInfo` fields parsed correctly

---

## Future / Backlog (not in current scope)

- [ ] Favorites / pinned servers (sort to top)
- [ ] Column sorting and filtering in the grid
- [ ] Historical player-count sparkline per server
- [ ] Export server list to CSV / JSON
- [ ] Discord webhook notifications
- [ ] System tray icon with background monitoring when window is minimized