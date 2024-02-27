import sys
from os import path

FILE_PATH = path.join("RimworldExtractorGUI", "Program.cs")


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


def cleanup_publish():
    for entry in os.listdir(directory):
        # entry가 디렉토리인지 파일인지 확인
        full_path = os.path.join(directory, entry)
        if os.path.isdir(full_path):
            print("Directory:", full_path)
            # 하위 폴더의 내용을 재귀적으로 출력
            list_subdirectories_and_files(full_path)
        else:
            print("File:", full_path)


if __name__ == "__main__":
    command = sys.argv[1]
    if command is 'version':
        result = get_current_version()
        if result is True:
            quit(0)
        else:
            quit(1)
    elif command is 'cleanup':
        cleanup_publish()
