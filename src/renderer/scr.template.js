var TEMPLATE_REGEX = /{([^{}]+?)}/g;

var TEMPLATE = function(contents){
  this.contents = contents;
};

TEMPLATE.prototype.apply = function(obj, processor){
  return this.contents.replace(TEMPLATE_REGEX, (full, match) => {
    var value = match.split(".").reduce((o, property) => o[property], obj);
    
    if (processor){
      var updated = processor(match, value);
      return typeof updated === "undefined" ? DOM.escapeHTML(value) : updated;
    }
    
    return DOM.escapeHTML(value);
  });
};
