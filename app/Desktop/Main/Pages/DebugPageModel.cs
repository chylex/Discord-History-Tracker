#if DEBUG
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Desktop.Dialogs.Progress;
using DHT.Server;
using DHT.Server.Data;
using DHT.Server.Service;

namespace DHT.Desktop.Main.Pages {
	sealed class DebugPageModel {
		public string GenerateChannels { get; set; } = "0";
		public string GenerateUsers { get; set; } = "0";
		public string GenerateMessages { get; set; } = "0";

		private readonly Window window;
		private readonly State state;

		[Obsolete("Designer")]
		public DebugPageModel() : this(null!, State.Dummy) {}

		public DebugPageModel(Window window, State state) {
			this.window = window;
			this.state = state;
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

			await ProgressDialog.Show(window, "Generating Random Data", async (_, callback) => await GenerateRandomData(channels, users, messages, callback));
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

			await state.Db.Users.Add(users);
			await state.Db.Servers.Add([server]);
			await state.Db.Channels.Add(channels);

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
						Attachments = ImmutableList<Attachment>.Empty,
						Embeds = ImmutableList<Embed>.Empty,
						Reactions = ImmutableList<Reaction>.Empty,
					};
				}).ToArray();

				await state.Db.Messages.Add(messages);

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

		private static readonly string[] RandomWords = [
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
			"zucchini"
		];

		private static string RandomText(Random rand, int maxWords) {
			int wordCount = 1 + (int) Math.Floor(maxWords * Math.Pow(rand.NextDouble(), 3));
			return string.Join(' ', Enumerable.Range(0, wordCount).Select(_ => RandomWords[rand.Next(RandomWords.Length)]));
		}
	}
}
#else
namespace DHT.Desktop.Main.Pages {
	sealed class DebugPageModel {
		public string GenerateChannels { get; set; } = "0";
		public string GenerateUsers { get; set; } = "0";
		public string GenerateMessages { get; set; } = "0";

		public void OnClickAddRandomDataToDatabase() {}
	}
}
#endif
