# Python 3

import glob
import os

uglifyjs = os.path.abspath("../lib/uglifyjs")
input_dir = os.path.abspath("./Resources/Tracker/scripts")
output_dir = os.path.abspath("./Resources/Tracker/scripts.min")

for file in glob.glob(input_dir + "/*.js"):
    name = os.path.basename(file)
    print("Minifying {0}...".format(name))
    os.system("{0} {1} -o {2}/{3}".format(uglifyjs, file, output_dir, name))

print("Done!")
