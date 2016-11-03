# Python 3

import fileinput
import glob
import shutil
import sys
import os


EXEC_UGLIFYJS = "uglifyjs --bare-returns --compress --mangle --mangle-props --reserve-domprops --reserved-file reserve.txt --screw-ie8 --output \"{1}\" \"{0}\""
EXEC_CLOSURECOMPILER = "java -jar lib/closure-compiler-v20160911.jar --js \"{0}\" --js_output_file \"{1}\""
EXEC_YUI = "java -jar lib/yuicompressor-2.4.8.jar --charset utf-8 --line-break 160 --type css -o \"{1}\" \"{0}\""

USE_UGLIFYJS = shutil.which("uglifyjs") != None and not "--closure" in sys.argv and not "--nominify" in sys.argv
USE_JAVA = shutil.which("java") != None and not "--nominify" in sys.argv


def combine_files(input_pattern, output_file):
  with fileinput.input(sorted(glob.glob(input_pattern))) as stream:
    for line in stream:
      output_file.write(line)


def build_tracker():
  output_file_raw = "bld/track.js"
  output_file_bookmark = "bld/track.html"
  
  output_file_tmp = "bld/track.tmp.js"
  input_pattern = "src/tracker/*.js"
  
  with open(output_file_raw, "w") as out:
    if not USE_UGLIFYJS:
      out.write("(function(){\n")
    
    combine_files(input_pattern, out)
    
    if not USE_UGLIFYJS:
      out.write("})()")
  
  if USE_UGLIFYJS:
    os.system(EXEC_UGLIFYJS.format(output_file_raw, output_file_tmp))
  elif USE_JAVA:
    os.system(EXEC_CLOSURECOMPILER.format(output_file_raw, output_file_tmp))
  else:
    return
  
  with open(output_file_raw, "w") as out:
    out.write("javascript:(function(){")
    
    with open(output_file_tmp, "r") as minified:
      out.write(minified.read().replace("\n", " ").replace("\r", ""))
    
    out.write("})()")
    
  os.remove(output_file_tmp)
  
  with open(output_file_bookmark, "w") as out:
    out.write("<a href='")
    
    with open(output_file_raw, "r") as raw:
      out.write(raw.read().replace("&", "&amp;").replace('"', "&quot;").replace("'", "&#x27;").replace("<", "&lt;").replace(">", "&gt;"))
    
    out.write("'>Add Bookmark</a>")


def build_renderer():
  output_file = "bld/render.html"
  input_html = "src/renderer/index.html"
  
  input_css_pattern = "src/renderer/*.css"
  tmp_css_file_combined = "bld/render.tmp.css"
  tmp_css_file_minified = "bld/render.min.css"
  
  with open(tmp_css_file_combined, "w") as out:
    combine_files(input_css_pattern, out)
  
  if USE_JAVA:
    os.system(EXEC_YUI.format(tmp_css_file_combined, tmp_css_file_minified))
  else:
    shutil.copyfile(tmp_css_file_combined, tmp_css_file_minified)
    
  os.remove(tmp_css_file_combined)
  
  input_js_pattern = "src/renderer/*.js"
  tmp_js_file_combined = "bld/render.tmp.js"
  tmp_js_file_minified = "bld/render.min.js"
  
  with open(tmp_js_file_combined, "w") as out:
    combine_files(input_js_pattern, out)
  
  if USE_UGLIFYJS:
    os.system(EXEC_UGLIFYJS.format(tmp_js_file_combined, tmp_js_file_minified))
  elif USE_JAVA:
    os.system(EXEC_CLOSURECOMPILER.format(tmp_js_file_combined, tmp_js_file_minified))
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


os.makedirs("bld", exist_ok = True)

print("Building tracker...")
build_tracker()
print("Building renderer...")
build_renderer()
