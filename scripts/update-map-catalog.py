"""Find wiki map images in public galleries and source credits, preserving existing pin IDs.

Only public reference websites and this workspace are used. Nothing reads the game.
The original Miraheze API is currently unavailable from this environment, so the
public Stories of MnM galleries and MnM Atlas source credits supply original wiki
file links. No atlas artwork or website code is copied.
"""
import argparse
import hashlib
import json
import re
import struct
import time
import urllib.parse
import urllib.request
from datetime import date
from html.parser import HTMLParser
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / 'src/MnMMaps.App/Data/maps.json'
CACHE = ROOT / 'artifacts/map-catalog-sources'
STORIES = 'https://storiesofmnm.com'
ATLAS_DATA = 'https://raw.githubusercontent.com/ocombe/mnmatlas/main/data/maps.json'
WIKI = 'https://monstersandmemories.miraheze.org/wiki/'
IMAGE_ROOT = 'https://static.wikitide.net/monstersandmemorieswiki/'
PLACEHOLDERS = ('phformaps', 'phicon', 'placeholder', 'map not yet')
# Reviewed: the Ail'Vorith gallery uses a scenic screenshot as its map fallback.
NON_MAP_FILES = {'ailvorith.jpg'}
MAP_NAMES = {
    'Ailvorith_foreignquarters2.jpg': "Ail'Vorith — Foreign quarter",
    'Blacktide_Bay.jpg': 'Blacktide Bay',
    'Evershade_Weald_june_26.jpg': 'Evershade Weald — Nindaan (June 26)',
    'Evershade_Weald_Map.jpg': 'Evershade Weald — Overview',
    'Faelindral_V2.jpg': 'Faelindral — V2',
    'Fallen_pass_v1.jpg': 'Fallen Pass — V1',
    'Graincellar.png': 'Grain Cellar — Plan',
    'Night_harbor_V5.jpg': 'Night Harbor — V5',
    'Scarwood.png': 'Scarwood',
    'Sungreet_Strand_map.jpg': 'Sungreet Strand — Overview',
    'Sungreet_Strand_v4.jpg': 'Sungreet Strand — V4',
    'Sunken_resort.png': 'Sunken Resort — All floors',
    'Keepersbighteg.png': "Keeper's Bight",
}


def read_url(url, refresh=False, binary=False):
    CACHE.mkdir(parents=True, exist_ok=True)
    suffix = '.image' if binary else '.source'
    cached = CACHE / (hashlib.sha256(url.encode()).hexdigest() + suffix)
    if refresh or not cached.exists():
        request = urllib.request.Request(url, headers={'User-Agent': 'MnMMapsCatalog/0.2 (public map reference; cached requests)'})
        time.sleep(0.35)
        with urllib.request.urlopen(request, timeout=45) as response:
            data = response.read(40 * 1024 * 1024 + 1)
        if len(data) > 40 * 1024 * 1024:
            raise ValueError('Image or source exceeds the viewer limit of 40 MiB')
        cached.write_bytes(data)
    return cached.read_bytes() if binary else cached.read_text(encoding='utf-8-sig')


class ZoneIndex(HTMLParser):
    def __init__(self):
        super().__init__()
        self.paths = set()

    def handle_starttag(self, tag, attrs):
        href = dict(attrs).get('href', '')
        url = urllib.parse.urlparse(urllib.parse.urljoin(STORIES, href))
        if tag == 'a' and url.netloc == 'storiesofmnm.com' and re.fullmatch(r'/zone/[a-z0-9-]+/', url.path):
            self.paths.add(url.path)


class MapGallery(HTMLParser):
    def __init__(self):
        super().__init__()
        self.zone = ''
        self.heading = None
        self.text = ''
        self.in_maps = False
        self.images = []

    def handle_starttag(self, tag, attrs):
        attributes = dict(attrs)
        if tag == 'h1' and attributes.get('class') == 'zone-title':
            self.heading, self.text = 'h1', ''
        elif tag == 'h2':
            self.heading, self.text = 'h2', ''
            self.in_maps = False
        elif tag == 'img' and self.in_maps:
            image = original_image(attributes.get('src', ''))
            if image:
                self.images.append((image, attributes.get('alt', '')))

    def handle_data(self, text):
        if self.heading:
            self.text += text

    def handle_endtag(self, tag):
        if tag != self.heading:
            return
        if tag == 'h1':
            self.zone = self.text.replace(' Zone Info and Map', '').strip()
        elif tag == 'h2':
            self.in_maps = self.text.strip().casefold() == 'zone map'
        self.heading = None


