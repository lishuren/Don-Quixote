
export function buildMapPayload(config, tables) {
  const { diningArea, zones, gridSize } = config;

  const toRect = (r) => ({
    x: (r && r.x != null) ? r.x : 0,
    y: (r && r.y != null) ? r.y : 0,
    width: (r && r.width != null) ? r.width : 0,
    height: (r && r.height != null) ? r.height : 0,
  });

  const serializeTable = (t) => {
    const width = t.width ?? (t.radius ? t.radius * 2 : t.side ?? 0);
    const height = t.height ?? (t.radius ? t.radius * 2 : t.side ?? 0);
    const cx = t.center?.x ?? (t.x != null ? t.x + width / 2 : 0);
    const cy = t.center?.y ?? (t.y != null ? t.y + height / 2 : 0);
    const bounds =
      t.bounds ??
      { x: cx - width / 2, y: cy - height / 2, width, height };

    return {
      id: t.id,
      type: t.type,
      shape: t.shape,
      center: { x: cx, y: cy },
      bounds: toRect(bounds),
    };
  };

  return {
    diningArea: toRect(diningArea),
    zones: Object.fromEntries(
      Object.entries(zones || {}).map(([k, z]) => [k, toRect(z)])
    ),
    tables: Array.isArray(tables) ? tables.map(serializeTable) : [],
    gridSize: gridSize || 10,
  };
}

export function defaultStartBottomRight(config) {
  console.log("diningArea", diningArea);
  const { diningArea } = config;
  return { x: diningArea.x + diningArea.width - 2, y: diningArea.y + diningArea.height - 2 };
}
