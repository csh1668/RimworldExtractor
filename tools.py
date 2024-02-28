import sys
from os import path
import os
import zipfile
import codecs

sys.stdout.reconfigure(encoding='utf-8')

ROOT_PATH = os.getcwd()
FILE_PATH = path.join("RimworldExtractorGUI", "Program.cs")
PORTABLE_PATH = path.join("publish_standard", "RimworldExtractorGUI.exe")
TEMPLATE_PATH = path.join('.github', 'release_template.txt')


def write_output(key, value):
    if 'GITHUB_OUTPUT' in os.environ:
        with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as state_file:
            state_file.write(f"{key}={value}\n")
        print(f'GITHUB_OUTPUT: written {key}={value}')
    else:
        print(f'{key}={value}')


def edit_template(changelog):
    with open(TEMPLATE_PATH, 'r', encoding='utf-8') as file:
        lines = file.readlines()
    lines.insert(0, changelog.strip())
    lines.insert(1, '\n')
    lines.insert(1, '\n')
    with open(TEMPLATE_PATH, 'w', encoding='utf-8') as file:
        file.writelines(lines)


def write_current_version(version):
    if not path.exists(FILE_PATH):
        print(f'no file exists in {FILE_PATH}')
        return False
    try:
        lines = []
        with open(FILE_PATH, 'r', encoding='utf-8') as file:
            lines = file.readlines()
            for (idx, line) in enumerate(lines):
                if "string VERSION" in line:
                    start = line.index('"') + 1
                    lines[idx] = f'{lines[idx][:start]}{version}";'
                    print(f'write_current_version done: {lines[idx]}')
                    break
        with open(FILE_PATH, 'w', encoding='utf-8') as file:
            file.writelines(lines)
    except ValueError as err:
        print("ValueError {0}".format(err))
        return False
    return True


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

    zip_filename = path.join(ROOT_PATH, 'RimworldExtractor-Standard.zip')
    with zipfile.ZipFile(zip_filename, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, _, files in os.walk(standard_path):
            for file in files:
                zipf.write(path.join(root, file), path.relpath(os.path.join(root, file), standard_path))
    print(zip_filename)


def cleanup_portable():
    portable_path = "publish_portable"
    print(path.exists(path.join(portable_path, 'RimworldExtractorGUI.exe')))
    new_path = path.join(ROOT_PATH, 'RimworldExtractor-Portable.exe')
    os.rename(path.join(portable_path, 'RimworldExtractorGUI.exe'), new_path)
    print(new_path)


def convert_utf8(file_path):
    print(f'convert_utf8: {file_path}')
    encodings_to_try = ['utf-8', 'cp949']
    for encoding in encodings_to_try:
        try:
            with codecs.open(file_path, 'r', encoding=encoding) as f:
                content = f.read()
            with codecs.open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Successfully converted from {encoding} to UTF-8.")
            return
        except UnicodeDecodeError as err:
            print(f"Failed to decode with {encoding}: {err}")


def ensure_utf8():
    for root, _, files in os.walk(ROOT_PATH):
        for file in files:
            full_path = path.join(root, file)
            if not file.endswith('.cs'):
                continue
            convert_utf8(full_path)


if __name__ == "__main__":
    print(ROOT_PATH)
    command = sys.argv[1]
    if command == 'version':
        result = write_current_version(sys.argv[2])
        if result is True:
            quit(0)
        else:
            quit(1)
    elif command == 'cleanup':
        cleanup_standard()
        cleanup_portable()
    elif command == 'utf8':
        ensure_utf8()
    elif command == 'template':
        edit_template(sys.argv[2])
