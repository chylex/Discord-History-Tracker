import discord from "./discord.mjs";

// ------------------------
// Global filter generators
// ------------------------

const filter = {
	byUser: ((user) => message => message.u === user),
	byTime: ((timeStart, timeEnd) => message => message.t >= timeStart && message.t <= timeEnd),
	byContents: ((substr) => message => ("m" in message ? message.m : "").indexOf(substr) !== -1),
	byRegex: ((regex) => message => regex.test("m" in message ? message.m : "")),
	withImages: (() => message => (message.e && message.e.some(embed => embed.type === "image")) || (message.a && message.a.some(discord.isImageAttachment))),
	withDownloads: (() => message => message.a && message.a.some(attachment => !discord.isImageAttachment(attachment))),
	withEmbeds: (() => message => message.e && message.e.length > 0),
	withAttachments: (() => message => message.a && message.a.length > 0),
	isEdited: (() => message => ("te" in message) ? message.te : (message.f & 1) === 1)
};

// --------------
// Global sorters
// --------------

const sorter = {
	oldestToNewest: (key1, key2) => {
		if (key1.length === key2.length) {
			return key1 > key2 ? 1 : key1 < key2 ? -1 : 0;
		}
		else {
			return key1.length > key2.length ? 1 : -1;
		}
	},
	
	newestToOldest: (key1, key2) => {
		if (key1.length === key2.length) {
			return key1 > key2 ? -1 : key1 < key2 ? 1 : 0;
		}
		else {
			return key1.length > key2.length ? -1 : 1;
		}
	}
};

export default {
	FILTER: filter,
	SORTER: sorter
};
