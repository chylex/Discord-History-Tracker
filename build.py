# Python 3

import fileinput
import glob
import os


def combine_files(input_pattern, output_file):
  with fileinput.input(sorted(glob.glob(input_pattern))) as stream:
      for line in stream:
        output_file.write(line)


def build_tracker():
  output_file = "bld/track.js"
  output_file_tmp = "bld/track.tmp.js"
  input_pattern = "src/tracker/*.js"
  
  with open(output_file, "w") as out:
    out.write("(function(){")
    combine_files(input_pattern, out)
    out.write("})()")
  
  os.system("java -jar lib/closure-compiler-v20160911.jar --js \"{0}\" --js_output_file=\"{1}\"".format(output_file, output_file_tmp))
  
  with open(output_file, "w") as out:
    out.write("javascript:(function(){")
    
    with open(output_file_tmp, "r") as minified:
      out.write(minified.read().replace("\n", " ").replace("\r", ""))
    
    out.write("})()")
    
  os.remove(output_file_tmp)
  

def build_renderer():
  output_file = "bld/render.html"
  input_html = "src/renderer/index.html"
  
  input_css_pattern = "src/renderer/*.css"
  tmp_css_file_combined = "bld/render.tmp.css"
  tmp_css_file_minified = "bld/render.min.css"
  
  with open(tmp_css_file_combined, "w") as out:
    combine_files(input_css_pattern, out)
  
  os.system("java -jar lib/yuicompressor-2.4.8.jar --charset utf-8 --line-break 160 --type css -o \"{1}\" \"{0}\"".format(tmp_css_file_combined, tmp_css_file_minified))
  os.remove(tmp_css_file_combined)
  
  input_js_pattern = "src/renderer/*.js"
  tmp_js_file_combined = "bld/render.tmp.js"
  tmp_js_file_minified = "bld/render.min.js"
  
  with open(tmp_js_file_combined, "w") as out:
    combine_files(input_js_pattern, out)
  
  os.system("java -jar lib/closure-compiler-v20160911.jar --js \"{0}\" --js_output_file=\"{1}\"".format(tmp_js_file_combined, tmp_js_file_minified))
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


print("Building tracker...")
build_tracker()
print("Building renderer...")
build_renderer()
