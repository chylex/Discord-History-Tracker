var UTILS = {
  /*
   * Reads a file object and converts the JSON-formatted contents to a JS object. The callback accepts the parsed object (or null on failure), and the original file object.
   */
  readJsonFile: function(file, callback){
    var reader = new FileReader();

    reader.onload = function(){
      var obj;

      try{
        obj = JSON.parse(reader.result);
      }catch(e){
        console.error(e);
        callback(null, file);
        return;
      }

      callback(obj, file);
    };

    reader.readAsText(file, "UTF-8");
  }
};
