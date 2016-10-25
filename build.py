# Python 3

import fileinput
import glob
import os


def build_tracker():
  output_file = "bld/track.js"
  output_file_tmp = "bld/track.tmp.js"
  input_pattern = "src/tracker/*.js"
  
  with open(output_file, "w") as out:
    out.write("(function(){")
    
    with fileinput.input(sorted(glob.glob(input_pattern))) as stream:
      for line in stream:
        out.write(line)
    
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
  input_file = "src/renderer/index.html"
  tmp_file = "bld/.tmp"
  
  tokens = {
    "/*{js}*/": "src/renderer/script.js",
    "/*{css}*/": "src/renderer/style.css"
  }
  
  with open(output_file, "w") as out:
    with open(input_file, "r") as fin:
      for line in fin:
        token = None
        
        for token in (token for token in tokens if token in line):
          token_path = tokens[token]
          file_type = token_path.split("/")[-1].split(".")[-1]
          
          os.system("java -jar lib/yuicompressor-2.4.8.jar --charset utf-8 --line-break 160 --type {2} -o \"{0}\" \"{1}\"".format(tmp_file, token_path, file_type))
          
          with open(tmp_file, "r") as token_file:
            embedded = token_file.read()
          
          out.write(embedded)
          os.remove(tmp_file)
          
        if token is None:
          out.write(line)


print("Building tracker...")
build_tracker()
print("Building renderer...")
build_renderer()
