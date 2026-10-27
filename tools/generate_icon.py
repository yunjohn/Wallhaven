"""Generate the app's original mountain mark as an ICO without third-party assets."""
from pathlib import Path
import math
import struct

size = 64
pixels = bytearray()
for y in range(size - 1, -1, -1):
    for x in range(size):
        dx = max(10 - x, 0, x - 53)
        dy = max(10 - y, 0, y - 53)
        inside = dx * dx + dy * dy <= 100
        mountain = (12 <= x <= 44 and 43 - (x - 12) * 1.7 <= y <= 48) or (32 <= x <= 54 and 43 - (x - 32) * 1.3 <= y <= 48)
        # Bounded triangle silhouettes.
        mountain = (14 <= x <= 44 and y >= 18 + abs(x - 29) * 1.65 and y <= 48) or (35 <= x <= 54 and y >= 31 + abs(x - 44) * 1.6 and y <= 48)
        r, g, b = (255, 255, 255) if mountain else (0, 90, 158)
        pixels += bytes((b, g, r, 255 if inside else 0))
mask = bytes(((size + 31) // 32) * 4 * size)
bitmap = struct.pack('<IIIHHIIIIII', 40, size, size * 2, 1, 32, 0, len(pixels), 0, 0, 0, 0) + pixels + mask
icon = struct.pack('<HHH', 0, 1, 1) + struct.pack('<BBBBHHII', size, size, 0, 0, 1, 32, len(bitmap), 22) + bitmap
path = Path(__file__).resolve().parents[1] / 'src/Wallhaven.App/Assets/app.ico'
path.parent.mkdir(parents=True, exist_ok=True)
path.write_bytes(icon)
print(path)