def original_image(url):
    parsed = urllib.parse.urlparse(url)
    if parsed.netloc != 'static.wikitide.net':
        return None
    parts = parsed.path.strip('/').split('/')
    if len(parts) == 6 and parts[:2] == ['monstersandmemorieswiki', 'thumb']:
        parts = [parts[0], *parts[2:5]]
    if len(parts) != 4 or parts[0] != 'monstersandmemorieswiki':
        return None
    filename = urllib.parse.unquote(parts[-1])
    if filename.casefold() in NON_MAP_FILES or not filename.lower().endswith(('.png', '.jpg', '.jpeg', '.gif')) or any(part in filename.casefold() for part in PLACEHOLDERS):
        return None
    return 'https://static.wikitide.net/' + '/'.join(parts)


def image_from_source(url):
    if image := original_image(url):
        return image
    if not url.startswith(WIKI + 'File:'):
        return None
    filename = urllib.parse.unquote(url.split('File:', 1)[1]).replace(' ', '_')
    filename = filename[0].upper() + filename[1:]
    if not filename.lower().endswith(('.png', '.jpg', '.jpeg', '.gif')):
        return None
    digest = hashlib.md5(filename.encode()).hexdigest()
    return IMAGE_ROOT + digest[0] + '/' + digest[:2] + '/' + urllib.parse.quote(filename, safe='')


def describe_variant(image, label):
    filename = urllib.parse.unquote(image.rsplit('/', 1)[-1])
    if 'isometric' in filename.casefold() or 'isometric' in label.casefold():
        return 'Isometric'
    if 'Combined' in filename:
        return 'Combined map'
    if 'Ground_Floor' in filename:
        return 'Ground floor'
    if 'BottomFloor' in filename:
        return 'Lower city'
    if 'Foreign' in filename or 'foreign' in filename:
        return 'Foreign quarter'
    if 'Naawa' in filename:
        return "Naawa's map"
    if 'Deep_Dunes' in filename:
        return 'Older Deep Dunes map'
    return Path(filename).stem.replace('_', ' ')


def candidate(zone, image, label, discovered):
    zone = zone.replace('’', "'").replace('‘', "'")
    filename = urllib.parse.unquote(image.rsplit('/', 1)[-1])
    zone_id = re.sub('[^a-z0-9]+', '-', zone.casefold()).strip('-')
    variant = describe_variant(image, label)
    return {
        'id': zone_id + '--' + hashlib.sha256(filename.encode()).hexdigest()[:10],
        'name': MAP_NAMES.get(filename, zone + ' — ' + variant),
        'area': zone,
        'imageUrl': image,
        'sourceUrl': WIKI + 'File:' + urllib.parse.quote(filename, safe=''),
        'credit': zone + ' · Community wiki map. See Wiki source for the original author and image license.',
        'discoveredFrom': discovered,
        'checkedOn': str(date.today()),
    }


def check_image(data):
    # Inspect actual raster headers rather than trusting a successful HTML response.
    if data.startswith(b'\x89PNG\r\n\x1a\n') and len(data) >= 24:
        width, height = struct.unpack('>II', data[16:24])
    elif data.startswith((b'GIF87a', b'GIF89a')):
        width, height = struct.unpack('<HH', data[6:10])
    elif data.startswith(b'\xff\xd8'):
        offset = 2
        while offset + 4 < len(data):
            if data[offset] != 255:
                raise ValueError('Invalid JPEG header')
            while data[offset] == 255:
                offset += 1
            marker = data[offset]
            offset += 1
            if marker in (0xD8, 0xD9) or 0xD0 <= marker <= 0xD7:
                continue
            length = int.from_bytes(data[offset:offset + 2], 'big')
            if marker in (0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF):
                height, width = struct.unpack('>HH', data[offset + 3:offset + 7])
                break
            if length < 2:
                raise ValueError('Invalid JPEG segment')
            offset += length
        else:
            raise ValueError('JPEG dimensions not found')
    else:
        raise ValueError('Not a supported raster image')
    if width <= 32 or height <= 32 or width * height > 100_000_000:
        raise ValueError('Image dimensions are outside the viewer limits')
    return width, height


