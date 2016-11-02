var PROCESSOR = function(messageObject){
};

// ------------------------
// Global filter generators
// ------------------------

PROCESSOR.FILTER = {
  byUser: ((userindex) => message => message.u === userindex),
  byTime: ((timeStart, timeEnd) => message => message.t >= timeStart && message.t <= timeEnd),
  byContents: ((search) => search.test ? message => search.test(message.m) : message => message.m.indexOf(search) !== -1),
  withEmbeds: (() => message => message.e && message.e.length > 0),
  withAttachments: (() => message => message.a && message.a.length > 0),
  isEdited: (() => message => (message.f&1) === 1)
};

// --------------
// Global sorters
// --------------

PROCESSOR.SORTER = {
  oldestToNewest: (key1, key2) => {
    if (key1.length === key2.length){
      return key1 > key2 ? 1 : key1 < key2 ? -1 : 0;
    }
    else{
      return key1.length > key2.length ? 1 : -1;
    }
  },
  
  newestToOldest: (key1, key2) => {
    if (key1.length === key2.length){
      return key1 > key2 ? -1 : key1 < key2 ? 1 : 0;
    }
    else{
      return key1.length > key2.length ? -1 : 1;
    }
  }
};
