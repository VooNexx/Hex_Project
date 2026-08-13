# Hex Project

A hex-grid movement system for turn-based tactics games, made in Unity (C#). Click a unit and every tile it can reach lights up; click a lit tile and the unit walks there along the cheapest path.


## Demo

<img width="1920" height="1080" alt="Aug 13 2026 Untitled" src="https://github.com/user-attachments/assets/d8f2aea3-f75b-4659-959d-b40b59ede4de" />

## Controls

Left click to select a unit, left click a highlighted tile to move it there.

## How it works

**Pathfinding** — `GraphSearch.cs` runs a breadth-first search out from the unit's tile. Each tile has its own movement cost, obstacles are skipped, and the search stops once a path would cost more than the unit's movement points. It returns both the set of reachable tiles and the parent links needed to rebuild any path back to the start.

**Coordinates** — a hex grid can't use plain x/z indices, because every other row is shifted sideways. `HexCoordinates.cs` converts world positions into offset coordinates and back, and `HexGrid.cs` resolves each tile's six neighbours depending on whether its row is odd or even.

**Movement** — `MovementSystem.cs` ties the two together: it asks the search for the range, highlights those tiles, generates the path to whichever tile you click, and moves the unit along it.

## Script structure

```
Assets/Scripts/Hexagon Scripts/
├── GraphSearch.cs      BFS range + path generation
├── HexCoordinates.cs   World position ↔ hex coordinate conversion
├── HexGrid.cs          Grid storage and neighbour lookup
├── Hex.cs              Single tile: cost, obstacle flag, highlight
├── MovementSystem.cs   Range display and unit movement
├── SelectionManager.cs Mouse selection
├── GlowHighlight.cs    Tile highlight visuals
├── Unit.cs             Movement points and movement execution
└── UnitManager.cs      Unit tracking
```

## Running it

Built with **Unity [6000.4.8f1]**. Clone the repo, open it through Unity Hub, load `Assets/Scenes/SampleScene.unity` and press Play.

## Credits

Programming by **Yunus Demir** ([@VooNexx](https://github.com/VooNexx)).
