# MapPlannerApi

Minimal ASP.NET Web API that accepts a map and plans routes avoiding tables and rectangular zones using a Nav2-like grid A*.

## Endpoints

- GET `/api/map` — Fetch the currently stored map and its `mapId`
- POST `/api/map` — Upload the map payload (returns `mapId`)
- POST `/api/plan` — Request a path from start to target
- GET `/swagger` — Swagger UI

## Run

```bash
cd BackendAPI/MapPlannerApi
dotnet restore
dotnet run
```

Default URL: http://localhost:5199

## Payloads

Example `/api/map` body:
```json
{
  "diningArea": { "x": 0, "y": 0, "width": 800, "height": 600 },
  "zones": {
    "kitchen": { "x": 50, "y": 570, "width": 100, "height": 30 },
    "reception": { "x": 150, "y": 0, "width": 100, "height": 30 },
    "cashier": { "x": 300, "y": 0, "width": 100, "height": 30 },
    "restrooms": { "x": 550, "y": 570, "width": 100, "height": 30 }
  },
  "tables": [
    { "id": 1, "center": { "x": 200, "y": 200 }, "bounds": { "x": 160, "y": 160, "width": 80, "height": 80 } }
  ],
  "gridSize": 10
}
```

Example `/api/plan` body:
Example `GET /api/map` response:
```json
{
  "mapId": "3",
  "map": {
    "diningArea": { "x": 0, "y": 0, "width": 800, "height": 600 },
    "zones": { /* ... */ },
    "tables": [ /* ... */ ],
    "gridSize": 10
  }
}
```

## Storage

Maps are stored in-memory in the singleton `MapStore`. Each successful `POST /api/map` replaces the current map and increments a version counter exposed as `mapId`. There is no disk persistence by default.
```json
{
  "start": { "x": 798, "y": 598 },
  "tableId": "1",
  "robotRadius": 16
}
```
