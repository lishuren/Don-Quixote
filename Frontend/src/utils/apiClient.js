// Simple client for backend MapPlannerApi
const API_BASE = process.env.REACT_APP_API_BASE || 'http://localhost:5199';

export async function postMap(payload) {
  const res = await fetch(`${API_BASE}/api/map`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(`postMap failed: ${res.status}`);
  return res.json();
}

export async function planRoute(request) {
  const res = await fetch(`${API_BASE}/api/plan`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request), // request may include { start, tableId, targetRect, end, robotRadius }
  });
  if (!res.ok) throw new Error(`planRoute failed: ${res.status}`);
  return res.json();
}

export async function getMap() {
  const res = await fetch(`${API_BASE}/api/map`);
  if (!res.ok) throw new Error(`getMap failed: ${res.status}`);
  return res.json();
}

export async function postTables(tableList) {
  const res = await fetch(`${API_BASE}/api/tables`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(tableList),
  });
  if (!res.ok) throw new Error(`postTables failed: ${res.status}`);
  return res.json();
}
