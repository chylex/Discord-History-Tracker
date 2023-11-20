var PROCESSOR = {};

// ------------------------
// Global filter generators
// ------------------------

PROCESSOR.FILTER = {
  byUser: ((userindex) => message => message.u === userindex),
  byTime: ((timeStart, timeEnd) => message => message.t >= timeStart && message.t <= timeEnd),
  byContents: ((substr) => message => ("m" in message ? message.m : "").indexOf(substr) !== -1),
  byRegex: ((regex) => message => regex.test("m" in message ? message.m : "")),
  withImages: (() => message => (message.e && message.e.some(embed => embed.type === "image")) || (message.a && message.a.some(DISCORD.isImageAttachment))),
  withDownloads: (() => message => message.a && message.a.some(attachment => !DISCORD.isImageAttachment(attachment))),
  withEmbeds: (() => message => message.e && message.e.length > 0),
  withAttachments: (() => message => message.a && message.a.length > 0),
  isEdited: (() => message => ("te" in message) ? message.te : (message.f & 1) === 1)
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
