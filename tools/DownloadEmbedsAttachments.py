import argparse
import json
import sys
import shutil
import pathlib
import random
import re
import requests
from os import path

ap = argparse.ArgumentParser(description = "Downloads attachments and image embeds from a DHT archive into one folder, and updates URLs in the archive to point to the downloaded files. The original archive will be backed up with a '.bak' extension; if a backup file already exists, it will NOT be overwritten.")

ap.add_argument("input_archive",
                metavar = "input-archive",
                help = "path to archive file")

ap.add_argument("-d", "--download-folder-name",
                default = "",
                required = False,
                metavar = "name",
                dest = "download_folder_name",
                help = "name of folder with downloaded files (if omitted, will be same as input archive name with '.downloads' appended to the end)")

if len(sys.argv) == 1:
    ap.print_help()
    exit(1)

args = ap.parse_args()

input_archive = args.input_archive
download_folder_name = args.download_folder_name if len(args.download_folder_name) else path.basename(input_archive) + ".downloads"
download_folder_path = path.join(pathlib.Path(input_archive).resolve().with_name(download_folder_name))

# Setup

try:
    pathlib.Path(download_folder_path).mkdir(parents = True, exist_ok = True)
except OSError as e:
    print("Could not create download folder: " + str(e))
    exit(1)

try:
    with open(input_archive, "r", encoding = "UTF-8") as f:
        archive = json.load(f)
except FileNotFoundError:
    print("Input archive file not found: " + input_archive)
    exit(1)
except json.JSONDecodeError as e:
    print("Input archive file has invalid format: " + str(e))
    exit(1)

backup_archive = input_archive + ".bak"

if not path.isfile(backup_archive):
    shutil.copy(input_archive, backup_archive)

# Collect

download_url_to_ele = {}
download_url_to_file = {}
download_file_to_url = {}


def add_file_to_download(ele):
    url = ele["url"]
    
    if url.startswith("file:"):
        return
    
    url_split = url.split("://", maxsplit = 2)
    
    if len(url_split) != 2:
        print("Invalid attachment URL: " + url)
        return
    
    download_file_name = re.sub(r"[^\w\-_.]", "_", url_split[1])
    
    while download_file_name in download_file_to_url:
        download_file_name += "_" + str(random.randint(0, 9))
    
    download_url_to_ele[url] = ele
    download_url_to_file[url] = download_file_name
    download_file_to_url[download_file_name] = url


archive_data = archive["data"]

for channel_id, channel_data in archive_data.items():
    for message_id, message_data in channel_data.items():
        if "a" in message_data:
            for attachment in message_data["a"]:
                add_file_to_download(attachment)
        
        if "e" in message_data:
            for embed in message_data["e"]:
                if embed["type"] == "image":
                    add_file_to_download(embed)

# Download

counter = 0
total = len(download_url_to_file)
digits = len(str(total))

print("Identified {} attachment(s) and image embed(s) to download.".format(total))
print("")

failed = list()

for url, file in download_url_to_file.items():
    counter += 1
    print("[{}/{}] {}".format(str(counter).rjust(digits, " "), total, url))
    
    full_path = download_folder_path + "/" + file
    
    if path.isfile(full_path):
        print("Already downloaded, skipping...")
    else:
        try:
            req = requests.get(url, timeout = 2)
            req.raise_for_status()
            
            with open(full_path, "wb") as f:
                f.write(req.content)
        
        except Exception as e:
            failed.append((url, file))
            print("Download failed... {}".format(e))
            continue
    
    download_url_to_ele[url]["url"] = "file:./" + download_folder_name + "/" + file

# Update

with open(input_archive, "w", encoding = "UTF-8") as f:
    json.dump(archive, f, separators = (",", ":"))

print("")

if len(failed) > 0:
    print("Archive was updated, but {} out of {} request(s) failed. You may re-run the script to try re-downloading failed requests again.".format(len(failed), total))
else:
    print("Archive was updated.")

print("To view the archive with downloaded files, you must place the viewer in the same folder as the archive file and download folder.")
