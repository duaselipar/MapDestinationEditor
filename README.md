# MapDestinationEditor

## What it does
- Auto-find `MapDestination.dat` in your client folder.
- Reads `GameMap.ini` to show **Map ID + Name**.
- 3 panes:
  - **Maps (dv1)** → list of maps.
  - **Paths (dv2)** → directories & coordinates for the selected map.
  - **Children (dv3)** → coordinates under the selected directory.
- Right-click menus & hotkeys to **add, move, delete** items.
- Edit **Name / Info / X / Y** in the detail panel, then **Update**.
- **Save** back to `MapDestination.dat` (auto `.bak` backup).

## Quick Start
1. Launch the app.
2. Select your **EO/CT client folder** (Client Path → **Find**) and click **Load**.
3. Pick a **map** on the left (dv1).
4. Use the middle/right lists to add or edit entries:
   - **Directory** = `X = Y = -1` (only Name/Info editable).
   - **Coordinate** = has `X,Y` (Name/Info/X/Y editable).
5. Click **Update** to apply field changes.
6. Click **Save** to write the file.

## Hotkeys (right-click menus available too)
- **dv1 (maps)**: Add (**Alt+A**), Delete (**Del**)
- **dv2 (paths)**: Move Up (**Q**), Move Down (**W**), Add Coordinate (**A**), Add Directory (**T**), Delete (**Del**)
- **dv3 (children)**: Move Up (**Q**), Move Down (**W**), Add Coordinate (**A**), Delete (**Del**)
