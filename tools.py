import sys
from os import path
import os
import zipfile

sys.stdout.reconfigure(encoding='utf-8')

ROOT_PATH = os.getcwd()
FILE_PATH = path.join("RimworldExtractorGUI", "Program.cs")
PORTABLE_PATH = path.join("publish_standard", "RimworldExtractorGUI.exe")


def get_current_version():
    if not path.exists(FILE_PATH):
        print(f'no file exists in {FILE_PATH}')
        return False
    try:
        with open(FILE_PATH, 'r') as file:
            line = file.readline()
            while line:
                if "string VERSION" in line:
                    start = line.index('"') + 1
                    end = line.rindex('"')
                    version = line[start:end]
                    print(f"::set-output name=tag_name::{version}")
                    return True
                line = file.readline()
    except ValueError as err:
        print("ValueError {0}".format(err))
    return False


def cleanup_standard():
    standard_path = "publish_standard"
    bin_path = path.join(standard_path, "bin")
    os.mkdir(bin_path)
    os.listdir(standard_path)
    for entry in os.listdir(standard_path):
        full_path = path.join(standard_path, entry)
        if path.isdir(full_path):
            continue
        ext = entry.split('.')[-1]
        if ext == 'dll' and entry != 'RimworldExtractorGUI.dll':
            os.rename(full_path, path.join(bin_path, entry))
    os.remove(path.join(standard_path, "RimworldExtractorGUI.deps.json"))
    os.remove(path.join(standard_path, "RimworldExtractorInternal.pdb"))

    zip_filename = path.join(ROOT_PATH, 'standard.zip')
    with zipfile.ZipFile(zip_filename, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, _, files in os.walk(standard_path):
            for file in files:
                zipf.write(path.join(root, file), path.relpath(os.path.join(root, file), standard_path))
    print(zip_filename)


def cleanup_portable():
    portable_path = "publish_portable"
    print(path.exists(path.join(portable_path, 'RimworldExtractorGUI.exe')))
    os.rename(path.join(portable_path, 'RimworldExtractorGUI.exe'), path.join(ROOT_PATH, 'portable.exe'))
    print(path.join(ROOT_PATH, 'portable.exe'))


if __name__ == "__main__":
    print(ROOT_PATH)
    command = sys.argv[1]
    if command == 'version':
        result = get_current_version()
        if result is True:
            quit(0)
        else:
            quit(1)
    elif command == 'cleanup':
        cleanup_standard()
        cleanup_portable()
