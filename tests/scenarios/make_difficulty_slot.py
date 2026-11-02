"""Write a copy of a SULFUR save slot with a different difficulty.

SULFUR stores a slot as gzip-compressed JSON (JsonUtility) with the difficulty as an
integer field, e.g. "DIFFICULTY":3. The death-protection scenarios need one slot per
difficulty; this produces them from any existing slot that stands in the hub.

Usage:
    python -I make_difficulty_slot.py <source.sss> <dest.sss> <VeryEasy|Easy|Medium|Hard|Hardcore>

The destination is refused if it already exists, unless --force is given.
"""
import gzip
import os
import re
import sys

DIFFICULTIES = {"VeryEasy": 0, "Easy": 1, "Medium": 2, "Hard": 3, "Hardcore": 4}


def main(argv):
    force = "--force" in argv
    args = [a for a in argv if a != "--force"]
    if len(args) != 3 or args[2] not in DIFFICULTIES:
        print(__doc__)
        return 2
    source, dest, name = args
    if os.path.exists(dest) and not force:
        print(f"refusing to overwrite {dest} (pass --force)")
        return 1

    with gzip.open(source, "rb") as f:
        data = f.read()
    matches = re.findall(rb'"DIFFICULTY":\d+', data)
    if len(matches) != 1:
        print(f"expected exactly one DIFFICULTY field in {source}, found {len(matches)}")
        return 1
    data = data.replace(matches[0], b'"DIFFICULTY":%d' % DIFFICULTIES[name])
    if b'"HARDCORE_DEAD":true' in data:
        print("source slot is a dead hardcore save; pick another source")
        return 1

    with gzip.open(dest, "wb") as f:
        f.write(data)
    print(f"{dest}: DIFFICULTY={DIFFICULTIES[name]} ({name})")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
