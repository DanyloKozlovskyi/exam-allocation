import { buildGroupColors, GROUP_PALETTE, groupColor, NO_GROUP_COLOR } from '../constants';

/** Two groups whose FNV-1a hashes land on the same palette entry (the smoke-test collision). */
const findHashCollision = () => {
  const seen = new Map<string, string>();
  for (let index = 0; ; index++) {
    const group = `${10 + (index % 2)}-${index}`;
    const color = groupColor(group);
    const other = seen.get(color);
    if (other) return [other, group];
    seen.set(color, group);
  }
};

describe('buildGroupColors', () => {
  it('gives groups whose hashes collide distinct colours within a room', () => {
    const [a, b] = findHashCollision();
    expect(groupColor(a)).toBe(groupColor(b));
    const colorOf = buildGroupColors([a, b, a]);
    expect(colorOf(a)).not.toBe(colorOf(b));
  });

  it('assigns palette entries by sorted group value and is consistent for seats and legend', () => {
    const groups = ['11-Б', '10-А', '11-А', '10-А', null, ''];
    const colorOf = buildGroupColors(groups);
    expect(colorOf('10-А')).toBe(GROUP_PALETTE[0]);
    expect(colorOf('11-А')).toBe(GROUP_PALETTE[1]);
    expect(colorOf('11-Б')).toBe(GROUP_PALETTE[2]);
    expect(colorOf(null)).toBe(NO_GROUP_COLOR);
    expect(colorOf('')).toBe(NO_GROUP_COLOR);
    // Same input order-independent: the legend (sorted unique) and the seats (any order) agree.
    const legendColorOf = buildGroupColors([...new Set(groups)].sort());
    groups.forEach((group) => expect(legendColorOf(group)).toBe(colorOf(group)));
  });

  it('falls back to generated distinct hues when a room has more groups than the palette', () => {
    const groups = Array.from({ length: GROUP_PALETTE.length + 5 }, (_, index) => `група-${String(index).padStart(2, '0')}`);
    const colorOf = buildGroupColors(groups);
    const colors = groups.map(colorOf);
    expect(new Set(colors).size).toBe(groups.length);
    colors.forEach((color) => expect(color).toMatch(/^hsl\(/));
  });
});
