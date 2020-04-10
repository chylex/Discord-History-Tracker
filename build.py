# Python 3

import fileinput
import glob
import shutil
import sys
import os
import distutils.dir_util


VERSION_SHORT = "BETA v.19"
VERSION_FULL = VERSION_SHORT + ", released 1 Apr 2020"


EXEC_UGLIFYJS_WIN = "{2}/lib/uglifyjs.cmd --parse bare_returns --compress --mangle toplevel --mangle-props keep_quoted,reserved=[{3}] --output \"{1}\" \"{0}\""
EXEC_UGLIFYJS_AUTO = "uglifyjs --parse bare_returns --compress --mangle toplevel --mangle-props keep_quoted,reserved=[{3}] --output \"{1}\" \"{0}\""
EXEC_YUI = "java -jar lib/yuicompressor-2.4.8.jar --charset utf-8 --line-break 160 --type css -o \"{1}\" \"{0}\""

USE_UGLIFYJS = "--nominify" not in sys.argv
USE_JAVA = "--nominify" not in sys.argv
BUILD_WEBSITE = "--website" in sys.argv
CLIPBOARD_TRACKER = "--copytracker" in sys.argv

WORKING_DIR = os.getcwd()


if USE_JAVA and shutil.which("java") is None:
  USE_JAVA = False
  print("Could not find 'java', CSS minification will be disabled")


if os.name == "nt":
  EXEC_UGLIFYJS = EXEC_UGLIFYJS_WIN
else:
  EXEC_UGLIFYJS = EXEC_UGLIFYJS_AUTO
  
  if USE_UGLIFYJS and shutil.which("uglifyjs") is None:
    USE_UGLIFYJS = False
    print("Could not find 'uglifyjs', JS minification will be disabled")


with open("reserve.txt", "r") as reserved:
  RESERVED_PROPS = ",".join(line.strip() for line in reserved.readlines())


def combine_files(input_pattern, output_file):
  with fileinput.input(sorted(glob.glob(input_pattern))) as stream:
    for line in stream:
      output_file.write(line.replace("{{{version:full}}}", VERSION_FULL))


def build_tracker_html():
  output_file_raw = "bld/track.js"
  output_file_html = "bld/track.html"
  
  output_file_tmp = "bld/track.tmp.js"
  input_pattern = "src/tracker/*.js"
  
  with open(output_file_raw, "w") as out:
    if not USE_UGLIFYJS:
      out.write("(function(){\n")
    
    combine_files(input_pattern, out)
    
    if not USE_UGLIFYJS:
      out.write("})()")
  
  if USE_UGLIFYJS:
    os.system(EXEC_UGLIFYJS.format(output_file_raw, output_file_tmp, WORKING_DIR, RESERVED_PROPS))
    
    with open(output_file_raw, "w") as out:
      out.write("javascript:(function(){")
      
      with open(output_file_tmp, "r") as minified:
        out.write(minified.read().replace("\n", " ").replace("\r", ""))
      
      out.write("})()")
    
    os.remove(output_file_tmp)
  
  with open(output_file_raw, "r") as raw:
    script_contents = raw.read().replace("&", "&amp;").replace('"', "&quot;").replace("'", "&#x27;").replace("<", "&lt;").replace(">", "&gt;")
  
  with open(output_file_html, "w") as out:
    out.write(script_contents)


def build_tracker_userscript():
  output_file = "bld/track.user.js"
  
  input_pattern = "src/tracker/*.js"
  userscript_base = "src/base/track.user.js"
  
  with open(userscript_base, "r") as base:
    userscript_contents = base.read().replace("{{{version}}}", VERSION_SHORT).split("{{{contents}}}")
  
  with open(output_file, "w") as out:
    out.write(userscript_contents[0])
    combine_files(input_pattern, out)
    out.write(userscript_contents[1])


def build_renderer():
  output_file = "bld/viewer.html"
  input_html = "src/renderer/index.html"
  
  input_css_pattern = "src/renderer/*.css"
  tmp_css_file_combined = "bld/viewer.tmp.css"
  tmp_css_file_minified = "bld/viewer.min.css"
  
  with open(tmp_css_file_combined, "w") as out:
    combine_files(input_css_pattern, out)
  
  if USE_JAVA:
    os.system(EXEC_YUI.format(tmp_css_file_combined, tmp_css_file_minified))
  else:
    shutil.copyfile(tmp_css_file_combined, tmp_css_file_minified)
    
  os.remove(tmp_css_file_combined)
  
  input_js_pattern = "src/renderer/*.js"
  tmp_js_file_combined = "bld/viewer.tmp.js"
  tmp_js_file_minified = "bld/viewer.min.js"
  
  with open(tmp_js_file_combined, "w") as out:
    combine_files(input_js_pattern, out)
  
  if USE_UGLIFYJS:
    os.system(EXEC_UGLIFYJS.format(tmp_js_file_combined, tmp_js_file_minified, WORKING_DIR, RESERVED_PROPS))
  else:
    shutil.copyfile(tmp_js_file_combined, tmp_js_file_minified)
  
  os.remove(tmp_js_file_combined)
  
  tokens = {
    "/*{js}*/": tmp_js_file_minified,
    "/*{css}*/": tmp_css_file_minified
  }
  
  with open(output_file, "w") as out:
    with open(input_html, "r") as fin:
      for line in fin:
        token = None
        
        for token in (token for token in tokens if token in line):
          with open(tokens[token], "r") as token_file:
            embedded = token_file.read()
          
          out.write(embedded)
          os.remove(tokens[token])
          
        if token is None:
          out.write(line)


def build_website():
  tracker_file_html = "bld/track.html"
  tracker_file_userscript = "bld/track.user.js"
  viewer_file = "bld/viewer.html"
  web_style_file = "bld/web/style.css"
  
  distutils.dir_util.copy_tree("web", "bld/web")
  index_file = "bld/web/index.php"
  
  with open(index_file, "r") as index:
    index_contents = index.read()
  
  with open(index_file, "w") as index:
    index.write(index_contents.replace("{{{version:web}}}", VERSION_SHORT.replace(" ", "&nbsp;")))
  
  os.makedirs("bld/web/build", exist_ok = True)
  shutil.copyfile(tracker_file_html, "bld/web/build/track.html")
  shutil.copyfile(tracker_file_userscript, "bld/web/build/track.user.js")
  shutil.copyfile(viewer_file, "bld/web/build/viewer.html")
  
  if USE_JAVA:
    os.system(EXEC_YUI.format(web_style_file, web_style_file))


os.makedirs("bld", exist_ok = True)

print("Building tracker html...")
build_tracker_html()

print("Building tracker userscript...")
build_tracker_userscript()

print("Building renderer...")
build_renderer()

if BUILD_WEBSITE:
  print("Building website...")
  build_website()

if CLIPBOARD_TRACKER:
  if os.name == "nt":
    print("Copying to clipboard...")
    os.system("clip < bld/track.js")
  else:
    print("Clipboard is only supported on Windows")

print("Done")
