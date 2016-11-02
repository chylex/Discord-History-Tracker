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
  },

  /*
   * Runs the callback with a key and value for each entry in an object.
   */
  forEachEntry: function(object, callback){
    for(var key of Object.keys(object)){
      callback(key, object[key]);
    }
  },

  /*
   * Runs the callback for each value in an object.
   */
  forEachValue: function(object, callback){
    for(var key of Object.keys(object)){
      callback(object[key]);
    }
  },

  /*
   * Adds all entries from the source object to the target object.
   */
  combineObjects: function(target, source){
    for(var key of Object.keys(source)){
      target[key] = source[key];
    }
  }
};
