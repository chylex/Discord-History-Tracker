#!/usr/bin/env python3

import glob
import os
import shutil
import sys

if os.name == "nt":
    uglifyjs = os.path.abspath("../../lib/uglifyjs.cmd")
else:
    uglifyjs = "uglifyjs"

if shutil.which(uglifyjs) is None:
    print("Cannot find executable: {0}".format(uglifyjs))
    sys.exit(1)

input_dir = os.path.abspath("./Tracker/scripts")
output_dir = os.path.abspath("../Desktop/bin/.res/scripts")

os.makedirs(output_dir, exist_ok=True)

for file in glob.glob(input_dir + "/*.js"):
    name = os.path.basename(file)
    print("Minifying {0}...".format(name))
    os.system("{0} {1} -o {2}/{3}".format(uglifyjs, file, output_dir, name))

print("Done!")