def discover(refresh):
    found = []
    index = ZoneIndex()
    index.feed(read_url(STORIES + '/zones/', refresh))
    failures = []
    for path in sorted(index.paths):
        try:
            gallery = MapGallery()
            gallery.feed(read_url(STORIES + path, refresh))
            if not gallery.zone:
                raise ValueError('Zone title missing; page markup may have changed')
            for image, label in gallery.images:
                found.append(candidate(gallery.zone, image, label, STORIES + path))
        except Exception as error:
            failures.append({'source': STORIES + path, 'error': str(error)})
    atlas = json.loads(read_url(ATLAS_DATA, refresh))
    for entry in atlas['maps']:
        attribution = entry.get('attribution', {})
        image = image_from_source(attribution.get('sourceUrl', ''))
        if image:
            found.append(candidate(entry['title'].replace('’', "'"), image, attribution.get('sourceTitle', ''), 'https://www.mnmatlas.com/' + entry['id'] + '/'))
    # The quest repo's zone catalog identifies this as a floor plan in the image field,
    # rather than in its (empty) primary map field.
    found.append(candidate('Blind Midden', IMAGE_ROOT + '7/71/Blind_Midden_-_Ground_Floor.png', 'Ground floor',
        'https://github.com/amyklindley/mo-betta-quests/blob/main/zones.json'))
    return found, failures


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--refresh', action='store_true', help='Re-fetch cached public source pages and new images')
    parser.add_argument('--discover-only', action='store_true', help='Write candidate report without changing the shipped catalog')
    args = parser.parse_args()
    existing = json.loads(CATALOG.read_text(encoding='utf-8'))
    existing = [entry for entry in existing if original_image(entry['imageUrl'])]
    for entry in existing:
        filename = urllib.parse.unquote(original_image(entry['imageUrl']).rsplit('/', 1)[-1])
        if filename in MAP_NAMES:
            entry['name'] = MAP_NAMES[filename]
    found, failures = discover(args.refresh)
    candidates = {original_image(entry['imageUrl']): entry for entry in found}
    old_urls = {original_image(entry['imageUrl']) for entry in existing}
    additions = [entry for url, entry in candidates.items() if url not in old_urls]
    report = {'candidates': additions, 'failures': failures, 'verified': []}
    if not args.discover_only:
        for entry in additions:
            try:
                try:
                    width, height = check_image(read_url(entry['imageUrl'], args.refresh, binary=True))
                except ValueError:
                    # Ask the wiki's image server for a smaller published rendition.
                    # Keep the original file description link and the same map ID.
                    original = entry['imageUrl']
                    relative = original.removeprefix(IMAGE_ROOT)
                    filename = relative.rsplit('/', 1)[-1]
                    rendition = IMAGE_ROOT + 'thumb/' + relative + '/4096px-' + filename
                    width, height = check_image(read_url(rendition, args.refresh, binary=True))
                    entry['imageUrl'] = rendition
                report['verified'].append(entry)
                print(f"Verified {entry['name']}: {width} x {height}", flush=True)
            except Exception as error:
                failures.append({'source': entry['imageUrl'], 'error': str(error)})
                print(f"Skipped {entry['name']}: {error}", flush=True)
        merged = existing + report['verified']
        if len({entry['id'] for entry in merged}) != len(merged):
            raise ValueError('Duplicate IDs; the catalog was not changed')
        temporary = CATALOG.with_suffix('.json.tmp')
        temporary.write_text(json.dumps(sorted(merged, key=lambda entry: entry['name'].casefold()), indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
        temporary.replace(CATALOG)
    report_path = ROOT / 'artifacts/map-catalog-report.json'
    report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(f"{len(existing)} existing maps; {len(additions)} new candidates; {len(report['verified'])} verified additions. Report: {report_path}", flush=True)


if __name__ == '__main__':
    main()
