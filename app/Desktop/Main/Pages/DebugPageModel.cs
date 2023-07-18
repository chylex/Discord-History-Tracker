#if DEBUG
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Server.Data;
using DHT.Server.Database;
using DHT.Server.Service;
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Pages {
	sealed class DebugPageModel : BaseModel {
		public string GenerateChannels { get; set; } = "0";
		public string GenerateUsers { get; set; } = "0";
		public string GenerateMessages { get; set; } = "0";

		private readonly Window window;
		private readonly IDatabaseFile db;

		[Obsolete("Designer")]
		public DebugPageModel() : this(null!, DummyDatabaseFile.Instance) {}

		public DebugPageModel(Window window, IDatabaseFile db) {
			this.window = window;
			this.db = db;
		}

		public async void OnClickAddRandomDataToDatabase() {
			if (!int.TryParse(GenerateChannels, out int channels) || channels < 1) {
				await Dialog.ShowOk(window, "Generate Random Data", "Amount of channels must be at least 1!");
				return;
			}

			if (!int.TryParse(GenerateUsers, out int users) || users < 1) {
				await Dialog.ShowOk(window, "Generate Random Data", "Amount of users must be at least 1!");
				return;
			}

			if (!int.TryParse(GenerateMessages, out int messages) || messages < 1) {
				await Dialog.ShowOk(window, "Generate Random Data", "Amount of messages must be at least 1!");
				return;
			}

			ProgressDialog progressDialog = new ProgressDialog {
				DataContext = new ProgressDialogModel(async callback => await GenerateRandomData(channels, users, messages, callback)) {
					Title = "Generating Random Data"
				}
			};

			await progressDialog.ShowDialog(window);
		}

		private const int BatchSize = 500;

		private async Task GenerateRandomData(int channelCount, int userCount, int messageCount, IProgressCallback callback) {
			int batchCount = (messageCount + BatchSize - 1) / BatchSize;
			await callback.Update("Adding messages in batches of " + BatchSize, 0, batchCount);

			var rand = new Random();
			var server = new DHT.Server.Data.Server {
				Id = RandomId(rand),
				Name = RandomName("s"),
				Type = ServerType.Server,
			};

			var channels = Enumerable.Range(0, channelCount).Select(i => new Channel {
				Id = RandomId(rand),
				Server = server.Id,
				Name = RandomName("c"),
				ParentId = null,
				Position = i,
				Topic = RandomText(rand, 10),
				Nsfw = rand.Next(4) == 0,
			}).ToArray();

			var users = Enumerable.Range(0, userCount).Select(_ => new User {
				Id = RandomId(rand),
				Name = RandomName("u"),
				AvatarUrl = null,
				Discriminator = rand.Next(0, 9999).ToString(),
			}).ToArray();

			db.AddServer(server);
			db.AddUsers(users);

			foreach (var channel in channels) {
				db.AddChannel(channel);
			}

			var now = DateTimeOffset.Now;
			int batchIndex = 0;

			while (messageCount > 0) {
				int hourOffset = batchIndex;

				var messages = Enumerable.Range(0, Math.Min(messageCount, BatchSize)).Select(i => {
					DateTimeOffset time = now.AddHours(hourOffset).AddMinutes(i * 60.0 / BatchSize);
					DateTimeOffset? edit = rand.Next(100) == 0 ? time.AddSeconds(rand.Next(1, 60)) : null;

					var timeMillis = time.ToUnixTimeMilliseconds();
					var editMillis = edit?.ToUnixTimeMilliseconds();

					return new Message {
						Id = (ulong) timeMillis,
						Sender = RandomBiasedIndex(rand, users).Id,
						Channel = RandomBiasedIndex(rand, channels).Id,
						Text = RandomText(rand, 100),
						Timestamp = timeMillis,
						EditTimestamp = editMillis,
						RepliedToId = null,
						Attachments = ImmutableArray<Attachment>.Empty,
						Embeds = ImmutableArray<Embed>.Empty,
						Reactions = ImmutableArray<Reaction>.Empty,
					};
				}).ToArray();

				db.AddMessages(messages);

				messageCount -= BatchSize;
				await callback.Update("Adding messages in batches of " + BatchSize, ++batchIndex, batchCount);
			}
		}

		private static ulong RandomId(Random rand) {
			ulong h = unchecked((ulong) rand.Next());
			ulong l = unchecked((ulong) rand.Next());
			return (h << 32) | l;
		}

		private static string RandomName(string prefix) {
			return prefix + "-" + ServerUtils.GenerateRandomToken(5);
		}

		private static T RandomBiasedIndex<T>(Random rand, T[] options) {
			return options[(int) Math.Floor(options.Length * rand.NextDouble() * rand.NextDouble())];
		}

		private static readonly string[] RandomWords = {
			"apple", "apricot", "artichoke", "arugula", "asparagus", "avocado",
			"banana", "bean", "beechnut", "beet", "blackberry", "blackcurrant", "blueberry", "boysenberry", "bramble", "broccoli",
			"cabbage", "cacao", "cantaloupe", "caper", "carambola", "carrot", "cauliflower", "celery", "chard", "cherry", "chokeberry", "citron", "clementine", "coconut", "corn", "crabapple", "cranberry", "cucumber", "currant",
			"daikon", "date", "dewberry", "durian",
			"edamame", "eggplant", "elderberry", "endive",
			"fig",
			"garlic", "ginger", "gooseberry", "grape", "grapefruit", "guava",
			"honeysuckle", "horseradish", "huckleberry",
			"jackfruit", "jicama",
			"kale", "kiwi", "kohlrabi", "kumquat",
			"leek", "lemon", "lentil", "lettuce", "lime",
			"mandarin", "mango", "mushroom", "myrtle",
			"nectarine", "nut",
			"olive", "okra", "onion", "orange",
			"papaya", "parsnip", "pawpaw", "peach", "pear", "pea", "pepper", "persimmon", "pineapple", "plum", "plantain", "pomegranate", "pomelo", "potato", "prune", "pumpkin",
			"quandong", "quinoa",
			"radicchio", "radish", "raisin", "raspberry", "redcurrant", "rhubarb", "rutabaga",
			"spinach", "strawberry", "squash",
			"tamarind", "tangerine", "tomatillo", "tomato", "turnip",
			"vanilla",
			"watercress", "watermelon",
			"yam",
			"zucchini",
		};

		private static string RandomText(Random rand, int maxWords) {
			int wordCount = 1 + (int) Math.Floor(maxWords * Math.Pow(rand.NextDouble(), 3));
			return string.Join(' ', Enumerable.Range(0, wordCount).Select(_ => RandomWords[rand.Next(RandomWords.Length)]));
		}
	}
}
#else
using DHT.Utils.Models;

namespace DHT.Desktop.Main.Pages {
	sealed class DebugPageModel : BaseModel {
		public string GenerateChannels { get; set; } = "0";
		public string GenerateUsers { get; set; } = "0";
		public string GenerateMessages { get; set; } = "0";

		public void OnClickAddRandomDataToDatabase() {}
	}
}
#endif
